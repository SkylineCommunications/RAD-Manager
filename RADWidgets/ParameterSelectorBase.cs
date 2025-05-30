namespace RadWidgets
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public abstract class ParameterSelectorBase<T> : MultiSelectorItemSelector<T>, IValidationWidget where T : MultiSelectorItem
	{
		private readonly IEngine _engine;
		private readonly RadParametersDropDown _parametersDropDown;
		private readonly TextBox _instanceTextBox;

		protected ParameterSelectorBase(IEngine engine, bool leaveFirstColEmpty)
		{
			_engine = engine;

			var parametersLabel = new Label("Parameter");
			_parametersDropDown = new RadParametersDropDown(engine);
			_parametersDropDown.Changed += (sender, args) => OnSelectedParameterChanged();

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

			int parametersCol = leaveFirstColEmpty ? 1 : 0;
			int parametersColSpan = leaveFirstColEmpty ? 1 : 2;
			AddWidget(parametersLabel, 0, parametersCol, 1, parametersColSpan);
			AddWidget(_parametersDropDown, 1, parametersCol, 1, parametersColSpan);
			AddWidget(instanceLabel, 0, 2);
			AddWidget(_instanceTextBox, 1, 2);
		}

		public event EventHandler<EventArgs> InstanceChanged;

		public UIValidationState ValidationState
		{
			get => _instanceTextBox.ValidationState;
			set => _instanceTextBox.ValidationState = value;
		}

		public string ValidationText
		{
			get => _instanceTextBox.ValidationText;
			set => _instanceTextBox.ValidationText = value;
		}

		protected IEngine Engine => _engine;

		protected DropDown<ParameterInfo> ParametersDropDown => _parametersDropDown;

		protected TextBox InstanceTextBox => _instanceTextBox;

		protected void OnSelectedParameterChanged()
		{
			var parameter = _parametersDropDown.Selected;
			_instanceTextBox.ValidationState = UIValidationState.Valid;
			if (parameter?.IsTableColumn != true)
			{
				_instanceTextBox.IsEnabled = false;
				_instanceTextBox.Text = string.Empty;
			}
			else
			{
				_instanceTextBox.IsEnabled = true;
			}
		}

		protected void SetPossibleParameters(int dataMinerID, int elementID)
		{
			_parametersDropDown.SetPossibleParameters(dataMinerID, elementID);
			OnSelectedParameterChanged();
		}

		protected void SetPossibleParameters(string protocolName, string protocolVersion)
		{
			_parametersDropDown.SetPossibleParameters(protocolName, protocolVersion);
			OnSelectedParameterChanged();
		}

		protected void ClearPossibleParameters()
		{
			_parametersDropDown.ClearPossibleParameters();
			OnSelectedParameterChanged();
		}

		private void OnInstanceChanged()
		{
			_instanceTextBox.ValidationState = UIValidationState.Valid;
			InstanceChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
