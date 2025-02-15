using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface IFundamentalDataRepository
{
    Task<IEnumerable<FundamentalData>> GetFundamentalDataForStockAsync(int stockId);
    Task<FundamentalData?> GetLatestFundamentalDataForStockAsync(int stockId);
    Task AddFundamentalDataAsync(FundamentalData data);
}
