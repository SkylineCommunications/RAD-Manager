namespace RemoveRADParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using RadWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public enum SharedModelGroupRemoveMode
	{
		EntireGroup,
		SubgroupsOnly,
	}

	/// <summary>
	/// A widget that allows the user to select whether to remove the entire shared model group or only specific subgroups from the parameter group.
	/// </summary>
	public class SharedModelRemoveRadioButtonList : AGroupRemoveSection
	{
		private readonly EnumRadioButtonList<SharedModelGroupRemoveMode> _radioButtonList;
		private readonly List<Tuple<RadSubgroupID, CheckBox>> _subgroupCheckBoxes;
		private readonly bool _hasAllSubgroups;
		private readonly RadGroupID _groupID;
		private readonly RadSubgroupID? _singleSubgroupID;
		private bool _isSelfEnabled = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="SharedModelRemoveRadioButtonList"/> class.
		/// </summary>
		/// <param name="groupID">The (parent) group ID.</param>
		/// <param name="subgroupInfos">The subgroups that could be removed.</param>
		/// <param name="hasAllSubgroups">Whether <paramref name="subgroupInfos"/> represents all subgroups of the given shared model group.</param>
		/// <param name="columnSpan">The column span of the resulting widget. Should be 2 or more.</param>
		/// <param name="textWrapWidth">The maximal amount of characters the labels of the checkboxes and radiobuttons should be.</param>
		/// <param name="textWrapIndentWidth">The maximal amount of characters to subtract from <paramref name="textWrapWidth"/> for indented widgets.</param>
		public SharedModelRemoveRadioButtonList(RadGroupID groupID, List<LiteRadSubgroupInfo> subgroupInfos, bool hasAllSubgroups, int columnSpan, int textWrapWidth,
			int textWrapIndentWidth)
		{
			_groupID = groupID;
			_hasAllSubgroups = hasAllSubgroups;

			_radioButtonList = new EnumRadioButtonList<SharedModelGroupRemoveMode>(v => GetRadioButtonText(v, subgroupInfos, textWrapWidth));
			_radioButtonList.Selected = SharedModelGroupRemoveMode.SubgroupsOnly;
			_radioButtonList.Changed += (sender, args) => OnRadioButtonListChanged();

			_subgroupCheckBoxes = new List<Tuple<RadSubgroupID, CheckBox>>();
			_singleSubgroupID = null;
			if (subgroupInfos.Count != 1)
			{
				foreach (var subgroup in subgroupInfos)
				{
					string checkBoxText;
					string tooltip;
					if (string.IsNullOrEmpty(subgroup.Name))
					{
						checkBoxText = $"Unnamed subgroup on {Utils.Shorten(subgroup.ParameterDescription, textWrapWidth - textWrapIndentWidth - 20)}";
						tooltip = $"Select to remove this subgroup from the parameter group {groupID.GroupName}.";
					}
					else
					{
						checkBoxText = subgroup.Name;
						tooltip = $"Select to remove the subgroup {subgroup.Name} from the parameter group {groupID.GroupName}.";
					}

					var checkBox = new CheckBox(checkBoxText)
					{
						Tooltip = tooltip,
						IsChecked = true,
					};
					_subgroupCheckBoxes.Add(Tuple.Create(new RadSubgroupID(groupID.DataMinerID, groupID.GroupName, subgroup.ID), checkBox));
				}
			}
			else
			{
				_singleSubgroupID = new RadSubgroupID(groupID.DataMinerID, groupID.GroupName, subgroupInfos.First().ID);
			}

			OnRadioButtonListChanged();

			int row = 0;
			AddWidget(_radioButtonList, row, 0, 1, columnSpan);
			row++;

			foreach (var s in _subgroupCheckBoxes)
			{
				AddWidget(s.Item2, row, 1, 1, columnSpan - 1);
				row++;
			}
		}

		public override RadGroupID GroupID => _groupID;

		public override bool RemoveGroup => _radioButtonList.Selected == SharedModelGroupRemoveMode.EntireGroup || (_hasAllSubgroups && _subgroupCheckBoxes.All(c => c.Item2.IsChecked));

		public override List<RadSubgroupID> SubgroupsToRemove
		{
			get
			{
				if (!RemoveGroup)
				{
					if (_singleSubgroupID != null)
						return new List<RadSubgroupID> { _singleSubgroupID.Value };
					else
						return _subgroupCheckBoxes.Where(c => c.Item2.IsChecked).Select(c => c.Item1).ToList();
				}
				else
				{
					return new List<RadSubgroupID>();
				}
			}
		}

		public new bool IsEnabled
		{
			get => _isSelfEnabled;
			set
			{
				if (_isSelfEnabled == value)
					return;

				_isSelfEnabled = value;

				_radioButtonList.IsEnabled = value;
				UpdateSubgroupCheckBoxesIsEnabled();
			}
		}

		private static string GetRadioButtonText(SharedModelGroupRemoveMode mode, List<LiteRadSubgroupInfo> subgroupInfos, int textWrapWidth)
		{
			switch (mode)
			{
				case SharedModelGroupRemoveMode.EntireGroup:
					return "Remove the entire group";
				case SharedModelGroupRemoveMode.SubgroupsOnly:
					if (subgroupInfos.Count == 1)
					{
						var subgroup = subgroupInfos.First();
						if (string.IsNullOrEmpty(subgroup.Name))
							return $"Remove only the subgroup on {Utils.Shorten(subgroup.ParameterDescription, textWrapWidth - 28)}";
						else
							return $"Remove only the subgroup '{subgroup.Name}'";
					}
					else
					{
						return "Remove only the following subgroups";
					}

				default:
					return "Unknown option";
			}
		}

		private void UpdateSubgroupCheckBoxesIsEnabled()
		{
			foreach (var c in _subgroupCheckBoxes)
				c.Item2.IsEnabled = _isSelfEnabled && _radioButtonList.Selected == SharedModelGroupRemoveMode.SubgroupsOnly;
		}

		private void OnRadioButtonListChanged()
		{
			UpdateSubgroupCheckBoxesIsEnabled();
		}
	}
}
