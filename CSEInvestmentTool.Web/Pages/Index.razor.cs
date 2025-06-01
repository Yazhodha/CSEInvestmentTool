using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Constants;
using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class Index
    {
        [Inject]
        private IConfiguration Configuration { get; set; } = default!;

        [Inject]
        private ILLMInvestmentService LLMInvestmentService { get; set; } = default!;

        private bool _loading = true;
        private List<StockScore> _scores = new();
        private List<InvestmentRecommendation> _recommendations = new();
        private List<InvestmentRecommendation> _filteredRecommendations = new();
        private List<Stock> _stocks = new();
        private List<string> _availableSectors = new();
        private string _selectedSector = "";
        private decimal _monthlyInvestmentAmount = 50000m;
        private decimal _newMonthlyAmount = 50000m;
        private bool _showBudgetModal = false;
        private string? _budgetErrorMessage;
        private string? _errorMessage;
        private string? _successMessage;
        private string? _currentRecommendationMethod = null;
        private DateTime? _lastRecommendationDate = null;
        private bool _methodSwitching = false;

        // Fixed method selection using string
        private string _selectedMethodString = "Algorithm";
        private InvestmentPhilosophyType _selectedPhilosophy = InvestmentPhilosophyType.BalancedApproach;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Load the monthly investment amount from database
                _monthlyInvestmentAmount = await AllocationService.GetMonthlyInvestmentAmountAsync();
                _newMonthlyAmount = _monthlyInvestmentAmount;

                await LoadData();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing dashboard");
                _errorMessage = "Failed to initialize dashboard. Please refresh the page.";
            }
            finally
            {
                _loading = false;
                StateHasChanged(); // Ensure UI updates when loading is complete
            }
        }

        private async Task OnMethodChanged()
        {
            try
            {
                _methodSwitching = true;
                StateHasChanged();

                // Clear any previous messages when method changes
                _errorMessage = null;
                _successMessage = null;

                Logger.LogInformation("Method changed to: {Method}", _selectedMethodString);

                // Clear recommendations if switching methods
                if (_currentRecommendationMethod != null && _currentRecommendationMethod != _selectedMethodString)
                {
                    _recommendations.Clear();
                    _filteredRecommendations.Clear();
                    _currentRecommendationMethod = null;
                    _lastRecommendationDate = null;

                    Logger.LogInformation("Cleared previous recommendations due to method change");
                }

                // If switching to AI, check if LLM service is configured
                if (_selectedMethodString == "AI")
                {
                    await CheckLLMConfiguration();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error changing method");
                _errorMessage = "Error changing method. Please try again.";
            }
            finally
            {
                _methodSwitching = false;
                StateHasChanged();
            }
        }

        private void OnSectorChanged()
        {
            try
            {
                FilterRecommendationsBySector();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error filtering by sector");
            }
        }

        private void FilterRecommendationsBySector()
        {
            if (string.IsNullOrEmpty(_selectedSector))
            {
                _filteredRecommendations = _recommendations.ToList();
            }
            else
            {
                _filteredRecommendations = _recommendations
                    .Where(r => r.Stock?.Sector?.Equals(_selectedSector, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            Logger.LogInformation("Filtered recommendations: {Count} stocks for sector '{Sector}'",
                _filteredRecommendations.Count, _selectedSector);
        }

        private async Task CheckLLMConfiguration()
        {
            try
            {
                // Check if we have the required configuration
                var deepseekApiKey = Configuration["LLM:Providers:Deepseek:ApiKey"];
                if (string.IsNullOrEmpty(deepseekApiKey) || deepseekApiKey == "your-deepseek-api-key-here")
                {
                    _errorMessage = "AI Analysis requires Deepseek API key configuration. Please add your API key to appsettings.Development.json.";
                    return;
                }

                // Try to get available philosophies to test the service
                var philosophies = await LLMInvestmentService.GetAvailablePhilosophiesAsync();
                if (philosophies?.Any() != true)
                {
                    _errorMessage = "AI Analysis service is not properly configured.";
                    return;
                }

                _successMessage = $"AI Analysis ready! Using {philosophies.Count} investment philosophies.";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error checking LLM configuration");
                _errorMessage = "AI Analysis service is not available. Please check your configuration.";
            }
        }

        private async Task LoadData()
        {
            try
            {
                // Don't set loading to true here if already loading to prevent UI flicker
                if (!_loading)
                {
                    _loading = true;
                    StateHasChanged();
                }

                _errorMessage = null;

                // Load stocks to get sectors
                _stocks = (await StockRepository.GetAllStocksAsync()).ToList();

                // Extract unique sectors
                _availableSectors = _stocks
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.Sector))
                    .Select(s => s.Sector!)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                // Get latest scores (always needed for algorithm reference)
                _scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();

                // Get latest recommendations
                _recommendations = (await RecommendationRepository.GetLatestRecommendationsAsync()).ToList();

                // Determine which method generated current recommendations
                if (_recommendations.Any())
                {
                    var firstRecommendation = _recommendations.First();
                    _lastRecommendationDate = firstRecommendation.RecommendationDate;

                    // Check if recommendations contain AI analysis indicators
                    bool hasAIRecommendations = _recommendations.Any(r =>
                        r.RecommendationReason?.Contains("Deepseek Analysis") == true ||
                        r.RecommendationReason?.Contains("AI Analysis") == true);

                    _currentRecommendationMethod = hasAIRecommendations ? "AI" : "Algorithm";

                    Logger.LogInformation("Detected {Method} recommendations from {Date}",
                        _currentRecommendationMethod, _lastRecommendationDate);
                }

                // Filter recommendations by selected sector
                FilterRecommendationsBySector();

                Logger.LogInformation("Loaded {ScoreCount} scores, {RecommendationCount} recommendations, and {SectorCount} sectors",
                    _scores.Count, _recommendations.Count, _availableSectors.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading dashboard data");
                _errorMessage = "Failed to load dashboard data.";
            }
            finally
            {
                _loading = false;
                StateHasChanged();
            }
        }

        private async Task GenerateRecommendations()
        {
            if (_loading) return; // Prevent multiple simultaneous operations

            try
            {
                _loading = true;
                _errorMessage = null;
                _successMessage = null;
                StateHasChanged();

                Logger.LogInformation("Generating recommendations using method: {Method}", _selectedMethodString);

                // Clear existing recommendations to avoid mixing algorithm and AI results
                await ClearExistingRecommendations();

                if (_selectedMethodString == "AI")
                {
                    await GenerateAIRecommendations();
                }
                else
                {
                    await GenerateAlgorithmRecommendations();
                }

                // Set the current method and date
                _currentRecommendationMethod = _selectedMethodString;
                _lastRecommendationDate = DateTime.UtcNow.Date;

                // Reload data to get the latest recommendations
                await LoadData();

                if (!_recommendations.Any())
                {
                    _errorMessage = "No recommendations were generated. Please check if you have stocks with fundamental data.";
                }
                else
                {
                    _successMessage = $"Successfully generated {_recommendations.Count} recommendations using {_selectedMethodString} method!";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating recommendations using {Method}", _selectedMethodString);
                _errorMessage = _selectedMethodString == "AI"
                    ? $"Failed to generate AI recommendations: {ex.Message}"
                    : $"Failed to generate algorithm recommendations: {ex.Message}";
            }
            finally
            {
                _loading = false;
                StateHasChanged();
            }
        }

        private async Task ClearExistingRecommendations()
        {
            try
            {
                // For now, we rely on the unique constraint on StockId + RecommendationDate
                // The repositories will handle updating existing recommendations for the same date
                Logger.LogInformation("Clearing existing recommendations for today");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error clearing existing recommendations");
            }
        }

        private async Task GenerateAlgorithmRecommendations()
        {
            Logger.LogInformation("Generating algorithm-based recommendations");

            // Get latest scores
            _scores = (await ScoreRepository.GetLatestScoresAsync()).ToList();

            if (!_scores.Any())
            {
                throw new InvalidOperationException("No stock scores available. Please calculate scores first from the Stocks page.");
            }

            // Filter scores by selected sector if applicable
            var filteredScores = _scores;
            if (!string.IsNullOrEmpty(_selectedSector))
            {
                filteredScores = _scores
                    .Where(s => s.Stock?.Sector?.Equals(_selectedSector, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                if (!filteredScores.Any())
                {
                    throw new InvalidOperationException($"No stocks available in {_selectedSector} sector with calculated scores.");
                }
            }

            var recommendations = await AllocationService.CalculateInvestmentAllocationsAsync(
                filteredScores,
                DateTime.UtcNow.Date,
                _monthlyInvestmentAmount);

            if (!recommendations.Any())
            {
                throw new InvalidOperationException("Algorithm generated no recommendations. Please check your stock data.");
            }

            // Save each recommendation
            foreach (var recommendation in recommendations)
            {
                try
                {
                    await RecommendationRepository.AddRecommendationAsync(recommendation);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error saving algorithm recommendation for stock {StockId}", recommendation.StockId);
                }
            }

            Logger.LogInformation("Successfully generated {Count} algorithm-based recommendations", recommendations.Count);
        }

        private async Task GenerateAIRecommendations()
        {
            Logger.LogInformation("Generating AI-based recommendations using {Philosophy} philosophy", _selectedPhilosophy);

            try
            {
                // Check if LLM service is available
                var deepseekApiKey = Configuration["LLM:Providers:Deepseek:ApiKey"];
                if (string.IsNullOrEmpty(deepseekApiKey) || deepseekApiKey == "your-deepseek-api-key-here")
                {
                    throw new InvalidOperationException("Deepseek API key is not configured. Please add your API key to appsettings.Development.json under LLM:Providers:Deepseek:ApiKey");
                }

                // Use the allocation service method for LLM with sector filtering
                var recommendations = await AllocationService.GenerateLLMRecommendationsAsync(
                    _selectedPhilosophy,
                    DateTime.UtcNow.Date,
                    _monthlyInvestmentAmount,
                    !string.IsNullOrEmpty(_selectedSector) ? $"Focus analysis on {_selectedSector} sector stocks only." : null);

                if (!recommendations.Any())
                {
                    throw new InvalidOperationException("AI analysis generated no recommendations. This might be due to insufficient stock data or API limitations.");
                }

                // Save each recommendation
                foreach (var recommendation in recommendations)
                {
                    try
                    {
                        await RecommendationRepository.AddRecommendationAsync(recommendation);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Error saving AI recommendation for stock {StockId}", recommendation.StockId);
                    }
                }

                Logger.LogInformation("Successfully generated {Count} AI-based recommendations", recommendations.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating AI recommendations");
                throw; // Re-throw to be handled by the calling method
            }
        }

        private void NavigateToStockDetails(int stockId)
        {
            Navigation.NavigateTo($"/stocks/{stockId}");
        }

        private void OpenBudgetModal()
        {
            if (_loading) return; // Prevent opening modal during loading

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
                    _successMessage = $"Monthly budget updated to LKR {_monthlyInvestmentAmount:N0}";
                    StateHasChanged();
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