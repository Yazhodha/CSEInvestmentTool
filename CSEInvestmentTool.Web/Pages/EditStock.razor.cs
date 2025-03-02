using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

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

                    // Reference the stock for display purposes only
                    _stockEntry.Stock = _stock;

                    if (_fundamentalData != null)
                    {
                        // Initialize with existing values but as a new object
                        _stockEntry.Fundamentals = new FundamentalData
                        {
                            // Don't set FundamentalId for a new record
                            StockId = _stock.StockId,
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

                // We're not updating stock properties anymore, only fundamental data
                // Only update the LastUpdated timestamp
                _stock.LastUpdated = DateTime.UtcNow;
                await StockRepository.UpdateStockAsync(_stock);

                // Create a completely new fundamental data entry with today's date
                var newFundamentalData = new FundamentalData
                {
                    StockId = _stock.StockId,
                    Date = DateTime.UtcNow.Date,
                    MarketPrice = _stockEntry.Fundamentals.MarketPrice,
                    NAV = _stockEntry.Fundamentals.NAV,
                    EPS = _stockEntry.Fundamentals.EPS,
                    AnnualDividend = _stockEntry.Fundamentals.AnnualDividend,
                    TotalLiabilities = _stockEntry.Fundamentals.TotalLiabilities,
                    TotalEquity = _stockEntry.Fundamentals.TotalEquity,
                    LastUpdated = DateTime.UtcNow
                };

                // Add as a new fundamental data record
                await FundamentalRepository.AddFundamentalDataAsync(newFundamentalData);

                // Calculate and save the score using the new fundamental data
                var score = ScoringService.CalculateScore(newFundamentalData);
                await ScoreRepository.AddStockScoreAsync(score);

                Logger.LogInformation("Successfully updated fundamental data for stock: {Symbol}", _stock.Symbol);

                // Navigate back to stock details
                NavigationManager.NavigateTo($"/stocks/{_stock.StockId}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating stock: {Symbol}", _stock.Symbol);

                if (ex is InvalidOperationException)
                {
                    _errorMessage = ex.Message;
                }
                else if (ex is DbUpdateException dbUpdateEx)
                {
                    _errorMessage = "Database error: Unable to update stock data. Please try again.";
                    Logger.LogError(dbUpdateEx, "Database error updating stock: {Symbol}", _stock.Symbol);
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