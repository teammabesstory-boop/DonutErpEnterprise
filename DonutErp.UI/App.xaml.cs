using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;
using DonutErp.Infrastructure.Services.Implements;
using DonutErp.UI.ViewModels.Inventory;
using DonutErp.UI.ViewModels.Production;
using DonutErp.UI.ViewModels.Finance;
using DonutErp.UI.ViewModels.POS;

namespace DonutErp.UI
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }
        public Window? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=donuterp.db"));

            // 2. Services
            services.AddTransient<IDatabaseSeeder, DbSeeder>();
            services.AddTransient<IInventoryService, InventoryService>();
            services.AddTransient<IProductionService, ProductionService>();
            services.AddTransient<IFinanceService, FinanceService>();

            // 3. ViewModels
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<ProductionViewModel>();
            services.AddTransient<FinanceViewModel>();
            services.AddTransient<PosViewModel>();
        }

        private async Task InitializeDatabaseAsync()
        {
            var scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
                    await seeder.SeedInitialDataAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DATABASE INIT FAILED: {ex.Message}");
                }
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // 1. BUKA JENDELA DULUAN (Supaya gak kena Timeout Windows)
            MainWindow = new MainWindow();
            MainWindow.Activate();

            // 2. BARU LOAD DATABASE DI BACKGROUND
            // Jendela akan tampil, mungkin kosong sebentar, lalu data masuk.
            await InitializeDatabaseAsync();
        }
    }
}