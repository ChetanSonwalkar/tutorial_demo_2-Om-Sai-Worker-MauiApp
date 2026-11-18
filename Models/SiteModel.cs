using System.Text.Json.Serialization;

namespace tutorial_demo_2.Models
{
    public class SiteResponse
    {
        public bool Success { get; set; }
        public List<SiteModel> Data { get; set; } = new();
        public object? Errors { get; set; }
    }

    public class SiteModel
    {
        public int SiteId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string SiteLocation { get; set; } = string.Empty;
        public string GpsLocation { get; set; } = string.Empty;
        public bool SiteStatus { get; set; }

        [JsonIgnore]
        public int SerialNumber { get; set; }
    }

    public class SiteSelectedModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string SiteGeoLocation { get; set; } = string.Empty;
    }
}
