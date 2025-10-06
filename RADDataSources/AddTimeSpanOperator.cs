namespace RadDataSources
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Add time span")]
	public class AddTimeSpanOperator : IGQIRowOperator, IGQIInputArguments, IGQIColumnOperator
	{
		private readonly GQIColumnDropdownArgument _dateTimeColumnArg = new GQIColumnDropdownArgument("Column")
		{
			IsRequired = true,
			Types = new[] { GQIColumnType.DateTime },
		};

		private readonly GQIDoubleArgument _hoursArg = new GQIDoubleArgument("Hours to add")
		{
			IsRequired = true,
			DefaultValue = 0.0,
		};

		private readonly GQIStringArgument _outputColumnArg = new GQIStringArgument("Output column name")
		{
			IsRequired = true,
			DefaultValue = "Adjusted DateTime",
		};

		private GQIColumn _dateTimeColumn;
		private TimeSpan _timeSpan;
		private string _outputColumnName;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _dateTimeColumnArg, _hoursArg, _outputColumnArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_dateTimeColumnArg, out _dateTimeColumn) || _dateTimeColumn == null)
				throw new ArgumentException("Required DateTime column was not provided");

			if (!args.TryGetArgumentValue(_hoursArg, out double hoursToAdd))
				throw new ArgumentException("Required hours value was not provided");

			_timeSpan = TimeSpan.FromHours(hoursToAdd);

			if (!args.TryGetArgumentValue(_outputColumnArg, out _outputColumnName) || string.IsNullOrWhiteSpace(_outputColumnName))
				throw new ArgumentException("Required output column name was not provided");

			return default;
		}

		public void HandleColumns(GQIEditableHeader header)
		{
			header.AddColumns(new GQIDateTimeColumn(_outputColumnName));
		}

		public void HandleRow(GQIEditableRow row)
		{
			if (row == null)
				return;

			object raw = row.GetValue(_dateTimeColumn.Name);
			if (!(raw is DateTime dt))
				return;

			row.SetValue(_outputColumnName, dt.Add(_timeSpan));
		}
	}
}