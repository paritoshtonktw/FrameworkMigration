using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;

namespace CryptoTrading.Core.Controllers;

[Route("api/transactions")]
[Authorize]
public class TransactionsController : BaseApiController
{
    private readonly IFinancialService _financialService;

    public TransactionsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] int limit = 100)
    {
        int userId = CurrentUserId;
        var txs = await _financialService.GetTransactionsAsync(userId, limit);
        return EnvelopeOk(txs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransactionById(int id)
    {
        int userId = CurrentUserId;
        var tx = await _financialService.GetTransactionByIdAsync(id, userId);
        return EnvelopeOk(tx);
    }
}
