namespace RasDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Select nth row")]
	public class NthOperator : IGQIRowOperator, IGQIInputArguments, IGQIOnInit
	{
		private Dictionary<List<object>, int> _groupCounts;
		private GQIColumn[] _groupColumns;
		private int _nth;

		private GQIColumnListArgument _groupColumnsArg = new GQIColumnListArgument("Group on columns")
		{
			IsRequired = true,
		};

		private GQIIntArgument _nthArg = new GQIIntArgument("Nth row")
		{
			DefaultValue = 1,
			IsRequired = false,
		};

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_groupCounts = new Dictionary<List<object>, int>(new ListValuesComparer());
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _groupColumnsArg, _nthArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_groupColumnsArg, out _groupColumns) || _groupColumns == null || _groupColumns.Length == 0)
				throw new ArgumentException($"Required argument '{_groupColumnsArg.Name}' was not provided or empty");

			if (!args.TryGetArgumentValue(_nthArg, out _nth) || _nth <= 0)
				throw new ArgumentException($"Required argument '{_nthArg.Name}' was not provided or invalid");

			return default;
		}

		public void HandleRow(GQIEditableRow row)
		{
			if (row == null)
				return;

			var values = _groupColumns.Select(c => row.GetValue(c.Name)).ToList();
			if (!_groupCounts.TryGetValue(values, out int count))
				_groupCounts[values] = count = 0;

			if (count + 1 != _nth)
				row.Delete();

			_groupCounts[values]++;
		}
	}

	public class ListValuesComparer : IEqualityComparer<List<object>>
	{
		public bool Equals(List<object> x, List<object> y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			if (x.Count != y.Count)
				return false;

			for (int i = 0; i < x.Count; i++)
			{
				if (!object.Equals(x[i], y[i]))
					return false;
			}

			return true;
		}

		public int GetHashCode(List<object> l)
		{
			if (l == null)
				return 0;
			if (l.Count == 0)
				return 1;

			int hash = l.First()?.GetHashCode() ?? 0;
			for (int i = 1; i < l.Count; i++)
				hash ^= l[i]?.GetHashCode() ?? 0;

			return hash;
		}
	}
}
