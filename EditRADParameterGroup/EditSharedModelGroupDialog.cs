﻿namespace EditRADParameterGroup
{
	using System;
	using System.Linq;
	using RadWidgets;
	using RadWidgets.Widgets.Editors;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class EditSharedModelGroupDialog : Dialog
	{
		private readonly RadSharedModelGroupEditor _sharedGroupEditor;
		private readonly Button _okButton;

		public EditSharedModelGroupDialog(IEngine engine, RadHelper radHelper, RadGroupInfo groupSettings, Guid? selectedSubgroup, int dataMinerID) : base(engine)
		{
			ShowScriptAbortPopup = false;
			DataMinerID = dataMinerID;
			Title = $"Edit Shared Model Group '{groupSettings.GroupName}'";

			var groupNames = radHelper.FetchParameterGroups();
			var parametersCache = new EngineParametersCache(engine);
			_sharedGroupEditor = new RadSharedModelGroupEditor(engine, radHelper, groupNames, parametersCache, groupSettings, selectedSubgroup);
			_sharedGroupEditor.ValidationChanged += (sender, args) => OnGroupEditorValidationChanged();

			_okButton = new Button("Apply")
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnGroupEditorValidationChanged();

			int row = 0;
			AddSection(_sharedGroupEditor, row, 0);
			row += _sharedGroupEditor.RowCount;

			AddWidget(cancelButton, row, 0, 1, 1);
			AddWidget(_okButton, row, 1, 1, 3);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public int DataMinerID { get; private set; }

		public RadGroupSettings GetGroupSettings() => _sharedGroupEditor.GetSettings();

		private void OnGroupEditorValidationChanged()
		{
			if (_sharedGroupEditor.IsValid)
			{
				_okButton.IsEnabled = true;
				_okButton.Tooltip = "Edit the selected shared model group as specified above";
			}
			else
			{
				_okButton.IsEnabled = false;
				_okButton.Tooltip = _sharedGroupEditor.ValidationText;
			}
		}
	}
}
