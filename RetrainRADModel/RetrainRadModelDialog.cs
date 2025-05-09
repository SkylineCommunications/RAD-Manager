namespace RetrainRADModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RetrainRadModelDialog : Dialog
	{
		private readonly Button _okButton;
		private readonly MultiTimeRangeSelector _timeRangeSelector;

		public RetrainRadModelDialog(IEngine engine, string groupName, int dataMinerID) : base(engine)
		{
			ShowScriptAbortPopup = false;
			GroupName = groupName;
			DataMinerID = dataMinerID;

			Title = $"Retrain model for parameter group '{groupName}'";

			var label = new Label($"Retrain the model using the following time ranges with normal behavior:");

			_timeRangeSelector = new MultiTimeRangeSelector(engine);
			_timeRangeSelector.Changed += (sender, args) => OnTimeRangeSelectorChanged();

			_okButton = new Button("Retrain")
			{
				Style = ButtonStyle.CallToAction,
				Tooltip = "Train the selected parameter group using the trend data in the time ranges selected above.",
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnTimeRangeSelectorChanged();

			int row = 0;
			AddWidget(label, row, 0, 1, _timeRangeSelector.ColumnCount);
			row++;

			AddSection(_timeRangeSelector, row, 0);
			row += _timeRangeSelector.RowCount;

			AddWidget(cancelButton, row, 0, 1, 2);
			AddWidget(_okButton, row, 2, 1, _timeRangeSelector.ColumnCount - 2);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public string GroupName { get; private set; }

		public int DataMinerID { get; private set; }

		public IEnumerable<RadUtils.TimeRange> GetSelectedTimeRanges()
		{
			return _timeRangeSelector.GetSelected().Select(i => i.TimeRange);
		}

		private void OnTimeRangeSelectorChanged()
		{
			_okButton.IsEnabled = _timeRangeSelector.GetSelected().Any();
		}
	}
}
