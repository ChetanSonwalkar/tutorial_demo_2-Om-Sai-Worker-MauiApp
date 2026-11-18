using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace tutorial_demo_2.Converters
{
	public class TupleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// value = WorkmanId (from binding)
			// parameter = WorkerId (from ConverterParameter)

			// Ensure WorkmanId is a string and WorkerId is an int?
			string? workmanId = value as string;
			int? workerId = parameter as int?;

			// Return a ValueTuple<string?, int?>
			return (workmanId, workerId);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
