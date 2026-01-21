/*
????????????????????????????????????????????????????????????????????????????????
?                      DONUTERP QUICK REFERENCE CARD                          ?
????????????????????????????????????????????????????????????????????????????????
*/

// ============================================================================
// SETUP (In App.xaml.cs)
// ============================================================================

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    public App()
    {
        this.InitializeComponent();
        // 1?? Initialize services
        Services = ServiceInitializer.InitializeServices();
    }
    
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        // 2?? Initialize database
        await ServiceInitializer.InitializeDatabaseAsync(Services);
    }
}


// ============================================================================
// VIEWMODEL INJECTION
// ============================================================================

public partial class ProductionViewModel : ObservableObject
{
    private readonly IHppCalculationService _hppService;
    private readonly IFinancialAnalysisService _financeService;
    private readonly IPredictiveAnalyticsService _predictiveService;
    private readonly IAuditTrailService _auditService;
    
    public ProductionViewModel(
        IHppCalculationService hppService,
        IFinancialAnalysisService financeService,
        IPredictiveAnalyticsService predictiveService,
        IAuditTrailService auditService)
    {
        _hppService = hppService;
        _financeService = financeService;
        _predictiveService = predictiveService;
        _auditService = auditService;
    }
}

// Get from App.Services:
var vm = App.Services.GetRequiredService<ProductionViewModel>();


// ============================================================================
// HPP CALCULATION (Harga Pokok Penjualan)
// ============================================================================

// ? Calculate standard HPP for a product
var (hpp, componentCosts) = await _hppService.CalculateHppForProductAsync(productId);
Console.WriteLine($"Standard HPP: {hpp}");
foreach (var (ingredientId, cost) in componentCosts)
{
    Console.WriteLine($"  Ingredient cost: {cost}");
}

// ? Calculate actual HPP for a completed batch
var batchCost = await _hppService.CalculateBatchHppAsync(batchId);
Console.WriteLine($"Total batch cost: {batchCost.TotalManufacturingCost}");
Console.WriteLine($"HPP per good unit: {batchCost.HppPerGoodUnit}");

// ? Get ingredient allocation with waste
var allocation = await _hppService.CalculateIngredientAllocationWithWasteAsync(
    ingredientId,
    quantityNeeded: 100,
    wastePercentage: 5);
Console.WriteLine($"Material cost: {allocation.MaterialCost}");
Console.WriteLine($"Waste cost: {allocation.WasteCost}");


// ============================================================================
// UNIT CONVERSION
// ============================================================================

// ? Convert between units
double grams = await _unitService.ConvertAsync("Sak", "Gram", 1, "Weight");
// Result: 25000 (1 Sak = 25 kg = 25000 grams)

double liters = await _unitService.ConvertAsync("Botol", "Liter", 10, "Volume");
// Result: 6 (10 bottles × 0.6 liters each)

// ? Normalize to base unit
var (normalizedQty, baseUnit) = await _unitService.NormalizeToBaseUnitAsync(
    "Kilogram", 2.5, "Weight");
// Result: (2500, "Gram")

// ? Format for display
string formatted = await _unitService.FormatQuantityAsync(
    baseQuantity: 2500,
    targetUnit: "Kilogram",
    category: "Weight");
// Result: "2.50 Kilogram"


// ============================================================================
// FINANCIAL ANALYSIS
// ============================================================================

// ? Get financial dashboard
var dashboard = await _financeService.GetFinancialDashboardAsync();
Console.WriteLine($"Current cash balance: {dashboard.CurrentCashBalance}");
Console.WriteLine($"Month-to-date profit: {dashboard.MonthToDateProfit}");
Console.WriteLine($"Profit margin: {dashboard.ProfitMargin:F1}%");

// ? Calculate P&L for date range
var (revenue, cogs, profit, profitMargin, operational, netProfit, netMargin) =
    await _financeService.CalculateProfitAndLossAsync(startDate, endDate);
Console.WriteLine($"Revenue: {revenue}");
Console.WriteLine($"Net profit margin: {netMargin:F1}%");

// ? Apply monthly depreciation (auto-create journal entries)
var (totalDepreciation, affectedAssets) = 
    await _financeService.CalculateAndApplyDepreciationAsync(month);
Console.WriteLine($"Total depreciation recorded: {totalDepreciation}");

// ? Process recurring transactions (auto-create salary, rent, etc)
var (processedCount, transactionIds) = 
    await _financeService.ProcessRecurringTransactionsAsync(startDate, endDate);
Console.WriteLine($"Processed {processedCount} recurring transactions");

// ? Analyze product profitability
var profitability = await _financeService.AnalyzeProductProfitabilityAsync();
foreach (var product in profitability)
{
    Console.WriteLine($"{product.ProductName}: {product.ProfitMargin:F1}% margin");
}

// ? Generate 3-month forecast
var forecast = await _financeService.GenerateForecastAsync(forecastMonths: 3);
Console.WriteLine($"Forecasted annual revenue: {forecast.ForecastedAnnualRevenue}");


// ============================================================================
// PREDICTIVE ANALYTICS & AI
// ============================================================================

// ? Forecast stock requirement (7 days ahead)
var stockForecast = await _predictiveService.ForecastStockRequirementAsync(ingredientId);
Console.WriteLine($"Average daily usage: {stockForecast.ExpectedAverageDaily}");
Console.WriteLine($"Trend: {stockForecast.Trend}");
foreach (var daily in stockForecast.DailyForecasts)
{
    Console.WriteLine($"{daily.Date:yyyy-MM-dd}: {daily.ForecastedQuantity} " +
                      $"(±{daily.ConfidenceRangeHigh - daily.ForecastedQuantity})");
}

// ? Detect consumption anomalies (unusual patterns)
var anomalies = await _predictiveService.DetectConsumptionAnomaliesAsync(
    dateFrom, dateTo, deviationThreshold: 2.0);
foreach (var anomaly in anomalies)
{
    if (anomaly.AlertLevel == "Critical")
    {
        Console.WriteLine($"?? {anomaly.IngredientName}: {anomaly.DeviationPercent:F1}% " +
                         $"deviation. Reason: {anomaly.PossibleCause}");
    }
}

// ? Get dynamic pricing recommendation
var pricing = await _predictiveService.GetDynamicPricingRecommendationAsync(
    productId, desiredMarginPercent: 30);
Console.WriteLine($"Current price: {pricing.CurrentPrice}");
Console.WriteLine($"Recommended price: {pricing.RecommendedPrice}");
Console.WriteLine($"Recommendation: {pricing.Recommendation}");

// ? Forecast product demand (14 days)
var demandForecast = await _predictiveService.ForecastProductDemandAsync(productId);
Console.WriteLine($"Forecasted units: {demandForecast.ForecastedUnits}");
Console.WriteLine($"Confidence: {demandForecast.Confidence}");

// ? Analyze inventory turnover (fast/slow/dead stock)
var inventory = await _predictiveService.AnalyzeInventoryTurnovAsync();
Console.WriteLine($"Fast-moving items: {inventory.FastMoving.Count}");
Console.WriteLine($"Slow-moving value: {inventory.TotalSlowMovingValue}");

// ? Get procurement recommendations
var recommendations = await _predictiveService.GenerateProcurementRecommendationsAsync();
foreach (var rec in recommendations.OrderByDescending(r => r.PriorityLevel))
{
    Console.WriteLine($"[Priority {rec.PriorityLevel}] Order {rec.RecommendedQuantity} " +
                      $"{rec.Unit} of {rec.IngredientName}");
}

// ? Detect fraud patterns
var fraudAlerts = await _predictiveService.DetectFraudPatternsAsync(dateFrom, dateTo);
foreach (var alert in fraudAlerts.Where(a => a.RiskScore > 70))
{
    Console.WriteLine($"?? Risk score {alert.RiskScore}: {alert.AnomalyType}");
}


// ============================================================================
// AUDIT TRAIL & COMPLIANCE
// ============================================================================

// ? Log any data change (must do this on every modification!)
await _auditService.LogDataChangeAsync(
    entityName: "Ingredient",
    entityId: ingredientId.ToString(),
    action: "UPDATE",
    oldValues: new { Name = "Terigu", Cost = 5000m },
    newValues: new { Name = "Terigu Premium", Cost = 5500m },
    username: "admin",
    userRole: "Admin",
    ipAddress: "192.168.1.100");

// ? Log authentication event
await _auditService.LogAuthenticationEventAsync(
    username: "user123",
    isSuccessful: true,
    reason: null,
    ipAddress: "192.168.1.100");

// ? Get audit history for specific entity
var history = await _auditService.GetAuditHistoryAsync(
    entityName: "Product",
    entityId: productId.ToString(),
    fromDate: DateTime.Now.AddDays(-30));
foreach (var entry in history)
{
    Console.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm} - {entry.Username} " +
                      $"({entry.Action}): {entry.Description}");
}

// ? Detect suspicious activities
var suspicious = await _auditService.DetectSuspiciousActivitiesAsync(
    dateFrom: DateTime.Now.AddDays(-7),
    dateTo: DateTime.Now);
foreach (var activity in suspicious.Where(a => a.RiskScore > 70))
{
    Console.WriteLine($"?? Risk {activity.RiskScore}: {activity.ActivityType}");
}

// ? Generate compliance report
var compliance = await _auditService.GenerateComplianceReportAsync(
    startDate: DateTime.Now.AddMonths(-1),
    endDate: DateTime.Now);
Console.WriteLine($"Total audit logs: {compliance.TotalAuditLogEntries}");
Console.WriteLine($"Suspicious activities: {compliance.SuspiciousActivities}");
Console.WriteLine($"Overall rating: {compliance.OverallRating}");

// ? Verify data integrity (detect tampering)
var integrity = await _auditService.VerifyDataIntegrityAsync(
    entityName: "Ingredient",
    entityId: ingredientId.ToString());
if (!integrity.IsIntegrityValid)
{
    foreach (var issue in integrity.Issues)
    {
        Console.WriteLine($"?? Issue found: {issue.IssueType} - {issue.Description}");
    }
}


// ============================================================================
// ERROR HANDLING PATTERN
// ============================================================================

try
{
    var result = await _hppService.CalculateHppForProductAsync(productId);
    // Use result
}
catch (InvalidOperationException ex)
{
    // Product not found or BOM invalid
    Console.WriteLine($"Logic error: {ex.Message}");
}
catch (ArgumentException ex)
{
    // Invalid parameter
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
catch (Exception ex)
{
    // Unexpected error
    Console.WriteLine($"Unexpected error: {ex.Message}");
}


// ============================================================================
// CONFIGURATION
// ============================================================================

// In ServiceInitializer.InitializeServices():
services.AddDonutErpServices(options =>
{
    options.EnableAdvancedAnalytics = true;              // Enable AI features
    options.EnableRealTimeAudit = true;                   // Log everything
    options.EnableBomCaching = true;                      // Cache BOM calculations
    options.EnableAnomalyDetection = true;                // Flag unusual patterns
    options.UnitConversionCacheDurationMinutes = 120;    // Cache for 2 hours
    options.HppCacheDurationMinutes = 240;               // Cache for 4 hours
    options.PredictionConfidenceLevel = 95;              // 95% confidence
    options.MinHistoricalRecordsForForecast = 10;        // Need 10+ data points
    options.SuspiciousActivityRiskThreshold = 70;        // Flag if risk score > 70
});


// ============================================================================
// COMMON USE CASES
// ============================================================================

// ?? Create a dashboard for manager
async Task<ManagerDashboard> GetManagerDashboardAsync()
{
    var dashboard = await _financeService.GetFinancialDashboardAsync();
    var forecast = await _financeService.GenerateForecastAsync(3);
    var lowStock = await _predictiveService.GenerateProcurementRecommendationsAsync();
    
    return new ManagerDashboard
    {
        CurrentBalance = dashboard.CurrentCashBalance,
        MonthProfit = dashboard.MonthToDateProfit,
        ForecastedRevenue = forecast.ForecastedAnnualRevenue,
        LowStockItems = lowStock.Count(r => r.PriorityLevel >= 4)
    };
}

// ?? Production: Calculate batch cost
async Task<decimal> CalculateBatchCostAsync(Guid batchId)
{
    var batchCost = await _hppService.CalculateBatchHppAsync(batchId);
    return batchCost.TotalManufacturingCost;
}

// ?? Pricing: Suggest new prices
async Task UpdateProductPricesAsync()
{
    var products = await _dbContext.Products.ToListAsync();
    foreach (var product in products)
    {
        var recommendation = await _predictiveService
            .GetDynamicPricingRecommendationAsync(product.Id, desiredMarginPercent: 30);
        
        if (recommendation.Recommendation == "Increase")
        {
            product.SellingPrice = recommendation.RecommendedPrice;
        }
    }
    await _dbContext.SaveChangesAsync();
}

// ?? Inventory: Auto-generate purchase orders
async Task GeneratePurchaseOrdersAsync()
{
    var recommendations = await _predictiveService
        .GenerateProcurementRecommendationsAsync();
    
    foreach (var rec in recommendations.Where(r => r.PriorityLevel >= 4))
    {
        // Create purchase order...
        Console.WriteLine($"Create PO for {rec.IngredientName}: {rec.RecommendedQuantity} {rec.Unit}");
    }
}


// ============================================================================
// DATABASE QUERIES (Using DbContext)
// ============================================================================

// Get all ingredients with price history
var ingredients = await _dbContext.Ingredients
    .Include(i => i.PriceHistories)
    .ToListAsync();

// Get all production batches with costs
var batches = await _dbContext.Set<ProductionBatch>()
    .Include(b => b.Outputs)
    .ThenInclude(o => o.Product)
    .ToListAsync();

// Get audit log for specific user
var userAudit = await _dbContext.Set<ComplianceAuditLog>()
    .Where(al => al.Username == "admin" && 
                 al.Timestamp >= DateTime.Now.AddDays(-7))
    .OrderByDescending(al => al.Timestamp)
    .ToListAsync();


?????????????????????????????????????????????????????????????????????????????????
                         Happy Coding! ??
?????????????????????????????????????????????????????????????????????????????????
*/
