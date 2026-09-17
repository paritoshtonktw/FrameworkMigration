using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/auth")]
[Authorize]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(request);
        return EnvelopeCreated($"/api/profile/{result.User.UserId}", result, "User registered successfully.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request);
        return EnvelopeOk(result, "Login successful.");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Stateless JWT logout
        return EnvelopeOk(true, "Logged out successfully.");
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        int userId = CurrentUserId;
        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
            return NotFound(new { success = false, message = "User not found.", errorCode = "NOT_FOUND" });

        return EnvelopeOk(user);
    }
}
