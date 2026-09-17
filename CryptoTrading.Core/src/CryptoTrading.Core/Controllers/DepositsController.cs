using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/deposits")]
[Authorize]
public class DepositsController : BaseApiController
{
    private readonly IFinancialService _financialService;

    public DepositsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    [HttpPost]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        int userId = CurrentUserId;
        var deposit = await _financialService.DepositAsync(userId, request);
        return EnvelopeCreated($"/api/deposits/{deposit.DepositId}", deposit, "Deposit completed successfully.");
    }

    [HttpGet]
    public async Task<IActionResult> GetDeposits([FromQuery] int limit = 100)
    {
        int userId = CurrentUserId;
        var deposits = await _financialService.GetDepositsAsync(userId, limit);
        return EnvelopeOk(deposits);
    }
}
