using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // Penting untuk query
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
                await _context.Database.EnsureCreatedAsync();

                bool hasData = await _context.Ingredients.AnyAsync();
                if (hasData) return;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("DATABASE RECREATING...");
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
            }

            // 1. DATA BAHAN BAKU (REAL PRICING)
            // Asumsi harga pasar saat ini
            var ingredients = new List<Ingredient>
            {
                // Tepung Cakra: Rp 12.000/kg -> Rp 12/gram
                new Ingredient { Id = Guid.NewGuid(), Name = "Tepung Cakra Kembar", Sku = "RM-001", PurchaseUnit = "Sak 25kg", UsageUnit = "Gram", ConversionRatio = 25000, CurrentStock = 25000, MinStockLevel = 5000, AvgCostPerUsageUnit = 12, LastPurchasePrice = 300000 },
                
                // Gula Pasir: Rp 16.000/kg -> Rp 16/gram
                new Ingredient { Id = Guid.NewGuid(), Name = "Gula Pasir", Sku = "RM-002", PurchaseUnit = "Karung 50kg", UsageUnit = "Gram", ConversionRatio = 50000, CurrentStock = 50000, MinStockLevel = 10000, AvgCostPerUsageUnit = 16, LastPurchasePrice = 800000 },
                
                // Telur: Rp 2.000/butir
                new Ingredient { Id = Guid.NewGuid(), Name = "Telur Ayam", Sku = "RM-003", PurchaseUnit = "Tray 30pcs", UsageUnit = "Pcs", ConversionRatio = 30, CurrentStock = 300, MinStockLevel = 50, AvgCostPerUsageUnit = 2000, LastPurchasePrice = 60000 },
                
                // Susu UHT: Rp 18.000/Liter -> Rp 18/ml
                new Ingredient { Id = Guid.NewGuid(), Name = "Susu UHT", Sku = "RM-004", PurchaseUnit = "Karton 12L", UsageUnit = "Mililiter", ConversionRatio = 12000, CurrentStock = 12000, MinStockLevel = 2000, AvgCostPerUsageUnit = 18, LastPurchasePrice = 216000 },
                
                // Ragi: Rp 50.000/500g -> Rp 100/gram
                new Ingredient { Id = Guid.NewGuid(), Name = "Ragi Instant", Sku = "RM-005", PurchaseUnit = "Pack 500g", UsageUnit = "Gram", ConversionRatio = 500, CurrentStock = 2500, MinStockLevel = 500, AvgCostPerUsageUnit = 100, LastPurchasePrice = 50000 },
                
                // Minyak Goreng: Rp 15.000/Liter -> Rp 15/ml
                new Ingredient { Id = Guid.NewGuid(), Name = "Minyak Goreng Padat", Sku = "RM-006", PurchaseUnit = "Karton 15kg", UsageUnit = "Gram", ConversionRatio = 15000, CurrentStock = 15000, MinStockLevel = 5000, AvgCostPerUsageUnit = 15, LastPurchasePrice = 225000, IsFryingOil = true },

                // Coklat Masak: Rp 60.000/kg -> Rp 60/gram
                new Ingredient { Id = Guid.NewGuid(), Name = "Coklat Batang DCC", Sku = "RM-007", PurchaseUnit = "Box 1kg", UsageUnit = "Gram", ConversionRatio = 1000, CurrentStock = 5000, MinStockLevel = 1000, AvgCostPerUsageUnit = 60, LastPurchasePrice = 60000 }
            };

            await _context.Ingredients.AddRangeAsync(ingredients);

            // 2. DATA PRODUK JADI
            var prodDonatGula = new Product { Id = Guid.NewGuid(), Name = "Donat Kampung Gula", Sku = "DN-001", Type = ProductType.RingDonut, SellingPrice = 5000, DiameterCm = 8, InnerHoleDiameterCm = 2, CachedHpp = 0 };
            var prodDonatCoklat = new Product { Id = Guid.NewGuid(), Name = "Donat Siram Coklat", Sku = "DN-002", Type = ProductType.RingDonut, SellingPrice = 7000, DiameterCm = 8, InnerHoleDiameterCm = 2, CachedHpp = 0 };
            var prodBomboloni = new Product { Id = Guid.NewGuid(), Name = "Bomboloni Original", Sku = "BM-001", Type = ProductType.Bomboloni, SellingPrice = 6000, DiameterCm = 7, InnerHoleDiameterCm = 0, CachedHpp = 0 };

            await _context.Products.AddRangeAsync(new[] { prodDonatGula, prodDonatCoklat, prodBomboloni });

            // 3. DATA RESEP (INI YANG BIKIN HPP JADI REAL)
            // Asumsi resep per 1 Pcs Donat

            // Ambil ID Bahan untuk mapping
            var tepungId = ingredients.First(i => i.Sku == "RM-001").Id;
            var gulaId = ingredients.First(i => i.Sku == "RM-002").Id;
            var telurId = ingredients.First(i => i.Sku == "RM-003").Id;
            var susuId = ingredients.First(i => i.Sku == "RM-004").Id;
            var ragiId = ingredients.First(i => i.Sku == "RM-005").Id;
            var coklatId = ingredients.First(i => i.Sku == "RM-007").Id;

            var recipes = new List<Recipe>();

            // Resep Donat Gula (Basic Dough + Gula Tabur)
            recipes.Add(new Recipe { ProductId = prodDonatGula.Id, IngredientId = tepungId, Quantity = 40 }); // 40gr Tepung
            recipes.Add(new Recipe { ProductId = prodDonatGula.Id, IngredientId = gulaId, Quantity = 5 });   // 5gr Gula Adonan
            recipes.Add(new Recipe { ProductId = prodDonatGula.Id, IngredientId = telurId, Quantity = 0.1 }); // 1/10 Butir Telur
            recipes.Add(new Recipe { ProductId = prodDonatGula.Id, IngredientId = susuId, Quantity = 15 });  // 15ml Susu
            recipes.Add(new Recipe { ProductId = prodDonatGula.Id, IngredientId = ragiId, Quantity = 1 });   // 1gr Ragi
            recipes.Add(new Recipe { ProductId = prodDonatGula.Id, IngredientId = gulaId, Quantity = 5 });   // 5gr Gula Tabur (Extra)

            // Resep Donat Coklat (Basic Dough + Topping Coklat)
            recipes.Add(new Recipe { ProductId = prodDonatCoklat.Id, IngredientId = tepungId, Quantity = 40 });
            recipes.Add(new Recipe { ProductId = prodDonatCoklat.Id, IngredientId = gulaId, Quantity = 5 });
            recipes.Add(new Recipe { ProductId = prodDonatCoklat.Id, IngredientId = telurId, Quantity = 0.1 });
            recipes.Add(new Recipe { ProductId = prodDonatCoklat.Id, IngredientId = susuId, Quantity = 15 });
            recipes.Add(new Recipe { ProductId = prodDonatCoklat.Id, IngredientId = ragiId, Quantity = 1 });
            recipes.Add(new Recipe { ProductId = prodDonatCoklat.Id, IngredientId = coklatId, Quantity = 15 }); // 15gr Coklat Leleh

            await _context.Recipes.AddRangeAsync(recipes);
            await _context.SaveChangesAsync();

            // 4. HITUNG HPP OTOMATIS BERDASARKAN RESEP DI ATAS
            // Logic manual simpel untuk seeding awal
            foreach (var p in new[] { prodDonatGula, prodDonatCoklat })
            {
                decimal hpp = 0;
                var myRecipes = recipes.Where(r => r.ProductId == p.Id).ToList();
                foreach (var r in myRecipes)
                {
                    var ing = ingredients.First(i => i.Id == r.IngredientId);
                    hpp += ((decimal)r.Quantity * ing.AvgCostPerUsageUnit);
                }
                p.CachedHpp = hpp;
            }
            _context.Products.UpdateRange(new[] { prodDonatGula, prodDonatCoklat });
            await _context.SaveChangesAsync();
        }
    }
}