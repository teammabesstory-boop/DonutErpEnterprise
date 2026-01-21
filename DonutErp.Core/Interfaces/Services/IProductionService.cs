using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DonutErp.Core.Entities;

namespace DonutErp.Core.Interfaces.Services
{
    public interface IProductionService
    {
        // Hitung HPP Teoritis (Async)
        Task<decimal> CalculateTheoreticalHppAsync(Guid productId);

        // Proses Produksi (Async)
        Task<ProductionBatch> CreateProductionBatchAsync(ProductionBatch batch, List<ProductionOutput> outputs);

        // History Batch (Async)
        Task<List<ProductionBatch>> GetRecentBatchesAsync();

        // Hitung Susut Minyak (Async)
        // PENTING: Pakai Task<decimal> biar cocok sama Service
        Task<decimal> CalculateOilLossCost(double startLevel, double endLevel, double addedLiter, decimal pricePerLiter);
    }
}