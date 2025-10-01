namespace RadDataSources
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Round")]
	public class RoundOperator : IGQIRowOperator, IGQIInputArguments
	{
		private readonly GQIColumnDropdownArgument _columnArg = new GQIColumnDropdownArgument("Column")
		{
			IsRequired = true,
			Types = new GQIColumnType[] { GQIColumnType.Double },
		};

		private readonly GQIIntArgument _decimalsArg = new GQIIntArgument("Decimals")
		{
			DefaultValue = 0,
			IsRequired = true,
		};

		private GQIColumn _column;
		private int _decimals;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _columnArg, _decimalsArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_columnArg, out _column) || _column == null)
				throw new ArgumentException($"Required argument '{_columnArg.Name}' was not provided");

			if (!args.TryGetArgumentValue(_decimalsArg, out _decimals) || _decimals < 0)
				throw new ArgumentException($"Argument '{_decimalsArg.Name}' must be zero or a positive integer");

			return default;
		}

		public void HandleRow(GQIEditableRow row)
		{
			if (row == null)
				return;

			object value = row.GetValue(_column.Name);
			if (value == null)
				return;
			double? doubleValue = value as double?;
			if (!doubleValue.HasValue)
				return;

			try
			{
				double rounded = Math.Round(doubleValue.Value, _decimals);
				row.SetDisplayValue(_column.Name, rounded.ToString($"G{_decimals + 1}"));
			}
			catch (ArgumentOutOfRangeException)
			{
				// Ignore non-roundable values.
			}
		}
	}
}
