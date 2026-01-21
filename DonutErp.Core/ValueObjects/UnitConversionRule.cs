#nullable enable

namespace DonutErp.Core.ValueObjects
{
    public record UnitConversionRule
    {
        public required string FromUnit { get; init; }
        public required string ToUnit { get; init; }
        public required double ConversionFactor { get; init; }
        public required string Category { get; init; } // Weight, Volume, Count
        
        public double Convert(double quantity) => quantity * ConversionFactor;
        public double ConvertBack(double quantity) => quantity / ConversionFactor;
    }

    public record PrecisionRule
    {
        public required int DecimalPlaces { get; init; }
        public required double MinimumQuantity { get; init; }
        public double Round(double value) => 
            Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
    }
}
