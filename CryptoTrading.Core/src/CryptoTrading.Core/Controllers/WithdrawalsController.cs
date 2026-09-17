using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/withdrawals")]
[Authorize]
public class WithdrawalsController : BaseApiController
{
    private readonly IFinancialService _financialService;

    public WithdrawalsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        int userId = CurrentUserId;
        var withdrawal = await _financialService.WithdrawAsync(userId, request);
        return EnvelopeCreated($"/api/withdrawals/{withdrawal.WithdrawalId}", withdrawal, "Withdrawal completed successfully.");
    }

    [HttpGet]
    public async Task<IActionResult> GetWithdrawals([FromQuery] int limit = 100)
    {
        int userId = CurrentUserId;
        var withdrawals = await _financialService.GetWithdrawalsAsync(userId, limit);
        return EnvelopeOk(withdrawals);
    }
}
