namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class GroupNameSection : VisibilitySection
	{
		private readonly TextBox _groupNameTextBox;
		private readonly List<string> _existingGroupNames;

		public GroupNameSection(string initialName, List<string> existingGroupNames, int textBoxColumnSpan, string groupNamePlaceHolder = null)
		{
			_existingGroupNames = existingGroupNames;
			if (!string.IsNullOrEmpty(initialName)) // The current group name should be accepted as valid
				_existingGroupNames.Remove(initialName);

			var groupNameTooltip = "Provide the name of the group. This name will be used when creating suggestion events for anomalies detected on this group.";
			var groupNameLabel = new Label("Group name")
			{
				Tooltip = groupNameTooltip,
			};
			_groupNameTextBox = new TextBox()
			{
				Text = initialName,
				MinWidth = 600,
				Tooltip = groupNameTooltip,
				PlaceHolder = groupNamePlaceHolder,
			};
			_groupNameTextBox.Changed += (sender, args) => OnGroupNameTextBoxChanged();
			OnGroupNameTextBoxChanged();

			AddWidget(groupNameLabel, 0, 0);
			AddWidget(_groupNameTextBox, 0, 1, 1, textBoxColumnSpan);
		}

		public event EventHandler ValidationChanged;

		public string GroupName => _groupNameTextBox.Text;

		public string GroupNamePlaceHolder => _groupNameTextBox.PlaceHolder;

		public bool IsValid => _groupNameTextBox.ValidationState == UIValidationState.Valid;

		public string ValidationText => _groupNameTextBox.ValidationText;

		private void OnGroupNameTextBoxChanged()
		{
			if (string.IsNullOrEmpty(_groupNameTextBox.Text))
			{
				if (string.IsNullOrEmpty(_groupNameTextBox.PlaceHolder))
				{
					_groupNameTextBox.ValidationState = UIValidationState.Invalid;
					_groupNameTextBox.ValidationText = "Provide a group name";
				}
				else
				{
					_groupNameTextBox.ValidationState = UIValidationState.Valid;
					_groupNameTextBox.ValidationText = string.Empty;
				}
			}
			else if (_existingGroupNames.Any(s => string.Equals(_groupNameTextBox.Text, s, StringComparison.OrdinalIgnoreCase)))
			{
				_groupNameTextBox.ValidationState = UIValidationState.Invalid;
				_groupNameTextBox.ValidationText = "Group name already exists";
			}
			else
			{
				_groupNameTextBox.ValidationState = UIValidationState.Valid;
				_groupNameTextBox.ValidationText = string.Empty;
			}

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
