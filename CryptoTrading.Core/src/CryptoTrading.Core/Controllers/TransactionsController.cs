using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;

namespace CryptoTrading.Core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : BaseApiController
    {
        private readonly IFinancialService _financialService;

        public TransactionsController(IFinancialService financialService)
        {
            _financialService = financialService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetTransactions([FromQuery] int limit = 100)
        {
            var txs = await _financialService.GetTransactionsAsync(CurrentUserId, limit);
            return EnvelopeOk(txs);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTransactionById(int id)
        {
            var tx = await _financialService.GetTransactionByIdAsync(id, CurrentUserId);
            return EnvelopeOk(tx);
        }
    }
}
