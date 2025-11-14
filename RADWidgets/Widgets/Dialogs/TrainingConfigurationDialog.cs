namespace RadWidgets.Widgets.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class TrainingConfigurationDialog : Dialog
	{
		private const int DEFAULT_TRAINING_DAYS = 60;
		private readonly Button _okButton;
		private readonly bool _forceTraining;
		private readonly List<TimeRangeItem> _defaultTimeRanges;
		private readonly MultiTimeRangeSelector _timeRangeSelector;
		private readonly CollapsibleCheckboxList<Guid> _excludedSubgroupsList = null;
		private readonly Button _resetButton;

		public TrainingConfigurationDialog(IEngine engine, bool forceTraining, List<RadSubgroupSelectorItem> subgroups = null,
			Widgets.TrainingConfiguration configuration = null) : base(engine)
		{
			_forceTraining = forceTraining;
			_defaultTimeRanges = new List<TimeRangeItem>();
			var endTime = DateTime.Now;
			var startTime = endTime - TimeSpan.FromDays(DEFAULT_TRAINING_DAYS);
			if (_forceTraining)
			{
				_defaultTimeRanges.Add(new TimeRangeItem(new TimeRange(startTime, endTime)));
			}

			Title = $"Specify Training Range";

			var label = new Label($"Train the model using data from the following time ranges with normal behavior:");

			string emptyText;
			if (_forceTraining)
				emptyText = "No time ranges selected. Select at least one time range above and press 'Add'.";
			else
				emptyText = "No time ranges selected. We will keep the existing model unchanged.";
			_timeRangeSelector = new MultiTimeRangeSelector(engine, startTime, endTime, emptyText);
			if (configuration?.SelectedTimeRanges != null)
				_timeRangeSelector.SetSelected(configuration.SelectedTimeRanges);
			else
				_timeRangeSelector.SetSelected(_defaultTimeRanges);
			_timeRangeSelector.Changed += (sender, args) => UpdateIsValid();

			if (subgroups != null)
			{
				var options = subgroups.Select(s => new Option<Guid>(s.DisplayName, s.ID)).OrderBy(o => o.DisplayValue);
				_excludedSubgroupsList = new CollapsibleCheckboxList<Guid>(options, _timeRangeSelector.ColumnCount)
				{
					Text = "Exclude specific subgroups",
					Tooltip = "Data from the selected subgroups will not be taken into account when training the model. " +
						"This can be used to exclude subgroups that had anomalous behavior during the selected time range.",
					ExpandText = "Select",
					CollapseText = "Unselect all",
				};
				if (configuration?.ExcludedSubgroupIDs != null)
					_excludedSubgroupsList.SetChecked(configuration.ExcludedSubgroupIDs);
				_excludedSubgroupsList.Changed += (sender, args) => UpdateIsValid();
			}

			_resetButton = new Button("Reset to default");
			if (_forceTraining)
			{
				if (subgroups != null)
					_resetButton.Tooltip = "Reset the time ranges and excluded subgroups to their default values.";
				else
					_resetButton.Tooltip = "Reset the time ranges to their default values.";
			}
			else
			{
				_resetButton.Tooltip = "Keep the existing model unchanged instead of retraining.";
			}

			_resetButton.Pressed += (sender, args) => OnResetButtonPressed();

			_okButton = new Button("Apply")
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			UpdateIsValid();

			int row = 0;
			AddWidget(label, row, 0, 1, _timeRangeSelector.ColumnCount);
			row++;

			AddSection(_timeRangeSelector, row, 0);
			row += _timeRangeSelector.RowCount;

			if (_excludedSubgroupsList != null)
			{
				AddSection(_excludedSubgroupsList, row, 0);
				row += _excludedSubgroupsList.RowCount;
			}

			AddWidget(_resetButton, row, _timeRangeSelector.ColumnCount - 1);
			row++;

			AddWidget(cancelButton, row, 0, 1, 2);
			AddWidget(_okButton, row, 2, 1, _timeRangeSelector.ColumnCount - 2);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public Widgets.TrainingConfiguration GetConfiguration()
		{
			var selectedTimeRanges = _timeRangeSelector.GetSelected().ToList();
			var excludedSubgroupIDs = _excludedSubgroupsList?.GetChecked().ToList() ?? new List<Guid>();

			var comparer = new TimeRangeItemListEqualityComparer();
			if (excludedSubgroupIDs.Count == 0 && comparer.Equals(selectedTimeRanges, _defaultTimeRanges))
				return null;

			return new Widgets.TrainingConfiguration(selectedTimeRanges, excludedSubgroupIDs);
		}

		private void UpdateIsValid()
		{
			bool timeRangeSelected = _timeRangeSelector.GetSelected().Any();
			bool subgroupExcluded = _excludedSubgroupsList != null && _excludedSubgroupsList.GetChecked().Any();

			if (!timeRangeSelected)
			{
				if (!subgroupExcluded && !_forceTraining)
				{
					_okButton.Tooltip = "The existing model will be kept unchanged.";
					_okButton.IsEnabled = true;
				}
				else
				{
					_okButton.Tooltip = "Select at least one time range to train the model.";
					_okButton.IsEnabled = false;
				}
			}
			else if (_excludedSubgroupsList != null && !_excludedSubgroupsList.GetUnchecked().Any())
			{
				_okButton.Tooltip = "At least one subgroup must be included for training the model.";
				_okButton.IsEnabled = false;
			}
			else
			{
				_okButton.Tooltip = "Train the selected relational anomaly group using the trend data in the time ranges selected above.";
				_okButton.IsEnabled = true;
			}
		}

		private void OnResetButtonPressed()
		{
			_timeRangeSelector.SetSelected(_defaultTimeRanges);
			_excludedSubgroupsList?.UncheckAll();
		}
	}
}
