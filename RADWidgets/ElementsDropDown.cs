namespace RadWidgets
{
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ElementsDropDown : DropDown<LiteElementInfoEvent>
	{
		public ElementsDropDown(IEngine engine)
		{
			IsDisplayFilterShown = true;
			IsSorted = true;
			MinWidth = 300;

			var elements = Utils.FetchElements(engine).Where(e => !e.IsDynamicElement).OrderBy(e => e.Name).ToList();
			Options = elements.Select(e => new Option<LiteElementInfoEvent>(e.Name, e));
			Changed += (sender, args) => OnChanged();
		}

		private void OnChanged()
		{
			Tooltip = Selected?.Name ?? string.Empty;
		}
	}
}
