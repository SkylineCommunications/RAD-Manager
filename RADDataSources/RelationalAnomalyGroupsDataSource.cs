namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadGroupRow : RadGroupBaseRow
	{
		public RadGroupRow(RadHelper radHelper, RadGroupInfo groupInfo)
			: base(name: groupInfo.GroupName,
				  dataMinerID: groupInfo.DataMinerID,
				  parameters: new List<ParameterKey>(),
				  updateModel: groupInfo.Options?.UpdateModel ?? false,
				  anomalyThreshold: groupInfo.Options?.GetAnomalyThresholdOrDefault(radHelper) ?? radHelper.DefaultAnomalyThreshold,
				  minimumAnomalyDuration: TimeSpan.FromMinutes(groupInfo.Options?.GetMinimalDurationOrDefault(radHelper) ?? radHelper.DefaultMinimumAnomalyDuration))
		{
			HasError = groupInfo.Subgroups.Any(sg => !sg.IsMonitored);
			IsSharedModelGroup = groupInfo.Subgroups?.Count > 1;
			if (!IsSharedModelGroup)
			{
				var subgroup = groupInfo.Subgroups.FirstOrDefault();
				if (subgroup != null)
				{
					Parameters = subgroup.Parameters?.Select(p => p?.Key).Where(p => p != null).ToList() ?? new List<ParameterKey>();
					if (subgroup.Options != null)
					{
						AnomalyThreshold = subgroup.Options.GetAnomalyThresholdOrDefault(radHelper, groupInfo.Options?.AnomalyThreshold);
						MinimumAnomalyDuration = TimeSpan.FromMinutes(subgroup.Options.GetMinimalDurationOrDefault(radHelper, groupInfo.Options?.MinimalDuration));
					}
				}
			}
		}

		public bool IsSharedModelGroup { get; set; }

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
				new GQICell { Value = IsSharedModelGroup },                      // Is Shared Model Group
			};
		}
	}

	/// <summary>
	/// Returns a row for each configured (parent) relational anomaly group.
	/// Supports optional sorting.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Groups")]
	public class RelationalAnomalyGroupsDataSource : RelationalAnomalyGroupsBaseDataSource<RadGroupRow>
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
				new GQIBooleanColumn("Is Shared Model Group"),
			};
		}

		protected override IEnumerable<RadGroupRow> Sort(IEnumerable<RadGroupRow> rows, SortingColumn sortBy, bool sortDescending)
		{
			var sorted = base.Sort(rows, sortBy, sortDescending);
			if (sorted != null)
				return sorted;

			if (sortBy == SortingColumn.UpdateModel)
			{
				// Order by and order by descending are inverted since 'adaptive' (i.e. true) should come before 'static' (i.e. false)
				return sortDescending ? rows.OrderBy(r => r.UpdateModel).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
					: rows.OrderByDescending(r => r.UpdateModel).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
			}

			return null;
		}

		protected override List<RadGroupInfo> GetGroupInfos()
		{
			return RadHelper.FetchParameterGroupInfos();
		}


		protected override IEnumerable<RadGroupRow> GetRowsForGroup(RadGroupInfo groupInfo, HashSet<Guid> subgroupsWithActiveAnomaly,
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

			yield return new RadGroupRow(RadHelper, groupInfo)
			{
				HasActiveAnomaly = groupInfo.Subgroups.Any(sg => subgroupsWithActiveAnomaly.Contains(sg.ID)),
				AnomaliesInLast30Days = groupInfo.Subgroups.Sum(sg => anomaliesPerSubgroup.TryGetValue(sg.ID, out int count) ? count : 0),
			};
		}
	}
}