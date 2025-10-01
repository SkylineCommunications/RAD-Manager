namespace RadDataSources
{
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Net;

	public static class Utils
	{
		public static ParamID ToParamID(this ParameterKey key)
		{
			if (key == null)
			{
				return null;
			}

			return new ParamID(key.DataMinerID, key.ElementID, key.ParameterID, key.Instance);
		}
	}
}
