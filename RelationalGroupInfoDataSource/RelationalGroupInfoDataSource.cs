namespace RelationalGroupInfoDataSource
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadDataSourceUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;

	/// <summary>
	/// The input is a group name. We return a table with the parameter keys, element names and parameter names for the selected group.
	/// </summary>
	public class RelationalGroupInfoDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("GroupName");
		private static readonly GQIIntArgument DataMinerID = new GQIIntArgument("DataMinerID");
		private static Connection connection_;
		private ElementNameCache elementNames_;
		private ParametersCache parameters_;
		private IGQILogger logger_;
		private string groupName_ = string.Empty;
		private int dataMinerID_ = -1;
		private List<ParameterKey> parameterKeys_ = new List<ParameterKey>();

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			InitializeConnection(args.DMS);
			logger_ = args.Logger;
			elementNames_ = new ElementNameCache(connection_, logger_);
			parameters_ = new ParametersCache(connection_, logger_);
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, DataMinerID };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(GroupName, out groupName_))
				logger_.Error("No group name provided");

			if (!args.TryGetArgumentValue(DataMinerID, out dataMinerID_))
				logger_.Error("No DataMiner ID provided");

			return new OnArgumentsProcessedOutputArgs();
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			if (string.IsNullOrEmpty(groupName_))
			{
				logger_.Error("Group name is empty");
				parameterKeys_ = new List<ParameterKey>();
				return default;
			}

			try
			{
				GetMADParameterGroupInfoMessage msg = new GetMADParameterGroupInfoMessage(groupName_)
				{
					DataMinerID = dataMinerID_,
				};
				var groupInfoResponse = connection_.HandleSingleResponseMessage(msg) as GetMADParameterGroupInfoResponseMessage;
				if (groupInfoResponse?.GroupInfo != null)
					parameterKeys_ = groupInfoResponse.GroupInfo.Parameters;
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException($"Failed to fetch group info for group {groupName_} on agent {dataMinerID_}", ex);
			}

			// Fetch and cache elementNames and protocol info
			foreach (ParameterKey paramKey in parameterKeys_)
			{
				elementNames_.TryGet(paramKey.DataMinerID, paramKey.ElementID, out var _);
				parameters_.TryGet(paramKey.DataMinerID, paramKey.ElementID, out var _);
			}

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Parameter Key"),
				new GQIIntColumn("DataMiner ID"),
				new GQIIntColumn("Element ID"),
				new GQIIntColumn("Parameter ID"),
				new GQIStringColumn("Primary Key"),
				new GQIStringColumn("Display Key"),
				new GQIStringColumn("Element Name"),
				new GQIStringColumn("Parameter Name"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			foreach (ParameterKey pKey in parameterKeys_)
			{
				var paramID = new ParamID(pKey.DataMinerID, pKey.ElementID, pKey.ParameterID, pKey.Instance);
				var key = paramID.ToString();
				var cells = new GQICell[]
				{
					new GQICell(){ Value = pKey.ToString() },
					new GQICell(){ Value = pKey.DataMinerID },
					new GQICell(){ Value = pKey.ElementID },
					new GQICell(){ Value = pKey.ParameterID },
					new GQICell(){ Value = pKey.Instance },
					new GQICell(){ Value = pKey.DisplayInstance },
					new GQICell(){ Value = GetElementName(pKey) },
					new GQICell(){ Value = GetParameterName(pKey) },
				};
				var parameterMetaData = new ObjectRefMetadata() { Object = paramID };
				var rowMetaData = new GenIfRowMetadata(new[] { parameterMetaData });
				rows.Add(new GQIRow(key, cells) { Metadata = rowMetaData });
			}

			return new GQIPage(rows.ToArray());
		}

		private static void InitializeConnection(GQIDMS dms)
		{
			if (connection_ == null)
			{
				connection_ = ConnectionHelper.CreateConnection(dms);
			}
		}

		#region Helper Methods

		private string GetElementName(ParameterKey paramKey)
		{
			if (elementNames_.TryGetFromCache(paramKey.DataMinerID, paramKey.ElementID, out string name))
				return name;
			else
				return "Unknown Element";
		}

		private string GetParameterName(ParameterKey paramKey)
		{
			if (!parameters_.TryGetFromCache(paramKey.DataMinerID, paramKey.ElementID, out var parameterInfos))
				return "Unknown Parameter";

			var paramInfo = parameterInfos?.FirstOrDefault(p => p.ID == paramKey.ParameterID);
			return paramInfo?.DisplayName ?? "Unknown Parameter";
		}

		#endregion
	}
}