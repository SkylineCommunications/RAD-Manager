namespace RadWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;

	public class ParameterKeyEqualityComparer : IEqualityComparer<ParameterKey>
	{
		public bool Equals(ParameterKey x, ParameterKey y) => x?.Equals(y) ?? false;

		public int GetHashCode(ParameterKey key) => key?.GetHashCode() ?? 0;
	}

	public class ParameterKeyListEqualityComparer : IEqualityComparer<List<ParameterKey>>
	{
		public bool Equals(List<ParameterKey> x, List<ParameterKey> y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return x.SequenceEqual(y, new ParameterKeyEqualityComparer());
		}

		public int GetHashCode(List<ParameterKey> key)
		{
			if (key == null)
				return 0;
			if (key.Count() == 0)
				return 1;

			int hash = key.First()?.GetHashCode() ?? 0;
			for (int i = 1; i < key.Count(); i++)
				hash ^= key[i]?.GetHashCode() ?? 0;

			return hash;
		}
	}
}
