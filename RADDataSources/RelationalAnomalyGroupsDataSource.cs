namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Enums;
	using Skyline.DataMiner.Net.Filters;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.MetaData.DataClass;
	using Skyline.DataMiner.Utils.RadToolkit;

	public enum SortingColumn
	{
		[Description("Name")]
		Name,
		[Description("Anomalies in Last 30 Days")]
		AnomaliesInLast30Days,
		[Description("Update Model")]
		UpdateModel,
		[Description("Anomaly Threshold")]
		AnomalyThreshold,
		[Description("Minimum Anomaly Duration")]
		MinimumAnomalyDuration,
	}

	public class RadGroupRow
	{
		public RadGroupRow(string name, int dataMinerID, List<ParameterKey> parameters, bool updateModel, double anomalyThreshold, TimeSpan minimumAnomalyDuration, bool isMonitored, string parentGroup,
			Guid subgroupID, bool isSharedModelGroup, bool hasActiveAnomaly, int anomaliesInLast30Days)
		{
			Name = name;
			DataMinerID = dataMinerID;
			Parameters = parameters;
			UpdateModel = updateModel;
			AnomalyThreshold = anomalyThreshold;
			MinimumAnomalyDuration = minimumAnomalyDuration;
			IsMonitored = isMonitored;
			ParentGroup = parentGroup;
			SubgroupID = subgroupID;
			IsSharedModelGroup = isSharedModelGroup;
			HasActiveAnomaly = hasActiveAnomaly;
			AnomaliesInLast30Days = anomaliesInLast30Days;
		}

		public string Name { get; set; }

		public int DataMinerID { get; set; }

		public List<ParameterKey> Parameters { get; set; }

		public bool UpdateModel { get; set; }

		public double AnomalyThreshold { get; set; }

		public TimeSpan MinimumAnomalyDuration { get; set; }

		public bool IsMonitored { get; set; }

		public string ParentGroup { get; set; }

		public Guid SubgroupID { get; set; }

		public bool IsSharedModelGroup { get; set; }

		public bool HasActiveAnomaly { get; set; }

		public int AnomaliesInLast30Days { get; set; }

		public GQIRow ToGQIRow()
		{
			var cells = new GQICell[]
			{
				new GQICell { Value = Name },                                    // Name
				new GQICell { Value = DataMinerID },                             // DataMiner Id
				new GQICell { Value = ParameterKeysToString(Parameters) },       // Parameters
				new GQICell { Value = UpdateModel },                             // Update Model
				new GQICell { Value = AnomalyThreshold },                        // Anomaly Threshold
				new GQICell { Value = MinimumAnomalyDuration },                  // Minimum Anomaly Duration
				new GQICell { Value = IsMonitored },                             // Is Monitored
				new GQICell { Value = ParentGroup },                             // Parent Group
				new GQICell { Value = SubgroupID.ToString() },                   // Subgroup ID
				new GQICell { Value = IsSharedModelGroup },                      // Is Shared Model Group
				new GQICell { Value = HasActiveAnomaly },                        // Has Active Anomaly
				new GQICell { Value = AnomaliesInLast30Days },                   // Anomalies in Last 30 Days
			};
			var row = new GQIRow(cells);
			var parameters = Parameters.Select(p => new ObjectRefMetadata() { Object = p?.ToParamID() })
				.WhereNotNull();
			if (parameters != null)
				row.Metadata = new GenIfRowMetadata(parameters.ToArray());

			return row;
		}

		private static string ParameterKeyToString(ParameterKey pKey)
		{
			if (pKey == null)
				return string.Empty;

			string result = $"{pKey.DataMinerID}/{pKey.ElementID}/{pKey.ParameterID}";
			string instance = !string.IsNullOrEmpty(pKey.DisplayInstance) ? pKey.DisplayInstance : pKey.Instance;
			return string.IsNullOrEmpty(instance) ? result : $"{result}/{instance}";
		}

		private static string ParameterKeysToString(IEnumerable<ParameterKey> pKeys)
		{
			if (pKeys == null)
				return string.Empty;

			return $"[{string.Join(", ", pKeys.Select(p => ParameterKeyToString(p)))}]";
		}
	}

	/// <summary>
	/// Returns a row per subgroup of every configured relational anomaly group.
	/// Supports optional sorting.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Groups")]
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments
	{
		private static readonly AnomaliesCache _anomaliesCache = new AnomaliesCache();

		private readonly GQIStringDropdownArgument _sortByArg = new GQIStringDropdownArgument("Sort by", EnumExtensions.GetDescriptions<SortingColumn>())
		{
			IsRequired = false,
		};

		private readonly GQIBooleanArgument _sortDirectionArg = new GQIBooleanArgument("Sort descending")
		{
			IsRequired = false,
			DefaultValue = false,
		};

		// New filter arguments
		private readonly GQIBooleanArgument _onlyUnmonitoredArg = new GQIBooleanArgument("Show only unmonitored groups")
		{
			IsRequired = false,
			DefaultValue = false,
		};

		private readonly GQIBooleanArgument _onlyWithAnomaliesArg = new GQIBooleanArgument("Show only groups with anoamlies")
		{
			IsRequired = false,
			DefaultValue = false,
		};

		private RadHelper _radHelper;
		private GQIDMS _dms;
		private IGQILogger _logger;

		private HashSet<Guid> _subgroupsWithActiveAnomaly;
		private Dictionary<Guid, int> _anomaliesPerSubgroup;
		private SortingColumn _sortBy;
		private bool _sortDescending;
		private bool _onlyUnmonitored;
		private bool _onlyWithAnomalies;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;
			_radHelper = ConnectionHelper.InitializeRadHelper(_dms, _logger);

			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _sortByArg, _sortDirectionArg, _onlyUnmonitoredArg, _onlyWithAnomaliesArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_sortByArg, out string sortByString) || !EnumExtensions.TryParseDescription(sortByString, out _sortBy))
			{
				_sortBy = SortingColumn.Name;
				_sortDescending = false;
			}

			if (!args.TryGetArgumentValue(_sortDirectionArg, out _sortDescending))
				_sortDescending = false;

			if (!args.TryGetArgumentValue(_onlyUnmonitoredArg, out _onlyUnmonitored))
				_onlyUnmonitored = false;

			if (!args.TryGetArgumentValue(_onlyWithAnomaliesArg, out _onlyWithAnomalies))
				_onlyWithAnomalies = false;

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

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var groupInfos = _radHelper.FetchParameterGroupInfos();
			if (groupInfos == null || groupInfos.Count == 0)
				return new GQIPage(Array.Empty<GQIRow>());

			_subgroupsWithActiveAnomaly = GetSubgroupsWithActiveAnomaly();
			_anomaliesPerSubgroup = GetAnomaliesPerSubgroup();

			IEnumerable<RadGroupRow> rows = groupInfos.Where(g => g != null).SelectMany(g => GetRowsForGroup(g));

			// Sorting
			switch (_sortBy)
			{
				case SortingColumn.Name:
					rows = _sortDescending ? rows.OrderByDescending(r => r.Name, StringComparer.OrdinalIgnoreCase) : rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
					break;
				case SortingColumn.AnomaliesInLast30Days:
					rows = _sortDescending ? rows.OrderByDescending(r => r.AnomaliesInLast30Days).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.AnomaliesInLast30Days).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
					break;
				case SortingColumn.UpdateModel:
					rows = _sortDescending ? rows.OrderByDescending(r => r.UpdateModel).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.UpdateModel).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
					break;
				case SortingColumn.AnomalyThreshold:
					rows = _sortDescending ? rows.OrderByDescending(r => r.AnomalyThreshold).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.AnomalyThreshold).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
					break;
				case SortingColumn.MinimumAnomalyDuration:
					rows = _sortDescending ? rows.OrderByDescending(r => r.MinimumAnomalyDuration.Ticks).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.MinimumAnomalyDuration.Ticks).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
					break;
				default:
					break;
			}

			return new GQIPage(rows.Select(r => r.ToGQIRow()).ToArray());
		}

		private IEnumerable<RadGroupRow> GetRowsForGroup(RadGroupInfo groupInfo)
		{
			if (groupInfo == null)
			{
				_logger.Error("Group info is null");
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

				if (_onlyUnmonitored && subgroupInfo.IsMonitored)
					continue;

				bool hasActiveAnomaly = _subgroupsWithActiveAnomaly.Contains(subgroupInfo.ID);
				if (_onlyWithAnomalies && !hasActiveAnomaly)
					continue;

				double anomalyThreshold;
				int minumumAnomalyDuration;
				if (subgroupInfo.Options != null)
				{
					anomalyThreshold = subgroupInfo.Options.GetAnomalyThresholdOrDefault(_radHelper, groupInfo.Options?.AnomalyThreshold);
					minumumAnomalyDuration = subgroupInfo.Options.GetMinimalDurationOrDefault(_radHelper, groupInfo.Options?.MinimalDuration);
				}
				else
				{
					anomalyThreshold = _radHelper.DefaultAnomalyThreshold;
					minumumAnomalyDuration = _radHelper.DefaultMinimumAnomalyDuration;
				}

				yield return new RadGroupRow(
					name: subgroupInfo.GetName(groupInfo.GroupName),
					dataMinerID: groupInfo.DataMinerID,
					parameters: subgroupInfo.Parameters?.Select(p => p?.Key).ToList(),
					updateModel: groupInfo.Options?.UpdateModel ?? false,
					anomalyThreshold: anomalyThreshold,
					minimumAnomalyDuration: TimeSpan.FromMinutes(minumumAnomalyDuration),
					isMonitored: subgroupInfo.IsMonitored,
					parentGroup: groupInfo.GroupName,
					subgroupID: subgroupInfo.ID,
					isSharedModelGroup: sharedModelGroup,
					hasActiveAnomaly: hasActiveAnomaly,
					anomaliesInLast30Days: _anomaliesPerSubgroup.TryGetValue(subgroupInfo.ID, out int count) ? count : 0);
			}
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
				var anomalies = _anomaliesCache.GetRelationalAnomalies(_radHelper, _logger);

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
		}
	}
}