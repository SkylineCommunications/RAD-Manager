namespace RelationalAnomalyGroupsDataSource
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using RadDataSourceUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit, IGQIOnPrepareFetch
	{
		private static Connection connection_;
		private ElementNameCache elementNames_;
		private ParametersCache parameters_;
		private GQIDMS dms_;
		private IGQILogger logger_;
		private IEnumerator<int> dmaIDEnumerator_;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			dms_ = args.DMS;
			logger_ = args.Logger;
			InitializeConnection(dms_);
			elementNames_ = new ElementNameCache(connection_, logger_);
			parameters_ = new ParametersCache(connection_, logger_);
			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Name"),
				new GQIIntColumn("DataMiner Id"),
				new GQIStringColumn("Parameters"),
				new GQIStringColumn("Update Model"),
				new GQIStringColumn("Anomaly Threshold"),
				new GQIStringColumn("Minimum Anomaly Duration"),
			};
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			var infoMessages = dms_.SendMessages(new GetInfoMessage(InfoType.DataMinerInfo));
			dmaIDEnumerator_ = infoMessages.Select(m => m as GetDataMinerInfoResponseMessage).Where(m => m != null).Select(m => m.ID).ToList().GetEnumerator();

			return default;
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (!dmaIDEnumerator_.MoveNext())
				return new GQIPage(new GQIRow[0]);

			int dataMinerID = dmaIDEnumerator_.Current;
			GetMADParameterGroupsMessage request = new GetMADParameterGroupsMessage()
			{
				DataMinerID = dataMinerID,
			};
			var response = connection_.HandleSingleResponseMessage(request) as GetMADParameterGroupsResponseMessage;
			if (response == null)
			{
				logger_.Error($"Could not fetch RAD group names from agent {dataMinerID}: no response or response of the wrong type received");
				return new GQIPage(new GQIRow[0]) { HasNextPage = true };
			}

			var rows = new List<GQIRow>(response.GroupNames.Count);
			foreach (var groupName in response.GroupNames)
			{
				GetMADParameterGroupInfoMessage msg = new GetMADParameterGroupInfoMessage(groupName)
				{
					DataMinerID = dataMinerID,
				};
				var groupInfoResponse = connection_.HandleSingleResponseMessage(msg) as GetMADParameterGroupInfoResponseMessage;
				if (groupInfoResponse?.GroupInfo == null)
				{
					logger_.Error($"Could not fetch RAD group info for group {groupName}: no response or response of the wrong type received");
					continue;
				}

				MADGroupInfo groupInfo = groupInfoResponse.GroupInfo;
				var parameterStr = groupInfo.Parameters.Select(p => ParameterKeyToString(p));
				rows.Add(new GQIRow(
					new GQICell[]
					{
						new GQICell() { Value = groupName },
						new GQICell() { Value = dataMinerID },
						new GQICell() { Value = $"[{string.Join(", ", parameterStr)}]" },
						new GQICell() { Value = groupInfo.UpdateModel.ToString() },
						new GQICell() { Value = groupInfo.AnomalyThreshold?.ToString(CultureInfo.InvariantCulture) ?? "3" },
						new GQICell() { Value = (groupInfo.MinimumAnomalyDuration?.ToString(CultureInfo.InvariantCulture) ?? "5") + " min" },
					}));
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = true,
			};
		}

		private static void InitializeConnection(GQIDMS dms)
		{
			if (connection_ == null)
			{
				connection_ = ConnectionHelper.CreateConnection(dms);
			}
		}

		private string ParameterKeyToString(ParameterKey pKey)
		{
			string elementName;
			if (!elementNames_.TryGet(pKey.DataMinerID, pKey.ElementID, out elementName))
				elementName = $"{pKey.DataMinerID}/{pKey.ElementID}";

			parameters_.TryGet(pKey.DataMinerID, pKey.ElementID, out ParameterInfo[] parameters);
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
	}
}