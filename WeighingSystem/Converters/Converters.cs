using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WeighingSystem.Converters
{
    /// <summary>
    /// Нужен для отображения фотки в форме
    /// </summary>
    public class ByteArrayToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                // Преобразование массива байтов в BitmapImage
                using (var stream = new MemoryStream(bytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Обеспечиваем потокобезопасность
                    return bitmap;
                }
            }
            else if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
            {
                // Преобразование пути к файлу в BitmapImage
                try
                {
                    var bitmap = new BitmapImage(new Uri(path, UriKind.Absolute));
                    bitmap.Freeze(); // Обеспечиваем потокобезопасность
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    return null;
                }
            }
            return null; // Если значение не поддерживается или файл не существует
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
