using Microsoft.EntityFrameworkCore;
using DonutErp.Core.Entities;

namespace DonutErp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ==========================================
        // REGISTRASI TABEL (DbSet)
        // ==========================================

        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Product> Products { get; set; }

        // UPDATE PENTING:
        // Di kode lama namanya "RecipeItems" (Tipe RecipeItem).
        // Di Service baru kita pakai "Recipes" (Tipe Recipe).
        // Kita standarkan jadi "Recipes" agar ProductionService jalan.
        public DbSet<Recipe> Recipes { get; set; }

        public DbSet<ProductionBatch> ProductionBatches { get; set; }
        public DbSet<ProductionOutput> ProductionOutputs { get; set; }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        // ==========================================
        // DATABASE CONFIGURATION (FLUENT API)
        // ==========================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. KONFIGURASI INGREDIENT (BAHAN BAKU) ---
            modelBuilder.Entity<Ingredient>(entity =>
            {
                // SKU harus unik
                entity.HasIndex(e => e.Sku).IsUnique();

                // Set default values
                entity.Property(e => e.ConversionRatio).HasDefaultValue(1.0);
                entity.Property(e => e.CurrentStock).HasDefaultValue(0);
                entity.Property(e => e.AvgCostPerUsageUnit).HasDefaultValue(0);
            });

            // --- 2. KONFIGURASI PRODUCT & RECIPE ---
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            });

            // Ganti RecipeItem jadi Recipe
            modelBuilder.Entity<Recipe>(entity =>
            {
                // Mencegah duplikasi bahan dalam 1 produk
                entity.HasIndex(e => new { e.ProductId, e.IngredientId }).IsUnique();

                // Relation: Product -> Recipes (Cascade Delete)
                // Hapus Produk = Hapus Resepnya
                entity.HasOne(d => d.Product)
                      .WithMany(p => p.Recipes) // Pastikan di class Product namanya 'Recipes'
                      .HasForeignKey(d => d.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation: Ingredient -> Recipes (Restrict Delete)
                // Tidak boleh hapus bahan baku jika masih dipakai di resep
                entity.HasOne(d => d.Ingredient)
                      .WithMany()
                      .HasForeignKey(d => d.IngredientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- 3. KONFIGURASI PRODUCTION (DAPUR) ---
            modelBuilder.Entity<ProductionBatch>(entity =>
            {
                entity.HasIndex(e => e.BatchCode).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>(); // Enum to String

                // Konfigurasi presisi desimal untuk duit
                entity.Property(e => e.CalculatedOilCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalBatchCost).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<ProductionOutput>(entity =>
            {
                entity.HasOne(d => d.ProductionBatch)
                      .WithMany(p => p.Outputs)
                      .HasForeignKey(d => d.ProductionBatchId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Product)
                      .WithMany()
                      .HasForeignKey(d => d.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- 4. KONFIGURASI TRANSACTION (KEUANGAN) ---
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.Property(e => e.Type).HasConversion<string>();

                // Konfigurasi presisi desimal
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<TransactionDetail>(entity =>
            {
                entity.HasOne(d => d.Transaction)
                      .WithMany(p => p.Details)
                      .HasForeignKey(d => d.TransactionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.PriceAtSale).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CostAtSale).HasColumnType("decimal(18,2)");
            });
        }
    }
}