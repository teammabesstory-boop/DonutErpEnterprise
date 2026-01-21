#nullable enable

using DonutErp.Core.ValueObjects;

namespace DonutErp.Core.Interfaces.Services
{
    /// <summary>
    /// Universal unit conversion service for food manufacturing.
    /// Handles weight, volume, and count units with high precision for recipe scaling.
    /// </summary>
    public interface IUnitConversionService
    {
        /// <summary>
        /// Converts quantity from one unit to another with high precision.
        /// Example: Convert 1 Sak (25kg) to grams for recipe.
        /// </summary>
        Task<double> ConvertAsync(
            string fromUnit,
            string toUnit,
            double quantity,
            string category = "Weight",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all supported conversion rules for a category.
        /// </summary>
        Task<List<UnitConversionRule>> GetConversionRulesAsync(
            string category,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a conversion rule in the system.
        /// </summary>
        Task<bool> SetConversionRuleAsync(
            UnitConversionRule rule,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a conversion path exists between two units.
        /// </summary>
        Task<bool> CanConvertAsync(
            string fromUnit,
            string toUnit,
            string category,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets base unit for a category (e.g., Gram for Weight, Mililiter for Volume).
        /// </summary>
        Task<string> GetBaseUnitAsync(
            string category,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Normalizes quantity to base unit for consistent internal calculations.
        /// </summary>
        Task<(double NormalizedQuantity, string BaseUnit)> NormalizeToBaseUnitAsync(
            string unit,
            double quantity,
            string category,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts from base unit to display unit with proper formatting.
        /// </summary>
        Task<string> FormatQuantityAsync(
            double baseQuantity,
            string targetUnit,
            string category,
            int decimalPlaces = 2,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets precision rules for a specific unit.
        /// </summary>
        Task<PrecisionRule?> GetPrecisionRuleAsync(
            string unit,
            CancellationToken cancellationToken = default);
    }
}
