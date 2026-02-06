namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class ParameterInstanceSelector : Section, IValidationWidget
	{
		private readonly IEngine _engine;
		private readonly ElementsDropDown _elementsDropDown;
		private readonly RadParametersDropDown _parametersDropDown;
		private readonly TooltipDropDown<DynamicTableIndex> _instanceDropDown;
		private UIValidationState _validationState = UIValidationState.Valid;
		private string _validationText = string.Empty;

		public ParameterInstanceSelector(IEngine engine, int instanceDropDownColumnSpan, RadHelper radHelper, RadSubgroupSelectorParameter parameter = null)
		{
			_engine = engine;

			_elementsDropDown = new ElementsDropDown(engine, radHelper);
			_elementsDropDown.Changed += (sender, args) => OnElementsDropDownChanged();

			_parametersDropDown = new RadParametersDropDown(engine);
			_parametersDropDown.Changed += (sender, args) => OnParametersDropDownChanged();

			_instanceDropDown = new TooltipDropDown<DynamicTableIndex>()
			{
				IsDisplayFilterShown = true,
				IsSorted = true,
				MinWidth = 300,
			};
			_instanceDropDown.Changed += (sender, args) => OnInstanceDropDownChanged();

			OnElementsDropDownChanged();
			if (parameter != null)
				SelectItem(parameter);

			AddWidget(_elementsDropDown, 0, 0);
			AddWidget(_parametersDropDown, 0, 1);
			AddWidget(_instanceDropDown, 0, 2, 1, instanceDropDownColumnSpan);
		}

		public event EventHandler Changed;

		public RadSubgroupSelectorParameter SelectedItem
		{
			get
			{
				if (!InternalIsValid)
					return null;

				var element = _elementsDropDown.Selected;
				var parameter = _parametersDropDown.Selected;
				DynamicTableIndex instance = _instanceDropDown.IsEnabled ? _instanceDropDown.Selected : null;

				return new RadSubgroupSelectorParameter()
				{
					ElementName = element.Name,
					ParameterName = parameter.DisplayName,
					Key = new ParameterKey(element.DataMinerID, element.ElementID, parameter.ID, instance?.IndexValue ?? string.Empty, instance?.DisplayValue ?? string.Empty),
				};
			}
		}

		public UIValidationState ValidationState
		{
			get => _validationState;
			set
			{
				if (_validationState == value)
					return;

				_validationState = value;
				UpdateIsValid();
			}
		}

		public string ValidationText
		{
			get => _validationText;
			set
			{
				if (_validationText == value)
					return;

				_validationText = value;
				UpdateIsValid();
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current selection is valid. This is not influenced by the ValidationState or ValidationText properties, which are used
		/// for external validation checks (not done by this widget).
		/// </summary>
		public bool InternalIsValid { get; private set; }

		/// <summary>
		/// Gets  a value indicating whether the current selection is valid. This is not influenced by the ValidationState or ValidationText properties, which are used
		/// for external validation checks (not done by this widget).
		/// </summary>
		public string InternalValidationText { get; private set; }

		private void SelectItem(RadSubgroupSelectorParameter parameter)
		{
			var elementOption = _elementsDropDown.Options.FirstOrDefault(e => e.Value.DataMinerID == parameter.Key.DataMinerID && e.Value.ElementID == parameter.Key.ElementID);
			if (elementOption == null)
			{
				OnElementsDropDownChanged();
				return;
			}

			_elementsDropDown.SelectedOption = elementOption;
			OnElementsDropDownChanged();

			var parameterOption = _parametersDropDown.Options.FirstOrDefault(p => p.Value.ID == parameter.Key.ParameterID);
			if (parameterOption == null)
				return;

			_parametersDropDown.SelectedOption = parameterOption;
			OnParametersDropDownChanged();

			if (!_instanceDropDown.IsEnabled)
				return;

			var instanceOption = _instanceDropDown.Options.FirstOrDefault(i => string.Equals(i.Value.IndexValue, parameter.Key.Instance, StringComparison.OrdinalIgnoreCase));
			if (instanceOption == null)
				return;

			_instanceDropDown.SelectedOption = instanceOption;
			OnInstanceDropDownChanged();
		}

		private void SetPossibleInstances(int dataMinerID, int elementID, ParameterInfo parameter)
		{
			var instances = Utils.FetchInstancesWithTrending(_engine, dataMinerID, elementID, parameter);
			_instanceDropDown.Options = instances.Select(i => new Option<DynamicTableIndex>(i.DisplayValue, i)).ToList();
		}

		private void UpdateIsValid()
		{
			if (_elementsDropDown.Selected == null)
			{
				InternalValidationText = "Select a valid element";
				InternalIsValid = false;

				_elementsDropDown.ValidationState = UIValidationState.Invalid;
				_elementsDropDown.ValidationText = InternalValidationText;
				_parametersDropDown.ValidationState = UIValidationState.Valid;
				_parametersDropDown.ValidationText = string.Empty;
				_instanceDropDown.ValidationState = UIValidationState.Valid;
				_instanceDropDown.ValidationText = string.Empty;
			}
			else if (_parametersDropDown.Selected == null)
			{
				InternalValidationText = "Select a valid parameter";
				InternalIsValid = false;

				_elementsDropDown.ValidationState = UIValidationState.Valid;
				_elementsDropDown.ValidationText = string.Empty;
				_parametersDropDown.ValidationState = UIValidationState.Invalid;
				_parametersDropDown.ValidationText = InternalValidationText;
				_instanceDropDown.ValidationState = UIValidationState.Valid;
				_instanceDropDown.ValidationText = string.Empty;
			}
			else if (_instanceDropDown.IsEnabled && _instanceDropDown.Selected == null)
			{
				InternalValidationText = "Select a valid instance";
				InternalIsValid = false;

				_elementsDropDown.ValidationState = UIValidationState.Valid;
				_elementsDropDown.ValidationText = string.Empty;
				_parametersDropDown.ValidationState = UIValidationState.Valid;
				_parametersDropDown.ValidationText = string.Empty;
				_instanceDropDown.ValidationState = UIValidationState.Invalid;
				_instanceDropDown.ValidationText = InternalValidationText;
			}
			else
			{
				InternalValidationText = string.Empty;
				InternalIsValid = true;

				_elementsDropDown.ValidationState = UIValidationState.Valid;
				_elementsDropDown.ValidationText = string.Empty;

				if (ValidationState != UIValidationState.Valid)
				{
					_elementsDropDown.ValidationState = UIValidationState.Valid;
					_elementsDropDown.ValidationText = string.Empty;
					if (_instanceDropDown.IsEnabled)
					{
						_parametersDropDown.ValidationState = UIValidationState.Valid;
						_parametersDropDown.ValidationText = string.Empty;
						_instanceDropDown.ValidationState = UIValidationState.Invalid;
						_instanceDropDown.ValidationText = ValidationText;
					}
					else
					{
						_parametersDropDown.ValidationState = UIValidationState.Invalid;
						_parametersDropDown.ValidationText = ValidationText;
						_instanceDropDown.ValidationState = UIValidationState.Valid;
						_instanceDropDown.ValidationText = string.Empty;
					}
				}
				else
				{
					_parametersDropDown.ValidationState = UIValidationState.Valid;
					_parametersDropDown.ValidationText = string.Empty;
					_instanceDropDown.ValidationState = UIValidationState.Valid;
					_instanceDropDown.ValidationText = string.Empty;
				}
			}
		}

		private void OnElementsDropDownChanged()
		{
			var element = _elementsDropDown.Selected;
			if (element == null)
				_parametersDropDown.ClearPossibleParameters();
			else
				_parametersDropDown.SetPossibleParameters(element.DataMinerID, element.ElementID);

			OnParametersDropDownChanged();
		}

		private void OnParametersDropDownChanged()
		{
			var element = _elementsDropDown.Selected;
			var parameter = _parametersDropDown.Selected;
			if (element == null || parameter?.IsTableColumn != true)
			{
				_instanceDropDown.Options = new List<Option<DynamicTableIndex>>();
				_instanceDropDown.IsEnabled = false;
			}
			else
			{
				_instanceDropDown.IsEnabled = true;
				SetPossibleInstances(element.DataMinerID, element.ElementID, parameter);
			}

			OnInstanceDropDownChanged();
		}

		private void OnInstanceDropDownChanged()
		{
			UpdateIsValid();
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
