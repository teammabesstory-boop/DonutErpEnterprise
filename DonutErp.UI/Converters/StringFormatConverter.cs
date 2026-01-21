using Microsoft.UI.Xaml.Data;
using System;

namespace DonutErp.UI.Converters
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Parameter adalah format string, misal: "Total Items: {0}"
            // Value adalah nilainya, misal: 5

            if (value == null) return string.Empty;
            if (parameter == null) return value.ToString();

            try
            {
                return string.Format((string)parameter, value);
            }
            catch
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}