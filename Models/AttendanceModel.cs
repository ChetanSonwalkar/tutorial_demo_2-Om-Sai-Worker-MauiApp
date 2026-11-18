using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{

	public class ApiResponseListData<T>
	{
		public bool Success { get; set; }
		public List<T> Data { get; set; }
		public Dictionary<string, List<string>> Errors { get; set; }
	}



	public class ApiResponse<T>
	{
		public bool Success { get; set; }
		public T Data { get; set; }	
		public Dictionary<string, List<string>> Errors { get; set; }
	}

    public class ValidateAttendanceResponse
    {
        [JsonPropertyName("inOutType")]
        public string? InOutType { get; set; }

        [JsonPropertyName("oldStatus")]
        public string? OldStatus { get; set; }

        [JsonPropertyName("remark")]
        public string? Remark { get; set; }

        [JsonPropertyName("isAttendanceAllowed")]
        public bool IsAttendanceAllowed { get; set; }

        [JsonPropertyName("newStatus")]
        public string? NewStatus { get; set; }

        [JsonPropertyName("statusCode")]
        public string? StatusCode { get; set; }

        [JsonPropertyName("timeDiffInMinutes")]
        public int? TimeDiffInMinutes { get; set; }

        [JsonPropertyName("siteId")]
        public int? SiteId { get; set; }

        [JsonPropertyName("siteShiftId")]
        public int? SiteShiftId { get; set; }

        [JsonPropertyName("gpsLocation")]
        public string? GpsLocation { get; set; }

        [JsonPropertyName("attendanceRecords")]
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraData { get; set; }

        public string? GetInOutType()
        {
            if (!string.IsNullOrWhiteSpace(InOutType))
            {
                string value = InOutType.Trim().ToUpperInvariant();

                if (value == "IN" || value == "OUT")
                    return value;
            }

            if (ExtraData == null)
                return null;

            string[] names =
            {
                "attendanceType",
                "attendance_type",
                "in_out_type",
                "type"
            };

            foreach (var item in ExtraData)
            {
                if (names.Any(x => string.Equals(
                    x,
                    item.Key,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    if (item.Value.ValueKind == JsonValueKind.String)
                    {
                        string? value = item.Value.GetString()?.Trim().ToUpperInvariant();

                        if (value == "IN" || value == "OUT")
                            return value;
                    }
                }
            }

            return null;
        }

        public string? GetNextAttendanceType()
        {
            var todayRecord = AttendanceRecords?
                .Where(x => x.CreatedAt.Date == DateTime.Today)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (todayRecord == null)
            {
                return AttendanceRecords == null ||
                       AttendanceRecords.Count == 0
                    ? "IN"
                    : null;
            }

            if (todayRecord.InTime.HasValue &&
                !todayRecord.OutTime.HasValue)
            {
                return "OUT";
            }

            if (todayRecord.InTime.HasValue &&
                todayRecord.OutTime.HasValue)
            {
                return "IN";
            }

            string? lastType = todayRecord.InOutType?
                .Trim()
                .ToUpperInvariant();

            if (lastType == "IN")
                return "OUT";

            if (lastType == "OUT")
                return "IN";

            return null;
        }
    }


    public class AttendanceRecordList
	{
		//public string CreatedAt { get; set; } // Format: 01 Mar 25
		//public string? InTime { get; set; } // Format: 02:30 PM
		//public string? OutTime { get; set; } // Format: 06:30 PM or N/A
		//public string? WorkingHrs { get; set; } // Format: 4:30

		public string CreatedAt { get; set; }
		public string? InTime { get; set; }
		public string? OutTime { get; set; }
		public string WorkingHrs { get; set; }

	}


	public class AttendanceRecord
	{
		public int Id { get; set; }
		public int WorkerId { get; set; }
		public string WorkerName { get; set; }
		public int SiteId { get; set; }
		public string SiteName { get; set; }
		public int ShiftId { get; set; }

		[JsonPropertyName("inTime")]
		public DateTime? InTime { get; set; }

		[JsonPropertyName("outTime")]
		public DateTime? OutTime { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; }

		[JsonPropertyName("inOutType")]
		public string InOutType { get; set; }

		public bool LateIn { get; set; }
		public bool EarlyOut { get; set; }

		public string InSelfiPath { get; set; }
		public string OutSelfiPath { get; set; }
		public string InGeoLocation { get; set; }
		public string OutGeoLocation { get; set; }
		public int? WorkingHrs { get; set; }

		[JsonPropertyName("createdAt")]
		public DateTime CreatedAt { get; set; }



		public static class ShiftColorMapper
		{
			private static readonly Dictionary<int, string> _shiftColors = new Dictionary<int, string>();
			private static readonly string[] _colorPalette = new[]
			{
			"#F5F5F5",  // Light Gray
            "#E8F5E9",  // Light Green
            "#E3F2FD",  // Light Blue
            "#FFF3E0"   // Light Orange
        };

			public static void Reset()
			{
				_shiftColors.Clear();
			}

			public static string GetColorForShift(int shiftId)
			{
				if (!_shiftColors.ContainsKey(shiftId))
				{
					// Assign next color from palette
					int colorIndex = _shiftColors.Count % _colorPalette.Length;
					_shiftColors[shiftId] = _colorPalette[colorIndex];
				}
				return _shiftColors[shiftId];
			}
		}

		public string StatusColor
		{
			get
			{
				return Status?.ToLower() switch
				{
					"present" => "#4CAF50",  // Green
					"absent" => "#F44336",   // Red
					"leave" => "#FF9800",    // Orange
					"holiday" => "#2196F3",  // Blue
					_ => "#9E9E9E"          // Gray
				};
			}
		}

		public string BackgroundColor => ShiftColorMapper.GetColorForShift(ShiftId);
	}



}
