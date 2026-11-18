using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Converters
{
	public class SelectedWorkerConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var selectedWorker = value as Workers;
			var currentWorker = parameter as Workers;

			return selectedWorker == currentWorker ? "LightGray" : "White"; // Change color when selected
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

}
