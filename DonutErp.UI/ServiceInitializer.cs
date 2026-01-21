#nullable enable

using System;
using System.Threading.Tasks;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Configuration;
using DonutErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace DonutErp.UI
{
    /// <summary>
    /// Service initialization helper for XAML UI applications.
    /// Use this in App.xaml.cs to properly set up dependency injection.
    /// </summary>
    public static class ServiceInitializer
    {
        /// <summary>
        /// Initializes all DonutErp services for the application.
        /// Call this in App.xaml.cs constructor or OnLaunched method.
        /// </summary>
        /// <example>
        /// In App.xaml.cs:
        /// public partial class App : Application
        /// {
        ///     public IServiceProvider Services { get; private set; }
        ///
        ///     public App()
        ///     {
        ///         this.InitializeComponent();
        ///         Services = ServiceInitializer.InitializeServices();
        ///     }
        /// }
        /// </example>
        public static IServiceProvider InitializeServices()
        {
            var services = new ServiceCollection();

            // ==========================================
            // 1. DATABASE CONFIGURATION
            // ==========================================
            services.AddDbContext<AppDbContext>(options =>
            {
                // Use SQL Server in production
                // options.UseSqlServer("Server=localhost;Database=DonutErp;Trusted_Connection=true;");

                // Use SQLite for development/offline-first
                options.UseSqlite("Data Source=donuterp.db");

                // Enable logging for development
                #if DEBUG
                options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
                #endif
            });

            // ==========================================
            // 2. CORE BUSINESS SERVICES
            // ==========================================
            services.AddDonutErpServices(options =>
            {
                options.EnableAdvancedAnalytics = true;
                options.EnableRealTimeAudit = true;
                options.EnableBomCaching = true;
                options.EnableAnomalyDetection = true;
                options.EnableAutoDepreciation = true;
                options.EnableSuspiciousActivityDetection = true;
                
                // Customize options as needed
                options.UnitConversionCacheDurationMinutes = 120;
                options.HppCacheDurationMinutes = 240;
                options.SuspiciousActivityRiskThreshold = 65;
            });

            // ==========================================
            // 3. EXISTING VIEWMODEL SERVICES (if any)
            // ==========================================
            // Register any view models and UI services here
            // Example: services.AddScoped<InventoryViewModel>();
            // Example: services.AddScoped<ProductionViewModel>();

            // ==========================================
            // 4. BUILD SERVICE PROVIDER
            // ==========================================
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Initializes the database with seed data on first run.
        /// Call this after InitializeServices().
        /// </summary>
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    // Create database if it doesn't exist
                    await dbContext.Database.MigrateAsync();
                    
                    System.Diagnostics.Debug.WriteLine("? Database initialized successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Database initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates that all required services are properly registered.
        /// Call this in development to ensure DI is configured correctly.
        /// </summary>
        public static bool ValidateServices(IServiceProvider serviceProvider)
        {
            var requiredServices = new[]
            {
                typeof(IHppCalculationService),
                typeof(IUnitConversionService),
                typeof(IFinancialAnalysisService),
                typeof(IPredictiveAnalyticsService),
                typeof(IAuditTrailService),
                typeof(AppDbContext)
            };

            foreach (var serviceType in requiredServices)
            {
                var service = serviceProvider.GetService(serviceType);
                if (service == null)
                {
                    System.Diagnostics.Debug.WriteLine($"? Missing service: {serviceType.Name}");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine($"? Service registered: {serviceType.Name}");
            }

            return true;
        }
    }

    /// <summary>
    /// Extension methods for IServiceProvider to simplify service access.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        public static IHppCalculationService GetHppCalculationService(this IServiceProvider provider)
            => provider.GetRequiredService<IHppCalculationService>();

        public static IUnitConversionService GetUnitConversionService(this IServiceProvider provider)
            => provider.GetRequiredService<IUnitConversionService>();

        public static IFinancialAnalysisService GetFinancialAnalysisService(this IServiceProvider provider)
            => provider.GetRequiredService<IFinancialAnalysisService>();

        public static IPredictiveAnalyticsService GetPredictiveAnalyticsService(this IServiceProvider provider)
            => provider.GetRequiredService<IPredictiveAnalyticsService>();

        public static IAuditTrailService GetAuditTrailService(this IServiceProvider provider)
            => provider.GetRequiredService<IAuditTrailService>();

        public static AppDbContext GetDbContext(this IServiceProvider provider)
            => provider.GetRequiredService<AppDbContext>();
    }
}
