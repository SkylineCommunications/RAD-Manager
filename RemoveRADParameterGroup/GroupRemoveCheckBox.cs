namespace RemoveRADParameterGroup
{
	using System.Collections.Generic;
	using RadWidgets;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class GroupRemoveCheckBox : AGroupRemoveSection
	{
		private readonly CheckBox _checkBox;
		private readonly RadGroupID _groupID;

		public GroupRemoveCheckBox(RadGroupID groupID, int columnSpan)
		{
			_checkBox = new CheckBox(groupID.GroupName)
			{
				Tooltip = $"Select to remove the parameter group '{groupID.GroupName}' from Relational Anomaly Detection.",
				IsChecked = true,
			};
			_groupID = groupID;

			AddWidget(_checkBox, 0, 0, 1, columnSpan);
		}

		public override RadGroupID GroupID
		{
			get => _groupID;
		}

		public override bool RemoveGroup => _checkBox.IsChecked;

		public override List<RadSubgroupID> GetSubgroupsToRemove() => new List<RadSubgroupID>();
	}
}
