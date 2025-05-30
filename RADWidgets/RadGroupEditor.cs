namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadGroupEditor : VisibilitySection
	{
		public const int MIN_PARAMETERS = 2;
		public const int MAX_PARAMETERS = 100;
		private readonly GroupNameSection _groupNameSection;
		private readonly MultiParameterSelector _parameterSelector;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly Label _detailsLabel;
		private bool _moreThanMinParametersSelected = false;
		private bool _lessThanMaxParametersSelected = false;

		public RadGroupEditor(IEngine engine, List<string> existingGroupNames, ParametersCache parametersCache, RadGroupSettings settings = null)
		{
			_parameterSelector = new MultiParameterSelector(engine, parametersCache, settings?.Parameters);
			_parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

			_groupNameSection = new GroupNameSection(settings?.GroupName, existingGroupNames, _parameterSelector.ColumnCount - 1);
			_groupNameSection.ValidationChanged += (sender, args) => OnGroupNameSectionValidationChanged();

			_optionsEditor = new RadGroupOptionsEditor(_parameterSelector.ColumnCount, settings?.Options);

			_detailsLabel = new Label();

			OnGroupNameSectionValidationChanged();
			OnParameterSelectorChanged();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddSection(_parameterSelector, row, 0);
			row += _parameterSelector.RowCount;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, _parameterSelector.ColumnCount);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public RadGroupSettings Settings
		{
			get
			{
				return new RadGroupSettings
				{
					GroupName = _groupNameSection.GroupName,
					Parameters = _parameterSelector.GetSelectedParameters(),
					Options = _optionsEditor.Options,
				};
			}
		}

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise isVisible of the underlying widgets is called instead of on the sections
			get => IsSectionVisible;
			set
			{
				if (IsSectionVisible == value)
					return;

				IsSectionVisible = value;

				_groupNameSection.IsVisible = value;
				_parameterSelector.IsVisible = value;
				_optionsEditor.IsVisible = value;
				UpdateDetailsLabelVisibility();
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateIsValid()
		{
			IsValid = _groupNameSection.IsValid && _moreThanMinParametersSelected && _lessThanMaxParametersSelected;

			List<string> validationTexts = new List<string>(2);
			if (!_groupNameSection.IsValid && !_moreThanMinParametersSelected)
				validationTexts.Add("provide a valid group name");
			if (!_moreThanMinParametersSelected || !_lessThanMaxParametersSelected)
				validationTexts.Add("make a valid selection of instances");

			ValidationText = validationTexts.HumanReadableJoin().Capitalize();
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateDetailsLabelVisibility()
		{
			_detailsLabel.IsVisible = IsSectionVisible && _groupNameSection.IsValid && (!_moreThanMinParametersSelected || !_lessThanMaxParametersSelected);
		}

		private void UpdateDetailsLabel()
		{
			UpdateDetailsLabelVisibility();

			if (!_moreThanMinParametersSelected)
				_detailsLabel.Text = "Select at least two instances.";
			else if (!_lessThanMaxParametersSelected)
				_detailsLabel.Text = $"Select at most {MAX_PARAMETERS} instances.";
			else
				_detailsLabel.Text = string.Empty;
		}

		private void OnParameterSelectorChanged()
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

		private void OnGroupNameSectionValidationChanged()
		{
			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}
	}
}
