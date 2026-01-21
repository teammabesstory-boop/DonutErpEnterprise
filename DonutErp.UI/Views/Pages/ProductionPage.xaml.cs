using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using DonutErp.UI.ViewModels.Production;

namespace DonutErp.UI.Views.Pages
{
    public sealed partial class ProductionPage : Page
    {
        public ProductionViewModel ViewModel { get; }

        public ProductionPage()
        {
            this.InitializeComponent();

            // 1. Inject ViewModel
            ViewModel = App.Current.Services.GetRequiredService<ProductionViewModel>();

            // 2. Set DataContext (Penting untuk Binding tradisional {Binding})
            this.DataContext = ViewModel;

            // 3. SET NAMA HALAMAN (KRUSIAL!)
            // Di XAML tadi ada kode: ElementName=RootPage. 
            // Kita set nama halaman ini jadi "RootPage" agar tombol Hapus di tabel bisa nemu ViewModel induknya.
            this.Name = "RootPage";
        }
    }
}