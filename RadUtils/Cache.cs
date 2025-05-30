namespace RadUtils
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public struct CacheRecord<T>
	{
		public CacheRecord(T value, DateTime timestamp)
		{
			Value = value;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets or sets the value of the cache record.
		/// </summary>
		public T Value { get; set; }

		/// <summary>
		/// Gets or sets the time when the record was last accessed.
		/// </summary>
		public DateTime Timestamp { get; set; }
	}

	/// <summary>
	/// Cache values based on element key. Values will be stored in the cache upon calling <seealso cref="TryGet"/> and the abstract <seealso cref="Fetch"/> method is used to fetch the values.
	/// </summary>
	/// <typeparam name="T">The value to be cached.</typeparam>
	public abstract class Cache<T>
	{
		private readonly Dictionary<ElementKey, CacheRecord<T>> _cache = new Dictionary<ElementKey, CacheRecord<T>>();
		private int _maxCacheSize;

		protected Cache(int maxCacheSize)
		{
			_maxCacheSize = maxCacheSize;
		}

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
			if (_cache.TryGetValue(key, out var cacheRecord))
			{
				value = cacheRecord.Value;
				cacheRecord.Timestamp = DateTime.UtcNow;
				return true;
			}

			if (Fetch(dataMinerID, elementID, out value))
			{
				_cache[key] = new CacheRecord<T>(value, DateTime.UtcNow);
				if (_cache.Count > _maxCacheSize)
					Clean();
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
			if (_cache.TryGetValue(key, out var cacheRecord))
			{
				value = cacheRecord.Value;
				cacheRecord.Timestamp = DateTime.UtcNow;
				return true;
			}
			else
			{
				value = default;
				return false;
			}
		}

		/// <summary>
		/// Tries to fetch the data from the source.
		/// </summary>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID of the element.</param>
		/// <param name="value">Variable to store the resulting value.</param>
		/// <returns>True if the value could be fetched, false otherwise.</returns>
		protected abstract bool Fetch(int dataMinerID, int elementID, out T value);

		private void Clean()
		{
			var keysToRemove = _cache.OrderBy(kvp => kvp.Value.Timestamp).Skip(_maxCacheSize / 2).ToList();
			foreach (var key in keysToRemove)
				_cache.Remove(key.Key);
		}

		private struct ElementKey
		{
			public int DataMinerID { get; set; }

			public int ElementID { get; set; }
		}
	}
}
