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
    public class ProductionService : IProductionService
    {
        private readonly AppDbContext _context;

        public ProductionService(AppDbContext context)
        {
            _context = context;
        }

        // =============================================================
        // IMPLEMENTASI INTERFACE (Fix Error CS0535)
        // =============================================================
        public Task<decimal> CalculateOilLossCost(double startLevel, double endLevel, double addedLiter, decimal pricePerLiter)
        {
            double consumed = (startLevel - endLevel) + addedLiter;
            if (consumed < 0) consumed = 0;

            decimal cost = (decimal)consumed * pricePerLiter;

            // Bungkus decimal jadi Task biar interface senang
            return Task.FromResult(cost);
        }

        // =============================================================
        // LOGIKA UTAMA
        // =============================================================

        public async Task<decimal> CalculateTheoreticalHppAsync(Guid productId)
        {
            // Ambil resep produk
            var recipes = await _context.Recipes
                .Include(r => r.Ingredient)
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            if (!recipes.Any()) return 0;

            decimal totalCost = 0;
            foreach (var r in recipes)
            {
                // Rumus: Harga Rata2 Bahan x Jumlah Pakai
                totalCost += r.Ingredient.AvgCostPerUsageUnit * (decimal)r.Quantity;
            }

            return totalCost;
        }

        public async Task<ProductionBatch> CreateProductionBatchAsync(ProductionBatch batch, List<ProductionOutput> outputs)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. HITUNG BIAYA MINYAK
                // Rumus: (Awal - Akhir + Tambah)
                batch.OilConsumedLiters = (batch.OilLevelStartLiter - batch.OilLevelEndLiter) + batch.OilAddedLiter;
                if (batch.OilConsumedLiters < 0) batch.OilConsumedLiters = 0; // Guard clause

                // Cari harga minyak (Kita asumsikan ada bahan baku bernama 'Minyak Goreng')
                // Kalau tidak ada, pakai default cost Rp 14.000/liter
                var oilIngredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Name.Contains("Minyak") || i.Name.Contains("Oil"));

                decimal oilPrice = oilIngredient?.AvgCostPerUsageUnit ?? 14000m;
                batch.CalculatedOilCost = (decimal)batch.OilConsumedLiters * oilPrice;

                // 2. HITUNG HPP PRODUK & POTONG STOK (Backflush)
                decimal totalIngredientsCost = 0;

                foreach (var item in outputs)
                {
                    // Ambil Resep
                    var recipes = await _context.Recipes
                        .Include(r => r.Ingredient)
                        .Where(r => r.ProductId == item.ProductId)
                        .ToListAsync();

                    foreach (var r in recipes)
                    {
                        // Total bahan yang dibutuhkan untuk output ini (Good + Reject tetap makan bahan)
                        double totalQtyNeeded = r.Quantity * (item.QuantityGood + item.QuantityReject);

                        // Potong Stok Gudang
                        r.Ingredient.CurrentStock -= totalQtyNeeded;

                        // Catat Biaya
                        totalIngredientsCost += r.Ingredient.AvgCostPerUsageUnit * (decimal)totalQtyNeeded;

                        // Update Stok di DB
                        _context.Ingredients.Update(r.Ingredient);
                    }

                    // Link ke Batch
                    item.ProductionBatchId = batch.Id;
                    // Reset object Product biar gak duplicate insert error
                    item.Product = null;

                    _context.ProductionOutputs.Add(item);
                }

                // 3. FINALISASI BATCH
                batch.TotalBatchCost = batch.CalculatedOilCost + totalIngredientsCost;

                _context.ProductionBatches.Add(batch);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return batch;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ProductionBatch>> GetRecentBatchesAsync()
        {
            return await _context.ProductionBatches
                .OrderByDescending(b => b.ProductionDate)
                .Take(20)
                .ToListAsync();
        }
    }
}