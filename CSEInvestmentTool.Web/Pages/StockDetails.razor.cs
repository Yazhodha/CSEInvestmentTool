using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class StockDetails
    {
        [Parameter]
        public int Id { get; set; }

        private Stock? _stock;
        private FundamentalData? _fundamentalData;
        private StockScore? _stockScore;
        private bool _loading = true;
        private bool _showDeleteConfirmation = false;

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

                    // Load score
                    _stockScore = await ScoreRepository.GetLatestScoreForStockAsync(Id);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading stock data for {StockId}", Id);
            }
            finally
            {
                _loading = false;
            }
        }

        private string GetScoreColorClass(decimal score)
        {
            return score switch
            {
                >= 80 => "text-green-600",
                >= 60 => "text-green-500",
                >= 40 => "text-yellow-500",
                >= 20 => "text-orange-500",
                _ => "text-red-500"
            };
        }

        private void DeleteStock()
        {
            _showDeleteConfirmation = true;
        }

        private async Task ConfirmDelete()
        {
            try
            {
                await StockRepository.DeleteStockAsync(Id);
                NavigationManager.NavigateTo("/stocks");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting stock {StockId}", Id);
            }
        }

        private void NavigateToEdit()
        {
            NavigationManager.NavigateTo($"/stocks/edit/{Id}");
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/stocks");
        }
    }
}