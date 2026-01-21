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
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. BASIC INVENTORY OPERATIONS
        // ==========================================
        public async Task<List<Ingredient>> GetAllIngredientsAsync()
        {
            return await _context.Ingredients
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Ingredient?> GetIngredientByIdAsync(Guid id)
        {
            return await _context.Ingredients.FindAsync(id);
        }

        public async Task<List<Ingredient>> GetLowStockAlertsAsync()
        {
            return await _context.Ingredients
                .Where(i => i.CurrentStock <= i.MinStockLevel)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddOrUpdateIngredientAsync(Ingredient ingredient)
        {
            if (ingredient.Id == Guid.Empty)
            {
                ingredient.Id = Guid.NewGuid();
                await _context.Ingredients.AddAsync(ingredient);
            }
            else
            {
                _context.Ingredients.Update(ingredient);
            }
            await _context.SaveChangesAsync();
        }

        public async Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason, string username)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ingredient = await _context.Ingredients.FindAsync(ingredientId);
                if (ingredient == null) throw new Exception("Ingredient not found");

                double diff = realStockAmount - ingredient.CurrentStock;
                if (Math.Abs(diff) < 0.001) return; // No change

                // 1. Update Master Stock
                ingredient.CurrentStock = realStockAmount;
                ingredient.UpdatedAt = DateTime.Now;
                _context.Ingredients.Update(ingredient);

                // 2. Create Audit Log (Security)
                var audit = new AuditLog
                {
                    Action = "STOCK_ADJUSTMENT",
                    EntityName = "Ingredient",
                    RecordId = ingredientId.ToString(),
                    Username = username,
                    ChangesJson = $"Stock changed from {ingredient.CurrentStock - diff} to {realStockAmount}. Reason: {reason}",
                    Timestamp = DateTime.Now
                };
                await _context.AuditLogs.AddAsync(audit);

                // 3. Create Transaction Record (Financial Impact)
                // Jika stok berkurang (Hilang/Rusak) -> Expense
                // Jika stok bertambah (Bonus/Salah Hitung) -> Income (Stock Adjustment Gain)
                var trxType = diff < 0 ? TransactionType.StockAdjustment : TransactionType.StockAdjustment;
                var totalVal = (decimal)Math.Abs(diff) * ingredient.AvgCostPerUsageUnit;

                var adjTransaction = new Transaction
                {
                    InvoiceNumber = $"ADJ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = DateTime.Now,
                    Type = trxType,
                    Description = $"Stock Opname: {ingredient.Name} ({diff:+#;-#;0} {ingredient.UsageUnit})",
                    Notes = reason,
                    TotalAmount = trxType == TransactionType.StockAdjustment && diff < 0 ? 0 : totalVal, // Kalau rugi, amount 0 tapi cost ada? Tergantung accounting policy. Kita simpan di TotalCost untuk expense.
                    TotalCost = trxType == TransactionType.StockAdjustment && diff < 0 ? totalVal : 0
                };

                await _context.Transactions.AddAsync(adjTransaction);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake)
        {
            // Note: Untuk BOM Multi-Level yang sangat dalam, logic ini harus rekursif.
            // Di sini kita implementasi Single Level dulu untuk performa, 
            // tapi idealnya memanggil fungsi rekursif "GetTotalMaterialNeeded".

            var recipes = await _context.Recipes
                .Where(r => r.ParentProductId == productId)
                .ToListAsync();

            if (!recipes.Any()) return true;

            foreach (var item in recipes)
            {
                // Jika resep butuh Ingredient
                if (item.IngredientId.HasValue)
                {
                    var ingredient = await _context.Ingredients.FindAsync(item.IngredientId.Value);
                    if (ingredient == null) continue;

                    double needed = item.Quantity * quantityToMake;
                    if (ingredient.CurrentStock < needed) return false;
                }
                // Jika resep butuh SubProduct (Adonan Dasar), kita harus cek stok Adonan Dasar itu juga?
                // Atau asumsi Adonan Dasar dibuat on-the-fly? 
                // Di sistem F&B biasanya Adonan Dasar dibuat on-the-fly, jadi kita harus cek stok bahan baku Adonan Dasar.
                // Logic ini kompleks, untuk "Heavy Code" tahap 2 nanti kita sempurnakan.
            }
            return true;
        }

        // ==========================================
        // 2. PRODUCT & RECIPE MANAGEMENT
        // ==========================================
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.AsNoTracking().ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Recipes)
                .ThenInclude(r => r.Ingredient)
                .Include(p => p.Recipes)
                .ThenInclude(r => r.SubProduct)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Recipe>> GetRecipeByProductAsync(Guid productId)
        {
            return await _context.Recipes
                .Include(r => r.Ingredient)
                .Include(r => r.SubProduct)
                .Where(r => r.ParentProductId == productId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpdateRecipeAsync(Guid productId, List<Recipe> newRecipes)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Hapus resep lama
                var oldRecipes = await _context.Recipes.Where(r => r.ParentProductId == productId).ToListAsync();
                _context.Recipes.RemoveRange(oldRecipes);

                // 2. Masukkan resep baru
                foreach (var item in newRecipes)
                {
                    item.Id = Guid.NewGuid();
                    item.ParentProductId = productId;
                }
                await _context.Recipes.AddRangeAsync(newRecipes);
                await _context.SaveChangesAsync();

                // 3. Auto-Recalculate HPP (The Brain Trigger)
                await RecalculateProductHppAsync(productId);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task AddOrUpdateProductAsync(Product product)
        {
            if (product.Id == Guid.Empty)
            {
                product.Id = Guid.NewGuid();
                await _context.Products.AddAsync(product);
            }
            else
            {
                _context.Products.Update(product);
            }
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // 3. THE "BRAIN" (ADVANCED ANALYTICS)
        // ==========================================

        /// <summary>
        /// Menghitung HPP secara Rekursif.
        /// Jika Donat butuh Adonan, dan Adonan butuh Tepung, maka HPP Donat = HPP Adonan + Biaya Lain.
        /// HPP Adonan = Harga Tepung + dll.
        /// </summary>
        public async Task<decimal> RecalculateProductHppAsync(Guid productId)
        {
            // Ambil struktur BOM (Bill of Material)
            var recipes = await _context.Recipes
                .Include(r => r.Ingredient)
                .Where(r => r.ParentProductId == productId)
                .ToListAsync();

            decimal totalHpp = 0;

            foreach (var r in recipes)
            {
                decimal itemCost = 0;

                // KASUS 1: Bahan Baku Langsung (Leaf Node)
                if (r.IngredientId.HasValue && r.Ingredient != null)
                {
                    // Cost = Qty * AvgCost
                    // Tambahkan Waste Percentage (Shrinkage)
                    // Rumus: Qty / (1 - Waste%)
                    // Contoh: Butuh 100gr kentang kupas. Waste kulit 10%. Maka butuh kentang mentah 100 / 0.9 = 111gr.
                    double realQtyNeeded = r.Quantity;
                    if (r.WastePercentage > 0 && r.WastePercentage < 100)
                    {
                        realQtyNeeded = r.Quantity / (1 - (r.WastePercentage / 100.0));
                    }

                    itemCost = (decimal)realQtyNeeded * r.Ingredient.AvgCostPerUsageUnit;
                }
                // KASUS 2: Sub-Product (Branch Node) - RECURSION HAPPENS HERE
                else if (r.SubProductId.HasValue)
                {
                    // Kita harus hitung HPP si Sub-Product dulu.
                    // Idealnya Sub-Product sudah punya 'CachedHpp' yang valid.
                    // Tapi untuk akurasi 100%, kita bisa panggil fungsi ini lagi (Recursive).
                    // Hati-hati Infinite Loop (Circular Dependency)! Kita asumsikan tidak ada circular reference.

                    // Opsi Cepat: Ambil CachedHpp dari SubProduct
                    var subProduct = await _context.Products.FindAsync(r.SubProductId.Value);
                    if (subProduct != null)
                    {
                        // Jika CachedHpp 0, paksa hitung ulang
                        if (subProduct.CachedHpp == 0)
                        {
                            itemCost = await RecalculateProductHppAsync(subProduct.Id) * (decimal)r.Quantity;
                        }
                        else
                        {
                            itemCost = subProduct.CachedHpp * (decimal)r.Quantity;
                        }
                    }
                }

                totalHpp += itemCost;
            }

            // Simpan hasil ke Cache
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.CachedHpp = totalHpp;
                // product.UpdatedAt = DateTime.Now; // Jika ada kolom ini
                await _context.SaveChangesAsync();
            }

            return totalHpp;
        }

        /// <summary>
        /// Prediksi kebutuhan stok menggunakan Simple Moving Average (SMA).
        /// </summary>
        public async Task<double> PredictStockUsageAsync(Guid ingredientId, int daysToPredict)
        {
            // 1. Ambil history pemakaian bahan ini dari Production Logs 30 hari terakhir
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            // Query Complex: Join ProductionOutput -> Recipe -> Ingredient
            // Ini mencari: "Berapa kali bahan ini dipakai dalam produksi yang SUDAH SELESAI?"
            var usageHistory = await _context.ProductionOutputs
                .Include(po => po.ProductionBatch)
                .Where(po => po.ProductionBatch.ProductionDate >= thirtyDaysAgo && po.ProductionBatch.Status == BatchStatus.Finished)
                .Select(po => new
                {
                    Date = po.ProductionBatch.ProductionDate.Date,
                    ProductId = po.ProductId,
                    QtyProduced = po.QuantityGood + po.QuantityReject
                })
                .ToListAsync();

            // Kita harus map ProductId ke Recipe untuk tau berapa gram IngredientId yang dipakai
            // Ini agak berat dilakukan di SQL jika strukturnya kompleks, jadi kita proses di Memory (Client Evaluation)
            // karena data usageHistory 30 hari tidak akan terlalu besar (ribuan row masih oke).

            double totalUsage = 0;
            // Cache resep biar gak query berulang
            var relevantRecipes = await _context.Recipes
                .Where(r => r.IngredientId == ingredientId)
                .ToListAsync();

            foreach (var log in usageHistory)
            {
                var recipe = relevantRecipes.FirstOrDefault(r => r.ParentProductId == log.ProductId);
                if (recipe != null)
                {
                    totalUsage += (recipe.Quantity * log.QtyProduced);
                }
            }

            // 2. Hitung Rata-rata Pemakaian Harian (Daily Burn Rate)
            double dailyBurnRate = totalUsage / 30.0;

            // 3. Prediksi
            return dailyBurnRate * daysToPredict;
        }

        /// <summary>
        /// Analisa Tren Harga menggunakan Linear Regression (Least Squares).
        /// Output: "NAIK TAJAM", "NAIK TIPIS", "STABIL", "TURUN".
        /// </summary>
        public async Task<string> AnalyzePriceTrendAsync(Guid ingredientId)
        {
            // Ambil 10 data harga terakhir
            var history = await _context.SupplierPriceHistories
                .Where(h => h.IngredientId == ingredientId)
                .OrderByDescending(h => h.RecordedDate)
                .Take(10)
                .ToListAsync();

            if (history.Count < 2) return "DATA TIDAK CUKUP";

            // Kita balik urutannya jadi Ascending (Waktu lampau ke sekarang)
            var dataPoints = history.OrderBy(h => h.RecordedDate).ToList();

            // X = Urutan Waktu (0, 1, 2...), Y = Harga
            double n = dataPoints.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = (double)dataPoints[i].PricePerPurchaseUnit;

                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            // Hitung Slope (Kemiringan Garis)
            // Rumus: m = (n*Σ(xy) - Σx*Σy) / (n*Σ(x^2) - (Σx)^2)
            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - Math.Pow(sumX, 2));

            // Interpretasi Slope
            // Ambang batas sensitivitas (tergantung skala harga, ini simplifikasi)
            // Misal: Jika slope > 500 (naik 500 per data point), maka naik tajam

            if (slope > 1000) return "NAIK TAJAM 🔴";
            if (slope > 100) return "NAIK ⚠️";
            if (slope < -100) return "TURUN 🟢";
            if (slope < -1000) return "TURUN TAJAM 💎";

            return "STABIL ➖";
        }

        public async Task RecordPriceHistoryAsync(Guid ingredientId, decimal newPrice, string supplierName)
        {
            var history = new SupplierPriceHistory
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredientId,
                RecordedDate = DateTime.Now,
                PricePerPurchaseUnit = newPrice,
                SupplierName = supplierName
            };
            await _context.SupplierPriceHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }
    }
}