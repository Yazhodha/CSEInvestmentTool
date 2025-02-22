using CSEInvestmentTool.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace CSEInvestmentTool.Web.Components
{
    public partial class StockCard
    {
        [Parameter] public Stock Stock { get; set; } = default!;
        [Parameter] public FundamentalData? FundamentalData { get; set; }
        [Parameter] public StockScore? Score { get; set; }

        private string GetScoreBackgroundColor()
        {
            if (Score == null) return "bg-gray-100";
            return Score.TotalScore switch
            {
                >= 70 => "bg-green-50 border-green-200",
                >= 50 => "bg-yellow-50 border-yellow-200",
                _ => "bg-red-50 border-red-200"
            };
        }

        private string GetScoreTextColor()
        {
            if (Score == null) return "text-gray-500";
            return Score.TotalScore switch
            {
                >= 70 => "text-green-700",
                >= 50 => "text-yellow-700",
                _ => "text-red-700"
            };
        }

        private string GetTrendIconClass()
        {
            if (Score == null) return "text-gray-400";
            return Score.TotalScore switch
            {
                >= 70 => "text-green-500",
                >= 50 => "text-yellow-500",
                _ => "text-red-500"
            };
        }
    }
}