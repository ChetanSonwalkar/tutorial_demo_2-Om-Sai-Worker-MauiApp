using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{

	public class LeaveTypeModel
	{
		[JsonPropertyName("leaveTypeId")]
		public int LeaveTypeId { get; set; }

		[JsonPropertyName("leaveTypeName")]
		public string LeaveTypeName { get; set; }

		[JsonPropertyName("status")]
		public bool Status { get; set; }

	}

	public class LeaveRequestModel
	{
		[JsonPropertyName("leaveRequestId")]
		public int LeaveRequestId { get; set; }

		[JsonPropertyName("workerId")]
		public int WorkerId { get; set; }

		[JsonPropertyName("workerName")]
		public string WorkerName { get; set; }

		[JsonPropertyName("leaveTypeId")]
		public int LeaveTypeId { get; set; }

		[JsonPropertyName("leaveTypeName")]
		public string LeaveTypeName { get; set; }

		[JsonPropertyName("startDate")]
		public DateTime StartDate { get; set; }

		[JsonPropertyName("endDate")]
		public DateTime EndDate { get; set; }

		[JsonPropertyName("reason")]
		public string Reason { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; }

		[JsonPropertyName("remark")]
		public string Remark { get; set; }

		[JsonPropertyName("approverId")]
		public int? ApproverId { get; set; }

		[JsonPropertyName("userName")]
		public string UserName { get; set; }

		[JsonPropertyName("createdAt")]
		public DateTime CreatedAt { get; set; }

		[JsonPropertyName("updatedAt")]
		public DateTime? UpdatedAt { get; set; }
	}

	public class LeaveRequestCreateModel
	{
		[JsonPropertyName("workerId")]
		public int WorkerId { get; set; }

		private DateTime _startDate;
		private DateTime _endDate;

		[JsonPropertyName("startDate")]
		public string StartDate
		{
			get => _startDate.ToString("yyyy-MM-dd");
			set => _startDate = DateTime.Parse(value);
		}

		[JsonPropertyName("endDate")]
		public string EndDate
		{
			get => _endDate.ToString("yyyy-MM-dd");
			set => _endDate = DateTime.Parse(value);
		}

		[JsonPropertyName("reason")]
		public string Reason { get; set; }

		[JsonPropertyName("leaveTypeId")]
		public int LeaveTypeId { get; set; }

		public void SetDates(DateTime startDate, DateTime endDate)
		{
			_startDate = startDate;
			_endDate = endDate;
		}
	}


}
