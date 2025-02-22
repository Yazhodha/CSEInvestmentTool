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

                // Always generate fresh recommendations based on current scores
                if (_scores.Any())
                {
                    var recommendations = AllocationService.CalculateInvestmentAllocations(
                        _scores,
                        DateTime.UtcNow.Date);

                    // Save recommendations
                    foreach (var recommendation in recommendations)
                    {
                        await RecommendationRepository.AddRecommendationAsync(recommendation);
                    }

                    // Get the latest recommendations after saving
                    _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();
                }
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