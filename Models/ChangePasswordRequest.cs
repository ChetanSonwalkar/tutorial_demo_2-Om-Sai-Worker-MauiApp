using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{
	public class ChangePasswordRequest
	{
		[JsonPropertyName("workerId")]
		public int WorkerId { get; set; }

		[JsonPropertyName("oldPassword")]
		public string OldPassword { get; set; }

		[JsonPropertyName("newPassword")]
		public string NewPassword { get; set; }

		[JsonPropertyName("confirmPassword")]
		public string ConfirmPassword { get; set; }
	}

	public class ChangePasswordResponse
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public bool? Data { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		[JsonPropertyName("errors")]
		public Dictionary<string, string[]> Errors { get; set; }
	}

}
