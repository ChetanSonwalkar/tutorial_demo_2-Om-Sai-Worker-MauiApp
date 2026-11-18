using tutorial_demo_2.Services;
using System.Globalization;

namespace tutorial_demo_2.Converters
{
    public class ImageUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                // Remove any leading slash from the image path
                imagePath = imagePath.TrimStart('/');
                
                // If path doesn't include media/profile_images, add it
                if (!imagePath.StartsWith("media/profile_images/"))
                {
                    imagePath = $"media/profile_images/{imagePath}";
                }

                var url = $"{ApiUrls.ImageUrl.TrimEnd('/')}/{imagePath}";
                System.Diagnostics.Debug.WriteLine($"Converted image URL: {url}");
                return url;
            }
            return "profile_placeholder.png"; // Default placeholder image
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
