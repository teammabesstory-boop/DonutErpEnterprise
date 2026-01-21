using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace DonutErp.UI.Converters
{
    /// <summary>
    /// Mengubah angka (decimal/double) menjadi format Rupiah Indonesia.
    /// Contoh: 15000 -> "Rp 15.000"
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        // Cache CultureInfo Indonesia agar tidak create object berulang-ulang (Hemat Memori)
        private static readonly CultureInfo _idCulture = new CultureInfo("id-ID");

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "Rp 0";

            try
            {
                // Handle berbagai tipe data angka
                if (value is decimal d)
                    return d.ToString("C0", _idCulture); // C0 = Currency, 0 desimal

                if (value is double db)
                    return db.ToString("C0", _idCulture);

                if (value is int i)
                    return i.ToString("C0", _idCulture);

                if (value is long l)
                    return l.ToString("C0", _idCulture);

                // Jika string angka, coba parse dulu
                if (value is string s && decimal.TryParse(s, out decimal parsed))
                    return parsed.ToString("C0", _idCulture);
            }
            catch
            {
                // Fallback jika gagal convert
                return "Rp -";
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Binding biasanya OneWay (Tampil saja), jadi ini jarang dipakai.
            // Kecuali lo mau input rupiah lalu convert balik ke angka polos.
            throw new NotImplementedException();
        }
    }
}