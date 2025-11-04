namespace RadUtils
{
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.RadToolkit;

	public static class Utils
	{
		public static bool IsRadSupported(this ParameterInfo info)
		{
			if (info == null)
				return false;
			if ((info.ID >= 64300 && info.ID < 70000) || (info.ID >= 100000 && info.ID < 1000000))
				return false;
			if (info.WriteType || info.IsDuplicate)
				return false;
			if (!info.IsTrendAnalyticsSupported)
				return false;
			return true;
		}

		public static bool HasTrending(this ParameterInfo info)
		{
			return info.RealTimeTrending || info.AverageTrending;
		}
	}
}
