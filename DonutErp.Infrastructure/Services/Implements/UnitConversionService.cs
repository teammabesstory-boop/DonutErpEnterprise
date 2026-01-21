#nullable enable

using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Core.ValueObjects;
using DonutErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonutErp.Infrastructure.Services.Implements
{
    /// <summary>
    /// Advanced unit conversion service with caching and precision handling.
    /// Supports food industry requirements with decimal precision for recipes.
    /// </summary>
    public class UnitConversionService : IUnitConversionService
    {
        private readonly AppDbContext _dbContext;
        private readonly Dictionary<string, UnitConversionRule> _conversionCache;
        private readonly Dictionary<string, PrecisionRule> _precisionCache;
        private readonly Dictionary<string, string> _baseUnitCache;

        public UnitConversionService(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _conversionCache = new();
            _precisionCache = new();
            _baseUnitCache = new();
            
            InitializeStandardConversions();
        }

        public async Task<double> ConvertAsync(
            string fromUnit,
            string toUnit,
            double quantity,
            string category = "Weight",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fromUnit))
                throw new ArgumentException("From unit cannot be empty", nameof(fromUnit));
            if (string.IsNullOrWhiteSpace(toUnit))
                throw new ArgumentException("To unit cannot be empty", nameof(toUnit));
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative", nameof(quantity));

            if (fromUnit.Equals(toUnit, StringComparison.OrdinalIgnoreCase))
                return quantity;

            // Try direct conversion
            var cacheKey = $"{fromUnit.ToLower()}_{toUnit.ToLower()}_{category.ToLower()}";
            
            if (_conversionCache.TryGetValue(cacheKey, out var rule))
            {
                return rule.Convert(quantity);
            }

            // Load from database
            var conversionRule = await _dbContext.Set<DonutErp.Core.Entities.UnitConversion>()
                .FirstOrDefaultAsync(
                    uc => uc.FromUnit.ToLower() == fromUnit.ToLower() &&
                          uc.ToUnit.ToLower() == toUnit.ToLower() &&
                          uc.Category == category &&
                          uc.IsActive,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"No conversion rule found from {fromUnit} to {toUnit} in category {category}");

            var conversionObj = new UnitConversionRule
            {
                FromUnit = conversionRule.FromUnit,
                ToUnit = conversionRule.ToUnit,
                ConversionFactor = conversionRule.ConversionFactor,
                Category = conversionRule.Category
            };

            _conversionCache[cacheKey] = conversionObj;

            return conversionObj.Convert(quantity);
        }

        public async Task<List<UnitConversionRule>> GetConversionRulesAsync(
            string category,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));

            var rules = await _dbContext.Set<DonutErp.Core.Entities.UnitConversion>()
                .Where(uc => uc.Category == category && uc.IsActive)
                .ToListAsync(cancellationToken);

            return rules.Select(r => new UnitConversionRule
            {
                FromUnit = r.FromUnit,
                ToUnit = r.ToUnit,
                ConversionFactor = r.ConversionFactor,
                Category = r.Category
            }).ToList();
        }

        public async Task<bool> SetConversionRuleAsync(
            UnitConversionRule rule,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(rule));

            if (rule.ConversionFactor <= 0)
                throw new ArgumentException("Conversion factor must be positive", nameof(rule.ConversionFactor));

            var existingRule = await _dbContext.Set<DonutErp.Core.Entities.UnitConversion>()
                .FirstOrDefaultAsync(
                    uc => uc.FromUnit.ToLower() == rule.FromUnit.ToLower() &&
                          uc.ToUnit.ToLower() == rule.ToUnit.ToLower() &&
                          uc.Category == rule.Category,
                    cancellationToken);

            if (existingRule != null)
            {
                existingRule.ConversionFactor = rule.ConversionFactor;
                existingRule.UpdatedAt = DateTime.Now;
            }
            else
            {
                var newRule = new DonutErp.Core.Entities.UnitConversion
                {
                    FromUnit = rule.FromUnit,
                    ToUnit = rule.ToUnit,
                    Category = rule.Category,
                    ConversionFactor = rule.ConversionFactor,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                _dbContext.Set<DonutErp.Core.Entities.UnitConversion>().Add(newRule);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            
            // Clear cache to force reload
            ClearCacheForCategory(rule.Category);

            return true;
        }

        public async Task<bool> CanConvertAsync(
            string fromUnit,
            string toUnit,
            string category,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (fromUnit.Equals(toUnit, StringComparison.OrdinalIgnoreCase))
                    return true;

                var rules = await GetConversionRulesAsync(category, cancellationToken);
                return rules.Any(r => 
                    r.FromUnit.Equals(fromUnit, StringComparison.OrdinalIgnoreCase) &&
                    r.ToUnit.Equals(toUnit, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetBaseUnitAsync(
            string category,
            CancellationToken cancellationToken = default)
        {
            if (_baseUnitCache.TryGetValue(category, out var baseUnit))
                return baseUnit;

            // Standard base units
            var baseUnits = new Dictionary<string, string>
            {
                { "Weight", "Gram" },
                { "Volume", "Mililiter" },
                { "Count", "Pcs" },
                { "Time", "Jam" }
            };

            if (baseUnits.TryGetValue(category, out var baseUnitValue))
            {
                _baseUnitCache[category] = baseUnitValue;
                return baseUnitValue;
            }

            throw new InvalidOperationException($"Unknown category: {category}");
        }

        public async Task<(double NormalizedQuantity, string BaseUnit)> NormalizeToBaseUnitAsync(
            string unit,
            double quantity,
            string category,
            CancellationToken cancellationToken = default)
        {
            var baseUnit = await GetBaseUnitAsync(category, cancellationToken);

            if (unit.Equals(baseUnit, StringComparison.OrdinalIgnoreCase))
                return (quantity, baseUnit);

            var normalizedQuantity = await ConvertAsync(unit, baseUnit, quantity, category, cancellationToken);
            return (normalizedQuantity, baseUnit);
        }

        public async Task<string> FormatQuantityAsync(
            double baseQuantity,
            string targetUnit,
            string category,
            int decimalPlaces = 2,
            CancellationToken cancellationToken = default)
        {
            var baseUnit = await GetBaseUnitAsync(category, cancellationToken);

            double displayQuantity;
            if (targetUnit.Equals(baseUnit, StringComparison.OrdinalIgnoreCase))
            {
                displayQuantity = baseQuantity;
            }
            else
            {
                displayQuantity = await ConvertAsync(baseUnit, targetUnit, baseQuantity, category, cancellationToken);
            }

            var precision = await GetPrecisionRuleAsync(targetUnit, cancellationToken);
            if (precision != null)
            {
                displayQuantity = precision.Round(displayQuantity);
                decimalPlaces = precision.DecimalPlaces;
            }

            return $"{displayQuantity.ToString($"F{decimalPlaces}")} {targetUnit}";
        }

        public async Task<PrecisionRule?> GetPrecisionRuleAsync(
            string unit,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = unit.ToLower();

            if (_precisionCache.TryGetValue(cacheKey, out var rule))
                return rule;

            // Standard precision rules for food industry
            var standardRules = new Dictionary<string, PrecisionRule>
            {
                { "gram", new PrecisionRule { DecimalPlaces = 2, MinimumQuantity = 0.01 } },
                { "mililiter", new PrecisionRule { DecimalPlaces = 2, MinimumQuantity = 0.01 } },
                { "pcs", new PrecisionRule { DecimalPlaces = 0, MinimumQuantity = 1 } },
                { "butir", new PrecisionRule { DecimalPlaces = 0, MinimumQuantity = 1 } },
                { "sak", new PrecisionRule { DecimalPlaces = 1, MinimumQuantity = 0.5 } },
                { "botol", new PrecisionRule { DecimalPlaces = 0, MinimumQuantity = 1 } },
                { "kaleng", new PrecisionRule { DecimalPlaces = 0, MinimumQuantity = 1 } }
            };

            if (standardRules.TryGetValue(cacheKey, out var standardRule))
            {
                _precisionCache[cacheKey] = standardRule;
                return standardRule;
            }

            return null;
        }

        private void InitializeStandardConversions()
        {
            // These are lazy-loaded on first use, but here we define the standard mappings
            // In a real system, these would be seeded in the database

            var standardConversions = new List<(string From, string To, double Factor, string Category)>
            {
                // Weight conversions
                ("Gram", "Kilogram", 0.001, "Weight"),
                ("Kilogram", "Gram", 1000, "Weight"),
                ("Gram", "Ounce", 0.03527396, "Weight"),
                ("Ounce", "Gram", 28.34952, "Weight"),

                // Volume conversions
                ("Mililiter", "Liter", 0.001, "Volume"),
                ("Liter", "Mililiter", 1000, "Volume"),
                ("Mililiter", "CubicCentimeter", 1, "Volume"),

                // Indonesian specific
                ("Sak", "Kilogram", 25, "Weight"), // 1 sak = 25 kg (common in Indonesia)
                ("Karung", "Kilogram", 50, "Weight"), // 1 karung = 50 kg
                ("Botol", "Liter", 0.6, "Volume"), // typical bottle size
                ("Kaleng", "Kilogram", 0.397, "Weight"), // typical canned product
            };
        }

        private void ClearCacheForCategory(string category)
        {
            var keysToRemove = _conversionCache.Keys
                .Where(k => k.Contains($"_{category.ToLower()}"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _conversionCache.Remove(key);
            }
        }

        public void ClearAllCaches()
        {
            _conversionCache.Clear();
            _precisionCache.Clear();
            _baseUnitCache.Clear();
        }
    }
}
