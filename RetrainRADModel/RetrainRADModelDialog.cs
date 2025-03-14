﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

namespace RetrainRADModel
{
	public class RetrainRADModelDialog : Dialog
	{
		private Button okButton_;
		private MultiTimeRangeSelector timeRangeSelector_;

		public event EventHandler Accepted;
		public event EventHandler Cancelled;

		public string GroupName { get; private set; }

		public int DataMinerID { get; private set; }

		public List<TimeRange> TimeRanges => timeRangeSelector_.SelectedItems.Select(i => i.TimeRange).ToList();

		private void OnTimeRangeSelectorChanged()
		{
			okButton_.IsEnabled = timeRangeSelector_.SelectedItems.Count > 0;
		}

		public RetrainRADModelDialog(IEngine engine, string groupName, int dataMinerID) : base(engine)
		{
			GroupName = groupName;
			DataMinerID = dataMinerID;

			Title = $"Retrain model for parameter group '{groupName}'";

			var label = new Label($"Retrain the model using the following well-behaved time ranges:");

			timeRangeSelector_ = new MultiTimeRangeSelector(engine);
			timeRangeSelector_.Changed += (sender, args) => OnTimeRangeSelectorChanged();

			okButton_ = new Button("Retrain");
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
	}
}
