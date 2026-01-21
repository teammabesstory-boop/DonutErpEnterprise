using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    // DTO (Data Transfer Object) untuk Laporan
    public record ProfitLossReport(
        decimal TotalRevenue,
        decimal TotalCogs,
        decimal GrossProfit,
        decimal TotalOperationalExpense,
        decimal TotalDepreciation,
        decimal NetProfit,
        List<ExpenseCategorySummary> ExpenseBreakdown
    );

    public record ExpenseCategorySummary(string Category, decimal Amount);

    public interface IFinanceService
    {
        // ==========================================
        // 1. CASHFLOW & WALLET MANAGEMENT
        // ==========================================
        Task<List<Wallet>> GetWalletsAsync();
        Task<Wallet?> GetWalletByIdAsync(Guid id);
        Task CreateWalletAsync(Wallet wallet);

        // Fitur Transfer Antar Akun (Misal: Setor Tunai Kasir ke Bank)
        // Harus Transactional (ACID)!
        Task TransferFundsAsync(Guid sourceWalletId, Guid targetWalletId, decimal amount, string notes, string username);

        // ==========================================
        // 2. TRANSACTION RECORDING
        // ==========================================
        Task RecordIncomeAsync(Transaction transaction); // Penjualan
        Task RecordExpenseAsync(string description, decimal amount, DateTime date, Guid walletId, string category, string username); // Pengeluaran
        Task<List<Transaction>> GetRecentTransactionsAsync(int count);

        // ==========================================
        // 3. ASSET & DEPRECIATION ENGINE
        // ==========================================
        Task<List<Asset>> GetActiveAssetsAsync();
        Task RegisterNewAssetAsync(Asset asset, Guid fundingWalletId, string username);

        // Tombol "Tutup Buku Bulan Ini": Hitung penyusutan semua alat
        Task RunMonthlyDepreciationAsync(DateTime period, string username);

        // ==========================================
        // 4. FINANCIAL REPORTING (THE LEDGER)
        // ==========================================
        Task<ProfitLossReport> GenerateProfitLossReportAsync(DateTime startDate, DateTime endDate);

        // Analisa Top Produk (Revenue Driver)
        Task<List<(string ProductName, int Qty, decimal Revenue)>> GetTopSellingProductsAsync(int topN);
    }
}