using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using CryptoTrading.Core.Controllers;
using CryptoTrading.Infrastructure.Reports;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class ReportsControllerTests
{
    private Mock<IPdfReportService> _pdfReportServiceMock = null!;
    private ReportsController _controller = null!;

    private const int TestUserId = 42;

    [SetUp]
    public void Setup()
    {
        _pdfReportServiceMock = new Mock<IPdfReportService>();

        _controller = new ReportsController(_pdfReportServiceMock.Object);

        // Mock User Claims Principal to authenticate as TestUserId
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString())
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task DownloadPnLReport_ValidRequest_ReturnsFileResult()
    {
        // Arrange
        var mockPdfBytes = new byte[] { 1, 2, 3, 4, 5 };
        var timeframe = "30d";

        _pdfReportServiceMock.Setup(s => s.GeneratePnLAndSettlementReportAsync(TestUserId, timeframe))
            .ReturnsAsync(mockPdfBytes);

        // Act
        var result = await _controller.DownloadPnLReport(timeframe);

        // Assert
        Assert.That(result, Is.InstanceOf<FileContentResult>());
        var fileResult = (FileContentResult)result;
        
        Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(fileResult.FileDownloadName, Does.StartWith($"Crypto_PnL_Settlement_Report_{timeframe}_"));
        Assert.That(fileResult.FileDownloadName, Does.EndWith(".pdf"));
        Assert.That(fileResult.FileContents, Is.EqualTo(mockPdfBytes));
        
        _pdfReportServiceMock.Verify(s => s.GeneratePnLAndSettlementReportAsync(TestUserId, timeframe), Times.Once);
    }

    [Test]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReportsController(null!));
    }
}
