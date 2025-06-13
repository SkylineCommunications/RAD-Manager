namespace AddRadParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AddParameterGroup;
	using RadUtils;
	using RadWidgets;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;
	using SLDataGateway.API.Collections.Linq;

	public class ParameterSelectorItemMatchInfo
	{
		public ProtocolParameterSelectorInfo SelectorItem { get; set; }

		public List<ParameterKey> MatchingParameters { get; set; }
	}

	public class GroupByProtocolInfo
	{
		public GroupByProtocolInfo(string elementName, string groupName, List<ParameterSelectorItemMatchInfo> parameters,
			bool groupNameExists)
		{
			ElementName = elementName;
			GroupName = groupName;
			GroupNameExists = groupNameExists;

			ParameterKeys = parameters.SelectMany(p => p.MatchingParameters).ToList();
			MoreThanMinInstances = ParameterKeys.Count >= RadGroupEditor.MIN_PARAMETERS;
			LessThanMaxInstances = ParameterKeys.Count <= RadGroupEditor.MAX_PARAMETERS;
			SelectorItemWithMultipleInstances = parameters.Any(p => p.MatchingParameters.Count > 1);
			SelectorItemWithNoInstances = parameters.Any(p => p.MatchingParameters.Count == 0);
		}

		public string ElementName { get; set; }

		public string GroupName { get; set; }

		public List<ParameterKey> ParameterKeys { get; private set; }

		public bool MoreThanMinInstances { get; private set; }

		public bool LessThanMaxInstances { get; private set; }

		public bool GroupNameExists { get; set; }

		/// <summary>
		/// Gets a value indicating whether there is a single item in the parameter selector that matches multiple instances.
		/// </summary>
		public bool SelectorItemWithMultipleInstances { get; private set; }

		/// <summary>
		/// Gets a value indicating whether there is a single item in the parameter selector that matches no instances.
		/// </summary>
		public bool SelectorItemWithNoInstances { get; private set; }

		public bool ValidStandalone => !GroupNameExists && MoreThanMinInstances && LessThanMaxInstances;

		public bool ValidSubgroup => MoreThanMinInstances && LessThanMaxInstances && !SelectorItemWithMultipleInstances && !SelectorItemWithNoInstances;
	}

	public class RadGroupByProtocolCreator : VisibilitySection
	{
		private const int WordWrapLength = 200;
		private readonly IEngine _engine;
		private readonly ParametersCache _parametersCache;
		private readonly List<string> _existingGroupNames;
		private readonly Label _groupPrefixLabel;
		private readonly TextBox _groupPrefixTextBox;
		private readonly MultiParameterPerProtocolSelector _parameterSelector;
		private readonly CheckBox _sharedModelCheckBox = null;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly MarginLabel _detailsLabel;

		public RadGroupByProtocolCreator(IEngine engine, List<string> existingGroupNames, ParametersCache parametersCache)
		{
			_engine = engine;
			_parametersCache = parametersCache;
			_existingGroupNames = existingGroupNames;

			_groupPrefixLabel = new Label();
			_groupPrefixTextBox = new TextBox()
			{
				MinWidth = 600,
			};
			_groupPrefixTextBox.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();

			_parameterSelector = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			_parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

			if (engine.GetRadHelper().AllowSharedModelGroups)
			{
				_sharedModelCheckBox = new CheckBox("Share model between subgroups")
				{
					Tooltip = "If checked, one shared model group will be created with subgroups for each element. If unchecked, separate groups will be created for " +
					"each element.",
					IsChecked = true,
				};
				_sharedModelCheckBox.Changed += (sender, args) => OnSharedModelCheckBoxChanged();
			}

			_optionsEditor = new RadGroupOptionsEditor(_parameterSelector.ColumnCount);

			_detailsLabel = new MarginLabel(null, _parameterSelector.ColumnCount, 10);

			OnGroupPrefixTextBoxChanged();
			OnSharedModelCheckBoxChanged();

			int row = 0;
			AddWidget(_groupPrefixLabel, row, 0);
			AddWidget(_groupPrefixTextBox, row, 1, 1, _parameterSelector.ColumnCount - 1);
			++row;

			AddSection(_parameterSelector, row, 0);
			row += _parameterSelector.RowCount;

			if (_sharedModelCheckBox != null)
			{
				AddWidget(_sharedModelCheckBox, row, 0, 1, _parameterSelector.ColumnCount);
				row++;
			}

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddSection(_detailsLabel, row, 0, GetDetailsLabelVisibility);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public List<RadGroupSettings> GetGroupsToAdd()
		{
			var groupInfos = GetSelectedGroupInfo();

			if (_sharedModelCheckBox?.IsChecked == true)
			{
				var subgroups = new List<RadSubgroupSettings>(groupInfos.Count);
				foreach (var g in groupInfos)
				{
					if (!g.ValidSubgroup)
						continue;

					var subgroup = new RadSubgroupSettings(g.GroupName, Guid.NewGuid(),
						g.ParameterKeys.Select(p => new RadParameter(p, null)).ToList(), new RadSubgroupOptions());
					subgroups.Add(subgroup);
				}

				var group = new RadGroupSettings(_groupPrefixTextBox.Text, _optionsEditor.Options, subgroups);
				return new List<RadGroupSettings>() { group };
			}
			else
			{
				var groups = new List<RadGroupSettings>(groupInfos.Count);
				foreach (var g in groupInfos)
				{
					if (!g.ValidStandalone)
						continue;

					var subgroup = new RadSubgroupSettings(g.GroupName, Guid.NewGuid(),
						g.ParameterKeys.Select(p => new RadParameter(p, null)).ToList(), new RadSubgroupOptions());
					var group = new RadGroupSettings(g.GroupName, _optionsEditor.Options, new List<RadSubgroupSettings> { subgroup });
					groups.Add(group);
				}

				return groups;
			}
		}

		private void UpdateIsValid()
		{
			if (_groupPrefixTextBox.ValidationState != UIValidationState.Valid)
			{
				IsValid = false;
				ValidationText = "Provide a valid group name prefix";
				ValidationChanged?.Invoke(this, EventArgs.Empty);
				return;
			}

			var groups = GetSelectedGroupInfo();
			UpdateDetailsLabel(groups);

			bool hasValidGroup;
			if (_sharedModelCheckBox?.IsChecked == true)
				hasValidGroup = groups.Any(g => g.ValidSubgroup);
			else
				hasValidGroup = groups.Any(g => g.ValidStandalone);
			IsValid = hasValidGroup;

			if (!IsValid)
				ValidationText = "Make sure at least one valid group will be added";
			else
				ValidationText = string.Empty;

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateGroupPrefixCheckboxValidity()
		{
			if (string.IsNullOrEmpty(_groupPrefixTextBox.Text))
			{
				_groupPrefixTextBox.ValidationState = UIValidationState.Invalid;
				_groupPrefixTextBox.ValidationText = _sharedModelCheckBox?.IsChecked == true ? "Provide a group name" : "Provide a group name prefix";
			}
			else if (_sharedModelCheckBox?.IsChecked == true && _existingGroupNames.Any(s => string.Equals(s, _groupPrefixTextBox.Text, StringComparison.OrdinalIgnoreCase)))
			{
				_groupPrefixTextBox.ValidationState = UIValidationState.Invalid;
				_groupPrefixTextBox.ValidationText = "Group name already exists";
			}
			else
			{
				_groupPrefixTextBox.ValidationState = UIValidationState.Valid;
				_groupPrefixTextBox.ValidationText = string.Empty;
			}
		}

		private List<ParameterSelectorItemMatchInfo> GetSelectedParametersForElement(Element element)
		{
			if (!_parametersCache.TryGet(element.DmaId, element.ElementId, out var parametersOnElement))
			{
				_engine.Log($"Could not find parameters for element {element.ElementName} ({element.DmaId}/{element.ElementId})");
				return new List<ParameterSelectorItemMatchInfo>();
			}

			var items = new List<ParameterSelectorItemMatchInfo>();
			foreach (var parameter in _parameterSelector.GetSelectedParameters())
			{
				List<ParameterKey> matchingKeys;

				var paramInfo = parametersOnElement.FirstOrDefault(p => p.ID == parameter.ParameterID);
				if (paramInfo == null)
				{
					_engine.Log($"Could not find parameter {parameter.ParameterID} on element {element.ElementName} ({element.DmaId}/{element.ElementId})");
					matchingKeys = new List<ParameterKey>();
				}
				else if (!paramInfo.HasTrending())
				{
					matchingKeys = new List<ParameterKey>();
				}
				else if (parameter.ParentTableID == null)
				{
					matchingKeys = new List<ParameterKey>() { new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID) };
				}
				else
				{
					var matchingInstances = RadWidgets.Utils.FetchInstancesWithTrending(_engine, element.DmaId, element.ElementId, paramInfo, parameter.DisplayKeyFilter);
					matchingKeys = matchingInstances.Select(i => new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID, i.IndexValue)).ToList();
				}

				var info = new ParameterSelectorItemMatchInfo()
				{
					SelectorItem = parameter,
					MatchingParameters = matchingKeys,
				};
				items.Add(info);
			}

			return items;
		}

		private List<GroupByProtocolInfo> GetSelectedGroupInfo()
		{
			if (_groupPrefixTextBox.ValidationState != UIValidationState.Valid)
				return new List<GroupByProtocolInfo>();

			var elements = _engine.FindElementsByProtocol(_parameterSelector.ProtocolName, _parameterSelector.ProtocolVersion);
			if (elements == null || elements.Length == 0)
				return new List<GroupByProtocolInfo>();

			var groups = new List<GroupByProtocolInfo>();
			foreach (var element in elements)
			{
				var parameters = GetSelectedParametersForElement(element);
				var groupName = $"{_groupPrefixTextBox.Text} ({element.ElementName})";
				groups.Add(new GroupByProtocolInfo(element.ElementName, groupName, parameters, _existingGroupNames.Contains(groupName)));
			}

			return groups;
		}

		private bool GetDetailsLabelVisibility()
		{
			return _groupPrefixTextBox.ValidationState == UIValidationState.Valid;
		}

		private List<string> GetValidGroupsText(List<GroupByProtocolInfo> groups, bool sharedModelGroup)
		{
			List<string> lines = new List<string>();

			if (groups.Count <= 0)
				return lines;

			if (sharedModelGroup)
				lines.Add($"The following shared model group will be created with {groups.Count} subgroups:");
			else
				lines.Add($"The following groups will be created:");
			lines.AddRange(groups.OrderBy(g => g.GroupName)
				.Take(5)
				.SelectMany(g => $"'{g.GroupName}' with {g.ParameterKeys.Count} instances".WordWrap(WordWrapLength))
				.Select(s => $"\t{s}"));
			if (groups.Count > 5)
				lines.Add($"\t... and {groups.Count - 5} more");

			return lines;
		}

		private List<string> GetGroupsWithInvalidNamesText(List<GroupByProtocolInfo> groups)
		{
			if (groups.Count <= 0)
				return new List<string>();

			return $"Not overwriting existing groups with the same name for {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithTooFewInstancesText(List<GroupByProtocolInfo> groups)
		{
			if (groups.Count <= 0)
				return new List<string>();

			return $"Too few instances have been selected, or instances are not trended for {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithTooManyInstancesText(List<GroupByProtocolInfo> groups)
		{
			if (groups.Count <= 0)
				return new List<string>();

			return $"Too many instances have been selected for {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithMultipleInstancesPerSelectorItemText(List<GroupByProtocolInfo> groups)
		{
			if (groups.Count <= 0)
				return new List<string>();

			return $"Some parameters selected above match multiple instances on {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithNoInstancesPerSelectorItemText(List<GroupByProtocolInfo> groups)
		{
			if (groups.Count <= 0)
				return new List<string>();

			return $"Some parameters selected above match no instances on {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithUnknownInvalidReasonText(List<GroupByProtocolInfo> groups)
		{
			if (groups.Count <= 0)
				return new List<string>();

			return $"Groups on the following elements can not be created due to unknown reasons {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetDetailsLabelTextSharedModelGroup(List<GroupByProtocolInfo> groups)
		{
			var validGroups = groups.Where(g => g.ValidSubgroup).ToList();
			var remainingInvalidGroups = groups.Except(validGroups).ToList();

			var groupsWithTooFewInstances = remainingInvalidGroups.Where(g => !g.MoreThanMinInstances).ToList();
			var groupsWithTooManyInstances = remainingInvalidGroups.Where(g => !g.LessThanMaxInstances).ToList();
			remainingInvalidGroups = remainingInvalidGroups.Except(groupsWithTooFewInstances).Except(groupsWithTooManyInstances).ToList();

			var groupsWithMultipleInstancesPerSelectorItem = remainingInvalidGroups.Where(g => g.SelectorItemWithMultipleInstances).ToList();
			var groupsWithNoInstancesPerSelectorItem = remainingInvalidGroups.Where(g => g.SelectorItemWithNoInstances).ToList();

			List<string> lines = new List<string>();
			lines.AddRange(GetValidGroupsText(validGroups, true));
			lines.AddRange(GetGroupsWithTooFewInstancesText(groupsWithTooFewInstances));
			lines.AddRange(GetGroupsWithTooManyInstancesText(groupsWithTooManyInstances));
			lines.AddRange(GetGroupsWithMultipleInstancesPerSelectorItemText(groupsWithMultipleInstancesPerSelectorItem));
			lines.AddRange(GetGroupsWithNoInstancesPerSelectorItemText(groupsWithNoInstancesPerSelectorItem));
			lines.AddRange(GetGroupsWithUnknownInvalidReasonText(remainingInvalidGroups));

			return lines;
		}

		private List<string> GetDetailsLabelTextSeparateGroups(List<GroupByProtocolInfo> groups)
		{
			var validGroups = groups.Where(g => g.ValidStandalone).ToList();
			var remainingInvalidGroups = groups.Except(validGroups).ToList();

			var groupsWithInvalidName = remainingInvalidGroups.Where(g => g.GroupNameExists).ToList();
			remainingInvalidGroups = remainingInvalidGroups.Except(groupsWithInvalidName).ToList();

			var groupsWithTooFewInstances = remainingInvalidGroups.Where(g => !g.MoreThanMinInstances).ToList();
			var groupsWithTooManyInstances = remainingInvalidGroups.Where(g => !g.LessThanMaxInstances).ToList();
			remainingInvalidGroups = remainingInvalidGroups.Except(groupsWithTooFewInstances).Except(groupsWithTooManyInstances).ToList();

			List<string> lines = new List<string>();
			lines.AddRange(GetValidGroupsText(validGroups, false));
			lines.AddRange(GetGroupsWithInvalidNamesText(groupsWithInvalidName));
			lines.AddRange(GetGroupsWithTooFewInstancesText(groupsWithTooFewInstances));
			lines.AddRange(GetGroupsWithTooManyInstancesText(groupsWithTooManyInstances));
			lines.AddRange(GetGroupsWithUnknownInvalidReasonText(remainingInvalidGroups));

			return lines;
		}

		private void UpdateDetailsLabel(List<GroupByProtocolInfo> groups)
		{
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisibility();
			if (!_detailsLabel.IsVisible)
				return;

			if (groups.Count == 0)
			{
				_detailsLabel.Text = "No elements found on the selected protocol";
				return;
			}

			bool sharedModelGroup = _sharedModelCheckBox?.IsChecked ?? false;
			List<string> lines;
			if (sharedModelGroup)
				lines = GetDetailsLabelTextSharedModelGroup(groups);
			else
				lines = GetDetailsLabelTextSeparateGroups(groups);

			_detailsLabel.Text = string.Join("\n", lines);
		}

		private void OnParameterSelectorChanged()
		{
			UpdateIsValid();
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UpdateGroupPrefixCheckboxValidity();
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisibility();
			UpdateIsValid();
		}

		private void OnSharedModelCheckBoxChanged()
		{
			if (_sharedModelCheckBox?.IsChecked == true)
			{
				_groupPrefixLabel.Text = "Group name";
				var tooltip = "The name of the shared model group. Each subgroup's name will be this name followed by the element name between brackets";
				_groupPrefixLabel.Tooltip = tooltip;
				_groupPrefixTextBox.Tooltip = tooltip;
			}
			else
			{
				_groupPrefixLabel.Text = "Group name prefix";
				var tooltip = "The prefix for the group names. The resulting group name will be the prefix followed by the element name between brackets.";
				_groupPrefixLabel.Tooltip = tooltip;
				_groupPrefixTextBox.Tooltip = tooltip;
			}

			UpdateGroupPrefixCheckboxValidity();
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisibility();
			UpdateIsValid();
		}
	}
}
