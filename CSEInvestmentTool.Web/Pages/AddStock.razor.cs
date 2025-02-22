using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class AddStock
    {
        private StockEntryModel _stockEntry = new();
        private string? _errorMessage;
        private readonly List<string> _sectors = new()
    {
        "Banks",
        "Diversified Holdings",
        "Telecommunications",
        "Manufacturing",
        "Hotels & Travel",
        "Beverage Food & Tobacco",
        "Insurance",
        "Construction & Engineering",
        "Power & Energy",
        "Healthcare",
        "Investment Trusts",
        "Trading",
        "Transportation",
        "Plantations"
    };

        protected override void OnInitialized()
        {
            // Initialize any default values
            _stockEntry.Stock.IsActive = true;
            _stockEntry.Fundamentals.Date = DateTime.UtcNow.Date;
            _stockEntry.Fundamentals.LastUpdated = DateTime.UtcNow;
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                _errorMessage = null;

                // Set last updated timestamp
                _stockEntry.Stock.LastUpdated = DateTime.UtcNow;

                // Add the stock first
                await StockRepository.AddStockAsync(_stockEntry.Stock);

                // Set the StockId for the fundamental data
                _stockEntry.Fundamentals.StockId = _stockEntry.Stock.StockId;

                // Add the fundamental data
                await FundamentalRepository.AddFundamentalDataAsync(_stockEntry.Fundamentals);

                // Calculate score
                var score = ScoringService.CalculateScore(_stockEntry.Fundamentals);
                await ScoreRepository.AddStockScoreAsync(score);

                // Calculate recommendation for the new stock
                var recommendations = AllocationService.CalculateInvestmentAllocations(
                    new List<StockScore> { score },
                    DateTime.UtcNow.Date);

                // Save each recommendation
                foreach (var recommendation in recommendations)
                {
                    await RecommendationRepository.AddRecommendationAsync(recommendation);
                }

                Logger.LogInformation("Successfully added new stock: {Symbol} with automatic score calculation",
                    _stockEntry.Stock.Symbol);

                // Navigate back to the stocks list
                NavigationManager.NavigateTo("/stocks");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving stock: {Symbol}", _stockEntry.Stock.Symbol);
                _errorMessage = $"Error saving stock: {ex.Message}";
            }
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/stocks");
        }

        private class StockEntryModel
        {
            public Stock Stock { get; set; } = new();
            public FundamentalData Fundamentals { get; set; } = new();
        }
    }
}