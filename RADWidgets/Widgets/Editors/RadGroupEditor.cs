namespace RadWidgets.Widgets.Editors
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadGroupEditor : VisibilitySection
	{
		public const int MIN_PARAMETERS = 2;
		public const int MAX_PARAMETERS = 100;
		private readonly GroupNameSection _groupNameSection;
		private readonly MultiParameterSelector _parameterSelector;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly TrainingConfigurationButton _trainingButton;
		private readonly MarginLabel _detailsLabel;
		private bool _moreThanMinParametersSelected = false;
		private bool _lessThanMaxParametersSelected = false;

		public RadGroupEditor(IEngine engine, RadHelper radHelper, List<string> existingGroupNames, ParametersCache parametersCache,
			RadGroupInfo settings = null)
		{
			_parameterSelector = new MultiParameterSelector(engine, parametersCache, settings?.Subgroups?.FirstOrDefault()?.Parameters?.Select(p => p?.Key));
			_parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

			_groupNameSection = new GroupNameSection(settings?.GroupName, existingGroupNames, _parameterSelector.ColumnCount - 1);
			_groupNameSection.ValidationChanged += (sender, args) => UpdateIsValidAndDetailsLabelVisibility();

			var options = settings?.Options;
			var subgroupOptions = settings?.Subgroups?.FirstOrDefault()?.Options;
			if (subgroupOptions != null)
			{
				options = new RadGroupOptions(options?.UpdateModel ?? true, subgroupOptions.AnomalyThreshold ?? options?.AnomalyThreshold,
					subgroupOptions.MinimalDuration ?? options?.MinimalDuration);
			}

			_optionsEditor = new RadGroupOptionsEditor(radHelper, _parameterSelector.ColumnCount, options);
			_optionsEditor.ValidationChanged += (sender, args) => UpdateIsValidAndDetailsLabelVisibility();

			_trainingButton = new TrainingConfigurationButton(engine, radHelper, _parameterSelector.ColumnCount, settings == null);

			_detailsLabel = new MarginLabel(string.Empty, _parameterSelector.ColumnCount, 10);

			UpdateParametersSelectedInRange();
			UpdateDetailsLabel();
			UpdateIsValid();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddSection(_parameterSelector, row, 0);
			row += _parameterSelector.RowCount;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddSection(_trainingButton, row, 0);
			row += _trainingButton.RowCount;

			AddSection(_detailsLabel, row, 0, GetDetailsLabelVisible);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public void GetSettings(out RadGroupSettings settings, out TrainingConfiguration trainingConfiguration)
		{
			var parameters = _parameterSelector.GetSelectedParameters().Select(p => new RadParameter(p, string.Empty)).ToList();
			var subgroup = new RadSubgroupSettings(_groupNameSection.GroupName, Guid.NewGuid(), parameters, new RadSubgroupOptions());
			settings = new RadGroupSettings(_groupNameSection.GroupName, _optionsEditor.Options, new List<RadSubgroupSettings> { subgroup });

			if (_trainingButton.Configuration != null)
			{
				var timeRanges = _trainingButton.Configuration.SelectedTimeRanges.Select(tr => tr.TimeRange).ToList();
				trainingConfiguration = new TrainingConfiguration(timeRanges, null);
			}
			else
			{
				trainingConfiguration = null;
			}
		}

		private void UpdateIsValid()
		{
			IsValid = _groupNameSection.IsValid && _optionsEditor.IsValid && _moreThanMinParametersSelected && _lessThanMaxParametersSelected;

			List<string> validationTexts = new List<string>(2);
			if (!_groupNameSection.IsValid && !_moreThanMinParametersSelected)
				validationTexts.Add("provide a valid group name");
			if (!_moreThanMinParametersSelected || !_lessThanMaxParametersSelected)
				validationTexts.Add("make a valid selection of instances");

			ValidationText = validationTexts.HumanReadableJoin().Capitalize();
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private bool GetDetailsLabelVisible()
		{
			return _groupNameSection.IsValid && _optionsEditor.IsValid && (!_moreThanMinParametersSelected || !_lessThanMaxParametersSelected);
		}

		private void UpdateDetailsLabel()
		{
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisible();

			if (!_moreThanMinParametersSelected)
				_detailsLabel.Text = "Select at least two instances.";
			else if (!_lessThanMaxParametersSelected)
				_detailsLabel.Text = $"Select at most {MAX_PARAMETERS} instances.";
			else
				_detailsLabel.Text = string.Empty;
		}

		private void UpdateParametersSelectedInRange()
		{
			var count = _parameterSelector.GetSelectedParameters().Count();
			bool newMinParametersState = count >= MIN_PARAMETERS;
			bool newMaxParametersState = count <= MAX_PARAMETERS;
			if (newMinParametersState != _moreThanMinParametersSelected || newMaxParametersState != _lessThanMaxParametersSelected)
			{
				_moreThanMinParametersSelected = newMinParametersState;
				_lessThanMaxParametersSelected = newMaxParametersState;
				UpdateDetailsLabel();
				UpdateIsValid();
			}
		}

		private void UpdateIsValidAndDetailsLabelVisibility()
		{
			_detailsLabel.IsVisible = IsSectionVisible && GetDetailsLabelVisible();
			UpdateIsValid();
		}

		private void OnParameterSelectorChanged()
		{
			UpdateParametersSelectedInRange();
		}
	}
}
