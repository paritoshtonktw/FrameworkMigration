using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/deposits")]
    public class DepositsController : BaseApiController
    {
        private readonly IFinancialService _financialService;

        public DepositsController(IFinancialService financialService)
        {
            _financialService = financialService;
        }

        [HttpPost("")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deposit = await _financialService.DepositAsync(CurrentUserId, request);
            return EnvelopeCreated($"api/deposits", deposit, "Deposit completed successfully.");
        }

        [HttpGet("")]
        public async Task<IActionResult> GetDeposits([FromQuery] int limit = 100)
        {
            var deposits = await _financialService.GetDepositsAsync(CurrentUserId, limit);
            return EnvelopeOk(deposits);
        }
    }
}
