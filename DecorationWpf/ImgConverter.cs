using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DecorationWpf
{
    public class ImgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    string fullPath = Path.IsPathRooted(path)
                        ? path
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", path);

                    if (File.Exists(fullPath))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bmp.EndInit();
                        return bmp;
                    }
                }
                catch { }
            }

            return new BitmapImage(new Uri("pack://application:,,,/Images/picture.png", UriKind.Absolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}