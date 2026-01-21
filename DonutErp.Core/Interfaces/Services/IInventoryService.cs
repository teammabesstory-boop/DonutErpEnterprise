using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    // ==========================================
    // 1. INVENTORY SERVICE (PENJAGA GUDANG)
    // ==========================================
    public interface IInventoryService
    {
        // Ambil semua bahan baku
        Task<List<Ingredient>> GetAllIngredientsAsync();

        // Ambil bahan yang stoknya kritis (di bawah minimum)
        Task<List<Ingredient>> GetLowStockAlertsAsync();

        // Tambah/Edit Bahan Baku Baru
        Task AddOrUpdateIngredientAsync(Ingredient ingredient);

        // Stock Opname (Penyesuaian Stok Manual)
        // reason: "Pecah Telur", "Bonus Supplier", "Salah Hitung"
        Task AdjustStockAsync(Guid ingredientId, double realStockAmount, string reason);

        // Cek apakah bahan cukup untuk resep tertentu
        Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantityToMake);
    }
}