#nullable enable

namespace DonutErp.Infrastructure.Documentation
{
    /// <summary>
    /// === DonutErp Enterprise Data Model Overview ===
    /// 
    /// LAYER 1: SECURITY & AUDIT
    /// ========================
    /// Users
    ///   ?? Id (Guid)
    ///   ?? Username (string)
    ///   ?? PasswordHash (string)
    ///   ?? Role (UserRole: Owner, Admin, Cashier, Warehouse, Kitchen)
    ///   ?? IsActive (bool)
    /// 
    /// ComplianceAuditLog (Advanced)
    ///   ?? Id (Guid)
    ///   ?? Timestamp (DateTime)
    ///   ?? Action (CREATE, UPDATE, DELETE, LOGIN, etc)
    ///   ?? EntityName + EntityId (WHAT changed)
    ///   ?? Username + UserRole (WHO made the change)
    ///   ?? OldValues + NewValues (JSON comparison)
    ///   ?? IpAddress + UserAgent (WHERE from)
    ///   ?? IsSuspicious + SuspicionReason (RISK assessment)
    ///   ?? CreatedAt (immutable timestamp)
    /// 
    /// 
    /// LAYER 2: INVENTORY & SUPPLY CHAIN
    /// ==================================
    /// Ingredient (Raw Materials)
    ///   ?? Id (Guid)
    ///   ?? Name, SKU, Category
    ///   ?? PurchaseUnit (Sak, Kilogram, Botol, etc)
    ///   ?? UsageUnit (Gram, Mililiter, Pcs, etc)
    ///   ?? ConversionRatio (Sak 25kg = 25000g)
    ///   ?? AvgCostPerUsageUnit (decimal) ? KEY for HPP calculation
    ///   ?? CurrentStock + MinStockLevel
    ///   ?? IsFryingOil (special handling)
    ///   ?? PriceHistories (SupplierPriceHistory[])
    /// 
    /// SupplierPriceHistory
    ///   ?? Id (Guid)
    ///   ?? IngredientId (FK)
    ///   ?? RecordedDate (DateTime)
    ///   ?? PricePerPurchaseUnit (decimal)
    ///   ?? SupplierName (string)
    ///   ?? Purpose: Track historical prices for cost trends & HPP accuracy
    /// 
    /// UnitConversion (NEW - Advanced)
    ///   ?? Id (Guid)
    ///   ?? FromUnit + ToUnit (Sak ? Gram, Botol ? Liter)
    ///   ?? ConversionFactor (1 Sak = 25000 Gram)
    ///   ?? Category (Weight, Volume, Count, Time)
    ///   ?? DecimalPlacesAllowed (precision: 0-4)
    ///   ?? IsActive (bool)
    ///   ?? Purpose: Flexible unit system for food industry
    /// 
    /// 
    /// LAYER 3: PRODUCT ENGINEERING (BILL OF MATERIALS / BOM)
    /// =======================================================
    /// Product
    ///   ?? Id (Guid)
    ///   ?? Name, SKU, Description
    ///   ?? Type (RingDonut, Bomboloni, Beverage, etc)
    ///   ?? SellingPrice (decimal)
    ///   ?? CachedHpp (decimal) ? Auto-calculated, refreshed on ingredient price change
    ///   ?? DiameterCm + InnerHoleDiameterCm (product specs)
    ///   ?? Recipes (Recipe[]) ? Multi-level BOM
    /// 
    /// Recipe (Multi-Level BOM)
    ///   ?? Id (Guid)
    ///   ?? ParentProductId (the product being made)
    ///   ?? IngredientId (raw material) ?? Ingredient (FK)
    ///   ?  OR
    ///   ?? SubProductId (intermediate product) ?? Product (FK)
    ///   ?  Example: "Adonan Dasar" is ingredient for "Donat Coklat"
    ///   ?? Quantity (how much needed)
    ///   ?? WastePercentage (0-100%, e.g., potato skin waste = 3%)
    ///   ?? Purpose: Recursive BOM resolution with waste tracking
    /// 
    /// 
    /// LAYER 4: PRODUCTION & BATCH TRACKING
    /// ====================================
    /// ProductionBatch
    ///   ?? Id (Guid)
    ///   ?? BatchCode (auto-generated, e.g., "BATCH-20240115-A1")
    ///   ?? ProductionDate (DateTime)
    ///   ?? Status (Planned ? InProgress ? QualityControl ? Finished/Failed)
    ///   ?? OilLevel (StartLiter, EndLiter, AddedLiter)
    ///   ?? OilConsumedLiters + CalculatedOilCost
    ///   ?? LaborCost + UtilitiesCost (overhead)
    ///   ?? TotalBatchCost (all inclusive)
    ///   ?? Notes (string)
    ///   ?? Outputs (ProductionOutput[])
    /// 
    /// ProductionOutput
    ///   ?? Id (Guid)
    ///   ?? ProductionBatchId (FK)
    ///   ?? ProductId (FK) ?? Product
    ///   ?? QuantityGood (units passed QC)
    ///   ?? QuantityReject (units failed QC)
    ///   ?? ActualHppPerUnit ? Calculated after batch completion
    ///   ?? Purpose: Track what was produced and its actual cost
    /// 
    /// BatchCostSnapshot (NEW - Advanced)
    ///   ?? Id (Guid)
    ///   ?? ProductionBatchId (FK)
    ///   ?? IngredientId (FK)
    ///   ?? QuantityUsed + WasteQuantity
    ///   ?? WastePercentage
    ///   ?? CostPerUnit (snapshot of ingredient cost at time of production)
    ///   ?? TotalMaterialCost + TotalWasteCost
    ///   ?? SupplierName + SnapshotDate
    ///   ?? Purpose: Historical accuracy - ingredient costs change over time
    /// 
    /// BatchOverheadAllocation (NEW - Advanced)
    ///   ?? Id (Guid)
    ///   ?? ProductionBatchId (FK)
    ///   ?? LaborCostAllocated
    ///   ?? UtilityCostAllocated
    ///   ?? OilCostAllocated
    ///   ?? DepreciationCostAllocated
    ///   ?? AllocationBasis (total units produced)
    ///   ?? AllocationPerUnit (calculated)
    ///   ?? CalculatedAt (DateTime)
    ///   ?? Purpose: Distribute indirect costs fairly across units
    /// 
    /// 
    /// LAYER 5: FINANCE & ACCOUNTING
    /// =============================
    /// Wallet (Cash, Bank, E-Wallet tracking)
    ///   ?? Id (Guid)
    ///   ?? Name (Kas Besar, Bank BCA, GCash, etc)
    ///   ?? Type (WalletType.Cash, .Bank, .EWallet)
    ///   ?? CurrentBalance (decimal) ? Always in sync via transactions
    ///   ?? AccountNumber (optional)
    /// 
    /// Asset (Fixed Assets for Depreciation)
    ///   ?? Id (Guid)
    ///   ?? Name (Mixer, Oven, Freezer, etc)
    ///   ?? PurchaseDate (DateTime)
    ///   ?? PurchasePrice (decimal)
    ///   ?? UsefulLifeMonths (60 for 5-year asset)
    ///   ?? ResidualValue (5% of purchase price typically)
    ///   ?? MonthlyDepreciation (calculated: (Price - Residual) / Months)
    /// 
    /// AssetDepreciation (NEW - Monthly Depreciation Records)
    ///   ?? Id (Guid)
    ///   ?? AssetId (FK)
    ///   ?? DepreciationMonth (DateTime, first of month)
    ///   ?? MonthlyDepreciation (decimal)
    ///   ?? AccumulatedDepreciation (total to date)
    ///   ?? BookValue (Purchase Price - Accumulated)
    ///   ?? TransactionId (FK) ? Links to journal entry
    ///   ?? RecordedAt (DateTime)
    /// 
    /// Transaction (All Financial Movements)
    ///   ?? Id (Guid)
    ///   ?? InvoiceNumber (unique identifier)
    ///   ?? Date (DateTime)
    ///   ?? Type (SalesIncome, MaterialExpense, OperationalExpense, AssetDepreciation, etc)
    ///   ?? WalletId (FK) ? which wallet affected
    ///   ?? Description (string)
    ///   ?? TotalAmount (decimal) ? Revenue or expense
    ///   ?? TotalCost (decimal) ? Cost of goods sold
    ///   ?? Notes (string)
    ///   ?? PaymentMethod (optional)
    ///   ?? IsRecurring (bool)
    ///   ?? Details (TransactionDetail[]) ? Line items
    /// 
    /// TransactionDetail (Line Items in Transactions)
    ///   ?? Id (Guid)
    ///   ?? TransactionId (FK)
    ///   ?? ProductId (FK) ?? Product
    ///   ?? Quantity (int)
    ///   ?? PriceAtSale (decimal) ? Historical selling price
    ///   ?? CostAtSale (decimal) ? HPP at time of sale
    ///   ?? Purpose: Detailed transaction tracking for profitability analysis
    /// 
    /// RecurringTransaction (NEW - Automation)
    ///   ?? Id (Guid)
    ///   ?? Name (Gaji, Sewa Ruko, Internet, etc)
    ///   ?? Description (string)
    ///   ?? Type (TransactionType)
    ///   ?? WalletId (FK)
    ///   ?? Amount (decimal)
    ///   ?? RecurrencePattern (Daily, Weekly, Monthly, Yearly)
    ///   ?? RecurrenceDay (1-31 for day of month)
    ///   ?? StartDate + EndDate (DateTime)
    ///   ?? IsActive (bool)
    ///   ?? NextDueDate (DateTime) ? Updated after each creation
    ///   ?? CreatedAt (DateTime)
    ///   ?? Purpose: Auto-create salary, rent, utility payments
    /// 
    /// 
    /// KEY RELATIONSHIPS
    /// =================
    /// 
    /// HPP Flow:
    /// Product (SellingPrice)
    /// ?
    /// Recipe[] (multi-level: Ingredient OR SubProduct)
    /// ?
    /// Ingredient (AvgCostPerUsageUnit) ? UnitConversion (for scaling)
    /// ?
    /// SupplierPriceHistory (historical costs)
    /// ?
    /// CachedHpp = ?(Quantity × Cost × (1 + WastePercentage))
    /// 
    /// 
    /// Batch Costing Flow:
    /// ProductionBatch
    /// ?? BatchCostSnapshot[] (ingredient costs at production time)
    /// ?? BatchOverheadAllocation[] (labor, utilities, depreciation)
    /// ?? ProductionOutput[] (what was produced, how much)
    ///    ?
    /// ActualHppPerUnit = TotalBatchCost / GoodUnits
    /// 
    /// 
    /// Financial Reporting Flow:
    /// Transaction (SalesIncome)
    /// ?? TransactionDetail[] (individual line items)
    /// ?   ?? PriceAtSale (historical selling price)
    /// ?   ?? CostAtSale (HPP at time of sale)
    /// ?   ?? Quantity
    /// ?? Wallet (which account used)
    /// ?? ComplianceAuditLog (who made the entry)
    ///    ?
    /// Real-time P&L = ?(Revenue) - ?(COGS) - ?(Overhead) - ?(Depreciation)
    /// 
    /// 
    /// Audit & Compliance Flow:
    /// ComplianceAuditLog
    /// ?? Action (CREATE/UPDATE/DELETE/LOGIN/ACCESS)
    /// ?? EntityName + EntityId (WHAT)
    /// ?? Username + UserRole + IpAddress (WHO)
    /// ?? OldValues + NewValues (WHAT changed)
    /// ?? IsSuspicious + SuspicionReason (WHY flagged)
    /// ?? Timestamp (WHEN, immutable)
    ///    ?
    /// Compliance Report (Summary with risk scores)
    /// ?
    /// Data Integrity Check (Detect tampering, reversions)
    /// 
    /// </summary>
    public class DataModelSummary
    {
        // This is a documentation class - no implementation needed
        // See XML comments above for complete data model overview
    }
}
