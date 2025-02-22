using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Pages
{
    public partial class StockDetails
    {
        [Parameter]
        public int Id { get; set; }

        private Stock? _stock;
        private bool _loading = true;
        private bool _showDeleteConfirmation = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadStock();
        }

        private async Task LoadStock()
        {
            try
            {
                _loading = true;
                _stock = await StockRepository.GetStockByIdAsync(Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading stock {StockId}", Id);
            }
            finally
            {
                _loading = false;
            }
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

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/stocks");
        }
    }
}