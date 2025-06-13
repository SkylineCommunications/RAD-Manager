namespace RadDataSources
{
	using System;
	using RadUtils;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for element names.
	/// </summary>
	public class ElementNameCache : Cache<string>
	{
		private readonly IGQILogger _logger = null;

		public ElementNameCache(IGQILogger logger) : base(1000)
		{
			_logger = logger;
		}

		protected override bool Fetch(int dataMinerID, int elementID, out string value)
		{
			try
			{
				var elementRequest = new GetElementByIDMessage(dataMinerID, elementID);
				var elementResponse = ConnectionHelper.Connection.HandleSingleResponseMessage(elementRequest) as ElementInfoEventMessage;

				if (elementResponse == null)
				{
					_logger.Error($"Failed to fetch element info for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					value = string.Empty;
					return false;
				}

				value = elementResponse.Name;
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				value = string.Empty;
				return false;
			}
		}
	}
}
