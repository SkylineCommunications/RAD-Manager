namespace RadWidgets.Widgets.Editors
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupEditor : VisibilitySection
	{
		private readonly GroupNameSection _groupNameSection;
		private readonly List<Tuple<Label, ParameterInstanceSelector>> _parameterSelectors;
		private readonly RadSubgroupOptionsEditor _optionsEditor;
		private readonly MarginLabel _detailsLabel;
		private readonly Guid _subgroupID;
		private readonly List<RadSubgroupSelectorItem> _otherSubgroups;
		private readonly List<string> _parameterLabels;
		private bool _hasInvalidParameter;
		private bool _hasDuplicatedParameters;
		private RadSubgroupSelectorItem _subgroupWithSameParameters;

		public RadSubgroupEditor(IEngine engine, RadHelper radHelper, List<RadSubgroupSelectorItem> allSubgroups, RadGroupOptions parentOptions,
			List<string> parameterLabels, string groupNamePlaceHolder, RadSubgroupSelectorItem settings = null)
		{
			_subgroupID = settings?.ID ?? Guid.NewGuid();
			_otherSubgroups = allSubgroups;
			_parameterLabels = parameterLabels ?? new List<string>();
			if (settings != null)
				_otherSubgroups = _otherSubgroups.Where(s => s.ID != settings.ID).ToList();

			var elementsLabel = new Label("Element");
			var parametersLabel = new Label("Parameter");
			var instanceLabel = new Label("Display key");

			_parameterSelectors = new List<Tuple<Label, ParameterInstanceSelector>>(_parameterLabels.Count);
			for (int i = 0; i < _parameterLabels.Count; i++)
			{
				var label = new Label(string.IsNullOrWhiteSpace(_parameterLabels[i]) ? $"Parameter {i + 1}" : _parameterLabels[i]);

				RadSubgroupSelectorParameter parameter = null;
				if (settings != null && i < settings.Parameters.Count)
					parameter = settings.Parameters[i];
				var parameterSelector = new ParameterInstanceSelector(engine, radHelper, parameter);
				parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

				_parameterSelectors.Add(Tuple.Create(label, parameterSelector));
			}

			int parameterSelectorColumnCount = _parameterSelectors.FirstOrDefault()?.Item2.ColumnCount ?? 1;
			_groupNameSection = new GroupNameSection(settings?.Name, _otherSubgroups.Select(s => s.Name).ToList(), parameterSelectorColumnCount, groupNamePlaceHolder);
			_groupNameSection.ValidationChanged += (sender, args) => UpdateIsValidAndDetailsLabelVisibility();

			_optionsEditor = new RadSubgroupOptionsEditor(radHelper, parameterSelectorColumnCount + 1, parentOptions, settings?.Options);
			_optionsEditor.ValidationChanged += (sender, args) => UpdateIsValidAndDetailsLabelVisibility();

			_detailsLabel = new MarginLabel(string.Empty, 2, 10)
			{
				MaxTextWidth = 200,
			};

			UpdateInvalidGroups();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddWidget(elementsLabel, row, 1);
			AddWidget(parametersLabel, row, 2);
			AddWidget(instanceLabel, row, 3);
			row++;

			foreach (var (label, parameterSelector) in _parameterSelectors)
			{
				AddWidget(label, row, 0, parameterSelector.RowCount, 1);
				AddSection(parameterSelector, row, 1);
				row += parameterSelector.RowCount;
			}

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddSection(_detailsLabel, row, 0, GetDetailsLabelVisible);
		}

		public event EventHandler ValidationChanged;

		public bool IsNameValid => _groupNameSection.IsValid;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public RadSubgroupSelectorItem GetSettings()
		{
			var displayName = string.IsNullOrEmpty(_groupNameSection.GroupName) ? _groupNameSection.GroupNamePlaceHolder : _groupNameSection.GroupName;
			return new RadSubgroupSelectorItem(_subgroupID, _groupNameSection.GroupName, _optionsEditor.Options,
				_parameterSelectors.Select(t => t.Item2.SelectedItem).ToList(), displayName, _parameterLabels);
		}

		private void UpdateSubgroupWithSameParameters()
		{
			var pKeys = _parameterSelectors.Select(p => p.Item2.SelectedItem?.Key).ToList();
			foreach (var s in _otherSubgroups)
			{
				if (s.HasSameParameters(pKeys))
				{
					_subgroupWithSameParameters = s;
					return;
				}
			}

			_subgroupWithSameParameters = null;
		}

		private void UpdateDuplicatedParameters()
		{
			var grouped = _parameterSelectors.GroupBy(p => p.Item2.SelectedItem?.Key, new ParameterKeyEqualityComparer());
			_hasDuplicatedParameters = false;
			foreach (var g in grouped)
			{
				if (g.Count() > 1)
				{
					_hasDuplicatedParameters = true;
					foreach (var selector in g)
					{
						selector.Item2.ValidationState = UIValidationState.Invalid;
						selector.Item2.ValidationText = "This parameter is duplicated";
					}
				}
				else
				{
					foreach (var selector in g)
					{
						selector.Item2.ValidationState = UIValidationState.Valid;
						selector.Item2.ValidationText = string.Empty;
					}
				}
			}
		}

		private void UpdateIsValid()
		{
			List<string> validationTexts = new List<string>(2);
			if (!_groupNameSection.IsValid)
				validationTexts.Add("provide a valid subgroup name");
			if (!_optionsEditor.IsValid)
				validationTexts.Add("provide valid options for the subgroup");
			if (_hasInvalidParameter)
				validationTexts.Add("make a valid selection for each parameter");
			else if (_subgroupWithSameParameters != null)
				validationTexts.Add("do not choose exactly the same parameters as another subgroup");

			IsValid = validationTexts.Count == 0;
			ValidationText = validationTexts.HumanReadableJoin().Capitalize();
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private bool GetDetailsLabelVisible()
		{
			return _groupNameSection.IsValid && _optionsEditor.IsValid && (_hasInvalidParameter || _hasDuplicatedParameters || _subgroupWithSameParameters != null);
		}

		private void UpdateDetailsLabel()
		{
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisible();

			if (_hasInvalidParameter)
			{
				var invalidParameterLabels = _parameterSelectors.Where(s => !s.Item2.InternalIsValid).Select(s => s.Item1.Text);
				if (invalidParameterLabels.Count() > 1)
					_detailsLabel.Text = $"Make a valid selection for parameters {invalidParameterLabels.HumanReadableJoin()}.";
				else
					_detailsLabel.Text = $"Make a valid selection for parameter {invalidParameterLabels.FirstOrDefault()}";
			}
			else if (_hasDuplicatedParameters)
			{
				_detailsLabel.Text = $"Some parameters are duplicated.";
			}
			else if (_subgroupWithSameParameters != null)
			{
				_detailsLabel.Text = $"The parameters you selected are exactly the same as those of subgroup '{_subgroupWithSameParameters.DisplayName}'.";
			}
			else
			{
				_detailsLabel.Text = string.Empty;
			}
		}

		private void UpdateInvalidGroups()
		{
			_hasInvalidParameter = _parameterSelectors.Any(p => !p.Item2.InternalIsValid);
			UpdateDuplicatedParameters();
			if (!_hasInvalidParameter)
				UpdateSubgroupWithSameParameters();

			UpdateIsValid();
			UpdateDetailsLabel();
		}

		private void UpdateIsValidAndDetailsLabelVisibility()
		{
			UpdateIsValid();
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisible();
		}

		private void OnParameterSelectorChanged()
		{
			UpdateInvalidGroups();
		}
	}
}
