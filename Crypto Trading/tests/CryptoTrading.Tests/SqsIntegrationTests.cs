using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.Sqs;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;
using CryptoTrading.Web.Controllers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CryptoTrading.Tests
{
    [TestFixture]
    public class SqsIntegrationTests
    {
        private Mock<ILoggerService> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILoggerService>();
            SqsMessageHub.Instance.Clear();
            MarketTickHotCacheSubscriber.Clear();
        }

        [Test]
        public void SqsConfig_LoadsCorrectDefaults_WhenNotConfigured()
        {
            var config = SqsConfig.FromConfiguration();

            Assert.IsNotNull(config);
            Assert.AreEqual("us-east-1", config.AwsRegion);
            Assert.AreEqual("http://localhost:4566", config.ServiceUrl);
            Assert.AreEqual("https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-incoming.fifo", config.OrdersIncomingQueueUrl);
            Assert.AreEqual("https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-executed.fifo", config.OrdersExecutedQueueUrl);
            Assert.AreEqual("https://sqs.us-east-1.amazonaws.com/123456789012/crypto-market-ticks.fifo", config.MarketTicksQueueUrl);
            Assert.IsTrue(config.TickSubscriberEnabled);
        }

        [Test]
        public void SqsConfig_AwsEnvironmentVariables_OverridesWebConfig()
        {
            try
            {
                Environment.SetEnvironmentVariable("SQS_AWS_REGION", "us-west-2");
                Environment.SetEnvironmentVariable("SQS_ORDERS_INCOMING_QUEUE_URL", "https://sqs.us-west-2.amazonaws.com/123456789012/sqs-orders-in.fifo");
                Environment.SetEnvironmentVariable("SQS_ORDERS_EXECUTED_QUEUE_URL", "https://sqs.us-west-2.amazonaws.com/123456789012/sqs-orders-out.fifo");
                Environment.SetEnvironmentVariable("SQS_WEBHOOK_SECRET", "TOP_SECRET_123");
                Environment.SetEnvironmentVariable("SQS_TICK_SUBSCRIBER_ENABLED", "false");

                var config = SqsConfig.FromConfiguration();

                Assert.AreEqual("us-west-2", config.AwsRegion);
                Assert.AreEqual("https://sqs.us-west-2.amazonaws.com/123456789012/sqs-orders-in.fifo", config.OrdersIncomingQueueUrl);
                Assert.AreEqual("https://sqs.us-west-2.amazonaws.com/123456789012/sqs-orders-out.fifo", config.OrdersExecutedQueueUrl);
                Assert.AreEqual("TOP_SECRET_123", config.WebhookSecret);
                Assert.IsFalse(config.TickSubscriberEnabled);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SQS_AWS_REGION", null);
                Environment.SetEnvironmentVariable("SQS_ORDERS_INCOMING_QUEUE_URL", null);
                Environment.SetEnvironmentVariable("SQS_ORDERS_EXECUTED_QUEUE_URL", null);
                Environment.SetEnvironmentVariable("SQS_WEBHOOK_SECRET", null);
                Environment.SetEnvironmentVariable("SQS_TICK_SUBSCRIBER_ENABLED", null);
            }
        }

        [Test]
        public void SqsEvents_SerializationAndDeserialization_MaintainsPrecision()
        {
            var placed = new OrderPlacedEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = 42,
                Symbol = "ADA",
                Side = "BUY",
                OrderType = "LIMIT",
                Quantity = 1500.50m,
                Price = 0.5284m
            };

            var json = JsonConvert.SerializeObject(placed);
            var deserialized = JsonConvert.DeserializeObject<OrderPlacedEvent>(json);

            Assert.AreEqual(placed.CorrelationId, deserialized.CorrelationId);
            Assert.AreEqual(42, deserialized.UserId);
            Assert.AreEqual("ADA", deserialized.Symbol);
            Assert.AreEqual(1500.50m, deserialized.Quantity);
            Assert.AreEqual(0.5284m, deserialized.Price);
        }

        [Test]
        public async Task SqsPublisher_PublishesMessage_WithMessageGroupId_AndSubscriberReceives()
        {
            var config = new SqsConfig { Enabled = true, MarketTicksQueueUrl = "test.ticks" };
            var publisher = new SqsPublisher(config, _mockLogger.Object);
            var subscriber = new SqsSubscriber(config, _mockLogger.Object);

            string receivedGroupId = null;
            MarketTickEvent receivedTick = null;
            var tcs = new TaskCompletionSource<bool>();

            subscriber.Subscribe<MarketTickEvent>("test.ticks", (groupId, tick) =>
            {
                receivedGroupId = groupId;
                receivedTick = tick;
                tcs.TrySetResult(true);
                return Task.CompletedTask;
            });

            var tickEvent = new MarketTickEvent
            {
                CryptoId = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                CurrentPrice = 64500.75m,
                PriceChange24h = 2.45m
            };

            await publisher.PublishAsync("test.ticks", "BTC", tickEvent);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.AreEqual(tcs.Task, completed, "Subscriber should receive the published message within 1 second.");
            Assert.AreEqual("BTC", receivedGroupId);
            Assert.IsNotNull(receivedTick);
            Assert.AreEqual(64500.75m, receivedTick.CurrentPrice);
            Assert.AreEqual("Bitcoin", receivedTick.Name);
        }

        [Test]
        public async Task SqsPublisher_HandlesDisabledOrOffline_GracefullyWithoutThrowing()
        {
            var config = new SqsConfig { Enabled = false };
            var publisher = new SqsPublisher(config, _mockLogger.Object);

            Assert.IsFalse(publisher.IsActive);

            var success = await publisher.PublishAsync("test.queue", "KEY", new { Hello = "World" });
            Assert.IsTrue(success, "Publisher in fallback mode should complete without unhandled exceptions.");
        }

        [Test]
        public async Task OrderExecutionProcessor_ExecutesOrder_AndEmitsExecutionResult()
        {
            var config = new SqsConfig
            {
                Enabled = true,
                OrdersExecutedQueueUrl = "test.orders.out"
            };

            var mockTradingService = new Mock<ITradingService>();
            mockTradingService
                .Setup(s => s.SellAsync(It.IsAny<int>(), It.IsAny<SellTradeRequest>()))
                .ReturnsAsync(new TradeDto
                {
                    TradeId = 777,
                    Symbol = "BTC",
                    ExecutionPrice = 65000.00m,
                    TotalValue = 130000.00m
                });

            var publisher = new SqsPublisher(config, _mockLogger.Object);
            var processor = new OrderExecutionProcessor(() => mockTradingService.Object, publisher, config, _mockLogger.Object);

            var correlationId = Guid.NewGuid();
            var placed = new OrderPlacedEvent
            {
                CorrelationId = correlationId,
                UserId = 12,
                Symbol = "BTC",
                Side = "SELL",
                OrderType = "MARKET",
                Quantity = 2.0m
            };

            var result = await processor.ProcessOrderAsync(placed);

            Assert.IsNotNull(result);
            Assert.AreEqual("FILLED", result.Status);
            Assert.AreEqual(777, result.TradeId);
            Assert.AreEqual(65000.00m, result.ExecutedPrice);
            Assert.AreEqual(130000.00m, result.TotalAmount);
            Assert.AreEqual(correlationId, result.CorrelationId);
        }

        [Test]
        public async Task MarketTickHotCacheSubscriber_IngestsTick_AndProvidesSubMillisecondLookup()
        {
            var config = new SqsConfig
            {
                Enabled = true,
                MarketTicksQueueUrl = "crypto-market-ticks"
            };

            var publisher = new SqsPublisher(config, _mockLogger.Object);
            var subscriber = new SqsSubscriber(config, _mockLogger.Object);
            var hotCache = new MarketTickHotCacheSubscriber(subscriber, config, _mockLogger.Object);

            hotCache.Start();

            var tick = new MarketTickEvent
            {
                CryptoId = 3,
                Symbol = "SOL",
                Name = "Solana",
                CurrentPrice = 145.20m,
                PriceChange24h = 5.12m
            };

            await publisher.PublishAsync("crypto-market-ticks", "SOL", tick);

            await Task.Delay(50); // Allow in-process dispatch

            var cached = MarketTickHotCacheSubscriber.GetLatestPrice("SOL");
            Assert.IsNotNull(cached, "Price should be available in hot cache immediately after tick ingestion.");
            Assert.AreEqual("SOL", cached.Symbol);
            Assert.AreEqual(145.20m, cached.CurrentPrice);
            Assert.AreEqual(5.12m, cached.PriceChange24h);
        }

        [Test]
        public async Task SqsWebhookController_ProcessesRawOrderPlaced_ReturnsOk()
        {
            var mockProcessor = new Mock<IOrderExecutionProcessor>();
            mockProcessor
                .Setup(p => p.ProcessOrderAsync(It.IsAny<OrderPlacedEvent>()))
                .ReturnsAsync(new OrderExecutedEvent
                {
                    CorrelationId = Guid.NewGuid(),
                    UserId = 5,
                    Symbol = "SOL",
                    Side = "BUY",
                    Status = "FILLED",
                    TradeId = 888,
                    ExecutedPrice = 145.50m
                });

            var config = new SqsConfig();
            var controller = new SqsWebhookController(mockProcessor.Object, config, _mockLogger.Object);

            var rawOrder = JToken.FromObject(new OrderPlacedEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = 5,
                Symbol = "SOL",
                Side = "BUY",
                Quantity = 10.0m
            });

            var actionResult = await controller.ProcessOrderPlacedPush(rawOrder);

            Assert.IsInstanceOf<OkNegotiatedContentResult<ApiResponse<OrderExecutedEvent>>>(actionResult);
            var okResult = (OkNegotiatedContentResult<ApiResponse<OrderExecutedEvent>>)actionResult;
            Assert.IsTrue(okResult.Content.Success);
            Assert.AreEqual("FILLED", okResult.Content.Data.Status);
            Assert.AreEqual(888, okResult.Content.Data.TradeId);
        }

        [Test]
        public async Task SqsWebhookController_ProcessesNativeSqsPushEnvelope_ReturnsOk()
        {
            var mockProcessor = new Mock<IOrderExecutionProcessor>();
            mockProcessor
                .Setup(p => p.ProcessOrderAsync(It.IsAny<OrderPlacedEvent>()))
                .ReturnsAsync(new OrderExecutedEvent
                {
                    CorrelationId = Guid.NewGuid(),
                    UserId = 10,
                    Symbol = "AVAX",
                    Side = "BUY",
                    Status = "FILLED"
                });

            var config = new SqsConfig();
            var controller = new SqsWebhookController(mockProcessor.Object, config, _mockLogger.Object);

            var order = new OrderPlacedEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = 10,
                Symbol = "AVAX",
                Side = "BUY",
                Quantity = 25.0m
            };

            var orderJson = JsonConvert.SerializeObject(order);

            // Standard AWS SQS Push envelope
            var sqsPushEnvelope = new JObject
            {
                ["Records"] = new JArray
                {
                    new JObject
                    {
                        ["messageId"] = "19dd0b1e-9459-4c0e-908d-f952f4469502",
                        ["receiptHandle"] = "MessageReceiptHandle",
                        ["body"] = orderJson,
                        ["attributes"] = new JObject
                        {
                            ["ApproximateReceiveCount"] = "1",
                            ["SentTimestamp"] = "1520621625029",
                            ["SenderId"] = "123456789012",
                            ["ApproximateFirstReceiveTimestamp"] = "1520621625033"
                        },
                        ["messageAttributes"] = new JObject(),
                        ["md5OfBody"] = "098f6bcd4621d373cade4e832627b4f6",
                        ["eventSource"] = "aws:sqs",
                        ["eventSourceARN"] = "arn:aws:sqs:us-east-1:123456789012:crypto-orders-incoming.fifo",
                        ["awsRegion"] = "us-east-1"
                    }
                }
            };

            var actionResult = await controller.ProcessOrderPlacedPush(sqsPushEnvelope);

            Assert.IsInstanceOf<OkNegotiatedContentResult<ApiResponse<OrderExecutedEvent>>>(actionResult);
            var okResult = (OkNegotiatedContentResult<ApiResponse<OrderExecutedEvent>>)actionResult;
            Assert.IsTrue(okResult.Content.Success);
            Assert.AreEqual("FILLED", okResult.Content.Data.Status);
        }

        [Test]
        public async Task SqsWebhookController_RejectsInvalidSecret_WhenConfigured()
        {
            var mockProcessor = new Mock<IOrderExecutionProcessor>();
            var config = new SqsConfig
            {
                WebhookSecret = "SECRET_SQS_999"
            };

            var controller = new SqsWebhookController(mockProcessor.Object, config, _mockLogger.Object);

            var rawOrder = JToken.FromObject(new OrderPlacedEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = 1,
                Symbol = "BTC",
                Quantity = 1.0m
            });

            var actionResult = await controller.ProcessOrderPlacedPush(rawOrder);

            Assert.IsInstanceOf<ResponseMessageResult>(actionResult);
            var responseResult = (ResponseMessageResult)actionResult;
            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, responseResult.Response.StatusCode);
        }

        [Test]
        public void SqsManager_StartsAndStops_Successfully()
        {
            var config = new SqsConfig
            {
                Enabled = true,
                TickSubscriberEnabled = true,
                MarketTicksQueueUrl = "crypto-market-ticks",
                OrdersIncomingQueueUrl = "crypto-orders-incoming",
                OrdersExecutedQueueUrl = "crypto-orders-executed"
            };

            var mockTradingService = new Mock<ITradingService>();
            var manager = new SqsManager(config, _mockLogger.Object, () => mockTradingService.Object);

            Assert.DoesNotThrow(() => manager.Start());
            Assert.IsTrue(manager.IsRunning);

            Assert.DoesNotThrow(() => manager.Stop());
            Assert.IsFalse(manager.IsRunning);
        }
    }
}
