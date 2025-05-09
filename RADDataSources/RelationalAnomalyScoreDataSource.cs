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
		private List<KeyValuePair<DateTime, double>> _anomalyScores = new List<KeyValuePair<DateTime, double>>();
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

			try
			{
				_anomalyScores = _anomalyScoreCache.GetAnomalyScores(_dataMinerID, _groupName, _startTime.Value, _endTime.Value);
			}
			catch (Exception e)
			{
				_logger.Error($"Failed to fetch anomaly scores: {e}");
				throw;
			}

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIDateTimeColumn("Time"), new GQIDoubleColumn("AnomalyScore") };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (_anomalyScores.Count == 0)
				return new GQIPage(new GQIRow[0]);

			List<GQIRow> rows = new List<GQIRow>(_anomalyScores.Count);
			if (_startTime.Value.AddMinutes(5) < _anomalyScores.First().Key)
				rows.Add(GetGQIRow(_startTime.Value, null));
			rows.AddRange(_anomalyScores.Select(s => GetGQIRow(s.Key, s.Value)));
			if (_anomalyScores.Last().Key.AddMinutes(5) < _endTime)
				rows.Add(GetGQIRow(_endTime.Value, null));

			return new GQIPage(rows.ToArray());
		}

		private GQIRow GetGQIRow(DateTime time, double? value)
		{
			GQICell[] cells = new GQICell[]
			{
				new GQICell { Value = time },
				new GQICell { Value = value },
			};
			return new GQIRow(cells);
		}
	}
}