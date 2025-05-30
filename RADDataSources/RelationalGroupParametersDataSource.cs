namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;

	/// <summary>
	/// The input is a group name. We return a table with the parameter keys, element names and parameter names for the selected group.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Group Parameters")]
	public class RelationalGroupParametersDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIIntArgument _dataMinerIDArgument = new GQIIntArgument("DataMiner ID");
		private static readonly GQIStringArgument _groupNameArgument = new GQIStringArgument("Group Name");
		private static readonly GQIStringArgument _subGroupNameArgument = new GQIStringArgument("Subgroup Name");
		private static readonly GQIStringArgument _subGroupIDArgument = new GQIStringArgument("Subgroup ID");
		private ElementNameCache _elementNamesCache;
		private ParametersCache _parametersCache;
		private IGQILogger _logger;
		private int _dataMinerID = -1;
		private string _groupName = string.Empty;
		private string _subGroupName = string.Empty;
		private Guid _subGroupID = Guid.Empty;
		private List<RadParameter> _parameters = new List<RadParameter>();

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			ConnectionHelper.InitializeConnection(args.DMS);
			_logger = args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _dataMinerIDArgument, _groupNameArgument, _subGroupNameArgument, _subGroupIDArgument };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_dataMinerIDArgument, out _dataMinerID))
				_logger.Error("No DataMiner ID provided");

			if (!args.TryGetArgumentValue(_groupNameArgument, out _groupName))
				_logger.Error("No group name provided");

			if (!args.TryGetArgumentValue(_subGroupNameArgument, out _subGroupName))
				_subGroupName = string.Empty;

			if (!args.TryGetArgumentValue(_subGroupIDArgument, out string s) || !Guid.TryParse(s, out _subGroupID))
				_subGroupID = Guid.Empty;

			return default;
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			_elementNamesCache = new ElementNameCache(_logger);
			_parametersCache = new ParametersCache(_logger);

			if (string.IsNullOrEmpty(_groupName))
			{
				_logger.Error("Group name is empty");
				_parameters = new List<RadParameter>();
				return default;
			}

			try
			{
				var groupInfo = RadMessageHelper.FetchParameterGroupInfo(ConnectionHelper.Connection, _dataMinerID, _groupName);
				if (groupInfo is RadGroupInfo parameterGroupInfo)
				{
					_parameters = parameterGroupInfo.Parameters.Select(p => new RadParameter() { Key = p }).ToList();
				}
				else if (groupInfo is RadSharedModelGroupInfo sharedModelGroupInfo)
				{
					RadSubgroupInfo subgroupInfo = null;
					if (_subGroupID != Guid.Empty)
						subgroupInfo = sharedModelGroupInfo.Subgroups.FirstOrDefault(s => s.ID == _subGroupID);
					else if (!string.IsNullOrEmpty(_subGroupName))
						subgroupInfo = sharedModelGroupInfo.Subgroups.FirstOrDefault(s => s.Name.Equals(_subGroupName, StringComparison.OrdinalIgnoreCase));

					if (subgroupInfo == null)
					{
						_logger.Error("Could not find subgroup. Provide either a valid subgroup ID or a valid subgroup name.");
						_parameters = new List<RadParameter>();
						return default;
					}

					_parameters = subgroupInfo.Parameters;
				}
				else
				{
					_logger.Error("Group not found, or group is of unknown type.");
					_parameters = new List<RadParameter>();
					return default;
				}
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException($"Failed to fetch group info for group {_groupName} on agent {_dataMinerID}", ex);
			}

			// Fetch and cache elementNames and protocol info
			foreach (var param in _parameters)
			{
				_elementNamesCache.TryGet(param.Key.DataMinerID, param.Key.ElementID, out var _);
				_parametersCache.TryGet(param.Key.DataMinerID, param.Key.ElementID, out var _);
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
				new GQIStringColumn("Label"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			foreach (var param in _parameters)
			{
				var paramID = new ParamID(param.Key.DataMinerID, param.Key.ElementID, param.Key.ParameterID, param.Key.Instance);
				var key = paramID.ToString();
				var cells = new GQICell[]
				{
					new GQICell(){ Value = param.Key.ToString() },
					new GQICell(){ Value = param.Key.DataMinerID },
					new GQICell(){ Value = param.Key.ElementID },
					new GQICell(){ Value = param.Key.ParameterID },
					new GQICell(){ Value = param.Key.Instance },
					new GQICell(){ Value = param.Key.DisplayInstance },
					new GQICell(){ Value = GetElementName(param.Key) },
					new GQICell(){ Value = GetParameterName(param.Key) },
					new GQICell(){ Value = param.Label }, // Label
				};
				var parameterMetaData = new ObjectRefMetadata() { Object = paramID };
				var rowMetaData = new GenIfRowMetadata(new[] { parameterMetaData });
				rows.Add(new GQIRow(key, cells) { Metadata = rowMetaData });
			}

			return new GQIPage(rows.ToArray());
		}

		#region Helper Methods

		private string GetElementName(ParameterKey paramKey)
		{
			if (_elementNamesCache.TryGetFromCache(paramKey.DataMinerID, paramKey.ElementID, out string name))
				return name;
			else
				return "Unknown Element";
		}

		private string GetParameterName(ParameterKey paramKey)
		{
			if (!_parametersCache.TryGetFromCache(paramKey.DataMinerID, paramKey.ElementID, out var parameterInfos))
				return "Unknown Parameter";

			var paramInfo = parameterInfos?.FirstOrDefault(p => p.ID == paramKey.ParameterID);
			return paramInfo?.DisplayName ?? "Unknown Parameter";
		}

		#endregion
	}
}