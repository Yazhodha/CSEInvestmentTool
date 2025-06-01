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

// Add HttpClient and Memory Cache
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Configure HttpClient with longer timeout for LLM operations
builder.Services.AddHttpClient("LLM_Deepseek", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for AI analysis
});

builder.Services.AddHttpClient("LLM_Groq", client =>
{
    client.Timeout = TimeSpan.FromMinutes(3); // 3 minute timeout for Groq (faster)
});

// Register caching service
builder.Services.AddScoped<ILLMCacheService, LLMCacheService>();

// Register existing services
builder.Services.AddScoped<IDataCollectionService, CSEDataCollectionService>();
builder.Services.AddScoped<IStockScoringService, StockScoringService>();
builder.Services.AddScoped<IInvestmentAllocationService, InvestmentAllocationService>();

// Register LLM services
builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ILLMInvestmentService, LLMInvestmentService>();

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure();
            npgsqlOptions.CommandTimeout(300); // 5 minute command timeout for complex queries
        }
    ));

// Register repositories
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IFundamentalDataRepository, FundamentalDataRepository>();
builder.Services.AddScoped<IStockScoreRepository, StockScoreRepository>();
builder.Services.AddScoped<IInvestmentRecommendationRepository, InvestmentRecommendationRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<ICSEApiService, CSEApiService>();
builder.Services.AddScoped<IStockCalculationService, StockCalculationService>();

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

app.MapBlazorHub(options =>
{
    // Increase SignalR timeout for long-running AI operations
    options.TransportSendTimeout = TimeSpan.FromMinutes(6);
});

app.MapFallbackToPage("/_Host");
app.MapControllers();

// Initialize database
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        // Clear LLM cache in development to avoid cached issues
        var cacheService = scope.ServiceProvider.GetRequiredService<ILLMCacheService>();
        cacheService.ClearCache();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Cleared LLM cache for development environment");

        // Log available LLM providers
        var llmFactory = scope.ServiceProvider.GetRequiredService<ILLMProviderFactory>();
        var availableProviders = llmFactory.GetAvailableProviders();
        logger.LogInformation("Available LLM providers: {Providers}", string.Join(", ", availableProviders));
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}

app.Run();