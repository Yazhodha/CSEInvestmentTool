using System;

namespace CSEInvestmentTool.Domain.Models;

public class Stock
{
    public int StockId { get; set; }
    public string Symbol { get; set; }
    public string CompanyName { get; set; }
    public string Sector { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastUpdated { get; set; }
}
