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
		private List<RadSubgroupSelectorItem> _subgroups;
		private bool _forceTraining;

		public TrainingConfigurationButton(IEngine engine, RadHelper radHelper, bool forceTraining, List<RadSubgroupSelectorItem> subgroups = null)
		{
			_engine = engine;
			_radHelper = radHelper;
			_subgroups = subgroups;
			_forceTraining = forceTraining;

			var configureButton = new Button("Configure model training...")
			{
				Tooltip = "Configure the time ranges and subgroups to use for training the model",
			};
			configureButton.Pressed += (sender, args) => OnConfigureButtonPressed();

			AddWidget(configureButton, 0, 0);
		}

		public TrainingConfiguration Configuration { get; private set; } = null;

		public void SetSubgroups(bool forceTraining, List<RadSubgroupSelectorItem> subgroups)
		{
			_forceTraining = forceTraining;
			_subgroups = subgroups;

			if (Configuration != null)
				Configuration.ExcludedSubgroupIDs = Configuration.ExcludedSubgroupIDs?.Where(id => subgroups.Any(s => s.ID == id)).ToList();
		}

		private void OnConfigureButtonPressed()
		{
			var app = new InteractiveController(_engine);

			var dialog = new TrainingConfigurationDialog(_engine, _radHelper, _forceTraining, _subgroups, Configuration);
			dialog.Accepted += (sender, args) =>
			{
				Configuration = dialog.GetConfiguration();
				app.Stop();
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}
	}
}
