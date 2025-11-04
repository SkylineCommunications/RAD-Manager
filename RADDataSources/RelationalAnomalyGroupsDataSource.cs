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

    public class RadGroupRow
    {
        public RadGroupRow(RadHelper radHelper, RadGroupInfo groupInfo, bool hasError, bool hasActiveAnomaly, int anomaliesInLast30Days)
        {
            Name = groupInfo.GroupName;
            DataMinerID = groupInfo.DataMinerID;
            Parameters = groupInfo.Subgroups.SelectMany(sg => sg.Parameters?.Select(p => p?.Key)).Where(p => p != null).ToList();
            UpdateModel = groupInfo.Options?.UpdateModel ?? false;
            HasError = hasError;
            IsSharedModelGroup = groupInfo.Subgroups.Count > 1;
            HasActiveAnomaly = hasActiveAnomaly;
            AnomaliesInLast30Days = anomaliesInLast30Days;
            AnomalyThreshold = groupInfo.Options?.GetAnomalyThresholdOrDefault(radHelper) ?? radHelper.DefaultAnomalyThreshold;

            int minimumAnomalyDurationMinutes = groupInfo.Options?.GetMinimalDurationOrDefault(radHelper) ?? radHelper.DefaultMinimumAnomalyDuration;
            MinimumAnomalyDuration = TimeSpan.FromMinutes(minimumAnomalyDurationMinutes);
        }

        public string Name { get; set; }

        public int DataMinerID { get; set; }

        public List<ParameterKey> Parameters { get; set; }

        public bool UpdateModel { get; set; }

        public double AnomalyThreshold { get; set; }

        public TimeSpan MinimumAnomalyDuration { get; set; }

        public bool HasError { get; set; }

        public bool IsSharedModelGroup { get; set; }

        public bool HasActiveAnomaly { get; set; }

        public int AnomaliesInLast30Days { get; set; }

        public GQIRow ToGQIRow()
        {
            var cells = new GQICell[]
            {
                new GQICell { Value = Name },                                    // Name
                new GQICell { Value = DataMinerID },                             // DataMiner Id
                new GQICell { Value = ParameterKeysToString(Parameters) },  //TODO: do I need this     // Parameters
                new GQICell { Value = UpdateModel },                             // Update Model
                new GQICell { Value = AnomalyThreshold },                        // Anomaly Threshold
                new GQICell { Value = MinimumAnomalyDuration },                  // Minimum Anomaly Duration
                new GQICell { Value = HasError },                                // Has Error
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
    /// Returns a row for configured relational anomaly group.
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
        private readonly GQIBooleanArgument _onlyWithErrorArg = new GQIBooleanArgument("Show only group with error")
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

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _dms = args.DMS;
            _logger = args.Logger;
            _radHelper = ConnectionHelper.InitializeRadHelper(_dms, _logger);

            return default;
        }

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[] { _sortByArg, _sortDirectionArg, _onlyWithErrorArg, _onlyWithAnomaliesArg };
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
                new GQIBooleanColumn("Has Error"),
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

            var subgroupsWithActiveAnomaly = GetSubgroupsWithActiveAnomaly();
            var anomaliesPerSubgroup = GetAnomaliesPerSubgroup();

            var rows = groupInfos.Select(g => GetRowForGroup(g, subgroupsWithActiveAnomaly, anomaliesPerSubgroup)).Where(r => r != null);

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

        private RadGroupRow GetRowForGroup(RadGroupInfo groupInfo, HashSet<Guid> subgroupsWithActiveAnomaly, Dictionary<Guid, int> anomaliesPerSubgroup)
        {
            if (groupInfo == null)
            {
                _logger.Error("Group info is null");
                return null;
            }

            if (groupInfo.Subgroups == null || groupInfo.Subgroups.Count == 0)
            {
                _logger.Error($"Group '{groupInfo.GroupName}' has no subgroups defined.");
                return null;
            }

            bool hasError = groupInfo.Subgroups.Any(sg => !sg.IsMonitored);
            if (_onlyWithError && !hasError)
                return null;

            bool hasActiveAnomaly = groupInfo.Subgroups.Any(sg => subgroupsWithActiveAnomaly.Contains(sg.ID));
            if (_onlyWithAnomalies && !hasActiveAnomaly)
                return null;

            int anomaliesInLast30Days = groupInfo.Subgroups.Sum(sg => anomaliesPerSubgroup.TryGetValue(sg.ID, out int count) ? count : 0);
            //TODO: average this per subgroup?

            return new RadGroupRow(_radHelper, groupInfo, hasError, hasActiveAnomaly, anomaliesInLast30Days);
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