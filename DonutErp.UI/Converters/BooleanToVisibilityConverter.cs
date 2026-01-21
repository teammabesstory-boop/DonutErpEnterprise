using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DonutErp.UI.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isVisible = false;

            if (value is bool b)
            {
                isVisible = b;
            }
            else if (value is int i)
            {
                // Jika input angka (misal: Count), 0 dianggap False (Hidden), >0 dianggap True (Visible)
                isVisible = i > 0;
            }

            // Logic Invert (Pembalik)
            // Jika parameter="Invert", maka True jadi Collapsed, False jadi Visible
            if (parameter is string paramStr && paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}