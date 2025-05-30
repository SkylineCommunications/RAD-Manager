namespace RadUtils
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;

	public class RadSharedModelGroupInfo : ARadSharedModelGroupSettings<RadSubgroupInfo>, IRadGroupBaseInfo
	{
	}

	public class RadSharedModelGroupSettings : ARadSharedModelGroupSettings<RadSubgroupSettings>
	{
	}

	public abstract class ARadSharedModelGroupSettings<T> : RadGroupBaseSettings where T : RadSubgroupSettings
	{
		/// <summary>
		/// Gets or sets the parameter subgroups in the RAD group.
		/// </summary>
		public List<T> Subgroups { get; set; }
	}

	public class RadSubgroupInfo : RadSubgroupSettings
	{
		public bool IsMonitored { get; set; } = true;
	}

	public class RadSubgroupOptions : RadGroupBaseOptions
	{
		/// <summary>
		/// Gets the anomaly threshold set in the options, or the parent anomaly threshold if none was set, or the default value if also that one was not set.
		/// </summary>
		/// <param name="parentAnomalyThreshold">The parent anomaly threshold.</param>
		/// <returns>The anomaly threshold.</returns>
		public double GetAnomalyThresholdOrDefault(double? parentAnomalyThreshold)
		{
			return AnomalyThreshold ?? parentAnomalyThreshold ?? DefaultAnomalyThreshold;
		}

		/// <summary>
		/// Gets the minimal anomaly duration set in the options, or the parent minimal duration if none was set,
		/// or the default value if also that one was not set.
		/// </summary>
		/// <param name="parentMinimalDuration">The parent minimal anomaly duration.</param>
		/// <returns>The minimal anomaly duration.</returns>
		public int GetMinimalDurationOrDefault(int? parentMinimalDuration)
		{
			return MinimalDuration ?? parentMinimalDuration ?? DefaultMinimalDuration;
		}
	}

	public class RadParameter
	{
		public ParameterKey Key { get; set; }

		public string Label { get; set; }
	}

	public class RadSubgroupSettings
	{
		/// <summary>
		/// Gets or sets the name of the subgroup.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the ID of the subgroup.
		/// </summary>
		public Guid ID { get; set; }

		/// <summary>
		/// Gets or sets the parameters in the subgroup.
		/// </summary>
		public List<RadParameter> Parameters { get; set; }

		/// <summary>
		/// Gets or sets the options for the subgroup.
		/// </summary>
		public RadSubgroupOptions Options { get; set; }

		public string GetName(string parentName)
		{
			return string.IsNullOrEmpty(Name) ? parentName : Name;
		}
	}
}
