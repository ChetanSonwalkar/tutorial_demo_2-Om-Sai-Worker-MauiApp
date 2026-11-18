using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{

	public class LoginRequest
	{
		public string LoginIdentifier { get; set; }
		public string Password { get; set; }		
	}

	public class LoginResponse
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public LoginData Data { get; set; }

		[JsonPropertyName("errors")]
		public object Errors { get; set; }
	}

	public class LoginData
	{
		[JsonPropertyName("workerId")]
		public int WorkerId { get; set; }

		[JsonPropertyName("workmanId")]
		public string WorkmanId { get; set; }

		[JsonPropertyName("profileImage")]
		public string ProfileImage { get; set; }

		[JsonPropertyName("firstName")]
		public string FirstName { get; set; }

		[JsonPropertyName("middleName")]
		public string MiddleName { get; set; }

		[JsonPropertyName("lastName")]
		public string LastName { get; set; }


		[JsonPropertyName("siteId")]
		public int SiteId { get; set; }

		[JsonPropertyName("attendanceGeoRadius")]
		public int AttendanceGeoRadius { get; set; }

		[JsonPropertyName("siteName")]
		public string SiteName { get; set; }
		public string SiteGeoLocation { get; set; }




		[JsonPropertyName("token")]
		public string Token { get; set; }



		public string FullName => $"{FirstName}{(string.IsNullOrEmpty(MiddleName) ? "" : $" {MiddleName}")}{(string.IsNullOrEmpty(LastName) ? "" : $" {LastName}")}".Trim();
	}

}
