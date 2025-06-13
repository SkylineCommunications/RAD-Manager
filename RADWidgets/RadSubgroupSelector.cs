namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AutomationUI.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;


	public class RadSubgroupSelectorParameter
	{
		public ParameterKey Key { get; set; }

		public string ElementName { get; set; }

		public string ParameterName { get; set; }

		public RadParameter ToRadParameter(string label = null)
		{
			return new RadParameter(Key, label);
		}

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Key?.DisplayInstance))
				return $"{ElementName}/{ParameterName}/{Key.DisplayInstance}";
			else if (!string.IsNullOrEmpty(Key?.Instance))
				return $"{ElementName}/{ParameterName}/{Key.Instance}";
			else
				return $"{ElementName}/{ParameterName}";
		}
	}

	public class RadSubgroupSelectorItem
	{
		public RadSubgroupSelectorItem(Guid id, string name, RadSubgroupOptions options, List<RadSubgroupSelectorParameter> parameters,
			string displayValue)
		{
			ID = id;
			Name = name;
			Options = options;
			Parameters = parameters;
			DisplayValue = displayValue;
		}

		public Guid ID { get; private set; }

		public string Name { get; private set; }

		public string DisplayValue { get; private set; }

		public RadSubgroupOptions Options { get; private set; }

		public List<RadSubgroupSelectorParameter> Parameters { get; set; }

		public RadSubgroupSettings ToRadSubgroupSettings(List<string> labels)
		{
			var parameters = new List<RadParameter>(labels.Count);
			for (int i = 0; i < labels.Count && i < Parameters.Count; i++)
				parameters.Add(Parameters[i].ToRadParameter(labels[i]));

			// Note that the name of the subgroup should be null instead of empty for SLAnalytics
			return new RadSubgroupSettings(string.IsNullOrEmpty(Name) ? null : Name, ID, parameters, Options);
		}

		public bool HasMissingParameters(List<string> labels)
		{
			return Parameters.Count < labels.Count || Parameters.Any(p => p == null);
		}

		public bool HasDuplicatedParameters()
		{
			if (Parameters == null || Parameters.Count < 2)
				return false;
			return Parameters.GroupBy(p => p?.Key, new ParameterKeyEqualityComparer()).Any(g => g.Count() > 1);
		}

		public bool HasSameParameters(List<ParameterKey> parameters)
		{
			if (Parameters == null || parameters == null)
				return parameters == null && Parameters == null;

			var comparer = new ParameterKeyListEqualityComparer();
			return comparer.Equals(Parameters.Select(p => p?.Key).ToList(), parameters);
		}
	}

	public class RadSubgroupSelector : VisibilitySection
	{
		public const int MinNrOfSubgroups = 2;
		public const int MaxNrOfSubgroups = 2500;
		private readonly IEngine _engine;
		private readonly TreeView _selectorTreeView;
		private readonly Label _noSubgroupsLabel;
		private readonly RadSubgroupDetailsView _detailsView;
		private readonly Button _editButton;
		private readonly Button _removeButton;
		private readonly ParametersCache _parametersCache;
		private List<string> _parameterLabels;
		private RadGroupOptions _parentOptions;
		private Dictionary<Guid, RadSubgroupSelectorItem> _subgroups;
		private int _unnamedSubgroupCount = 0;
		private HashSet<string> _subgroupsWithMissingParameters;
		private HashSet<string> _subgroupsWithDuplicatedParameters;
		private List<string> _duplicatedSubgroupNames;
		private List<string> _subgroupsWithSameParameters;

		public RadSubgroupSelector(IEngine engine, RadGroupOptions parentOptions, List<string> parameterLabels, ParametersCache parametersCache,
			List<RadSubgroupInfo> subgroups = null, Guid? selectedSubgroup = null)
		{
			_engine = engine;
			_parentOptions = parentOptions;
			_parameterLabels = parameterLabels;
			_parametersCache = parametersCache;

			_noSubgroupsLabel = new Label("Add a subgroup by selecting 'Add subgroup...' below")
			{
				MinWidth = 600,
			};

			_selectorTreeView = new TreeView(new List<TreeViewItem>())
			{
				Tooltip = "The subgroups of the current group. Click on a subgroup to view its parameters and options.",
				IsReadOnly = false,
				MinWidth = 300,
			};
			_selectorTreeView.Changed += (sender, args) => OnSelectorTreeViewChanged();

			_detailsView = new RadSubgroupDetailsView();

			_editButton = new Button("Edit subgroup...")
			{
				Tooltip = "Edit the parameters and options of this subgroup.",
			};
			_editButton.Pressed += (sender, args) => OnEditButtonPressed();

			_removeButton = new Button("Remove subgroup")
			{
				Tooltip = "Remove this subgroup.",
			};
			_removeButton.Pressed += (sender, args) => OnRemoveButtonPressed();

			var whitespace = new WhiteSpace()
			{
				MinHeight = 100,
			};

			var addButton = new Button("Add subgroup...")
			{
				Tooltip = "Add a new subgroup.",
			};
			addButton.Pressed += (sender, args) => OnAddButtonPressed();

			SetSubgroups(subgroups, selectedSubgroup);

			AddWidget(_noSubgroupsLabel, 0, 0, 1, 1 + _detailsView.ColumnCount, () => !GetTreeViewVisible());
			AddWidget(_selectorTreeView, new WidgetLayout(1, 0, _detailsView.RowCount, 1, verticalAlignment: VerticalAlignment.Top),
				GetTreeViewVisible);
			AddSection(_detailsView, 1, 1, GetTreeViewVisible);
			AddWidget(_editButton, 0, 2, 3, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_removeButton, 3, 2, verticalAlignment: VerticalAlignment.Top);
			AddWidget(whitespace, 4, 2);
			AddWidget(addButton, 5, 2);
		}

		public event EventHandler ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public List<RadSubgroupSettings> GetSubgroups()
		{
			return _subgroups.Values.Select(s => s.ToRadSubgroupSettings(_parameterLabels)).ToList();
		}

		public void UpdateParameterLabels(List<string> parameterLabels)
		{
			_parameterLabels = parameterLabels;

			CalculateSubgroupsWithMissingParameters();
			CalculateSubgroupsWithDuplicatedParameters();
			CalculateSubgroupsWithSameParameters();
			UpdateIsValid();

			// We might need to append the missing parameters suffix to the display value, hence recalculate all items
			UpdateSelectorTreeViewItems(_selectorTreeView.Items.Where(i => i.IsChecked).Select(i => Guid.Parse(i.KeyValue)).ToArray());
		}

		public void UpdateParentOptions(RadGroupOptions parentOptions)
		{
			_parentOptions = parentOptions;
			UpdateSelectedSubgroup();
		}

		private void SetInvalidSelection(string detailsText, bool removeButtonEnabled)
		{
			_detailsView.ShowError(detailsText);
			_editButton.IsEnabled = false;
			_removeButton.IsEnabled = removeButtonEnabled;
		}

		private bool GetTreeViewVisible()
		{
			return _subgroups.Count > 0;
		}

		private void UpdateTreeViewAndLabelVisibility()
		{
			bool treeViewVisible = GetTreeViewVisible();
			_noSubgroupsLabel.IsVisible = IsSectionVisible && !treeViewVisible;
			_selectorTreeView.IsVisible = IsSectionVisible && treeViewVisible;
			_detailsView.IsVisible = IsSectionVisible && treeViewVisible;
		}

		private string GetSubgroupPlaceHolderName(int count)
		{
			return $"Unnamed subgroup {count}";
		}

		private RadSubgroupSelectorItem GetSelectedSubgroup()
		{
			var selectedItem = _selectorTreeView.Items.FirstOrDefault(i => i.IsChecked);
			if (selectedItem == null)
				return null;

			return _subgroups.TryGetValue(Guid.Parse(selectedItem.KeyValue), out var subgroup) ? subgroup : null;
		}

		private void SetSubgroups(List<RadSubgroupInfo> subgroups, Guid? selectedSubgroup)
		{
			if (subgroups == null)
			{
				_subgroups = new Dictionary<Guid, RadSubgroupSelectorItem>();
				CalculateAndUpdateIsValid();
				UpdateSelectorTreeViewItems();
				return;
			}

			_subgroups = new Dictionary<Guid, RadSubgroupSelectorItem>(subgroups.Count);
			foreach (var subgroup in subgroups)
			{
				var parameters = new List<RadSubgroupSelectorParameter>(subgroup.Parameters.Count);
				foreach (var p in subgroup.Parameters)
				{
					var element = _engine.FindElement(p.Key.DataMinerID, p.Key.ElementID);
					var paramInfo = Utils.FetchParameterInfo(_engine, _parametersCache, p.Key.DataMinerID, p.Key.ElementID, p.Key.ParameterID);
					parameters.Add(new RadSubgroupSelectorParameter
					{
						Key = p.Key,
						ElementName = element?.ElementName ?? "Unknown element",
						ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
					});
				}

				string displayValue = string.IsNullOrEmpty(subgroup.Name) ? GetSubgroupPlaceHolderName(++_unnamedSubgroupCount) : subgroup.Name;
				_subgroups[subgroup.ID] = new RadSubgroupSelectorItem(subgroup.ID, subgroup.Name, subgroup.Options, parameters, displayValue);
			}

			CalculateAndUpdateIsValid();
			if (selectedSubgroup.HasValue)
				UpdateSelectorTreeViewItems(selectedSubgroup.Value);
			else
				UpdateSelectorTreeViewItems();
		}

		private void UpdateSelectorTreeViewItems(params Guid[] selectedGroups)
		{
			const string missingParametersSuffix = " (missing parameters)";
			const string duplicatedParametersSuffix = " (duplicated parameters)";
			var newItems = new List<TreeViewItem>();
			foreach (var kvp in _subgroups.OrderBy(kvp => kvp.Value.DisplayValue, StringComparer.CurrentCultureIgnoreCase))
			{
				string displayValue = kvp.Value.DisplayValue;
				if (kvp.Value.HasMissingParameters(_parameterLabels))
					displayValue += missingParametersSuffix;
				else if (kvp.Value.HasDuplicatedParameters())
					displayValue += duplicatedParametersSuffix;
				var newItem = new TreeViewItem(displayValue, kvp.Key.ToString())
				{
					IsChecked = selectedGroups.Contains(kvp.Key),
				};
				newItems.Add(newItem);
			}

			_selectorTreeView.Items = newItems;
			UpdateSelectedSubgroup();
		}

		private void UpdateSelectedSubgroup()
		{
			UpdateTreeViewAndLabelVisibility();
			if (_subgroups.Count == 0)
			{
				_editButton.IsEnabled = false;
				_removeButton.IsEnabled = false;
				return;
			}

			var selected = _selectorTreeView.Items.Where(i => i.IsChecked).ToList();
			if (selected.Count == 0)
			{
				SetInvalidSelection("No subgroup selected.", false);
				return;
			}
			else if (selected.Count >= 2)
			{
				SetInvalidSelection("Multiple subgroups selected.", true);
				return;
			}

			var settings = GetSelectedSubgroup();
			if (settings == null)
			{
				SetInvalidSelection("Selected subgroup not found.", false);
				return;
			}

			_editButton.IsEnabled = true;
			_removeButton.IsEnabled = true;
			_detailsView.ShowSubgroup(settings, _parameterLabels, _parentOptions);
		}

		private void CalculateSubgroupsWithMissingParameters()
		{
			_subgroupsWithMissingParameters = _subgroups.Where(kvp => kvp.Value.HasMissingParameters(_parameterLabels)).Select(kvp => kvp.Value.DisplayValue).ToHashSet();
		}

		private void CalculateSubgroupsWithDuplicatedParameters()
		{
			_subgroupsWithDuplicatedParameters = _subgroups.Where(kvp => kvp.Value.HasDuplicatedParameters()).Select(kvp => kvp.Value.DisplayValue).ToHashSet();
		}

		private void CalculateDuplicatedSubgroupNames()
		{
			_duplicatedSubgroupNames = _subgroups.Where(s => !string.IsNullOrEmpty(s.Value.Name))
				.GroupBy(kvp => kvp.Value.Name, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();
		}

		/// <summary>
		/// Check whether any two subgroups have exactly the same parameters. Stops as soon as it finds one collection of subgroups that have the same parameters.
		/// </summary>
		private void CalculateSubgroupsWithSameParameters()
		{
			var parameterKeyLists = _subgroups.Select(s => Tuple.Create(s.Value.DisplayValue, s.Value.Parameters.Select(p => p?.Key).ToList()));
			var groupsWithSameParameters = parameterKeyLists.GroupBy(t => t.Item2, new ParameterKeyListEqualityComparer()).Where(g => g.Count() > 1);
			if (groupsWithSameParameters.Any())
				_subgroupsWithSameParameters = groupsWithSameParameters.First().Select(t => t.Item1).ToList();
			else
				_subgroupsWithSameParameters = new List<string>();
		}

		private void CalculateAndUpdateIsValidOnEditedSubgroup(RadSubgroupSelectorItem newSettings, RadSubgroupSelectorItem oldSettings)
		{
			if (oldSettings.HasMissingParameters(_parameterLabels))
				_subgroupsWithMissingParameters.Remove(oldSettings.DisplayValue);
			if (newSettings.HasMissingParameters(_parameterLabels))
				_subgroupsWithMissingParameters.Add(newSettings.DisplayValue);

			if (oldSettings.HasDuplicatedParameters())
				_subgroupsWithDuplicatedParameters.Remove(oldSettings.DisplayValue);
			if (newSettings.HasDuplicatedParameters())
				_subgroupsWithDuplicatedParameters.Add(newSettings.DisplayValue);

			if (!string.Equals(newSettings.Name, oldSettings.Name))
				CalculateDuplicatedSubgroupNames();

			if (!newSettings.HasSameParameters(oldSettings.Parameters?.Select(p => p?.Key).ToList()))
				CalculateSubgroupsWithSameParameters();
		}

		private void CalculateAndUpdateIsValidOnAddedSubgroup(RadSubgroupSelectorItem newSettings)
		{
			if (newSettings.HasMissingParameters(_parameterLabels))
				_subgroupsWithMissingParameters.Add(newSettings.DisplayValue);

			if (newSettings.HasDuplicatedParameters())
				_subgroupsWithDuplicatedParameters.Add(newSettings.DisplayValue);

			string newName = newSettings.Name;
			if (!string.IsNullOrEmpty(newName) && _subgroups.Count(s => string.Equals(s.Value.Name, newName, StringComparison.OrdinalIgnoreCase)) >= 2)
				_duplicatedSubgroupNames.Add(newName);

			CalculateSubgroupsWithSameParameters();
		}

		private void UpdateValidationText()
		{
			ValidationText = string.Empty;
			if (_subgroups.Count < MinNrOfSubgroups || _subgroups.Count > MaxNrOfSubgroups)
			{
				ValidationText = $"The number of subgroups must be between {MinNrOfSubgroups} and {MaxNrOfSubgroups}.";
				return;
			}

			if (_subgroupsWithMissingParameters.Count > 0)
			{
				if (_subgroupsWithMissingParameters.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithMissingParameters.First()} is missing parameters.";
				else
					ValidationText = $"Subgroups {_subgroupsWithMissingParameters.HumanReadableJoin()} are missing parameters.";
				return;
			}

			if (_subgroupsWithDuplicatedParameters.Count > 0)
			{
				if (_subgroupsWithDuplicatedParameters.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithDuplicatedParameters.First()} has duplicated parameters.";
				else
					ValidationText = $"Subgroups {_subgroupsWithDuplicatedParameters.HumanReadableJoin()} have duplicated parameters.";
				return;
			}

			if (_duplicatedSubgroupNames.Count > 0)
			{
				if (_duplicatedSubgroupNames.Count == 1)
					ValidationText = $"The name {_duplicatedSubgroupNames.First()} is used by multiple subgroups.";
				else
					ValidationText = $"The names {_duplicatedSubgroupNames.HumanReadableJoin()} are used by multiple subgroups.";
				return;
			}

			if (_subgroupsWithSameParameters.Count > 0)
				ValidationText = $"The parameters of the subgroups {_subgroupsWithSameParameters.HumanReadableJoin()} are exactly the same. Provide unique parameters for each subgroup.";
		}

		/// <summary>
		/// Update the IsValid state and ValidationText based on the already calculated state booleans.
		/// </summary>
		private void UpdateIsValid()
		{
			UpdateValidationText();
			IsValid = string.IsNullOrEmpty(ValidationText);
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Recaculate all the state booleans and update the IsValid state.
		/// </summary>
		private void CalculateAndUpdateIsValid()
		{
			CalculateSubgroupsWithDuplicatedParameters();
			CalculateSubgroupsWithMissingParameters();
			CalculateDuplicatedSubgroupNames();
			CalculateSubgroupsWithSameParameters();

			UpdateIsValid();
		}

		private void OnEditButtonPressed()
		{
			var settings = GetSelectedSubgroup();
			if (settings == null)
				return;
			var placeHolderName = string.IsNullOrEmpty(settings.Name) ? settings.DisplayValue : GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);

			InteractiveController app = new InteractiveController(_engine);
			EditSubgroupDialog dialog = new EditSubgroupDialog(_engine, _subgroups.Values.ToList(), _parameterLabels, settings, placeHolderName, _parentOptions);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as EditSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.GetSettings();
				_subgroups[settings.ID] = newSettings;
				if (string.IsNullOrEmpty(newSettings.Name) && !string.IsNullOrEmpty(settings.Name))
					_unnamedSubgroupCount++;

				CalculateAndUpdateIsValidOnEditedSubgroup(newSettings, settings);
				UpdateIsValid();
				UpdateSelectorTreeViewItems(settings.ID);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnRemoveButtonPressed()
		{
			foreach (var item in _selectorTreeView.Items.Where(i => i.IsChecked))
				_subgroups.Remove(Guid.Parse(item.KeyValue));

			CalculateAndUpdateIsValid();
			UpdateSelectorTreeViewItems();
		}

		private void OnAddButtonPressed()
		{
			var placeHolderName = GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);
			InteractiveController app = new InteractiveController(_engine);
			AddSubgroupDialog dialog = new AddSubgroupDialog(_engine, _subgroups.Values.ToList(), _parameterLabels, _parentOptions, placeHolderName);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as AddSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.GetSettings();
				_subgroups[newSettings.ID] = newSettings;
				if (string.IsNullOrEmpty(newSettings.Name))
					_unnamedSubgroupCount++;

				CalculateAndUpdateIsValidOnAddedSubgroup(newSettings);
				UpdateIsValid();
				UpdateSelectorTreeViewItems(newSettings.ID);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnSelectorTreeViewChanged()
		{
			UpdateSelectedSubgroup();
		}
	}
}
