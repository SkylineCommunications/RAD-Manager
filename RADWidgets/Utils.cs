namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public static class Utils
	{
		/// <summary>
		/// Get the group name and DataMiner ID from the script parameters.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <returns>A list with the DataMiner IDs and names of the provided groups, or an empty list if none were provided.</returns>
		/// <exception cref="FormatException">Thrown when the DataMiner ID script parameter could not be parsed, or when the number of group names and data miner IDs provided do not match.</exception>
		public static List<Tuple<int, string>> GetGroupNameAndDataMinerID(InteractiveController app)
		{
			var groupNames = ParseScriptParameterValue(app.Engine.GetScriptParam("GroupName")?.Value);
			var dataMinerIDs = ParseScriptParameterValue(app.Engine.GetScriptParam("DataMinerID")?.Value);
			if (groupNames.IsNullOrEmpty() || dataMinerIDs.IsNullOrEmpty())
				return new List<Tuple<int, string>>();

			if (groupNames.Count != dataMinerIDs.Count)
				throw new FormatException("The number of group names and DataMiner IDs must be equal");

			var result = new List<Tuple<int, string>>(groupNames.Count);
			for (int i = 0; i < groupNames.Count; ++i)
			{
				if (!int.TryParse(dataMinerIDs[i], out int dataMinerID))
					throw new FormatException($"DataMinerID parameter is not a valid number, got '{dataMinerIDs[i]}'");

				result.Add(new Tuple<int, string>(dataMinerID, groupNames[i]));
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

		public static IEnumerable<string> FetchMatchingInstances(IEngine engine, int dataMinerID, int elementID, ParameterInfo parameterInfo, string displayKeyFilter)
		{
			return FetchMatchingInstances(engine, dataMinerID, elementID, parameterInfo.ParentTablePid, displayKeyFilter);
		}

		public static IEnumerable<string> FetchMatchingInstances(IEngine engine, int dataMinerID, int elementID, int tableParameterID, string displayKeyFilter)
		{
			try
			{
				var indicesRequest = new GetDynamicTableIndices(dataMinerID, elementID, tableParameterID)
				{
					KeyFilter = displayKeyFilter,
					KeyFilterType = GetDynamicTableIndicesKeyFilterType.DisplayKey,
				};
				var indicesResponse = engine.SendSLNetSingleResponseMessage(indicesRequest) as DynamicTableIndicesResponse;
				if (indicesResponse == null)
				{
					engine.Log(
						$"Could not fetch primary keys for element {dataMinerID}/{elementID} parameter {tableParameterID} with filter {displayKeyFilter}: " +
						$"no response, or response of the wrong type received",
						LogType.Error,
						5);
					return new List<string> { };
				}

				return indicesResponse.Indices.Select(i => i.IndexValue);
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch primary keys for element {dataMinerID}/{elementID} parameter {tableParameterID} with filter {displayKeyFilter}: {e}", LogType.Error, 5);
				return new List<string> { };
			}
		}

		public static List<string> FetchRadGroupNames(IEngine engine)
		{
			try
			{
				var request = new GetMADParameterGroupsMessage();
				var response = engine.SendSLNetSingleResponseMessage(request) as GetMADParameterGroupsResponseMessage;
				if (response == null)
				{
					engine.Log("Could not fetch RAD group names: no response or response of the wrong type received", LogType.Error, 5);
					return new List<string> { };
				}

				return response.GroupNames;
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch RAD group names: {e}", LogType.Error, 5);
				return new List<string>() { };
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
	}
}
