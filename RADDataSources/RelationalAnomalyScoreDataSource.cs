namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Utils.RadToolkit;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Score")]
	public class RelationalAnomalyScoreDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIIntArgument DataMinerID = new GQIIntArgument("DataMiner ID");
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("Group Name");
		private static readonly GQIStringArgument _subGroupNameArgument = new GQIStringArgument("Subgroup Name");
		private static readonly GQIStringArgument _subGroupIDArgument = new GQIStringArgument("Subgroup ID");
		private static readonly GQIDateTimeArgument StartTime = new GQIDateTimeArgument("Start Time");
		private static readonly GQIDateTimeArgument EndTime = new GQIDateTimeArgument("End Time");
		private static readonly GQIBooleanArgument SkipCache = new GQIBooleanArgument("Skip Cache");
#pragma warning disable CS0618 // Type or member is obsolete
		private static readonly AnomalyScoreCache _anomalyScoreCache = new AnomalyScoreCache();
#pragma warning restore CS0618 // Type or member is obsolete
		private RadHelper _radHelper;
		private List<KeyValuePair<DateTime, double>> _anomalyScores = new List<KeyValuePair<DateTime, double>>();
		private int _dataMinerID = -1;
		private string _groupName = string.Empty;
		private string _subGroupName = string.Empty;
		private Guid _subGroupID = Guid.Empty;
		private bool _skipCache = false;
		private DateTime? _startTime = null;
		private DateTime? _endTime = null;
		private IGQILogger _logger;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_logger = args.Logger;
			_radHelper = ConnectionHelper.InitializeRadHelper(args.DMS, _logger);

			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { DataMinerID, GroupName, _subGroupNameArgument, _subGroupIDArgument, StartTime, EndTime, SkipCache };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(DataMinerID, out _dataMinerID))
				_dataMinerID = -1;

			if (!args.TryGetArgumentValue(GroupName, out _groupName))
				_logger.Error("No group name provided");

			if (!args.TryGetArgumentValue(_subGroupNameArgument, out _subGroupName))
				_subGroupName = string.Empty;

			if (!args.TryGetArgumentValue(_subGroupIDArgument, out string s) || !Guid.TryParse(s, out _subGroupID))
				_subGroupID = Guid.Empty;

			if (args.TryGetArgumentValue(StartTime, out DateTime startTimeValue) && args.TryGetArgumentValue(EndTime, out DateTime endTimeValue))
			{
				_startTime = startTimeValue;
				_endTime = endTimeValue;
			}
			else
			{
				_logger.Error("Unable to parse input times");
			}

			if (!args.TryGetArgumentValue(SkipCache, out _skipCache))
				_skipCache = false;

			return default;
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			if (string.IsNullOrEmpty(_groupName) || _startTime == null || _endTime == null)
			{
				_anomalyScores = new List<KeyValuePair<DateTime, double>>();
				return default;
			}

			RadGroupInfo groupInfo;
			try
			{
				groupInfo = _radHelper.FetchParameterGroupInfo(_dataMinerID, _groupName);
				if (groupInfo?.Subgroups == null)
				{
					_logger.Error($"Group '{_groupName}' not found on DataMiner {_dataMinerID} or no subgroups found in the group");
					_anomalyScores = new List<KeyValuePair<DateTime, double>>();
					return default;
				}
			}
			catch (Exception e)
			{
				_logger.Error($"Failed to fetch group info for group '{_groupName}' on DataMiner {_dataMinerID}: {e}");
				throw new DataMinerCommunicationException("Failed to fetch group info", e);
			}

			if (groupInfo.Subgroups.Count == 1)
				_anomalyScores = FetchAnomalyScoresForParentGroup(groupInfo);
			else if (_subGroupID != Guid.Empty)
				_anomalyScores = FetchAnomalyScoresBySubgroupID(groupInfo, _subGroupID);
			else if (!string.IsNullOrEmpty(_subGroupName))
				_anomalyScores = FetchAnomalyScoresBySubgroupName(groupInfo, _subGroupName);
			else
				_anomalyScores = new List<KeyValuePair<DateTime, double>>();

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

		private List<KeyValuePair<DateTime, double>> FetchAnomalyScoresBySubgroupID(RadGroupInfo groupInfo, Guid subgroupID)
		{
			if (!groupInfo.Subgroups.Any(sg => sg.ID == subgroupID))
				return new List<KeyValuePair<DateTime, double>>();

			try
			{
				if (_radHelper.AnomalyScoreCacheAvailable)
				{
					return _radHelper.FetchAnomalyScoreData(groupInfo.DataMinerID, groupInfo.GroupName, subgroupID, _startTime.Value, _endTime.Value);
				}
				else
				{
					var id = new RadSubgroupID(groupInfo.DataMinerID, groupInfo.GroupName, subgroupID);
					return _anomalyScoreCache.GetAnomalyScores(_radHelper, id, _startTime.Value, _endTime.Value, _skipCache);
				}
			}
			catch (Exception e)
			{
				_logger.Error($"Failed to fetch anomaly scores for subgroup with id {subgroupID} in group '{groupInfo.GroupName}' on agent {groupInfo.DataMinerID}: {e}");
				throw new DataMinerCommunicationException("Failed to fetch anomaly scores", e);
			}
		}

		private List<KeyValuePair<DateTime, double>> FetchAnomalyScoresBySubgroupName(RadGroupInfo groupInfo, string subgroupName)
		{
			if (!groupInfo.Subgroups.Any(sg => string.Equals(sg.Name, subgroupName, StringComparison.OrdinalIgnoreCase)))
				return new List<KeyValuePair<DateTime, double>>();

			try
			{
				if (_radHelper.AnomalyScoreCacheAvailable)
				{
					return _radHelper.FetchAnomalyScoreData(groupInfo.DataMinerID, groupInfo.GroupName, subgroupName, _startTime.Value, _endTime.Value);
				}
				else
				{
					var id = new RadSubgroupID(groupInfo.DataMinerID, groupInfo.GroupName, subgroupName);
					return _anomalyScoreCache.GetAnomalyScores(_radHelper, id, _startTime.Value, _endTime.Value, _skipCache);
				}
			}
			catch (Exception e)
			{
				_logger.Error($"Failed to fetch anomaly scores for subgroup '{subgroupName}' in group '{groupInfo.GroupName}' on agent {groupInfo.DataMinerID}: {e}");
				throw new DataMinerCommunicationException("Failed to fetch anomaly scores", e);
			}
		}

		private List<KeyValuePair<DateTime, double>> FetchAnomalyScoresForParentGroup(RadGroupInfo groupInfo)
		{
			try
			{
				if (_radHelper.AnomalyScoreCacheAvailable)
				{
					return _radHelper.FetchAnomalyScoreData(groupInfo.DataMinerID, groupInfo.GroupName, _startTime.Value, _endTime.Value);
				}
				else
				{
					var id = new RadGroupID(groupInfo.DataMinerID, groupInfo.GroupName);
					return _anomalyScoreCache.GetAnomalyScores(_radHelper, id, _startTime.Value, _endTime.Value, _skipCache);
				}
			}
			catch (Exception e)
			{
				_logger.Error($"Failed to fetch anomaly scores for group '{groupInfo.GroupName}' on agent {groupInfo.DataMinerID}: {e}");
				throw new DataMinerCommunicationException("Failed to fetch anomaly scores", e);
			}
		}
	}
}