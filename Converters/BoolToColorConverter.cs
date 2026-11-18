using System.Globalization;

namespace tutorial_demo_2.Converters
{
	public class BoolToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool boolValue && parameter is string colorParams)
			{
				var colors = colorParams.Split(',');
				if (colors.Length == 2)
				{
					string selectedColor = boolValue ? colors[0].Trim() : colors[1].Trim();

					// Ensure the selected color is a valid ARGB hex before converting
					try
					{
						return Color.FromArgb(selectedColor);
					}
					catch
					{
						return Colors.Transparent; // Fallback if color conversion fails
					}
				}
			}
			return Colors.Transparent; // Default fallback
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
