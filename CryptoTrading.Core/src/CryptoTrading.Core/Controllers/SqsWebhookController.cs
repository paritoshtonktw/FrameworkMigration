using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CryptoTrading.Infrastructure.Sqs;

namespace CryptoTrading.Core.Controllers
{
    /// <summary>
    /// AWS SQS Push Subscription receiver / Webhook endpoint.
    /// Standard endpoint to accept asynchronous orders dispatched from AWS services (e.g. SQS trigger via Lambda/API Gateway).
    /// </summary>
    [ApiController]
    [Route("api/sqs")]
    public class SqsWebhookController : BaseApiController
    {
        private readonly IOrderExecutionProcessor _orderProcessor;
        private readonly SqsConfig _config;
        private readonly ILogger<SqsWebhookController> _logger;

        public SqsWebhookController(
            IOrderExecutionProcessor orderProcessor,
            IConfiguration configuration,
            ILogger<SqsWebhookController> logger)
        {
            _orderProcessor = orderProcessor;
            _config = SqsConfig.FromConfiguration(configuration);
            _logger = logger;
        }

        /// <summary>
        /// Native AWS SQS Push webhook for incoming trade orders.
        /// Automatically extracts the JSON body from SQS records,
        /// executes the trade atomically, and emits the completion event.
        /// </summary>
        [HttpPost("order-placed")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessOrderPlacedPush([FromBody] JToken payload)
        {
            if (!ValidatePushSecurity())
            {
                return Unauthorized(new { success = false, message = "Unauthorized SQS push request: invalid secret token.", errorCode = "UNAUTHORIZED_SQS" });
            }

            if (payload == null)
            {
                return BadRequest(new { success = false, message = "SQS message payload cannot be empty.", errorCode = "INVALID_PAYLOAD" });
            }

            try
            {
                var order = ExtractOrderPlaced(payload);
                if (order == null || order.UserId <= 0 || string.IsNullOrWhiteSpace(order.Symbol))
                {
                    return BadRequest(new { success = false, message = "Invalid OrderPlacedEvent payload structure.", errorCode = "MALFORMED_EVENT" });
                }

                _logger.LogInformation("[Sqs:Webhook] Received OrderPlacedEvent: CorrelationId={CorrelationId}, Symbol={Symbol}, Side={Side}, Quantity={Quantity}", order.CorrelationId, order.Symbol, order.Side, order.Quantity);

                var result = await _orderProcessor.ProcessOrderAsync(order);

                return EnvelopeOk(result, $"SQS Order {order.CorrelationId} acknowledged with status: {result.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sqs:Webhook] Order execution failed: {Message}", ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, new { success = false, message = $"Order execution failed: {ex.Message}", errorCode = "ORDER_EXECUTION_FAILED" });
            }
        }

        /// <summary>
        /// AWS SQS Push webhook for executed order notifications and auditing.
        /// </summary>
        [HttpPost("order-executed")]
        [AllowAnonymous]
        public IActionResult ProcessOrderExecutedPush([FromBody] JToken payload)
        {
            if (!ValidatePushSecurity())
            {
                return Unauthorized(new { success = false, message = "Unauthorized SQS push request: invalid secret token.", errorCode = "UNAUTHORIZED_SQS" });
            }

            if (payload == null)
            {
                return BadRequest(new { success = false, message = "SQS message payload cannot be empty.", errorCode = "INVALID_PAYLOAD" });
            }

            try
            {
                var executed = ExtractOrderExecuted(payload);
                if (executed == null)
                {
                    return BadRequest(new { success = false, message = "Invalid OrderExecutedEvent payload.", errorCode = "MALFORMED_EVENT" });
                }

                _logger.LogInformation("[Sqs:Audit] Trade Notification: Order {CorrelationId} ({Symbol} {Side}) -> Status={Status}, TradeId={TradeId}, Total=${TotalAmount}", executed.CorrelationId, executed.Symbol, executed.Side, executed.Status, executed.TradeId, executed.TotalAmount);

                return EnvelopeOk(new
                {
                    Acknowledged = true,
                    CorrelationId = executed.CorrelationId,
                    Status = executed.Status,
                    TradeId = executed.TradeId,
                    Timestamp = DateTime.UtcNow
                }, "Order execution audit recorded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sqs:Webhook] Audit notification failed: {Message}", ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, new { success = false, message = $"Audit notification failed: {ex.Message}", errorCode = "AUDIT_FAILED" });
            }
        }

        private bool ValidatePushSecurity()
        {
            var expectedSecret = _config?.WebhookSecret;
            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                return true; // No secret configured; allow open local dev
            }

            // Check custom header
            if (Request.Headers.TryGetValue("X-Sqs-Secret", out var headerValues) &&
                headerValues.Any(v => string.Equals(v, expectedSecret, StringComparison.Ordinal)))
            {
                return true;
            }

            // Check query string ?secret=...
            if (Request.Query.TryGetValue("secret", out var querySecret) &&
                string.Equals(querySecret.ToString(), expectedSecret, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static OrderPlacedEvent? ExtractOrderPlaced(JToken token)
        {
            if (token is JObject obj)
            {
                // Standard AWS SQS Records envelope
                var records = obj["Records"] as JArray;
                if (records != null && records.Count > 0)
                {
                    var firstRecord = records[0];
                    var bodyStr = firstRecord["body"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bodyStr))
                    {
                        return JsonConvert.DeserializeObject<OrderPlacedEvent>(bodyStr);
                    }
                }
            }

            return token.ToObject<OrderPlacedEvent>();
        }

        private static OrderExecutedEvent? ExtractOrderExecuted(JToken token)
        {
            if (token is JObject obj)
            {
                // Standard AWS SQS Records envelope
                var records = obj["Records"] as JArray;
                if (records != null && records.Count > 0)
                {
                    var firstRecord = records[0];
                    var bodyStr = firstRecord["body"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bodyStr))
                    {
                        return JsonConvert.DeserializeObject<OrderExecutedEvent>(bodyStr);
                    }
                }
            }

            return token.ToObject<OrderExecutedEvent>();
        }
    }
}
