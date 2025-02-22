using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSEInvestmentTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFundamentalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtToEquityRatio",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "DividendYield",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "HighPrice",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "LowPrice",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "NetProfitMargin",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "PERatio",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "ROE",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "TradingVolume",
                table: "FundamentalData");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualDividend",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EPS",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketPrice",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NAV",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEquity",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLiabilities",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualDividend",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "EPS",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "MarketPrice",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "NAV",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "TotalEquity",
                table: "FundamentalData");

            migrationBuilder.DropColumn(
                name: "TotalLiabilities",
                table: "FundamentalData");

            migrationBuilder.AddColumn<decimal>(
                name: "DebtToEquityRatio",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DividendYield",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HighPrice",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowPrice",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetProfitMargin",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PERatio",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ROE",
                table: "FundamentalData",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TradingVolume",
                table: "FundamentalData",
                type: "bigint",
                nullable: true);
        }
    }
}
