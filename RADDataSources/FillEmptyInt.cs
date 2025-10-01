namespace RadDataSources
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Fill empty int")]
	public class FillEmptyInt : IGQIRowOperator, IGQIInputArguments
	{
		private readonly GQIColumnDropdownArgument _columnArg = new GQIColumnDropdownArgument("Target column")
		{
			IsRequired = true,
			Types = new[] { GQIColumnType.Int },
		};

		private readonly GQIIntArgument _fillValueArg = new GQIIntArgument("Fill value")
		{
			IsRequired = true,
			DefaultValue = 0,
		};

		private GQIColumn _targetColumn;
		private int _fillValue;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _columnArg, _fillValueArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_columnArg, out _targetColumn) || _targetColumn == null)
				throw new ArgumentException($"Required argument '{_columnArg.Name}' was not provided");

			if (!args.TryGetArgumentValue(_fillValueArg, out _fillValue))
				throw new ArgumentException($"Required argument '{_fillValueArg.Name}' was not provided");

			return default;
		}

		public void HandleRow(GQIEditableRow row)
		{
			if (row == null)
				return;

			object current = row.GetValue(_targetColumn.Name);
			if (current == null)
				row.SetValue(_targetColumn.Name, _fillValue);
		}
	}
}