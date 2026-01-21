using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    public interface IInventoryService
    {
        // --- EXISTING ---
        Task<List<Ingredient>> GetAllIngredientsAsync();
        Task<List<Ingredient>> GetLowStockAlertsAsync();
        Task AddOrUpdateIngredientAsync(Ingredient ingredient);
        Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason);
        Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake);

        // --- NEW FEATURES (HPP & RECIPE) ---

        // 1. Ambil Resep untuk Produk tertentu
        Task<List<Recipe>> GetRecipeByProductAsync(Guid productId);

        // 2. Update/Tambah Resep (Misal: Ubah takaran tepung)
        Task UpdateRecipeAsync(Guid productId, List<Recipe> newRecipes);

        // 3. HITUNG ULANG HPP (Fitur Paling Mahal)
        // Dipanggil setiap kali harga bahan baku berubah atau resep berubah
        Task<decimal> RecalculateProductHppAsync(Guid productId);
    }
}