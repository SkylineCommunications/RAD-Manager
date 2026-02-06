namespace RadWidgets.Widgets
{
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class ElementsDropDown : TooltipDropDown<LiteElementInfoEvent>
	{
		public ElementsDropDown(IEngine engine, RadHelper radHelper)
		{
			IsDisplayFilterShown = true;
			IsSorted = true;
			MinWidth = 300;

			IEnumerable<LiteElementInfoEvent> elements = Utils.FetchElements(engine);
			if (!radHelper.AllowSharedModelGroups)
				elements = elements.Where(e => !e.IsDynamicElement);
			elements = elements.OrderBy(e => e.Name).ToList();
			Options = elements.Select(e => new Option<LiteElementInfoEvent>(e.Name, e));
		}
	}
}
