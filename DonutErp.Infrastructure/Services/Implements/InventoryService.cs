#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;

namespace DonutErp.Infrastructure.Services.Implements
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Ingredient>> GetAllIngredientsAsync()
        {
            return await _context.Ingredients
                .OrderBy(i => i.CurrentStock)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Ingredient>> GetLowStockAlertsAsync()
        {
            return await _context.Ingredients
                .Where(i => i.CurrentStock <= i.MinStockLevel)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddOrUpdateIngredientAsync(Ingredient ingredient)
        {
            if (ingredient.Id == Guid.Empty)
            {
                ingredient.Id = Guid.NewGuid();
                await _context.Ingredients.AddAsync(ingredient);
            }
            else
            {
                _context.Ingredients.Update(ingredient);
            }

            // PENTING: Jika harga bahan berubah, HPP produk yang pakai bahan ini harus diupdate!
            // Tapi untuk sekarang kita simpan dulu biar gak berat.
            await _context.SaveChangesAsync();
        }

        public async Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ingredient = await _context.Ingredients.FindAsync(ingredientId);
                if (ingredient == null) throw new Exception("Ingredient not found");

                double diff = realStockAmount - ingredient.CurrentStock;
                if (diff == 0) return;

                // Update Master Data
                ingredient.CurrentStock = realStockAmount;
                ingredient.UpdatedAt = DateTime.Now;
                _context.Ingredients.Update(ingredient);

                // Catat History
                var adjustmentLog = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"ADJ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Date = DateTime.Now,
                    Type = TransactionType.StockAdjustment,
                    Description = $"Stock Opname: {ingredient.Name}",
                    Notes = reason,
                    TotalAmount = (decimal)diff * ingredient.AvgCostPerUsageUnit,
                    TotalCost = 0
                };

                await _context.Transactions.AddAsync(adjustmentLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake)
        {
            var recipes = await _context.Recipes
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            if (!recipes.Any()) return true;

            foreach (var item in recipes)
            {
                var ingredient = await _context.Ingredients.FindAsync(item.IngredientId);
                if (ingredient == null) continue;

                double needed = item.Quantity * quantityToMake;
                if (ingredient.CurrentStock < needed) return false;
            }
            return true;
        }

        // ==========================================================
        // FITUR BARU: MANAJEMEN RESEP & HPP REAL
        // ==========================================================

        public async Task<List<Recipe>> GetRecipeByProductAsync(Guid productId)
        {
            return await _context.Recipes
                .Include(r => r.Ingredient) // Join ke tabel Ingredient
                .Where(r => r.ProductId == productId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpdateRecipeAsync(Guid productId, List<Recipe> newRecipes)
        {
            // 1. Hapus resep lama
            var oldRecipes = await _context.Recipes.Where(r => r.ProductId == productId).ToListAsync();
            _context.Recipes.RemoveRange(oldRecipes);

            // 2. Masukkan resep baru
            foreach (var item in newRecipes)
            {
                item.Id = Guid.NewGuid();
                item.ProductId = productId;
                // Pastikan IngredientId valid
            }
            await _context.Recipes.AddRangeAsync(newRecipes);
            await _context.SaveChangesAsync();

            // 3. Auto-Calculate HPP Baru
            await RecalculateProductHppAsync(productId);
        }

        public async Task<decimal> RecalculateProductHppAsync(Guid productId)
        {
            // Logic: HPP = Sum(Qty * AvgCostPerUnit)
            var recipes = await _context.Recipes
                .Include(r => r.Ingredient)
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            decimal newHpp = 0;

            foreach (var r in recipes)
            {
                if (r.Ingredient != null)
                {
                    // Rumus: Pemakaian x Harga Rata-rata Bahan
                    newHpp += (decimal)r.Quantity * r.Ingredient.AvgCostPerUsageUnit;
                }
            }

            // Simpan HPP baru ke Master Produk
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.CachedHpp = newHpp;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }

            return newHpp;
        }
    }
}