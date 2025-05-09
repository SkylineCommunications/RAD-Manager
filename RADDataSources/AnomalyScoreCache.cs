namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Net.Exceptions;

	public class AnomalyScoreData
	{
		public string GroupName { get; set; }

		public int DataMinerID { get; set; }

		public List<KeyValuePair<DateTime, double>> AnomalyScores { get; set; }

		public DateTime CacheTime { get; set; }

		public DateTime RequestStartTime { get; set; }

		public DateTime RequestEndTime { get; set; }
	}

	public class AnomalyScoreCache
	{
		private readonly object _anomalyScoreDataLock = new object();
		private AnomalyScoreData _anomalyScoreData = null;

		public List<KeyValuePair<DateTime, double>> GetAnomalyScores(int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			lock (_anomalyScoreDataLock)
			{
				if (_anomalyScoreData == null ||
					groupName != _anomalyScoreData.GroupName ||
					DateTime.UtcNow > _anomalyScoreData.CacheTime.AddMinutes(5) ||
					startTime < _anomalyScoreData.RequestStartTime.AddMinutes(-5) ||
					endTime > _anomalyScoreData.RequestEndTime.AddMinutes(5))
				{
					UpdateAnomalyScoreData(dataMinerID, groupName, startTime, endTime);
				}

				return _anomalyScoreData.AnomalyScores
					.Where(p => p.Key >= startTime && p.Key <= endTime)
					.ToList();
			}
		}

		private void UpdateAnomalyScoreData(int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			DateTime now = DateTime.UtcNow;

			try
			{
				var requestStartTime = Min(now.AddMonths(-1), startTime);
				var requestEndTime = Max(now, endTime);
				var anomalyScores = RadMessageHelper.FetchAnomalyScoreData(ConnectionHelper.Connection, dataMinerID, groupName, requestStartTime, requestEndTime);
				if (anomalyScores == null)
					throw new DataMinerCommunicationException("No response or a response of the wrong type received");

				_anomalyScoreData = new AnomalyScoreData()
				{
					GroupName = groupName,
					DataMinerID = dataMinerID,
					CacheTime = now,
					RequestStartTime = requestStartTime,
					RequestEndTime = requestEndTime,
					AnomalyScores = anomalyScores,
				};
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException("Failed to fetch RAD data", ex);
			}
		}

		private DateTime Min(DateTime time1, DateTime time2)
		{
			return time1 < time2 ? time1 : time2;
		}

		private DateTime Max(DateTime time1, DateTime time2)
		{
			return time1 > time2 ? time1 : time2;
		}
	}
}
