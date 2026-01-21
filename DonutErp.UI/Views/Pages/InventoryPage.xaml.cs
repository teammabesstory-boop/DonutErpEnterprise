using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using DonutErp.UI.ViewModels.Inventory;
using Microsoft.UI.Xaml.Input;

namespace DonutErp.UI.Views.Pages
{
    public sealed partial class InventoryPage : Page
    {
        public InventoryViewModel ViewModel { get; }

        public InventoryPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<InventoryViewModel>();
        }

        private void TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                ViewModel.SearchCommand.Execute(textBox.Text);
            }
        }

        // Event Handler untuk Search Box (AutoSuggestBox)
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Panggil Command Search di ViewModel
            // UserReason = User mengetik
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.SearchCommand.Execute(sender.Text);
            }
        }
    }
}