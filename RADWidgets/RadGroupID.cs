namespace RadWidgets
{
	using System;

	public interface IRadGroupID
	{
		int DataMinerID { get; set; }

		string GroupName { get; set; }
	}

	public struct RadGroupID : IRadGroupID
	{
		public RadGroupID(int dataMinerID, string groupName)
		{
			DataMinerID = dataMinerID;
			GroupName = groupName;
		}

		public int DataMinerID { get; set; }

		public string GroupName { get; set; }
	}

	public struct RadSubgroupID : IRadGroupID
	{
		public RadSubgroupID(int dataMinerID, string groupName, Guid subgroupID)
		{
			DataMinerID = dataMinerID;
			GroupName = groupName;
			SubgroupID = subgroupID;
			SubgroupName = null;
		}

		public RadSubgroupID(int dataMinerID, string groupName, string subgroupName)
		{
			DataMinerID = dataMinerID;
			GroupName = groupName;
			SubgroupID = null;
			SubgroupName = subgroupName;
		}

		public int DataMinerID { get; set; }

		public string GroupName { get; set; }

		public Guid? SubgroupID { get; set; }

		public string SubgroupName { get; set; }
	}
}
