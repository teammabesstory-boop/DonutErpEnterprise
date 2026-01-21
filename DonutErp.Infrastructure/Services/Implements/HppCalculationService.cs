#nullable enable

using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Core.ValueObjects;
using DonutErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonutErp.Infrastructure.Services.Implements
{
    /// <summary>
    /// Advanced HPP calculation engine supporting multi-level BOM, batch tracking, and waste management.
    /// This is the core financial engine that drives accurate product costing.
    /// </summary>
    public class HppCalculationService : IHppCalculationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IUnitConversionService _unitConversionService;
        private readonly Dictionary<string, List<(Guid IngredientId, double Quantity, double Waste)>> _bomCache;

        public HppCalculationService(
            AppDbContext dbContext,
            IUnitConversionService unitConversionService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _unitConversionService = unitConversionService ?? throw new ArgumentNullException(nameof(unitConversionService));
            _bomCache = new();
        }

        public async Task<(decimal StandardHpp, Dictionary<Guid, decimal> ComponentCosts)> 
            CalculateHppForProductAsync(
                Guid productId,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(productId));

            var product = await _dbContext.Products
                .Include(p => p.Recipes)
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
                ?? throw new InvalidOperationException($"Product {productId} not found");

            var bomItems = await ResolveBomAsync(productId, 1.0, cancellationToken);
            var componentCosts = new Dictionary<Guid, decimal>();
            decimal totalHpp = 0;

            foreach (var (ingredientId, quantity, wastePercent) in bomItems)
            {
                var ingredient = await _dbContext.Ingredients
                    .FirstOrDefaultAsync(i => i.Id == ingredientId, cancellationToken)
                    ?? throw new InvalidOperationException($"Ingredient {ingredientId} not found");

                // Calculate actual quantity needed including waste
                var actualQuantityNeeded = quantity * (1 + (wastePercent / 100.0));

                // Get current cost per unit
                var costPerUnit = ingredient.AvgCostPerUsageUnit;

                // Calculate component cost
                var componentCost = costPerUnit * (decimal)actualQuantityNeeded;

                componentCosts[ingredientId] = componentCost;
                totalHpp += componentCost;
            }

            // Cache the standard HPP
            product.CachedHpp = totalHpp;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return (totalHpp, componentCosts);
        }

        public async Task<BatchCostCalculation> CalculateBatchHppAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            var batch = await _dbContext.Set<ProductionBatch>()
                .Include(b => b.Outputs)
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
                ?? throw new InvalidOperationException($"Batch {batchId} not found");

            var costSnapshots = await _dbContext.Set<BatchCostSnapshot>()
                .Where(cs => cs.ProductionBatchId == batchId)
                .ToListAsync(cancellationToken);

            var overheadAllocations = await _dbContext.Set<BatchOverheadAllocation>()
                .Where(oa => oa.ProductionBatchId == batchId)
                .FirstOrDefaultAsync(cancellationToken);

            // Calculate material costs
            decimal rawMaterialCost = 0;
            decimal wasteMaterialCost = 0;

            foreach (var snapshot in costSnapshots)
            {
                rawMaterialCost += snapshot.TotalMaterialCost;
                wasteMaterialCost += snapshot.TotalWasteCost;
            }

            // Get overhead costs
            var laborCost = overheadAllocations?.LaborCostAllocated ?? 0;
            var utilityCost = overheadAllocations?.UtilityCostAllocated ?? 0;
            var oilCost = overheadAllocations?.OilCostAllocated ?? 0;
            var depreciationCost = overheadAllocations?.DepreciationCostAllocated ?? 0;

            // Calculate unit counts
            var totalGoodUnits = batch.Outputs.Sum(o => o.QuantityGood);
            var totalRejectUnits = batch.Outputs.Sum(o => o.QuantityReject);
            var totalUnits = totalGoodUnits + totalRejectUnits;

            var calculation = new BatchCostCalculation
            {
                BatchId = batchId,
                CalculatedAt = DateTime.Now,
                RawMaterialCost = rawMaterialCost,
                WasteMaterialCost = wasteMaterialCost,
                LaborCostAllocated = laborCost,
                UtilityCostAllocated = utilityCost,
                OilCostAllocated = oilCost,
                DeprecationCostAllocated = depreciationCost,
                TotalUnitProduced = totalUnits,
                TotalUnitGood = totalGoodUnits,
                TotalUnitReject = totalRejectUnits
            };

            // Update product outputs with actual HPP
            foreach (var output in batch.Outputs)
            {
                output.ActualHppPerUnit = calculation.HppWithRejectAllocation;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return calculation;
        }

        public async Task<List<(Guid IngredientId, double Quantity, double WastePercentage)>> 
            ResolveBomAsync(
                Guid productId,
                double quantity = 1.0,
                CancellationToken cancellationToken = default)
        {
            var cacheKey = productId.ToString();
            
            if (_bomCache.TryGetValue(cacheKey, out var cached))
            {
                return cached.Select(item => 
                    (item.IngredientId, item.Quantity * quantity, item.Waste)).ToList();
            }

            var resolved = new List<(Guid, double, double)>();
            var visited = new HashSet<Guid>();

            await ResolveBomRecursiveAsync(
                productId,
                quantity,
                resolved,
                visited,
                cancellationToken);

            _bomCache[cacheKey] = resolved.ToList();

            return resolved;
        }

        private async Task ResolveBomRecursiveAsync(
            Guid productId,
            double quantity,
            List<(Guid IngredientId, double Quantity, double Waste)> resolved,
            HashSet<Guid> visited,
            CancellationToken cancellationToken)
        {
            if (visited.Contains(productId))
                throw new InvalidOperationException($"Circular dependency detected in BOM for product {productId}");

            visited.Add(productId);

            var recipes = await _dbContext.Set<Recipe>()
                .Where(r => r.ParentProductId == productId)
                .ToListAsync(cancellationToken);

            foreach (var recipe in recipes)
            {
                var actualQuantity = quantity * recipe.Quantity;

                if (recipe.IngredientId.HasValue)
                {
                    // Direct ingredient
                    var existing = resolved.FirstOrDefault(r => r.IngredientId == recipe.IngredientId.Value);
                    
                    if (existing == default)
                    {
                        resolved.Add((recipe.IngredientId.Value, actualQuantity, recipe.WastePercentage));
                    }
                    else
                    {
                        var index = resolved.IndexOf(existing);
                        resolved[index] = (existing.IngredientId, 
                            existing.Quantity + actualQuantity, existing.Waste);
                    }
                }
                else if (recipe.SubProductId.HasValue)
                {
                    // Sub-product (multi-level BOM)
                    await ResolveBomRecursiveAsync(
                        recipe.SubProductId.Value,
                        actualQuantity,
                        resolved,
                        visited,
                        cancellationToken);
                }
            }

            visited.Remove(productId);
        }

        public async Task<CostSnapshot?> GetIngredientCostSnapshotAsync(
            Guid ingredientId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var ingredient = await _dbContext.Ingredients
                .FirstOrDefaultAsync(i => i.Id == ingredientId, cancellationToken);

            if (ingredient == null)
                return null;

            // Get the latest price history before the specified date
            var priceHistory = await _dbContext.Set<SupplierPriceHistory>()
                .Where(ph => ph.IngredientId == ingredientId && ph.RecordedDate <= date)
                .OrderByDescending(ph => ph.RecordedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (priceHistory == null)
            {
                // Use average cost as fallback
                return new CostSnapshot
                {
                    IngredientId = ingredientId,
                    SnapshotDate = date,
                    CostPerUnit = ingredient.AvgCostPerUsageUnit,
                    SupplierPrice = ingredient.LastPurchasePrice,
                    QuantityAtCost = ingredient.CurrentStock,
                    CostMethod = "WeightedAverage"
                };
            }

            return new CostSnapshot
            {
                IngredientId = ingredientId,
                SnapshotDate = date,
                CostPerUnit = (decimal)priceHistory.PricePerPurchaseUnit,
                SupplierPrice = (decimal)priceHistory.PricePerPurchaseUnit,
                QuantityAtCost = ingredient.CurrentStock,
                CostMethod = "FIFO",
                SupplierName = priceHistory.SupplierName
            };
        }

        public async Task<decimal> AllocateIngredientCostAsync(
            Guid ingredientId,
            double quantity,
            string costMethod = "FIFO",
            DateTime? atDate = null,
            CancellationToken cancellationToken = default)
        {
            var ingredient = await _dbContext.Ingredients
                .FirstOrDefaultAsync(i => i.Id == ingredientId, cancellationToken)
                ?? throw new InvalidOperationException($"Ingredient {ingredientId} not found");

            var costDate = atDate ?? DateTime.Now;

            // Get cost snapshot for the date
            var snapshot = await GetIngredientCostSnapshotAsync(ingredientId, costDate, cancellationToken)
                ?? throw new InvalidOperationException($"No cost snapshot available for ingredient {ingredientId}");

            return snapshot.CostPerUnit * (decimal)quantity;
        }

        public async Task<BatchIngredientAllocation> CalculateIngredientAllocationWithWasteAsync(
            Guid ingredientId,
            double quantityNeeded,
            double wastePercentage,
            CancellationToken cancellationToken = default)
        {
            var ingredient = await _dbContext.Ingredients
                .FirstOrDefaultAsync(i => i.Id == ingredientId, cancellationToken)
                ?? throw new InvalidOperationException($"Ingredient {ingredientId} not found");

            var wasteQuantity = quantityNeeded * (wastePercentage / 100.0);
            var totalQuantity = quantityNeeded + wasteQuantity;

            var costPerUnit = ingredient.AvgCostPerUsageUnit;

            return new BatchIngredientAllocation
            {
                IngredientId = ingredientId,
                QuantityUsed = quantityNeeded,
                WasteQuantity = wasteQuantity,
                CostPerUnit = costPerUnit,
                WastePercentage = wastePercentage
            };
        }

        public async Task<Dictionary<string, decimal>> AllocateOverheadCostsAsync(
            Guid batchId,
            Dictionary<string, decimal> overheadCosts,
            int totalUnitsProduced,
            CancellationToken cancellationToken = default)
        {
            if (totalUnitsProduced <= 0)
                throw new ArgumentException("Total units produced must be greater than 0", nameof(totalUnitsProduced));

            var allocation = new Dictionary<string, decimal>();

            foreach (var (costType, amount) in overheadCosts)
            {
                allocation[costType] = amount / totalUnitsProduced;
            }

            return allocation;
        }

        public async Task<List<Guid>> RecalculateAffectedProductsAsync(
            Guid ingredientId,
            CancellationToken cancellationToken = default)
        {
            var affectedProductIds = new HashSet<Guid>();

            // Find all recipes that use this ingredient directly
            var directRecipes = await _dbContext.Set<Recipe>()
                .Where(r => r.IngredientId == ingredientId)
                .Select(r => r.ParentProductId)
                .ToListAsync(cancellationToken);

            affectedProductIds.UnionWith(directRecipes);

            // Find all products that use products that contain this ingredient (multi-level)
            var allProducts = await _dbContext.Products.ToListAsync(cancellationToken);
            
            foreach (var product in allProducts)
            {
                try
                {
                    var bom = await ResolveBomAsync(product.Id, 1.0, cancellationToken);
                    if (bom.Any(item => item.IngredientId == ingredientId))
                    {
                        affectedProductIds.Add(product.Id);
                    }
                }
                catch
                {
                    // Skip products with invalid BOM
                }
            }

            // Recalculate HPP for all affected products
            foreach (var productId in affectedProductIds)
            {
                await CalculateHppForProductAsync(productId, cancellationToken);
            }

            return affectedProductIds.ToList();
        }

        public async Task<(bool IsValid, List<string> Issues)> ValidateBomAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var issues = new List<string>();
            var visited = new HashSet<Guid>();

            try
            {
                await ResolveBomAsync(productId, 1.0, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                issues.Add($"BOM Integrity Error: {ex.Message}");
                return (false, issues);
            }

            var recipes = await _dbContext.Set<Recipe>()
                .Where(r => r.ParentProductId == productId)
                .ToListAsync(cancellationToken);

            if (!recipes.Any())
            {
                issues.Add("Product has no recipes defined");
            }

            foreach (var recipe in recipes)
            {
                if (recipe.IngredientId == null && recipe.SubProductId == null)
                {
                    issues.Add($"Recipe {recipe.Id} has neither ingredient nor sub-product reference");
                }

                if (recipe.Quantity <= 0)
                {
                    issues.Add($"Recipe {recipe.Id} has invalid quantity {recipe.Quantity}");
                }

                if (recipe.WastePercentage < 0 || recipe.WastePercentage > 100)
                {
                    issues.Add($"Recipe {recipe.Id} has invalid waste percentage {recipe.WastePercentage}");
                }
            }

            return (issues.Count == 0, issues);
        }

        public void ClearCache()
        {
            _bomCache.Clear();
        }
    }
}
