namespace RadDataSources
{
	using System.ComponentModel;

	public enum SortingColumn
	{
		[Description("Name")]
		Name,
		[Description("Number of anomalies in Last 30 Days")]
		AnomaliesInLast30Days,
		[Description("Adaptive / Static model")]
		UpdateModel,
		[Description("Anomaly Threshold")]
		AnomalyThreshold,
		[Description("Minimum Anomaly Duration")]
		MinimumAnomalyDuration,
		[Description("Is Outlier")]
		IsOutlier,
	}
}
