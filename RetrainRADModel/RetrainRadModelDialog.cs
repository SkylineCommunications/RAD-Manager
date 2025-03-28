namespace RetrainRADModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RetrainRadModelDialog : Dialog
	{
		private readonly Button okButton_;
		private readonly MultiTimeRangeSelector timeRangeSelector_;

		public RetrainRadModelDialog(IEngine engine, string groupName, int dataMinerID) : base(engine)
		{
			ShowScriptAbortPopup = false;
			GroupName = groupName;
			DataMinerID = dataMinerID;

			Title = $"Retrain model for parameter group '{groupName}'";

			var label = new Label($"Retrain the model using the following time ranges with normal behavior:");

			timeRangeSelector_ = new MultiTimeRangeSelector(engine);
			timeRangeSelector_.Changed += (sender, args) => OnTimeRangeSelectorChanged();

			okButton_ = new Button("Retrain")
			{
				Style = ButtonStyle.CallToAction,
			};
			okButton_.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnTimeRangeSelectorChanged();

			int row = 0;
			AddWidget(label, row, 0, 1, timeRangeSelector_.ColumnCount);
			row++;

			AddSection(timeRangeSelector_, row, 0);
			row += timeRangeSelector_.RowCount;

			AddWidget(cancelButton, row, 0, 1, 2);
			AddWidget(okButton_, row, 2, 1, timeRangeSelector_.ColumnCount - 2);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public string GroupName { get; private set; }

		public int DataMinerID { get; private set; }

		public IEnumerable<TimeRange> GetSelectedTimeRanges()
		{
			return timeRangeSelector_.GetSelected().Select(i => i.TimeRange);
		}

		private void OnTimeRangeSelectorChanged()
		{
			okButton_.IsEnabled = timeRangeSelector_.GetSelected().Any();
		}
	}
}
