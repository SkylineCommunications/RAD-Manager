namespace EditRADParameterGroup
{
	using System;
	using RadWidgets;
	using RadWidgets.Widgets.Editors;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class EditParameterGroupDialog : Dialog
	{
		private readonly RadGroupEditor _groupEditor;
		private readonly Button _okButton;

		public EditParameterGroupDialog(IEngine engine, RadHelper radHelper, RadGroupInfo groupSettings, int dataMinerID) : base(engine)
		{
			ShowScriptAbortPopup = false;
			DataMinerID = dataMinerID;
			Title = $"Edit Group '{groupSettings.GroupName}'";
			var parametersCache = new EngineParametersCache(engine);

			var groupNames = radHelper.FetchParameterGroups();
			_groupEditor = new RadGroupEditor(engine, radHelper, groupNames, parametersCache, groupSettings);
			_groupEditor.ValidationChanged += (sender, args) => OnGroupEditorValidationChanged();

			_okButton = new Button("Apply")
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel")
			{
				MaxWidth = Constants.GROUP_EDITOR_CANCEL_BUTTON_MAX_WIDTH,
			};
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnGroupEditorValidationChanged();

			int row = 0;
			AddSection(_groupEditor, row, 0);
			row += _groupEditor.RowCount;

			AddWidget(cancelButton, row, _groupEditor.ColumnCount - 2, horizontalAlignment: HorizontalAlignment.Right);
			AddWidget(_okButton, row, _groupEditor.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public int DataMinerID { get; private set; }

		public void GetSettings(out RadGroupSettings settings, out TrainingConfiguration trainingConfiguration)
		{
			_groupEditor.GetSettings(out settings, out trainingConfiguration);
		}

		private void OnGroupEditorValidationChanged()
		{
			if (_groupEditor.IsValid)
			{
				_okButton.IsEnabled = true;
				_okButton.Tooltip = "Edit the selected relational anomaly group as specified above";
			}
			else
			{
				_okButton.IsEnabled = false;
				_okButton.Tooltip = _groupEditor.ValidationText;
			}
		}
	}
}
