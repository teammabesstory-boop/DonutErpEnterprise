using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using DonutErp.UI.Views.Pages; // Pastikan namespace ini benar

namespace DonutErp.UI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Set Judul Jendela
            this.Title = "Donut ERP - Enterprise Edition";
        }

        // =========================================================
        // 1. SAAT APLIKASI PERTAMA KALI DIBUKA
        // =========================================================
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Otomatis pilih menu pertama (Gudang/Inventory)
            // Pastikan menu Inventory ada di urutan pertama (index 0) di XAML
            if (NavView.MenuItems.Count > 0)
            {
                NavView.SelectedItem = NavView.MenuItems[0];

                // Load Halaman Inventory sebagai default
                NavView_Navigate("InventoryPage", new EntranceNavigationTransitionInfo());
            }
        }

        // =========================================================
        // 2. SAAT MENU DIKLIK (ROUTER)
        // =========================================================
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // TODO: Buat SettingsPage nanti
                // NavView_Navigate("SettingsPage", args.RecommendedNavigationTransitionInfo);
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;

                // Cek agar tidak error jika Tag null
                if (selectedItem?.Tag != null)
                {
                    string pageTag = selectedItem.Tag.ToString();
                    NavView_Navigate(pageTag, args.RecommendedNavigationTransitionInfo);
                }
            }
        }

        // =========================================================
        // 3. LOGIC GANTI HALAMAN (NAVIGATOR)
        // =========================================================
        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            Type _page = null;

            switch (navItemTag)
            {
                // --- HALAMAN YANG SUDAH JADI ---
                case "InventoryPage":
                    _page = typeof(InventoryPage);
                    break;

                case "ProductionPage":
                    _page = typeof(ProductionPage);
                    break;

                case "FinancePage":
                    _page = typeof(FinancePage);
                    break;

                case "PosPage":
                    _page = typeof(PosPage);
                    break;

            }

            // Dapatkan tipe halaman yang sedang aktif sekarang
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Hanya navigate jika halaman tujuan valid DAN berbeda dengan halaman sekarang
            if (_page != null && !Type.Equals(preNavPageType, _page))
            {
                ContentFrame.Navigate(_page, null, transitionInfo);
            }
        }
    }
}