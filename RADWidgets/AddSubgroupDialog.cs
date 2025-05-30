namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class AddSubgroupDialog : Dialog
	{
		private readonly RadSubgroupEditor _subgroupEditor;
		private readonly Button _okButton;
		private readonly Button _cancelButton;

		public AddSubgroupDialog(IEngine engine, List<RadSubgroupSelectorItem> existingSubgroups, List<string> labels, RadGroupOptions parentOptions,
			string groupNamePlaceHolder) : base(engine)
		{
			Title = "Add Subgroup";
			_subgroupEditor = new RadSubgroupEditor(engine, existingSubgroups, parentOptions, labels, groupNamePlaceHolder);
			_subgroupEditor.ValidationChanged += (sender, args) => OnSubgroupEditorValidationChanged();

			_okButton = new Button("OK")
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (s, e) => Accepted?.Invoke(this, EventArgs.Empty);

			_cancelButton = new Button("Cancel")
			{
				Tooltip = "Do not add the subgroup specified above.",
			};
			_cancelButton.Pressed += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);

			int row = 0;
			AddSection(_subgroupEditor, 0, 0);
			row += _subgroupEditor.RowCount;

			AddWidget(_cancelButton, row, 0);
			AddWidget(_okButton, row, 1, 1, _subgroupEditor.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public RadSubgroupSelectorItem Settings
		{
			get
			{
				return _subgroupEditor.Settings;
			}
		}

		private void OnSubgroupEditorValidationChanged()
		{
			if (_subgroupEditor.IsValid)
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
