using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Core.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Safely extracts the authenticated User ID from the JWT NameIdentifier claim.
    /// </summary>
    protected int CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User identification claim is missing or invalid.");
            }
            return userId;
        }
    }

    /// <summary>
    /// Formats success payloads matching the standard legacy Uniform Response Envelope.
    /// </summary>
    protected IActionResult EnvelopeOk<T>(T data, string message = "Operation completed successfully.")
    {
        return Ok(new
        {
            success = true,
            message,
            data
        });
    }

    /// <summary>
    /// Formats creation payloads matching the standard legacy Uniform Response Envelope (HTTP 201).
    /// </summary>
    protected IActionResult EnvelopeCreated<T>(string uri, T data, string message = "Resource created successfully.")
    {
        return Created(uri, new
        {
            success = true,
            message,
            data
        });
    }
}
