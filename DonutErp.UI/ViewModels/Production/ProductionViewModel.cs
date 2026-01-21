#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using System.Linq;

namespace DonutErp.UI.ViewModels.Production
{
    public partial class ProductionViewModel : ObservableObject
    {
        private readonly IProductionService _productionService;

        // List Batch Aktif
        [ObservableProperty]
        private ObservableCollection<ProductionBatch> _activeBatches = new();

        // Form New Plan
        [ObservableProperty] private string _newBatchCode = $"BATCH-{DateTime.Now:yyyyMMdd}";
        [ObservableProperty] private string _newBatchNotes = "";

        // Form Execution - Oil Management
        [ObservableProperty] private double _oilStartLevel;
        [ObservableProperty] private double _oilAdded;
        [ObservableProperty] private double _oilEndLevel;

        // Form Execution - Batch Details
        [ObservableProperty] private ProductionBatch? _selectedBatch;
        [ObservableProperty] private string _batchCode = "";
        [ObservableProperty] private DateTimeOffset _productionDate = DateTimeOffset.Now;
        
        // Form Execution - Batch Inputs
        [ObservableProperty] private double _startOilLevel;
        [ObservableProperty] private double _endOilLevel;
        [ObservableProperty] private decimal _laborCostInput;
        [ObservableProperty] private decimal _utilityCostInput;

        // Product Selection and Batch Output
        [ObservableProperty]
        private ObservableCollection<Product> _availableProducts = new();

        [ObservableProperty] private Product? _selectedProductToAdd;
        [ObservableProperty] private double _qtyGoodInput;
        [ObservableProperty] private double _qtyRejectInput;

        // Batch Outputs
        [ObservableProperty]
        private ObservableCollection<ProductionOutput> _batchOutputs = new();

        [ObservableProperty] private bool _isBusy;

        public ProductionViewModel(IProductionService productionService)
        {
            _productionService = productionService;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _productionService.GetActiveBatchesAsync();
                ActiveBatches = new ObservableCollection<ProductionBatch>(list);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task CreatePlanAsync()
        {
            IsBusy = true;
            try
            {
                // Manggil Method BARU: CreatePlannedBatchAsync
                await _productionService.CreatePlannedBatchAsync(NewBatchCode, NewBatchNotes);

                NewBatchCode = $"BATCH-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}";
                NewBatchNotes = "";

                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task StartBatchAsync()
        {
            if (SelectedBatch == null) return;
            IsBusy = true;
            try
            {
                await _productionService.StartBatchAsync(SelectedBatch.Id, OilStartLevel);
                await LoadDataAsync();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task FinishBatchAsync()
        {
            if (SelectedBatch == null) return;
            IsBusy = true;
            try
            {
                // Manggil Method BARU: CompleteBatchAsync
                await _productionService.CompleteBatchAsync(
                    SelectedBatch.Id,
                    OilEndLevel,
                    LaborCostInput,
                    UtilityCostInput,
                    "Admin");

                // Reset Inputs
                OilEndLevel = 0;
                LaborCostInput = 0;
                UtilityCostInput = 0;
                SelectedBatch = null;

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void AddToBatch()
        {
            if (SelectedProductToAdd == null || QtyGoodInput <= 0) return;
            
            var batchOutput = new ProductionOutput
            {
                ProductId = SelectedProductToAdd.Id,
                Product = SelectedProductToAdd,
                QuantityGood = (int)QtyGoodInput,
                QuantityReject = (int)QtyRejectInput
            };

            BatchOutputs.Add(batchOutput);
            
            // Reset inputs
            SelectedProductToAdd = null;
            QtyGoodInput = 0;
            QtyRejectInput = 0;
        }

        [RelayCommand]
        public async Task SubmitBatchAsync()
        {
            if (SelectedBatch == null || BatchOutputs.Count == 0) return;
            
            IsBusy = true;
            try
            {
                // Implement batch submission logic
                // This would typically save the batch outputs
                await LoadDataAsync();
                BatchOutputs.Clear();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}