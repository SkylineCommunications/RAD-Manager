namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class AnomaliesData
	{
		public AnomaliesData(string userDomainName, DateTime fetchTime, List<RelationalAnomaly> anomalies)
		{
			UserDomainName = userDomainName;
			FetchTime = fetchTime;
			Anomalies = anomalies ?? new List<RelationalAnomaly>();
		}

		public string UserDomainName { get; set; }

		public DateTime FetchTime { get; set; }

		public List<RelationalAnomaly> Anomalies { get; set; }

		public bool IsSameUser(string userDomainName)
		{
			return string.Equals(UserDomainName, userDomainName, StringComparison.OrdinalIgnoreCase);
		}

		public bool IsExpired(DateTime utcNow)
		{
			return utcNow > FetchTime.AddMinutes(AnomaliesCache.CACHE_TIME_MINUTES);
		}

		public bool IsValidEntry(string userDomainName, DateTime utcNow)
		{
			return IsSameUser(userDomainName) && !IsExpired(utcNow);
		}
	}

	public class AnomaliesCache
	{
		public const int MAX_CACHE_SIZE = 5;
		public const int CACHE_TIME_MINUTES = 5;
		private readonly object _cachedDataLock = new object();
		private readonly List<AnomaliesData> _cachedData = new List<AnomaliesData>();

		public List<RelationalAnomaly> GetRelationalAnomalies(RadHelper radHelper)
		{
			lock (_cachedDataLock)
			{
				var now = DateTime.UtcNow;
				var cachedData = _cachedData.FirstOrDefault(d => d.IsValidEntry(radHelper.Connection.UserDomainName, now));
				if (cachedData != null)
					return cachedData.Anomalies;

				var anomalies = Fetch(radHelper, now);

				_cachedData.RemoveAll(p => p.IsSameUser(radHelper.Connection.UserDomainName) || p.IsExpired(now));
				if (_cachedData.Count >= MAX_CACHE_SIZE)
					_cachedData.RemoveAt(0); // Remove the oldest entry if cache size exceeds limit
				_cachedData.Add(new AnomaliesData(radHelper.Connection.UserDomainName, now, anomalies));
				return anomalies;
			}
		}

		private List<RelationalAnomaly> Fetch(RadHelper radHelper, DateTime utcNow)
		{
			try
			{
				var anomalies = radHelper.FetchRelationalAnomalies(utcNow.AddDays(-30), utcNow);
				if (anomalies == null)
					throw new DataMinerCommunicationException("No response or a response of the wrong type received");

				return anomalies;
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException("Failed to fetch RAD data", ex);
			}
		}
	}
}
