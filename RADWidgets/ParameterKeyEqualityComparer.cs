namespace RadWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadParameterEqualityComparer : IEqualityComparer<RadParameter>
	{
		private readonly ParameterKeyEqualityComparer _parameterKeyComparer = new ParameterKeyEqualityComparer();

		public bool Equals(RadParameter x, RadParameter y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return string.Equals(x.Label, y.Label, System.StringComparison.OrdinalIgnoreCase) && _parameterKeyComparer.Equals(x.Key, y.Key);
		}

		public int GetHashCode(RadParameter parameter)
		{
			if (parameter == null)
				return 0;

			int hash = parameter.Label?.GetHashCode() ?? 0;
			hash ^= parameter.Key?.GetHashCode() ?? 0;

			return hash;
		}
	}

	public class ParameterKeyEqualityComparer : IEqualityComparer<ParameterKey>
	{
		public bool Equals(ParameterKey x, ParameterKey y) => x?.Equals(y) ?? false;

		public int GetHashCode(ParameterKey key) => key?.GetHashCode() ?? 0;
	}

	public class ParameterKeyListEqualityComparer : IEqualityComparer<List<ParameterKey>>
	{
		private readonly ParameterKeyEqualityComparer _parameterKeyComparer = new ParameterKeyEqualityComparer();

		public bool Equals(List<ParameterKey> x, List<ParameterKey> y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return x.SequenceEqual(y, _parameterKeyComparer);
		}

		public int GetHashCode(List<ParameterKey> key)
		{
			if (key == null)
				return 0;
			if (key.Count == 0)
				return 1;

			int hash = key.First()?.GetHashCode() ?? 0;
			for (int i = 1; i < key.Count; i++)
				hash ^= key[i]?.GetHashCode() ?? 0;

			return hash;
		}
	}
}
