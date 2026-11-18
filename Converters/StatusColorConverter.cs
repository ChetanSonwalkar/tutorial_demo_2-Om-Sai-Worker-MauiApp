using System.Globalization;

namespace tutorial_demo_2.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    // Present - Green theme
                    "present" => Color.FromArgb("#1B5E20"),  // Dark green
                    
                    // Absent - Red theme
                    "absent" => Color.FromArgb("#B71C1C"),   // Dark red
                    
                    // Leave - Blue theme
                    "leave" => Color.FromArgb("#01579B"),    // Dark blue
                    
                    // On-time entries - Purple theme
                    "on-time-in" => Color.FromArgb("#4527A0"),  // Dark purple
                    "on-time-out" => Color.FromArgb("#6A1B9A"), // Dark pink-purple
                    
                    // Late/Early - Orange/Red theme
                    "late-in" => Color.FromArgb("#E65100"),     // Dark orange
                    "early-out" => Color.FromArgb("#C62828"),   // Dark red
                    
                    // Half day - Teal theme
                    "half-day-in" => Color.FromArgb("#0069bb"), // Dark teal
                    "half-day" => Color.FromArgb("#00695C"),    // Dark teal
                    
                    // Default - Gray theme
                    _ => Color.FromArgb("#757575")              // Dark gray
                };
            }
            return Color.FromArgb("#757575"); // Default gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
