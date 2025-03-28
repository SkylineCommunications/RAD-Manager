namespace RetrainRADModel
{
	using System;
	using System.Globalization;
	using RadWidgets;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class TimeRangeItem : MultiSelectorItem
	{
		public TimeRangeItem(TimeRange range)
		{
			this.TimeRange = range;
		}

		public TimeRange TimeRange { get; set; }

		public override string GetKey()
		{
			return $"{TimeRange.StartTime.ToString("o", CultureInfo.InvariantCulture)}-{TimeRange.EndTime.ToString("o", CultureInfo.InvariantCulture)}";
		}

		public override string GetDisplayValue()
		{
			return $"From {TimeRange.StartTime} to {TimeRange.EndTime}";
		}
	}

	public class TimeRangeSelector : MultiSelectorItemSelector<TimeRangeItem>
	{
		private readonly DateTimePicker startTimePicker_;
		private readonly DateTimePicker endTimePicker_;

		public TimeRangeSelector(IEngine engine)
		{
			var fromLabel = new Label("From");

			startTimePicker_ = new DateTimePicker()
			{
				Maximum = DateTime.Now,
				DateTime = DateTime.Now - TimeSpan.FromDays(30),
			};
			startTimePicker_.Changed += (sender, args) => OnStartTimeSelectorChanged();

			var toLabel = new Label(" to ");

			endTimePicker_ = new DateTimePicker()
			{
				Maximum = DateTime.Now,
				DateTime = DateTime.Now,
			};
			endTimePicker_.Changed += (sender, args) => OnEndTimeSelectorChanged();

			AddWidget(fromLabel, 0, 0);
			AddWidget(startTimePicker_, 0, 1);
			AddWidget(toLabel, 0, 2);
			AddWidget(endTimePicker_, 0, 3);
		}

		public override TimeRangeItem SelectedItem
		{
			get
			{
				if (startTimePicker_.DateTime >= endTimePicker_.DateTime)
					return null;
				return new TimeRangeItem(new TimeRange(startTimePicker_.DateTime, endTimePicker_.DateTime));
			}
		}

		private void OnStartTimeSelectorChanged()
		{
			startTimePicker_.ValidationState = UIValidationState.Valid;
			if (endTimePicker_.DateTime <= startTimePicker_.DateTime)
				endTimePicker_.ValidationState = UIValidationState.Invalid;
			else
				endTimePicker_.ValidationState = UIValidationState.Valid;
		}

		private void OnEndTimeSelectorChanged()
		{
			endTimePicker_.ValidationState = UIValidationState.Valid;
			if (endTimePicker_.DateTime <= startTimePicker_.DateTime)
				startTimePicker_.ValidationState = UIValidationState.Invalid;
			else
				startTimePicker_.ValidationState = UIValidationState.Valid;
		}
	}

	public class MultiTimeRangeSelector : MultiSelector<TimeRangeItem>
	{
		public MultiTimeRangeSelector(IEngine engine) : base(new TimeRangeSelector(engine))
		{
		}
	}
}
