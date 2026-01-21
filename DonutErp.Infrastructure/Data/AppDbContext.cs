using Microsoft.EntityFrameworkCore;
using DonutErp.Core.Entities;

namespace DonutErp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor injection untuk konfigurasi dari App.xaml.cs
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ==========================================
        // 1. SECURITY & AUDIT
        // ==========================================
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ComplianceAuditLog> ComplianceAuditLogs { get; set; }

        // ==========================================
        // 2. INVENTORY & SUPPLY CHAIN
        // ==========================================
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<SupplierPriceHistory> SupplierPriceHistories { get; set; }
        public DbSet<UnitConversion> UnitConversions { get; set; }

        // ==========================================
        // 3. PRODUCT ENGINEERING (BOM)
        // ==========================================
        public DbSet<Product> Products { get; set; }
        public DbSet<Recipe> Recipes { get; set; }

        // ==========================================
        // 4. FACTORY & PRODUCTION
        // ==========================================
        public DbSet<ProductionBatch> ProductionBatches { get; set; }
        public DbSet<ProductionOutput> ProductionOutputs { get; set; }
        public DbSet<BatchCostSnapshot> BatchCostSnapshots { get; set; }
        public DbSet<BatchOverheadAllocation> BatchOverheadAllocations { get; set; }

        // ==========================================
        // 5. FINANCE & ACCOUNTING
        // ==========================================
        public DbSet<Wallet> Wallets { get; set; } // Kas & Bank
        public DbSet<Asset> Assets { get; set; }   // Aset Tetap (Mesin, dll)
        public DbSet<AssetDepreciation> AssetDepreciations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }
        public DbSet<RecurringTransaction> RecurringTransactions { get; set; }

        // Fallback configuration jika DI gagal (Safety Net)
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=donuterp.db");
            }
        }

        // Konfigurasi Hubungan Antar Tabel (Fluent API)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- RECIPE (MULTI-LEVEL BOM) CONFIGURATION ---
            // Resep bisa menunjuk ke Bahan Baku (Ingredient)
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.Ingredient)
                .WithMany()
                .HasForeignKey(r => r.IngredientId)
                .OnDelete(DeleteBehavior.Restrict); // Jangan hapus resep kalo bahan dihapus (biar aman)

            // ATAU Resep bisa menunjuk ke Produk Lain (Sub-Product / Intermediate)
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.SubProduct)
                .WithMany()
                .HasForeignKey(r => r.SubProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- PRICE HISTORY ---
            modelBuilder.Entity<SupplierPriceHistory>()
                .HasOne<Ingredient>()
                .WithMany(i => i.PriceHistories)
                .HasForeignKey(h => h.IngredientId)
                .OnDelete(DeleteBehavior.Cascade); // Kalo bahan dihapus, history harganya ikut hilang

            // --- PRODUCTION ---
            modelBuilder.Entity<ProductionOutput>()
                .HasOne(o => o.ProductionBatch)
                .WithMany(b => b.Outputs)
                .HasForeignKey(o => o.ProductionBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- BATCH COST TRACKING ---
            modelBuilder.Entity<BatchCostSnapshot>()
                .HasOne(bcs => bcs.ProductionBatch)
                .WithMany()
                .HasForeignKey(bcs => bcs.ProductionBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BatchCostSnapshot>()
                .HasOne(bcs => bcs.Ingredient)
                .WithMany()
                .HasForeignKey(bcs => bcs.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- BATCH OVERHEAD ---
            modelBuilder.Entity<BatchOverheadAllocation>()
                .HasOne(boa => boa.ProductionBatch)
                .WithMany()
                .HasForeignKey(boa => boa.ProductionBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- ASSET DEPRECIATION ---
            modelBuilder.Entity<AssetDepreciation>()
                .HasOne(ad => ad.Asset)
                .WithMany()
                .HasForeignKey(ad => ad.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssetDepreciation>()
                .HasOne(ad => ad.Transaction)
                .WithMany()
                .HasForeignKey(ad => ad.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- RECURRING TRANSACTIONS ---
            modelBuilder.Entity<RecurringTransaction>()
                .HasOne(rt => rt.Wallet)
                .WithMany()
                .HasForeignKey(rt => rt.WalletId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}