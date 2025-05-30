namespace RadUtils
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters.
	/// </summary>
	public abstract class ParametersCache : Cache<ParameterInfo[]>
	{
		protected ParametersCache() : base(100)
		{
		}

		protected abstract void LogError(string message);

		protected abstract DMSMessage SendSingleResponseMessage(DMSMessage request);

		protected override bool Fetch(int dataMinerID, int elementID, out ParameterInfo[] value)
		{
			try
			{
				var protocolRequest = new GetElementProtocolMessage(dataMinerID, elementID);
				var protocolResponse = SendSingleResponseMessage(protocolRequest) as GetElementProtocolResponseMessage;

				if (protocolResponse == null)
				{
					LogError($"Failed to fetch protocol for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					value = new ParameterInfo[0];
					return false;
				}

				value = protocolResponse.AllParameters.Where(p => p.IsRadSupported()).ToArray();
				return true;
			}
			catch (Exception ex)
			{
				LogError($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				value = new ParameterInfo[0];
				return false;
			}
		}
	}
}
