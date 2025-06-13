namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadParametersDropDown : DropDown<ParameterInfo>
	{
		private readonly IEngine _engine;

		public RadParametersDropDown(IEngine engine)
		{
			_engine = engine;

			IsDisplayFilterShown = true;
			IsSorted = true;
			Changed += (sender, args) => OnChanged();
		}

		public void ClearPossibleParameters()
		{
			Options = new List<Option<ParameterInfo>>();
		}

		public void SetPossibleParameters(int dataMinerID, int elementID)
		{
			var protocol = Utils.FetchElementProtocol(_engine, dataMinerID, elementID);
			SetPossibleParameters(protocol, p => p.IsRadSupported() && p.HasTrending());
		}

		public void SetPossibleParameters(string protocolName, string protocolVersion)
		{
			if (string.IsNullOrEmpty(protocolName) || string.IsNullOrEmpty(protocolVersion))
			{
				ClearPossibleParameters();
				return;
			}

			var protocol = Utils.FetchProtocol(_engine, protocolName, protocolVersion);
			SetPossibleParameters(protocol, p => p.IsRadSupported());
		}

		private void SetPossibleParameters(GetProtocolInfoResponseMessage protocol, Predicate<ParameterInfo> predicate)
		{
			if (protocol == null)
			{
				ClearPossibleParameters();
				return;
			}

			Options = protocol.Parameters.Where(p => predicate(p)).OrderBy(p => p.DisplayName).Select(p => new Option<ParameterInfo>(p.DisplayName, p));
		}

		private void OnChanged()
		{
			Tooltip = Selected?.DisplayName ?? string.Empty;
		}
	}
}
