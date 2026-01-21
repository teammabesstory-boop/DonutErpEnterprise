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
    public class ProductionService : IProductionService
    {
        private readonly AppDbContext _context;

        public ProductionService(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. BATCH LIFECYCLE MANAGEMENT
        // ==========================================

        public async Task<List<ProductionBatch>> GetActiveBatchesAsync()
        {
            return await _context.ProductionBatches
                .Include(b => b.Outputs).ThenInclude(o => o.Product)
                .Where(b => b.Status != BatchStatus.Finished && b.Status != BatchStatus.Failed)
                .OrderByDescending(b => b.ProductionDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProductionBatch?> GetBatchByIdAsync(Guid id)
        {
            return await _context.ProductionBatches
                .Include(b => b.Outputs).ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<ProductionBatch> CreatePlannedBatchAsync(string batchCode, string? notes)
        {
            var batch = new ProductionBatch
            {
                Id = Guid.NewGuid(),
                BatchCode = batchCode,
                ProductionDate = DateTime.Now,
                Status = BatchStatus.Planned,
                Notes = notes,
                OilLevelStartLiter = 0,
                OilLevelEndLiter = 0,
                OilAddedLiter = 0,
                OilConsumedLiters = 0
            };

            await _context.ProductionBatches.AddAsync(batch);
            await _context.SaveChangesAsync();
            return batch;
        }

        public async Task StartBatchAsync(Guid batchId, double oilStartLevelLiter)
        {
            var batch = await _context.ProductionBatches.FindAsync(batchId);
            if (batch == null) throw new Exception("Batch not found");
            if (batch.Status != BatchStatus.Planned) throw new Exception("Batch already started or finished.");

            batch.Status = BatchStatus.InProgress;
            batch.OilLevelStartLiter = oilStartLevelLiter;
            batch.ProductionDate = DateTime.Now; // Update timestamp to actual start

            _context.ProductionBatches.Update(batch);
            await _context.SaveChangesAsync();
        }

        public async Task RefillOilAsync(Guid batchId, double litersAdded)
        {
            var batch = await _context.ProductionBatches.FindAsync(batchId);
            if (batch == null) throw new Exception("Batch not found");

            batch.OilAddedLiter += litersAdded;

            // Catat history kecil (optional, bisa lewat audit log)
            // Disini kita langsung update state aja
            _context.ProductionBatches.Update(batch);
            await _context.SaveChangesAsync();
        }

        public async Task AddOutputAsync(Guid batchId, Guid productId, int qtyGood, int qtyReject)
        {
            var batch = await _context.ProductionBatches.FindAsync(batchId);
            if (batch == null) throw new Exception("Batch not found");

            var output = new ProductionOutput
            {
                Id = Guid.NewGuid(),
                ProductionBatchId = batchId,
                ProductId = productId,
                QuantityGood = qtyGood,
                QuantityReject = qtyReject,
                ActualHppPerUnit = 0 // Belum dihitung
            };

            await _context.ProductionOutputs.AddAsync(output);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// THE CORE LOGIC: Finalisasi Produksi & Hitung HPP
        /// Melakukan:
        /// 1. Hitung konsumsi minyak
        /// 2. Potong stok bahan baku (Backflushing)
        /// 3. Potong stok minyak
        /// 4. Hitung biaya Variable (Labor + Utilities)
        /// 5. Update ActualHppPerUnit setiap produk
        /// </summary>
        public async Task<ProductionBatch> CompleteBatchAsync(Guid batchId, double oilEndLevelLiter, decimal laborCost, decimal utilitiesCost, string username)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Load Data Batch Lengkap
                var batch = await _context.ProductionBatches
                    .Include(b => b.Outputs)
                    .FirstOrDefaultAsync(b => b.Id == batchId);

                if (batch == null) throw new Exception("Batch not found");
                if (batch.Status == BatchStatus.Finished) throw new Exception("Batch already finished.");

                // 2. Finalisasi Minyak & Overhead
                batch.OilLevelEndLiter = oilEndLevelLiter;
                batch.OilConsumedLiters = (batch.OilLevelStartLiter + batch.OilAddedLiter) - oilEndLevelLiter;
                if (batch.OilConsumedLiters < 0) batch.OilConsumedLiters = 0; // Guard clause

                batch.LaborCost = laborCost;
                batch.UtilitiesCost = utilitiesCost;
                batch.Status = BatchStatus.Finished;

                // 3. Hitung Cost Minyak Real
                var oilIngredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IsFryingOil);
                decimal totalOilCost = 0;

                if (oilIngredient != null && batch.OilConsumedLiters > 0)
                {
                    // Konversi Liter ke UsageUnit (misal: Liter -> Ml/Gram)
                    // Asumsi UsageUnit minyak adalah "Mililiter" atau "Gram" dengan rasio ~1000 per liter
                    // Logic konversi sederhana: 
                    double usageAmount = batch.OilConsumedLiters * 1000; // Liter ke ML

                    // Potong Stok
                    oilIngredient.CurrentStock -= usageAmount;

                    // Hitung Cost: QtyPakai * HargaRata2
                    totalOilCost = (decimal)usageAmount * oilIngredient.AvgCostPerUsageUnit;

                    _context.Ingredients.Update(oilIngredient);
                }
                batch.CalculatedOilCost = totalOilCost;

                // 4. Backflushing Bahan Baku & Costing per Produk
                decimal totalMaterialCostBatch = 0;
                int totalGoodUnitsBatch = batch.Outputs.Sum(o => o.QuantityGood);

                foreach (var output in batch.Outputs)
                {
                    // Ambil Resep Produk ini
                    var recipes = await _context.Recipes
                        .Include(r => r.Ingredient)
                        .Include(r => r.SubProduct) // Support Multi-level, though we usually flatten it before prod
                        .Where(r => r.ParentProductId == output.ProductId)
                        .ToListAsync();

                    decimal productMaterialCostPerUnit = 0;

                    foreach (var r in recipes)
                    {
                        // Logic Bahan Baku
                        if (r.Ingredient != null)
                        {
                            // Hitung Total Kebutuhan (Good + Reject)
                            // Reject tetap memakan bahan baku!
                            double totalQtyNeeded = r.Quantity * (output.QuantityGood + output.QuantityReject);

                            // Masukkan faktor waste shrinkage resep (misal kulit telur)
                            if (r.WastePercentage > 0)
                            {
                                totalQtyNeeded = totalQtyNeeded / (1 - (r.WastePercentage / 100.0));
                            }

                            // Potong Stok
                            r.Ingredient.CurrentStock -= totalQtyNeeded;
                            _context.Ingredients.Update(r.Ingredient);

                            // Hitung Cost
                            productMaterialCostPerUnit += ((decimal)r.Quantity * r.Ingredient.AvgCostPerUsageUnit);
                            // Note: Cost dihitung berdasarkan resep standar per unit, 
                            // waste reject nanti akan menaikkan HPP Good Unit secara total batch.
                        }
                    }

                    // Total Material Cost untuk Output ini
                    decimal totalMatCostForOutput = productMaterialCostPerUnit * (output.QuantityGood + output.QuantityReject);
                    totalMaterialCostBatch += totalMatCostForOutput;

                    // 5. Alokasi Overhead (Oil + Labor + Utils) ke Produk ini
                    // Metode: Alokasi berdasarkan QUANTITY. (Bisa juga berdasarkan Berat/Waktu, tapi Qty paling umum).
                    // Rumus: (TotalOverhead / TotalGoodUnitsBatch)

                    decimal totalOverheadBatch = totalOilCost + laborCost + utilitiesCost;

                    decimal overheadPerUnit = 0;
                    if (totalGoodUnitsBatch > 0)
                    {
                        overheadPerUnit = totalOverheadBatch / totalGoodUnitsBatch;
                    }

                    // 6. Hitung Actual HPP Per Unit (Good Units menyerap cost Reject)
                    // Rumus: (TotalMaterialUsed + AllocatedOverhead) / QuantityGood
                    // Jika QuantityGood 0 (Gagal Total), Cost jadi Infinite/Loss.

                    if (output.QuantityGood > 0)
                    {
                        // Cost Material Total (termasuk yang kebuang di reject) dibagi ke Good Units
                        decimal realMaterialCostPerGoodUnit = totalMatCostForOutput / output.QuantityGood;

                        output.ActualHppPerUnit = realMaterialCostPerGoodUnit + overheadPerUnit;
                    }
                    else
                    {
                        output.ActualHppPerUnit = 0; // Failed batch
                    }
                }

                batch.TotalBatchCost = totalMaterialCostBatch + totalOilCost + laborCost + utilitiesCost;

                // Audit Log
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = "PRODUCTION_FINISH",
                    EntityName = "ProductionBatch",
                    RecordId = batch.Id.ToString(),
                    Username = username,
                    ChangesJson = $"Cost: {batch.TotalBatchCost:C2}, Oil: {batch.OilConsumedLiters}L",
                    Timestamp = DateTime.Now
                });

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

        // ==========================================
        // 2. ANALYTICS & REPORTING
        // ==========================================

        public async Task<List<ProductionBatch>> GetBatchHistoryAsync(DateTime from, DateTime to)
        {
            return await _context.ProductionBatches
                .Include(b => b.Outputs).ThenInclude(o => o.Product)
                .Where(b => b.ProductionDate >= from && b.ProductionDate <= to && b.Status == BatchStatus.Finished)
                .OrderByDescending(b => b.ProductionDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<decimal> CompareTheoreticalVsActualCostAsync(Guid batchId)
        {
            // Fitur Analytics: Bandingkan HPP System vs Realisasi
            // Berguna untuk mendeteksi inefisiensi/pencurian

            var batch = await _context.ProductionBatches
                .Include(b => b.Outputs)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null) return 0;

            decimal theoreticalTotal = 0;

            foreach (var output in batch.Outputs)
            {
                // Ambil cached HPP standard
                var product = await _context.Products.FindAsync(output.ProductId);
                if (product != null)
                {
                    theoreticalTotal += (product.CachedHpp * output.QuantityGood);
                }
            }

            // Return Variance (Selisih)
            // Positif = Boros (Actual > Theory), Negatif = Hemat
            return batch.TotalBatchCost - theoreticalTotal;
        }
    }
}