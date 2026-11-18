using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{
    public class UserDetailsModel
    {
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("workerId")]
		public int WorkerId { get; set; }

		[JsonPropertyName("workmanId")]
		public string WorkmanId { get; set; }

		[JsonPropertyName("profileImage")]
		public string ProfileImage { get; set; }


		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("email")]
		public string Email { get; set; }

		[JsonPropertyName("mobile")]
		public string Mobile { get; set; }


		[JsonPropertyName("siteId")]
		public int SiteId { get; set; }

		[JsonPropertyName("attendanceGeoRadius")]
		public int AttendanceGeoRadius { get; set; }


		[JsonPropertyName("siteName")]
		public string SiteName { get; set; }
		public string SiteGeoLocation { get; set; }

		
		[JsonPropertyName("token")]
		public string Token { get; set; }

		[JsonPropertyName("role")]
		public string Role { get; set; }

		[JsonPropertyName("isActive")]
		public bool IsActive { get; set; }

		public override string ToString()
		{
			return $"{Name} ({Email})";
		}

		public static implicit operator string(UserDetailsModel userDetails)
		{
			return userDetails?.ToString();
		}

		public static explicit operator UserDetailsModel(string value)
		{
			throw new InvalidOperationException("Cannot convert string to UserDetails directly. Use JSON deserialization instead.");
		}
	}
}
