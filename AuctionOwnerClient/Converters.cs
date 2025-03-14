using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AuctionOwnerClient
{
    public class AuctionStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == "Pending" ? "Закрыть" : "Восстановить";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AuctionStatusToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status)
                return DependencyProperty.UnsetValue;

            // Получаем стиль глобально из Application.Resources
            return status == "Pending"
                ? Application.Current.TryFindResource("RedButtonStyle")
                : Application.Current.TryFindResource("GreenButtonStyle");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
