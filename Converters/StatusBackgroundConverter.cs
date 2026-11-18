using System.Globalization;

namespace tutorial_demo_2.Converters
{
    public class StatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    // Present - Green theme
                    "present" => Color.FromArgb("#E8F5E9"),  // Light green
                    
                    // Absent - Red theme
                    "absent" => Color.FromArgb("#FFEBEE"),   // Light red
                    
                    // Leave - Blue theme
                    "leave" => Color.FromArgb("#E1F5FE"),    // Light blue
                    
                    // On-time entries - Purple theme
                    "on-time-in" => Color.FromArgb("#EDE7F6"),  // Light purple
                    "on-time-out" => Color.FromArgb("#F3E5F5"), // Light pink-purple
                    
                    // Late/Early - Orange/Red theme
                    "late-in" => Color.FromArgb("#FFF3E0"),     // Light orange
                    "early-out" => Color.FromArgb("#FFEBEE"),   // Light red
                    
                    // Half day - Teal theme
                    "half-day-in" => Color.FromArgb("#E0F2aa"), // Light teal
                    "half-day" => Color.FromArgb("#E0F2F1"),    // Light teal
                    
                    // Default - Gray theme
                    _ => Color.FromArgb("#F5F5F5")              // Light gray
                };
            }
            return Color.FromArgb("#F5F5F5"); // Default light gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
