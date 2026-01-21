using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace DonutErp.Infrastructure.Data
{
    public class DbSeeder : IDatabaseSeeder
    {
        private readonly AppDbContext _context;

        public DbSeeder(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedInitialDataAsync()
        {
            try
            {
                // PERCOBAAN 1: Normal Init
                await _context.Database.EnsureCreatedAsync();

                // Cek apakah tabel benar-benar ada dengan mencoba akses
                // Jika tabel tidak ada, baris ini akan throw error dan masuk catch
                bool hasData = await _context.Ingredients.AnyAsync();
                if (hasData) return;
            }
            catch (Exception)
            {
                // SELF-HEALING: Jika error (misal "no such table"), 
                // berarti DB korup/kosong. Kita Reset Paksa.
                System.Diagnostics.Debug.WriteLine("DATABASE CORRUPT DETECTED. RECREATING...");

                await _context.Database.EnsureDeletedAsync(); // Hapus DB lama
                await _context.Database.EnsureCreatedAsync(); // Bikin baru fresh
            }

            // --- SEEDING DATA (Copas data dummy yang tadi) ---

            var ingredients = new List<Ingredient>
            {
                new Ingredient { Name = "Tepung Terigu Cakra", Sku = "RM-001", PurchaseUnit = "Sak 25kg", UsageUnit = "Gram", ConversionRatio = 25000, CurrentStock = 250000, MinStockLevel = 50000, AvgCostPerUsageUnit = 12, LastPurchasePrice = 300000 },
                new Ingredient { Name = "Gula Pasir", Sku = "RM-002", PurchaseUnit = "Karung 50kg", UsageUnit = "Gram", ConversionRatio = 50000, CurrentStock = 100000, MinStockLevel = 20000, AvgCostPerUsageUnit = 15, LastPurchasePrice = 750000 },
                new Ingredient { Name = "Minyak Goreng Padat", Sku = "RM-003", PurchaseUnit = "Karton 15kg", UsageUnit = "Gram", ConversionRatio = 15000, CurrentStock = 45000, MinStockLevel = 15000, AvgCostPerUsageUnit = 25, LastPurchasePrice = 375000, IsFryingOil = true },
                new Ingredient { Name = "Telur Ayam", Sku = "RM-004", PurchaseUnit = "Tray 30pcs", UsageUnit = "Pcs", ConversionRatio = 30, CurrentStock = 300, MinStockLevel = 60, AvgCostPerUsageUnit = 2000, LastPurchasePrice = 60000 },
                new Ingredient { Name = "Ragi Instant", Sku = "RM-005", PurchaseUnit = "Pack 500g", UsageUnit = "Gram", ConversionRatio = 500, CurrentStock = 5000, MinStockLevel = 1000, AvgCostPerUsageUnit = 100, LastPurchasePrice = 50000 },
                new Ingredient { Name = "Susu UHT", Sku = "RM-006", PurchaseUnit = "Karton 12L", UsageUnit = "Mililiter", ConversionRatio = 12000, CurrentStock = 24000, MinStockLevel = 5000, AvgCostPerUsageUnit = 18, LastPurchasePrice = 216000 }
            };

            await _context.Ingredients.AddRangeAsync(ingredients);

            var products = new List<Product>
            {
                new Product { Name = "Donut Gula Halus", Sku = "DN-001", Type = ProductType.RingDonut, SellingPrice = 5000, DiameterCm = 8, InnerHoleDiameterCm = 2 },
                new Product { Name = "Donut Coklat Leleh", Sku = "DN-002", Type = ProductType.RingDonut, SellingPrice = 7000, DiameterCm = 8, InnerHoleDiameterCm = 2 },
                new Product { Name = "Bomboloni Strawberry", Sku = "BM-001", Type = ProductType.Bomboloni, SellingPrice = 8000, DiameterCm = 7, InnerHoleDiameterCm = 0 },
                new Product { Name = "Kopi Susu Gula Aren", Sku = "BV-001", Type = ProductType.Beverage, SellingPrice = 18000 }
            };

            await _context.Products.AddRangeAsync(products);

            await _context.SaveChangesAsync();
        }
    }
}