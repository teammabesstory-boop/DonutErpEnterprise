using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonutErp.Core.Entities
{
    // ==========================================
    // ENUMS
    // ==========================================
    public enum UnitType { Gram, Mililiter, Pcs, Butir }
    public enum ProductType { RingDonut, Bomboloni, Beverage, AddOn, RawMaterial }
    public enum TransactionType { SalesIncome, MaterialExpense, OperationalExpense, AssetDepreciation, StockAdjustment }
    public enum BatchStatus { Planned, Mixing, Proofing, Frying, Finished, Failed }

    // ==========================================
    // 1. INVENTORY (GUDANG)
    // ==========================================
    [Table("Ingredients")]
    public class Ingredient
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public required string Name { get; set; }

        [Required, MaxLength(20)]
        public required string Sku { get; set; }

        [Required, MaxLength(20)]
        public required string PurchaseUnit { get; set; }

        [Required, MaxLength(20)]
        public required string UsageUnit { get; set; }

        public double ConversionRatio { get; set; } = 1.0;

        [Column(TypeName = "decimal(18, 4)")]
        public decimal AvgCostPerUsageUnit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal LastPurchasePrice { get; set; }

        public double CurrentStock { get; set; }
        public double MinStockLevel { get; set; }
        public bool IsFryingOil { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // ==========================================
    // 2. PRODUCT & RECIPE (PRODUK & RESEP)
    // ==========================================
    [Table("Products")]
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string Name { get; set; }

        // --- INI YANG HILANG TADI ---
        [Required]
        public required string Sku { get; set; }
        // ----------------------------

        public string? Description { get; set; }
        public ProductType Type { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SellingPrice { get; set; }

        [NotMapped]
        public decimal CachedHpp { get; set; }

        public double DiameterCm { get; set; }
        public double InnerHoleDiameterCm { get; set; }

        public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }

    [Table("Recipes")]
    public class Recipe
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public Guid IngredientId { get; set; }
        [ForeignKey("IngredientId")]
        public virtual Ingredient? Ingredient { get; set; }

        public double Quantity { get; set; }

        public double WastePercentage { get; set; } = 0;
        public bool IsBaseDoughIngredient { get; set; } = false;
    }

    // ==========================================
    // 3. PRODUCTION (DAPUR)
    // ==========================================
    [Table("ProductionBatches")]
    public class ProductionBatch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string BatchCode { get; set; }

        public DateTime ProductionDate { get; set; } = DateTime.Now;
        public BatchStatus Status { get; set; } = BatchStatus.Planned;

        public double OilLevelStartLiter { get; set; }
        public double OilLevelEndLiter { get; set; }
        public double OilAddedLiter { get; set; }

        public double OilConsumedLiters { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CalculatedOilCost { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalBatchCost { get; set; }

        public string? Notes { get; set; }

        public virtual ICollection<ProductionOutput> Outputs { get; set; } = new List<ProductionOutput>();
    }

    [Table("ProductionOutputs")]
    public class ProductionOutput
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductionBatchId { get; set; }
        [ForeignKey("ProductionBatchId")]
        public virtual ProductionBatch? ProductionBatch { get; set; }

        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int QuantityGood { get; set; }
        public int QuantityReject { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal FinalHppPerUnit { get; set; }
    }

    // ==========================================
    // 4. FINANCE (KEUANGAN)
    // ==========================================
    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string InvoiceNumber { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
        public TransactionType Type { get; set; }

        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalCost { get; set; }

        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }

        public virtual ICollection<TransactionDetail> Details { get; set; } = new List<TransactionDetail>();
    }

    [Table("TransactionDetails")]
    public class TransactionDetail
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TransactionId { get; set; }
        [ForeignKey("TransactionId")]
        public virtual Transaction? Transaction { get; set; }

        public Guid? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PriceAtSale { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CostAtSale { get; set; }
    }
}