namespace RadDataSourceUtils
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters.
	/// </summary>
	public class ParametersCache : Cache<ParameterInfo[]>
	{
		private readonly IGQILogger logger_ = null;
		private readonly IConnection connection_ = null;

		public ParametersCache(IConnection connection, IGQILogger logger)
		{
			connection_ = connection;
			logger_ = logger;
		}

		protected override bool Fetch(int dataMinerID, int elementID, out ParameterInfo[] value)
		{
			try
			{
				var protocolRequest = new GetElementProtocolMessage(dataMinerID, elementID);
				var protocolResponse = connection_.HandleSingleResponseMessage(protocolRequest) as GetElementProtocolResponseMessage;

				if (protocolResponse == null)
				{
					logger_.Error($"Failed to fetch protocol for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					value = new ParameterInfo[0];
					return false;
				}

				value = protocolResponse.AllParameters;
				return true;
			}
			catch (Exception ex)
			{
				logger_.Error($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				value = new ParameterInfo[0];
				return false;
			}
		}
	}
}
