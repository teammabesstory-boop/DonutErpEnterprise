#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonutErp.Core.Entities
{
    /// <summary>
    /// Unit conversion mapping for food manufacturing precision.
    /// Stores all unit conversion rules for consistent calculations across the system.
    /// </summary>
    [Table("UnitConversions")]
    public class UnitConversion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required, MaxLength(20)]
        public required string FromUnit { get; set; }
        
        [Required, MaxLength(20)]
        public required string ToUnit { get; set; }
        
        [Required]
        public required string Category { get; set; } // Weight, Volume, Count, Time
        
        [Range(0.000001, double.MaxValue)]
        public double ConversionFactor { get; set; }
        
        public int DecimalPlacesAllowed { get; set; } = 2;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Batch-specific cost snapshot for historical accuracy.
    /// Records exact costs at time of production for accurate HPP calculation.
    /// </summary>
    [Table("BatchCostSnapshots")]
    public class BatchCostSnapshot
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [ForeignKey("ProductionBatch")]
        public Guid ProductionBatchId { get; set; }
        public virtual ProductionBatch? ProductionBatch { get; set; }
        
        [ForeignKey("Ingredient")]
        public Guid IngredientId { get; set; }
        public virtual Ingredient? Ingredient { get; set; }
        
        public double QuantityUsed { get; set; }
        public double WasteQuantity { get; set; }
        public double WastePercentage { get; set; }
        
        [Column(TypeName = "decimal(18, 4)")]
        public decimal CostPerUnit { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalMaterialCost { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalWasteCost { get; set; }
        
        public string? SupplierName { get; set; }
        public DateTime SnapshotDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Overhead cost allocation tracking for batches.
    /// Tracks labor, utilities, depreciation per batch for accurate total cost.
    /// </summary>
    [Table("BatchOverheadAllocations")]
    public class BatchOverheadAllocation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [ForeignKey("ProductionBatch")]
        public Guid ProductionBatchId { get; set; }
        public virtual ProductionBatch? ProductionBatch { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal LaborCostAllocated { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UtilityCostAllocated { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OilCostAllocated { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DepreciationCostAllocated { get; set; }
        
        public int AllocationBasis { get; set; } // Units produced
        
        [Column(TypeName = "decimal(18, 4)")]
        public decimal AllocationPerUnit => AllocationBasis > 0 ? 
            (LaborCostAllocated + UtilityCostAllocated + OilCostAllocated + DepreciationCostAllocated) / AllocationBasis : 0;
        
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Recurring transaction definitions for automation.
    /// Enables automatic creation of salary, rent, utilities, etc. transactions.
    /// </summary>
    [Table("RecurringTransactions")]
    public class RecurringTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required, MaxLength(100)]
        public required string Name { get; set; }
        
        [Required, MaxLength(500)]
        public required string Description { get; set; }
        
        public TransactionType Type { get; set; }
        
        [ForeignKey("Wallet")]
        public Guid? WalletId { get; set; }
        public virtual Wallet? Wallet { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
        
        public string RecurrencePattern { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Yearly
        public int RecurrenceDay { get; set; } // Day of month, 1-31, or day of week
        
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime NextDueDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Tracks depreciation calculations for asset management.
    /// </summary>
    [Table("AssetDepreciations")]
    public class AssetDepreciation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [ForeignKey("Asset")]
        public Guid AssetId { get; set; }
        public virtual Asset? Asset { get; set; }
        
        public DateTime DepreciationMonth { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MonthlyDepreciation { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AccumulatedDepreciation { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BookValue { get; set; }
        
        [ForeignKey("Transaction")]
        public Guid? TransactionId { get; set; }
        public virtual Transaction? Transaction { get; set; }
        
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Enhanced audit trail for comprehensive compliance and fraud detection.
    /// </summary>
    [Table("ComplianceAuditLogs")]
    public class ComplianceAuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [Required, MaxLength(50)]
        public required string Action { get; set; }
        
        [Required, MaxLength(100)]
        public required string EntityName { get; set; }
        
        [Required]
        public required string EntityId { get; set; }
        
        [Required]
        public required string Username { get; set; }
        
        public string UserRole { get; set; } = string.Empty;
        
        [Column(TypeName = "nvarchar(max)")]
        public string OldValues { get; set; } = string.Empty; // JSON
        
        [Column(TypeName = "nvarchar(max)")]
        public string NewValues { get; set; } = string.Empty; // JSON
        
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        
        public bool IsDataModification { get; set; }
        public bool IsSuspicious { get; set; }
        public string? SuspicionReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
