using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Data.Repositories;

public interface IPortfolioRepository
{
    Task<PortfolioDto> GetPortfolioAsync(int userId);
    Task<IEnumerable<HoldingDto>> GetPortfolioHoldingsAsync(int userId);
    Task<PortfolioSummaryDto?> GetPortfolioSummaryAsync(int userId);
    Task<PortfolioPerformanceDto?> GetPortfolioPerformanceAsync(int userId);
    Task<decimal> GetUnrealizedProfitLossAsync(int userId);
    Task<decimal> GetRealizedProfitLossAsync(int userId);
}

public class PortfolioRepository : IPortfolioRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public PortfolioRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var parameters = new { UserId = userId };

        using var multi = await connection.QueryMultipleAsync(
            "usp_GetPortfolio",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var portfolio = new PortfolioDto();
        portfolio.Summary = await multi.ReadFirstOrDefaultAsync<PortfolioSummaryDto>()
            ?? new PortfolioSummaryDto
            {
                UserId = userId,
                Username = "",
                CashBalance = 0,
                InvestedValue = 0,
                HoldingsMarketValue = 0,
                TotalPortfolioValue = 0,
                UnrealizedProfitLoss = 0,
                RealizedProfitLoss = 0,
                TotalProfitLoss = 0
            };

        portfolio.Holdings = (await multi.ReadAsync<HoldingDto>()).ToList();
        return portfolio;
    }

    public async Task<IEnumerable<HoldingDto>> GetPortfolioHoldingsAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryAsync<HoldingDto>(
            "usp_GetPortfolioHoldings",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<PortfolioSummaryDto?> GetPortfolioSummaryAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<PortfolioSummaryDto>(
            "usp_GetPortfolioSummary",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<PortfolioPerformanceDto?> GetPortfolioPerformanceAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<PortfolioPerformanceDto>(
            "usp_GetPortfolioPerformance",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<decimal> GetUnrealizedProfitLossAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<decimal>(
            "usp_GetUnrealizedProfitLoss",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<decimal> GetRealizedProfitLossAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<decimal>(
            "usp_GetRealizedProfitLoss",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
