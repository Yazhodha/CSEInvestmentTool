using CSEInvestmentTool.Application.Interfaces;

namespace CSEInvestmentTool.Infrastructure.Services;

public class TestDataSeeder
{
    private readonly IStockRepository _stockRepository;

    public TestDataSeeder(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async Task SeedTestDataAsync()
    {
        // No test data seeding - stocks will be added through the UI
    }
}