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

                // Calculate fresh recommendations based on current scores
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
                            // Continue with other recommendations even if one fails
                        }
                    }

                    // Get the latest recommendations after saving
                    _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();
                }
                else
                {
                    _recommendations = new List<InvestmentRecommendation>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading recommendations");
                // Even if there's an error, try to get existing recommendations
                _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();
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

                // Calculate fresh recommendations based on current scores
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
                            // Continue with other recommendations even if one fails
                        }
                    }

                    // Get the latest recommendations after saving
                    _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();
                }
                else
                {
                    _recommendations = new List<InvestmentRecommendation>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating recommendations");
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

                // Update in database
                var result = await AllocationService.UpdateMonthlyInvestmentAmountAsync(_newMonthlyAmount);

                if (result)
                {
                    // Update local value too
                    _monthlyInvestmentAmount = _newMonthlyAmount;

                    // Close the modal
                    _showBudgetModal = false;

                    // Regenerate recommendations with the new budget
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
    }
}