namespace AddRadParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using AddRadParameterGroup.GroupByProtocolCreator;
	using RadWidgets;
	using RadWidgets.Widgets.Editors;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public enum AddGroupType
	{
		[Description("Add single group")]
		Single,
		[Description("Add group for each element with given connector")]
		MultipleOnProtocol,
		[Description("Add group with shared model")]
		SharedModel,
	}

	public class AddParameterGroupDialog : Dialog
	{
		private readonly EnumDropDown<AddGroupType> _addTypeDropDown;
		private readonly RadGroupEditor _groupEditor;
		private readonly GroupByProtocolCreatorWidget _groupByProtocolCreator;
		private readonly RadSharedModelGroupEditor _sharedModelGroupEditor;
		private readonly Button _okButton;

		public AddParameterGroupDialog(IEngine engine, RadHelper radHelper) : base(engine)
		{
			ShowScriptAbortPopup = false;
			Title = "Add Relational Anomaly Group";

			var addTypeLabel = new Label("What to add?")
			{
				Tooltip = "Choose whether to add a single group, or multiple groups at once using the specified method.",
			};
			List<AddGroupType> excludedTypes = new List<AddGroupType>();
			if (!RadUtils.Utils.AllowSharedModelGroups(radHelper))
				excludedTypes.Add(AddGroupType.SharedModel);
			_addTypeDropDown = new EnumDropDown<AddGroupType>(excludedTypes)
			{
				Selected = AddGroupType.Single,
			};
			_addTypeDropDown.Changed += (sender, args) => OnAddTypeChanged();

			var existingGroupNames = radHelper.FetchParameterGroups();
			var parametersCache = new EngineParametersCache(engine);
			_groupEditor = new RadGroupEditor(engine, radHelper, existingGroupNames, parametersCache);
			_groupEditor.ValidationChanged += (sender, args) => OnEditorValidationChanged(_groupEditor.IsValid, _groupEditor.ValidationText);

			_groupByProtocolCreator = new GroupByProtocolCreatorWidget(engine, radHelper, existingGroupNames, parametersCache);
			_groupByProtocolCreator.ValidationChanged += (sender, args) => OnEditorValidationChanged(_groupByProtocolCreator.IsValid, _groupByProtocolCreator.ValidationText);

			_sharedModelGroupEditor = new RadSharedModelGroupEditor(engine, radHelper, existingGroupNames, parametersCache);
			_sharedModelGroupEditor.ValidationChanged += (sender, args) => OnEditorValidationChanged(_sharedModelGroupEditor.IsValid, _sharedModelGroupEditor.ValidationText);

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

			AddSection(_sharedModelGroupEditor, row, 0);
			row += _sharedModelGroupEditor.RowCount;

			AddWidget(cancelButton, row, 0, 1, 1);
			AddWidget(_okButton, row, 1, 1, _groupByProtocolCreator.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<RadGroupSettings> GetGroupsToAdd()
		{
			if (_addTypeDropDown.Selected == AddGroupType.Single)
				return new List<RadGroupSettings>() { _groupEditor.Settings };
			else if (_addTypeDropDown.Selected == AddGroupType.MultipleOnProtocol)
				return _groupByProtocolCreator.GetGroupsToAdd();
			else
				return new List<RadGroupSettings>() { _sharedModelGroupEditor.GetSettings() };
		}

		private void OnEditorValidationChanged(bool isValid, string validationText)
		{
			if (isValid)
			{
				_okButton.IsEnabled = true;
				if (_addTypeDropDown.Selected == AddGroupType.Single || _addTypeDropDown.Selected == AddGroupType.SharedModel)
				{
					_okButton.Tooltip = "Add the relational anomaly group specified above to the RAD configuration";
				}
				else
				{
					_okButton.Tooltip = "Add the relational anomaly group(s) specified above to the RAD configuration";
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
				_sharedModelGroupEditor.IsVisible = false;
				_okButton.Text = "Add group";
				_addTypeDropDown.Tooltip = "Add the relational anomaly group specified below.";
				OnEditorValidationChanged(_groupEditor.IsValid, _groupEditor.ValidationText);
			}
			else if (_addTypeDropDown.Selected == AddGroupType.MultipleOnProtocol)
			{
				_groupEditor.IsVisible = false;
				_groupByProtocolCreator.IsVisible = true;
				_sharedModelGroupEditor.IsVisible = false;
				_okButton.Text = "Add group(s)";
				_addTypeDropDown.Tooltip = "Add a relational anomaly group with the instances and options specified below for each element that uses the given connection and connector version.";
				OnEditorValidationChanged(_groupByProtocolCreator.IsValid, _groupByProtocolCreator.ValidationText);
			}
			else
			{
				_groupEditor.IsVisible = false;
				_groupByProtocolCreator.IsVisible = false;
				_sharedModelGroupEditor.IsVisible = true;
				_okButton.Text = "Add group";
				_addTypeDropDown.Tooltip = "Add a relational anomaly group with multiple subgroups that share a single model.";
				OnEditorValidationChanged(_sharedModelGroupEditor.IsValid, _sharedModelGroupEditor.ValidationText);
			}
		}
	}
}
