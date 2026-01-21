#nullable enable

namespace DonutErp.Core.Interfaces.Services
{
    /// <summary>
    /// Financial analysis and P&L calculation engine.
    /// Provides real-time profit/loss analysis with multi-wallet support.
    /// </summary>
    public interface IFinancialAnalysisService
    {
        /// <summary>
        /// Calculates real-time P&L for a specific date range.
        /// </summary>
        Task<(decimal TotalRevenue, decimal TotalCogs, decimal GrossProfit, decimal GrossProfitMargin, 
            decimal TotalOperational, decimal NetProfit, decimal NetProfitMargin)> 
        CalculateProfitAndLossAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive financial dashboard with KPIs.
        /// </summary>
        Task<FinancialDashboard> GetFinancialDashboardAsync(
            DateTime? asOfDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates monthly depreciation for all assets and creates journal entries.
        /// </summary>
        Task<(decimal TotalDepreciation, List<Guid> AffectedAssetIds)> CalculateAndApplyDepreciationAsync(
            DateTime month,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets daily cashflow analysis across all wallets.
        /// </summary>
        Task<DailyBalanceSnapshot> GetDailyBalanceSnapshotAsync(
            DateTime date,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates and reconciles wallets against actual balances.
        /// </summary>
        Task<WalletReconciliation> ReconcileWalletsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes recurring transactions for a date range (salary, rent, etc).
        /// </summary>
        Task<(int ProcessedCount, List<Guid> CreatedTransactionIds)> ProcessRecurringTransactionsAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes profit margin by product with actual HPP.
        /// </summary>
        Task<List<ProductProfitAnalysis>> AnalyzeProductProfitabilityAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets variance analysis: planned vs actual costs.
        /// </summary>
        Task<VarianceAnalysis> GetVarianceAnalysisAsync(
            DateTime month,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates financial forecast based on historical trends.
        /// </summary>
        Task<FinancialForecast> GenerateForecastAsync(
            int forecastMonths = 3,
            CancellationToken cancellationToken = default);
    }

    public record FinancialDashboard
    {
        public decimal CurrentCashBalance { get; init; }
        public decimal CurrentAssetValue { get; init; }
        public decimal TotalLiabilities { get; init; }
        public decimal EquityValue => CurrentCashBalance + CurrentAssetValue - TotalLiabilities;
        public decimal MonthToDateRevenue { get; init; }
        public decimal MonthToDateCogs { get; init; }
        public decimal MonthToDateProfit { get; init; }
        public decimal ProfitMargin => MonthToDateRevenue > 0 ? (MonthToDateProfit / MonthToDateRevenue) * 100 : 0;
        public List<WalletBalance> WalletBalances { get; init; } = new();
        public DateTime GeneratedAt { get; init; }
    }

    public record WalletBalance
    {
        public Guid WalletId { get; init; }
        public string WalletName { get; init; } = string.Empty;
        public decimal Balance { get; init; }
        public string Type { get; init; } = string.Empty;
    }

    public record DailyBalanceSnapshot
    {
        public DateTime Date { get; init; }
        public decimal CashBalance { get; init; }
        public decimal BankBalance { get; init; }
        public decimal EWalletBalance { get; init; }
        public decimal TotalBalance => CashBalance + BankBalance + EWalletBalance;
        public decimal DailyInflow { get; init; }
        public decimal DailyOutflow { get; init; }
    }

    public record WalletReconciliation
    {
        public DateTime ReconciliationDate { get; init; }
        public List<WalletVariance> Variances { get; init; } = new();
        public bool IsBalanced => Variances.All(v => v.Variance == 0);
    }

    public record WalletVariance
    {
        public Guid WalletId { get; init; }
        public string WalletName { get; init; } = string.Empty;
        public decimal SystemBalance { get; init; }
        public decimal ActualBalance { get; init; }
        public decimal Variance => ActualBalance - SystemBalance;
    }

    public record ProductProfitAnalysis
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int UnitsSold { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal TotalCogs { get; init; }
        public decimal GrossProfit { get; init; }
        public decimal ProfitMargin => TotalRevenue > 0 ? (GrossProfit / TotalRevenue) * 100 : 0;
        public decimal AverageSellingPrice => UnitsSold > 0 ? TotalRevenue / UnitsSold : 0;
        public decimal AverageCost => UnitsSold > 0 ? TotalCogs / UnitsSold : 0;
    }

    public record VarianceAnalysis
    {
        public DateTime Month { get; init; }
        public decimal PlannedRevenue { get; init; }
        public decimal ActualRevenue { get; init; }
        public decimal RevenueVariance => ActualRevenue - PlannedRevenue;
        public decimal PlannedCogs { get; init; }
        public decimal ActualCogs { get; init; }
        public decimal CogsVariance => ActualCogs - PlannedCogs;
        public List<string> Insights { get; init; } = new();
    }

    public record FinancialForecast
    {
        public DateTime ForecastStartDate { get; init; }
        public int ForecastMonths { get; init; }
        public List<ForecastMonthData> MonthlyForecasts { get; init; } = new();
        public decimal ForecastedAnnualRevenue { get; init; }
        public decimal ForecastedNetProfitMargin { get; init; }
        public DateTime GeneratedAt { get; init; }
    }

    public record ForecastMonthData
    {
        public DateTime Month { get; init; }
        public decimal ForecastedRevenue { get; init; }
        public decimal ForecastedCogs { get; init; }
        public decimal ForecastedOperational { get; init; }
        public decimal ForecastedNetProfit { get; init; }
        public string Confidence { get; init; } = "Medium"; // Low, Medium, High
    }
}
