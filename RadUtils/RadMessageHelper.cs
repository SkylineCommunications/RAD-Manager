namespace RadUtils
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Class with helper functions to fetch messages from the DataMiner.
	/// </summary>
	public static class RadMessageHelper
	{
		public static List<string> FetchParameterGroups(Connection connection, int dataMinerID)
		{
			return FetchParameterGroups(connection.HandleSingleResponseMessage, dataMinerID);
		}

		public static List<string> FetchParameterGroups(IEngine engine, int dataMinerID)
		{
			return FetchParameterGroups(engine.SendSLNetSingleResponseMessage, dataMinerID);
		}

		public static MADGroupInfo FetchParameterGroupInfo(Connection connection, int dataMinerID, string groupName)
		{
			return FetchParameterGroupInfo(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static MADGroupInfo FetchParameterGroupInfo(IEngine engine, int dataMinerID, string groupName)
		{
			return FetchParameterGroupInfo(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(Connection connection, int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			return FetchAnomalyScoreData(connection.HandleSingleResponseMessage, dataMinerID, groupName, startTime, endTime);
		}

		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(IEngine engine, int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			return FetchAnomalyScoreData(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, startTime, endTime);
		}

		public static void RemoveParameterGroup(Connection connection, int dataMinerID, string groupName)
		{
			RemoveParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static void RemoveParameterGroup(IEngine engine, int dataMinerID, string groupName)
		{
			RemoveParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static void AddParameterGroup(Connection connection, MADGroupInfo groupInfo)
		{
			AddParameterGroup(connection.HandleSingleResponseMessage, groupInfo);
		}

		public static void AddParameterGroup(IEngine engine, MADGroupInfo groupInfo)
		{
			AddParameterGroup(engine.SendSLNetSingleResponseMessage, groupInfo);
		}

		public static void RetrainParameterGroup(Connection connection, int dataMinerID, string groupName, IEnumerable<TimeRange> timeRanges)
		{
			RetrainParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName, timeRanges);
		}

		public static void RetrainParameterGroup(IEngine engine, int dataMinerID, string groupName, IEnumerable<TimeRange> timeRanges)
		{
			RetrainParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, timeRanges);
		}

#pragma warning disable CS0618 // Type or member is obsolete: messages are obsolete since 10.5.5, but replacements were only added in that version
		private static List<string> FetchParameterGroups(Func<DMSMessage, DMSMessage> sendMessageFunc, int dataMinerID)
		{
			GetMADParameterGroupsMessage request = new GetMADParameterGroupsMessage()
			{
				DataMinerID = dataMinerID,
			};

			var response = sendMessageFunc(request) as GetMADParameterGroupsResponseMessage;
			return response?.GroupNames;
		}

		private static MADGroupInfo FetchParameterGroupInfo(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			GetMADParameterGroupInfoMessage request = new GetMADParameterGroupInfoMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};

			var response = sendMessageFunc(request) as GetMADParameterGroupInfoResponseMessage;
			return response?.GroupInfo;
		}

		private static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName,
			DateTime startTime,
			DateTime endTime)
		{
			GetMADDataMessage request = new GetMADDataMessage(groupName, startTime, endTime)
			{
				DataMinerID = dataMinerID,
			};
			var response = sendMessageFunc(request) as GetMADDataResponseMessage;
			return response?.Data.Select(p => new KeyValuePair<DateTime, double>(p.Timestamp.ToUniversalTime(), p.AnomalyScore)).ToList();
		}

		private static RemoveMADParameterGroupResponseMessage RemoveParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			var request = new RemoveMADParameterGroupMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RemoveMADParameterGroupResponseMessage;
		}

		private static AddMADParameterGroupResponseMessage AddParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			MADGroupInfo groupInfo)
		{
			var request = new AddMADParameterGroupMessage(groupInfo);
			return sendMessageFunc(request) as AddMADParameterGroupResponseMessage;
		}

		private static RetrainMADModelResponseMessage RetrainParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName,
			IEnumerable<TimeRange> timeRanges)
		{
			var request = new RetrainMADModelMessage(groupName, timeRanges.Select(r => new Skyline.DataMiner.Analytics.Mad.TimeRange(r.Start, r.End)).ToList())
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RetrainMADModelResponseMessage;
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}

	public class TimeRange
	{
		public TimeRange(DateTime start, DateTime end)
		{
			Start = start;
			End = end;
		}

		public DateTime Start { get; set; }

		public DateTime End { get; set; }
	}
}
