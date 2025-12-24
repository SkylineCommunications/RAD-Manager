namespace RadWidgets.Widgets
{
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class MultiParameterSelector : MultiSelector<ParameterSelectorInfo>
	{
		private bool _parameterAlreadySelected = false;

		public MultiParameterSelector(IEngine engine, RadHelper radHelper, ParametersCache parametersCache, IEnumerable<ParameterKey> parameters = null) :
			base(new ParameterSelector(engine, radHelper), null, "No parameters selected")
		{
			AddButtonTooltip = "Add the instance specified on the left to the relational anomaly group.";
			RemoveButtonTooltip = "Remove the instance(s) selected on the left from the relational anomaly group.";

			if (parameters != null)
			{
				var selection = new List<ParameterSelectorInfo>(parameters.Count());
				foreach (var parameter in parameters)
				{
					var element = engine.FindElement(parameter.DataMinerID, parameter.ElementID);
					var paramInfo = RadWidgets.Utils.FetchParameterInfo(engine, parametersCache, parameter.DataMinerID, parameter.ElementID, parameter.ParameterID);
					string displayKeyFilter = parameter.DisplayInstance;
					if (paramInfo?.IsTableColumn == true && string.IsNullOrEmpty(parameter.DisplayInstance))
						displayKeyFilter = parameter.Instance;

					selection.Add(new ParameterSelectorInfo()
					{
						ElementName = element?.ElementName ?? "Unknown element",
						ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
						DataMinerID = parameter.DataMinerID,
						ElementID = parameter.ElementID,
						ParameterID = parameter.ParameterID,
						DisplayKeyFilter = displayKeyFilter,
						MatchingInstances = new List<DynamicTableIndex>() { new DynamicTableIndex(parameter.Instance, parameter.DisplayInstance) },
						IsTableColumn = paramInfo?.IsTableColumn ?? false,
					});
				}

				SetSelected(selection);
			}

			var selector = ItemSelector as ParameterSelector;
			selector.Changed += (sender, args) => OnChanged();
			Changed += (sender, args) => OnChanged();
		}

		public IEnumerable<ParameterKey> GetSelectedParameters()
		{
			return GetSelected().SelectMany(i => i.GetParameterKeys()).Distinct(new ParameterKeyEqualityComparer());
		}

		protected override bool AddItem(ParameterSelectorInfo item)
		{
			var newParameters = item.GetParameterKeys().ToList();
			if (!newParameters.Except(GetSelectedParameters()).Any())
			{
				var selector = ItemSelector as ParameterSelector;
				selector.ValidationState = UIValidationState.Invalid;
				selector.ValidationText = newParameters.Count > 1 ? "Parameters already selected" : "Parameter already selected";
				_parameterAlreadySelected = true;
				return false;
			}

			return base.AddItem(item);
		}

		private void OnChanged()
		{
			if (_parameterAlreadySelected)
			{
				// If an item has been added or removed, then the validation state of the selector should be reset
				var selector = ItemSelector as ParameterSelector;
				selector.ValidationState = UIValidationState.Valid;
				selector.ValidationText = string.Empty;
				_parameterAlreadySelected = false;
			}
		}
	}
}
