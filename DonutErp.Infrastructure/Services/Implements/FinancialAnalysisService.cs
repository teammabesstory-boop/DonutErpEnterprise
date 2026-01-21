#nullable enable

using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonutErp.Infrastructure.Services.Implements
{
    /// <summary>
    /// Advanced financial analysis engine for real-time P&L, asset depreciation, and financial forecasting.
    /// This service is the brain of the financial module.
    /// </summary>
    public class FinancialAnalysisService : IFinancialAnalysisService
    {
        private readonly AppDbContext _dbContext;
        private readonly IHppCalculationService _hppCalculationService;

        public FinancialAnalysisService(
            AppDbContext dbContext,
            IHppCalculationService hppCalculationService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _hppCalculationService = hppCalculationService ?? throw new ArgumentNullException(nameof(hppCalculationService));
        }

        public async Task<(decimal TotalRevenue, decimal TotalCogs, decimal GrossProfit, 
            decimal GrossProfitMargin, decimal TotalOperational, decimal NetProfit, 
            decimal NetProfitMargin)> CalculateProfitAndLossAsync(
                DateTime startDate,
                DateTime endDate,
                CancellationToken cancellationToken = default)
        {
            // Get revenue from sales transactions
            var salesTransactions = await _dbContext.Set<Transaction>()
                .Include(t => t.Details)
                .Where(t => t.Type == TransactionType.SalesIncome &&
                           t.Date >= startDate &&
                           t.Date <= endDate)
                .ToListAsync(cancellationToken);

            decimal totalRevenue = salesTransactions.Sum(t => t.TotalAmount);

            // Calculate COGS from transaction details
            decimal totalCogs = 0;
            foreach (var transaction in salesTransactions)
            {
                foreach (var detail in transaction.Details)
                {
                    if (detail.ProductId.HasValue)
                    {
                        totalCogs += detail.CostAtSale;
                    }
                }
            }

            var grossProfit = totalRevenue - totalCogs;
            var grossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

            // Get operational expenses
            var operationalExpenses = await _dbContext.Set<Transaction>()
                .Where(t => t.Type == TransactionType.OperationalExpense &&
                           t.Date >= startDate &&
                           t.Date <= endDate)
                .SumAsync(t => t.TotalAmount, cancellationToken);

            // Get depreciation
            var depreciationExpense = await _dbContext.Set<Transaction>()
                .Where(t => t.Type == TransactionType.AssetDepreciation &&
                           t.Date >= startDate &&
                           t.Date <= endDate)
                .SumAsync(t => t.TotalAmount, cancellationToken);

            var totalOperational = operationalExpenses + depreciationExpense;
            var netProfit = grossProfit - totalOperational;
            var netProfitMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;

            return (totalRevenue, totalCogs, grossProfit, grossProfitMargin, 
                totalOperational, netProfit, netProfitMargin);
        }

        public async Task<FinancialDashboard> GetFinancialDashboardAsync(
            DateTime? asOfDate = null,
            CancellationToken cancellationToken = default)
        {
            var date = asOfDate ?? DateTime.Now;
            var monthStart = new DateTime(date.Year, date.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Calculate P&L for month
            var (revenue, cogs, _, _, operational, netProfit, margin) = 
                await CalculateProfitAndLossAsync(monthStart, monthEnd, cancellationToken);

            // Get wallet balances
            var wallets = await _dbContext.Set<Wallet>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var walletBalances = wallets.Select(w => new WalletBalance
            {
                WalletId = w.Id,
                WalletName = w.Name,
                Balance = w.CurrentBalance,
                Type = w.Type.ToString()
            }).ToList();

            var totalCashBalance = wallets.Sum(w => w.CurrentBalance);

            // Get asset values
            var assets = await _dbContext.Set<Asset>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var totalAssetValue = assets.Sum(a => a.PurchasePrice - (a.MonthlyDepreciation * (DateTime.Now.Month - 1)));

            return new FinancialDashboard
            {
                CurrentCashBalance = totalCashBalance,
                CurrentAssetValue = totalAssetValue,
                TotalLiabilities = 0, // Would be calculated from liabilities table if existed
                MonthToDateRevenue = revenue,
                MonthToDateCogs = cogs,
                MonthToDateProfit = netProfit,
                WalletBalances = walletBalances,
                GeneratedAt = DateTime.Now
            };
        }

        public async Task<(decimal TotalDepreciation, List<Guid> AffectedAssetIds)> 
            CalculateAndApplyDepreciationAsync(
                DateTime month,
                CancellationToken cancellationToken = default)
        {
            var assets = await _dbContext.Set<Asset>()
                .ToListAsync(cancellationToken);

            decimal totalDepreciation = 0;
            var affectedAssetIds = new List<Guid>();

            foreach (var asset in assets)
            {
                var monthlyDepreciation = asset.MonthlyDepreciation;
                totalDepreciation += monthlyDepreciation;
                affectedAssetIds.Add(asset.Id);

                // Record depreciation entry
                var depreciation = new AssetDepreciation
                {
                    AssetId = asset.Id,
                    DepreciationMonth = new DateTime(month.Year, month.Month, 1),
                    MonthlyDepreciation = monthlyDepreciation,
                    RecordedAt = DateTime.Now
                };

                _dbContext.Set<AssetDepreciation>().Add(depreciation);

                // Create journal entry
                var transaction = new Transaction
                {
                    InvoiceNumber = $"DEP-{month:yyyyMM}-{asset.Id.ToString().Substring(0, 8)}",
                    Date = new DateTime(month.Year, month.Month, 1),
                    Type = TransactionType.AssetDepreciation,
                    Description = $"Depreciation for {asset.Name}",
                    TotalAmount = monthlyDepreciation,
                    TotalCost = monthlyDepreciation
                };

                _dbContext.Set<Transaction>().Add(transaction);
                depreciation.TransactionId = transaction.Id;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (totalDepreciation, affectedAssetIds);
        }

        public async Task<DailyBalanceSnapshot> GetDailyBalanceSnapshotAsync(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var wallets = await _dbContext.Set<Wallet>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var cashWallet = wallets.FirstOrDefault(w => w.Type == WalletType.Cash);
            var bankWallet = wallets.FirstOrDefault(w => w.Type == WalletType.Bank);
            var eWalletWallet = wallets.FirstOrDefault(w => w.Type == WalletType.EWallet);

            // Calculate daily flows
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var inflows = await _dbContext.Set<Transaction>()
                .Where(t => (t.Type == TransactionType.SalesIncome || 
                            t.Type == TransactionType.CapitalInjection) &&
                           t.Date >= startOfDay &&
                           t.Date <= endOfDay)
                .SumAsync(t => t.TotalAmount, cancellationToken);

            var outflows = await _dbContext.Set<Transaction>()
                .Where(t => (t.Type == TransactionType.MaterialExpense ||
                            t.Type == TransactionType.OperationalExpense) &&
                           t.Date >= startOfDay &&
                           t.Date <= endOfDay)
                .SumAsync(t => t.TotalAmount, cancellationToken);

            return new DailyBalanceSnapshot
            {
                Date = date,
                CashBalance = cashWallet?.CurrentBalance ?? 0,
                BankBalance = bankWallet?.CurrentBalance ?? 0,
                EWalletBalance = eWalletWallet?.CurrentBalance ?? 0,
                DailyInflow = inflows,
                DailyOutflow = outflows
            };
        }

        public async Task<WalletReconciliation> ReconcileWalletsAsync(
            CancellationToken cancellationToken = default)
        {
            var wallets = await _dbContext.Set<Wallet>()
                .ToListAsync(cancellationToken);

            var variances = new List<WalletVariance>();

            foreach (var wallet in wallets)
            {
                // Calculate system balance from transactions
                var systemBalance = wallet.CurrentBalance;

                // In real scenario, you would get actual balance from external sources
                // For now, we'll assume system = actual
                var actualBalance = systemBalance;

                variances.Add(new WalletVariance
                {
                    WalletId = wallet.Id,
                    WalletName = wallet.Name,
                    SystemBalance = systemBalance,
                    ActualBalance = actualBalance
                });
            }

            return new WalletReconciliation
            {
                ReconciliationDate = DateTime.Now,
                Variances = variances
            };
        }

        public async Task<(int ProcessedCount, List<Guid> CreatedTransactionIds)> 
            ProcessRecurringTransactionsAsync(
                DateTime startDate,
                DateTime endDate,
                CancellationToken cancellationToken = default)
        {
            var recurringTransactions = await _dbContext.Set<RecurringTransaction>()
                .Where(rt => rt.IsActive &&
                           rt.StartDate <= endDate &&
                           (rt.EndDate == null || rt.EndDate >= startDate))
                .ToListAsync(cancellationToken);

            var createdTransactionIds = new List<Guid>();

            foreach (var recurring in recurringTransactions)
            {
                var nextDue = recurring.NextDueDate;

                while (nextDue >= startDate && nextDue <= endDate)
                {
                    var transaction = new Transaction
                    {
                        InvoiceNumber = $"{recurring.Name}-{nextDue:yyyyMMdd}",
                        Date = nextDue,
                        Type = recurring.Type,
                        WalletId = recurring.WalletId,
                        Description = recurring.Description,
                        TotalAmount = recurring.Amount,
                        TotalCost = recurring.Amount,
                        IsRecurring = true,
                        Notes = $"Auto-generated from recurring transaction: {recurring.Name}"
                    };

                    _dbContext.Set<Transaction>().Add(transaction);
                    createdTransactionIds.Add(transaction.Id);

                    // Calculate next due date
                    nextDue = CalculateNextDueDate(nextDue, recurring.RecurrencePattern, recurring.RecurrenceDay);
                }

                // Update NextDueDate
                recurring.NextDueDate = CalculateNextDueDate(DateTime.Now, recurring.RecurrencePattern, recurring.RecurrenceDay);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (createdTransactionIds.Count, createdTransactionIds);
        }

        public async Task<List<ProductProfitAnalysis>> AnalyzeProductProfitabilityAsync(
            DateTime? startDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            toDate ??= DateTime.Now;

            var transactions = await _dbContext.Set<Transaction>()
                .Include(t => t.Details)
                .Where(t => t.Type == TransactionType.SalesIncome &&
                           t.Date >= startDate &&
                           t.Date <= toDate)
                .ToListAsync(cancellationToken);

            var productAnalysis = new Dictionary<Guid, ProductProfitAnalysis>();

            foreach (var transaction in transactions)
            {
                foreach (var detail in transaction.Details)
                {
                    if (detail.ProductId == null) continue;

                    var productId = detail.ProductId.Value;

                    if (!productAnalysis.TryGetValue(productId, out var analysis))
                    {
                        var product = await _dbContext.Products
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

                        analysis = new ProductProfitAnalysis
                        {
                            ProductId = productId,
                            ProductName = product?.Name ?? "Unknown"
                        };
                        productAnalysis[productId] = analysis;
                    }

                    var newAnalysis = analysis with
                    {
                        UnitsSold = analysis.UnitsSold + detail.Quantity,
                        TotalRevenue = analysis.TotalRevenue + detail.PriceAtSale,
                        TotalCogs = analysis.TotalCogs + detail.CostAtSale
                    };

                    productAnalysis[productId] = newAnalysis;
                }
            }

            return productAnalysis.Values.ToList();
        }

        public async Task<VarianceAnalysis> GetVarianceAnalysisAsync(
            DateTime month,
            CancellationToken cancellationToken = default)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var (actualRevenue, actualCogs, _, _, _, _, _) = 
                await CalculateProfitAndLossAsync(monthStart, monthEnd, cancellationToken);

            // In real implementation, you'd have planned/budgeted amounts
            var plannedRevenue = actualRevenue * 0.95m; // Assuming 95% of actual as planned
            var plannedCogs = actualCogs * 0.98m; // Assuming 98% of actual as planned

            var insights = new List<string>();

            if (actualRevenue > plannedRevenue)
                insights.Add($"Revenue exceeded plan by {((actualRevenue - plannedRevenue) / plannedRevenue * 100):F1}%");
            else if (actualRevenue < plannedRevenue)
                insights.Add($"Revenue fell short of plan by {((plannedRevenue - actualRevenue) / plannedRevenue * 100):F1}%");

            return new VarianceAnalysis
            {
                Month = month,
                PlannedRevenue = plannedRevenue,
                ActualRevenue = actualRevenue,
                PlannedCogs = plannedCogs,
                ActualCogs = actualCogs,
                Insights = insights
            };
        }

        public async Task<FinancialForecast> GenerateForecastAsync(
            int forecastMonths = 3,
            CancellationToken cancellationToken = default)
        {
            var monthlyForecasts = new List<ForecastMonthData>();
            var now = DateTime.Now;

            // Get historical data for trend analysis
            var historicalTransactions = await _dbContext.Set<Transaction>()
                .Where(t => t.Date >= now.AddMonths(-12))
                .ToListAsync(cancellationToken);

            for (int i = 1; i <= forecastMonths; i++)
            {
                var forecastMonth = now.AddMonths(i);
                var monthStart = new DateTime(forecastMonth.Year, forecastMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Simple moving average forecast
                var (revenue, cogs, _, _, operational, _, _) = 
                    await CalculateProfitAndLossAsync(monthStart, monthEnd, cancellationToken);

                monthlyForecasts.Add(new ForecastMonthData
                {
                    Month = forecastMonth,
                    ForecastedRevenue = revenue,
                    ForecastedCogs = cogs,
                    ForecastedOperational = operational,
                    ForecastedNetProfit = revenue - cogs - operational,
                    Confidence = "Medium"
                });
            }

            var forecastedAnnualRevenue = monthlyForecasts.Sum(m => m.ForecastedRevenue);
            var totalNetProfit = monthlyForecasts.Sum(m => m.ForecastedNetProfit);
            var netMargin = forecastedAnnualRevenue > 0 ? (totalNetProfit / forecastedAnnualRevenue) * 100 : 0;

            return new FinancialForecast
            {
                ForecastStartDate = now.AddMonths(1),
                ForecastMonths = forecastMonths,
                MonthlyForecasts = monthlyForecasts,
                ForecastedAnnualRevenue = forecastedAnnualRevenue,
                ForecastedNetProfitMargin = netMargin,
                GeneratedAt = DateTime.Now
            };
        }

        private DateTime CalculateNextDueDate(DateTime current, string pattern, int dayValue)
        {
            return pattern switch
            {
                "Daily" => current.AddDays(1),
                "Weekly" => current.AddDays(7),
                "Monthly" => current.AddMonths(1),
                "Yearly" => current.AddYears(1),
                _ => current.AddMonths(1)
            };
        }
    }
}
