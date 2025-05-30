namespace RadWidgets
{
	using System;
	using System.Runtime.CompilerServices;
	using Skyline.DataMiner.Analytics.Rad;//TODO: am I allowed to include RAD here?
	using Skyline.DataMiner.Automation;

	public static class Compatibility
	{
		private static bool? _hasSharedModelGroups = null;

		/// <summary>
		/// Returns true if there are shared model groups in the current DataMiner, these are available from version TODO: version.
		/// </summary>
		/// <param name="engine">The engine object.</param>
		/// <returns>True is shared model groups are available, false otherwise.</returns>
		public static bool HasSharedModelGroups(IEngine engine)
		{
			if (_hasSharedModelGroups.HasValue)
				return _hasSharedModelGroups.Value;

			try
			{
				engine.Log("Checking for shared model groups support in DataMiner version...", LogType.Information, 0);
				CheckAddRADSharedModelGroupMessage(engine);
				engine.Log("Shared model group support found in this DataMiner version.", LogType.Information, 0);
				_hasSharedModelGroups = true;
				return true;
			}
			catch (TypeLoadException)
			{
				engine.Log("No shared model group support in this DataMiner version.", LogType.Information, 0);
				_hasSharedModelGroups = false;
				return false;
			}
		}

		/// <summary>
		/// Supported from TODO: version.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void CheckAddRADSharedModelGroupMessage(IEngine engine)
		{
			Type type = typeof(AddRADSharedModelGroupMessage);
			engine.Log($"Found type {type.FullName}", LogType.Information, 5);
		}
	}
}
