namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupRow : RadGroupBaseRow
	{
		public RadSubgroupRow(RadHelper radHelper, RadGroupInfo groupInfo, RadSubgroupInfo subgroupInfo, string subgroupName, FitScore fitScore)
			: base(name: subgroupName,
				 dataMinerID: groupInfo.DataMinerID,
				 parameters: subgroupInfo.Parameters?.Select(p => p?.Key).WhereNotNull().ToList() ?? new List<ParameterKey>(),
				 updateModel: groupInfo.Options?.UpdateModel ?? false,
				 anomalyThreshold: subgroupInfo.Options?.GetAnomalyThresholdOrDefault(radHelper, groupInfo.Options?.AnomalyThreshold) ?? radHelper.DefaultAnomalyThreshold,
				 minimumAnomalyDuration: TimeSpan.FromMinutes(subgroupInfo.Options?.GetMinimalDurationOrDefault(radHelper, groupInfo.Options?.MinimalDuration) ?? radHelper.DefaultMinimumAnomalyDuration))
		{
			ParentGroup = groupInfo.GroupName;
			SubgroupID = subgroupInfo.ID;
			HasError = !subgroupInfo.IsMonitored;
			if (fitScore != null)
			{
				ModelFitScore = fitScore.ModelFit;
				IsOutlier = fitScore.IsOutlier;
			}
			else
			{
				ModelFitScore = double.NaN;
				IsOutlier = false;
			}
		}

		public string ParentGroup { get; set; }

		public Guid SubgroupID { get; set; }

		public double ModelFitScore { get; set; }

		public bool IsOutlier { get; set; }

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
				new GQICell { Value = ModelFitScore },                           // Model Fit Score
				new GQICell { Value = IsOutlier },                               // Is Outlier Group
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
		private static readonly GQIIntArgument _dataMinerIDArg = new GQIIntArgument("DataMiner ID")
		{
			IsRequired = false,
			DefaultValue = -1,
		};

		private static readonly GQIStringArgument _groupNameArg = new GQIStringArgument("Group Name")
		{
			IsRequired = true,
		};

		private int _dataMinerID;
		private string _groupName = null;

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
				new GQIDoubleColumn("Model Fit Score"),
				new GQIBooleanColumn("Is Outlier Group"),
			};
		}

		public override GQIArgument[] GetInputArguments()
		{
			var extraArgs = new GQIArgument[] { _dataMinerIDArg, _groupNameArg };
			return extraArgs.Concat(base.GetInputArguments()).ToArray();
		}

		public override OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_dataMinerIDArg, out _dataMinerID))
				_dataMinerID = -1;

			if (!args.TryGetArgumentValue(_groupNameArg, out _groupName))
				throw new ArgumentException("No group name provided");

			return base.OnArgumentsProcessed(args);
		}

		protected override bool Sort(IEnumerable<RadSubgroupRow> rows, SortingColumn sortBy, bool sortDescending, out IEnumerable<RadSubgroupRow> sortedRows)
		{
			bool sorted = base.Sort(rows, sortBy, sortDescending, out sortedRows);
			if (sorted)
				return true;

			if (sortBy == SortingColumn.IsOutlierGroup)
			{
				sortedRows = sortDescending ? rows.OrderByDescending(r => r.ModelFitScore).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
					: rows.OrderBy(r => r.ModelFitScore).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
				return true;
			}

			return false;
		}

		protected override List<RadGroupInfo> GetGroupInfos()
		{
			if (string.IsNullOrEmpty(_groupName))
				return new List<RadGroupInfo>();

			var groupInfo = RadHelper.FetchParameterGroupInfo(_dataMinerID, _groupName);
			if (groupInfo == null)
			{
				string text = $"Could not fetch info for group with name {_groupName} (DataMiner ID={_dataMinerID})";
				Logger.Error(text);
				throw new DataMinerCommunicationException(text);
			}

			return new List<RadGroupInfo> { groupInfo };
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

			var fitScores = RadHelper.FitScoreAvailable ? RadHelper.FetchFitScores(groupInfo.DataMinerID, groupInfo.GroupName) : null;

			int unnamedSubgroupCount = 0;
			foreach (var subgroupInfo in groupInfo.Subgroups)
			{
				if (subgroupInfo == null)
				{
					Logger.Error($"Subgroup info for group '{groupInfo.GroupName}' is null.");
					continue;
				}

				string subgroupDisplayName = string.IsNullOrEmpty(subgroupInfo.Name) ? RadUtils.Utils.GetSubgroupPlaceHolderName(++unnamedSubgroupCount) : subgroupInfo.Name;
				FitScore fitScore;
				if (fitScores == null || !fitScores.TryGetValue(subgroupInfo.ID, out fitScore))
					fitScore = null;

				yield return new RadSubgroupRow(RadHelper, groupInfo, subgroupInfo, subgroupDisplayName, fitScore)
				{
					HasActiveAnomaly = subgroupsWithActiveAnomaly.Contains(subgroupInfo.ID),
					AnomaliesInLast30Days = anomaliesPerSubgroup.TryGetValue(subgroupInfo.ID, out int count) ? count : 0,
				};
			}
		}
	}
}