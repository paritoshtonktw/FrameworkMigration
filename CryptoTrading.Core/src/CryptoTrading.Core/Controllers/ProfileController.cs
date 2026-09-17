using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/profile")]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        int userId = CurrentUserId;
        var profile = await _userService.GetUserProfileAsync(userId);
        return EnvelopeOk(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        int userId = CurrentUserId;
        var updated = await _userService.UpdateUserProfileAsync(userId, request);
        return EnvelopeOk(updated, "Profile updated successfully.");
    }
}
