using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;

namespace DonutErp.Infrastructure.Services.Implements
{
    public class FinanceService : IFinanceService
    {
        private readonly AppDbContext _context;

        public FinanceService(AppDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // 1. RECORD SALES (PENCATATAN PENJUALAN)
        // =================================================================
        public async Task<Transaction> RecordSalesAsync(Transaction transaction)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal totalTransactionCost = 0;

                // Loop setiap item belanjaan
                foreach (var detail in transaction.Details)
                {
                    // Ambil data produk terbaru untuk intip HPP saat ini
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product == null) continue;

                    // --- PROFIT LOCKING MECHANISM ---
                    detail.CostAtSale = product.CachedHpp;
                    detail.PriceAtSale = product.SellingPrice;

                    // Hitung total COGS (HPP) transaksi ini
                    totalTransactionCost += (detail.CostAtSale * detail.Quantity);
                }

                // Lengkapi Header Transaksi
                transaction.Type = TransactionType.SalesIncome;
                transaction.Date = DateTime.Now;
                transaction.TotalCost = totalTransactionCost; // Total Modal untuk penjualan ini

                // Save ke Database
                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
                return transaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        // =================================================================
        // 2. RECORD EXPENSE (PENGELUARAN OPERASIONAL)
        // =================================================================
        public async Task RecordExpenseAsync(string description, decimal amount, DateTime date)
        {
            var expense = new Transaction
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"EXP-{date:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                Date = date,
                Type = TransactionType.OperationalExpense,
                Description = description,
                TotalAmount = amount,
                TotalCost = 0, // Pengeluaran tidak punya HPP
                PaymentMethod = "CASH"
            };

            await _context.Transactions.AddAsync(expense);
            await _context.SaveChangesAsync();
        }

        // =================================================================
        // 3. DASHBOARD ANALYTICS (REAL-TIME P&L)
        // =================================================================
        public async Task<(decimal TotalRevenue, decimal TotalCogs, decimal NetProfit)> GetProfitLossSummaryAsync(DateTime startDate, DateTime endDate)
        {
            // Ambil semua transaksi dalam periode
            var transactions = await _context.Transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .AsNoTracking()
                .ToListAsync();

            // A. REVENUE (OMZET KOTOR)
            decimal revenue = transactions
                .Where(t => t.Type == TransactionType.SalesIncome)
                .Sum(t => t.TotalAmount);

            // Tambah inventory gain (jika ada stock opname surplus)
            // PERBAIKAN: Ganti Adjustment -> StockAdjustment
            decimal inventoryGain = transactions
                .Where(t => t.Type == TransactionType.StockAdjustment && t.TotalAmount > 0)
                .Sum(t => t.TotalAmount);

            // B. COGS (HARGA POKOK PENJUALAN)
            decimal cogs = transactions
                .Where(t => t.Type == TransactionType.SalesIncome)
                .Sum(t => t.TotalCost);

            // C. EXPENSES (BEBAN OPERASIONAL)
            decimal opex = transactions
                .Where(t => t.Type == TransactionType.OperationalExpense)
                .Sum(t => t.TotalAmount);

            // PERBAIKAN: Ganti Adjustment -> StockAdjustment
            decimal inventoryLoss = transactions
                .Where(t => t.Type == TransactionType.StockAdjustment && t.TotalAmount < 0)
                .Sum(t => Math.Abs(t.TotalAmount));

            // D. NET PROFIT (LABA BERSIH)
            decimal totalRevenueFinal = revenue + inventoryGain;
            decimal totalExpenseFinal = cogs + opex + inventoryLoss;
            decimal netProfit = totalRevenueFinal - totalExpenseFinal;

            return (totalRevenueFinal, cogs, netProfit);
        }

        // =================================================================
        // 4. TOP SELLING PRODUCTS
        // =================================================================
        public async Task<List<(string ProductName, int QtySold)>> GetTopSellingProductsAsync(int topN)
        {
            var topProducts = await _context.TransactionDetails
                .Include(d => d.Transaction)
                .Where(d => d.Transaction!.Type == TransactionType.SalesIncome)
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQty = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(topN)
                .ToListAsync();

            var result = new List<(string, int)>();
            foreach (var item in topProducts)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    result.Add((product.Name, item.TotalQty));
                }
            }

            return result;
        }
    }
}