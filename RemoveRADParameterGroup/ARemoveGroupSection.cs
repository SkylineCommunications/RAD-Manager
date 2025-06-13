namespace RemoveRADParameterGroup
{
	using System.Collections.Generic;
	using RadWidgets;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public abstract class AGroupRemoveSection : Section
	{
		public abstract RadGroupID GroupID { get; }

		public abstract bool RemoveGroup { get; }

		public abstract List<RadSubgroupID> GetSubgroupsToRemove();
	}
}
