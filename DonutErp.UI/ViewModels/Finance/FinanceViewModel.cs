#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using Microsoft.UI.Xaml.Controls;

namespace DonutErp.UI.ViewModels.Finance
{
    public partial class FinanceViewModel : ObservableObject
    {
        private readonly IFinanceService _financeService;

        // ==========================================
        // 1. DASHBOARD SUMMARY (P&L REPORT)
        // ==========================================
        [ObservableProperty]
        private DateTimeOffset _startDate = DateTime.Now.AddDays(-30);

        [ObservableProperty]
        private DateTimeOffset _endDate = DateTime.Now;

        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private decimal _totalCogs;
        [ObservableProperty] private decimal _grossProfit;
        [ObservableProperty] private decimal _totalOpex;
        [ObservableProperty] private decimal _netProfit;
        [ObservableProperty] private double _netProfitMargin;

        // Breakdown Expense (Pie Chart Data)
        [ObservableProperty]
        private ObservableCollection<ExpenseCategorySummary> _expenseBreakdown = new();

        // ==========================================
        // 2. WALLET & CASHFLOW
        // ==========================================
        [ObservableProperty]
        private ObservableCollection<Wallet> _wallets = new();

        [ObservableProperty]
        private decimal _totalLiquidity; // Total Uang Cash + Bank

        // ==========================================
        // 3. TRANSACTION HISTORY
        // ==========================================
        [ObservableProperty]
        private ObservableCollection<Transaction> _recentTransactions = new();

        // ==========================================
        // 4. FORMS (INPUT DATA)
        // ==========================================

        // Form: Quick Expense
        [ObservableProperty] private string _expenseDesc = string.Empty;
        [ObservableProperty] private double _expenseAmt; // Double for NumberBox
        [ObservableProperty] private Wallet? _selectedExpenseWallet;
        [ObservableProperty] private string _expenseCategory = "Operasional";

        // Form: Transfer Funds
        [ObservableProperty] private Wallet? _sourceWallet;
        [ObservableProperty] private Wallet? _targetWallet;
        [ObservableProperty] private double _transferAmt;
        [ObservableProperty] private string _transferNotes = string.Empty;

        [ObservableProperty] private bool _isBusy;

        public FinanceViewModel(IFinanceService financeService)
        {
            _financeService = financeService;
            _ = LoadDashboardAsync();
        }

        [RelayCommand]
        public async Task LoadDashboardAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // 1. Load P&L Report
                var report = await _financeService.GenerateProfitLossReportAsync(StartDate.DateTime, EndDate.DateTime);

                TotalRevenue = report.TotalRevenue;
                TotalCogs = report.TotalCogs;
                GrossProfit = report.GrossProfit;
                TotalOpex = report.TotalOperationalExpense + report.TotalDepreciation; // Masukkan depresiasi ke Opex view
                NetProfit = report.NetProfit;

                // Hitung Margin %
                NetProfitMargin = TotalRevenue > 0 ? (double)(NetProfit / TotalRevenue) * 100 : 0;

                ExpenseBreakdown = new ObservableCollection<ExpenseCategorySummary>(report.ExpenseBreakdown);

                // 2. Load Wallets
                var walletList = await _financeService.GetWalletsAsync();
                Wallets = new ObservableCollection<Wallet>(walletList);
                TotalLiquidity = walletList.Sum(w => w.CurrentBalance);

                // Default selection for forms
                if (SelectedExpenseWallet == null) SelectedExpenseWallet = Wallets.FirstOrDefault();
                if (SourceWallet == null) SourceWallet = Wallets.FirstOrDefault();
                if (TargetWallet == null) TargetWallet = Wallets.LastOrDefault();

                // 3. Load History
                var txs = await _financeService.GetRecentTransactionsAsync(20);
                RecentTransactions = new ObservableCollection<Transaction>(txs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Load Finance: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SubmitExpenseAsync()
        {
            if (ExpenseAmt <= 0 || string.IsNullOrWhiteSpace(ExpenseDesc) || SelectedExpenseWallet == null) return;

            IsBusy = true;
            try
            {
                // Hardcoded user "Admin" sementara belum ada Login Session
                await _financeService.RecordExpenseAsync(
                    ExpenseDesc,
                    (decimal)ExpenseAmt,
                    DateTime.Now,
                    SelectedExpenseWallet.Id,
                    ExpenseCategory,
                    "Admin");

                // Reset & Refresh
                ExpenseDesc = string.Empty;
                ExpenseAmt = 0;
                await LoadDashboardAsync();

                await ShowDialog("Berhasil", "Pengeluaran tercatat & Saldo terpotong.");
            }
            catch (Exception ex)
            {
                await ShowDialog("Gagal", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ProcessTransferAsync()
        {
            if (SourceWallet == null || TargetWallet == null || TransferAmt <= 0) return;
            if (SourceWallet.Id == TargetWallet.Id)
            {
                await ShowDialog("Error", "Sumber dan Tujuan tidak boleh sama.");
                return;
            }

            IsBusy = true;
            try
            {
                await _financeService.TransferFundsAsync(
                    SourceWallet.Id,
                    TargetWallet.Id,
                    (decimal)TransferAmt,
                    TransferNotes,
                    "Admin");

                TransferAmt = 0;
                TransferNotes = "";
                await LoadDashboardAsync();

                await ShowDialog("Transfer Sukses", $"Dana berhasil dipindahkan ke {TargetWallet.Name}");
            }
            catch (Exception ex)
            {
                await ShowDialog("Transfer Gagal", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShowDialog(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok",
                XamlRoot = App.Current.MainWindow.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}