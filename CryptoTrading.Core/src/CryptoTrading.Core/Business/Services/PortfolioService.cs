using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Business.Services
{
    public interface IPortfolioService
    {
        Task<PortfolioDto> GetPortfolioAsync(int userId);
        Task<List<HoldingDto>> GetHoldingsAsync(int userId);
        Task<PortfolioPerformanceDto> GetPerformanceAsync(int userId);
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

        public async Task<List<HoldingDto>> GetHoldingsAsync(int userId)
        {
            var holdings = await _portfolioRepo.GetPortfolioHoldingsAsync(userId);
            return holdings.ToList();
        }

        public async Task<PortfolioPerformanceDto> GetPerformanceAsync(int userId)
        {
            var perf = await _portfolioRepo.GetPortfolioPerformanceAsync(userId);
            if (perf == null)
            {
                throw new KeyNotFoundException($"Portfolio performance for user #{userId} was not found.");
            }
            return perf;
        }
    }
}
