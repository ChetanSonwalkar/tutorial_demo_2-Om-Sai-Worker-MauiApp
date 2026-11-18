using System.Collections.Generic;

namespace tutorial_demo_2.Services
{
	public class SessionService
	{
		private readonly Dictionary<string, object> _sessionData = new();

		// Store data in session
		public void Set(string key, object value)
		{
			_sessionData[key] = value;
		}

		// Retrieve data from session
		public T Get<T>(string key)
		{
			if (_sessionData.TryGetValue(key, out var value))
			{
				return (T)value;
			}
			return default;
		}

		// Remove data from session
		public void Remove(string key)
		{
			_sessionData.Remove(key);
		}

		// Clear all session data
		public void Clear()
		{
			_sessionData.Clear();
		}
	}
}