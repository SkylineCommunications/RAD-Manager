namespace RADDataSources
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Filter days ago")]
	public class FilterDaysAgoOperator : IGQIRowOperator, IGQIInputArguments
	{
		private const string MORE_RECENT = "More recent";
		private const string OLDER = "Older";

		private readonly GQIColumnDropdownArgument _timeColumnArg = new GQIColumnDropdownArgument("Time column")
		{
			IsRequired = true,
			Types = new[] { GQIColumnType.DateTime },
		};

		private readonly GQIStringDropdownArgument _moreRecentArg = new GQIStringDropdownArgument("Keep more recent/older", new[] { MORE_RECENT, OLDER })
		{
			DefaultValue = "More recent",
			IsRequired = true,
		};

		private readonly GQIDoubleArgument _daysAgoArg = new GQIDoubleArgument("Days ago")
		{
			DefaultValue = 0,
			IsRequired = true,
		};

		private GQIColumn _timeColumn;
		private DateTime _thresholdUtc;
		private bool _keepMoreRecent = true;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _timeColumnArg, _moreRecentArg, _daysAgoArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_timeColumnArg, out _timeColumn) || _timeColumn == null)
				throw new ArgumentException($"Required argument '{_timeColumnArg.Name}' was not provided");

			if (!args.TryGetArgumentValue(_moreRecentArg, out string moreRecent) || string.IsNullOrWhiteSpace(moreRecent))
				throw new ArgumentException($"Required argument '{_moreRecentArg.Name}' was not provided");

			if (!args.TryGetArgumentValue(_daysAgoArg, out double daysAgo) || daysAgo < 0)
				throw new ArgumentException($"Argument '{_daysAgoArg.Name}' must be zero or positive");

			_thresholdUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(daysAgo));
			_keepMoreRecent = moreRecent == MORE_RECENT;

			return default;
		}

		public void HandleRow(GQIEditableRow row)
		{
			if (row == null)
				return;

			object raw = row.GetValue(_timeColumn.Name);
			if (!(raw is DateTime dt))
				return;
			if (dt.Kind != DateTimeKind.Utc)
				throw new ArgumentException($"Time column '{_timeColumn.Name}' must contain UTC DateTime values");

			if (_keepMoreRecent)
			{
				 if (dt < _thresholdUtc)
					row.Delete();
			}
			else
			{
				if (dt > _thresholdUtc)
					row.Delete();
			}
		}
	}
}
