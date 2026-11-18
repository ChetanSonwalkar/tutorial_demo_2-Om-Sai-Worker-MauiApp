using System.Globalization;

namespace tutorial_demo_2.Converters
{
    public class BoolToVerificationStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVerified)
            {
                return isVerified ? "Verified" : "Pending";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
