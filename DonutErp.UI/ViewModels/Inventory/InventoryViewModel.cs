#nullable enable
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using System.Collections.Generic;

namespace DonutErp.UI.ViewModels.Inventory
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private List<Ingredient> _allIngredientsCache = new();

        // =========================================================
        // 1. DASHBOARD KPI (Untuk Header UI)
        // =========================================================
        [ObservableProperty] private decimal _totalInventoryAssetValue;
        [ObservableProperty] private int _lowStockItemCount;

        // =========================================================
        // 2. MAIN DATA LISTS
        // =========================================================
        [ObservableProperty]
        private ObservableCollection<Ingredient> _ingredients = new();

        [ObservableProperty]
        private ObservableCollection<Ingredient> _lowStockIngredients = new();

        [ObservableProperty]
        private ObservableCollection<string> _categorySuggestions = new();

        // =========================================================
        // 3. INTERACTION & SELECTION
        // =========================================================
        [ObservableProperty]
        private Ingredient? _selectedIngredient; // Untuk Binding GridView

        [ObservableProperty]
        private string _searchText = ""; // Untuk Binding TextBox Search

        [ObservableProperty]
        private bool _isLoading; // XAML minta 'IsLoading', bukan 'IsBusy'

        // =========================================================
        // 4. FORMS (INPUT & ADJUSTMENT)
        // =========================================================
        [ObservableProperty]
        private Ingredient _newIngredient = new()
        {
            Id = System.Guid.Empty,
            Name = "",
            Category = "",
            Sku = "",
            PurchaseUnit = "",
            UsageUnit = ""
        };

        // State untuk Stock Opname / Adjustment
        [ObservableProperty] private Ingredient? _selectedAdjustmentItem;
        [ObservableProperty] private double _adjustmentRealStock;
        [ObservableProperty] private string _adjustmentReason = "";

        // =========================================================
        // CONSTRUCTOR & LOADER
        // =========================================================
        public InventoryViewModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _inventoryService.GetAllIngredientsAsync();
                _allIngredientsCache = list;
                Ingredients = new ObservableCollection<Ingredient>(list);

                // Hitung KPI
                TotalInventoryAssetValue = list.Sum(x => (decimal)x.CurrentStock * x.AvgCostPerUsageUnit);

                var alerts = await _inventoryService.GetLowStockAlertsAsync();
                LowStockIngredients = new ObservableCollection<Ingredient>(alerts);
                LowStockItemCount = alerts.Count;

                // Categories
                var cats = list.Select(x => x.Category)
                               .Where(c => !string.IsNullOrEmpty(c))
                               .Distinct().OrderBy(c => c).ToList();
                CategorySuggestions = new ObservableCollection<string>(cats);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================
        // COMMANDS
        // =========================================================

        [RelayCommand]
        public void Search(string query)
        {
            SearchText = query; // Sync property
            if (string.IsNullOrWhiteSpace(query))
            {
                Ingredients = new ObservableCollection<Ingredient>(_allIngredientsCache);
            }
            else
            {
                var filtered = _allIngredientsCache
                    .Where(i => i.Name.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
                                i.Sku.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
                                i.Category.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Ingredients = new ObservableCollection<Ingredient>(filtered);
            }
        }

        [RelayCommand]
        public async Task SaveIngredientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewIngredient.Name)) return;

            IsLoading = true;
            try
            {
                await _inventoryService.AddOrUpdateIngredientAsync(NewIngredient);

                // Reset Form
                NewIngredient = new Ingredient
                {
                    Id = System.Guid.Empty,
                    Name = "",
                    Category = "",
                    Sku = "",
                    PurchaseUnit = "",
                    UsageUnit = ""
                };

                await LoadDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Command Khusus untuk membuka Dialog/Mode Stock Opname (Diminta XAML)
        // Disini kita simplifikasi: Set item yang dipilih sebagai item yang mau di-adjust
        [RelayCommand]
        public void StockOpname()
        {
            if (SelectedIngredient != null)
            {
                SelectedAdjustmentItem = SelectedIngredient;
                AdjustmentRealStock = SelectedIngredient.CurrentStock; // Default value = stok sekarang
                AdjustmentReason = "Stock Opname Rutin";
            }
        }

        [RelayCommand]
        public async Task SubmitAdjustmentAsync()
        {
            if (SelectedAdjustmentItem == null || string.IsNullOrWhiteSpace(AdjustmentReason)) return;

            IsLoading = true;
            try
            {
                await _inventoryService.AdjustStockAsync(
                    SelectedAdjustmentItem.Id,
                    AdjustmentRealStock,
                    AdjustmentReason,
                    "Admin");

                SelectedAdjustmentItem = null;
                AdjustmentRealStock = 0;
                AdjustmentReason = "";

                await LoadDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}