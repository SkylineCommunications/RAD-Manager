namespace RadWidgets.Widgets
{
	using System.Linq;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ElementsDropDown : TooltipDropDown<LiteElementInfoEvent>
	{
		public ElementsDropDown(IEngine engine)
		{
			IsDisplayFilterShown = true;
			IsSorted = true;
			MinWidth = 300;

			var elements = Utils.FetchElements(engine).Where(e => !e.IsDynamicElement).OrderBy(e => e.Name).ToList();
			Options = elements.Select(e => new Option<LiteElementInfoEvent>(e.Name, e));
		}
	}
}
