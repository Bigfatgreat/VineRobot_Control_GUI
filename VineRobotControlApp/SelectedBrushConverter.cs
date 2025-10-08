using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VineRobotControlApp
{
    public class SelectedBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush ActiveBrush = new(Color.FromRgb(34, 197, 94));
        private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(209, 213, 219));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return ActiveBrush;
            }

            return InactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
