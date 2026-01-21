#nullable enable
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

        // ==========================================
        // 1. CASHFLOW & WALLET MANAGEMENT
        // ==========================================
        public async Task<List<Wallet>> GetWalletsAsync()
        {
            return await _context.Wallets.AsNoTracking().ToListAsync();
        }

        public async Task<Wallet?> GetWalletByIdAsync(Guid id)
        {
            return await _context.Wallets.FindAsync(id);
        }

        public async Task CreateWalletAsync(Wallet wallet)
        {
            if (wallet.Id == Guid.Empty) wallet.Id = Guid.NewGuid();
            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();
        }

        public async Task TransferFundsAsync(Guid sourceWalletId, Guid targetWalletId, decimal amount, string notes, string username)
        {
            // TRANSACTIONAL BLOCK: All or Nothing
            using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var source = await _context.Wallets.FindAsync(sourceWalletId);
                var target = await _context.Wallets.FindAsync(targetWalletId);

                if (source == null || target == null) throw new Exception("Wallet not found");
                if (source.CurrentBalance < amount) throw new Exception($"Saldo tidak cukup di {source.Name}. Sisa: {source.CurrentBalance:C0}");

                // 1. Potong Sumber
                source.CurrentBalance -= amount;
                _context.Wallets.Update(source);

                // 2. Tambah Target
                target.CurrentBalance += amount;
                _context.Wallets.Update(target);

                // 3. Catat Mutasi Keluar
                var trxOut = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"TRF-OUT-{DateTime.Now:MMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = DateTime.Now,
                    Type = TransactionType.Transfer,
                    WalletId = sourceWalletId,
                    Description = $"Transfer ke {target.Name}",
                    Notes = notes,
                    TotalAmount = -amount, // Negatif karena keluar
                    PaymentMethod = "INTERNAL_TRANSFER"
                };
                await _context.Transactions.AddAsync(trxOut);

                // 4. Catat Mutasi Masuk
                var trxIn = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"TRF-IN-{DateTime.Now:MMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = DateTime.Now,
                    Type = TransactionType.Transfer,
                    WalletId = targetWalletId,
                    Description = $"Terima dari {source.Name}",
                    Notes = notes,
                    TotalAmount = amount, // Positif karena masuk
                    PaymentMethod = "INTERNAL_TRANSFER"
                };
                await _context.Transactions.AddAsync(trxIn);

                // 5. Audit Log
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = "FUND_TRANSFER",
                    EntityName = "Wallet",
                    RecordId = $"{sourceWalletId}->{targetWalletId}",
                    Username = username,
                    ChangesJson = $"Amount: {amount}, Notes: {notes}",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();
            }
            catch
            {
                await dbTrans.RollbackAsync();
                throw;
            }
        }

        // ==========================================
        // 2. TRANSACTION RECORDING
        // ==========================================
        public async Task RecordIncomeAsync(Transaction transaction)
        {
            using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Saldo Wallet
                if (transaction.WalletId.HasValue)
                {
                    var wallet = await _context.Wallets.FindAsync(transaction.WalletId.Value);
                    if (wallet != null)
                    {
                        wallet.CurrentBalance += transaction.TotalAmount;
                        _context.Wallets.Update(wallet);
                    }
                }

                await _context.Transactions.AddAsync(transaction);

                // Simpan Detail Transaksi (Items)
                if (transaction.Details != null && transaction.Details.Any())
                {
                    foreach (var detail in transaction.Details)
                    {
                        if (detail.Id == Guid.Empty) detail.Id = Guid.NewGuid();
                        detail.TransactionId = transaction.Id;
                        // HPP CostAtSale biasanya sudah diisi oleh POS Service sebelum dikirim kesini
                    }
                    // EF Core biasanya handle child insert otomatis jika di-link, tapi eksplisit lebih aman
                }

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();
            }
            catch
            {
                await dbTrans.RollbackAsync();
                throw;
            }
        }

        public async Task RecordExpenseAsync(string description, decimal amount, DateTime date, Guid walletId, string category, string username)
        {
            using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var wallet = await _context.Wallets.FindAsync(walletId);
                if (wallet == null) throw new Exception("Wallet not found");
                if (wallet.CurrentBalance < amount) throw new Exception("Saldo Kas tidak cukup untuk biaya ini.");

                // Potong Saldo
                wallet.CurrentBalance -= amount;
                _context.Wallets.Update(wallet);

                var trx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"EXP-{date:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = date,
                    Type = TransactionType.OperationalExpense,
                    WalletId = walletId,
                    Description = description,
                    Notes = category, // Kita simpan Kategori di Notes sementara
                    TotalAmount = amount, // Expense is Positive amount in logic, but reduces wallet
                    TotalCost = amount, // For P&L Calculation
                    PaymentMethod = wallet.Type.ToString()
                };

                await _context.Transactions.AddAsync(trx);

                // Audit
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = "RECORD_EXPENSE",
                    EntityName = "Transaction",
                    RecordId = trx.Id.ToString(),
                    Username = username,
                    ChangesJson = $"Desc: {description}, Amt: {amount}",
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();
            }
            catch
            {
                await dbTrans.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Transaction>> GetRecentTransactionsAsync(int count)
        {
            return await _context.Transactions
                .Include(t => t.Wallet)
                .OrderByDescending(t => t.Date)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        // ==========================================
        // 3. ASSET & DEPRECIATION ENGINE
        // ==========================================
        public async Task<List<Asset>> GetActiveAssetsAsync()
        {
            return await _context.Assets.AsNoTracking().ToListAsync();
        }

        public async Task RegisterNewAssetAsync(Asset asset, Guid fundingWalletId, string username)
        {
            using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Potong Uang Pembelian Aset
                var wallet = await _context.Wallets.FindAsync(fundingWalletId);
                if (wallet == null) throw new Exception("Funding Wallet not found");
                if (wallet.CurrentBalance < asset.PurchasePrice) throw new Exception("Saldo tidak cukup beli aset.");

                wallet.CurrentBalance -= asset.PurchasePrice;
                _context.Wallets.Update(wallet);

                // 2. Catat Transaksi Pembelian (Capex)
                var capexTrx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"CAPEX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = asset.PurchaseDate,
                    Type = TransactionType.OperationalExpense, // Atau bikin Type baru: AssetPurchase
                    WalletId = fundingWalletId,
                    Description = $"Beli Aset: {asset.Name}",
                    TotalAmount = asset.PurchasePrice,
                    TotalCost = 0 // Capex tidak langsung masuk P&L sebagai Expense, tapi lewat depresiasi
                };
                await _context.Transactions.AddAsync(capexTrx);

                // 3. Simpan Data Aset
                if (asset.Id == Guid.Empty) asset.Id = Guid.NewGuid();
                await _context.Assets.AddAsync(asset);

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();
            }
            catch
            {
                await dbTrans.RollbackAsync();
                throw;
            }
        }

        public async Task RunMonthlyDepreciationAsync(DateTime period, string username)
        {
            // Logic: Cari semua aset aktif, hitung nilai susut bulan ini, catat sebagai Expense (Non-Cash)

            var assets = await _context.Assets.ToListAsync();
            decimal totalDepreciation = 0;
            var depreciationLogs = new List<Transaction>();

            foreach (var asset in assets)
            {
                // Cek apakah aset sudah habis masa pakainya?
                // Logic sederhana: Asumsi depresiasi mulai bulan depan setelah beli
                // Hitung umur aset dalam bulan
                var ageMonths = ((period.Year - asset.PurchaseDate.Year) * 12) + period.Month - asset.PurchaseDate.Month;

                if (ageMonths > 0 && ageMonths <= asset.UsefulLifeMonths)
                {
                    decimal monthlyAmount = asset.MonthlyDepreciation;
                    totalDepreciation += monthlyAmount;

                    // Kita bisa catat detail per aset atau digabung (Aggregated)
                    // Untuk Enterprise, biasanya digabung per kategori, tapi disini kita catat aggregated.
                }
            }

            if (totalDepreciation > 0)
            {
                // Catat Expense Non-Cash (Tidak mengurangi Wallet, tapi mengurangi Profit)
                var depTrx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"DEPR-{period:yyyyMM}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = period,
                    Type = TransactionType.AssetDepreciation,
                    Description = $"Penyusutan Aset Periode {period:MMM yyyy}",
                    TotalAmount = 0, // Cash Out 0
                    TotalCost = totalDepreciation, // Expense P&L Ada
                    WalletId = null // Non-Cash Transaction
                };

                await _context.Transactions.AddAsync(depTrx);
                await _context.SaveChangesAsync();
            }
        }

        // ==========================================
        // 4. FINANCIAL REPORTING (THE LEDGER)
        // ==========================================
        public async Task<ProfitLossReport> GenerateProfitLossReportAsync(DateTime startDate, DateTime endDate)
        {
            var txs = await _context.Transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .AsNoTracking()
                .ToListAsync();

            // 1. REVENUE (OMZET)
            decimal revenue = txs.Where(t => t.Type == TransactionType.SalesIncome).Sum(t => t.TotalAmount);

            // 2. COGS (HPP)
            decimal cogs = txs.Where(t => t.Type == TransactionType.SalesIncome).Sum(t => t.TotalCost)
                           + txs.Where(t => t.Type == TransactionType.MaterialExpense).Sum(t => t.TotalCost); // Direct Material Buy bisa masuk sini kalau sistem periodik

            decimal grossProfit = revenue - cogs;

            // 3. OPERATIONAL EXPENSES (OPEX)
            var opexTransactions = txs.Where(t => t.Type == TransactionType.OperationalExpense).ToList();
            decimal totalOpex = opexTransactions.Sum(t => t.TotalCost); // Pakai TotalCost karena TotalAmount mungkin 0 untuk non-cash

            // Breakdown per Kategori (disimpan di Notes)
            var expenseBreakdown = opexTransactions
                .GroupBy(t => t.Notes ?? "Uncategorized")
                .Select(g => new ExpenseCategorySummary(g.Key, g.Sum(x => x.TotalCost)))
                .ToList();

            // 4. DEPRECIATION
            decimal totalDepr = txs.Where(t => t.Type == TransactionType.AssetDepreciation).Sum(t => t.TotalCost);

            decimal netProfit = grossProfit - totalOpex - totalDepr;

            return new ProfitLossReport(revenue, cogs, grossProfit, totalOpex, totalDepr, netProfit, expenseBreakdown);
        }

        public async Task<List<(string ProductName, int Qty, decimal Revenue)>> GetTopSellingProductsAsync(int topN)
        {
            // Complex Query: Group TransactionDetails by Product
            var data = await _context.TransactionDetails
                .Include(d => d.Product)
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    Name = g.First().Product != null ? g.First().Product!.Name : "Unknown",
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalRev = g.Sum(x => x.PriceAtSale * x.Quantity)
                })
                .OrderByDescending(x => x.TotalRev)
                .Take(topN)
                .ToListAsync();

            return data.Select(x => (x.Name, x.TotalQty, x.TotalRev)).ToList();
        }
    }
}