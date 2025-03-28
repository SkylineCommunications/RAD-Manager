namespace RelationalAnomalyScoreDataSource
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadDataSourceUtils;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class RelationalAnomalyScoreDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("groupName");
		private static readonly GQIIntArgument DataMinerID = new GQIIntArgument("dataMinerID");
		private static readonly GQIDateTimeArgument StartTime = new GQIDateTimeArgument("startTime");
		private static readonly GQIDateTimeArgument EndTime = new GQIDateTimeArgument("endTime");
		private static AnomalyScoreData anomalyScoreData_ = new AnomalyScoreData();
		private static Connection connection_;
		private string groupName_ = string.Empty;
		private int dataMinerID_ = -1;
		private DateTime? startTime_ = null;
		private DateTime? endTime_ = null;
		private IGQILogger logger_;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			InitializeConnection(args.DMS);
			logger_ = args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, DataMinerID, StartTime, EndTime };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(GroupName, out groupName_))
				logger_.Error("No group name provided");

			if (!args.TryGetArgumentValue(DataMinerID, out dataMinerID_))
				logger_.Error("No DataMiner ID provided");

			if (args.TryGetArgumentValue(StartTime, out DateTime startTimeValue) && args.TryGetArgumentValue(EndTime, out DateTime endTimeValue))
			{
				startTime_ = startTimeValue.ToUniversalTime();
				endTime_ = endTimeValue.ToUniversalTime();
			}
			else
			{
				logger_.Error("Unable to parse input times");
			}

			return new OnArgumentsProcessedOutputArgs();
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			if (string.IsNullOrEmpty(groupName_) || startTime_ == null || endTime_ == null)
			{
				logger_.Error("Group name or time range is empty");
				anomalyScoreData_.AnomalyScores.Clear();
				return default;
			}

			if (groupName_ == anomalyScoreData_.GroupName && anomalyScoreData_.AnomalyScores.IsNotNullOrEmpty())
			{
				// ie we've already fetched the data for this group previously
				var lastTimestamp = anomalyScoreData_.AnomalyScores.Last().Key;
				if ((DateTime.UtcNow - lastTimestamp).TotalMinutes <= 5)
					return default;
			}

			try
			{
				anomalyScoreData_.AnomalyScores.Clear();
				GetMADDataMessage msg = new GetMADDataMessage(groupName_, DateTime.Now.AddMonths(-1), DateTime.Now)
				{
					DataMinerID = dataMinerID_,
				};
				var madDataReponse = connection_.HandleSingleResponseMessage(msg) as GetMADDataResponseMessage;
				logger_.Information($"Number of points: {madDataReponse?.Data.Count}");
				if (madDataReponse != null)
				{
					foreach (MADDataPoint point in madDataReponse.Data)
					{
						anomalyScoreData_.AnomalyScores.Add(new KeyValuePair<DateTime, double>(point.Timestamp, point.AnomalyScore));
					}
				}

				anomalyScoreData_.GroupName = groupName_;
				anomalyScoreData_.DataMinerID = dataMinerID_;

				return default;
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException("Failed to fetch MAD data", ex);
			}
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIDateTimeColumn("Time"), new GQIDoubleColumn("AnomalyScore") };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			foreach (var entry in anomalyScoreData_.AnomalyScores)
			{
				if (entry.Key.ToUniversalTime() < startTime_)
				{
					continue;
				}
				else if (entry.Key.ToUniversalTime() > endTime_)
				{
					break;
				}

				GQICell[] cells = new GQICell[2];
				cells[0] = new GQICell { Value = entry.Key.ToUniversalTime() };
				cells[1] = new GQICell { Value = entry.Value };
				rows.Add(new GQIRow(cells));
			}

			return new GQIPage(rows.ToArray());
		}

		private static void InitializeConnection(GQIDMS dms)
		{
			if (connection_ == null)
			{
				connection_ = ConnectionHelper.CreateConnection(dms);
			}
		}
	}

	public class AnomalyScoreData
	{
		public AnomalyScoreData()
		{
			AnomalyScores = new List<KeyValuePair<DateTime, double>>();
		}

		public string GroupName { get; set; }

		public int DataMinerID { get; set; }

		public List<KeyValuePair<DateTime, double>> AnomalyScores { get; set; }
	}
}