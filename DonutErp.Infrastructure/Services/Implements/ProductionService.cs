#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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

        // =================================================================
        // 1. CREATE BATCH (MEMULAI PRODUKSI)
        // =================================================================
        // FIX: Ubah return type jadi Task<ProductionBatch>
        public async Task<ProductionBatch> CreateProductionBatchAsync(ProductionBatch batch, List<ProductionOutput> outputs)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // A. Simpan Header Batch
                if (batch.Id == Guid.Empty) batch.Id = Guid.NewGuid();

                // Hitung pemakaian minyak real
                double oilConsumed = (batch.OilLevelStartLiter + batch.OilAddedLiter) - batch.OilLevelEndLiter;
                if (oilConsumed < 0) oilConsumed = 0;
                batch.OilConsumedLiters = oilConsumed;

                // Hitung Cost Minyak
                var oilIngredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IsFryingOil);
                decimal oilPricePerLiter = 0;

                if (oilIngredient != null)
                {
                    double qtyToDeduct = oilConsumed * 1000; // Liter ke Gram/Ml

                    // Potong Stok Minyak
                    oilIngredient.CurrentStock -= qtyToDeduct;
                    _context.Ingredients.Update(oilIngredient);

                    // Hitung Biaya
                    oilPricePerLiter = oilIngredient.AvgCostPerUsageUnit * 1000;
                }

                batch.CalculatedOilCost = (decimal)oilConsumed * oilPricePerLiter;

                // B. Simpan Outputs & Potong Stok Bahan Baku
                decimal totalMaterialCost = 0;

                foreach (var output in outputs)
                {
                    output.Id = Guid.NewGuid();
                    output.ProductionBatchId = batch.Id;

                    var recipes = await _context.Recipes
                        .Where(r => r.ProductId == output.ProductId)
                        .ToListAsync();

                    decimal productHppBase = 0;

                    foreach (var r in recipes)
                    {
                        var ingredient = await _context.Ingredients.FindAsync(r.IngredientId);
                        if (ingredient != null)
                        {
                            // Potong Stok Bahan Baku
                            double totalQtyNeeded = r.Quantity * (output.QuantityGood + output.QuantityReject);
                            ingredient.CurrentStock -= totalQtyNeeded;
                            _context.Ingredients.Update(ingredient);

                            // Hitung Cost Bahan
                            productHppBase += ((decimal)r.Quantity * ingredient.AvgCostPerUsageUnit);
                        }
                    }

                    totalMaterialCost += (productHppBase * (output.QuantityGood + output.QuantityReject));

                    // Update HPP Final
                    decimal allocatedOilCost = (output.QuantityGood > 0)
                        ? (batch.CalculatedOilCost / outputs.Sum(o => o.QuantityGood))
                        : 0;

                    output.FinalHppPerUnit = productHppBase + allocatedOilCost;

                    await _context.ProductionOutputs.AddAsync(output);
                }

                // C. Finalisasi Header
                batch.TotalBatchCost = totalMaterialCost + batch.CalculatedOilCost;

                await _context.ProductionBatches.AddAsync(batch);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // FIX: Wajib return object batch
                return batch;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =================================================================
        // 2. GET RECENT BATCHES
        // =================================================================
        public async Task<List<ProductionBatch>> GetRecentBatchesAsync()
        {
            return await _context.ProductionBatches
                .Include(b => b.Outputs)
                .ThenInclude(o => o.Product)
                .OrderByDescending(b => b.ProductionDate)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();
        }

        // =================================================================
        // 3. CALCULATE THEORETICAL HPP
        // =================================================================
        public async Task<decimal> CalculateTheoreticalHppAsync(Guid productId)
        {
            var recipes = await _context.Recipes
                .Include(r => r.Ingredient)
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            decimal hpp = 0;
            foreach (var r in recipes)
            {
                if (r.Ingredient != null)
                {
                    hpp += (decimal)r.Quantity * r.Ingredient.AvgCostPerUsageUnit;
                }
            }
            return hpp;
        }

        // =================================================================
        // 4. CALCULATE OIL LOSS COST
        // =================================================================
        // FIX: Ubah return type jadi Task<decimal>
        public Task<decimal> CalculateOilLossCost(double startLevel, double endLevel, double added, decimal oilPricePerLiter)
        {
            double consumed = (startLevel + added) - endLevel;
            if (consumed < 0) consumed = 0;

            decimal cost = (decimal)consumed * oilPricePerLiter;

            // Bungkus hasil synchronous ke dalam Task agar sesuai Interface
            return Task.FromResult(cost);
        }
    }
}