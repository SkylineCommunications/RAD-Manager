namespace RadDataSourceUtils
{
	using System.Collections.Generic;

	/// <summary>
	/// Cache values based on element key. Values will be stored in the cache upon calling <seealso cref="TryGet"/> and the abstract <seealso cref="Fetch"/> method is used to fetch the values.
	/// </summary>
	/// <typeparam name="T">The value to be cached.</typeparam>
	public abstract class Cache<T>
	{
		private readonly Dictionary<ElementKey, T> cache_ = new Dictionary<ElementKey, T>();

		/// <summary>
		/// Tries to get the value from the cache, or by fetching the data. If the data could be fetched, it will be stored in the cache.
		/// </summary>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID.</param>
		/// <param name="value">Variable to store the resulting value.</param>
		/// <returns>True if the value was in cache or could be fetched, false if we could not fetch the value.</returns>
		public bool TryGet(int dataMinerID, int elementID, out T value)
		{
			var key = new ElementKey { DataMinerID = dataMinerID, ElementID = elementID };
			if (cache_.TryGetValue(key, out value))
				return true;

			if (Fetch(dataMinerID, elementID, out value))
			{
				cache_[key] = value;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tries to get the value from the cache. If the value is not in cache, we will return false.
		/// </summary>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID.</param>
		/// <param name="value">Variable to store the resulting value.</param>
		/// <returns>True if the value was in cache, false if not.</returns>
		public bool TryGetFromCache(int dataMinerID, int elementID, out T value)
		{
			var key = new ElementKey { DataMinerID = dataMinerID, ElementID = elementID };
			return cache_.TryGetValue(key, out value);
		}

		/// <summary>
		/// Tries to fetches the data from the source.
		/// </summary>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID of the element.</param>
		/// <param name="value">Variable to store the resulting value.</param>
		/// <returns>True if the value could be fetched, false otherwise.</returns>
		protected abstract bool Fetch(int dataMinerID, int elementID, out T value);

		private struct ElementKey
		{
			public int DataMinerID { get; set; }

			public int ElementID { get; set; }
		}
	}
}
