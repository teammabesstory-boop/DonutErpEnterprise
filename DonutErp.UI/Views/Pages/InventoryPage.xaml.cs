using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection; // Wajib untuk GetRequiredService
using DonutErp.UI.ViewModels.Inventory;

namespace DonutErp.UI.Views.Pages
{
    /// <summary>
    /// Halaman Gudang (Inventory).
    /// </summary>
    public sealed partial class InventoryPage : Page
    {
        // Property ViewModel agar bisa dibaca oleh x:Bind di XAML
        public InventoryViewModel ViewModel { get; }

        public InventoryPage()
        {
            this.InitializeComponent();

            // =========================================================
            // MANUAL INJECTION (SERVICE LOCATOR PATTERN)
            // =========================================================
            // Karena WinUI Page dibuat otomatis oleh Frame navigasi,
            // kita tarik ViewModel dari DI Container yang sudah kita setup di App.xaml.cs

            ViewModel = App.Current.Services.GetRequiredService<InventoryViewModel>();

            // Set DataContext untuk Binding tradisional (jika ada yg tidak pakai x:Bind)
            this.DataContext = ViewModel;
        }

        // =========================================================
        // UI EVENT HANDLERS
        // =========================================================

        /// <summary>
        /// Logic agar user bisa tekan ENTER di search box untuk mencari.
        /// </summary>
        private void TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // Pakai Command dari ViewModel
                if (ViewModel.SearchCommand.CanExecute(null))
                {
                    ViewModel.SearchCommand.Execute(null);
                }
            }
        }
    }
}