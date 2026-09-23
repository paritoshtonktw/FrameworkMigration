using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NUnit.Framework;
using CryptoTrading.Data;
using CryptoTrading.Data.Repositories;

namespace CryptoTrading.Tests
{
    [TestFixture]
    public class DatabaseIntegrationTests
    {
        private IDbConnectionFactory _dbFactory;
        private IAccountRepository _accountRepo;
        private ITradingRepository _tradingRepo;
        private IDepositRepository _depositRepo;
        private IWithdrawalRepository _withdrawalRepo;
        private IPortfolioRepository _portfolioRepo;
        private IUserRepository _userRepo;

        [SetUp]
        public void Setup()
        {
            _dbFactory = new SqlConnectionFactory(@"Server=localhost,1433;Database=CryptoTradingDB;User Id=sa;Password=CryptoTrading!2026Secure;TrustServerCertificate=True;");
            _accountRepo = new AccountRepository(_dbFactory);
            _tradingRepo = new TradingRepository(_dbFactory);
            _depositRepo = new DepositRepository(_dbFactory);
            _withdrawalRepo = new WithdrawalRepository(_dbFactory);
            _portfolioRepo = new PortfolioRepository(_dbFactory);
            _userRepo = new UserRepository(_dbFactory);
        }

        [Test]
        public async Task LocalDb_GetPortfolio_ReturnsValidSummaryAndHoldings()
        {
            // Test user 1 (trader1 from seed data)
            var portfolio = await _portfolioRepo.GetPortfolioAsync(1);

            Assert.IsNotNull(portfolio);
            Assert.IsNotNull(portfolio.Summary);
            Assert.AreEqual(1, portfolio.Summary.UserId);
            Assert.GreaterOrEqual(portfolio.Summary.CashBalance, 0);
            Assert.GreaterOrEqual(portfolio.Holdings.Count, 1);
        }

        [Test]
        public async Task LocalDb_DepositAndWithdrawal_AtomicAndMaintainsIntegrity()
        {
            // Create a test user for isolated financial test
            string testUsername = "fin_user_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = await _userRepo.CreateUserAsync(
                testUsername,
                testUsername + "@test.com",
                "dummy_hash",
                "Fin",
                "Tester"
            );

            Assert.IsNotNull(user);
            int userId = user.UserId;

            // 1. Initial balance should be 0
            var initialBal = await _accountRepo.GetAccountBalanceAsync(userId, "USD");
            Assert.AreEqual(0.00m, initialBal);

            // 2. Deposit $5,000
            var deposit = await _depositRepo.ProcessDepositAsync(userId, 5000.00m, "USD");
            Assert.AreEqual(5000.00m, deposit.NewBalance);

            // 3. Withdraw $1,500
            var withdrawal = await _withdrawalRepo.ProcessWithdrawalAsync(userId, 1500.00m, "USD");
            Assert.AreEqual(3500.00m, withdrawal.RemainingBalance);

            // 4. Over-withdrawal of $10,000 should fail atomically (SRS Section 24)
            var ex = Assert.ThrowsAsync<SqlException>(async () =>
                await _withdrawalRepo.ProcessWithdrawalAsync(userId, 10000.00m, "USD")
            );
            Assert.That(ex.Message, Does.Contain("Insufficient balance"));

            // 5. Balance should remain unchanged at $3,500
            var finalBal = await _accountRepo.GetAccountBalanceAsync(userId, "USD");
            Assert.AreEqual(3500.00m, finalBal);
        }

        [Test]
        public async Task LocalDb_BuyAndSellExecution_CalculatesRealizedPLAndBalances()
        {
            // Create an isolated trader user
            string testUsername = "trade_user_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = await _userRepo.CreateUserAsync(
                testUsername,
                testUsername + "@test.com",
                "dummy_hash",
                "Trade",
                "Tester"
            );
            int userId = user.UserId;

            // Fund account with $20,000
            await _depositRepo.ProcessDepositAsync(userId, 20000.00m, "USD");

            // 1. Buy 0.1 BTC at $60,000 -> Trade Value: $6,000
            var buyTrade = await _tradingRepo.ExecuteBuyOrderAsync(userId, "BTC", 0.1m, 60000.00m);
            Assert.AreEqual(6000.00m, buyTrade.TotalValue);
            Assert.AreEqual(14000.00m, buyTrade.RemainingBalance);

            // 2. Sell 0.1 BTC at $65,000 -> Trade Value: $6,500, Realized P/L: +$500
            var sellTrade = await _tradingRepo.ExecuteSellOrderAsync(userId, "BTC", 0.1m, 65000.00m);
            Assert.AreEqual(6500.00m, sellTrade.TotalValue);
            Assert.AreEqual(500.00m, sellTrade.RealizedProfitLoss); // (65000 - 60000) * 0.1 = 500
            Assert.AreEqual(20500.00m, sellTrade.RemainingBalance);  // 14000 + 6500 = 20500
            Assert.AreEqual(0.00000000m, sellTrade.RemainingCryptoHolding);

            // 3. Selling when holdings are 0 must fail (SRS Section 24)
            var ex = Assert.ThrowsAsync<SqlException>(async () =>
                await _tradingRepo.ExecuteSellOrderAsync(userId, "BTC", 0.1m, 65000.00m)
            );
            Assert.That(ex.Message, Does.Contain("Insufficient cryptocurrency holdings"));
        }
    }
}

