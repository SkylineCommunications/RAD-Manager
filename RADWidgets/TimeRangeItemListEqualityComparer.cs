namespace RadWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets;

	public class TimeRangeItemEqualityComparer : IEqualityComparer<TimeRangeItem>
	{
		public bool Equals(TimeRangeItem x, TimeRangeItem y) => x?.Equals(y) ?? false;

		public int GetHashCode(TimeRangeItem key) => key?.GetHashCode() ?? 0;
	}

	public class TimeRangeItemListEqualityComparer : IEqualityComparer<List<TimeRangeItem>>
	{
		private readonly TimeRangeItemEqualityComparer _timeRangeItemEqualityComparer = new TimeRangeItemEqualityComparer();

		public bool Equals(List<TimeRangeItem> x, List<TimeRangeItem> y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return x.SequenceEqual(y, _timeRangeItemEqualityComparer);
		}

		public int GetHashCode(List<TimeRangeItem> key)
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
