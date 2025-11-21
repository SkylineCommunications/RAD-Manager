namespace AddRadParameterGroup
{
	using System;
	using RadWidgets;
	using RadWidgets.Widgets.Editors;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class AddParameterGroupDialog : Dialog
	{
		private const string OK_BUTTON_TOOLTIP = "Add the relational anomaly group specified above to the RAD configuration";
		private readonly RadGroupEditor _groupEditor;
		private readonly Button _okButton;

		public AddParameterGroupDialog(IEngine engine, RadHelper radHelper) : base(engine)
		{
			ShowScriptAbortPopup = false;
			Title = "Add Relational Anomaly Group";

			var existingGroupNames = radHelper.FetchParameterGroups();
			var parametersCache = new EngineParametersCache(engine);
			_groupEditor = new RadGroupEditor(engine, radHelper, existingGroupNames, parametersCache);
			_groupEditor.ValidationChanged += (sender, args) => OnEditorValidationChanged();

			_okButton = new Button("Add group")
			{
				Style = ButtonStyle.CallToAction,
				Tooltip = OK_BUTTON_TOOLTIP,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel")
			{
				MaxWidth = 150,
			};
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnEditorValidationChanged();

			int row = 0;
			AddSection(_groupEditor, row, 0);
			row += _groupEditor.RowCount;

			AddWidget(cancelButton, row, _groupEditor.ColumnCount - 2, horizontalAlignment: HorizontalAlignment.Right);
			AddWidget(_okButton, row, _groupEditor.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public void GetSettings(out RadGroupSettings settings, out TrainingConfiguration trainingConfiguration)
		{
			_groupEditor.GetSettings(out settings, out trainingConfiguration);
		}

		private void OnEditorValidationChanged()
		{
			if (_groupEditor.IsValid)
			{
				_okButton.IsEnabled = true;
				_okButton.Tooltip = OK_BUTTON_TOOLTIP;
			}
			else
			{
				_okButton.IsEnabled = false;
				_okButton.Tooltip = _groupEditor.ValidationText;
			}
		}
	}
}
