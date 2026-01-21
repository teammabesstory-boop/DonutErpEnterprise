using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using DonutErp.UI.ViewModels.Finance;

namespace DonutErp.UI.Views.Pages
{
    public sealed partial class FinancePage : Page
    {
        public FinanceViewModel ViewModel { get; }

        public FinancePage()
        {
            this.InitializeComponent();

            // Inject ViewModel dari Service Container
            ViewModel = App.Current.Services.GetRequiredService<FinanceViewModel>();
        }
    }
}