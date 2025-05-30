namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Net.Exceptions;

	public class AnomalyScoreData
	{
		public int DataMinerID { get; set; }

		public string GroupName { get; set; }

		public string SubGroupName { get; set; }

		public Guid SubGroupID { get; set; }

		public List<KeyValuePair<DateTime, double>> AnomalyScores { get; set; }

		public DateTime CacheTime { get; set; }

		public DateTime RequestStartTime { get; set; }

		public DateTime RequestEndTime { get; set; }
	}

	public class AnomalyScoreCache
	{
		private readonly object _anomalyScoreDataLock = new object();
		private AnomalyScoreData _anomalyScoreData = null;

		public List<KeyValuePair<DateTime, double>> GetAnomalyScores(int dataMinerID, string groupName, string subGroupName, Guid subGroupID,
			DateTime startTime, DateTime endTime, bool skipCache)
		{
			lock (_anomalyScoreDataLock)
			{
				if (_anomalyScoreData == null ||
					dataMinerID != _anomalyScoreData.DataMinerID ||
					groupName != _anomalyScoreData.GroupName ||
					subGroupName != _anomalyScoreData.SubGroupName ||
					subGroupID != _anomalyScoreData.SubGroupID ||
					DateTime.UtcNow > _anomalyScoreData.CacheTime.AddMinutes(5) ||
					startTime < _anomalyScoreData.RequestStartTime.AddMinutes(-5) ||
					endTime > _anomalyScoreData.RequestEndTime.AddMinutes(5) ||
					skipCache)
				{
					UpdateAnomalyScoreData(dataMinerID, groupName, subGroupName, subGroupID, startTime, endTime);
				}

				return _anomalyScoreData.AnomalyScores
					.Where(p => p.Key >= startTime && p.Key <= endTime)
					.ToList();
			}
		}

		private void UpdateAnomalyScoreData(int dataMinerID, string groupName, string subGroupName, Guid subGroupID, DateTime startTime, DateTime endTime)
		{
			DateTime now = DateTime.UtcNow;

			try
			{
				var requestStartTime = Min(now.AddDays(-7), startTime);
				var requestEndTime = Max(now, endTime);
				List<KeyValuePair<DateTime, double>> anomalyScores = null;

				anomalyScores = FetchAnomalyScore(dataMinerID, groupName, subGroupName, subGroupID, requestStartTime, requestEndTime);
				if (anomalyScores == null)
					throw new DataMinerCommunicationException("No response or a response of the wrong type received");

				_anomalyScoreData = new AnomalyScoreData()
				{
					DataMinerID = dataMinerID,
					GroupName = groupName,
					SubGroupName = subGroupName,
					SubGroupID = subGroupID,
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

		private List<KeyValuePair<DateTime, double>> FetchAnomalyScore(int dataMinerID, string groupName, string subGroupName,
			Guid subGroupID, DateTime startTime, DateTime endTime)
		{
			try
			{
				if (subGroupID != Guid.Empty && subGroupID != null)
					return RadMessageHelper.FetchAnomalyScoreData(ConnectionHelper.Connection, dataMinerID, groupName, subGroupID, startTime, endTime);
				if (!string.IsNullOrEmpty(subGroupName))
					return RadMessageHelper.FetchAnomalyScoreData(ConnectionHelper.Connection, dataMinerID, groupName, subGroupName, startTime, endTime);
			}
			catch (TypeLoadException)
			{
			}
			catch (MissingMethodException)
			{
			}

			return RadMessageHelper.FetchAnomalyScoreData(ConnectionHelper.Connection, dataMinerID, groupName, startTime, endTime);
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
