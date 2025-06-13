namespace AddParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class MultiParameterPerProtocolSelector : VisibilitySection
	{
		private readonly IEngine _engine;
		private readonly DropDown<GetProtocolsResponseMessage> _protocolNameDropDown;
		private readonly DropDown<string> _protocolVersionDropDown;
		private readonly MultiProtocolParameterSelector _parameterSelector;

		public MultiParameterPerProtocolSelector(IEngine engine) : base()
		{
			_engine = engine;

			string protocolNameTooltip = "Make a parameter group for each element that uses this connector.";
			var protocolNameLabel = new Label("Connector")
			{
				Tooltip = protocolNameTooltip,
			};
			_protocolNameDropDown = new DropDown<GetProtocolsResponseMessage>()
			{
				Options = FetchProtocols().Where(p => !p.IsExportedProtocol).Select(p => new Option<GetProtocolsResponseMessage>(p.Protocol, p)),
				IsDisplayFilterShown = true,
				IsSorted = true,
				Tooltip = protocolNameTooltip,
			};
			_protocolNameDropDown.Changed += (sender, args) => OnSelectedProtocolChanged();

			string protocolVersionTooltip = "Make a parameter group for each element that uses this connector version.";
			var protocolVersionLabel = new Label("Connector version")
			{
				Tooltip = protocolVersionTooltip,
			};
			_protocolVersionDropDown = new DropDown<string>()
			{
				Options = new List<Option<string>>(),
				IsDisplayFilterShown = true,
				IsSorted = true,
				Tooltip = protocolVersionTooltip,
			};
			_protocolVersionDropDown.Changed += (sender, args) => OnSelectedProtocolVersionChanged();

			_parameterSelector = new MultiProtocolParameterSelector(string.Empty, string.Empty, engine);
			_parameterSelector.Changed += (sender, args) => Changed?.Invoke(this, EventArgs.Empty);
			OnSelectedProtocolChanged();

			AddWidget(protocolNameLabel, 0, 0);
			AddWidget(_protocolNameDropDown, 0, 1, 1, _parameterSelector.ColumnCount - 1);

			AddWidget(protocolVersionLabel, 1, 0);
			AddWidget(_protocolVersionDropDown, 1, 1, 1, _parameterSelector.ColumnCount - 1);

			AddSection(_parameterSelector, 2, 0);
		}

		public event EventHandler Changed;

		public string ProtocolName => _protocolNameDropDown.Selected?.Protocol;

		public string ProtocolVersion => _protocolVersionDropDown.Selected;

		public IEnumerable<ProtocolParameterSelectorInfo> GetSelectedParameters()
		{
			return _parameterSelector.GetSelected();
		}

		private static Option<string> GetProtocolVersionOption(string version)
		{
			if (version.StartsWith("Production"))
				return new Option<string>(version, "Production");
			else
				return new Option<string>(version);
		}

		private void OnSelectedProtocolVersionChanged()
		{
			var protocol = _protocolNameDropDown.Selected;
			var version = _protocolVersionDropDown.Selected;
			if (protocol == null || string.IsNullOrEmpty(version))
				return;

			_parameterSelector.SetProtocol(protocol.Protocol, version);
		}

		private void OnSelectedProtocolChanged()
		{
			var protocol = _protocolNameDropDown.Selected;
			if (protocol == null)
			{
				_protocolVersionDropDown.Options = new List<Option<string>>();
			}
			else
			{
				_protocolVersionDropDown.Options = protocol.Versions.OrderBy(v => v).Select(s => GetProtocolVersionOption(s)).ToList();
				var defaultSelection = _protocolVersionDropDown.Options.FirstOrDefault(v => v?.Value == "Production");
				if (defaultSelection != null)
					_protocolVersionDropDown.SelectedOption = defaultSelection;
			}

			OnSelectedProtocolVersionChanged();
		}

		private List<GetProtocolsResponseMessage> FetchProtocols()
		{
			try
			{
				var request = new GetInfoMessage(InfoType.Protocols);
				var responses = _engine.SendSLNetMessage(request);
				return responses.Select(r => r as GetProtocolsResponseMessage).Where(r => r != null).OrderBy(p => p.Protocol).ToList();
			}
			catch (Exception e)
			{
				_engine.Log($"Failed to fetch protocols: {e}", LogType.Error, 5);
				return new List<GetProtocolsResponseMessage>();
			}
		}
	}
}
