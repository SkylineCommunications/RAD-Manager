﻿namespace RadDataSources
{
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters.
	/// </summary>
	public class ParametersCache : RadUtils.ParametersCache
	{
		private readonly IGQILogger _logger = null;

		public ParametersCache(IGQILogger logger)
		{
			_logger = logger;
		}

		protected override void LogError(string message)
		{
			_logger.Error(message);
		}

		protected override DMSMessage SendSingleResponseMessage(DMSMessage request)
		{
			return ConnectionHelper.Connection.HandleSingleResponseMessage(request);
		}
	}
}
