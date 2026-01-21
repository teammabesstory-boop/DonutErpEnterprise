using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using Microsoft.UI.Xaml.Controls; // Untuk Dialog sederhana
using DonutErp.Infrastructure.Data;

namespace DonutErp.UI.ViewModels.Production
{
    public partial class ProductionViewModel : ObservableObject
    {
        private readonly IProductionService _productionService;
        private readonly AppDbContext _context; // Kita butuh context untuk ambil list produk (shortcut)

        // ==========================================
        // STATE: BATCH HEADER (Data Minyak & Umum)
        // ==========================================
        [ObservableProperty]
        private string _batchCode;

        [ObservableProperty]
        private DateTimeOffset _productionDate = DateTime.Now;

        [ObservableProperty]
        private double _oilStartLevel;

        [ObservableProperty]
        private double _oilEndLevel;

        [ObservableProperty]
        private double _oilAdded;

        // ==========================================
        // STATE: ITEM INPUT (Form Tambah Donat)
        // ==========================================
        [ObservableProperty]
        private ObservableCollection<Product> _availableProducts = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddToBatchCommand))]
        private Product? _selectedProductToAdd;

        [ObservableProperty]
        private int _qtyGoodInput;

        [ObservableProperty]
        private int _qtyRejectInput;

        // ==========================================
        // STATE: LIST BATCH (Keranjang Sementara)
        // ==========================================
        // Ini daftar donat yang mau diproses dalam batch ini
        [ObservableProperty]
        private ObservableCollection<ProductionOutput> _batchOutputs = new();

        [ObservableProperty]
        private bool _isBusy;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public ProductionViewModel(IProductionService productionService, AppDbContext context)
        {
            _productionService = productionService;
            _context = context;

            // Auto-Generate Batch Code (Format: PRD-YYYYMMDD-Random)
            GenerateNewBatchCode();

            // Load Data Produk Master
            _ = LoadMasterDataAsync();
        }

        private void GenerateNewBatchCode()
        {
            BatchCode = $"PRD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        }

        // ==========================================
        // LOGIC
        // ==========================================

        [RelayCommand]
        public async Task LoadMasterDataAsync()
        {
            // Ambil semua produk yang tipenya bukan 'RawMaterial'
            // Kita pakai _context langsung untuk read-only data biar cepat (Idealnya via Service terpisah)
            var products = _context.Products
                .Where(p => p.Type != ProductType.RawMaterial)
                .ToList();

            AvailableProducts = new ObservableCollection<Product>(products);
        }

        [RelayCommand]
        public async Task CalculateTheoreticalHppAsync()
        {
            if (SelectedProductToAdd == null) return;

            IsBusy = true;
            try
            {
                // Hitung HPP di atas kertas untuk produk yang dipilih
                decimal hpp = await _productionService.CalculateTheoreticalHppAsync(SelectedProductToAdd.Id);

                // Tampilkan info (Nanti kita bind ke UI TextBlock atau Dialog)
                // Untuk sekarang kita update property di object Product biar UI refresh
                SelectedProductToAdd.CachedHpp = hpp;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddToBatch))]
        public void AddToBatch()
        {
            if (SelectedProductToAdd == null) return;

            // Buat object Output sementara (belum masuk DB)
            var item = new ProductionOutput
            {
                ProductId = SelectedProductToAdd.Id,
                Product = SelectedProductToAdd, // Reference object biar Nama tampil di Tabel
                QuantityGood = QtyGoodInput,
                QuantityReject = QtyRejectInput
            };

            BatchOutputs.Add(item);

            // Reset Input form
            QtyGoodInput = 0;
            QtyRejectInput = 0;
            SelectedProductToAdd = null;
        }

        private bool CanAddToBatch()
        {
            return SelectedProductToAdd != null && (QtyGoodInput > 0 || QtyRejectInput > 0);
        }

        [RelayCommand(CanExecute = nameof(CanSubmitBatch))]
        public async Task SubmitBatchAsync()
        {
            IsBusy = true;
            try
            {
                // 1. Siapkan Object Header Batch
                var batch = new ProductionBatch
                {
                    Id = Guid.NewGuid(),
                    BatchCode = BatchCode,
                    ProductionDate = ProductionDate.DateTime,
                    Status = BatchStatus.Finished, // Langsung selesai

                    // Data Minyak Goreng
                    OilLevelStartLiter = OilStartLevel,
                    OilLevelEndLiter = OilEndLevel,
                    OilAddedLiter = OilAdded,

                    // Kosongkan dulu hasil kalkulasi (akan diisi oleh Service)
                    OilConsumedLiters = 0,
                    CalculatedOilCost = 0,
                    TotalBatchCost = 0
                };

                // 2. Panggil Service "God Mode"
                // Service ini akan melakukan Backflush stok, hitung minyak, dan simpan DB.
                var result = await _productionService.CreateProductionBatchAsync(batch, BatchOutputs.ToList());

                // 3. Sukses! Reset UI.
                ContentDialog successDialog = new ContentDialog
                {
                    Title = "Produksi Berhasil!",
                    Content = $"Batch {result.BatchCode} tersimpan.\n" +
                              $"Total Cost: Rp {result.TotalBatchCost:N0}\n" +
                              $"Minyak Terpakai: {result.OilConsumedLiters:N2} Liter",
                    CloseButtonText = "Ok",
                    XamlRoot = App.Current.MainWindow.Content.XamlRoot // Wajib di WinUI 3
                };
                await successDialog.ShowAsync();

                // Bersihkan form untuk batch baru
                BatchOutputs.Clear();
                GenerateNewBatchCode();
                OilStartLevel = OilEndLevel; // Level akhir batch ini jadi level awal batch besok
                OilEndLevel = 0;
                OilAdded = 0;
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Gagal Memproses Batch",
                    Content = ex.Message,
                    CloseButtonText = "Tutup",
                    XamlRoot = App.Current.MainWindow.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSubmitBatch()
        {
            // Tombol Process hanya aktif jika ada item di keranjang & data minyak masuk akal
            return BatchOutputs.Count > 0 && OilStartLevel >= 0;
        }

        // Helper untuk hapus item dari keranjang
        [RelayCommand]
        public void RemoveFromBatch(ProductionOutput item)
        {
            if (BatchOutputs.Contains(item))
            {
                BatchOutputs.Remove(item);
            }
            SubmitBatchCommand.NotifyCanExecuteChanged();
        }
    }
}