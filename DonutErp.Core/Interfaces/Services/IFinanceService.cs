using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
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
}