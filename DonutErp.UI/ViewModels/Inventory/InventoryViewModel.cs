using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;

namespace DonutErp.UI.ViewModels.Inventory
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;

        // ==========================================
        // OBSERVABLE PROPERTIES (STATE UI)
        // ==========================================

        // List Bahan Baku untuk ditampilkan di Tabel
        [ObservableProperty]
        private ObservableCollection<Ingredient> _ingredients = new();

        // Bahan yang sedang dipilih user di tabel
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StockOpnameCommand))] // Update tombol Opname aktif/tidak
        private Ingredient? _selectedIngredient;

        // Loading Indicator (untuk Spinner)
        [ObservableProperty]
        private bool _isLoading;

        // Text pencarian
        [ObservableProperty]
        private string _searchText = string.Empty;

        // Statistik Dashboard Kecil di atas Tabel
        [ObservableProperty]
        private decimal _totalInventoryAssetValue;

        [ObservableProperty]
        private int _lowStockItemCount;

        // ==========================================
        // CONSTRUCTOR (DEPENDENCY INJECTION)
        // ==========================================
        public InventoryViewModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;

            // Load data saat ViewModel dibuat (atau bisa dipanggil manual nanti)
            _ = LoadDataAsync();
        }

        // ==========================================
        // COMMANDS (AKSI USER)
        // ==========================================

        /// <summary>
        /// Mengambil data dari Database dan menghitung statistik.
        /// </summary>
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                // 1. Ambil Data Mentah dari Service
                var rawData = await _inventoryService.GetAllIngredientsAsync();

                // 2. Filter Pencarian (Client-Side filtering for speed on small data)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    rawData = rawData.Where(i =>
                        i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        i.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 3. Update UI Collection
                Ingredients = new ObservableCollection<Ingredient>(rawData);

                // 4. Hitung Statistik Real-time
                CalculateDashboardStats();
            }
            catch (Exception ex)
            {
                // In God Mode, we assume a Logging Service exists, but for now Debug.
                System.Diagnostics.Debug.WriteLine($"[ERROR] Load Inventory: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Melakukan Stock Opname (Penyesuaian Stok)
        /// Command ini hanya aktif jika ada item yang dipilih (SelectedIngredient != null).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanModifyIngredient))]
        public async Task StockOpnameAsync(double newRealStock)
        {
            if (SelectedIngredient == null) return;

            try
            {
                IsLoading = true;

                // Panggil Service untuk logic Opname & Jurnal Akuntansi
                await _inventoryService.AdjustStockAsync(
                    SelectedIngredient.Id,
                    newRealStock,
                    "Manual Stock Opname via Dashboard");

                // Refresh data untuk melihat perubahan
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stock Opname: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Validasi tombol: Hanya bisa klik jika ada item terpilih
        private bool CanModifyIngredient() => SelectedIngredient != null;

        // Trigger pencarian saat user menekan Enter di TextBox
        [RelayCommand]
        public async Task SearchAsync()
        {
            await LoadDataAsync();
        }

        // ==========================================
        // HELPER LOGIC
        // ==========================================
        private void CalculateDashboardStats()
        {
            // Menghitung Total Aset Uang yang mengendap di Gudang
            // Rumus: Sum(Stok * Harga Rata-rata)
            TotalInventoryAssetValue = Ingredients.Sum(i => (decimal)i.CurrentStock * i.AvgCostPerUsageUnit);

            // Hitung item yang perlu belanja ulang
            LowStockItemCount = Ingredients.Count(i => i.CurrentStock <= i.MinStockLevel);
        }
    }
}