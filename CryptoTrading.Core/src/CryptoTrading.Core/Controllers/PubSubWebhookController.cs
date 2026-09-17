using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.PubSub;

namespace CryptoTrading.Core.Controllers;

[Route("api/pubsub")]
[AllowAnonymous]
public class PubSubWebhookController : ControllerBase
{
    private readonly IOrderExecutionProcessor _orderProcessor;
    private readonly ILoggerService _logger;
    private readonly string? _pushSecret;

    public PubSubWebhookController(
        IOrderExecutionProcessor orderProcessor,
        ILoggerService logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _orderProcessor = orderProcessor;
        _logger = logger;
        _pushSecret = configuration["PubSub:PushEndpointSecret"];
    }

    [HttpPost("order-placed")]
    public async Task<IActionResult> ProcessOrderPlacedPush([FromBody] JsonElement payload, [FromQuery] string? token = null)
    {
        if (!ValidatePushSecurity(token))
        {
            return Unauthorized(new { success = false, message = "Unauthorized Pub/Sub push request: invalid secret token.", errorCode = "UNAUTHORIZED_PUBSUB" });
        }

        try
        {
            if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            {
                return BadRequest(new { success = false, message = "Pub/Sub message payload cannot be empty.", errorCode = "INVALID_PAYLOAD" });
            }

            var order = ExtractOrderPlaced(payload);
            if (order == null || order.UserId <= 0 || string.IsNullOrWhiteSpace(order.Symbol))
            {
                return BadRequest(new { success = false, message = "Invalid OrderPlacedEvent payload structure.", errorCode = "MALFORMED_EVENT" });
            }

            _logger.Info($"[PubSub:Webhook] Received OrderPlacedEvent: CorrelationId={order.CorrelationId}, Symbol={order.Symbol}, Side={order.Side}, Quantity={order.Quantity}");

            var result = await _orderProcessor.ProcessOrderAsync(order);
            return Ok(new { success = true, message = $"Pub/Sub Order {order.CorrelationId} acknowledged with status: {result.Status}", data = result });
        }
        catch (Exception ex)
        {
            _logger.Error($"[PubSub:Webhook] Order execution failed: {ex.Message}", ex);
            return StatusCode(500, new { success = false, message = $"Order execution failed: {ex.Message}", errorCode = "ORDER_EXECUTION_FAILED" });
        }
    }

    private bool ValidatePushSecurity(string? token)
    {
        if (string.IsNullOrEmpty(_pushSecret))
        {
            return true; // if no secret configured, skip security check
        }
        return string.Equals(_pushSecret, token, StringComparison.Ordinal);
    }

    private static OrderPlacedEvent? ExtractOrderPlaced(JsonElement payload)
    {
        try
        {
            if (payload.TryGetProperty("message", out var messageElement) && messageElement.TryGetProperty("data", out var dataElement))
            {
                var base64Data = dataElement.GetString();
                if (string.IsNullOrEmpty(base64Data)) return null;

                var jsonBytes = Convert.FromBase64String(base64Data);
                var jsonString = Encoding.UTF8.GetString(jsonBytes);

                return JsonSerializer.Deserialize<OrderPlacedEvent>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch
        {
            // Parsing failure
        }
        return null;
    }
}
