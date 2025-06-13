namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
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
		private bool _hasInvalidParameter;
		private IGrouping<ParameterKey, Tuple<Label, ParameterInstanceSelector>> _duplicatedParameters;
		private RadSubgroupSelectorItem _subgroupWithSameParameters;

		public RadSubgroupEditor(IEngine engine, List<RadSubgroupSelectorItem> allSubgroups, RadGroupOptions parentOptions,
			List<string> parameterLabels, string groupNamePlaceHolder, RadSubgroupSelectorItem settings = null)
		{
			_subgroupID = settings?.ID ?? Guid.NewGuid();
			_otherSubgroups = allSubgroups;
			if (settings != null)
				_otherSubgroups = _otherSubgroups.Where(s => s.ID != settings.ID).ToList();

			_parameterSelectors = new List<Tuple<Label, ParameterInstanceSelector>>(parameterLabels.Count);
			for (int i = 0; i < parameterLabels.Count; i++)
			{
				var label = new Label(string.IsNullOrEmpty(parameterLabels[i]) ? $"Parameter {i + 1}" : parameterLabels[i]);

				RadSubgroupSelectorParameter parameter = null;
				if (settings != null && i < settings.Parameters.Count)
					parameter = settings.Parameters[i];
				var parameterSelector = new ParameterInstanceSelector(engine, parameter);
				parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

				_parameterSelectors.Add(Tuple.Create(label, parameterSelector));
			}

			int parameterSelectorColumnCount = _parameterSelectors.FirstOrDefault()?.Item2.ColumnCount ?? 1;
			_groupNameSection = new GroupNameSection(settings?.Name, _otherSubgroups.Select(s => s.Name).ToList(), parameterSelectorColumnCount, groupNamePlaceHolder);
			_groupNameSection.ValidationChanged += (sender, args) => OnGroupNameSectionValidationChanged();

			_optionsEditor = new RadSubgroupOptionsEditor(parameterSelectorColumnCount + 1, parentOptions, settings?.Options);

			_detailsLabel = new MarginLabel(string.Empty, 2, 10)
			{
				MaxTextWidth = 200,
			};

			OnGroupNameSectionValidationChanged();
			OnParameterSelectorChanged();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

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

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public RadSubgroupSelectorItem GetSettings()
		{
			return new RadSubgroupSelectorItem(_subgroupID, _groupNameSection.GroupName, _optionsEditor.Options,
				_parameterSelectors.Select(t => t.Item2.SelectedItem).ToList(),
				string.IsNullOrEmpty(_groupNameSection.GroupName) ? _groupNameSection.GroupNamePlaceHolder : _groupNameSection.GroupName);
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
			_duplicatedParameters = _parameterSelectors.GroupBy(p => p.Item2.SelectedItem?.Key, new ParameterKeyEqualityComparer()).FirstOrDefault(g => g.Count() > 1);
		}

		private void UpdateIsValid()
		{
			List<string> validationTexts = new List<string>(2);
			if (!_groupNameSection.IsValid)
				validationTexts.Add("provide a valid subgroup name");
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
			return _groupNameSection.IsValid && (_hasInvalidParameter || _duplicatedParameters != null || _subgroupWithSameParameters != null);
		}

		private void UpdateDetailsLabel()
		{
			_detailsLabel.IsVisible = this.IsSectionVisible && GetDetailsLabelVisible();

			if (_hasInvalidParameter)
			{
				var invalidParameterLabels = _parameterSelectors.Where(s => !s.Item2.IsValid).Select(s => s.Item1.Text);
				if (invalidParameterLabels.Count() > 1)
					_detailsLabel.Text = $"Make a valid selection for parameters {invalidParameterLabels.HumanReadableJoin()}.";
				else
					_detailsLabel.Text = $"Make a valid selection for parameter {invalidParameterLabels.FirstOrDefault()}";
			}
			else if (_duplicatedParameters != null)
			{
				_detailsLabel.Text = $"The parameters you selected for {_duplicatedParameters.Select(s => s.Item1.Text).HumanReadableJoin()} are exactly the same.";
			}
			else if (_subgroupWithSameParameters != null)
			{
				_detailsLabel.Text = $"The parameters you selected are exactly the same as those of subgroup '{_subgroupWithSameParameters.DisplayValue}'.";
			}
			else
			{
				_detailsLabel.Text = string.Empty;
			}
		}

		private void OnParameterSelectorChanged()
		{
			_hasInvalidParameter = _parameterSelectors.Any(p => !p.Item2.IsValid);
			if (!_hasInvalidParameter)
			{
				UpdateSubgroupWithSameParameters();
				UpdateDuplicatedParameters();
			}

			UpdateIsValid();
			UpdateDetailsLabel();
		}

		private void OnGroupNameSectionValidationChanged()
		{
			UpdateIsValid();
			_detailsLabel.IsVisible = this.IsSectionVisible && GetDetailsLabelVisible();
		}
	}
}
