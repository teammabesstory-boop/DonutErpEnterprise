using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;
using DonutErp.Infrastructure.Services.Implements;

namespace DonutErp.UI
{
    public partial class App : Application
    {
        // THE SERVICE LOCATOR
        // Properti ini memungkinkan seluruh halaman UI mengakses Logic tanpa tahu detailnya.
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; private set; }

        private Window m_window;
        public Window MainWindow => m_window;

        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Titik masuk utama aplikasi.
        /// </summary>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // 1. KONFIGURASI SERVICES (DEPENDENCY INJECTION)
            Services = ConfigureServices();

            // 2. DATABASE INITIALIZATION (AUTO-MIGRATE & SEED)
            // Kita jalankan ini setiap start agar database selalu sinkron dengan kode.
            await InitializeDatabaseAsync();

            // 3. BUKA JENDELA UTAMA
            m_window = new MainWindow();

            // Kita bisa inject ViewModel ke MainWindow di sini nanti jika perlu
            // var mainViewModel = Services.GetRequiredService<MainViewModel>();

            m_window.Activate();
        }

        /// <summary>
        /// Mendaftarkan semua "Onderdil" aplikasi ke dalam Container.
        /// </summary>
        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // A. DATABASE CONNECTION (SQLite Local)
            // File database akan muncul di folder bin/Debug/net10.../donuterp.db
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite("Data Source=donuterp.db");
            });

            // B. REGISTER CORE SERVICES (Logic Bisnis)
            // Transient: Dibuat baru setiap kali diminta (Hemat memori)
            // Scoped: Dibuat satu per request (Cocok untuk Web, di Desktop mirip Transient)
            // Singleton: Dibuat sekali seumur hidup aplikasi (Hati-hati dengan DbContext!)

            services.AddTransient<IInventoryService, InventoryService>();
            services.AddTransient<IProductionService, ProductionService>();
            services.AddTransient<IFinanceService, FinanceService>();
            services.AddTransient<IDatabaseSeeder, DbSeeder>();
            services.AddTransient<ViewModels.Inventory.InventoryViewModel>();
            services.AddTransient<ViewModels.Production.ProductionViewModel>();

            // C. REGISTER VIEWMODELS (OTAK UI)
            // Kita akan daftarkan ViewModel di sini nanti setelah kita buat filenya.
            // Contoh: services.AddTransient<DashboardViewModel>();
            // services.AddTransient<InventoryViewModel>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Memastikan Database siap pakai sebelum user melihat apapun.
        /// </summary>
        private async System.Threading.Tasks.Task InitializeDatabaseAsync()
        {
            try
            {
                // Buat Scope baru untuk akses database
                using (var scope = Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

                    // 1. Migrate: Pastikan tabel update sesuai codingan terakhir
                    await context.Database.MigrateAsync();

                    // 2. Seed: Isi data awal (Tepung, Gula, dll) jika kosong
                    await seeder.SeedInitialDataAsync();
                }
            }
            catch (System.Exception ex)
            {
                // Jika error parah (misal permission denied), kita diamkan dulu atau log.
                // Di God Mode, kita lempar debugger biar ketahuan.
                System.Diagnostics.Debug.WriteLine($"DATABASE INIT FAILED: {ex.Message}");

                // Opsional: Tampilkan Dialog Error jika UI sudah siap
            }
        }
    }
}