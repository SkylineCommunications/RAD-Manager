namespace RemoveRADParameterGroup
{
	using System.Collections.Generic;
	using RadUtils;
	using RadWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	/// <summary>
	/// A widget that allows the user to select whether to remove the entire shared model group, only specific subgroups from the parameter group, or do nothing at all.
	/// </summary>
	public class SharedModelRemoveCheckBox : AGroupRemoveSection
	{
		private readonly CheckBox _checkBox;
		private readonly SharedModelRemoveRadioButtonList _radioButtons;

		/// <summary>
		/// Initializes a new instance of the <see cref="SharedModelRemoveCheckBox"/> class.
		/// </summary>
		/// <param name="groupID">The (parent) group ID.</param>
		/// <param name="subgroupInfos">The subgroups that could be removed.</param>
		/// <param name="hasAllSubgroups">Whether <paramref name="subgroupInfos"/> represents all subgroups of the given shared model group.</param>
		/// <param name="columnSpan">The column span of the resulting widget. Should be 3 or more.</param>
		/// <param name="textWrapWidth">The maximal amount of characters the labels of the checkboxes and radiobuttons should be.</param>
		/// <param name="textWrapIndentWidth">The maximal amount of characters to subtract from <paramref name="textWrapWidth"/> for indented widgets.</param>
		public SharedModelRemoveCheckBox(RadGroupID groupID, List<LiteRadSubgroupInfo> subgroupInfos, bool hasAllSubgroups, int columnSpan, int textWrapWidth,
			int textWrapIndentWidth)
		{
			_checkBox = new CheckBox(groupID.GroupName)
			{
				Tooltip = $"Select to remove the parameter group '{groupID.GroupName}' or some of its subgroups.",
				IsChecked = true,
			};
			_checkBox.Changed += (sender, args) => OnCheckBoxChanged();

			_radioButtons = new SharedModelRemoveRadioButtonList(groupID, subgroupInfos, hasAllSubgroups, columnSpan - 1, textWrapWidth - textWrapIndentWidth, textWrapIndentWidth);

			int row = 0;
			AddWidget(_checkBox, row, 0, 1, columnSpan);
			row++;

			AddSection(_radioButtons, row, 1);
		}

		public override RadGroupID GroupID => _radioButtons.GroupID;

		public override bool RemoveGroup => _checkBox.IsChecked && _radioButtons.RemoveGroup;

		public override List<RadSubgroupID> SubgroupsToRemove => _checkBox.IsChecked ? _radioButtons.SubgroupsToRemove : new List<RadSubgroupID>();

		private void OnCheckBoxChanged()
		{
			_radioButtons.IsEnabled = _checkBox.IsChecked;
		}
	}
}
