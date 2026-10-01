using System;
using Microsoft.Extensions.Configuration;

namespace CryptoTrading.Infrastructure.Sqs
{
    public class SqsConfig
    {
        public bool Enabled { get; set; }
        public string AwsRegion { get; set; } = "us-east-1";
        public string ServiceUrl { get; set; } = "http://localhost:4566";
        public string OrdersIncomingQueueUrl { get; set; } = "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-incoming.fifo";
        public string OrdersExecutedQueueUrl { get; set; } = "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-executed.fifo";
        public string MarketTicksQueueUrl { get; set; } = "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-market-ticks.fifo";
        public string? WebhookSecret { get; set; }
        public bool TickSubscriberEnabled { get; set; } = true;

        public static SqsConfig FromConfiguration(IConfiguration? configuration = null)
        {
            var config = new SqsConfig();

            var enabledEnv = Environment.GetEnvironmentVariable("SQS_ENABLED");
            var enabledApp = configuration?["Sqs:Enabled"];
            config.Enabled = !string.IsNullOrEmpty(enabledEnv)
                ? (bool.TryParse(enabledEnv, out var b) ? b : true)
                : (!string.IsNullOrEmpty(enabledApp) && (bool.TryParse(enabledApp, out var b2) ? b2 : true));

            config.AwsRegion = Environment.GetEnvironmentVariable("SQS_AWS_REGION")
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? configuration?["Sqs:AwsRegion"]
                ?? "us-east-1";

            config.ServiceUrl = Environment.GetEnvironmentVariable("SQS_SERVICE_URL")
                ?? configuration?["Sqs:ServiceUrl"]
                ?? "http://localhost:4566";

            config.OrdersIncomingQueueUrl = Environment.GetEnvironmentVariable("SQS_ORDERS_INCOMING_QUEUE_URL")
                ?? configuration?["Sqs:OrdersIncomingQueueUrl"]
                ?? "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-incoming.fifo";

            config.OrdersExecutedQueueUrl = Environment.GetEnvironmentVariable("SQS_ORDERS_EXECUTED_QUEUE_URL")
                ?? configuration?["Sqs:OrdersExecutedQueueUrl"]
                ?? "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-executed.fifo";

            config.MarketTicksQueueUrl = Environment.GetEnvironmentVariable("SQS_MARKET_TICKS_QUEUE_URL")
                ?? configuration?["Sqs:MarketTicksQueueUrl"]
                ?? "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-market-ticks.fifo";

            config.WebhookSecret = Environment.GetEnvironmentVariable("SQS_WEBHOOK_SECRET")
                ?? configuration?["Sqs:WebhookSecret"];

            var tickSubEnv = Environment.GetEnvironmentVariable("SQS_TICK_SUBSCRIBER_ENABLED");
            var tickSubApp = configuration?["Sqs:TickSubscriberEnabled"];
            config.TickSubscriberEnabled = !string.IsNullOrEmpty(tickSubEnv)
                ? (bool.TryParse(tickSubEnv, out var ts) ? ts : true)
                : (!string.IsNullOrEmpty(tickSubApp) ? (bool.TryParse(tickSubApp, out var ts2) ? ts2 : true) : true);

            return config;
        }
    }
}
