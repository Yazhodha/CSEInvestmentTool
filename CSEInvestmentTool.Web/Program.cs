using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Services;
using CSEInvestmentTool.Infrastructure.Data;
using CSEInvestmentTool.Infrastructure.Repositories;
using CSEInvestmentTool.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Add HttpClient
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<IDataCollectionService, CSEDataCollectionService>();
// In Program.cs
builder.Services.AddScoped<IStockScoringService, StockScoringService>();
builder.Services.AddScoped<IInvestmentAllocationService, InvestmentAllocationService>();

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));

// Register repositories
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IFundamentalDataRepository, FundamentalDataRepository>();
builder.Services.AddScoped<IStockScoreRepository, StockScoreRepository>();
builder.Services.AddScoped<IInvestmentRecommendationRepository, InvestmentRecommendationRepository>();

// Register TestDataSeeder
builder.Services.AddScoped<TestDataSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

// After getting the seeder but before using it
// if (app.Environment.IsDevelopment())
// {
//     using var scope = app.Services.CreateScope();
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     await db.Database.MigrateAsync();
    
//     var seeder = scope.ServiceProvider.GetRequiredService<TestDataSeeder>();
//     await seeder.SeedTestDataAsync();
// }

app.Run();