namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Dialogs;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

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
		private readonly Button _configureButton;
		private readonly Label _detailsLabel;
		private List<RadSubgroupSelectorItem> _subgroups;
		private bool _forceTraining;

		public TrainingConfigurationButton(IEngine engine, int columnSpan, bool forceTraining, List<RadSubgroupSelectorItem> subgroups = null)
		{
			_engine = engine;
			_subgroups = subgroups;
			_forceTraining = forceTraining;

			_configureButton = new Button("Configure model training...")
			{
				Tooltip = "Configure the time ranges and subgroups to use for training the model",
			};
			_configureButton.Pressed += (sender, args) => OnConfigureButtonPressed();

			_detailsLabel = new Label();

			UpdateDetailsLabel();

			AddWidget(_configureButton, 0, 0);
			AddWidget(_detailsLabel, 0, 1, 1, columnSpan - 1);
			//TODO: improve lay-out
			//TODO: press edit -> specify retraining -> change number of params -> reset training: can not press apply (seems something with excluded subgroups)
		}

		public TrainingConfiguration Configuration { get; private set; } = null;

		public void SetSubgroups(bool forceTraining, List<RadSubgroupSelectorItem> subgroups)
		{
			_forceTraining = forceTraining;
			_subgroups = subgroups;

			if (Configuration != null)
				Configuration.ExcludedSubgroupIDs = Configuration.ExcludedSubgroupIDs?.Where(id => subgroups.Any(s => s.ID == id)).ToList();
			UpdateDetailsLabel();
		}

		private void OnConfigureButtonPressed()
		{
			var app = new InteractiveController(_engine);

			var dialog = new TrainingConfigurationDialog(_engine, _forceTraining, _subgroups, Configuration);
			dialog.Accepted += (sender, args) =>
			{
				Configuration = dialog.GetConfiguration();
				app.Stop();

				UpdateDetailsLabel();
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void UpdateDetailsLabel()
		{
			if (Configuration == null)
			{
				if (_forceTraining)
					_detailsLabel.Text = "Train using default settings";
				else
					_detailsLabel.Text = "Keep the existing model unchanged";
			}
			else
			{
				List<string> lines = new List<string>()
				{
					$"Train the model using data from the following time ranges:",
				};

				lines.AddRange(Configuration.SelectedTimeRanges.Select(tr => $"\t{tr.GetDisplayValue()}").Take(2));
				if (Configuration.SelectedTimeRanges.Count > 2)
					lines.Add($"\tand {Configuration.SelectedTimeRanges.Count - 2} more...");

				if (Configuration.ExcludedSubgroupIDs.Count > 0)
				{
					lines.Add("Excluding data from the following subgroups:");
					lines.AddRange(_subgroups.Where(s => Configuration.ExcludedSubgroupIDs.Contains(s.ID)).Select(s => $"\t{s.DisplayName}").Take(2));
					if (Configuration.ExcludedSubgroupIDs.Count > 2)
						lines.Add($"\tand {Configuration.ExcludedSubgroupIDs.Count - 2} more...");
				}

				_detailsLabel.Text = string.Join("\n", lines);
			}
		}
	}
}
