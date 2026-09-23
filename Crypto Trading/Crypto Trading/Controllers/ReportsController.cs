using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using CryptoTrading.Infrastructure.Reports;
using CryptoTrading.Web.Security;

namespace CryptoTrading.Web.Controllers
{
    [JwtAuthorize]
    [RoutePrefix("api/reports")]
    public class ReportsController : BaseApiController
    {
        private readonly IPdfReportService _pdfReportService;

        public ReportsController() : this(DependencyConfig.PdfReportService)
        {
        }

        public ReportsController(IPdfReportService pdfReportService)
        {
            _pdfReportService = pdfReportService;
        }

        [HttpGet]
        [Route("pnl-settlement")]
        public async Task<HttpResponseMessage> DownloadPnLReport([FromUri] string timeframe = "30d")
        {
            var pdfBytes = await _pdfReportService.GeneratePnLAndSettlementReportAsync(CurrentUserId, timeframe);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes)
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Crypto_PnL_Settlement_Report_{timeframe}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf"
            };

            return response;
        }
    }
}
