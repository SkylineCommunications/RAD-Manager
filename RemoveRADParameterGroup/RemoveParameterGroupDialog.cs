namespace RemoveRADParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection.Emit;
	using RadUtils;
	using RadWidgets;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

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
		public const int NrOfColumns = 6;
		private readonly IEngine _engine;
		private readonly RadHelper _radHelper;
		private readonly ParametersCache _parametersCache;
		private readonly WrappingLabel _label;
		private readonly Button _yesButton;
		private readonly Button _noButton;
		private readonly Button _cancelButton;
		private readonly Button _okButton;
		private List<AGroupRemoveSection> _groupRemoveWidgets;
		private List<RadGroupID> _extraGroupsToRemove;

		public RemoveParameterGroupDialog(IEngine engine, RadHelper radHelper, List<IRadGroupID> groupIDs) : base(engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
			_radHelper = radHelper ?? throw new ArgumentNullException(nameof(radHelper));
			_parametersCache = new EngineParametersCache(engine);
			ShowScriptAbortPopup = false;

			_label = new WrappingLabel(TextWrapWidth);

			_noButton = new Button()
			{
				Text = "No",
			};
			_noButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			_yesButton = new Button()
			{
				Text = "Yes",
				Style = ButtonStyle.CallToAction,
			};
			_yesButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			_cancelButton = new Button()
			{
				Text = "Cancel",
			};
			_cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			_okButton = new Button()
			{
				Text = "OK",
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var parameterGroups = groupIDs.GroupBy(g => new RadGroupID(g.DataMinerID, g.GroupName));
			if (parameterGroups.Count() == 1)
			{
				var g = parameterGroups.First();
				SetWidgetsForSingleGroup(g.Key, g.OfType<RadSubgroupID>());
			}
			else
			{
				SetWidgetsForMultipleGroups(parameterGroups);
			}

			// White spaces to make the indentation and buttons look pretty
			var whitespaces = new List<WhiteSpace>();
			for (int i = 0; i < 2; ++i)
			{
				whitespaces.Add(new WhiteSpace()
				{
					MinWidth = IndentWidth,
				});
			}

			for (int i = 2; i < NrOfColumns - 2; ++i)
			{
				whitespaces.Add(new WhiteSpace());
			}

			int row = 0;
			AddWidget(_label, row, 0, 1, 4);
			row++;

			foreach (var groupWidget in _groupRemoveWidgets)
			{
				AddSection(groupWidget, row, 0);
				row += groupWidget.RowCount;
			}

			for (int i = 0; i < whitespaces.Count; ++i)
				AddWidget(whitespaces[i], row, i);
			row += 1;

			AddWidget(_yesButton, row, NrOfColumns - 2);
			AddWidget(_noButton, row, NrOfColumns - 1);
			row++;

			AddWidget(_cancelButton, row, NrOfColumns - 2);
			AddWidget(_okButton, row, NrOfColumns - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<RadGroupID> GetGroupsToRemove() => _groupRemoveWidgets.Where(g => g.RemoveGroup).Select(g => g.GroupID).Concat(_extraGroupsToRemove).ToList();

		public List<RadSubgroupID> GetSubgroupsToRemove() => _groupRemoveWidgets.SelectMany(g => g.GetSubgroupsToRemove()).ToList();

		private void SetWidgetsForSingleGroup(RadGroupID groupID, IEnumerable<RadSubgroupID> subgroupIDs)
		{
			Title = "Remove Relational Anomaly Group";

			var groupInfo = FetchGroupInfo(groupID);
			_groupRemoveWidgets = new List<AGroupRemoveSection>();
			_extraGroupsToRemove = new List<RadGroupID>();

			if (groupInfo == null || groupInfo.Subgroups == null || groupInfo.Subgroups.Count <= 1 || !subgroupIDs.Any())
			{
				_label.Text = $"Are you sure you want to remove the relational anomaly group '{groupID.GroupName}'?";
				SetYesNoButtonsVisible();
				_extraGroupsToRemove.Add(groupID);
				return;
			}

			var matchingSubgroups = GetMatchingSubgroups(groupInfo, subgroupIDs);
			if (matchingSubgroups.Count == 1)
			{
				var subgroup = matchingSubgroups.First();
				if (string.IsNullOrEmpty(subgroup.Name))
					_label.Text = $"Do you want to remove the whole shared model group '{groupID.GroupName}' or only the subgroup on {subgroup.ParameterDescription} from Relational Anomaly Detection?";
				else
					_label.Text = $"Do you want to remove the whole shared model group '{groupID.GroupName}' or only the subgroup '{subgroup.Name}' from Relational Anomaly Detection?";
			}
			else
			{
				_label.Text = $"Do you want to remove the whole shared model group '{groupID.GroupName}' or only the subgroups below from Relational Anomaly Detection?";
			}

			SetOKCancelButtonsVisible();

			var section = new SharedModelRemoveRadioButtonList(groupID, matchingSubgroups, matchingSubgroups.Count == groupInfo.Subgroups.Count,
				4, TextWrapWidth, TextWrapIndentWidth);
			_groupRemoveWidgets.Add(section);
		}

		private void SetWidgetsForMultipleGroups(IEnumerable<IGrouping<RadGroupID, IRadGroupID>> parameterGroups)
		{
			Title = "Remove Relational Anomaly Groups";
			_label.Text = "Are you sure you want to remove the following relational anomaly groups?";
			SetYesNoButtonsVisible();

			_groupRemoveWidgets = new List<AGroupRemoveSection>();
			_extraGroupsToRemove = new List<RadGroupID>();
			foreach (var group in parameterGroups)
			{
				var groupInfo = FetchGroupInfo(group.Key);
				var subgroupIDs = group.OfType<RadSubgroupID>();
				if (groupInfo?.Subgroups?.Count > 1 && subgroupIDs.Any())
				{
					var matchingSubgroups = GetMatchingSubgroups(groupInfo, subgroupIDs);
					var section = new SharedModelRemoveCheckBox(group.Key, matchingSubgroups, matchingSubgroups.Count == groupInfo.Subgroups.Count, 4, TextWrapWidth, TextWrapIndentWidth);
					_groupRemoveWidgets.Add(section);
				}
				else
				{
					var checkBox = new GroupRemoveCheckBox(group.Key, 4);
					_groupRemoveWidgets.Add(checkBox);
				}
			}
		}

		private void SetYesNoButtonsVisible()
		{
			_yesButton.IsVisible = true;
			_noButton.IsVisible = true;
			_cancelButton.IsVisible = false;
			_okButton.IsVisible = false;
		}

		private void SetOKCancelButtonsVisible()
		{
			_yesButton.IsVisible = false;
			_noButton.IsVisible = false;
			_cancelButton.IsVisible = true;
			_okButton.IsVisible = true;
		}

		private RadGroupInfo FetchGroupInfo(RadGroupID groupID)
		{
			try
			{
				return _radHelper.FetchParameterGroupInfo(groupID.DataMinerID, groupID.GroupName);
			}
			catch (Exception e)
			{
				_engine.Log($"Failed to fetch group info for relational anomaly group {groupID.DataMinerID}/{groupID.GroupName}: {e}");
				return null;
			}
		}

		private List<LiteRadSubgroupInfo> GetMatchingSubgroups(RadGroupInfo groupInfo, IEnumerable<RadSubgroupID> subgroupIDs)
		{
			var subgroupGUIDs = subgroupIDs.Select(id => id.SubgroupID).Where(id => id != null).ToHashSet();
			var subgroupNames = subgroupIDs.Select(id => id.GroupName).Where(n => n != null).ToHashSet();
			var subgroups = groupInfo.Subgroups.Where(s => subgroupGUIDs.Contains(s.ID) || subgroupNames.Contains(s.Name));
			return subgroups.Select(s => new LiteRadSubgroupInfo()
			{
				Name = s.Name,
				ID = s.ID,
				ParameterDescription = string.IsNullOrEmpty(s.Name) ? RadWidgets.Utils.GetParameterDescription(_engine, _parametersCache, s) : string.Empty,
			}).ToList();
		}
	}
}
