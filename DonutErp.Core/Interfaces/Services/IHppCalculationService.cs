#nullable enable

using DonutErp.Core.Entities;
using DonutErp.Core.ValueObjects;

namespace DonutErp.Core.Interfaces.Services
{
    /// <summary>
    /// Advanced HPP (Harga Pokok Penjualan / Cost of Goods Sold) calculation service.
    /// Handles multi-level BOM, batch tracking, waste management, and variable cost allocation.
    /// </summary>
    public interface IHppCalculationService
    {
        /// <summary>
        /// Calculates HPP for a product based on current ingredient costs.
        /// Supports multi-level BOM (recipes within recipes).
        /// </summary>
        Task<(decimal StandardHpp, Dictionary<Guid, decimal> ComponentCosts)> CalculateHppForProductAsync(
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates actual HPP for a completed batch considering all waste and overhead costs.
        /// </summary>
        Task<BatchCostCalculation> CalculateBatchHppAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves multi-level BOM (e.g., "Adonan Dasar" is ingredient for "Donat Coklat").
        /// Returns flattened list of raw ingredients needed for a product.
        /// </summary>
        Task<List<(Guid IngredientId, double Quantity, double WastePercentage)>> ResolveBomAsync(
            Guid productId,
            double quantity = 1.0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets historical cost snapshot for an ingredient at a specific date.
        /// Essential for calculating HPP of past batches accurately.
        /// </summary>
        Task<CostSnapshot?> GetIngredientCostSnapshotAsync(
            Guid ingredientId,
            DateTime date,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Allocates production costs to each unit based on cost method (FIFO, LIFO, Weighted Average).
        /// </summary>
        Task<decimal> AllocateIngredientCostAsync(
            Guid ingredientId,
            double quantity,
            string costMethod = "FIFO",
            DateTime? atDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies waste percentage and calculates actual material cost including waste.
        /// </summary>
        Task<BatchIngredientAllocation> CalculateIngredientAllocationWithWasteAsync(
            Guid ingredientId,
            double quantityNeeded,
            double wastePercentage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Allocates indirect costs (labor, utilities, depreciation) to batch units.
        /// </summary>
        Task<Dictionary<string, decimal>> AllocateOverheadCostsAsync(
            Guid batchId,
            Dictionary<string, decimal> overheadCosts,
            int totalUnitsProduced,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Recalculates HPP for products affected by ingredient cost change.
        /// </summary>
        Task<List<Guid>> RecalculateAffectedProductsAsync(
            Guid ingredientId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates BOM integrity and detects circular dependencies or invalid configurations.
        /// </summary>
        Task<(bool IsValid, List<string> Issues)> ValidateBomAsync(
            Guid productId,
            CancellationToken cancellationToken = default);
    }
}
