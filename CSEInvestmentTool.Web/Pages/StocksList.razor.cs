using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class StocksList
    {
        private bool _loading = true;
        private List<Stock> _stocks = new();
        private List<FundamentalData> _fundamentalData = new();
        private List<StockScore> _scores = new();
        private string _selectedSector = "";
        private List<string> _sectors = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                _loading = true;
                StateHasChanged();

                // Get all active stocks
                _stocks = (await StockRepository.GetAllStocksAsync()).ToList();

                if (_stocks.Any())
                {
                    // Get fundamental data
                    _fundamentalData = new List<FundamentalData>();
                    foreach (var stock in _stocks)
                    {
                        var fundamentalData = await FundamentalRepository.GetLatestFundamentalDataForStockAsync(stock.StockId);
                        if (fundamentalData != null)
                        {
                            _fundamentalData.Add(fundamentalData);
                        }
                    }

                    // Get scores
                    _scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();

                    // Get unique sectors
                    _sectors = _stocks.Select(s => s.Sector)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct()
                        .OrderBy(s => s)
                        .Cast<string>()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                _loading = false;
                StateHasChanged();
            }
        }

        private IEnumerable<Stock> GetFilteredStocks()
        {
            return string.IsNullOrEmpty(_selectedSector)
                ? _stocks
                : _stocks.Where(s => s.Sector == _selectedSector);
        }

        private string GetScoreBadgeClass(decimal score)
        {
            var baseClass = "px-2 py-1 text-xs font-medium rounded-full ";
            return score switch
            {
                >= 70 => baseClass + "bg-green-100 text-green-800",
                >= 50 => baseClass + "bg-yellow-100 text-yellow-800",
                _ => baseClass + "bg-red-100 text-red-800"
            };
        }

        private async Task CalculateScore(int stockId, FundamentalData fundamentals)
        {
            try
            {
                var score = ScoringService.CalculateScore(fundamentals);
                await ScoreRepository.AddStockScoreAsync(score);
                await LoadData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating score: {ex.Message}");
            }
        }

        private void AddNewStock()
        {
            Navigation.NavigateTo("/stocks/add");
        }

        private void NavigateToDetails(int stockId)
        {
            Navigation.NavigateTo($"/stocks/{stockId}");
        }
    }
}