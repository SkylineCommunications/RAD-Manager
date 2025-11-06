namespace AddRadSharedModelGroup
{
	using System;
	using RadWidgets;
	using RadWidgets.Widgets;
	using RadWidgets.Widgets.Editors;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class AddSharedModelGroupDialog : Dialog
	{
		private const string OK_BUTTON_TOOLTIP = "Add a relational anomaly group with multiple subgroups that share a single model.";
		private readonly RadSharedModelGroupEditor _groupEditor;
		private readonly Button _okButton;

		public AddSharedModelGroupDialog(IEngine engine, RadHelper radHelper) : base(engine)
		{
			ShowScriptAbortPopup = false;
			Title = "Add Relational Anomaly Group With Shared Model";

			var existingGroupNames = radHelper.FetchParameterGroups();
			var parametersCache = new EngineParametersCache(engine);
			_groupEditor = new RadSharedModelGroupEditor(engine, radHelper, existingGroupNames, parametersCache);
			_groupEditor.ValidationChanged += (sender, args) => OnEditorValidationChanged();

			_okButton = new Button("Add group")
			{
				Style = ButtonStyle.CallToAction,
				Tooltip = OK_BUTTON_TOOLTIP,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnEditorValidationChanged();

			int row = 0;
			AddSection(_groupEditor, row, 0);
			row += _groupEditor.RowCount;

			AddWidget(cancelButton, row, 0, 1, 1);
			AddWidget(_okButton, row, 1, 1, _groupEditor.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public TrainingConfiguration TrainingConfiguration => _groupEditor.TrainingConfiguration;

		public RadGroupSettings GetSettings() => _groupEditor.GetSettings(out var _, out var _);

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
