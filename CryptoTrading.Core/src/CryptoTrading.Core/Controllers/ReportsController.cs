using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Infrastructure.Reports;

namespace CryptoTrading.Core.Controllers;

[Route("api/reports")]
[Authorize]
public class ReportsController : BaseApiController
{
    private readonly IPdfReportService _pdfReportService;

    public ReportsController(IPdfReportService pdfReportService)
    {
        _pdfReportService = pdfReportService;
    }

    [HttpGet("pnl-settlement")]
    public async Task<IActionResult> DownloadPnLReport([FromQuery] string timeframe = "30d")
    {
        int userId = CurrentUserId;
        var pdfBytes = await _pdfReportService.GeneratePnLAndSettlementReportAsync(userId, timeframe);

        string fileName = $"Crypto_PnL_Settlement_Report_{timeframe}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
