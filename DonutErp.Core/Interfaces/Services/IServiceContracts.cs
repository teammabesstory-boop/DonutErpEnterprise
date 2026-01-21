using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    // ==========================================
    // 1. INVENTORY SERVICE (PENJAGA GUDANG)
    // ==========================================
    public interface IInventoryService
    {
        // Ambil semua bahan baku
        Task<List<Ingredient>> GetAllIngredientsAsync();

        // Ambil bahan yang stoknya kritis (di bawah minimum)
        Task<List<Ingredient>> GetLowStockAlertsAsync();

        // Tambah/Edit Bahan Baku Baru
        Task AddOrUpdateIngredientAsync(Ingredient ingredient);

        // Stock Opname (Penyesuaian Stok Manual)
        // reason: "Pecah Telur", "Bonus Supplier", "Salah Hitung"
        Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason);

        // Cek apakah bahan cukup untuk resep tertentu
        Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake);
    }

    // ==========================================
    // 2. PRODUCTION SERVICE (LOGIKA DAPUR & HPP)
    // ==========================================
    public interface IProductionService
    {
        // 1. THEORETICAL HPP CALCULATOR
        // Menghitung HPP ideal berdasarkan resep saat ini.
        // Return: Detail HPP per komponen (Bahan + Waste + Estimasi Minyak)
        Task<decimal> CalculateTheoreticalHppAsync(Guid productId);

        // 2. RUN PRODUCTION BATCH (EKSEKUSI PRODUKSI)
        // Ini akan memotong stok bahan baku secara otomatis (Backflush).
        Task<ProductionBatch> CreateProductionBatchAsync(ProductionBatch batch, List<ProductionOutput> outputs);

        // 3. ANALISA MINYAK (DEEP FRY LOGIC)
        // Menghitung biaya real minyak yang hilang berdasarkan data batch (Start Level vs End Level).
        decimal CalculateOilLossCost(double startLevelLiter, double endLevelLiter, double oilAddedLiter, decimal oilPricePerLiter);
    }

    // ==========================================
    // 3. FINANCE SERVICE (AKUNTAN)
    // ==========================================
    public interface IFinanceService
    {
        // Catat Penjualan (Otomatis potong stok produk jadi jika ada, atau sekadar catat uang masuk)
        Task<Transaction> RecordSalesAsync(Transaction transaction);

        // Catat Pengeluaran Operasional (Listrik, Gaji, dll)
        Task RecordExpenseAsync(string description, decimal amount, DateTime date);

        // Dashboard Data: Profit & Loss Real-time
        Task<(decimal TotalRevenue, decimal TotalCogs, decimal NetProfit)> GetProfitLossSummaryAsync(DateTime startDate, DateTime endDate);

        // Dashboard Data: Top Selling Products
        Task<List<(string ProductName, int QtySold)>> GetTopSellingProductsAsync(int topN);
    }

    // ==========================================
    // 4. DATA SEEDER (INITIALIZER)
    // ==========================================
    // Kontrak untuk mengisi data awal (Bahan baku umum) biar aplikasi gak kosong melompong saat pertama run.
    public interface IDatabaseSeeder
    {
        Task SeedInitialDataAsync();
    }
}