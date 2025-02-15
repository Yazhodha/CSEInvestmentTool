using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CSEInvestmentTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    StockId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sector = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.StockId);
                });

            migrationBuilder.CreateTable(
                name: "FundamentalData",
                columns: table => new
                {
                    FundamentalId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PERatio = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ROE = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DividendYield = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DebtToEquityRatio = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NetProfitMargin = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MarketCap = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TradingVolume = table.Column<long>(type: "bigint", nullable: true),
                    HighPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    LowPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalData", x => x.FundamentalId);
                    table.ForeignKey(
                        name: "FK_FundamentalData_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "StockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentRecommendations",
                columns: table => new
                {
                    RecommendationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    RecommendationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecommendedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RecommendationReason = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentRecommendations", x => x.RecommendationId);
                    table.ForeignKey(
                        name: "FK_InvestmentRecommendations_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "StockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockScores",
                columns: table => new
                {
                    ScoreId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    ScoreDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PEScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ROEScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DividendYieldScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DebtEquityScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProfitMarginScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockScores", x => x.ScoreId);
                    table.ForeignKey(
                        name: "FK_StockScores_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "StockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalData_StockId_Date",
                table: "FundamentalData",
                columns: new[] { "StockId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentRecommendations_StockId_RecommendationDate",
                table: "InvestmentRecommendations",
                columns: new[] { "StockId", "RecommendationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Symbol",
                table: "Stocks",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockScores_StockId_ScoreDate",
                table: "StockScores",
                columns: new[] { "StockId", "ScoreDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundamentalData");

            migrationBuilder.DropTable(
                name: "InvestmentRecommendations");

            migrationBuilder.DropTable(
                name: "StockScores");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
