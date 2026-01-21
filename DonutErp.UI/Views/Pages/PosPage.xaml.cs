using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using DonutErp.UI.ViewModels.POS;

namespace DonutErp.UI.Views.Pages
{
    public sealed partial class PosPage : Page
    {
        public PosViewModel ViewModel { get; }

        public PosPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<PosViewModel>();

            // PENTING: Beri nama halaman agar Binding ElementName di XAML bisa menemukan ViewModel
            this.Name = "PosRoot";
        }
    }
}