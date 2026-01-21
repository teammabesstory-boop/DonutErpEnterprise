#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;

namespace DonutErp.UI.ViewModels.Finance
{
    public partial class FinanceViewModel : ObservableObject
    {
        private readonly IFinanceService _financeService;

        // ==========================================
        // STATE: FILTER PERIODE
        // ==========================================
        [ObservableProperty]
        private DateTimeOffset _startDate = DateTime.Now.AddDays(-30);

        [ObservableProperty]
        private DateTimeOffset _endDate = DateTime.Now;

        // ==========================================
        // STATE: KARTU UTAMA (DASHBOARD)
        // ==========================================
        [ObservableProperty]
        private decimal _totalRevenue;

        [ObservableProperty]
        private decimal _totalCogs;

        [ObservableProperty]
        private decimal _netProfit;

        [ObservableProperty]
        private bool _isLoading;

        // ==========================================
        // STATE: DATA PENDUKUNG
        // ==========================================
        [ObservableProperty]
        private ObservableCollection<TopProductDisplay> _topProducts = new();

        // Input Pengeluaran Cepat (Quick Expense)
        [ObservableProperty]
        private string _expenseDescription = string.Empty;

        // FIX: Ubah jadi double agar kompatibel dengan UI NumberBox
        [ObservableProperty]
        private double _expenseAmount;

        public FinanceViewModel(IFinanceService financeService)
        {
            _financeService = financeService;
            _ = LoadDashboardDataAsync();
        }

        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var summary = await _financeService.GetProfitLossSummaryAsync(StartDate.DateTime, EndDate.DateTime);

                TotalRevenue = summary.TotalRevenue;
                TotalCogs = summary.TotalCogs;
                NetProfit = summary.NetProfit;

                var topList = await _financeService.GetTopSellingProductsAsync(5);

                TopProducts.Clear();
                foreach (var item in topList)
                {
                    TopProducts.Add(new TopProductDisplay(item.ProductName, item.QtySold));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Load Finance: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task SubmitExpenseAsync()
        {
            if (string.IsNullOrWhiteSpace(ExpenseDescription) || ExpenseAmount <= 0) return;

            IsLoading = true;
            try
            {
                // FIX: Cast double ke decimal saat kirim ke Service
                await _financeService.RecordExpenseAsync(ExpenseDescription, (decimal)ExpenseAmount, DateTime.Now);

                ExpenseDescription = string.Empty;
                ExpenseAmount = 0;

                await LoadDashboardDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public record TopProductDisplay(string Name, int Qty);
}