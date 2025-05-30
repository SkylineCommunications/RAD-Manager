namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Groups")]
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit, IGQIOnPrepareFetch
	{
		private ElementNameCache _elementNames;
		private ParametersCache _parameters;
		private GQIDMS _dms;
		private IGQILogger _logger;
		private IEnumerator<int> _dmaIDEnumerator;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;
			ConnectionHelper.InitializeConnection(_dms);
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
			};
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			var infoMessages = _dms.SendMessages(new GetInfoMessage(InfoType.DataMinerInfo));
			_dmaIDEnumerator = infoMessages
				.OfType<GetDataMinerInfoResponseMessage>()
				.Select(m => m.ID)
				.Distinct()
				.GetEnumerator();

			_elementNames = new ElementNameCache(_logger);
			_parameters = new ParametersCache(_logger);

			return default;
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (!_dmaIDEnumerator.MoveNext())
				return new GQIPage(Array.Empty<GQIRow>());

			int dataMinerID = _dmaIDEnumerator.Current;
			var groupNames = RadMessageHelper.FetchParameterGroups(ConnectionHelper.Connection, dataMinerID);
			if (groupNames == null)
			{
				_logger.Error($"Could not fetch RAD group names from agent {dataMinerID}: no response or response of the wrong type received");
				return new GQIPage(new GQIRow[0]) { HasNextPage = true };
			}

			var rows = new List<GQIRow>(groupNames.Count);
			foreach (var groupName in groupNames)
			{
				var groupInfo = RadMessageHelper.FetchParameterGroupInfo(ConnectionHelper.Connection, dataMinerID, groupName);
				if (groupInfo is RadGroupInfo parameterGroupInfo)
				{
					rows.Add(new GQIRow(
						new GQICell[]
						{
							new GQICell() { Value = groupName },
							new GQICell() { Value = dataMinerID },
							new GQICell() { Value = ParameterKeysToString(parameterGroupInfo.Parameters) },
							new GQICell() { Value = parameterGroupInfo.Options.UpdateModel },
							new GQICell() { Value = parameterGroupInfo.Options.GetAnomalyThresholdOrDefault() },
							new GQICell() { Value = TimeSpan.FromMinutes(parameterGroupInfo.Options.GetMinimalDurationOrDefault()) },
							new GQICell() { Value = parameterGroupInfo.IsMonitored },
							new GQICell() { Value = groupName }, // Parent group
							new GQICell() { Value = string.Empty }, // Subgroup ID
						}));
				}
				else if (groupInfo is RadSharedModelGroupInfo sharedModelGroupInfo)
				{
					foreach (var subgroupInfo in sharedModelGroupInfo.Subgroups)
					{
						rows.Add(new GQIRow(
							new GQICell[]
							{
								new GQICell() { Value = subgroupInfo.GetName(sharedModelGroupInfo.GroupName) },
								new GQICell() { Value = dataMinerID },
								new GQICell() { Value = ParameterKeysToString(subgroupInfo.Parameters.Select(p => p.Key)) },
								new GQICell() { Value = sharedModelGroupInfo.Options.UpdateModel },
								new GQICell() { Value = subgroupInfo.Options.GetAnomalyThresholdOrDefault(sharedModelGroupInfo.Options.AnomalyThreshold) },
								new GQICell() { Value = TimeSpan.FromMinutes(subgroupInfo.Options.GetMinimalDurationOrDefault(sharedModelGroupInfo.Options.MinimalDuration)) },
								new GQICell() { Value = subgroupInfo.IsMonitored },
								new GQICell() { Value = sharedModelGroupInfo.GroupName }, // Parent group
								new GQICell() { Value = subgroupInfo.ID.ToString() }, // Subgroup ID
							}));
					}
				}
				else
				{
					_logger.Error($"Could not fetch RAD group info for group {groupName}: no response or response of the wrong type received");
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = true,
			};
		}

		private string ParameterKeyToString(ParameterKey pKey)
		{
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
			return $"[{string.Join(", ", pKeys.Select(p => ParameterKeyToString(p)))}]";
		}
	}
}