namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Score")]
	public class RelationalAnomalyScoreDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("groupName");
		private static readonly GQIIntArgument DataMinerID = new GQIIntArgument("dataMinerID");
		private static readonly GQIDateTimeArgument StartTime = new GQIDateTimeArgument("startTime");
		private static readonly GQIDateTimeArgument EndTime = new GQIDateTimeArgument("endTime");
		private static readonly AnomalyScoreCache _anomalyScoreCache = new AnomalyScoreCache();
		private IEnumerable<KeyValuePair<DateTime, double>> _anomalyScores = new List<KeyValuePair<DateTime, double>>();
		private string _groupName = string.Empty;
		private int _dataMinerID = -1;
		private DateTime? _startTime = null;
		private DateTime? _endTime = null;
		private IGQILogger _logger;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			ConnectionHelper.InitializeConnection(args.DMS);
			_logger = args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, DataMinerID, StartTime, EndTime };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(GroupName, out _groupName))
				_logger.Error("No group name provided");

			if (!args.TryGetArgumentValue(DataMinerID, out _dataMinerID))
				_logger.Error("No DataMiner ID provided");

			if (args.TryGetArgumentValue(StartTime, out DateTime startTimeValue) && args.TryGetArgumentValue(EndTime, out DateTime endTimeValue))
			{
				_startTime = startTimeValue;
				_endTime = endTimeValue;
			}
			else
			{
				_logger.Error("Unable to parse input times");
			}

			return default;
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			if (string.IsNullOrEmpty(_groupName) || _startTime == null || _endTime == null)
			{
				_logger.Error("Group name or time range is empty");
				_anomalyScores = new List<KeyValuePair<DateTime, double>>();
				return default;
			}

			_anomalyScores = _anomalyScoreCache.GetAnomalyScores(_dataMinerID, _groupName, _startTime.Value, _endTime.Value);
			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIDateTimeColumn("Time"), new GQIDoubleColumn("AnomalyScore") };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = _anomalyScores.Select(s =>
			{
				GQICell[] cells = new GQICell[]
				{
					new GQICell { Value = s.Key },
					new GQICell { Value = s.Value },
				};
				return new GQIRow(cells);
			}).ToArray();

			return new GQIPage(rows.ToArray());
		}
	}
}