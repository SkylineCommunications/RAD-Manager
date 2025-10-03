namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Enums;
	using Skyline.DataMiner.Net.Filters;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.MetaData.DataClass;
	using Skyline.DataMiner.Utils.RadToolkit;

	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Groups")]
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit, IGQIOnPrepareFetch
	{
		private const int PAGE_SIZE = 1000;
		private RadHelper _radHelper;
		private ElementNameCache _elementNames;
		private ParametersCache _parameters;
		private GQIDMS _dms;
		private IGQILogger _logger;
		private IEnumerator<RadGroupInfo> _groupInfoEnumerator;
		private HashSet<Guid> _subgroupsWithActiveAnomaly;
		private Dictionary<Guid, int> _anomaliesPerSubgroup;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;
			_radHelper = ConnectionHelper.InitializeRadHelper(_dms, _logger);

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Name"),
				new GQIIntColumn("DataMiner Id"),
				new GQIStringColumn("Parameters"),
				new GQIBooleanColumn("Update Model"),
				new GQIDoubleColumn("Anomaly Threshold"),
				new GQITimeSpanColumn("Minimum Anomaly Duration"),
				new GQIBooleanColumn("Is Monitored"),
				new GQIStringColumn("Parent Group"),
				new GQIStringColumn("Subgroup ID"),
				new GQIBooleanColumn("Is Shared Model Group"),
				new GQIBooleanColumn("Has Active Anomaly"),
				new GQIIntColumn("Anomalies in Last 30 Days"),
			};
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			_elementNames = new ElementNameCache(_logger, _dms);
			_parameters = new ParametersCache(_logger, _dms);

			var groupInfos = _radHelper.FetchParameterGroupInfos();
			_groupInfoEnumerator = groupInfos.GetEnumerator();

			_subgroupsWithActiveAnomaly = GetSubgroupsWithActiveAnomaly();
			_anomaliesPerSubgroup = GetAnomaliesPerSubgroup();

			return default;
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (_groupInfoEnumerator == null)
				return new GQIPage(Array.Empty<GQIRow>());

			List<GQIRow> rows = new List<GQIRow>(PAGE_SIZE);
			bool hasMore = true;
			while (rows.Count < PAGE_SIZE)
			{
				if (!_groupInfoEnumerator.MoveNext())
				{
					hasMore = false;
					break;
				}

				var groupInfo = _groupInfoEnumerator.Current;
				if (groupInfo == null)
					continue;

				rows.AddRange(GetRowsForGroup(groupInfo));
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = hasMore,
			};
		}

		private IEnumerable<GQIRow> GetRowsForGroup(RadGroupInfo groupInfo)
		{
			if (groupInfo == null)
			{
				_logger.Error($"Group info is null");
				yield break;
			}

			if (groupInfo.Subgroups == null || groupInfo.Subgroups.Count == 0)
			{
				_logger.Error($"Group '{groupInfo.GroupName}' has no subgroups defined.");
				yield break;
			}

			bool sharedModelGroup = groupInfo.Subgroups.Count > 1;
			foreach (var subgroupInfo in groupInfo.Subgroups)
			{
				if (subgroupInfo == null)
				{
					_logger.Error($"Subgroup info for group '{groupInfo.GroupName}' is null.");
					continue;
				}

				double anomalyThreshold;
				int minumumAnomalyDuration;
				if (subgroupInfo.Options != null)
				{
					anomalyThreshold = subgroupInfo.Options.GetAnomalyThresholdOrDefault(_radHelper,
						groupInfo.Options?.AnomalyThreshold);
					minumumAnomalyDuration = subgroupInfo.Options.GetMinimalDurationOrDefault(_radHelper,
						groupInfo.Options?.MinimalDuration);
				}
				else
				{
					anomalyThreshold = _radHelper.DefaultAnomalyThreshold;
					minumumAnomalyDuration = _radHelper.DefaultMinimumAnomalyDuration;
				}

				var cells = new GQICell[]
				{
					new GQICell() { Value = subgroupInfo.GetName(groupInfo.GroupName) },
					new GQICell() { Value = groupInfo.DataMinerID },
					new GQICell() { Value = ParameterKeysToString(subgroupInfo.Parameters?.Select(p => p?.Key)) },
					new GQICell() { Value = groupInfo.Options?.UpdateModel ?? false },
					new GQICell() { Value = anomalyThreshold },
					new GQICell() { Value = TimeSpan.FromMinutes(minumumAnomalyDuration) },
					new GQICell() { Value = subgroupInfo.IsMonitored },
					new GQICell() { Value = groupInfo.GroupName }, // Parent group
					new GQICell() { Value = subgroupInfo.ID.ToString() }, // Subgroup ID
					new GQICell() { Value = sharedModelGroup }, // Is Shared Model Group
					new GQICell() { Value = _subgroupsWithActiveAnomaly.Contains(subgroupInfo.ID) }, // Has Active Anomaly
					new GQICell() { Value = _anomaliesPerSubgroup.TryGetValue(subgroupInfo.ID, out int count) ? count : 0 }, // Anomalies in Last 30 Days
				};
				var parameters = subgroupInfo.Parameters?.Where(p => p?.Key != null)
					.Select(p => new ObjectRefMetadata() { Object = p?.Key?.ToParamID() });
				var row = new GQIRow(cells);
				if (parameters != null)
					row.Metadata = new GenIfRowMetadata(parameters.ToArray());

				yield return row;
			}
		}

		private string ParameterKeyToString(ParameterKey pKey)
		{
			if (pKey == null)
				return string.Empty;

			string elementName;
			if (!_elementNames.TryGet(pKey.DataMinerID, pKey.ElementID, out elementName))
				elementName = $"{pKey.DataMinerID}/{pKey.ElementID}";

			_parameters.TryGet(pKey.DataMinerID, pKey.ElementID, out ParameterInfo[] parameters);
			var parameter = parameters?.FirstOrDefault(p => p.ID == pKey.ParameterID);
			var parameterName = parameter?.DisplayName ?? pKey.ParameterID.ToString();

			string instance = string.Empty;
			if (!string.IsNullOrEmpty(pKey.DisplayInstance))
				instance = pKey.DisplayInstance;
			else
				instance = pKey.Instance;

			if (string.IsNullOrEmpty(instance))
				return $"{elementName}/{parameterName}";
			else
				return $"{elementName}/{parameterName}/{instance}";
		}

		private string ParameterKeysToString(IEnumerable<ParameterKey> pKeys)
		{
			if (pKeys == null)
				return string.Empty;

			return $"[{string.Join(", ", pKeys.Select(p => ParameterKeyToString(p)))}]";
		}

		private HashSet<Guid> GetSubgroupsWithActiveAnomaly()
		{
			try
			{
				var activeSuggestionsRequest = new GetActiveAlarmsMessage()
				{
					Filter = new AlarmFilter(new AlarmFilterItemInt(AlarmFilterField.SourceID, new int[] { (int)SLAlarmSource.SuggestionEngine })),
				};
				var activeSuggestionsResponse = _dms.SendMessage(activeSuggestionsRequest) as ActiveAlarmsResponseMessage;
				if (activeSuggestionsResponse == null)
				{
					_logger.Error("Failed to fetch active anomalies: Received no response or response of the wrong type");
					return new HashSet<Guid>();
				}

				return activeSuggestionsResponse.ActiveAlarms?
					.Where(a => a != null)
					.Select(a => a.MetaData as MultivariateAnomalyMetaData)
					.Where(m => m != null)
					.Select(m => m.ParameterGroupID)
					.ToHashSet() ?? new HashSet<Guid>();
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to fetch active anomalies: " + ex.Message);
				return new HashSet<Guid>();
			}
		}

		private Dictionary<Guid, int> GetAnomaliesPerSubgroup()
		{
			if (!_radHelper.HistoricalAnomaliesAvailable)
				return new Dictionary<Guid, int>();

			try
			{
				var now = DateTime.Now;
				var anomalies = _radHelper.FetchRelationalAnomalies(now.AddDays(-30), now);
				if (anomalies == null)
				{
					_logger.Error("Failed to fetch historical anomalies.");
					return new Dictionary<Guid, int>();
				}

				return anomalies.Where(a => a != null)
					.DistinctBy(a => a.AnomalyID)
					.GroupBy(a => a.SubgroupID)
					.ToDictionary(g => g.Key, g => g.Count());
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to fetch anomaly counts: " + ex.Message);
				return new Dictionary<Guid, int>();
			}
		}//TODO: make filtering options work as well
		//TODO: best cache historical anomalies, I guess
	}
}