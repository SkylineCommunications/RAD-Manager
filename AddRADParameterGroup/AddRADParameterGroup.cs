using System;
using System.Collections.Generic;
using System.Linq;
using AddParameterGroup;
using RadWidgets;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

public class Script
{
	private InteractiveController app;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(IEngine engine)
	{
		// DO NOT REMOVE THE COMMENTED OUT CODE BELOW OR THE SCRIPT WONT RUN!
		// Interactive scripts need to be launched differently.
		// This is determined by a simple string search looking for "engine.ShowUI" in the source code.
		// However, due to the NuGet package, this string can no longer be detected.
		// This comment is here as a temporary workaround until it has been fixed.
		//// engine.ShowUI();

		try
		{
			app = new InteractiveController(engine);

			var dialog = new AddParameterGroupDialog(engine);
			dialog.Accepted += Dialog_Accepted;
			dialog.Cancelled += Dialog_Cancelled;

			app.ShowDialog(dialog);
		}
		catch (ScriptAbortException)
		{
			throw;
		}
		catch (ScriptForceAbortException)
		{
			throw;
		}
		catch (ScriptTimeoutException)
		{
			throw;
		}
		catch (InteractiveUserDetachedException)
		{
			throw;
		}
		catch (Exception e)
		{
			engine.ExitFail(e.ToString());
		}
	}

	private void Dialog_Cancelled(object sender, EventArgs e)
	{
		app.Engine.ExitSuccess("Adding parameter group cancelled");
	}

	private void Dialog_Accepted(object sender, EventArgs e)
	{
		var dialog = sender as AddParameterGroupDialog;
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		var failedGroups = new List<Tuple<string, Exception>>();
		var groups = dialog.GetGroupsToAdd();
		foreach (var group in groups)
		{
			try
			{
				var message = new AddMADParameterGroupMessage(group);
				app.Engine.SendSLNetSingleResponseMessage(message);
			}
			catch (Exception ex)
			{
				app.Engine.GenerateInformation($"Failed to add parameter group '{group.Name}': {ex}");
				failedGroups.Add(Tuple.Create(group.Name, ex));
			}
		}

		if (failedGroups.Count > 0)
		{
			var ex = new AggregateException("Failed to add parameter group(s) to RAD configuration", failedGroups.Select(p => p.Item2));
			Utils.ShowExceptionDialog(app, $"Failed to create {failedGroups.Select(p => p.Item1).HumanReadableJoin()}", ex, dialog);

			return;
		}

		app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}
}