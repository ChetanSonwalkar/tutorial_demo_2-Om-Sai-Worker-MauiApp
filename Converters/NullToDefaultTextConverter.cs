using System.Globalization;

namespace tutorial_demo_2.Converters
{
	public class NullToDefaultTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.IsNullOrWhiteSpace(value?.ToString()) ? (parameter?.ToString() ?? "Default Text") : value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
