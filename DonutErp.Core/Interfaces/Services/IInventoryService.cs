using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    public interface IInventoryService
    {
        // ==========================================
        // 1. BASIC INVENTORY OPERATIONS
        // ==========================================
        Task<List<Ingredient>> GetAllIngredientsAsync();
        Task<Ingredient?> GetIngredientByIdAsync(Guid id);
        Task<List<Ingredient>> GetLowStockAlertsAsync();
        Task AddOrUpdateIngredientAsync(Ingredient ingredient);

        // Advanced Stock Adjustment dengan Audit Trail
        Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason, string username);

        // Smart Check: Apakah cukup stok untuk bikin X donat? (Memperhitungkan BOM bertingkat)
        Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake);

        // ==========================================
        // 2. PRODUCT ENGINEERING (BOM & RECIPE)
        // ==========================================
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<List<Recipe>> GetRecipeByProductAsync(Guid productId);
        Task UpdateRecipeAsync(Guid productId, List<Recipe> newRecipes);
        Task AddOrUpdateProductAsync(Product product);

        // ==========================================
        // 3. THE "BRAIN" (ADVANCED ANALYTICS)
        // ==========================================

        // Hitung HPP secara Rekursif (Menelusuri Sub-Product sedalam mungkin)
        Task<decimal> RecalculateProductHppAsync(Guid productId);

        // AI Lite: Prediksi kebutuhan stok untuk N hari ke depan berdasarkan history pemakaian
        Task<double> PredictStockUsageAsync(Guid ingredientId, int daysToPredict);

        // AI Lite: Analisa tren harga supplier (Naik/Turun/Stabil)
        Task<string> AnalyzePriceTrendAsync(Guid ingredientId);

        // Record History Harga Beli (dipanggil saat Purchase Order)
        Task RecordPriceHistoryAsync(Guid ingredientId, decimal newPrice, string supplierName);
    }
}