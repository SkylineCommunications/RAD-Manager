namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Dialogs;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class TrainingConfiguration
	{
		public TrainingConfiguration(List<TimeRangeItem> selectedTimeRanges, List<Guid> excludedSubgroupIDs)
		{
			ExcludedSubgroupIDs = excludedSubgroupIDs;
			SelectedTimeRanges = selectedTimeRanges;
		}

		public List<Guid> ExcludedSubgroupIDs { get; set; }

		public List<TimeRangeItem> SelectedTimeRanges { get; set; }
	}

	public class TrainingConfigurationButton : Section
	{
		private readonly IEngine _engine;
		private readonly RadHelper _radHelper;
		private readonly CheckBox _overrideCheckBox;
		private readonly Button _configureButton;
		private List<RadSubgroupSelectorItem> _subgroups;
		private bool _forceTraining;
		private TrainingConfiguration _configuration;

		public TrainingConfigurationButton(IEngine engine, RadHelper radHelper, int columnSpan, bool forceTraining, List<RadSubgroupSelectorItem> subgroups = null)
		{
			_engine = engine;
			_radHelper = radHelper;
			_subgroups = subgroups;
			_forceTraining = forceTraining;

			_overrideCheckBox = new CheckBox()
			{
				IsChecked = false,
			};
			SetOverrideCheckBoxText();
			_overrideCheckBox.Changed += (sender, args) => OnOverrideCheckBoxChanged();

			_configureButton = new Button("Configure model training...")
			{
				Tooltip = "Configure the time ranges and subgroups to use for training the model",
				Width = 200,
			};
			_configureButton.Pressed += (sender, args) => OnConfigureButtonPressed();

			OnOverrideCheckBoxChanged();

			AddWidget(_overrideCheckBox, 0, 0);
			AddWidget(_configureButton, 0, 1, 1, columnSpan - 1);
		}

		public TrainingConfiguration Configuration => _overrideCheckBox.IsChecked ? _configuration : null;

		public void SetSubgroups(bool forceTraining, List<RadSubgroupSelectorItem> subgroups)
		{
			_forceTraining = forceTraining;
			_subgroups = subgroups;

			if (Configuration != null)
				Configuration.ExcludedSubgroupIDs = Configuration.ExcludedSubgroupIDs?.Where(id => subgroups.Any(s => s.ID == id)).ToList();

			SetOverrideCheckBoxText();
		}

		private void SetOverrideCheckBoxText()
		{
			if (_forceTraining)
			{
				_overrideCheckBox.Text = "Override default settings for model training";
				_overrideCheckBox.Tooltip = "Whether the override the default settings for training the relational anomaly detection model. By default, " +
					"all available data from the last two months of data is used for training.";
			}
			else
			{
				_overrideCheckBox.Text = "Retrain relational anomaly model";
				_overrideCheckBox.Tooltip = "Whether to retrain the model for this relational anomaly group. If checked, a new model will be built with according " +
					"to the options you select. If unchecked, the existing model will remain unchanged.";
			}
		}

		private void OnConfigureButtonPressed()
		{
			var app = new InteractiveController(_engine);

			var dialog = new TrainingConfigurationDialog(_engine, _radHelper, _subgroups, Configuration);
			dialog.Accepted += (sender, args) =>
			{
				_configuration = dialog.GetConfiguration();
				app.Stop();
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnOverrideCheckBoxChanged()
		{
			_configureButton.IsEnabled = _overrideCheckBox.IsChecked;
			if (_overrideCheckBox.IsChecked)
			{
				var endTime = DateTime.Now;
				var startTime = endTime - TimeSpan.FromDays(_radHelper.DefaultTrainingDays);
				var timeRanges = new List<TimeRangeItem>() { new TimeRangeItem(new TimeRange(startTime, endTime)) };
				_configuration = new TrainingConfiguration(timeRanges, new List<Guid>());
			}
			else
			{
				_configuration = null;
			}
		}
	}
}
