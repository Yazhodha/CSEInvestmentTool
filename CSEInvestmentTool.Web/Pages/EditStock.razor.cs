using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class EditStock
    {
        [Parameter]
        public int Id { get; set; }

        private StockEntryModel _stockEntry = new();
        private Stock? _stock;
        private FundamentalData? _fundamentalData;
        private string? _errorMessage;
        private bool _loading = true;
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

        protected override async Task OnInitializedAsync()
        {
            await LoadStockData();
        }

        private async Task LoadStockData()
        {
            try
            {
                _loading = true;

                // Load stock
                _stock = await StockRepository.GetStockByIdAsync(Id);

                if (_stock != null)
                {
                    // Load fundamental data
                    _fundamentalData = await FundamentalRepository.GetLatestFundamentalDataForStockAsync(Id);

                    // Copy data to edit model
                    _stockEntry.Stock = new Stock
                    {
                        StockId = _stock.StockId,
                        Symbol = _stock.Symbol,
                        CompanyName = _stock.CompanyName,
                        Sector = _stock.Sector,
                        IsActive = _stock.IsActive,
                        LastUpdated = _stock.LastUpdated
                    };

                    if (_fundamentalData != null)
                    {
                        _stockEntry.Fundamentals = new FundamentalData
                        {
                            FundamentalId = _fundamentalData.FundamentalId,
                            StockId = _fundamentalData.StockId,
                            Date = DateTime.UtcNow.Date, // Use current date for new entry
                            MarketPrice = _fundamentalData.MarketPrice,
                            NAV = _fundamentalData.NAV,
                            EPS = _fundamentalData.EPS,
                            AnnualDividend = _fundamentalData.AnnualDividend,
                            TotalLiabilities = _fundamentalData.TotalLiabilities,
                            TotalEquity = _fundamentalData.TotalEquity,
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        // Initialize new fundamental data if none exists
                        _stockEntry.Fundamentals = new FundamentalData
                        {
                            StockId = _stock.StockId,
                            Date = DateTime.UtcNow.Date,
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading stock data for editing {StockId}", Id);
                _errorMessage = "Failed to load stock data. Please try again.";
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                _errorMessage = null;

                if (_stock == null)
                {
                    _errorMessage = "Stock not found.";
                    return;
                }

                // Update the stock properties
                _stock.Symbol = _stockEntry.Stock.Symbol;
                _stock.CompanyName = _stockEntry.Stock.CompanyName;
                _stock.Sector = _stockEntry.Stock.Sector;
                _stock.LastUpdated = DateTime.UtcNow;

                // Update the stock
                await StockRepository.UpdateStockAsync(_stock);

                // Set the StockId for fundamental data (in case it's a new entry)
                _stockEntry.Fundamentals.StockId = _stock.StockId;

                // Make sure date is set correctly
                _stockEntry.Fundamentals.Date = DateTime.UtcNow.Date;
                _stockEntry.Fundamentals.LastUpdated = DateTime.UtcNow;

                // Add/Update the fundamental data
                await FundamentalRepository.AddFundamentalDataAsync(_stockEntry.Fundamentals);

                // Calculate and save the score
                var score = ScoringService.CalculateScore(_stockEntry.Fundamentals);
                await ScoreRepository.AddStockScoreAsync(score);

                Logger.LogInformation("Successfully updated stock: {Symbol}", _stock.Symbol);

                // Navigate back to stock details
                NavigationManager.NavigateTo($"/stocks/{_stock.StockId}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating stock: {Symbol}", _stockEntry.Stock.Symbol);

                if (ex is InvalidOperationException)
                {
                    _errorMessage = ex.Message;
                }
                else
                {
                    _errorMessage = "An error occurred while updating the stock. Please try again.";
                }

                StateHasChanged();
            }
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo($"/stocks/{Id}");
        }

        private class StockEntryModel
        {
            public Stock Stock { get; set; } = new();
            public FundamentalData Fundamentals { get; set; } = new();
        }
    }
}