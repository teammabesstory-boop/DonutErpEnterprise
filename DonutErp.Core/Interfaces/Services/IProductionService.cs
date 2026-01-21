using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    public interface IProductionService
    {
        // ==========================================
        // 1. BATCH LIFECYCLE MANAGEMENT
        // ==========================================
        Task<List<ProductionBatch>> GetActiveBatchesAsync();
        Task<ProductionBatch?> GetBatchByIdAsync(Guid id);

        // Step 1: Planning (Cuma niat bikin, belum potong stok)
        Task<ProductionBatch> CreatePlannedBatchAsync(string batchCode, string? notes);

        // Step 2: Start Production (Catat level minyak awal)
        Task StartBatchAsync(Guid batchId, double oilStartLevelLiter);

        // Step 3: Add Oil (Refill di tengah jalan)
        Task RefillOilAsync(Guid batchId, double litersAdded);

        // Step 4: Record Output (QC Pass/Reject)
        Task AddOutputAsync(Guid batchId, Guid productId, int qtyGood, int qtyReject);

        // Step 5: Finish & Costing (The heavy calculation happens here)
        // Parameter: Level minyak akhir, Biaya Gaji, Biaya Listrik/Gas
        Task<ProductionBatch> CompleteBatchAsync(Guid batchId, double oilEndLevelLiter, decimal laborCost, decimal utilitiesCost, string username);

        // ==========================================
        // 2. ANALYTICS & REPORTING
        // ==========================================
        Task<List<ProductionBatch>> GetBatchHistoryAsync(DateTime from, DateTime to);

        // Menghitung HPP Teoritis (Ideal) vs Aktual untuk analisa varians
        Task<decimal> CompareTheoreticalVsActualCostAsync(Guid batchId);
    }
}