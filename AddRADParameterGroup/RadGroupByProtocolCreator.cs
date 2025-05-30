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
		private readonly IEngine _engine;
		private readonly ParametersCache _parametersCache;
		private readonly List<string> _existingGroupNames;
		private readonly Label _groupPrefixLabel;
		private readonly TextBox _groupPrefixTextBox;
		private readonly MultiParameterPerProtocolSelector _parameterSelector;
		private readonly CheckBox _sharedModelCheckBox = null;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly Label _detailsLabel;

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

			if (Compatibility.HasSharedModelGroups(engine))
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

			_detailsLabel = new Label()
			{
				MaxWidth = 900,
			};

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

			AddWidget(_detailsLabel, row, 0, 1, _parameterSelector.ColumnCount, HorizontalAlignment.Stretch);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise it will make the details label always visible
			get => IsSectionVisible;
			set
			{
				if (IsSectionVisible == value)
					return;

				IsSectionVisible = value;

				_groupPrefixLabel.IsVisible = value;
				_groupPrefixTextBox.IsVisible = value;
				_parameterSelector.IsVisible = value;
				if (_sharedModelCheckBox != null)
					_sharedModelCheckBox.IsVisible = value;
				_optionsEditor.IsVisible = value;
				UpdateDetailsLabelVisibility();
			}
		}

		public List<RadGroupBaseSettings> GetGroupsToAdd()
		{
			var groupInfos = GetSelectedGroupInfo();

			if (_sharedModelCheckBox?.IsChecked == true)
			{
				var subgroups = new List<RadSubgroupSettings>(groupInfos.Count);
				foreach (var g in groupInfos)
				{
					if (!g.ValidSubgroup)
						continue;

					var subgroup = new RadSubgroupSettings()
					{
						Name = g.GroupName,
						ID = Guid.NewGuid(),
						Parameters = g.ParameterKeys.Select(p => new RadParameter() { Key = p, Label = null }).ToList(),
						Options = new RadSubgroupOptions(),
					};
					subgroups.Add(subgroup);
				}

				var group = new RadSharedModelGroupSettings()
				{
					GroupName = _groupPrefixTextBox.Text,
					Subgroups = subgroups,
					Options = _optionsEditor.Options,
				};
				return new List<RadGroupBaseSettings>() { group };
			}
			else
			{
				var groups = new List<RadGroupBaseSettings>(groupInfos.Count);
				foreach (var g in groupInfos)
				{
					if (!g.ValidStandalone)
						continue;

					var group = new RadGroupSettings()
					{
						GroupName = g.GroupName,
						Parameters = g.ParameterKeys,
						Options = _optionsEditor.Options,
					};
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

		private void UpdateDetailsLabelVisibility()
		{
			_detailsLabel.IsVisible = IsSectionVisible && _groupPrefixTextBox.ValidationState == UIValidationState.Valid;
		}

		private void UpdateDetailsLabel(List<GroupByProtocolInfo> groups)
		{
			UpdateDetailsLabelVisibility();
			if (!_detailsLabel.IsVisible)
				return;

			if (groups.Count == 0)
			{
				_detailsLabel.Text = "No elements found on the selected protocol";
				return;
			}

			bool sharedModelGroup = _sharedModelCheckBox?.IsChecked ?? false;
			List<GroupByProtocolInfo> validGroups;
			if (sharedModelGroup)
				validGroups = groups.Where(g => g.ValidSubgroup).ToList();
			else
				validGroups = groups.Where(g => g.ValidStandalone).ToList();

			List<GroupByProtocolInfo> remainingInvalidGroups = groups.Except(validGroups).ToList();

			List<GroupByProtocolInfo> groupsWithInvalidName;
			if (sharedModelGroup)
				groupsWithInvalidName = new List<GroupByProtocolInfo>();
			else
				groupsWithInvalidName = remainingInvalidGroups.Where(g => g.GroupNameExists).ToList();
			remainingInvalidGroups = remainingInvalidGroups.Except(groupsWithInvalidName).ToList();

			List<GroupByProtocolInfo> groupsWithTooFewInstances = remainingInvalidGroups.Where(g => !g.MoreThanMinInstances).ToList();
			List<GroupByProtocolInfo> groupsWithTooManyInstances = remainingInvalidGroups.Where(g => !g.LessThanMaxInstances).ToList();
			remainingInvalidGroups = remainingInvalidGroups.Except(groupsWithTooFewInstances).Except(groupsWithTooManyInstances).ToList();

			List<GroupByProtocolInfo> groupsWithMultipleInstancesPerSelectorItem;
			List<GroupByProtocolInfo> groupsWithNoInstancesPerSelectorItem;
			if (sharedModelGroup)
			{
				groupsWithMultipleInstancesPerSelectorItem = remainingInvalidGroups.Where(g => g.SelectorItemWithMultipleInstances).ToList();
				groupsWithNoInstancesPerSelectorItem = remainingInvalidGroups.Where(g => g.SelectorItemWithNoInstances).ToList();
			}
			else
			{
				groupsWithMultipleInstancesPerSelectorItem = new List<GroupByProtocolInfo>();
				groupsWithNoInstancesPerSelectorItem = new List<GroupByProtocolInfo>();
			}

			remainingInvalidGroups = remainingInvalidGroups.Except(groupsWithMultipleInstancesPerSelectorItem).Except(groupsWithNoInstancesPerSelectorItem).ToList();

			List<string> lines = new List<string>();
			if (validGroups.Count > 0)
			{
				if (sharedModelGroup)
					lines.Add($"The following shared model group will be created with {validGroups.Count} subgroups:");
				else
					lines.Add($"The following groups will be created:");
				lines.AddRange(validGroups.OrderBy(g => g.GroupName).Select(g => $"\t'{g.GroupName}' with {g.ParameterKeys.Count} instances").Take(5));
				if (validGroups.Count > 5)
					lines.Add($"\t... and {validGroups.Count - 5} more");
			}

			if (groupsWithInvalidName.Count > 0)
				lines.Add($"Not overwriting existing groups with the same name for {groupsWithInvalidName.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			if (groupsWithTooFewInstances.Count > 0)
				lines.Add($"Too few instances have been selected, or instances are not trended for {groupsWithTooFewInstances.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			if (groupsWithTooManyInstances.Count > 0)
				lines.Add($"Too many instances have been selected for {groupsWithTooManyInstances.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			if (groupsWithMultipleInstancesPerSelectorItem.Count > 0)
				lines.Add($"Some parameters selected above match multiple instances on {groupsWithMultipleInstancesPerSelectorItem.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			if (groupsWithNoInstancesPerSelectorItem.Count > 0)
				lines.Add($"Some parameters selected above match no instances on {groupsWithNoInstancesPerSelectorItem.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			if (remainingInvalidGroups.Count > 0)
				lines.Add($"Groups on the following elements can not be created due to unknown reasons {remainingInvalidGroups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");

			_detailsLabel.Text = string.Join("\n", lines);
		}

		private void OnParameterSelectorChanged()
		{
			UpdateIsValid();
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UpdateGroupPrefixCheckboxValidity();
			UpdateDetailsLabelVisibility();
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
			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}
	}
}
