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

	public abstract class RadGroupBaseRow
	{
		protected RadGroupBaseRow(string name, int dataMinerID, List<ParameterKey> parameters, bool updateModel, double anomalyThreshold,
			TimeSpan minimumAnomalyDuration)
		{
			Name = name;
			DataMinerID = dataMinerID;
			Parameters = parameters;
			UpdateModel = updateModel;
			AnomalyThreshold = anomalyThreshold;
			MinimumAnomalyDuration = minimumAnomalyDuration;
		}

		public string Name { get; set; }

		public int DataMinerID { get; set; }

		public List<ParameterKey> Parameters { get; set; }

		public bool HasError { get; set; } = false;

		public bool UpdateModel { get; set; }

		public double AnomalyThreshold { get; set; }

		public TimeSpan MinimumAnomalyDuration { get; set; }

		public bool HasActiveAnomaly { get; set; } = false;

		public int AnomaliesInLast30Days { get; set; } = 0;

		public abstract GQICell[] GetGQICells();

		public GQIRow ToGQIRow()
		{
			var row = new GQIRow(GetGQICells());
			var parameters = Parameters?.Select(p => new ObjectRefMetadata() { Object = p?.ToParamID() })
				.WhereNotNull().ToArray();
			if (parameters?.Length > 0)
				row.Metadata = new GenIfRowMetadata(parameters);

			return row;
		}
	}

	public abstract class RelationalAnomalyGroupsBaseDataSource
	{
		protected static readonly AnomaliesCache _anomaliesCache = new AnomaliesCache();
	}

	public abstract class RelationalAnomalyGroupsBaseDataSource<T> : RelationalAnomalyGroupsBaseDataSource, IGQIDataSource, IGQIOnInit, IGQIInputArguments where T : RadGroupBaseRow
	{
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
		private readonly GQIBooleanArgument _onlyWithErrorArg = new GQIBooleanArgument("Show only groups with error")
		{
			IsRequired = false,
			DefaultValue = false,
		};

		private readonly GQIBooleanArgument _onlyWithAnomaliesArg = new GQIBooleanArgument("Show only groups with an active anomaly")
		{
			IsRequired = false,
			DefaultValue = false,
		};

		private RadHelper _radHelper;
		private GQIDMS _dms;
		private IGQILogger _logger;

		private SortingColumn _sortBy;
		private bool _sortDescending;
		private bool _onlyWithError;
		private bool _onlyWithAnomalies;

		protected RadHelper RadHelper => _radHelper;

		protected IGQILogger Logger => _logger;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;
			_radHelper = ConnectionHelper.InitializeRadHelper(_dms, _logger);

			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			var inputArgs = new GQIArgument[] { _sortByArg, _sortDirectionArg, _onlyWithErrorArg, _onlyWithAnomaliesArg };
			var additionalArgs = GetAdditionalInputArguments();

			if (additionalArgs != null)
				return additionalArgs.Concat(inputArgs).ToArray();
			else
				return inputArgs;
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_sortByArg, out string sortByString) || !EnumExtensions.TryParseDescription(sortByString, out _sortBy))
				_sortBy = SortingColumn.Name;

			if (!args.TryGetArgumentValue(_sortDirectionArg, out _sortDescending))
				_sortDescending = false;

			if (!args.TryGetArgumentValue(_onlyWithErrorArg, out _onlyWithError))
				_onlyWithError = false;

			if (!args.TryGetArgumentValue(_onlyWithAnomaliesArg, out _onlyWithAnomalies))
				_onlyWithAnomalies = false;

			OnAdditionalArgumentsProcessed(args);

			return default;
		}

		public abstract GQIColumn[] GetColumns();

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var groupInfos = GetGroupInfos();
			if (groupInfos == null || groupInfos.Count == 0)
				return new GQIPage(Array.Empty<GQIRow>());

			var subgroupsWithActiveAnomaly = GetSubgroupsWithActiveAnomaly();
			var anomaliesPerSubgroup = GetAnomaliesPerSubgroup();

			IEnumerable<T> rows = groupInfos.SelectMany(g => GetRowsForGroup(g, subgroupsWithActiveAnomaly, anomaliesPerSubgroup));

			// Filtering
			if (_onlyWithError)
				rows = rows.Where(r => r.HasError);
			if (_onlyWithAnomalies)
				rows = rows.Where(r => r.HasActiveAnomaly);

			// Sorting
			var sortedRows = Sort(rows, _sortBy);
			if (sortedRows == null)
				sortedRows = rows;

			return new GQIPage(sortedRows.Select(r => r.ToGQIRow()).ToArray());
		}

		protected virtual IEnumerable<GQIArgument> GetAdditionalInputArguments()
		{
			return null;
		}

		protected virtual void OnAdditionalArgumentsProcessed(OnArgumentsProcessedInputArgs args) { }

		protected virtual IEnumerable<T> Sort(IEnumerable<T> rows, SortingColumn sortBy, bool sortDescending)
		{
			switch (sortBy)
			{
				case SortingColumn.Name:
					return sortDescending ? rows.OrderByDescending(r => r.Name, StringComparer.OrdinalIgnoreCase) : rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
				case SortingColumn.AnomaliesInLast30Days:
					return sortDescending ? rows.OrderByDescending(r => r.AnomaliesInLast30Days).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.AnomaliesInLast30Days).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
				case SortingColumn.AnomalyThreshold:
					return sortDescending ? rows.OrderByDescending(r => r.AnomalyThreshold).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.AnomalyThreshold).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
				case SortingColumn.MinimumAnomalyDuration:
					return sortDescending ? rows.OrderByDescending(r => r.MinimumAnomalyDuration.Ticks).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						: rows.OrderBy(r => r.MinimumAnomalyDuration.Ticks).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
				default:
					return null;
			}
		}

		protected abstract List<RadGroupInfo> GetGroupInfos();

		protected abstract IEnumerable<T> GetRowsForGroup(RadGroupInfo groupInfo, HashSet<Guid> subgroupsWithActiveAnomaly, Dictionary<Guid, int> anomaliesPerSubgroup);

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

				if (activeSuggestionsResponse.ActiveAlarms == null)
					return new HashSet<Guid>();

				return activeSuggestionsResponse.ActiveAlarms
					.Select(a => a?.MetaData as MultivariateAnomalyMetaData)
					.Where(m => m != null)
					.Select(m => m.ParameterGroupID)
					.ToHashSet();
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
				var anomalies = _anomaliesCache.GetRelationalAnomalies(_radHelper);

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
