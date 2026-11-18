using System.Globalization;


namespace tutorial_demo_2.Services
{
	public class WorkingHrsToTimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int totalMinutes)
			{
				int hours = totalMinutes / 60;
				int minutes = totalMinutes % 60;
				return $"{hours:D2}:{minutes:D2}"; // Formats as HH:mm
			}
			return "00:00";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException(); // Usually, ConvertBack is not needed
		}
	}
}
