namespace RadWidgets.Widgets
{
	using System;
	using System.Globalization;
	using RadWidgets.Widgets.Dialogs;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class TimeRangeItem : SelectorItem
	{
		public TimeRangeItem(TimeRange range)
		{
			this.TimeRange = range;
		}

		public TimeRange TimeRange { get; set; }

		public override string GetKey()
		{
			return $"{TimeRange.Start.ToString("o", CultureInfo.InvariantCulture)}-{TimeRange.End.ToString("o", CultureInfo.InvariantCulture)}";
		}

		public override string GetDisplayValue()
		{
			return $"From {TimeRange.Start} to {TimeRange.End}";
		}

		public override bool Equals(object obj)
		{
			if (obj is TimeRangeItem other)
				return other.TimeRange.Start == this.TimeRange.Start && other.TimeRange.End == this.TimeRange.End;

			return false;
		}

		public override int GetHashCode()
		{
			var hash = TimeRange.Start.GetHashCode();
			hash ^= TimeRange.End.GetHashCode();

			return hash;
		}
	}

	public class TimeRangeSelector : MultiSelectorItemSelector<TimeRangeItem>
	{
		private readonly DateTimePicker _startTimePicker;
		private readonly DateTimePicker _endTimePicker;

		public TimeRangeSelector(IEngine engine, DateTime startTime, DateTime endTime)
		{
			var fromLabel = new Label("From");

			_startTimePicker = new DateTimePicker()
			{
				Maximum = DateTime.Now,
				DateTime = startTime,
			};
			_startTimePicker.Changed += (sender, args) => OnStartTimeSelectorChanged();

			var toLabel = new Label(" to ");

			_endTimePicker = new DateTimePicker()
			{
				Maximum = DateTime.Now,
				DateTime = endTime,
			};
			_endTimePicker.Changed += (sender, args) => OnEndTimeSelectorChanged();

			AddWidget(fromLabel, 0, 0);
			AddWidget(_startTimePicker, 0, 1);
			AddWidget(toLabel, 0, 2);
			AddWidget(_endTimePicker, 0, 3);
		}

		public override TimeRangeItem SelectedItem
		{
			get
			{
				if (_startTimePicker.DateTime >= _endTimePicker.DateTime)
					return null;
				return new TimeRangeItem(new TimeRange(_startTimePicker.DateTime, _endTimePicker.DateTime));
			}
		}

		private void OnStartTimeSelectorChanged()
		{
			_startTimePicker.ValidationState = UIValidationState.Valid;
			if (_endTimePicker.DateTime <= _startTimePicker.DateTime)
				_endTimePicker.ValidationState = UIValidationState.Invalid;
			else
				_endTimePicker.ValidationState = UIValidationState.Valid;
		}

		private void OnEndTimeSelectorChanged()
		{
			_endTimePicker.ValidationState = UIValidationState.Valid;
			if (_endTimePicker.DateTime <= _startTimePicker.DateTime)
				_startTimePicker.ValidationState = UIValidationState.Invalid;
			else
				_startTimePicker.ValidationState = UIValidationState.Valid;
		}
	}

	public class MultiTimeRangeSelector : MultiSelector<TimeRangeItem>
	{
		public MultiTimeRangeSelector(IEngine engine, DateTime initialStartTime, DateTime initialEndTime, string emptyText) :
			base(new TimeRangeSelector(engine, initialStartTime, initialEndTime), null, emptyText)
		{
			AddButtonTooltip = "Add the range specified on the left to the ranges with normal behavior.";
			RemoveButtonTooltip = "Remove the range(s) selected on the left from the ranges with normal behavior.";
		}
	}
}
