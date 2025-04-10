﻿namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public abstract class ParameterSelectorBase<T> : MultiSelectorItemSelector<T>, IValidationWidget where T : MultiSelectorItem
	{
		private readonly IEngine engine_;
		private readonly DropDown<ParameterInfo> parametersDropDown_;
		private readonly TextBox instanceTextBox_;

		protected ParameterSelectorBase(IEngine engine, bool leaveFirstColEmpty)
		{
			engine_ = engine;

			var parametersLabel = new Label("Parameter");
			parametersDropDown_ = new DropDown<ParameterInfo>()
			{
				IsDisplayFilterShown = true,
				IsSorted = true,
			};
			parametersDropDown_.Changed += (sender, args) => OnSelectedParameterChanged();

			string instanceTooltip = "Specify the display key to include specific cells from the current table column. Use * and ? as wildcards.";
			var instanceLabel = new Label("Display key filter")
			{
				Tooltip = instanceTooltip,
			};
			instanceTextBox_ = new TextBox()
			{
				Tooltip = instanceTooltip,
			};
			instanceTextBox_.Changed += (sender, args) => OnInstanceChanged();

			int parametersCol = leaveFirstColEmpty ? 1 : 0;
			int parametersColSpan = leaveFirstColEmpty ? 1 : 2;
			AddWidget(parametersLabel, 0, parametersCol, 1, parametersColSpan);
			AddWidget(parametersDropDown_, 1, parametersCol, 1, parametersColSpan);
			AddWidget(instanceLabel, 0, 2);
			AddWidget(instanceTextBox_, 1, 2);
		}

		public event EventHandler<EventArgs> InstanceChanged;

		public UIValidationState ValidationState
		{
			get => instanceTextBox_.ValidationState;
			set => instanceTextBox_.ValidationState = value;
		}

		public string ValidationText
		{
			get => instanceTextBox_.ValidationText;
			set => instanceTextBox_.ValidationText = value;
		}

		protected IEngine Engine => engine_;

		protected DropDown<ParameterInfo> ParametersDropDown => parametersDropDown_;

		protected TextBox InstanceTextBox => instanceTextBox_;

		protected void OnSelectedParameterChanged()
		{
			var parameter = parametersDropDown_.Selected;
			parametersDropDown_.Tooltip = parameter?.DisplayName ?? string.Empty;
			instanceTextBox_.ValidationState = UIValidationState.Valid;
			if (parameter?.IsTableColumn != true)
			{
				instanceTextBox_.IsEnabled = false;
				instanceTextBox_.Text = string.Empty;
			}
			else
			{
				instanceTextBox_.IsEnabled = true;
			}
		}

		protected virtual bool IsValidForRAD(ParameterInfo info)
		{
			return info.IsRadSupported();
		}

		protected void SetPossibleParameters(GetProtocolInfoResponseMessage protocol)
		{
			if (protocol == null)
			{
				engine_.Log("Got invalid protocol", LogType.Error, 5);
				ClearPossibleParameters();
				return;
			}

			parametersDropDown_.Options = protocol.Parameters.Where(p => IsValidForRAD(p)).OrderBy(p => p.DisplayName).Select(p => new Option<ParameterInfo>(p.DisplayName, p));
			OnSelectedParameterChanged();
		}

		protected void ClearPossibleParameters()
		{
			parametersDropDown_.Options = new List<Option<ParameterInfo>>();
			OnSelectedParameterChanged();
		}

		private void OnInstanceChanged()
		{
			instanceTextBox_.ValidationState = UIValidationState.Valid;
			InstanceChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
