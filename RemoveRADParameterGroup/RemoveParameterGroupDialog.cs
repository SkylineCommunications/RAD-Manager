namespace RemoveRADParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using RadWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class LiteRadSubgroupInfo
	{
		public string Name { get; set; }

		public Guid ID { get; set; }

		public string ParameterDescription { get; set; }
	}

	public class RemoveParameterGroupDialog : Dialog
    {
		public const int IndentWidth = 10;
		public const int TextWrapWidth = 150;
		public const int TextWrapIndentWidth = 5;
		private readonly List<AGroupRemoveSection> _groupRemoveWidgets;
		private readonly List<RadGroupID> _extraGroupsToRemove;

		public RemoveParameterGroupDialog(IEngine engine, List<IRadGroupID> groupIDs) : base(engine)
		{
			ShowScriptAbortPopup = false;

			var label = new WrappingLabel(TextWrapWidth);

			var rejectButton = new Button()
			{
				MinWidth = 300,
			};
			rejectButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			var acceptButton = new Button()
			{
				Style = ButtonStyle.CallToAction,
				MinWidth = 300,
			};
			acceptButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			_groupRemoveWidgets = new List<AGroupRemoveSection>();
			_extraGroupsToRemove = new List<RadGroupID>();
			var parameterGroups = groupIDs.GroupBy(g => new RadGroupID(g.DataMinerID, g.GroupName));
			var parametersCache = new EngineParametersCache(engine);
			if (parameterGroups.Count() == 1)
			{
				Title = "Remove parameter group";
				var groupID = parameterGroups.First().Key;
				var groupInfo = FetchGroupInfo(engine, groupID);
				if (groupInfo is RadGroupInfo parameterGroupInfo)
				{
					label.Text = $"Are you sure you want to remove the parameter group '{groupID.GroupName}' from Relational Anomaly Detection?";
					acceptButton.Text = "Yes";
					rejectButton.Text = "No";
					_extraGroupsToRemove.Add(groupID);
				}
				else if (groupInfo is RadSharedModelGroupInfo sharedModelGroupInfo)
				{
					var subgroups = GetMatchingSubgroups(engine, parametersCache, sharedModelGroupInfo, groupIDs.OfType<RadSubgroupID>());

					if (subgroups.Count == 1)
					{
						var subgroup = subgroups.First();
						if (string.IsNullOrEmpty(subgroup.Name))
							label.Text = $"Do you want to remove the whole shared model group '{groupID.GroupName}' or only the subgroup on {subgroup.ParameterDescription} from Relational Anomaly Detection?";
						else
							label.Text = $"Do you want to remove the whole shared model group '{groupID.GroupName}' or only the subgroup '{subgroup.Name}' from Relational Anomaly Detection?";
					}
					else
					{
						label.Text = $"Do you want to remove the whole shared model group '{groupID.GroupName}' or only the subgroups below from Relational Anomaly Detection?";
					}

					acceptButton.Text = "OK";
					rejectButton.Text = "Cancel";

					var section = new SharedModelRemoveRadioButtonList(groupID, subgroups, subgroups.Count == sharedModelGroupInfo.Subgroups.Count, 4, TextWrapWidth, TextWrapIndentWidth);
					_groupRemoveWidgets.Add(section);
				}
				else
				{
					engine.Log($"Failed to fetch group info for group {parameterGroups.First().Key.DataMinerID}/{parameterGroups.First().Key.GroupName}: no response received or response of unexpected type received.");
					throw new Exception($"Failed to fetch group info");
				}
			}
			else
			{
				Title = "Remove parameter groups";
				label.Text = "Are you sure you want to remove the following parameter groups from Relational Anomaly Detection?";
				acceptButton.Text = "Yes";
				rejectButton.Text = "No";

				foreach (var group in parameterGroups)
				{
					var groupInfo = FetchGroupInfo(engine, group.Key);
					if (groupInfo is RadGroupInfo parameterGroupInfo)
					{
						var checkBox = new GroupRemoveCheckBox(group.Key, 4);
						_groupRemoveWidgets.Add(checkBox);
					}
					else if (groupInfo is RadSharedModelGroupInfo sharedModelGroupInfo)
					{
						var subgroups = GetMatchingSubgroups(engine, parametersCache, sharedModelGroupInfo, group.OfType<RadSubgroupID>());
						var section = new SharedModelRemoveCheckBox(group.Key, subgroups, subgroups.Count == sharedModelGroupInfo.Subgroups.Count, 4, TextWrapWidth, TextWrapIndentWidth);
						_groupRemoveWidgets.Add(section);
					}
					else
					{
						engine.Log($"Failed to fetch group info for group {group.Key.DataMinerID}/{group.Key.GroupName}: no response received or response of unexpected type received.");
					}
				}
			}

			// White spaces to make the indentation look pretty
			var whitespace1 = new WhiteSpace()
			{
				MinWidth = IndentWidth,
			};
			var whitespace2 = new WhiteSpace()
			{
				MinWidth = IndentWidth,
			};
			var whitespace3 = new WhiteSpace()
			{
				MinWidth = rejectButton.MinWidth - (2 * IndentWidth),
			};

			int row = 0;
			AddWidget(label, row, 0, 1, 4);
			row++;

			foreach (var groupWidget in _groupRemoveWidgets)
			{
				AddSection(groupWidget, row, 0);
				row += groupWidget.RowCount;
			}

			AddWidget(whitespace1, row, 0);
			AddWidget(whitespace2, row, 1);
			AddWidget(whitespace3, row, 2);
			row += 1;

			AddWidget(rejectButton, row, 0, 1, 3);
			AddWidget(acceptButton, row, 3);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<RadGroupID> GroupsToRemove => _groupRemoveWidgets.Where(g => g.RemoveGroup).Select(g => g.GroupID).Concat(_extraGroupsToRemove).ToList();

		public List<RadSubgroupID> SubgroupsToRemove => _groupRemoveWidgets.SelectMany(g => g.SubgroupsToRemove).ToList();

		private static IRadGroupBaseInfo FetchGroupInfo(IEngine engine, RadGroupID groupID)
		{
			try
			{
				return RadMessageHelper.FetchParameterGroupInfo(engine, groupID.DataMinerID, groupID.GroupName);
			}
			catch (Exception e)
			{
				engine.Log($"Failed to fetch group info for parameter group {groupID.DataMinerID}/{groupID.GroupName}: {e}");
				return null;
			}
		}

		private static List<LiteRadSubgroupInfo> GetMatchingSubgroups(IEngine engine, ParametersCache parametersCache, RadSharedModelGroupInfo groupInfo,
			IEnumerable<RadSubgroupID> subgroupIDs)
		{
			var subgroupGUIDs = subgroupIDs.Select(id => id.SubgroupID).Where(id => id != null).ToHashSet();
			var subgroupNames = subgroupIDs.Select(id => id.GroupName).Where(n => n != null).ToHashSet();
			var subgroups = groupInfo.Subgroups.Where(s => subgroupGUIDs.Contains(s.ID) || subgroupNames.Contains(s.Name));
			return subgroups.Select(s => new LiteRadSubgroupInfo()
			{
				Name = s.Name,
				ID = s.ID,
				ParameterDescription = string.IsNullOrEmpty(s.Name) ? RadWidgets.Utils.GetParameterDescription(engine, parametersCache, s) : string.Empty,
			}).ToList();
		}
	}
}
