namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime;
	using System.Text;
	using System.Text.RegularExpressions;
	using RadUtils;
	using RadWidgets.Widgets;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public static class Utils
	{
		/// <summary>
		/// Get the group name, DataMiner ID and (if provided) subgroup ID from the script parameters.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <returns>A list of IRadGroupIDs representing the IDs of the groups provided in the script arguments.</returns>
		/// <exception cref="FormatException">Thrown when the provided group IDs could not be parsed.</exception>
		public static List<IRadGroupID> ParseGroupIDParameter(InteractiveController app)
		{
			var groupIDs = ParseScriptParameterValue(app.Engine.GetScriptParam("Group ID")?.Value);

			var result = new List<IRadGroupID>();
			Regex regex = new Regex(@"^(?<DataMinerID>\d+)/(?<GroupName>.+)/(?<SubgroupID>[-A-Za-z0-9]*?)$");
			foreach (var groupIDStr in groupIDs)
			{
				var match = regex.Match(groupIDStr);
				if (!match.Success)
					throw new FormatException($"Group ID parameter '{groupIDStr}' is not in the correct format. Expected format: 'DataMinerID/GroupName/SubgroupID'");

				if (!int.TryParse(match.Groups["DataMinerID"].Value, out int dataMinerID))
					throw new FormatException($"DataMinerID parameter is not a valid number, got '{match.Groups["DataMinerID"].Value}'");
				string groupName = match.Groups["GroupName"].Value;
				if (string.IsNullOrEmpty(match.Groups["SubgroupID"].Value))
					result.Add(new RadGroupID(dataMinerID, groupName));
				else if (Guid.TryParse(match.Groups["SubgroupID"].Value, out var subgroupID))
					result.Add(new RadSubgroupID(dataMinerID, groupName, subgroupID));
				else
					throw new FormatException($"SubgroupID parameter is not a valid GUID, got '{match.Groups["SubgroupID"].Value}'");
			}

			return result;
		}

		/// <summary>
		/// Parses the provided script parameter value to a list of strings.
		/// </summary>
		/// <param name="parameterValue">The parameter value.</param>
		/// <returns>The parsed values.</returns>
		public static List<string> ParseScriptParameterValue(string parameterValue)
		{
			if (parameterValue == null)
				return new List<string>();
			if (!parameterValue.StartsWith("[") || !parameterValue.EndsWith("]") || parameterValue == "[]")
				return new List<string>();

			var values = new List<string>();

			bool inQuotes = false;
			bool escaped = false;
			var curValue = new StringBuilder(parameterValue.Length);
			for (int i = 1; i < parameterValue.Length - 1; ++i)
			{
				if (parameterValue[i] == '"' && !escaped)
				{
					inQuotes = !inQuotes;
				}
				else if (parameterValue[i] == '\\' && !escaped)
				{
					escaped = true;
				}
				else if (parameterValue[i] == ',' && !inQuotes)
				{
					values.Add(curValue.ToString());
					curValue.Clear();
				}
				else
				{
					escaped = false;
					curValue.Append(parameterValue[i]);
				}
			}

			values.Add(curValue.ToString());
			return values;
		}

		/// <summary>
		/// Shows a message dialog with the given title and message. The script will exit when the OK button is pressed.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="message">The message to display on the dialog.</param>
		public static void ShowMessageDialog(InteractiveController app, string title, string message)
		{
			var dialog = new MessageDialog(app.Engine, message);
			dialog.Title = title;
			dialog.OkButton.Pressed += (s, args) => app.Engine.ExitSuccess(message);

			app.ShowDialog(dialog);
		}

		/// <summary>
		/// Shows an excpetion dialog with the given title and message. The script will exit when the OK button is pressed.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="ex">The exception to display in the dialog.</param>
		/// <param name="parent">Parent dialog to show (again) after pressing OK. If null, the script will exit after pressing OK.</param>
		public static void ShowExceptionDialog(InteractiveController app, string title, Exception ex, Dialog parent = null)
		{
			var exceptionDialog = new ExceptionDialog(app.Engine, ex);
			exceptionDialog.Title = title;
			if (parent == null)
			{
				exceptionDialog.OkButton.Pressed += (s, args) => app.Engine.ExitSuccess(title);
				exceptionDialog.ShowScriptAbortPopup = false;
			}
			else
			{
				exceptionDialog.OkButton.Pressed += (s, args) => app.ShowDialog(parent);
			}

			app.ShowDialog(exceptionDialog);
		}

		/// <summary>
		/// Fetches all elements (including hidden, paused, stopped and service elements) from the DataMiner system. Returns an empty list and logs an exception if the elements could not be fetched.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <returns>A list of elements, or an empty list when an error occured..</returns>
		public static List<LiteElementInfoEvent> FetchElements(IEngine engine)
		{
			try
			{
				var request = new GetLiteElementInfo()
				{
					IncludeHidden = true,
					IncludePaused = true,
					IncludeStopped = true,
					IncludeServiceElements = true,
				};
				return engine.SendSLNetMessage(request).Select(r => r as LiteElementInfoEvent).Where(r => r != null).ToList();
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch elements: {e}");
				return new List<LiteElementInfoEvent>();
			}
		}

		/// <summary>
		/// Fetch the protocol for the given element. Returns null and logs an exception if the protocol could not be fetched.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID.</param>
		/// <returns>The element protocol, or null if it could not be fetched.</returns>
		public static GetElementProtocolResponseMessage FetchElementProtocol(IEngine engine, int dataMinerID, int elementID)
		{
			try
			{
				var request = new GetElementProtocolMessage(dataMinerID, elementID);
				return engine.SendSLNetSingleResponseMessage(request) as GetElementProtocolResponseMessage;
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch protocol for element {dataMinerID}/{elementID}: {e}");
				return null;
			}
		}

		/// <summary>
		/// Fetches the parameter info for the given element and parameter ID. Returns null and logs an exception if the parameter info could not be fetched.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="cache">The parameters cache.</param>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID.</param>
		/// <param name="parameterID">The parameter ID.</param>
		/// <returns>The parameter info, or null if the parameter could not be found.</returns>
		public static ParameterInfo FetchParameterInfo(IEngine engine, ParametersCache cache, int dataMinerID, int elementID, int parameterID)
		{
			if (!cache.TryGet(dataMinerID, elementID, out var parameterInfos))
				return null;

			var paramInfo = parameterInfos?.FirstOrDefault(p => p.ID == parameterID);
			if (paramInfo == null)
			{
				engine.Log($"Could not find parameter {parameterID} in protocol for element {dataMinerID}/{elementID}");
				return null;
			}

			return paramInfo;
		}

		/// <summary>
		/// Fetches the protocol information for the given protocol name and version. Returns null and logs an exception if the protocol could not be fetched.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="protocolName">The name of the protocol.</param>
		/// <param name="protocolVersion">The version of the protocol.</param>
		/// <returns>The protocol information, or null.</returns>
		public static GetProtocolInfoResponseMessage FetchProtocol(IEngine engine, string protocolName, string protocolVersion)
		{
			try
			{
				var request = new GetProtocolMessage(protocolName, protocolVersion);
				return engine.SendSLNetSingleResponseMessage(request) as GetProtocolInfoResponseMessage;
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch protocol with name '{protocolName}' and version '{protocolVersion}': {e}");
				return null;
			}
		}

		/// <summary>
		/// Fetches all trended instances of a table based on the given DataMiner ID, element ID and parameter info of a table column.
		/// Returns an empty list and logs an exception if the instances could not be fetched.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="dataMinerID">The DataMiner ID.</param>
		/// <param name="elementID">The element ID.</param>
		/// <param name="parameterInfo">The parameter info of the table column.</param>
		/// <param name="displayKeyFilter">The filter to use on the display keys.</param>
		/// <returns>A list of all matching, trended parameters. Or an empty list when an error occured.</returns>
		public static IEnumerable<DynamicTableIndex> FetchInstancesWithTrending(IEngine engine, int dataMinerID, int elementID, ParameterInfo parameterInfo, string displayKeyFilter = null)
		{
			return FetchInstances(engine, dataMinerID, elementID, parameterInfo.ParentTablePid, displayKeyFilter)
				.Where(i => parameterInfo.IsRealTimeTrended(i.DisplayValue) || parameterInfo.IsAverageTrended(i.DisplayValue));
		}

		/// <summary>
		/// Convert a parameter to a string of the form "ElementName/ParameterName/Instance". If the parameter has no instance, it will be "ElementName/ParameterName".
		/// </summary>
		/// <param name="key">The parameter key.</param>
		/// <param name="engine">The engine object.</param>
		/// <param name="parametersCache">The cache object.</param>
		/// <returns>A string representing the current parameter key.</returns>
		public static string ToHumanReadableString(this ParameterKey key, IEngine engine, ParametersCache parametersCache)
		{
			if (key == null)
				return string.Empty;

			var element = engine.FindElement(key.DataMinerID, key.ElementID);
			string elementName = element?.ElementName ?? $"{key.DataMinerID}/{key.ElementID}";
			var paramInfo = FetchParameterInfo(engine, parametersCache, key.DataMinerID, key.ElementID, key.ParameterID);
			string parameterName = paramInfo?.DisplayName ?? key.ParameterID.ToString();
			if (!string.IsNullOrEmpty(key.DisplayInstance))
				return $"{elementName}/{parameterName}/{key.DisplayInstance}";
			else if (!string.IsNullOrEmpty(key.Instance))
				return $"{elementName}/{parameterName}/{key.Instance}";
			else
				return $"{elementName}/{parameterName}";
		}

		/// <summary>
		/// Gets the parameter description of a subgroup. This is a human readable string containing the parameters in the group.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="parametersCache">The parameters cache.</param>
		/// <param name="info">The subgroup info.</param>
		/// <returns>The parameter description.</returns>
		public static string GetParameterDescription(IEngine engine, ParametersCache parametersCache, RadSubgroupInfo info)
		{
			if (info == null)
				return string.Empty;

			var parameterStrs = new List<string>();
			foreach (var p in info.Parameters)
				parameterStrs.Add(p?.Key.ToHumanReadableString(engine, parametersCache));

			return parameterStrs.Select(s => $"'{s}'").HumanReadableJoin();
		}

		/// <summary>
		/// Return true if <paramref name="a"/> has the same parameters as <paramref name="b"/>, taking the order into account. For a good comparison,
		/// the parameters need to be normalized with <see cref="NormalizeParameters(RadSubgroupSettings)"/>.
		/// </summary>
		/// <param name="a">The first group settings.</param>
		/// <param name="b">The second group settings.</param>
		/// <returns>True if both groups has the same parameter, false otherwise.</returns>
		public static bool HasSameOrderedParameters(this RadSubgroupSettings a, RadSubgroupSettings b)
		{
			if (a?.Parameters == null && b?.Parameters == null)
				return true;
			if (a?.Parameters == null || b?.Parameters == null)
				return false;

			return a.Parameters.SequenceEqual(b.Parameters, new RadParameterEqualityComparer());
		}

		/// <summary>
		/// Normalizes the parameters of the given RadSubgroupSettings by ordering the parameters by their label names (if any are provided).
		/// </summary>
		/// <param name="settings">The subgroup settings.</param>
		public static void NormalizeParameters(this RadSubgroupSettings settings)
		{
			if (settings?.Parameters == null)
				return;
			if (!string.IsNullOrEmpty(settings.Parameters.FirstOrDefault()?.Label))
				settings.Parameters = settings.Parameters.OrderBy(p => p?.Label, StringComparer.OrdinalIgnoreCase).ToList();
		}

		public static void AddParameterGroup(InteractiveController app, RadHelper radHelper, RadGroupSettings settings,
			Skyline.DataMiner.Utils.RadToolkit.TrainingConfiguration trainingConfiguration, Dialog parent)
		{
			if (radHelper.TrainingConfigInAddGroupMessageAvailable)
			{
				radHelper.AddParameterGroup(settings, trainingConfiguration);
			}
			else
			{
				radHelper.AddParameterGroup(settings);
				if (trainingConfiguration != null)
				{
					try
					{
						Retrain(radHelper, settings.GroupName, trainingConfiguration.TimeRanges, trainingConfiguration.ExcludedSubgroups.Select(i => settings.Subgroups[i]).ToList());
					}
					catch (Exception ex)
					{
						app.Engine.GenerateInformation($"Failed to retrain relational anomaly group '{settings.GroupName}' after adding it: {ex}");
						ShowExceptionDialog(app, $"Failed to retrain group with name {settings.GroupName} after adding it", ex, parent);
					}
				}
			}
		}

		/// <summary>
		/// Join list of strings into a human readable string. E.g. ["a", "b", "c"] -> "a, b and c".
		/// </summary>
		/// <param name="l">A list of strings.</param>
		/// <param name="separator">The separator to use between all items except for the last two items.</param>
		/// <param name="finalSeparator">The separator to use between the last two items.</param>
		/// <returns>The resulting string.</returns>
		public static string HumanReadableJoin(this IEnumerable<string> l, string separator = ", ", string finalSeparator = " and ")
		{
			var sb = new StringBuilder();
			var enumerator = l.GetEnumerator();

			// Got an empty list, so return an empty string
			if (!enumerator.MoveNext())
				return string.Empty;

			string previous = enumerator.Current;

			// If we only got a single item, we return it
			if (!enumerator.MoveNext())
				return previous;

			// Add the first item, and set previous to the second item
			sb.Append(previous);
			previous = enumerator.Current;

			// Add the second and all subsequent items, except the last one
			while (enumerator.MoveNext())
			{
				if (sb.Length > 0)
					sb.Append(separator);
				sb.Append(previous);
				previous = enumerator.Current;
			}

			// Add the last item
			if (sb.Length > 0)
				sb.Append(finalSeparator);
			sb.Append(previous);

			return sb.ToString();
		}

		/// <summary>
		/// Wrap the text to the specified maximum line length. Source: https://gist.github.com/anderssonjohan/660952.
		/// </summary>
		/// <param name="text">The text to wrap.</param>
		/// <param name="maxLineLength">The maximal line length.</param>
		/// <returns>The wrapped text.</returns>
		public static List<string> WordWrap(this string text, int maxLineLength)
		{
			var list = new List<string>();
			if (string.IsNullOrEmpty(text))
				return list;
			if (maxLineLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxLineLength), "Max line length must be greater than 0.");

			int currentIndex;
			var lastWrap = 0;
			var whitespace = new[] { ' ', '\r', '\n', '\t' };
			var breakChars = new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '\n', '\r', '\t' };
			do
			{
				if (lastWrap + maxLineLength > text.Length)
					currentIndex = text.Length;
				else
					currentIndex = text.LastIndexOfAny(breakChars, Math.Min(text.Length - 1, lastWrap + maxLineLength)) + 1;
				if (currentIndex <= lastWrap)
					currentIndex = Math.Min(lastWrap + maxLineLength, text.Length);
				list.Add(text.Substring(lastWrap, currentIndex - lastWrap).Trim(whitespace));
				lastWrap = currentIndex;
			}
			while (currentIndex < text.Length);

			return list;
		}

		/// <summary>
		/// Capitalize the first letter of the string. If the provided string is null, null will be returned. If the provided string is empty,
		/// an empty string will be returned.
		/// </summary>
		/// <param name="s">The string to capitalize, or null.</param>
		/// <returns>The capitalized string, an empty string, or null.</returns>
		public static string Capitalize(this string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			return char.ToUpper(s[0]) + s.Substring(1);
		}

		public static RadHelper GetRadHelper(IEngine engine)
		{
			if (engine == null)
				throw new ArgumentNullException(nameof(engine));

			return new RadHelper(Engine.SLNetRaw, new Logger(s => engine.Log(s, LogType.Error, 0)));
		}

		/// <summary>
		/// Gets the subgroups from <paramref name="groupSettings"/> that have the same parameters as any of the subgroups in <paramref name="subgroupsToFind"/>.
		/// </summary>
		/// <param name="groupSettings">The group settings.</param>
		/// <param name="subgroupsToFind">The subgroups to look for.</param>
		/// <returns>The list of matching subgroups.</returns>
		private static List<RadSubgroupInfo> GetSubgroupsWithSameParameters(RadGroupInfo groupSettings, List<RadSubgroupSettings> subgroupsToFind)
		{
			if (subgroupsToFind == null || groupSettings?.Subgroups == null)
				return new List<RadSubgroupInfo>();

			foreach (var subgroup in subgroupsToFind)
				subgroup.NormalizeParameters();

			return groupSettings.Subgroups.Where(g => subgroupsToFind.Any(s => g.HasSameOrderedParameters(s))).ToList();
		}

		private static DynamicTableIndex[] FetchInstances(IEngine engine, int dataMinerID, int elementID, int tableParameterID, string displayKeyFilter = null)
		{
			try
			{
				var indicesRequest = new GetDynamicTableIndices(dataMinerID, elementID, tableParameterID);
				if (!string.IsNullOrEmpty(displayKeyFilter))
				{
					indicesRequest.KeyFilter = displayKeyFilter;
					indicesRequest.KeyFilterType = GetDynamicTableIndicesKeyFilterType.DisplayKey;
				}

				var indicesResponse = engine.SendSLNetSingleResponseMessage(indicesRequest) as DynamicTableIndicesResponse;
				if (indicesResponse == null)
				{
					engine.Log(
						$"Could not fetch primary keys for element {dataMinerID}/{elementID} parameter {tableParameterID} with filter '{displayKeyFilter ?? string.Empty}': " +
						$"no response, or response of the wrong type received",
						LogType.Error,
						5);
					return Array.Empty<DynamicTableIndex>();
				}

				return indicesResponse.Indices;
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch primary keys for element {dataMinerID}/{elementID} parameter {tableParameterID} with filter '{displayKeyFilter ?? string.Empty}': {e}", LogType.Error, 5);
				return Array.Empty<DynamicTableIndex>();
			}
		}

		/// <summary>
		/// Retrain the given group, excluding the subgroups with the same parameters as those in <paramref name="excludedSubgroups"/>. Note that this method will ignore the ID of
		/// the subgroups to exclude, and will only compare the parameters.
		/// </summary>
		/// <param name="radHelper">The RAD helper.</param>
		/// <param name="groupName">The name of the group to retrain.</param>
		/// <param name="timeRanges">The time ranges to use for retraining.</param>
		/// <param name="excludedSubgroups">The subgroups to exclude from retraining.</param>
		/// <exception cref="DataMinerCommunicationException">Thrown when the group could not be found.</exception>
		private static void Retrain(RadHelper radHelper, string groupName, List<TimeRange> timeRanges, List<RadSubgroupSettings> excludedSubgroups)
		{
			// The IDs of the subgroups do not match with the IDs we receive from the dialog, so we need to find them based on their parameters.
			var addedGroup = radHelper.FetchParameterGroupInfo(groupName);
			if (addedGroup == null)
				throw new DataMinerCommunicationException($"Could not find newly added relational anomaly group '{groupName}'");

			var excludedSubgroupsFromInfo = GetSubgroupsWithSameParameters(addedGroup, excludedSubgroups);
			var excludedSubgroupIDs = excludedSubgroupsFromInfo.Select(s => s.ID).ToList();
			radHelper.RetrainParameterGroup(-1, groupName, timeRanges, excludedSubgroupIDs);
		}
	}
}
