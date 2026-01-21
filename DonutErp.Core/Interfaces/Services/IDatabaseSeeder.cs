using System.Threading.Tasks;

namespace DonutErp.Core.Interfaces.Services
{
    // ==========================================
    // 4. DATA SEEDER (INITIALIZER)
    // ==========================================
    // Kontrak untuk mengisi data awal (Bahan baku umum) biar aplikasi gak kosong melompong saat pertama run.
    public interface IDatabaseSeeder
    {
        Task SeedInitialDataAsync();
    }
}