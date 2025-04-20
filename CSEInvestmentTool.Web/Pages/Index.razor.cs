using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class Index
    {
        [Inject]
        private IConfiguration Configuration { get; set; } = default!;

        private bool _loading = true;
        private List<StockScore> _scores = new();
        private List<InvestmentRecommendation> _recommendations = new();
        private decimal _monthlyInvestmentAmount = 50000m;
        private decimal _newMonthlyAmount = 50000m;
        private bool _showBudgetModal = false;
        private string? _budgetErrorMessage;
        private string _selectedSector = "";
        private List<string> _sectors = new();

        protected override async Task OnInitializedAsync()
        {
            // Load the monthly investment amount from database
            _monthlyInvestmentAmount = await AllocationService.GetMonthlyInvestmentAmountAsync();
            _newMonthlyAmount = _monthlyInvestmentAmount;

            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                _loading = true;

                // Get latest scores
                _scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();

                // Get unique sectors
                _sectors = _scores
                    .Where(s => s.Stock != null)
                    .Select(s => s.Stock!.Sector)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                // Get latest recommendations
                _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading recommendations");
                _recommendations = new List<InvestmentRecommendation>();
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task GenerateRecommendations()
        {
            try
            {
                _loading = true;

                // Get latest scores
                _scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();

                // Calculate fresh recommendations
                if (_scores.Any())
                {
                    var recommendations = await AllocationService.CalculateInvestmentAllocationsAsync(
                        _scores,
                        DateTime.UtcNow.Date,
                        _monthlyInvestmentAmount);

                    // Save each recommendation
                    foreach (var recommendation in recommendations)
                    {
                        try
                        {
                            await RecommendationRepository.AddRecommendationAsync(recommendation);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Error saving recommendation for stock {StockId}", recommendation.StockId);
                        }
                    }

                    // Get the latest recommendations after saving
                    _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private void NavigateToStockDetails(int stockId)
        {
            Navigation.NavigateTo($"/stocks/{stockId}");
        }

        private void OpenBudgetModal()
        {
            _newMonthlyAmount = _monthlyInvestmentAmount;
            _budgetErrorMessage = null;
            _showBudgetModal = true;
        }

        private void CloseBudgetModal()
        {
            _showBudgetModal = false;
        }

        private async Task UpdateBudget()
        {
            try
            {
                if (_newMonthlyAmount <= 0)
                {
                    _budgetErrorMessage = "Monthly budget must be greater than zero.";
                    return;
                }

                var result = await AllocationService.UpdateMonthlyInvestmentAmountAsync(_newMonthlyAmount);

                if (result)
                {
                    _monthlyInvestmentAmount = _newMonthlyAmount;
                    _showBudgetModal = false;
                    await GenerateRecommendations();
                }
                else
                {
                    _budgetErrorMessage = "Failed to update the budget. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating monthly investment budget");
                _budgetErrorMessage = "An error occurred while updating the budget.";
            }
        }

        private async Task ShowAllSectors()
        {
            _selectedSector = "";
            await LoadData();
        }

        private decimal GetAverageScore()
        {
            var filteredScores = string.IsNullOrEmpty(_selectedSector)
                ? _scores
                : _scores.Where(s => s.Stock?.Sector == _selectedSector);

            return filteredScores.Any() ? filteredScores.Average(s => s.TotalScore) : 0;
        }

        private int GetFilteredRecommendationsCount()
        {
            return string.IsNullOrEmpty(_selectedSector)
                ? _recommendations.Count
                : _recommendations.Count(r => r.Stock?.Sector == _selectedSector);
        }
    }
}