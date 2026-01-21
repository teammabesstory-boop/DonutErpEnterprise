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

        // =================================================================
        // 1. DATA RETRIEVAL (GET)
        // =================================================================

        public async Task<List<Ingredient>> GetAllIngredientsAsync()
        {
            // Mengambil semua bahan baku, diurutkan dari yang stoknya paling tipis
            return await _context.Ingredients
                .OrderBy(i => i.CurrentStock)
                .AsNoTracking() // Performance Optimization: Read-only query lebih cepat
                .ToListAsync();
        }

        public async Task<List<Ingredient>> GetLowStockAlertsAsync()
        {
            // Filter bahan yang stoknya <= Minimum Level
            return await _context.Ingredients
                .Where(i => i.CurrentStock <= i.MinStockLevel)
                .AsNoTracking()
                .ToListAsync();
        }

        // =================================================================
        // 2. MODIFICATION (ADD/EDIT)
        // =================================================================

        public async Task AddOrUpdateIngredientAsync(Ingredient ingredient)
        {
            if (ingredient.Id == Guid.Empty)
            {
                // New Ingredient
                ingredient.Id = Guid.NewGuid();
                await _context.Ingredients.AddAsync(ingredient);
            }
            else
            {
                // Update Existing
                _context.Ingredients.Update(ingredient);
            }

            await _context.SaveChangesAsync();
        }

        // =================================================================
        // 3. STOCK OPNAME (PENYESUAIAN STOK)
        // =================================================================
        // Fitur ini digunakan saat fisik gudang beda dengan sistem (misal: pecah, hilang, bonus).
        // Kita tidak hanya ubah angka, tapi catat ADJUSTMENT TRANSACTION di keuangan.
        public async Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ingredient = await _context.Ingredients.FindAsync(ingredientId);
                if (ingredient == null) throw new Exception("Ingredient not found");

                double oldStock = ingredient.CurrentStock;
                double diff = realStockAmount - oldStock; // Selisih (+ atau -)

                if (diff == 0) return; // Tidak ada perubahan

                // 1. Update Stok Master
                ingredient.CurrentStock = realStockAmount;
                ingredient.UpdatedAt = DateTime.Now;
                _context.Ingredients.Update(ingredient);

                // 2. Catat Log Keuangan (Adjustment)
                // Jika Stok Fisik LEBIH BANYAK dari Sistem (Diff Positif) -> Keuntungan (Inventory Gain)
                // Jika Stok Fisik LEBIH DIKIT dari Sistem (Diff Negatif) -> Kerugian (Inventory Loss/Shrinkage)

                decimal adjustmentValue = (decimal)diff * ingredient.AvgCostPerUsageUnit;

                var adjustmentLog = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"ADJ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    Date = DateTime.Now,
                    Type = TransactionType.Adjustment,
                    Notes = $"Stock Opname: {ingredient.Name}. {reason}",
                    TotalAmount = adjustmentValue, // Bisa minus (Rugi) atau plus (Untung)
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

        // =================================================================
        // 4. VALIDASI KETERSEDIAAN RESEP
        // =================================================================
        public async Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake)
        {
            // Ambil Resep Produk
            var recipeItems = await _context.RecipeItems
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            if (!recipeItems.Any()) return true; // Gak pake bahan apa-apa? Aman.

            foreach (var item in recipeItems)
            {
                // Cek Stok Gudang
                var ingredient = await _context.Ingredients.FindAsync(item.IngredientId);
                if (ingredient == null) continue;

                double needed = item.Amount * quantityToMake;

                if (ingredient.CurrentStock < needed)
                {
                    return false; // Ada satu bahan aja gak cukup, batal.
                }
            }

            return true;
        }
    }
}