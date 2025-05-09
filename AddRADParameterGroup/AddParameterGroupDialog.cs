﻿namespace AddParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using AddRadParameterGroup;
	using RadWidgets;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public enum AddGroupType
	{
		[Description("Add single group")]
		Single,
		[Description("Add group for each element with given connector")]
		MultipleOnProtocol,
	}

	public class AddParameterGroupDialog : Dialog
	{
		private readonly EnumDropDown<AddGroupType> _addTypeDropDown;
		private readonly RadGroupEditor _groupEditor;
		private readonly RadGroupByProtocolCreator _groupByProtocolCreator;
		private readonly Button _okButton;

		public AddParameterGroupDialog(IEngine engine) : base(engine)
		{
			ShowScriptAbortPopup = false;
			Title = "Add Parameter Group";

			var addTypeLabel = new Label("What to add?")
			{
				Tooltip = "Choose whether to add a single group, or multiple groups at once using the specified method.",
			};
			_addTypeDropDown = new EnumDropDown<AddGroupType>()
			{
				Selected = AddGroupType.Single,
			};
			_addTypeDropDown.Changed += (sender, args) => OnAddTypeChanged();

			var existingGroupNames = Utils.FetchRadGroupNames(engine);
			_groupEditor = new RadGroupEditor(engine, existingGroupNames);
			_groupEditor.ValidationChanged += (sender, args) => OnEditorValidationChanged(_groupEditor.IsValid, _groupEditor.ValidationText);

			_groupByProtocolCreator = new RadGroupByProtocolCreator(engine, existingGroupNames);
			_groupByProtocolCreator.ValidationChanged += (sender, args) => OnEditorValidationChanged(_groupByProtocolCreator.IsValid, _groupByProtocolCreator.ValidationText);

			_okButton = new Button()
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnAddTypeChanged();

			int row = 0;
			AddWidget(addTypeLabel, row, 0);
			AddWidget(_addTypeDropDown, row, 1, 1, _groupByProtocolCreator.ColumnCount - 1);
			++row;

			AddSection(_groupEditor, row, 0);
			row += _groupEditor.RowCount;

			AddSection(_groupByProtocolCreator, row, 0);
			row += _groupByProtocolCreator.RowCount;

			AddWidget(cancelButton, row, 0, 1, 1);
			AddWidget(_okButton, row, 1, 1, _groupByProtocolCreator.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<MADGroupInfo> GetGroupsToAdd()
		{
			if (_addTypeDropDown.Selected == AddGroupType.Single)
			{
				var groupInfo = new MADGroupInfo(
					_groupEditor.Settings.GroupName,
					_groupEditor.Settings.Parameters.ToList(),
					_groupEditor.Settings.Options.UpdateModel,
					_groupEditor.Settings.Options.AnomalyThreshold,
					_groupEditor.Settings.Options.MinimalDuration);
				return new List<MADGroupInfo>() { groupInfo };
			}
			else
			{
				return _groupByProtocolCreator.GetGroupsToAdd();
			}
		}

		private void OnEditorValidationChanged(bool isValid, string validationText)
		{
			if (isValid)
			{
				_okButton.IsEnabled = true;
				if (_addTypeDropDown.Selected == AddGroupType.Single)
				{
					_okButton.Tooltip = "Add the parameter group specified above to the RAD configuration";
				}
				else
				{
					_okButton.Tooltip = "Add the parameter group(s) specified above to the RAD configuration";
				}
			}
			else
			{
				_okButton.IsEnabled = false;
				_okButton.Tooltip = validationText;
			}
		}

		private void OnAddTypeChanged()
		{
			if (_addTypeDropDown.Selected == AddGroupType.Single)
			{
				_groupEditor.IsVisible = true;
				_groupByProtocolCreator.IsVisible = false;
				_okButton.Text = "Add group";
				_addTypeDropDown.Tooltip = "Add the parameter group specified below.";
				OnEditorValidationChanged(_groupEditor.IsValid, _groupEditor.ValidationText);
			}
			else
			{
				_groupEditor.IsVisible = false;
				_groupByProtocolCreator.IsVisible = true;
				_okButton.Text = "Add group(s)";
				_addTypeDropDown.Tooltip = "Add a parameter group with the instances and options specified below for each element that uses the given connection and connector version.";
				OnEditorValidationChanged(_groupByProtocolCreator.IsValid, _groupByProtocolCreator.ValidationText);
			}
		}
	}
}
