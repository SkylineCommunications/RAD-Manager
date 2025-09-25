namespace RadDataSources
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Extend time range")]
	public class ExtendTimeRangeOperator : IGQIRowOperator, IGQIInputArguments
	{
		private GQIDoubleArgument _factorArg = new GQIDoubleArgument("Factor")
		{
			DefaultValue = 3,
			IsRequired = true,
		};

		private double _factor;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _factorArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_factorArg, out _factor))
			{
				throw new ArgumentException($"Required argument factor was not provided");
			}

			if (_factor <= 0)
			{
				throw new ArgumentException($"Factor must be greater than 0");
			}

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

			var duration = existingTimeRange.EndTime - existingTimeRange.StartTime;
			double multiplier = (_factor - 1) / 2;
			var newTimeRange = new TimeRangeMetadata()
			{
				StartTime = existingTimeRange.StartTime.AddSeconds(-duration.TotalSeconds * multiplier),
				EndTime = existingTimeRange.EndTime.AddSeconds(duration.TotalSeconds * multiplier),
			};

			row.Metadata = new GenIfRowMetadata(existingMetaData.Where(m => !(m is TimeRangeMetadata)).Concat(new[] { newTimeRange }).ToArray());
		}
	}
}