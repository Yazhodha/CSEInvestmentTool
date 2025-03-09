namespace CSEInvestmentTool.Application.Models
{
    public class CompanySearchResult
    {
        public string CompanyName { get; set; } = string.Empty;
        public List<string> Symbols { get; set; } = new List<string>();

        public bool HasMultipleStocks => Symbols.Count > 1;

        public override string ToString()
        {
            return CompanyName;
        }
    }
}