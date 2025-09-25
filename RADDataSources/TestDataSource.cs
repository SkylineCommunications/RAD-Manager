namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Rad;
	using Skyline.DataMiner.Net.Enums;
	using Skyline.DataMiner.Net.Filters;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.MetaData.DataClass;
	using Skyline.DataMiner.Utils.RadToolkit;

	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	[GQIMetaData(Name = "Get Test Data")]//TODO: remove this and fix the internal data sources
	public class TestDataSource : IGQIDataSource, IGQIOnInit
	{
		private Random _random;
		private GQIDMS _dms;
		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_random = new Random();
			_dms = args.DMS;
			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIDateTimeColumn("Start Time"),
				new GQIDateTimeColumn("End Time"),
				new GQIDoubleColumn("Anomaly Score"),
				new GQIStringColumn("Group Name"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var request = new GetRADParameterGroupsMessage();
			var response = _dms.SendMessage(request) as GetRADParameterGroupsResponseMessage;

			List<GQIRow> rows = new List<GQIRow>();
			foreach (var group in response.GroupNames)
			{
				if (group == "Group with error")
					continue;

				for (int i = 0; i < 10; ++i)
				{
					var startTime = DateTime.UtcNow.AddMinutes(-10 * (i + 1));
					var endTime = DateTime.UtcNow.AddMinutes(-10 * i);
					var row = new GQIRow(new GQICell[]
					{
					new GQICell() { Value = startTime },
					new GQICell() { Value = endTime },
					new GQICell() { Value = Math.Round(_random.NextDouble() * 10, 1) },
					new GQICell() { Value = group },
					});
					row.Metadata = new GenIfRowMetadata(new RowMetadataBase[]
					{
					new TimeRangeMetadata()
					{
						StartTime = startTime,
						EndTime = endTime,
					},
					});
					rows.Add(row);
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = false,
			};
		}
	}
}