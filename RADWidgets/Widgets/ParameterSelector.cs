namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ParameterSelectorInfo : SelectorItem
	{
		public string ElementName { get; set; }

		public string ParameterName { get; set; }

		public int DataMinerID { get; set; }

		public int ElementID { get; set; }

		public int ParameterID { get; set; }

		public string DisplayKeyFilter { get; set; }

		public bool IsTableColumn { get; set; }

		/// <summary>
		/// Gets or sets a list of instance primary keys for which the display key matches the provided filter.
		/// </summary>
		public List<DynamicTableIndex> MatchingInstances { get; set; }

		public override string GetKey()
		{
			if (!string.IsNullOrEmpty(DisplayKeyFilter))
				return $"{DataMinerID}/{ElementID}/{ParameterID}/{DisplayKeyFilter}";
			else
				return $"{DataMinerID}/{ElementID}/{ParameterID}";
		}

		public override string GetDisplayValue()
		{
			if (IsTableColumn)
			{
				if (MatchingInstances.Count != 1 || !string.Equals(MatchingInstances[0].DisplayValue, DisplayKeyFilter, StringComparison.OrdinalIgnoreCase))
					return $"{ElementName}/{ParameterName}/{DisplayKeyFilter} ({MatchingInstances.Count} matching instances)";
				else
					return $"{ElementName}/{ParameterName}/{DisplayKeyFilter}";
			}
			else
			{
				return $"{ElementName}/{ParameterName}";
			}
		}

		public IEnumerable<ParameterKey> GetParameterKeys()
		{
			if (MatchingInstances?.Count > 0)
				return MatchingInstances.Select(i => new ParameterKey(DataMinerID, ElementID, ParameterID, i.IndexValue)).ToList();
			else
				return new List<ParameterKey> { new ParameterKey(DataMinerID, ElementID, ParameterID) };
		}
	}

	public class ParameterSelector : MultiSelectorItemSelector<ParameterSelectorInfo>, IValidationWidget
	{
		private readonly IEngine _engine;
		private readonly ElementsDropDown _elementsDropDown;
		private readonly RadParametersDropDown _parametersDropDown;
		private readonly TextBox _instanceTextBox;
		private UIValidationState _validationState = UIValidationState.Valid;
		private string _validationText = string.Empty;
		private bool _hasInvalidInstance = false;
		private bool _initial;

		public ParameterSelector(IEngine engine)
		{
			_engine = engine;
			_initial = true;

			var elementsLabel = new Label("Element");
			_elementsDropDown = new ElementsDropDown(engine);
			_elementsDropDown.Changed += (sender, args) => OnSelectedElementChanged(false);

			var parametersLabel = new Label("Parameter");
			_parametersDropDown = new RadParametersDropDown(engine);
			_parametersDropDown.Changed += (sender, args) => OnSelectedParameterChanged(false);

			string instanceTooltip = "Specify the display key to include specific cells from the current table column. Use * and ? as wildcards.";
			var instanceLabel = new Label("Display key filter")
			{
				Tooltip = instanceTooltip,
			};
			_instanceTextBox = new TextBox()
			{
				Tooltip = instanceTooltip,
			};
			_instanceTextBox.Changed += (sender, args) => OnInstanceChanged();

			OnSelectedElementChanged(true);

			AddWidget(elementsLabel, 0, 0);
			AddWidget(_elementsDropDown, 1, 0);
			AddWidget(parametersLabel, 0, 1);
			AddWidget(_parametersDropDown, 1, 1);
			AddWidget(instanceLabel, 0, 2);
			AddWidget(_instanceTextBox, 1, 2);
		}

		public event EventHandler<EventArgs> Changed;

		public UIValidationState ValidationState
		{
			get => _validationState;
			set
			{
				if (_validationState == value)
					return;

				_validationState = value;
				UpdateValidationState();
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
				UpdateValidationState();
			}
		}

		public override ParameterSelectorInfo SelectedItem
		{
			get
			{
				var element = _elementsDropDown.Selected;
				if (element == null)
				{
					UpdateValidationState();
					return null;
				}

				var parameter = _parametersDropDown.Selected;
				if (parameter == null)
				{
					UpdateValidationState();
					return null;
				}

				var matchingInstances = new List<DynamicTableIndex>();
				if (parameter.IsTableColumn && parameter.ParentTable != null)
				{
					matchingInstances = Utils.FetchInstancesWithTrending(_engine, element.DataMinerID, element.ElementID, parameter, _instanceTextBox.Text).ToList();
					if (matchingInstances.Count == 0)
					{
						_hasInvalidInstance = true;
						UpdateValidationState();
						return null;
					}
				}

				return new ParameterSelectorInfo
				{
					ElementName = element.Name,
					ParameterName = parameter.DisplayName,
					DataMinerID = element.DataMinerID,
					ElementID = element.ElementID,
					ParameterID = parameter.ID,
					DisplayKeyFilter = parameter.IsTableColumn ? _instanceTextBox.Text : string.Empty,
					MatchingInstances = matchingInstances,
					IsTableColumn = parameter.IsTableColumn,
				};
			}
		}

		private void OnSelectedParameterChanged(bool initial)
		{
			var parameter = _parametersDropDown.Selected;
			if (parameter?.IsTableColumn != true)
			{
				_instanceTextBox.IsEnabled = false;
				_instanceTextBox.Text = string.Empty;
			}
			else
			{
				_instanceTextBox.IsEnabled = true;
			}

			Changed?.Invoke(this, EventArgs.Empty);
			_hasInvalidInstance = false;
			_initial = initial;
			UpdateValidationState();
		}

		private void UpdateValidationState()
		{
			if (_elementsDropDown.Selected == null)
			{
				_elementsDropDown.ValidationState = UIValidationState.Invalid;
				_elementsDropDown.ValidationText = "Select a valid element";
				_parametersDropDown.ValidationState = UIValidationState.Valid;
				_parametersDropDown.ValidationText = string.Empty;
				_instanceTextBox.ValidationState = UIValidationState.Valid;
				_instanceTextBox.ValidationText = string.Empty;
			}
			else if (_parametersDropDown.Selected == null)
			{
				_elementsDropDown.ValidationState = UIValidationState.Valid;
				_elementsDropDown.ValidationText = string.Empty;
				_parametersDropDown.ValidationState = UIValidationState.Invalid;
				_parametersDropDown.ValidationText = "Select a valid parameter";
				_instanceTextBox.ValidationState = UIValidationState.Valid;
				_instanceTextBox.ValidationText = string.Empty;
			}
			else if (_hasInvalidInstance)
			{
				_elementsDropDown.ValidationState = UIValidationState.Valid;
				_elementsDropDown.ValidationText = string.Empty;
				_parametersDropDown.ValidationState = UIValidationState.Valid;
				_parametersDropDown.ValidationText = string.Empty;
				_instanceTextBox.ValidationState = UIValidationState.Invalid;
				_instanceTextBox.ValidationText = "No matching instances found";
			}
			else
			{
				_elementsDropDown.ValidationState = UIValidationState.Valid;
				_elementsDropDown.ValidationText = string.Empty;

				if (_instanceTextBox.IsEnabled)
				{
					_instanceTextBox.ValidationState = _validationState;
					_instanceTextBox.ValidationText = _validationText;
					_parametersDropDown.ValidationState = UIValidationState.Valid;
					_parametersDropDown.ValidationText = string.Empty;
				}
				else
				{
					_instanceTextBox.ValidationState = UIValidationState.Valid;
					_instanceTextBox.ValidationText = string.Empty;
					_parametersDropDown.ValidationState = _validationState;
					_parametersDropDown.ValidationText = _validationText;
				}
			}
		}

		private void SetPossibleParameters(int dataMinerID, int elementID, bool initial)
		{
			_parametersDropDown.SetPossibleParameters(dataMinerID, elementID);
			OnSelectedParameterChanged(initial);
		}

		private void ClearPossibleParameters()
		{
			_parametersDropDown.ClearPossibleParameters();
			OnSelectedParameterChanged(false);
		}

		private void OnInstanceChanged()
		{
			_hasInvalidInstance = false;
			_initial = false;
			UpdateValidationState();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		private void OnSelectedElementChanged(bool initial)
		{
			var element = _elementsDropDown.Selected;
			if (element == null)
			{
				ClearPossibleParameters();
				return;
			}

			SetPossibleParameters(element.DataMinerID, element.ElementID, initial);
			UpdateValidationState();
		}
	}
}
