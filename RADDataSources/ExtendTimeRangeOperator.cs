namespace RadDataSources
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Extend time range")]
	public class ExtendTimeRangeOperator : IGQIRowOperator, IGQIInputArguments
	{
		private readonly GQIDoubleArgument _factorArg = new GQIDoubleArgument("Factor")
		{
			DefaultValue = 3,
			IsRequired = true,
		};

		private readonly GQIDoubleArgument _minimumHoursArg = new GQIDoubleArgument("Minimum duration in hours")
		{
			DefaultValue = 0,
			IsRequired = false,
		};

		private double _factor;
		private double _minimumHours;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _factorArg, _minimumHoursArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_factorArg, out _factor))
				throw new ArgumentException($"Required argument factor was not provided");

			if (_factor <= 0)
				throw new ArgumentException($"Factor must be greater than 0");

			if (!args.TryGetArgumentValue(_minimumHoursArg, out _minimumHours))
				_minimumHours = 0;

			if (_minimumHours < 0)
				throw new ArgumentException($"Minimum duration can not be negative");

			return default;
		}

		public void HandleRow(GQIEditableRow row)
		{
			var existingMetaData = row?.Metadata?.Metadata;
			if (existingMetaData == null)
				return;

			var existingTimeRange = existingMetaData.OfType<TimeRangeMetadata>().FirstOrDefault();
			if (existingTimeRange == null)
				return;

			var startTime = existingTimeRange.StartTime;
			var minimumEndTime = startTime.AddHours(_minimumHours);
			var endTime = existingTimeRange.EndTime < minimumEndTime ? minimumEndTime : existingTimeRange.EndTime;

			var duration = endTime - startTime;
			double multiplier = (_factor - 1) / 2;
			var newTimeRange = new TimeRangeMetadata()
			{
				StartTime = startTime.AddSeconds(-duration.TotalSeconds * multiplier),
				EndTime = endTime.AddSeconds(duration.TotalSeconds * multiplier),
			};

			row.Metadata = new GenIfRowMetadata(existingMetaData.Where(m => !(m is TimeRangeMetadata)).Concat(new[] { newTimeRange }).ToArray());
		}
	}
}