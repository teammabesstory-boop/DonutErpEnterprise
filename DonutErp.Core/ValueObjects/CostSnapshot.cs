#nullable enable

namespace DonutErp.Core.ValueObjects
{
    /// <summary>
    /// Represents the cost breakdown for a batch or ingredient at a specific point in time.
    /// This is crucial for accurate historical cost tracking in food manufacturing.
    /// </summary>
    public record CostSnapshot
    {
        public Guid IngredientId { get; init; }
        public DateTime SnapshotDate { get; init; }
        public decimal CostPerUnit { get; init; }
        public decimal SupplierPrice { get; init; }
        public double QuantityAtCost { get; init; }
        public string CostMethod { get; init; } = "FIFO"; // FIFO, LIFO, WeightedAverage, StandardCost
        public string? SupplierName { get; init; }
        
        public decimal TotalCost => CostPerUnit * (decimal)QuantityAtCost;
    }

    /// <summary>
    /// Represents actual ingredient consumption in a batch with precise tracking.
    /// </summary>
    public record BatchIngredientAllocation
    {
        public Guid IngredientId { get; init; }
        public double QuantityUsed { get; init; }
        public double WasteQuantity { get; init; }
        public decimal CostPerUnit { get; init; }
        public double WastePercentage { get; init; } // e.g., 5 for 5%
        
        public decimal MaterialCost => CostPerUnit * (decimal)QuantityUsed;
        public decimal WasteCost => CostPerUnit * (decimal)WasteQuantity;
        public decimal TotalCost => MaterialCost + WasteCost;
        public double ActualYield => QuantityUsed / (QuantityUsed + WasteQuantity);
    }

    /// <summary>
    /// Comprehensive batch cost calculation with all overhead allocations.
    /// </summary>
    public record BatchCostCalculation
    {
        public Guid BatchId { get; init; }
        public DateTime CalculatedAt { get; init; }
        
        // Material Costs
        public decimal RawMaterialCost { get; init; }
        public decimal WasteMaterialCost { get; init; }
        
        // Overhead Costs
        public decimal LaborCostAllocated { get; init; }
        public decimal UtilityCostAllocated { get; init; }
        public decimal OilCostAllocated { get; init; }
        public decimal DeprecationCostAllocated { get; init; }
        
        // Final Calculations
        public int TotalUnitProduced { get; init; }
        public int TotalUnitGood { get; init; }
        public int TotalUnitReject { get; init; }
        
        public decimal TotalManufacturingCost => 
            RawMaterialCost + WasteMaterialCost + LaborCostAllocated + 
            UtilityCostAllocated + OilCostAllocated + DeprecationCostAllocated;
        
        public decimal HppPerGoodUnit => 
            TotalUnitGood > 0 ? TotalManufacturingCost / TotalUnitGood : 0;
        
        public decimal HppWithRejectAllocation => 
            TotalUnitProduced > 0 ? TotalManufacturingCost / TotalUnitProduced : 0;
        
        public double YieldRate => 
            TotalUnitProduced > 0 ? (double)TotalUnitGood / TotalUnitProduced : 0;
    }
}
