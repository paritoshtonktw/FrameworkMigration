using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Infrastructure.Reports;

public interface IPdfReportService
{
    Task<byte[]> GeneratePnLAndSettlementReportAsync(int userId, string timeframe = "30d");
}

public class PdfReportService : IPdfReportService
{
    private readonly IUserRepository _userRepo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly ITradingRepository _tradingRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly ILoggerService _logger;

    public PdfReportService(
        IUserRepository userRepo,
        IPortfolioRepository portfolioRepo,
        ITradingRepository tradingRepo,
        ITransactionRepository txRepo,
        ILoggerService logger)
    {
        _userRepo = userRepo;
        _portfolioRepo = portfolioRepo;
        _tradingRepo = tradingRepo;
        _txRepo = txRepo;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePnLAndSettlementReportAsync(int userId, string timeframe = "30d")
    {
        var normalizedTf = (timeframe ?? "30d").Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        DateTime startDate;
        string tfLabel;
        switch (normalizedTf)
        {
            case "7d":
                startDate = now.AddDays(-7);
                tfLabel = "Last 7 Days";
                break;
            case "90d":
                startDate = now.AddDays(-90);
                tfLabel = "Last 90 Days";
                break;
            case "ytd":
                startDate = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                tfLabel = $"Year to Date ({now.Year})";
                break;
            case "all":
                startDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                tfLabel = "All Historical Data";
                break;
            case "30d":
            default:
                startDate = now.AddDays(-30);
                tfLabel = "Last 30 Days";
                break;
        }

        _logger.Info($"Generating PDF PnL & Settlement statement for UserId {userId}, Timeframe: {tfLabel}");

        // 1. Gather all required report data
        var user = await _userRepo.GetUserByIdAsync(userId);
        var username = user?.Username ?? $"User #{userId}";
        var email = user?.Email ?? "N/A";

        var portfolio = await _portfolioRepo.GetPortfolioAsync(userId);
        var holdings = portfolio.Holdings ?? new List<HoldingDto>();

        var allTrades = await _tradingRepo.GetTradesByUserAsync(userId, 1000) ?? new List<TradeDto>();
        var periodTrades = allTrades
            .Where(t => t.ExecutedDate >= startDate && t.ExecutedDate <= now)
            .OrderByDescending(t => t.ExecutedDate)
            .ToList();

        var allTransactions = await _txRepo.GetTransactionsByUserAsync(userId, 1000) ?? new List<TransactionDto>();
        var periodTransactions = allTransactions
            .Where(tx => tx.CreatedDate >= startDate && tx.CreatedDate <= now)
            .OrderByDescending(tx => tx.CreatedDate)
            .ToList();

        // 2. Compute summary metrics
        decimal totalPortfolioVal = portfolio.Summary?.TotalPortfolioValue ?? 0m;
        decimal cashBalance = portfolio.Summary?.CashBalance ?? 0m;
        decimal cryptoHoldingsVal = portfolio.Summary?.HoldingsMarketValue ?? 0m;
        decimal totalUnrealizedPnL = portfolio.Summary?.UnrealizedProfitLoss ?? 0m;

        decimal periodRealizedPnL = periodTrades.Sum(t => t.RealizedProfitLoss);
        decimal periodTradingVolume = periodTrades.Sum(t => t.TotalValue);

        decimal periodDeposits = periodTransactions
            .Where(t => (t.TransactionType ?? "").Contains("DEPOSIT", StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Amount);

        decimal periodWithdrawals = periodTransactions
            .Where(t => (t.TransactionType ?? "").Contains("WITHDRAW", StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Amount);

        decimal netSettlementCashFlow = periodDeposits - periodWithdrawals;

        // 3. Build the PDF Document
        var builder = new SimplePdfBuilder();

        // First Page
        builder.NewPage();

        // Top Navy Brand Banner
        builder.DrawFilledRect(0, 785, 595.28f, 56.89f, 0.06f, 0.09f, 0.16f);
        builder.DrawText(40, 815, "CRYPTO TRADING PLATFORM", 16, true, 1.0f, 1.0f, 1.0f);
        builder.DrawText(40, 798, "OFFICIAL PnL & SETTLEMENT STATEMENT", 8, false, 0.58f, 0.77f, 0.99f);
        builder.DrawText(430, 805, "CONFIDENTIAL", 9, true, 0.94f, 0.27f, 0.24f);

        // Document Title & Sub-header
        float cursorY = 750;
        builder.DrawText(40, cursorY, "PORTFOLIO SETTLEMENT & REALIZED P/L REPORT", 13, true, 0.12f, 0.16f, 0.23f);
        cursorY -= 16;
        builder.DrawText(40, cursorY, $"Period: {tfLabel} ({startDate:yyyy-MM-dd} to {now:yyyy-MM-dd})", 9, false, 0.35f, 0.40f, 0.48f);
        builder.DrawText(360, cursorY, $"Generated: {now:yyyy-MM-dd HH:mm:ss} UTC", 8, false, 0.45f, 0.50f, 0.58f);

        cursorY -= 16;
        builder.DrawText(40, cursorY, $"Account Holder: {username} | Email: {email} | User Ref: #{userId}", 9, true, 0.20f, 0.24f, 0.32f);

        cursorY -= 12;
        builder.DrawLine(40, cursorY, 555, cursorY, 0.85f, 0.88f, 0.92f, 1.0f);

        // Executive Summary Metric Cards (4 Cards)
        cursorY -= 62;
        builder.DrawMetricCard(40, cursorY, 115, 50, "Total Portfolio Value", $"${totalPortfolioVal:N2}", $"Cash: ${cashBalance:N2}");
        builder.DrawMetricCard(165, cursorY, 115, 50, "Crypto Asset Value", $"${cryptoHoldingsVal:N2}", $"Assets: {holdings.Count}");
        builder.DrawMetricCard(290, cursorY, 115, 50, "Period Realized P/L", $"{(periodRealizedPnL >= 0 ? "+" : "")}${periodRealizedPnL:N2}", $"Volume: ${periodTradingVolume:N2}", periodRealizedPnL >= 0);
        builder.DrawMetricCard(415, cursorY, 115, 50, "Net Cash Flow", $"{(netSettlementCashFlow >= 0 ? "+" : "")}${netSettlementCashFlow:N2}", $"Deposited: ${periodDeposits:N2}", netSettlementCashFlow >= 0);

        cursorY -= 20;

        // Section: Active Asset Holdings
        cursorY = builder.EnsureSpace(cursorY, 150);
        builder.DrawSectionHeader(40, cursorY, "ACTIVE CRYPTOCURRENCY HOLDINGS");
        cursorY -= 22;

        float[] holdCols = { 40, 100, 200, 280, 360, 460, 555 };
        string[] holdHeaders = { "Symbol", "Asset Name", "Quantity Held", "Avg Cost", "Market Value", "Unrealized P/L" };
        builder.DrawTableHeader(holdCols, holdHeaders, cursorY);
        cursorY -= 14;

        if (holdings.Count == 0)
        {
            builder.DrawText(48, cursorY, "No active digital asset holdings found in wallet.", 8.5f, false, 0.40f, 0.40f, 0.40f);
            cursorY -= 14;
        }
        else
        {
            bool zebra = false;
            foreach (var h in holdings)
            {
                cursorY = builder.EnsureSpace(cursorY, 14);
                builder.DrawTableRowBackground(40, 555, cursorY, 14, zebra);

                float pnlR = h.UnrealizedProfitLoss >= 0 ? 0.10f : 0.85f;
                float pnlG = h.UnrealizedProfitLoss >= 0 ? 0.60f : 0.15f;
                float pnlB = h.UnrealizedProfitLoss >= 0 ? 0.20f : 0.15f;
                string pnlSign = h.UnrealizedProfitLoss >= 0 ? "+" : "";

                builder.DrawText(holdCols[0] + 4, cursorY + 3, h.Symbol, 8.5f, true, 0.10f, 0.15f, 0.20f);
                builder.DrawText(holdCols[1] + 4, cursorY + 3, h.Name ?? "N/A", 8, false, 0.30f, 0.30f, 0.30f);
                builder.DrawText(holdCols[2] + 4, cursorY + 3, $"{h.Quantity:F6}", 8, false, 0.20f, 0.20f, 0.20f);
                builder.DrawText(holdCols[3] + 4, cursorY + 3, $"${h.AverageCost:N2}", 8, false, 0.30f, 0.30f, 0.30f);
                builder.DrawText(holdCols[4] + 4, cursorY + 3, $"${h.CurrentValue:N2}", 8.5f, true, 0.15f, 0.20f, 0.25f);
                builder.DrawText(holdCols[5] + 4, cursorY + 3, $"{pnlSign}${h.UnrealizedProfitLoss:N2} ({h.UnrealizedProfitLossPercentage:N2}%)", 8, true, pnlR, pnlG, pnlB);

                cursorY -= 14;
                zebra = !zebra;
            }
        }

        // Section: Period Trade Execution log
        cursorY -= 15;
        cursorY = builder.EnsureSpace(cursorY, 150);
        builder.DrawSectionHeader(40, cursorY, "PERIOD TRADE EXECUTION LOG");
        cursorY -= 22;

        float[] tradeCols = { 40, 130, 180, 260, 340, 440, 555 };
        string[] tradeHeaders = { "Date (UTC)", "Type/Side", "Symbol", "Trade Qty", "Execution Price", "Total Settled" };
        builder.DrawTableHeader(tradeCols, tradeHeaders, cursorY);
        cursorY -= 14;

        if (periodTrades.Count == 0)
        {
            builder.DrawText(48, cursorY, "No trades executed during the selected reporting period.", 8.5f, false, 0.40f, 0.40f, 0.40f);
            cursorY -= 14;
        }
        else
        {
            bool zebra = false;
            foreach (var t in periodTrades)
            {
                cursorY = builder.EnsureSpace(cursorY, 14);
                builder.DrawTableRowBackground(40, 555, cursorY, 14, zebra);

                bool isBuy = string.Equals(t.Side, "BUY", StringComparison.OrdinalIgnoreCase);
                float sideR = isBuy ? 0.10f : 0.85f;
                float sideG = isBuy ? 0.60f : 0.15f;
                float sideB = isBuy ? 0.20f : 0.15f;

                builder.DrawText(tradeCols[0] + 4, cursorY + 3, $"{t.ExecutedDate:yyyy-MM-dd HH:mm}", 7.5f, false, 0.30f, 0.30f, 0.30f);
                builder.DrawText(tradeCols[1] + 4, cursorY + 3, t.Side ?? "BUY", 8.5f, true, sideR, sideG, sideB);
                builder.DrawText(tradeCols[2] + 4, cursorY + 3, t.Symbol, 8.5f, true, 0.10f, 0.15f, 0.20f);
                builder.DrawText(tradeCols[3] + 4, cursorY + 3, $"{t.Quantity:F6}", 8, false, 0.20f, 0.20f, 0.20f);
                builder.DrawText(tradeCols[4] + 4, cursorY + 3, $"${t.ExecutionPrice:N2}", 8, false, 0.30f, 0.30f, 0.30f);
                builder.DrawText(tradeCols[5] + 4, cursorY + 3, $"${t.TotalValue:N2}", 8.5f, true, 0.15f, 0.20f, 0.25f);

                cursorY -= 14;
                zebra = !zebra;
            }
        }

        // Section: Cash Movements
        cursorY -= 15;
        cursorY = builder.EnsureSpace(cursorY, 150);
        builder.DrawSectionHeader(40, cursorY, "CASH FLOW SETTLEMENTS (DEPOSITS & WITHDRAWALS)");
        cursorY -= 22;

        float[] txCols = { 40, 130, 240, 300, 420, 555 };
        string[] txHeaders = { "Date (UTC)", "Transaction Type", "Currency", "Settled Amount", "Status" };
        builder.DrawTableHeader(txCols, txHeaders, cursorY);
        cursorY -= 14;

        if (periodTransactions.Count == 0)
        {
            builder.DrawText(48, cursorY, "No deposits or withdrawals processed during the reporting period.", 8.5f, false, 0.40f, 0.40f, 0.40f);
            cursorY -= 14;
        }
        else
        {
            bool zebra = false;
            int rowIndex = 0;
            foreach (var tx in periodTransactions)
            {
                // Max out table after 12 records on page 1, or paginate safely
                if (rowIndex >= 12 && cursorY > 150)
                {
                    cursorY = builder.EnsureSpace(cursorY, 200); // trigger page break
                    rowIndex = 0;
                }

                cursorY = builder.EnsureSpace(cursorY, 14);
                builder.DrawTableRowBackground(40, 555, cursorY, 14, zebra);

                bool isDeposit = (tx.TransactionType ?? "").Contains("DEPOSIT", StringComparison.OrdinalIgnoreCase);
                string amtSign = isDeposit ? "+" : "-";
                float amtR = isDeposit ? 0.10f : 0.85f;
                float amtG = isDeposit ? 0.60f : 0.15f;
                float amtB = isDeposit ? 0.20f : 0.15f;

                builder.DrawText(txCols[0] + 4, cursorY + 3, $"{tx.CreatedDate:yyyy-MM-dd HH:mm}", 7.5f, false, 0.30f, 0.30f, 0.30f);
                builder.DrawText(txCols[1] + 4, cursorY + 3, tx.TransactionType ?? "", 8, true, 0.15f, 0.20f, 0.25f);
                builder.DrawText(txCols[2] + 4, cursorY + 3, tx.Currency ?? "USD", 8, false, 0.30f, 0.30f, 0.30f);
                builder.DrawText(txCols[3] + 4, cursorY + 3, $"{amtSign}${tx.Amount:N2}", 8, true, amtR, amtG, amtB);
                builder.DrawText(txCols[4] + 4, cursorY + 3, tx.Status ?? "COMPLETED", 8, false, 0.20f, 0.50f, 0.30f);

                cursorY -= 14;
                zebra = !zebra;
                rowIndex++;
            }
        }

        // Summary notes & disclaimer
        cursorY = builder.EnsureSpace(cursorY, 40);
        cursorY -= 10;
        builder.DrawLine(40, cursorY, 555, cursorY, 0.85f, 0.88f, 0.92f, 1.0f);
        cursorY -= 12;
        builder.DrawText(40, cursorY, "Notice: This statement is computer-generated for informational and tax reporting purposes. Crypto assets are volatile. Keep for records.", 7.5f, false, 0.50f, 0.55f, 0.60f);

        return builder.ToByteArray();
    }
}

public class SimplePdfBuilder
{
    private class PageStream
    {
        public StringBuilder Content { get; } = new StringBuilder();
    }

    private readonly List<PageStream> _pages = new();
    private PageStream? _currentPage;
    private const float PageWidth = 595.28f;
    private const float PageHeight = 841.89f;

    public void NewPage()
    {
        _currentPage = new PageStream();
        _pages.Add(_currentPage);
    }

    public float EnsureSpace(float currentY, float requiredHeight)
    {
        if (currentY - requiredHeight < 45)
        {
            NewPage();
            // Subsequent page subtle top banner
            DrawFilledRect(0, 810, PageWidth, 31.89f, 0.08f, 0.12f, 0.20f);
            DrawText(40, 820, "CRYPTO TRADING PLATFORM - STATEMENT (CONTINUED)", 10, true, 1.0f, 1.0f, 1.0f);
            return 790;
        }
        return currentY;
    }

    public void DrawText(float x, float y, string text, float fontSize = 9, bool isBold = false, float r = 0, float g = 0, float b = 0)
    {
        if (string.IsNullOrEmpty(text) || _currentPage == null) return;
        string fontName = isBold ? "/F2" : "/F1";
        string safeText = EscapePdfString(text);

        _currentPage.Content.AppendLine("BT");
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} rg", r, g, b));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1:F1} Tf", fontName, fontSize));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0:F2} {1:F2} Tm", x, y));
        _currentPage.Content.AppendLine($"({safeText}) Tj");
        _currentPage.Content.AppendLine("ET");
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float r = 0.8f, float g = 0.8f, float b = 0.8f, float lineWidth = 1.0f)
    {
        if (_currentPage == null) return;
        _currentPage.Content.AppendLine("q");
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} RG", r, g, b));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2} w", lineWidth));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2} {1:F2} m", x1, y1));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2} {1:F2} l", x2, y2));
        _currentPage.Content.AppendLine("S Q");
    }

    public void DrawFilledRect(float x, float y, float width, float height, float r, float g, float b)
    {
        if (_currentPage == null) return;
        _currentPage.Content.AppendLine("q");
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} rg", r, g, b));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} {3:F2} re", x, y, width, height));
        _currentPage.Content.AppendLine("f Q");
    }

    public void DrawRect(float x, float y, float width, float height, float r, float g, float b, float lineWidth = 1.0f)
    {
        if (_currentPage == null) return;
        _currentPage.Content.AppendLine("q");
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} RG", r, g, b));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2} w", lineWidth));
        _currentPage.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} {3:F2} re", x, y, width, height));
        _currentPage.Content.AppendLine("S Q");
    }

    public void DrawMetricCard(float x, float y, float w, float h, string title, string primaryVal, string subVal, bool? isPositive = null)
    {
        // Card background & subtle border
        DrawFilledRect(x, y, w, h, 0.96f, 0.97f, 0.99f);
        DrawRect(x, y, w, h, 0.85f, 0.88f, 0.93f, 1.0f);

        // Title
        DrawText(x + 8, y + h - 14, title, 7f, true, 0.40f, 0.45f, 0.55f);

        // Value
        float valR = 0.08f, valG = 0.12f, valB = 0.18f;
        if (isPositive.HasValue)
        {
            valR = isPositive.Value ? 0.05f : 0.85f;
            valG = isPositive.Value ? 0.60f : 0.15f;
            valB = isPositive.Value ? 0.25f : 0.15f;
        }
        DrawText(x + 8, y + h - 29, primaryVal, 11f, true, valR, valG, valB);

        // Subtitle
        DrawText(x + 8, y + 7, subVal, 7f, false, 0.45f, 0.50f, 0.58f);
    }

    public void DrawSectionHeader(float x, float y, string title)
    {
        DrawFilledRect(x, y - 4, 515, 18, 0.92f, 0.94f, 0.97f);
        DrawLine(x, y - 4, x, y + 14, 0.15f, 0.38f, 0.92f, 3.0f); // Blue accent tag
        DrawText(x + 8, y, title, 9f, true, 0.10f, 0.18f, 0.30f);
    }

    public void DrawTableHeader(float[] colX, string[] titles, float y)
    {
        float totalWidth = colX[^1] - colX[0];
        DrawFilledRect(colX[0], y - 3, totalWidth, 14, 0.12f, 0.16f, 0.24f);

        for (int i = 0; i < titles.Length && i < colX.Length - 1; i++)
        {
            DrawText(colX[i] + 4, y + 1, titles[i], 7.5f, true, 0.95f, 0.95f, 0.95f);
        }
    }

    public void DrawTableRowBackground(float x1, float x2, float y, float h, bool zebra)
    {
        if (zebra)
        {
            DrawFilledRect(x1, y - 2, x2 - x1, h, 0.97f, 0.98f, 0.99f);
        }
        DrawLine(x1, y - 2, x2, y - 2, 0.90f, 0.92f, 0.95f, 0.5f);
    }

    public byte[] ToByteArray()
    {
        if (_pages.Count == 0)
        {
            NewPage();
        }

        // Draw footers on every page now that total page count is known
        int totalPages = _pages.Count;
        for (int i = 0; i < totalPages; i++)
        {
            var page = _pages[i];
            string footerText = $"Crypto Trader Platform • Page {i + 1} of {totalPages} • ISO 32000 Financial Statement";
            page.Content.AppendLine("BT");
            page.Content.AppendLine("0.5 0.55 0.6 rg");
            page.Content.AppendLine("/F1 7.5 Tf");
            page.Content.AppendLine(string.Format(CultureInfo.InvariantCulture, "1 0 0 1 40 25 Tm"));
            page.Content.AppendLine($"({EscapePdfString(footerText)}) Tj");
            page.Content.AppendLine("ET");

            // Thin footer divider
            page.Content.AppendLine("q 0.85 0.88 0.92 RG 0.5 w 40 35 m 555 35 l S Q");
        }

        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, Encoding.ASCII))
        {
            writer.NewLine = "\n";
            writer.WriteLine("%PDF-1.4");
            writer.WriteLine("%âãÏÓ");
            writer.Flush();

            var offsets = new List<long> { 0 }; // object 0 is unused in PDF

            // Object 1: Catalog
            offsets.Add(ms.Position);
            writer.WriteLine("1 0 obj");
            writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
            writer.WriteLine("endobj");
            writer.Flush();

            int fontF1Obj = 3;
            int fontF2Obj = 4;
            int fontF3Obj = 5;
            int firstPageObj = 6;

            // Object 2: Pages
            offsets.Add(ms.Position);
            writer.WriteLine("2 0 obj");
            writer.Write("<< /Type /Pages /Kids [");
            for (int i = 0; i < totalPages; i++)
            {
                writer.Write($"{firstPageObj + 2 * i} 0 R ");
            }
            writer.WriteLine($"] /Count {totalPages} >>");
            writer.WriteLine("endobj");
            writer.Flush();

            // Object 3: Font F1
            offsets.Add(ms.Position);
            writer.WriteLine($"{fontF1Obj} 0 obj");
            writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            writer.WriteLine("endobj");
            writer.Flush();

            // Object 4: Font F2
            offsets.Add(ms.Position);
            writer.WriteLine($"{fontF2Obj} 0 obj");
            writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            writer.WriteLine("endobj");
            writer.Flush();

            // Object 5: Font F3
            offsets.Add(ms.Position);
            writer.WriteLine($"{fontF3Obj} 0 obj");
            writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>");
            writer.WriteLine("endobj");
            writer.Flush();

            // For each page, write Page object followed by Contents stream object
            for (int i = 0; i < totalPages; i++)
            {
                int pageObjNum = firstPageObj + 2 * i;
                int contentObjNum = pageObjNum + 1;
                var pageContent = _pages[i].Content.ToString();
                var contentBytes = Encoding.ASCII.GetBytes(pageContent);

                // Page Object
                offsets.Add(ms.Position);
                writer.WriteLine($"{pageObjNum} 0 obj");
                writer.WriteLine("<<");
                writer.WriteLine("  /Type /Page");
                writer.WriteLine("  /Parent 2 0 R");
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "  /MediaBox [0 0 {0:F2} {1:F2}]", PageWidth, PageHeight));
                writer.WriteLine("  /Resources <<");
                writer.WriteLine($"    /Font << /F1 {fontF1Obj} 0 R /F2 {fontF2Obj} 0 R /F3 {fontF3Obj} 0 R >>");
                writer.WriteLine("  >>");
                writer.WriteLine($"  /Contents {contentObjNum} 0 R");
                writer.WriteLine(">>");
                writer.WriteLine("endobj");
                writer.Flush();

                // Content Stream Object
                offsets.Add(ms.Position);
                writer.WriteLine($"{contentObjNum} 0 obj");
                writer.WriteLine($"<< /Length {contentBytes.Length} >>");
                writer.WriteLine("stream");
                writer.Flush();

                ms.Write(contentBytes, 0, contentBytes.Length);
                writer.WriteLine();
                writer.WriteLine("endstream");
                writer.WriteLine("endobj");
                writer.Flush();
            }

            // XRef Table
            long xrefStart = ms.Position;
            int totalObjects = offsets.Count;
            writer.WriteLine("xref");
            writer.WriteLine($"0 {totalObjects}");
            writer.WriteLine("0000000000 65535 f ");
            for (int i = 1; i < totalObjects; i++)
            {
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:D10} 00000 n ", offsets[i]));
            }

            // Trailer
            writer.WriteLine("trailer");
            writer.WriteLine("<<");
            writer.WriteLine($"  /Size {totalObjects}");
            writer.WriteLine("  /Root 1 0 R");
            writer.WriteLine(">>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefStart);
            writer.WriteLine("%%EOF");
            writer.Flush();
        }

        return ms.ToArray();
    }

    private static string EscapePdfString(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length + 10);
        foreach (char c in text)
        {
            if (c == '\\' || c == '(' || c == ')')
            {
                sb.Append('\\').Append(c);
            }
            else if (c >= 32 && c <= 126)
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }
}
