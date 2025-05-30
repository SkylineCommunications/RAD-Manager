namespace RadWidgets
{
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters using the IEngine class to send messages.
	/// </summary>
	public class EngineParametersCache : RadUtils.ParametersCache
	{
		private readonly IEngine _engine;

		public EngineParametersCache(IEngine engine)
		{
			_engine = engine;
		}

		protected override void LogError(string message)
		{
			_engine.Log(message);
		}

		protected override DMSMessage SendSingleResponseMessage(DMSMessage request)
		{
			return _engine.SendSLNetSingleResponseMessage(request);
		}
	}
}
