using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
                // 1. SYSTEM HEALTH CHECK & RECOVERY
                // Cek apakah database ada/korup. Jika korup, recreate.
                if (!await _context.Database.CanConnectAsync())
                {
                    await _context.Database.EnsureCreatedAsync();
                }
                else
                {
                    // Pastikan schema terbaru ter-apply (Migration Runtime)
                    // Untuk SQLite sederhana, EnsureCreated cukup cerdas skip jika sudah ada.
                    await _context.Database.EnsureCreatedAsync();
                }

                // 2. SECURITY BOOTSTRAP (ROOT USER)
                // Perusahaan Raksasa butuh Admin.
                if (!await _context.Users.AnyAsync())
                {
                    var rootUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "admin",
                        // Hashing Password Sederhana (SHA256) - Enterprise Grade harusnya pakai BCrypt/Argon2
                        // Ini simulasi "admin123"
                        PasswordHash = ComputeSha256Hash("admin123"),
                        FullName = "System Super Admin",
                        Role = UserRole.Owner, // Highest Privilege
                        IsActive = true
                    };

                    await _context.Users.AddAsync(rootUser);

                    // Catat Audit Log
                    await _context.AuditLogs.AddAsync(new AuditLog
                    {
                        Action = "SYSTEM_INIT",
                        EntityName = "User",
                        RecordId = rootUser.Id.ToString(),
                        Username = "SYSTEM",
                        ChangesJson = "Created Root User",
                        Timestamp = DateTime.Now
                    });
                }

                // 3. FINANCIAL INFRASTRUCTURE (WALLETS)
                // Setup akun keuangan dasar. Tanpa ini, modul Finance lumpuh.
                if (!await _context.Wallets.AnyAsync())
                {
                    var wallets = new List<Wallet>
                    {
                        // Kas Utama (Brankas)
                        new Wallet
                        {
                            Id = Guid.NewGuid(),
                            Name = "Main Vault (Brankas Besar)",
                            Type = WalletType.Cash,
                            CurrentBalance = 0, // Saldo awal 0, nanti diisi lewat Transaction "Modal Awal"
                            AccountNumber = "CASH-001"
                        },
                        
                        // Bank Account (Corporate)
                        new Wallet
                        {
                            Id = Guid.NewGuid(),
                            Name = "BCA Corporate",
                            Type = WalletType.Bank,
                            CurrentBalance = 0,
                            AccountNumber = "888-999-0000"
                        },
                        
                        // Petty Cash (Operasional Harian)
                        new Wallet
                        {
                            Id = Guid.NewGuid(),
                            Name = "Petty Cash (Kas Kecil)",
                            Type = WalletType.Cash,
                            CurrentBalance = 0,
                            AccountNumber = "PC-HQ-001"
                        }
                    };

                    await _context.Wallets.AddRangeAsync(wallets);
                }

                // 4. INVENTORY CONFIGURATION
                // Kita TIDAK memasukkan bahan dummy (Tepung, dll).
                // Tapi kita memastikan tabel Ingredients siap menerima data real.

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Critical Error Logging
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] SYSTEM BOOTSTRAP FAILED: {ex.Message}");
                // Dalam skenario real, ini harus kirim alert ke IT Admin
                throw;
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            // Simple hashing utility
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}