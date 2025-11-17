namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using RadWidgets;
	using RadWidgets.Widgets.Dialogs;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
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

	public class RadSubgroupSelectorItem : SelectorItem
	{
		public RadSubgroupSelectorItem(Guid id, string name, RadSubgroupOptions options, List<RadSubgroupSelectorParameter> parameters,
			string displayName, List<string> parameterLabels)
		{
			ID = id;
			Name = name;
			Options = options;
			Parameters = parameters;
			DisplayName = displayName;
			UpdateHasMissingParameters(parameterLabels);
			HasDuplicatedParameters = Parameters != null && Parameters.Count >= 2 && Parameters.GroupBy(p => p?.Key, new ParameterKeyEqualityComparer()).Any(g => g.Count() > 1);
		}

		public Guid ID { get; private set; }

		public string Name { get; private set; }

		public string DisplayName { get; private set; }

		public bool HasMissingParameters { get; private set; }

		public bool HasDuplicatedParameters { get; private set; }

		public bool HasInvalidOptions => (Options.MinimalDuration.HasValue && Options.MinimalDuration.Value < RadGroupBaseOptionsEditor.MinimumDurationLowerLimit) ||
			(Options.AnomalyThreshold.HasValue && Options.AnomalyThreshold.Value <= 0);

		public bool HasWhiteSpaceName => string.IsNullOrWhiteSpace(Name) && !string.IsNullOrEmpty(Name);

		public RadSubgroupOptions Options { get; private set; }

		public List<RadSubgroupSelectorParameter> Parameters { get; private set; }

		public RadSubgroupSettings ToRadSubgroupSettings(List<string> labels)
		{
			var parameters = new List<RadParameter>(labels.Count);
			for (int i = 0; i < labels.Count && i < Parameters.Count; i++)
				parameters.Add(Parameters[i].ToRadParameter(labels[i]));

			// Note that the name of the subgroup should be null instead of empty for SLAnalytics
			return new RadSubgroupSettings(string.IsNullOrEmpty(Name) ? null : Name, ID, parameters, Options);
		}

		public bool HasSameParameters(List<ParameterKey> parameters)
		{
			if (Parameters == null || parameters == null)
				return parameters == null && Parameters == null;

			var comparer = new ParameterKeyListEqualityComparer();
			return comparer.Equals(Parameters.Select(p => p?.Key).ToList(), parameters);
		}

		public void UpdateHasMissingParameters(List<string> parameterLabels)
		{
			if (parameterLabels == null)
				HasMissingParameters = false;
			else if (Parameters == null)
				HasMissingParameters = parameterLabels.Count > 0;
			else if (Parameters.Count < parameterLabels.Count)
				HasMissingParameters = true;
			else
				HasMissingParameters = Parameters.Take(Math.Min(parameterLabels.Count, Parameters.Count)).Any(p => p == null);
		}

		public override string GetKey()
		{
			return ID.ToString();
		}

		public override string GetDisplayValue()
		{
			if (HasMissingParameters)
				return $"{DisplayName} (missing parameters)";
			else if (HasDuplicatedParameters)
				return $"{DisplayName} (duplicated parameters)";
			else if (HasWhiteSpaceName)
				return $"{DisplayName} (invalid name)";
			else if (HasInvalidOptions)
				return $"{DisplayName} (invalid options)";
			else
				return DisplayName;
		}
	}

	public class RadSubgroupSelector : VisibilitySection
	{
		public const int MinNrOfSubgroups = 1;
		public const int MaxNrOfSubgroups = 2500;
		private readonly IEngine _engine;
		private readonly RadHelper _radHelper;
		private readonly DetailsViewer<RadSubgroupSelectorItem> _subgroupViewer;
		private readonly RadSubgroupDetailsView _subgroupDetailsView;
		private readonly Button _editButton;
		private readonly Button _removeButton;
		private readonly ParametersCache _parametersCache;
		private List<string> _parameterLabels;
		private RadGroupOptions _parentOptions;
		private int _unnamedSubgroupCount = 0;
		private HashSet<string> _subgroupsWithMissingParameters;
		private HashSet<string> _subgroupsWithDuplicatedParameters;
		private List<string> _duplicatedSubgroupNames;
		private List<string> _subgroupsWithWhitespaceNames;
		private List<string> _subgroupsWithSameParameters;
		private List<string> _subgroupsWithInvalidOptions;

		public RadSubgroupSelector(IEngine engine, RadHelper radHelper, RadGroupOptions parentOptions, List<string> parameterLabels, ParametersCache parametersCache,
			List<RadSubgroupInfo> subgroups = null, Guid? selectedSubgroup = null)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
			_radHelper = radHelper ?? throw new ArgumentNullException(nameof(radHelper));
			_parameterLabels = parameterLabels;
			_parentOptions = parentOptions ?? throw new ArgumentNullException(nameof(parentOptions));
			_parametersCache = parametersCache;

			_subgroupDetailsView = new RadSubgroupDetailsView(radHelper, 2, parameterLabels, parentOptions);

			_subgroupViewer = new DetailsViewer<RadSubgroupSelectorItem>(_subgroupDetailsView, "Subgroups");
			_subgroupViewer.SelectionChanged += (sender, args) => OnSubgroupViewerSelectionChanged(args.Selection);

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

			AddSection(_subgroupViewer, 0, 0);
			AddWidget(addButton, 0, 2);
			AddWidget(_editButton, 1, 2, 2, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_removeButton, 3, 2, 1, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(whitespace, 4, 2);
		}

		public event EventHandler ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public List<RadSubgroupSettings> GetSubgroups()
		{
			return _subgroupViewer.GetItems().Select(s => s.ToRadSubgroupSettings(_parameterLabels)).ToList();
		}

		public void UpdateParameterLabels(List<string> parameterLabels)
		{
			_parameterLabels = parameterLabels;
			_subgroupDetailsView.SetParameterLabels(parameterLabels);

			var newItems = _subgroupViewer.GetItems();
			Guid? selectedID = _subgroupViewer.GetSelected()?.ID;
			foreach (var item in newItems)
				item.UpdateHasMissingParameters(_parameterLabels);

			CalculateSubgroupsWithMissingParameters(newItems);
			CalculateSubgroupsWithDuplicatedParameters(newItems);
			CalculateSubgroupsWithSameParameters(newItems);
			UpdateIsValid(newItems);

			// We might need to append the missing parameters suffix to the display value, hence recalculate all items
			UpdateSubgroupViewerItems(newItems, selectedID);
		}

		public void UpdateParentOptions(RadGroupOptions parentOptions)
		{
			_parentOptions = parentOptions ?? throw new ArgumentNullException(nameof(parentOptions));
			_subgroupDetailsView.SetParentOptions(parentOptions);
		}

		private void SetSubgroups(List<RadSubgroupInfo> subgroups, Guid? selectedSubgroup)
		{
			List<RadSubgroupSelectorItem> items = new List<RadSubgroupSelectorItem>(subgroups?.Count ?? 0);
			if (subgroups == null)
			{
				UpdateSubgroupViewerItems(items);
				CalculateAndUpdateIsValid(items);
				return;
			}

			foreach (var subgroup in subgroups)
			{
				var parameters = new List<RadSubgroupSelectorParameter>(subgroup.Parameters.Count);
				foreach (var p in subgroup.Parameters)
				{
					var element = _engine.FindElement(p.Key.DataMinerID, p.Key.ElementID);
					var paramInfo = RadWidgets.Utils.FetchParameterInfo(_engine, _parametersCache, p.Key.DataMinerID, p.Key.ElementID, p.Key.ParameterID);
					parameters.Add(new RadSubgroupSelectorParameter
					{
						Key = p.Key,
						ElementName = element?.ElementName ?? "Unknown element",
						ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
					});
				}

				string displayName = string.IsNullOrEmpty(subgroup.Name) ? RadUtils.Utils.GetSubgroupPlaceHolderName(++_unnamedSubgroupCount) : subgroup.Name;
				items.Add(new RadSubgroupSelectorItem(subgroup.ID, subgroup.Name, subgroup.Options, parameters, displayName, _parameterLabels));
			}

			if (selectedSubgroup.HasValue)
				UpdateSubgroupViewerItems(items, selectedSubgroup.Value);
			else
				UpdateSubgroupViewerItems(items);
			CalculateAndUpdateIsValid(items);
		}

		private void UpdateSubgroupViewerItems(List<RadSubgroupSelectorItem> subgroups, Guid? selectedGroup = null)
		{
			_subgroupViewer.SetItems(subgroups.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(), selectedGroup?.ToString());
		}

		private void CalculateSubgroupsWithMissingParameters(List<RadSubgroupSelectorItem> subgroups)
		{
			_subgroupsWithMissingParameters = subgroups.Where(s => s.HasMissingParameters).Select(s => s.DisplayName).ToHashSet();
		}

		private void CalculateSubgroupsWithDuplicatedParameters(List<RadSubgroupSelectorItem> subgroups)
		{
			_subgroupsWithDuplicatedParameters = subgroups.Where(s => s.HasDuplicatedParameters).Select(s => s.DisplayName).ToHashSet();
		}

		private void CalculateDuplicatedSubgroupNames(List<RadSubgroupSelectorItem> subgroups)
		{
			_duplicatedSubgroupNames = subgroups.Where(s => !string.IsNullOrEmpty(s.Name))
				.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();
		}

		private void CalculateSubgroupsWithWhitespaceNames(List<RadSubgroupSelectorItem> subgroups)
		{
			_subgroupsWithWhitespaceNames = subgroups.Where(s => s.HasWhiteSpaceName).Select(s => s.DisplayName).ToList();
		}

		/// <summary>
		/// Check whether any two subgroups have exactly the same parameters. Stops as soon as it finds one collection of subgroups that have the same parameters.
		/// </summary>
		private void CalculateSubgroupsWithSameParameters(List<RadSubgroupSelectorItem> subgroups)
		{
			var parameterKeyLists = subgroups.Select(s => Tuple.Create(s.DisplayName, s.Parameters.Select(p => p?.Key).ToList()));
			var groupsWithSameParameters = parameterKeyLists.GroupBy(t => t.Item2, new ParameterKeyListEqualityComparer()).Where(g => g.Count() > 1);
			if (groupsWithSameParameters.Any())
				_subgroupsWithSameParameters = groupsWithSameParameters.First().Select(t => t.Item1).ToList();
			else
				_subgroupsWithSameParameters = new List<string>();
		}

		private void CalculateSubgroupsWithInvalidOptions(List<RadSubgroupSelectorItem> subgroups)
		{
			_subgroupsWithInvalidOptions = subgroups.Where(s => s.HasInvalidOptions).Select(s => s.DisplayName).ToList();
		}

		private void CalculateIsValidOnEditedSubgroup(List<RadSubgroupSelectorItem> subgroups, RadSubgroupSelectorItem newSettings, RadSubgroupSelectorItem oldSettings)
		{
			if (oldSettings.HasMissingParameters)
				_subgroupsWithMissingParameters.Remove(oldSettings.DisplayName);
			if (newSettings.HasMissingParameters)
				_subgroupsWithMissingParameters.Add(newSettings.DisplayName);

			if (oldSettings.HasDuplicatedParameters)
				_subgroupsWithDuplicatedParameters.Remove(oldSettings.DisplayName);
			if (newSettings.HasDuplicatedParameters)
				_subgroupsWithDuplicatedParameters.Add(newSettings.DisplayName);

			if (oldSettings.HasWhiteSpaceName)
				_subgroupsWithWhitespaceNames.Remove(oldSettings.DisplayName);
			if (newSettings.HasWhiteSpaceName)
				_subgroupsWithWhitespaceNames.Add(newSettings.DisplayName);

			if (oldSettings.HasInvalidOptions)
				_subgroupsWithInvalidOptions.Remove(oldSettings.DisplayName);
			if (newSettings.HasInvalidOptions)
				_subgroupsWithInvalidOptions.Add(newSettings.DisplayName);

			if (!string.Equals(newSettings.Name, oldSettings.Name))
				CalculateDuplicatedSubgroupNames(subgroups);

			if (!newSettings.HasSameParameters(oldSettings.Parameters?.Select(p => p?.Key).ToList()))
				CalculateSubgroupsWithSameParameters(subgroups);
		}

		private void CalculateIsValidOnAddedSubgroup(List<RadSubgroupSelectorItem> subgroups, RadSubgroupSelectorItem newSettings)
		{
			if (newSettings.HasMissingParameters)
				_subgroupsWithMissingParameters.Add(newSettings.DisplayName);

			if (newSettings.HasDuplicatedParameters)
				_subgroupsWithDuplicatedParameters.Add(newSettings.DisplayName);

			if (newSettings.HasWhiteSpaceName)
				_subgroupsWithWhitespaceNames.Add(newSettings.DisplayName);

			if (newSettings.HasInvalidOptions)
				_subgroupsWithInvalidOptions.Add(newSettings.DisplayName);

			string newName = newSettings.Name;
			if (!string.IsNullOrEmpty(newName) && subgroups.Count(s => string.Equals(s.Name, newName, StringComparison.OrdinalIgnoreCase)) >= 2)
				_duplicatedSubgroupNames.Add(newName);

			CalculateSubgroupsWithSameParameters(subgroups);
		}

		private bool UpdateValidationTextForNrOfSubgroups(List<RadSubgroupSelectorItem> subgroups)
		{
			if (subgroups.Count < MinNrOfSubgroups || subgroups.Count > MaxNrOfSubgroups)
			{
				ValidationText = $"The number of subgroups must be between {MinNrOfSubgroups} and {MaxNrOfSubgroups}.";
				return true;
			}

			return false;
		}

		private bool UpdateValidationTextForMissingParameters()
		{
			if (_subgroupsWithMissingParameters.Count > 0)
			{
				if (_subgroupsWithMissingParameters.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithMissingParameters.First()} is missing parameters.";
				else
					ValidationText = $"Subgroups {_subgroupsWithMissingParameters.HumanReadableJoin()} are missing parameters.";

				return true;
			}

			return false;
		}

		private bool UpdateValidationTextForDuplicatedParameters()
		{
			if (_subgroupsWithDuplicatedParameters.Count > 0)
			{
				if (_subgroupsWithDuplicatedParameters.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithDuplicatedParameters.First()} has duplicated parameters.";
				else
					ValidationText = $"Subgroups {_subgroupsWithDuplicatedParameters.HumanReadableJoin()} have duplicated parameters.";

				return true;
			}

			return false;
		}

		private bool UpdateValidationTextForDuplicatedNames()
		{
			if (_duplicatedSubgroupNames.Count > 0)
			{
				if (_duplicatedSubgroupNames.Count == 1)
					ValidationText = $"The name {_duplicatedSubgroupNames.First()} is used by multiple subgroups.";
				else
					ValidationText = $"The names {_duplicatedSubgroupNames.HumanReadableJoin()} are used by multiple subgroups.";

				return true;
			}

			return false;
		}

		private bool UpdateValidationTextForWhitespaceNames()
		{
			if (_subgroupsWithWhitespaceNames.Count > 0)
			{
				if (_subgroupsWithWhitespaceNames.Count == 1)
					ValidationText = $"One subgroup name only contains whitespace characters. This is not allowed.";
				else
					ValidationText = $"Multiple subgroup names only contain whitespace characters. This is not allowed.";

				return true;
			}

			return false;
		}

		private bool UpdateValidationTextForSameParameters()
		{
			if (_subgroupsWithSameParameters.Count > 0)
			{
				ValidationText = $"The parameters of the subgroups {_subgroupsWithSameParameters.HumanReadableJoin()} are exactly the same. Provide unique parameters for each subgroup.";
				return true;
			}

			return false;
		}

		private bool UpdateValidationTextForInvalidOptions()
		{
			if (_subgroupsWithInvalidOptions.Count > 0)
			{
				if (_subgroupsWithInvalidOptions.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithInvalidOptions.First()} has invalid options.";
				else
					ValidationText = $"Subgroups {_subgroupsWithInvalidOptions.HumanReadableJoin()} have invalid options.";
				return true;
			}

			return false;
		}

		private void UpdateValidationText(List<RadSubgroupSelectorItem> subgroups)
		{
			if (UpdateValidationTextForNrOfSubgroups(subgroups))
				return;

			if (UpdateValidationTextForMissingParameters())
				return;

			if (UpdateValidationTextForDuplicatedParameters())
				return;

			if (UpdateValidationTextForDuplicatedNames())
				return;

			if (UpdateValidationTextForWhitespaceNames())
				return;

			if (UpdateValidationTextForSameParameters())
				return;

			if (UpdateValidationTextForInvalidOptions())
				return;

			ValidationText = string.Empty; // No validation errors found
		}

		/// <summary>
		/// Update the IsValid state and ValidationText based on the already calculated state booleans.
		/// </summary>
		private void UpdateIsValid(List<RadSubgroupSelectorItem> subgroups)
		{
			UpdateValidationText(subgroups);
			IsValid = string.IsNullOrEmpty(ValidationText);
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Recaculate all the state booleans and update the IsValid state.
		/// </summary>
		private void CalculateAndUpdateIsValid(List<RadSubgroupSelectorItem> subgroups)
		{
			CalculateSubgroupsWithDuplicatedParameters(subgroups);
			CalculateSubgroupsWithMissingParameters(subgroups);
			CalculateSubgroupsWithWhitespaceNames(subgroups);
			CalculateDuplicatedSubgroupNames(subgroups);
			CalculateSubgroupsWithSameParameters(subgroups);
			CalculateSubgroupsWithInvalidOptions(subgroups);

			UpdateIsValid(subgroups);
		}

		private void OnEditButtonPressed()
		{
			var settings = _subgroupViewer.GetSelected();
			if (settings == null)
				return;
			var placeHolderName = string.IsNullOrEmpty(settings.Name) ? settings.DisplayName : RadUtils.Utils.GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);

			InteractiveController app = new InteractiveController(_engine);
			EditSubgroupDialog dialog = new EditSubgroupDialog(_engine, _radHelper, _subgroupViewer.GetItems().ToList(), _parameterLabels, settings, placeHolderName, _parentOptions);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as EditSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.GetSettings();
				var newItems = _subgroupViewer.GetItems().Select(s => s.ID == settings.ID ? newSettings : s).ToList();
				if (string.IsNullOrEmpty(newSettings.Name) && !string.IsNullOrEmpty(settings.Name))
					_unnamedSubgroupCount++;

				UpdateSubgroupViewerItems(newItems, settings.ID);
				CalculateIsValidOnEditedSubgroup(newItems, newSettings, settings);
				UpdateIsValid(newItems);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnRemoveButtonPressed()
		{
			var selectedKey = _subgroupViewer.GetSelected()?.ID;
			if (selectedKey == null)
				return;

			var newItems = _subgroupViewer.GetItems().Where(s => s.ID != selectedKey).ToList();
			CalculateAndUpdateIsValid(newItems);
			UpdateSubgroupViewerItems(newItems);
		}

		private void OnAddButtonPressed()
		{
			var placeHolderName = RadUtils.Utils.GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);
			InteractiveController app = new InteractiveController(_engine);
			AddSubgroupDialog dialog = new AddSubgroupDialog(_engine, _radHelper, _subgroupViewer.GetItems().ToList(), _parameterLabels, _parentOptions, placeHolderName);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as AddSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.GetSettings();
				var newItems = _subgroupViewer.GetItems();
				newItems.Add(newSettings);
				if (string.IsNullOrEmpty(newSettings.Name))
					_unnamedSubgroupCount++;

				CalculateIsValidOnAddedSubgroup(newItems, newSettings);
				UpdateIsValid(newItems);
				UpdateSubgroupViewerItems(newItems, newSettings.ID);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnSubgroupViewerSelectionChanged(RadSubgroupSelectorItem selection)
		{
			_editButton.IsEnabled = selection != null;
			_removeButton.IsEnabled = selection != null;
		}
	}
}
