using CSEInvestmentTool.Application.Models;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Web.Helpers;
using Microsoft.AspNetCore.Components;
using System.Timers;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class AddStock : IDisposable
    {
        private StockEntryModel _stockEntry = new();
        private string? _errorMessage;
        private bool _loadingSymbol = false;
        private bool _symbolFound = false;
        private List<StockSymbolInfo> _relatedSymbols = new();
        private long _totalIssuedQuantity = 0;

        // Formatted inputs
        private string _formattedLiabilities = string.Empty;
        private string _formattedEquity = string.Empty;

        // Debounce timer for symbol input
        private System.Timers.Timer? _debounceTimer;
        private string _currentSymbolInput = string.Empty;

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

            // Initialize debounce timer
            _debounceTimer = new System.Timers.Timer(500); // 500ms delay
            _debounceTimer.Elapsed += OnDebounceElapsed;
            _debounceTimer.AutoReset = false;
        }

        private async void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
        {
            // This runs after the debounce timer elapses
            await InvokeAsync(async () =>
            {
                await FetchStockDataAsync(_currentSymbolInput);
                StateHasChanged();
            });
        }

        private async Task OnSymbolInputAsync(ChangeEventArgs e)
        {
            _symbolFound = false;
            _relatedSymbols.Clear();
            _totalIssuedQuantity = 0;
            _stockEntry.Fundamentals.MarketPrice = 0;
            _stockEntry.Fundamentals.NAV = 0;

            var symbol = e.Value?.ToString();
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return;
            }

            // Store current input and restart debounce timer
            _currentSymbolInput = symbol;
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private async Task FetchStockDataAsync(string symbol)
        {
            try
            {
                _loadingSymbol = true;
                StateHasChanged();

                if (string.IsNullOrWhiteSpace(symbol) || symbol.Length < 3)
                {
                    return;
                }

                // Get stock data from API
                var stockData = await StockCalculationService.GetStockDataBySymbolAsync(symbol);

                if (stockData != null)
                {
                    // Stock found
                    _symbolFound = true;

                    // Update company name if it's empty
                    if (string.IsNullOrEmpty(_stockEntry.Stock.CompanyName))
                    {
                        _stockEntry.Stock.CompanyName = stockData.CompanyName;
                    }

                    // Set market price (rounded to 2 decimal places)
                    _stockEntry.Fundamentals.MarketPrice = Math.Round(stockData.MarketPrice, 2);

                    // Get related symbols
                    _relatedSymbols = await StockCalculationService.GetRelatedStockSymbolsAsync(symbol);

                    // Calculate total issued quantity
                    _totalIssuedQuantity = _relatedSymbols.Sum(s => s.IssuedQuantity);

                    // If total equity is already provided, calculate NAV
                    if (_stockEntry.Fundamentals.TotalEquity > 0)
                    {
                        await CalculateNAVAsync();
                    }
                }
                else
                {
                    // Stock not found
                    _symbolFound = false;
                    _relatedSymbols.Clear();
                    _totalIssuedQuantity = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching stock data for symbol {Symbol}", symbol);
            }
            finally
            {
                _loadingSymbol = false;
                StateHasChanged();
            }
        }

        private void OnLiabilitiesInput(ChangeEventArgs e)
        {
            var input = e.Value?.ToString() ?? string.Empty;
            _formattedLiabilities = input;

            // Parse the formatted input to get the actual value
            _stockEntry.Fundamentals.TotalLiabilities = DecimalFormatter.ParseShortForm(input);
        }

        private async Task OnEquityInput(ChangeEventArgs e)
        {
            var input = e.Value?.ToString() ?? string.Empty;
            _formattedEquity = input;

            // Parse the formatted input to get the actual value
            _stockEntry.Fundamentals.TotalEquity = DecimalFormatter.ParseShortForm(input);

            // If we have the total issued quantity, calculate NAV
            if (_totalIssuedQuantity > 0 && _symbolFound)
            {
                await CalculateNAVAsync();
            }
        }

        private async Task CalculateNAVAsync()
        {
            if (_stockEntry.Fundamentals.TotalEquity <= 0 || _totalIssuedQuantity <= 0 || !_symbolFound)
            {
                return;
            }

            try
            {
                // Calculate NAV and round to 2 decimal places
                decimal nav = Math.Round(_stockEntry.Fundamentals.TotalEquity / _totalIssuedQuantity, 2);
                _stockEntry.Fundamentals.NAV = nav;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error calculating NAV");
            }
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                _errorMessage = null;

                // Validate that we have the necessary data
                if (!_symbolFound)
                {
                    _errorMessage = "Please enter a valid stock symbol.";
                    StateHasChanged();
                    return;
                }

                if (_stockEntry.Fundamentals.TotalEquity <= 0)
                {
                    _errorMessage = "Total Equity must be greater than zero.";
                    StateHasChanged();
                    return;
                }

                // Make sure NAV is calculated and rounded to 2 decimal places
                if (_stockEntry.Fundamentals.NAV <= 0 && _totalIssuedQuantity > 0)
                {
                    _stockEntry.Fundamentals.NAV = Math.Round(_stockEntry.Fundamentals.TotalEquity / _totalIssuedQuantity, 2);
                }

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

                Logger.LogInformation("Successfully added new stock: {Symbol}", _stockEntry.Stock.Symbol);

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

        public void Dispose()
        {
            // Dispose the timer when the component is disposed
            _debounceTimer?.Dispose();
        }

        private class StockEntryModel
        {
            public Stock Stock { get; set; } = new();
            public FundamentalData Fundamentals { get; set; } = new();
        }
    }
}