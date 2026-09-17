using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Business.Services;

public interface IPortfolioService
{
    Task<PortfolioDto> GetPortfolioAsync(int userId);
    Task<IEnumerable<HoldingDto>> GetHoldingsAsync(int userId);
    Task<PortfolioPerformanceDto?> GetPerformanceAsync(int userId);
}

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepo;

    public PortfolioService(IPortfolioRepository portfolioRepo)
    {
        _portfolioRepo = portfolioRepo;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(int userId)
    {
        return await _portfolioRepo.GetPortfolioAsync(userId);
    }

    public async Task<IEnumerable<HoldingDto>> GetHoldingsAsync(int userId)
    {
        return await _portfolioRepo.GetPortfolioHoldingsAsync(userId);
    }

    public async Task<PortfolioPerformanceDto?> GetPerformanceAsync(int userId)
    {
        return await _portfolioRepo.GetPortfolioPerformanceAsync(userId);
    }
}
