using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/withdrawals")]
    public class WithdrawalsController : BaseApiController
    {
        private readonly IFinancialService _financialService;

        public WithdrawalsController(IFinancialService financialService)
        {
            _financialService = financialService;
        }

        [HttpPost("")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var withdrawal = await _financialService.WithdrawAsync(CurrentUserId, request);
            return EnvelopeCreated($"api/withdrawals", withdrawal, "Withdrawal completed successfully.");
        }

        [HttpGet("")]
        public async Task<IActionResult> GetWithdrawals([FromQuery] int limit = 100)
        {
            var withdrawals = await _financialService.GetWithdrawalsAsync(CurrentUserId, limit);
            return EnvelopeOk(withdrawals);
        }
    }
}
