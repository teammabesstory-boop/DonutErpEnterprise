#nullable enable

namespace DonutErp.Core.Interfaces.Services
{
    /// <summary>
    /// Advanced predictive analytics for inventory and dynamic pricing.
    /// Includes stock forecasting, anomaly detection, and pricing recommendations.
    /// </summary>
    public interface IPredictiveAnalyticsService
    {
        /// <summary>
        /// Forecasts stock requirements based on historical sales trends.
        /// Uses moving average, exponential smoothing, and seasonal analysis.
        /// </summary>
        Task<StockForecast> ForecastStockRequirementAsync(
            Guid ingredientId,
            int forecastDaysAhead = 7,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects anomalies in ingredient consumption patterns.
        /// Alerts if usage is significantly different from baseline.
        /// </summary>
        Task<List<AnomalyAlert>> DetectConsumptionAnomaliesAsync(
            DateTime dateFrom,
            DateTime dateTo,
            double deviationThreshold = 2.0, // Standard deviations
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Recommends selling price based on desired margin and cost trends.
        /// </summary>
        Task<PricingRecommendation> GetDynamicPricingRecommendationAsync(
            Guid productId,
            decimal desiredMarginPercent = 30,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes product demand patterns and forecast sales.
        /// </summary>
        Task<DemandForecast> ForecastProductDemandAsync(
            Guid productId,
            int forecastDaysAhead = 14,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Identifies slow-moving and fast-moving inventory items.
        /// </summary>
        Task<InventoryAnalysis> AnalyzeInventoryTurnovAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates procurement recommendations to optimize inventory.
        /// </summary>
        Task<List<ProcurementRecommendation>> GenerateProcurementRecommendationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes price trends for ingredients to optimize purchasing.
        /// </summary>
        Task<PriceTrendAnalysis> AnalyzePriceTrendAsync(
            Guid ingredientId,
            int monthsToAnalyze = 6,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides margin health analysis for products.
        /// </summary>
        Task<List<MarginHealthReport>> AnalyzeProductMarginHealthAsync(
            decimal minMarginThresholdPercent = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects unusual transactions that might indicate fraud or errors.
        /// </summary>
        Task<List<FraudAlertItem>> DetectFraudPatternsAsync(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Trains model with latest data to improve forecasts.
        /// Should be called periodically (weekly or monthly).
        /// </summary>
        Task<ModelTrainingResult> TrainPredictiveModelsAsync(
            CancellationToken cancellationToken = default);
    }

    // ============ VALUE OBJECTS & DTOs ============

    public record StockForecast
    {
        public Guid IngredientId { get; init; }
        public string IngredientName { get; init; } = string.Empty;
        public DateTime ForecastDate { get; init; }
        public List<DailyForecast> DailyForecasts { get; init; } = new();
        public double ExpectedAverageDaily { get; init; }
        public double ConfidenceInterval { get; init; } // 95% confidence +/- this value
        public string Trend { get; init; } = "Stable"; // Increasing, Decreasing, Stable, Seasonal
        public DateTime GeneratedAt { get; init; }
        public int AccuracyScore { get; init; } // 0-100, based on historical accuracy
    }

    public record DailyForecast
    {
        public DateTime Date { get; init; }
        public double ForecastedQuantity { get; init; }
        public double ConfidenceRangeHigh { get; init; }
        public double ConfidenceRangeLow { get; init; }
        public string Recommendation { get; init; } = string.Empty;
    }

    public record AnomalyAlert
    {
        public Guid IngredientId { get; init; }
        public string IngredientName { get; init; } = string.Empty;
        public DateTime AnomalyDate { get; init; }
        public double ActualConsumption { get; init; }
        public double ExpectedConsumption { get; init; }
        public double DeviationPercent { get; init; }
        public int DeviationStandardDevs { get; init; }
        public string AlertLevel { get; init; } = "Medium"; // Low, Medium, High, Critical
        public string PossibleCause { get; init; } = string.Empty;
        public string RecommendedAction { get; init; } = string.Empty;
    }

    public record PricingRecommendation
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal CurrentPrice { get; init; }
        public decimal RecommendedPrice { get; init; }
        public decimal CurrentMargin { get; init; }
        public decimal ProjectedMargin { get; init; }
        public decimal TargetMargin { get; init; }
        public string Recommendation { get; init; } = string.Empty; // "Increase", "Decrease", "Maintain"
        public string ReasonsForChange { get; init; } = string.Empty;
        public decimal EstimatedDemandImpact { get; init; } // Percentage change in demand
        public decimal EstimatedRevenueImpact { get; init; }
        public DateTime ValidUntil { get; init; }
    }

    public record DemandForecast
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public DateTime ForecastPeriodStart { get; init; }
        public DateTime ForecastPeriodEnd { get; init; }
        public int ForecastedUnits { get; init; }
        public decimal ForecastedRevenue { get; init; }
        public List<(DateTime Date, int ForecastedQty)> DailyBreakdown { get; init; } = new();
        public string Confidence { get; init; } = "Medium"; // Low, Medium, High
        public List<string> InfluencingFactors { get; init; } = new();
    }

    public record InventoryAnalysis
    {
        public List<InventoryItem> FastMoving { get; init; } = new();
        public List<InventoryItem> SlowMoving { get; init; } = new();
        public List<InventoryItem> DeadStock { get; init; } = new();
        public double AverageTurnoverDays { get; init; }
        public decimal TotalSlowMovingValue { get; init; }
        public string OverallHealthStatus { get; init; } = "Good"; // Good, Fair, Poor
    }

    public record InventoryItem
    {
        public Guid IngredientId { get; init; }
        public string IngredientName { get; init; } = string.Empty;
        public double CurrentStock { get; init; }
        public double MonthlyConsumption { get; init; }
        public int TurnoverDays { get; init; } // Days until stock runs out at current usage
        public decimal InventoryValue { get; init; }
    }

    public record ProcurementRecommendation
    {
        public Guid IngredientId { get; init; }
        public string IngredientName { get; init; } = string.Empty;
        public double RecommendedQuantity { get; init; }
        public string Unit { get; init; } = string.Empty;
        public DateTime RecommendedOrderDate { get; init; }
        public DateTime RecommendedDeliveryDate { get; init; }
        public int PriorityLevel { get; init; } // 1-5, 5 = highest priority
        public string Reason { get; init; } = string.Empty;
        public decimal EstimatedCost { get; init; }
        public double ReorderPoint { get; init; }
    }

    public record PriceTrendAnalysis
    {
        public Guid IngredientId { get; init; }
        public string IngredientName { get; init; } = string.Empty;
        public decimal AveragePriceThisMonth { get; init; }
        public decimal AveragePriceLastMonth { get; init; }
        public decimal PriceChangePercent { get; init; }
        public List<(DateTime Date, decimal Price)> PriceTrend { get; init; } = new();
        public string Trend { get; init; } = "Stable"; // Upward, Downward, Stable, Volatile
        public decimal ProjectedPriceNextMonth { get; init; }
        public string Recommendation { get; init; } = string.Empty;
    }

    public record MarginHealthReport
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal CurrentMarginPercent { get; init; }
        public decimal TargetMarginPercent { get; init; }
        public decimal SellingPrice { get; init; }
        public decimal CurrentHpp { get; init; }
        public bool IsMarginHealthy => CurrentMarginPercent >= TargetMarginPercent;
        public decimal CostIncreasePercent { get; init; }
        public string HealthStatus { get; init; } = "Healthy"; // Healthy, AtRisk, Critical
        public string ActionRequired { get; init; } = string.Empty;
    }

    public record FraudAlertItem
    {
        public Guid TransactionId { get; init; }
        public string TransactionType { get; init; } = string.Empty;
        public DateTime TransactionDate { get; init; }
        public decimal Amount { get; init; }
        public string AnomalyType { get; init; } = string.Empty; // "Unusual Amount", "Wrong User", etc
        public int RiskScore { get; init; } // 0-100
        public string RecommendedAction { get; init; } = string.Empty;
    }

    public record ModelTrainingResult
    {
        public DateTime TrainedAt { get; init; }
        public int RecordsProcessed { get; init; }
        public double ModelAccuracy { get; init; }
        public List<string> MetricsImproved { get; init; } = new();
        public List<string> Warnings { get; init; } = new();
        public bool IsSuccessful => ModelAccuracy >= 0.7; // 70% minimum accuracy
    }
}
