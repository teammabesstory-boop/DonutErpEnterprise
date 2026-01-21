#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Controls;

namespace DonutErp.UI.ViewModels.POS
{
    public partial class PosViewModel : ObservableObject
    {
        private readonly IFinanceService _financeService;
        private readonly AppDbContext _context;

        // ==========================================
        // STATE: KATALOG PRODUK
        // ==========================================
        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private Product? _selectedProduct;

        // ==========================================
        // STATE: KERANJANG BELANJA
        // ==========================================
        [ObservableProperty]
        private ObservableCollection<TransactionDetail> _cartItems = new();

        [ObservableProperty]
        private decimal _grandTotal;

        // ==========================================
        // STATE: PEMBAYARAN
        // ==========================================
        // FIX: Ubah jadi double agar kompatibel dengan NumberBox UI
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount))]
        private double _paymentAmount;

        // Logic Kembalian: Cast PaymentAmount ke decimal dulu
        public decimal ChangeAmount => (decimal)PaymentAmount > GrandTotal ? (decimal)PaymentAmount - GrandTotal : 0;

        [ObservableProperty]
        private bool _isBusy;

        public PosViewModel(IFinanceService financeService, AppDbContext context)
        {
            _financeService = financeService;
            _context = context;

            _ = LoadProductsAsync();
        }

        [RelayCommand]
        public async Task LoadProductsAsync()
        {
            var list = await _context.Products
                .Where(p => p.Type != ProductType.RawMaterial)
                .AsNoTracking()
                .ToListAsync();

            Products = new ObservableCollection<Product>(list);
        }

        [RelayCommand]
        public void AddToCart(Product product)
        {
            if (product == null) return;

            var existingItem = CartItems.FirstOrDefault(x => x.ProductId == product.Id);

            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + 1;
                CartItems.Remove(existingItem);

                existingItem.Quantity = newQty;
                CartItems.Add(existingItem);
            }
            else
            {
                var newItem = new TransactionDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    Quantity = 1,
                    PriceAtSale = product.SellingPrice,
                    CostAtSale = product.CachedHpp
                };
                CartItems.Add(newItem);
            }

            RecalculateTotal();
        }

        [RelayCommand]
        public void RemoveFromCart(TransactionDetail item)
        {
            if (CartItems.Contains(item))
            {
                CartItems.Remove(item);
                RecalculateTotal();
            }
        }

        [RelayCommand]
        public void ClearCart()
        {
            CartItems.Clear();
            RecalculateTotal();
            PaymentAmount = 0;
        }

        private void RecalculateTotal()
        {
            GrandTotal = CartItems.Sum(x => x.Quantity * x.PriceAtSale);
            OnPropertyChanged(nameof(ChangeAmount));
        }

        [RelayCommand]
        public async Task ProcessCheckoutAsync()
        {
            if (CartItems.Count == 0) return;

            // FIX: Cast PaymentAmount ke decimal untuk validasi
            if ((decimal)PaymentAmount < GrandTotal)
            {
                await ShowDialog("Pembayaran Kurang", "Uang yang dibayarkan kurang dari total belanja.");
                return;
            }

            IsBusy = true;
            try
            {
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"POS-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    Date = DateTime.Now,
                    PaymentMethod = "CASH",
                    TotalAmount = GrandTotal,
                    Details = CartItems.ToList()
                };

                await _financeService.RecordSalesAsync(transaction);
                await ShowDialog("Transaksi Berhasil", $"Kembalian: Rp {ChangeAmount:N0}");
                ClearCart();
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
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