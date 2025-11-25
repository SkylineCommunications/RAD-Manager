namespace RadWidgets.Widgets.Dialogs
{
	using System;
	using System.Collections.Generic;
	using RadWidgets.Widgets;
	using RadWidgets.Widgets.Editors;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class AddSubgroupDialog : Dialog
	{
		private readonly RadSubgroupEditor _subgroupEditor;
		private readonly Button _okButton;

		public AddSubgroupDialog(IEngine engine, RadHelper radHelper, List<RadSubgroupSelectorItem> existingSubgroups, List<string> labels,
			RadGroupOptions parentOptions,
			string groupNamePlaceHolder) : base(engine)
		{
			Title = "Add Subgroup";
			_subgroupEditor = new RadSubgroupEditor(engine, radHelper, existingSubgroups, parentOptions, labels, groupNamePlaceHolder);
			_subgroupEditor.ValidationChanged += (sender, args) => OnSubgroupEditorValidationChanged();

			_okButton = new Button("OK")
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (s, e) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel")
			{
				Tooltip = "Do not add the subgroup specified above.",
			};
			cancelButton.Pressed += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);

			int row = 0;
			AddSection(_subgroupEditor, 0, 0);
			row += _subgroupEditor.RowCount;

			AddWidget(cancelButton, row, _subgroupEditor.ColumnCount - 2);
			AddWidget(_okButton, row, _subgroupEditor.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public RadSubgroupSelectorItem GetSettings()
		{
			return _subgroupEditor.GetSettings();
		}

		private void OnSubgroupEditorValidationChanged()
		{
			_okButton.IsEnabled = _subgroupEditor.IsNameValid;
			if (!_subgroupEditor.IsNameValid)
			{
				_okButton.Tooltip = "Provide a valid group name";
			}
			else if (_subgroupEditor.IsValid)
			{
				_okButton.Tooltip = "Create a subgroup with the specified settings.";
			}
			else
			{
				_okButton.Tooltip = "Create a subgroup with the specified settings. Note that some fields above are invalid and will have to be fixed " +
					$"before submitting the shared model group: {_subgroupEditor.ValidationText}.";
			}
		}
	}
}
