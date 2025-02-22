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

                // Get latest recommendations
                var recommendations = await RecommendationRepository.GetLatestRecommendationsAsync();
                _recommendations = recommendations.ToList();

                // Get scores for reference
                _scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading recommendations");
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

                var scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();

                if (scores.Any())
                {
                    var recommendations = AllocationService.CalculateInvestmentAllocations(
                        scores,
                        DateTime.UtcNow.Date);

                    // Save recommendations to database
                    foreach (var recommendation in recommendations)
                    {
                        await RecommendationRepository.AddRecommendationAsync(recommendation);
                    }

                    // Reload data
                    await LoadData();
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
    }
}