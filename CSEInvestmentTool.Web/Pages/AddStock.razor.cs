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

                // Add or reactivate the stock and get the stock with ID
                var savedStock = await StockRepository.AddStockAsync(_stockEntry.Stock);

                // Set the StockId from the saved stock
                _stockEntry.Fundamentals.StockId = savedStock.StockId;

                // Add the fundamental data
                await FundamentalRepository.AddFundamentalDataAsync(_stockEntry.Fundamentals);

                // Calculate and save the score automatically
                var score = ScoringService.CalculateScore(_stockEntry.Fundamentals);
                await ScoreRepository.AddStockScoreAsync(score);

                Logger.LogInformation("Successfully added new stock: {Symbol}",
                    _stockEntry.Stock.Symbol);

                // Navigate back to the stocks list
                NavigationManager.NavigateTo("/stocks");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving stock: {Symbol}", _stockEntry.Stock.Symbol);

                if (ex is InvalidOperationException)
                {
                    _errorMessage = ex.Message;
                }
                else if (ex.InnerException?.Message.Contains("IX_Stocks_Symbol") == true)
                {
                    _errorMessage = $"A stock with symbol '{_stockEntry.Stock.Symbol}' already exists.";
                }
                else if (ex.InnerException?.Message.Contains("FK_FundamentalData_Stocks_StockId") == true)
                {
                    _errorMessage = "Error associating fundamental data with stock. Please try again.";
                }
                else
                {
                    _errorMessage = "An error occurred while saving the stock. Please try again.";
                }

                StateHasChanged();
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