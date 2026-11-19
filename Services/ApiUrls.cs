using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tutorial_demo_2.Services
{

    public static class ApiUrls
    {
		//public static string ImageUrl => "http://192.168.0.11:85";   //  MVC URL as Image URL
		//public const string Base = "http://192.168.0.11:81";

		// --------------- Prod Server --------------------
		//*
		public static string ImageUrl => "https://omsaiservices.com";   //  MVC URL as Image URL
		//public static string ImageUrl => "https://localhost:7251";   //  MVC URL as Image URL

		public const string Base = "https://apis.omsaiservices.com";
		//public const string Base = "https://localhost:7251";
		//*/

		// --------------- Local Server --------------------
		/*
		public static string ImageUrl => "http://192.168.0.6:85";   //  MVC URL as Image URL
		public const string Base = "http://192.168.0.6:81";
		/ */           

		public static string BaseUrl => $"{Base}/api3/";

		// Auth endpoints
		public static string PostLoginEndpoint => $"{BaseUrl}ApiWorkerAuth/login";


		// #########  Worker endpoints ######### 
		public static string GetWorkerProfileEndpoint => $"{BaseUrl}worker/Profile/";  // Parameter required
		public static string GetWorkerBySiteIdEndpoint => $"{BaseUrl}worker/get-workers-by-site-id/";  // Parameter required
																									   //{{host}}api/worker/get-workers-by-site-id/40

		public static string PostChangeProfileImageEndpoint => $"{BaseUrl}worker/upload-profile-image";  // (POST)
		public static string PostChangePasswordEndpoint => $"{BaseUrl}ApiWorkerAuth/change-password";  // (POST)



		// #########  Attendace endpoints ######### 
		public static string GetAttendanceValidateEndpoint => $"{BaseUrl}attendance/validate-attendance2/";  // Parameter required WorkmanId
		// Mark Attendance
		public static string PostSelfieAttendanceEndpoint => $"{BaseUrl}attendance/worker-selfie-attendance";

		// ######### Attdance API ######### 		

		//Get Report
		public static string PostAttendanceEndpoint()
		{
			return $"{BaseUrl}attendance/worker-attendance-report";
		}



		// #########  Site endpoints ######### 
		public static string GetSitesEndpoint => $"{BaseUrl}site/site-list";  // Parameter not required (GET)


		// #########  Leave endpoints ######### 
		public static string GetLeaveTypesEndpoint => $"{BaseUrl}worker-leave/get-all-leave-types";  // (GET)
		public static string GetWorkerLeavesEndpoint => $"{BaseUrl}worker-leave/get-by-worker-id/";  // Parameter required (GET)
		public static string PostWorkerLeavesEndpoint => $"{BaseUrl}worker-leave/create-leave-request";  // (Post)
		public static string PostWorkerLeavesDeleteEndpoint => $"{BaseUrl}worker-leave/delete-leave-request/";  // (Post) Parameter requred


		public static string GetSiteListEndpoint => $"{BaseUrl}site/site-list";  // (GET)

	}
}
