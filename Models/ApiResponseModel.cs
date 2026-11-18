using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tutorial_demo_2.Models
{

	public class ApiResponseModel<T>
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public ApiDataModel<T> Data { get; set; }

		[JsonPropertyName("errors")]
		public object Errors { get; set; }
	}

	public class ApiDataModel<T>
	{
		[JsonPropertyName("request")]
		public T Request { get; set; }

		[JsonPropertyName("worker")]
		public T Worker { get; set; }

		public T GetData()
		{
			return Request ?? Worker;
		}
	}

	public class ApiDirectResponseModel<T>
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public T Data { get; set; }

		[JsonPropertyName("errors")]
		public object Errors { get; set; }
	}

}
