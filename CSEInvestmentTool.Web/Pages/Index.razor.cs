using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class Index
    {
        private bool _loading = true;
        private List<StockScore> _scores = new();
        private List<InvestmentRecommendation> _recommendations = new();

        protected override async Task OnInitializedAsync()
        {
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
                    var recommendations = AllocationService.CalculateInvestmentAllocations(
                        _scores,
                        DateTime.UtcNow.Date);

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

                await LoadData();
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
    }
}