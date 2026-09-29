using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.Sqs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoTrading.Web.Controllers
{
    /// <summary>
    /// AWS SQS Push Subscription receiver / Webhook endpoint.
    /// Standard endpoint to accept asynchronous orders dispatched from AWS services (e.g. SQS trigger via Lambda/API Gateway).
    /// </summary>
    [RoutePrefix("api/sqs")]
    public class SqsWebhookController : BaseApiController
    {
        private readonly IOrderExecutionProcessor _orderProcessor;
        private readonly SqsConfig _config;
        private readonly ILoggerService _logger;

        public SqsWebhookController()
            : this(DependencyConfig.OrderExecutionProcessor, DependencyConfig.SqsConfig, DependencyConfig.Logger)
        {
        }

        public SqsWebhookController(
            IOrderExecutionProcessor orderProcessor,
            SqsConfig config,
            ILoggerService logger)
        {
            _orderProcessor = orderProcessor;
            _config = config;
            _logger = logger;
            Request = new HttpRequestMessage();
            Request.SetConfiguration(new HttpConfiguration());
        }

        /// <summary>
        /// Native AWS SQS Push webhook for incoming trade orders.
        /// Automatically extracts the JSON body from SQS records,
        /// executes the trade atomically, and emits the completion event.
        /// </summary>
        [HttpPost]
        [Route("order-placed")]
        public async Task<IHttpActionResult> ProcessOrderPlacedPush([FromBody] JToken payload)
        {
            if (!ValidatePushSecurity())
            {
                return ErrorResponse("Unauthorized SQS push request: invalid secret token.", "UNAUTHORIZED_SQS", HttpStatusCode.Unauthorized);
            }

            if (payload == null)
            {
                return ErrorResponse("SQS message payload cannot be empty.", "INVALID_PAYLOAD", HttpStatusCode.BadRequest);
            }

            try
            {
                var order = ExtractOrderPlaced(payload);
                if (order == null || order.UserId <= 0 || string.IsNullOrWhiteSpace(order.Symbol))
                {
                    return ErrorResponse("Invalid OrderPlacedEvent payload structure.", "MALFORMED_EVENT", HttpStatusCode.BadRequest);
                }

                _logger?.Info($"[Sqs:Webhook] Received OrderPlacedEvent: CorrelationId={order.CorrelationId}, Symbol={order.Symbol}, Side={order.Side}, Quantity={order.Quantity}");

                var result = await _orderProcessor.ProcessOrderAsync(order);

                return OkResponse(result, $"SQS Order {order.CorrelationId} acknowledged with status: {result.Status}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"[Sqs:Webhook] Order execution failed: {ex.Message}", ex);
                return ErrorResponse($"Order execution failed: {ex.Message}", "ORDER_EXECUTION_FAILED", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// AWS SQS Push webhook for executed order notifications and auditing.
        /// </summary>
        [HttpPost]
        [Route("order-executed")]
        public IHttpActionResult ProcessOrderExecutedPush([FromBody] JToken payload)
        {
            if (!ValidatePushSecurity())
            {
                return ErrorResponse("Unauthorized SQS push request: invalid secret token.", "UNAUTHORIZED_SQS", HttpStatusCode.Unauthorized);
            }

            if (payload == null)
            {
                return ErrorResponse("SQS message payload cannot be empty.", "INVALID_PAYLOAD", HttpStatusCode.BadRequest);
            }

            try
            {
                var executed = ExtractOrderExecuted(payload);
                if (executed == null)
                {
                    return ErrorResponse("Invalid OrderExecutedEvent payload.", "MALFORMED_EVENT", HttpStatusCode.BadRequest);
                }

                _logger?.Info($"[Sqs:Audit] Trade Notification: Order {executed.CorrelationId} ({executed.Symbol} {executed.Side}) -> Status={executed.Status}, TradeId={executed.TradeId}, Total=${executed.TotalAmount}");

                return OkResponse(new
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
                _logger?.Error($"[Sqs:Webhook] Audit notification failed: {ex.Message}", ex);
                return ErrorResponse($"Audit notification failed: {ex.Message}", "AUDIT_FAILED", HttpStatusCode.InternalServerError);
            }
        }

        private bool ValidatePushSecurity()
        {
            var expectedSecret = _config?.WebhookSecret;
            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                return true; // No secret configured; allow open local dev
            }

            if (Request == null || Request.Headers == null)
            {
                return false;
            }

            // Check custom header
            if (Request.Headers.TryGetValues("X-Sqs-Secret", out var headerValues) &&
                headerValues.Any(v => string.Equals(v, expectedSecret, StringComparison.Ordinal)))
            {
                return true;
            }

            // Check query string ?secret=...
            var query = Request.GetQueryNameValuePairs();
            if (query != null && query.Any(kvp => string.Equals(kvp.Key, "secret", StringComparison.OrdinalIgnoreCase) && string.Equals(kvp.Value, expectedSecret, StringComparison.Ordinal)))
            {
                return true;
            }

            return false;
        }

        private static OrderPlacedEvent ExtractOrderPlaced(JToken token)
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

        private static OrderExecutedEvent ExtractOrderExecuted(JToken token)
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
