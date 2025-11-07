namespace RadDataSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Analytics.DataTypes;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Utils.RadToolkit;

    public class RadSubgroupRow : RadGroupBaseRow
    {
        public RadSubgroupRow(RadHelper radHelper, RadGroupInfo groupInfo, RadSubgroupInfo subgroupInfo)
            : base(name: subgroupInfo.GetName(groupInfo.GroupName),
                 dataMinerID: groupInfo.DataMinerID,
                 parameters: subgroupInfo.Parameters?.Select(p => p?.Key).WhereNotNull().ToList() ?? new List<ParameterKey>(),
                 updateModel: groupInfo.Options?.UpdateModel ?? false,
                 anomalyThreshold: subgroupInfo.Options?.GetAnomalyThresholdOrDefault(radHelper, groupInfo.Options?.AnomalyThreshold) ?? radHelper.DefaultAnomalyThreshold,
                 minimumAnomalyDuration: TimeSpan.FromMinutes(subgroupInfo.Options?.GetMinimalDurationOrDefault(radHelper, groupInfo.Options?.MinimalDuration) ?? radHelper.DefaultMinimumAnomalyDuration))
        {
            ParentGroup = groupInfo.GroupName;
            SubgroupID = subgroupInfo.ID;
            HasError = !subgroupInfo.IsMonitored;
        }

        public string ParentGroup { get; set; }

        public Guid SubgroupID { get; set; }

        public override GQICell[] GetGQICells()
        {
            return new GQICell[]
            {
                new GQICell { Value = Name },                                    // Name
                new GQICell { Value = DataMinerID },                             // DataMiner Id
                new GQICell { Value = Utils.ParameterKeysToString(Parameters) }, // Parameters
                new GQICell { Value = UpdateModel },                             // Update Model
                new GQICell { Value = AnomalyThreshold },                        // Anomaly Threshold
                new GQICell { Value = MinimumAnomalyDuration },                  // Minimum Anomaly Duration
                new GQICell { Value = HasError },                                // Has Error
                new GQICell { Value = HasActiveAnomaly },                        // Has Active Anomaly
                new GQICell { Value = AnomaliesInLast30Days },                   // Anomalies in Last 30 Days
                new GQICell { Value = ParentGroup },                             // Parent Group
                new GQICell { Value = SubgroupID.ToString() },                   // Subgroup ID
            };
        }
    }

    /// <summary>
    /// Returns a row per subgroup of every configured relational anomaly group.
    /// Supports optional sorting.
    /// </summary>
    [GQIMetaData(Name = "Get Relational Anomaly Subgroups")]
    public class RelationalAnomalySubgroupsDataSource : RelationalAnomalyGroupsBaseDataSource<RadSubgroupRow>
    {
        public override GQIColumn[] GetColumns()
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
                new GQIBooleanColumn("Has Active Anomaly"),
                new GQIIntColumn("Anomalies in Last 30 Days"),
                new GQIStringColumn("Parent Group"),
                new GQIStringColumn("Subgroup ID"),
            };
        }

        protected override IEnumerable<RadSubgroupRow> GetRowsForGroup(RadGroupInfo groupInfo, HashSet<Guid> subgroupsWithActiveAnomaly,
            Dictionary<Guid, int> anomaliesPerSubgroup)
        {
            if (groupInfo == null)
            {
                Logger.Error("Group info is null");
                yield break;
            }

            if (groupInfo.Subgroups == null || groupInfo.Subgroups.Count == 0)
            {
                Logger.Error($"Group '{groupInfo.GroupName}' has no subgroups defined.");
                yield break;
            }

            foreach (var subgroupInfo in groupInfo.Subgroups)
            {
                if (subgroupInfo == null)
                {
                    Logger.Error($"Subgroup info for group '{groupInfo.GroupName}' is null.");
                    continue;
                }

                yield return new RadSubgroupRow(RadHelper, groupInfo, subgroupInfo)
                {
                    HasActiveAnomaly = subgroupsWithActiveAnomaly.Contains(subgroupInfo.ID),
                    AnomaliesInLast30Days = anomaliesPerSubgroup.TryGetValue(subgroupInfo.ID, out int count) ? count : 0,
                };
            }
        }
    }
}