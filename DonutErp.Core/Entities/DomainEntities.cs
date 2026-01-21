using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonutErp.Core.Entities
{
    // ==========================================
    // ENUMS & CONSTANTS
    // ==========================================
    public enum UnitType { Gram, Mililiter, Pcs, Butir, Jam }
    public enum ProductType { RingDonut, Bomboloni, Beverage, AddOn, RawMaterial, Intermediate }
    public enum TransactionType { SalesIncome, MaterialExpense, OperationalExpense, AssetDepreciation, StockAdjustment, Transfer, CapitalInjection }
    public enum BatchStatus { Planned, InProgress, QualityControl, Finished, Failed }
    public enum UserRole { Owner, Admin, Cashier, Warehouse, Kitchen }
    public enum WalletType { Cash, Bank, EWallet }

    // ==========================================
    // 1. SECURITY & AUDIT (THE GUARDIANS)
    // ==========================================
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string ChangesJson { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    // ==========================================
    // 2. INVENTORY & SUPPLY CHAIN (THE WAREHOUSE)
    // ==========================================
    [Table("Ingredients")]
    public class Ingredient
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(100)] public required string Name { get; set; }
        [Required, MaxLength(50)] public string Category { get; set; } = "Umum";
        [Required, MaxLength(20)] public required string Sku { get; set; }

        public string PurchaseUnit { get; set; } = string.Empty;
        public string UsageUnit { get; set; } = string.Empty;
        public double ConversionRatio { get; set; } = 1.0;

        [Column(TypeName = "decimal(18, 4)")]
        public decimal AvgCostPerUsageUnit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal LastPurchasePrice { get; set; }

        public double CurrentStock { get; set; }
        public double MinStockLevel { get; set; }
        public bool IsFryingOil { get; set; } = false;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<SupplierPriceHistory> PriceHistories { get; set; } = new List<SupplierPriceHistory>();
    }

    [Table("SupplierPriceHistories")]
    public class SupplierPriceHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IngredientId { get; set; }
        public DateTime RecordedDate { get; set; } = DateTime.Now;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerPurchaseUnit { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }

    // ==========================================
    // 3. PRODUCT, BOM & ENGINEERING (THE LAB)
    // ==========================================
    [Table("Products")]
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string Name { get; set; }
        [Required] public required string Sku { get; set; }
        public string? Description { get; set; }
        public ProductType Type { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
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
        public Guid ParentProductId { get; set; }

        public Guid? IngredientId { get; set; }
        public Guid? SubProductId { get; set; }

        [ForeignKey("IngredientId")]
        public virtual Ingredient? Ingredient { get; set; }

        [ForeignKey("SubProductId")]
        public virtual Product? SubProduct { get; set; }

        public double Quantity { get; set; }
        public double WastePercentage { get; set; } = 0;
    }

    // ==========================================
    // 4. PRODUCTION & BATCH TRACKING (THE FACTORY)
    // ==========================================
    [Table("ProductionBatches")]
    public class ProductionBatch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string BatchCode { get; set; }
        public DateTime ProductionDate { get; set; } = DateTime.Now;
        public BatchStatus Status { get; set; } = BatchStatus.Planned;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal LaborCost { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UtilitiesCost { get; set; }

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
        // FIX: Navigation Property ini yang tadi hilang!
        [ForeignKey("ProductionBatchId")]
        public virtual ProductionBatch? ProductionBatch { get; set; }

        public Guid ProductId { get; set; }
        // FIX: Ini juga penting buat Include(o => o.Product)
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int QuantityGood { get; set; }
        public int QuantityReject { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ActualHppPerUnit { get; set; }
    }

    // ==========================================
    // 5. FINANCE & LEDGER (THE TREASURY)
    // ==========================================
    [Table("Wallets")]
    public class Wallet
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Name { get; set; }
        public WalletType Type { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentBalance { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
    }

    [Table("Assets")]
    public class Asset
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Name { get; set; }
        public DateTime PurchaseDate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PurchasePrice { get; set; }

        public int UsefulLifeMonths { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ResidualValue { get; set; }

        public decimal MonthlyDepreciation => (PurchasePrice - ResidualValue) / (UsefulLifeMonths > 0 ? UsefulLifeMonths : 1);
    }

    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string InvoiceNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public TransactionType Type { get; set; }

        public Guid? WalletId { get; set; }
        [ForeignKey("WalletId")] public virtual Wallet? Wallet { get; set; }

        public string Description { get; set; } = string.Empty;

        // FIX: Property Notes ini tadi hilang, makanya FinanceService error!
        public string? Notes { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalCost { get; set; }

        public string? PaymentMethod { get; set; }
        public bool IsRecurring { get; set; } = false;

        public virtual ICollection<TransactionDetail> Details { get; set; } = new List<TransactionDetail>();
    }

    [Table("TransactionDetails")]
    public class TransactionDetail
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TransactionId { get; set; }
        // FIX: Navigation Property ke Parent Transaction
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