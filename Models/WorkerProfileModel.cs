using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{

	public class WorkerListResponse<T>
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public List<T> Data { get; set; }

		[JsonPropertyName("errors")]
		public object Errors { get; set; }
	}

	public class WorkerListData
	{
		[JsonPropertyName("workerId")]
		public int? WorkerId { get; set; }

		[JsonPropertyName("workmanId")]
		public string WorkmanId { get; set; }

		[JsonPropertyName("firstName")]
		public string FirstName { get; set; }

		[JsonPropertyName("middleName")]
		public string MiddleName { get; set; }

		[JsonPropertyName("lastName")]
		public string LastName { get; set; }

		[JsonPropertyName("name")]
		public string Name => $"{FirstName} {LastName}";

		
		[JsonPropertyName("profileImage")]
		public string? ProfileImage { get; set; }



	}

	public class WorkerProfileResponse
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public WorkerProfileData Data { get; set; }

		[JsonPropertyName("errors")]
		public object Errors { get; set; }
	}


	public class WorkerProfileData
	{
		[JsonPropertyName("worker")]
		public Worker Worker { get; set; }

		[JsonPropertyName("address")]
		public List<Address> Addresses { get; set; }

		[JsonPropertyName("documents")]
		public List<Document> Documents { get; set; }

		// Helper property to get SiteId from Worker
		public int SiteId => Worker?.SiteId ?? 0;
	}

	public class Worker
	{
		[JsonPropertyName("workerId")]
		public int? WorkerId { get; set; }

		[JsonPropertyName("workmanId")]
		public string WorkmanId { get; set; }

		[JsonPropertyName("firstName")]
		public string FirstName { get; set; }

		[JsonPropertyName("middleName")]
		public string MiddleName { get; set; }

		[JsonPropertyName("lastName")]
		public string LastName { get; set; }

		[JsonPropertyName("fullName")]
		public string FullName => $"{FirstName} {LastName}";

		[JsonPropertyName("departmentId")]
		public int? DepartmentId { get; set; }

		[JsonPropertyName("departmentName")]
		public string DepartmentName { get; set; }

		[JsonPropertyName("profileImage")]
		public string ProfileImage { get; set; }

		[JsonPropertyName("password")]
		public string Password { get; set; }

		[JsonPropertyName("marritalStatus")]
		public string MarritalStatus { get; set; }

		[JsonPropertyName("spouseName")]
		public string SpouseName { get; set; }

		[JsonPropertyName("dateofBirth")]
		public DateTime? DateofBirth { get; set; }
		public string FormattedDateofBirth => DateofBirth?.ToString("dd/MM/yyyy") ?? "N/A";

		[JsonPropertyName("age")]
		public int? Age { get; set; }

		[JsonPropertyName("gender")]
		public string Gender { get; set; }

		[JsonPropertyName("workerQualificationId")]
		public int? WorkerQualificationId { get; set; }

		[JsonPropertyName("qualificationId")]
		public int? QualificationId { get; set; }

		[JsonPropertyName("qualificationName")]
		public string QualificationName { get; set; }

		[JsonPropertyName("dateofJoining")]
		public DateTime? DateofJoining { get; set; }

		public string FormattedDateofJoining => DateofJoining?.ToString("dd/MM/yyyy") ?? "N/A";


		[JsonPropertyName("workerMobileNumberId")]
		public int? WorkerMobileNumberId { get; set; }

		[JsonPropertyName("mobileNumber")]
		public string MobileNumber { get; set; }

		[JsonPropertyName("mobileNumbers")]
		public string MobileNumbers { get; set; }

		[JsonPropertyName("email")]
		public string Email { get; set; }

		[JsonPropertyName("projectId")]
		public int? ProjectId { get; set; }

		[JsonPropertyName("projectName")]
		public string ProjectName { get; set; }

		[JsonPropertyName("siteId")]
		public int? SiteId { get; set; }

		[JsonPropertyName("siteName")]
		public string SiteName { get; set; }

		[JsonPropertyName("siteLocation")]
		public string SiteLocation { get; set; }

		[JsonPropertyName("gpsLocation")]
		public string GpsLocation { get; set; }

		[JsonPropertyName("workerShiftId")]
		public int? WorkerShiftId { get; set; }

		[JsonPropertyName("siteShiftId")]
		public int? SiteShiftId { get; set; }

		[JsonPropertyName("shiftName")]
		public string ShiftName { get; set; }

		[JsonPropertyName("startTime")]
		[JsonConverter(typeof(NullableTimeSpanConverter))]
		public TimeSpan? StartTime { get; set; }

		[JsonPropertyName("endTime")]
		[JsonConverter(typeof(NullableTimeSpanConverter))]
		public TimeSpan? EndTime { get; set; }

		[JsonPropertyName("shiftCreatedAt")]
		public DateTime? ShiftCreatedAt { get; set; }
	}


	public class NullableTimeSpanConverter : JsonConverter<TimeSpan?>
	{
		public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null)
				return null;

			var value = reader.GetString();
			if (string.IsNullOrEmpty(value))
				return null;

			return TimeSpan.TryParse(value, out var timeSpan) ? timeSpan : null;
		}

		public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
		{
			if (!value.HasValue)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.Value.ToString());
		}
	}



	public class Address
	{
		[JsonPropertyName("workerAddressId")]
		public int WorkerAddressId { get; set; }

		[JsonPropertyName("workerId")]
		public int WorkerId { get; set; }

		[JsonPropertyName("addressType")]
		public string AddressType { get; set; }

		[JsonPropertyName("address")]
		public string AddressLine1 { get; set; }

		[JsonPropertyName("stateName")]
		public string State { get; set; }

		[JsonPropertyName("cityName")]
		public string City { get; set; }

		[JsonPropertyName("pincode")]
		public string Pincode { get; set; }

		public string AddressText => AddressLine1 ?? string.Empty;
	}


	public class AddressView
	{
		public string PermanentAddress { get; set; } = "Not Provided";
		public string CurrentAddress { get; set; } = "Not Provided";
	}


	public class Document
	{
		[JsonPropertyName("documentId")]
		public int DocumentId { get; set; }

		[JsonPropertyName("documentName")]
		public string DocumentName { get; set; }

		[JsonPropertyName("documentNumber")]
		public string Number { get; set; }

		[JsonPropertyName("documentImage")]
		public string DocumentUrl { get; set; }

		[JsonPropertyName("isVerified")]
		public bool IsVerified { get; set; }

		[JsonPropertyName("documentRemark")]
		public string Remark { get; set; }

		public string Status => IsVerified ? "Verified" : "Pending";

		public Color StatusColor => IsVerified ? Colors.Green : Colors.Orange;

	}


	public class DocumentViewModel
	{
		public string DocumentName { get; set; }
		public string Status { get; set; }
		public Color StatusColor { get; set; }
		public bool IsVerified { get; set; }

	}

	public class Workers
	{
		public int WorkerId { get; set; }
		public string WorkmanId { get; set; }
		public string Name { get; set; }
		public string? ProfileImage { get; set; }

	}


}
