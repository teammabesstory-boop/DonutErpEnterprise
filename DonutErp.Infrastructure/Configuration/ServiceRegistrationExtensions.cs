#nullable enable

using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Services.Implements;
using Microsoft.Extensions.DependencyInjection;

namespace DonutErp.Infrastructure.Configuration
{
    /// <summary>
    /// Service registration extension for dependency injection.
    /// Call this from App.xaml.cs to register all services.
    /// </summary>
    public static class ServiceRegistrationExtensions
    {
        /// <summary>
        /// Registers all DonutErp services with the DI container.
        /// Must be called during application startup.
        /// </summary>
        public static IServiceCollection AddDonutErpServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(nameof(services));

            // ==========================================
            // CORE BUSINESS LOGIC SERVICES
            // ==========================================

            // HPP & Production Cost Calculation Engine
            services.AddScoped<IHppCalculationService, HppCalculationService>();

            // Unit Conversion Service with Caching
            services.AddScoped<IUnitConversionService, UnitConversionService>();

            // Financial Analysis & P&L Engine
            services.AddScoped<IFinancialAnalysisService, FinancialAnalysisService>();

            // Predictive Analytics (Stock Forecasting, Anomaly Detection, Dynamic Pricing)
            services.AddScoped<IPredictiveAnalyticsService, PredictiveAnalyticsService>();

            // Audit Trail & Compliance Logging
            services.AddScoped<IAuditTrailService, AuditTrailService>();

            // ==========================================
            // EXISTING SERVICES (Compatibility)
            // ==========================================

            // Register any existing services that haven't been replaced
            // Example: services.AddScoped<IInventoryService, InventoryService>();
            // Example: services.AddScoped<IProductionService, ProductionService>();
            // Example: services.AddScoped<IFinanceService, FinanceService>();

            return services;
        }

        /// <summary>
        /// Registers services with custom options for advanced scenarios.
        /// </summary>
        public static IServiceCollection AddDonutErpServices(
            this IServiceCollection services,
            Action<DonutErpServiceOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(nameof(services));
            ArgumentNullException.ThrowIfNull(nameof(configureOptions));

            var options = new DonutErpServiceOptions();
            configureOptions(options);

            // Add services with configuration
            services.AddScoped(sp => options);
            services.AddDonutErpServices();

            // Apply advanced configurations if needed
            if (options.EnableAdvancedAnalytics)
            {
                // Additional analytics setup could go here
            }

            if (options.EnableRealTimeAudit)
            {
                // Real-time audit configuration
            }

            return services;
        }
    }

    /// <summary>
    /// Configuration options for DonutErp services.
    /// </summary>
    public class DonutErpServiceOptions
    {
        /// <summary>
        /// Enable advanced predictive analytics features.
        /// Default: true
        /// </summary>
        public bool EnableAdvancedAnalytics { get; set; } = true;

        /// <summary>
        /// Enable real-time audit trail logging.
        /// Default: true
        /// </summary>
        public bool EnableRealTimeAudit { get; set; } = true;

        /// <summary>
        /// Enable automatic BOM caching for performance.
        /// Default: true
        /// </summary>
        public bool EnableBomCaching { get; set; } = true;

        /// <summary>
        /// Enable anomaly detection in consumption patterns.
        /// Default: true
        /// </summary>
        public bool EnableAnomalyDetection { get; set; } = true;

        /// <summary>
        /// Cache duration for unit conversion rules (in minutes).
        /// Default: 60
        /// </summary>
        public int UnitConversionCacheDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Cache duration for HPP calculations (in minutes).
        /// Default: 120
        /// </summary>
        public int HppCacheDurationMinutes { get; set; } = 120;

        /// <summary>
        /// Precision for stock forecast confidence intervals.
        /// Default: 95 (95% confidence)
        /// </summary>
        public double PredictionConfidenceLevel { get; set; } = 95;

        /// <summary>
        /// Minimum number of historical records for reliable forecasting.
        /// Default: 10
        /// </summary>
        public int MinHistoricalRecordsForForecast { get; set; } = 10;

        /// <summary>
        /// Enable automatic depreciation calculation and journaling.
        /// Default: true
        /// </summary>
        public bool EnableAutoDepreciation { get; set; } = true;

        /// <summary>
        /// Enable suspicious activity detection.
        /// Default: true
        /// </summary>
        public bool EnableSuspiciousActivityDetection { get; set; } = true;

        /// <summary>
        /// Risk score threshold for triggering alerts.
        /// Default: 70
        /// </summary>
        public int SuspiciousActivityRiskThreshold { get; set; } = 70;
    }
}
