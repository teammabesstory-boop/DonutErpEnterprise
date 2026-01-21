using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;

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
            // 1. Pastikan Database Terbuat
            // Ini akan membuat file .db jika belum ada (Code First Migration otomatis)
            await _context.Database.EnsureCreatedAsync();

            // 2. Cek apakah Gudang Kosong? Jika ya, isi Starter Pack.
            if (!_context.Ingredients.Any())
            {
                var starterIngredients = new List<Ingredient>
                {
                    // --- TEPUNG & DASAR ADONAN ---
                    new Ingredient
                    {
                        Name = "Tepung Terigu Protein Tinggi (Cakra)",
                        Sku = "ING-FLR-HI-01",
                        PurchaseUnit = "Sak 25kg",
                        UsageUnit = "Gram",
                        ConversionRatio = 25000, // 25kg = 25.000g
                        AvgCostPerUsageUnit = 0.52m, // Asumsi Rp 13.000/kg -> Rp 13/g (Harga 2026 estimasi)
                        LastPurchasePrice = 325000,
                        CurrentStock = 50000, // Stok awal 2 Sak
                        MinStockLevel = 25000
                    },
                    new Ingredient
                    {
                        Name = "Tepung Terigu Protein Sedang (Segitiga)",
                        Sku = "ING-FLR-MED-01",
                        PurchaseUnit = "Sak 25kg",
                        UsageUnit = "Gram",
                        ConversionRatio = 25000,
                        AvgCostPerUsageUnit = 0.48m,
                        LastPurchasePrice = 300000,
                        CurrentStock = 25000,
                        MinStockLevel = 10000
                    },
                    new Ingredient
                    {
                        Name = "Ragi Instant (Fermipan/Saf)",
                        Sku = "ING-YST-01",
                        PurchaseUnit = "Box 500g",
                        UsageUnit = "Gram",
                        ConversionRatio = 500,
                        AvgCostPerUsageUnit = 120m, // Rp 60.000/500g
                        LastPurchasePrice = 60000,
                        CurrentStock = 2000,
                        MinStockLevel = 500
                    },
                    new Ingredient
                    {
                        Name = "Bread Improver (Baker's Bonus)",
                        Sku = "ING-IMP-01",
                        PurchaseUnit = "Pack 500g",
                        UsageUnit = "Gram",
                        ConversionRatio = 500,
                        AvgCostPerUsageUnit = 80m,
                        LastPurchasePrice = 40000,
                        CurrentStock = 1000,
                        MinStockLevel = 200
                    },

                    // --- LEMAK & MINYAK (CRITICAL FOR HPP) ---
                    new Ingredient
                    {
                        Name = "Margarine (Blueband Master)",
                        Sku = "ING-FAT-MRG-01",
                        PurchaseUnit = "Pail 15kg",
                        UsageUnit = "Gram",
                        ConversionRatio = 15000,
                        AvgCostPerUsageUnit = 30m,
                        LastPurchasePrice = 450000,
                        CurrentStock = 15000,
                        MinStockLevel = 5000
                    },
                    new Ingredient
                    {
                        Name = "Minyak Padat/Frying Fat (Cita Fry)",
                        Sku = "ING-OIL-FRY-01",
                        PurchaseUnit = "Karton 15kg",
                        UsageUnit = "Gram", // Minyak padat dihitung gram
                        ConversionRatio = 15000,
                        AvgCostPerUsageUnit = 28m,
                        LastPurchasePrice = 420000,
                        CurrentStock = 45000, // 3 Karton
                        MinStockLevel = 15000,
                        IsFryingOil = true // Flag penting untuk Logic Deep Fry!
                    },

                    // --- DAIRY & TELUR ---
                    new Ingredient
                    {
                        Name = "Susu Bubuk Full Cream",
                        Sku = "ING-MLK-PWD-01",
                        PurchaseUnit = "Sack 25kg",
                        UsageUnit = "Gram",
                        ConversionRatio = 25000,
                        AvgCostPerUsageUnit = 70m,
                        LastPurchasePrice = 1750000,
                        CurrentStock = 5000,
                        MinStockLevel = 1000
                    },
                    new Ingredient
                    {
                        Name = "Telur Ayam Negeri",
                        Sku = "ING-EGG-01",
                        PurchaseUnit = "Tray (30 Butir)",
                        UsageUnit = "Butir", // Bisa ubah ke Gram jika resep lo main berat (1 butir ~= 60g)
                        ConversionRatio = 30,
                        AvgCostPerUsageUnit = 2000m, // Rp 2000/butir
                        LastPurchasePrice = 60000,
                        CurrentStock = 300,
                        MinStockLevel = 60
                    },

                    // --- SWEETENERS ---
                    new Ingredient
                    {
                        Name = "Gula Pasir (Rafinasi)",
                        Sku = "ING-SGR-01",
                        PurchaseUnit = "Sak 50kg",
                        UsageUnit = "Gram",
                        ConversionRatio = 50000,
                        AvgCostPerUsageUnit = 16m,
                        LastPurchasePrice = 800000,
                        CurrentStock = 50000,
                        MinStockLevel = 25000
                    },
                    new Ingredient
                    {
                        Name = "Gula Halus (Dusting)",
                        Sku = "ING-SGR-DST-01",
                        PurchaseUnit = "Sak 25kg",
                        UsageUnit = "Gram",
                        ConversionRatio = 25000,
                        AvgCostPerUsageUnit = 20m,
                        LastPurchasePrice = 500000,
                        CurrentStock = 5000,
                        MinStockLevel = 2000
                    }
                };

                await _context.Ingredients.AddRangeAsync(starterIngredients);
                await _context.SaveChangesAsync();
            }
        }
    }
}