namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ParameterInstanceSelector : Section
	{
		private readonly IEngine _engine;
		private readonly ElementsDropDown _elementsDropDown;
		private readonly RadParametersDropDown _parametersDropDown;
		private readonly DropDown<DynamicTableIndex> _instanceDropDown;

		public ParameterInstanceSelector(IEngine engine, RadSubgroupSelectorParameter parameter = null)
		{
			_engine = engine;

			var elementsLabel = new Label("Element");
			_elementsDropDown = new ElementsDropDown(engine);
			_elementsDropDown.Changed += (sender, args) => OnElementsDropDownChanged();

			var parametersLabel = new Label("Parameter");
			_parametersDropDown = new RadParametersDropDown(engine);
			if (parameter != null)
				_parametersDropDown.Changed += (sender, args) => OnParametersDropDownChanged();

			var instanceLabel = new Label("Display key");
			_instanceDropDown = new DropDown<DynamicTableIndex>()
			{
				IsDisplayFilterShown = true,
				IsSorted = true,
				MinWidth = 300,
			};
			_instanceDropDown.Changed += (sender, args) => OnInstanceDropDownChanged();

			OnElementsDropDownChanged();
			if (parameter != null)
				SelectItem(parameter);

			AddWidget(elementsLabel, 0, 0);
			AddWidget(_elementsDropDown, 1, 0);

			AddWidget(parametersLabel, 0, 1);
			AddWidget(_parametersDropDown, 1, 1);

			AddWidget(instanceLabel, 0, 2);
			AddWidget(_instanceDropDown, 1, 2);
		}

		public event EventHandler Changed;

		public RadSubgroupSelectorParameter SelectedItem
		{
			get
			{
				if (!IsValid)
					return null;

				var element = _elementsDropDown.Selected;
				var parameter = _parametersDropDown.Selected;
				DynamicTableIndex instance = _instanceDropDown.IsEnabled ? _instanceDropDown.Selected : null;

				return new RadSubgroupSelectorParameter()
				{
					ElementName = element.Name,
					ParameterName = parameter.DisplayName,
					Key = new ParameterKey(element.DataMinerID, element.ElementID, parameter.ID, instance?.IndexValue, instance?.DisplayValue),
				};
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

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
				ValidationText = "Select a valid element";
				IsValid = false;
			}
			else if (_parametersDropDown.Selected == null)
			{
				ValidationText = "Select a valid parameter";
				IsValid = false;
			}
			else if (_instanceDropDown.IsEnabled && _instanceDropDown.Selected == null)
			{
				ValidationText = "Select a valid instance";
				IsValid = false;
			}
			else
			{
				ValidationText = string.Empty;
				IsValid = true;
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
