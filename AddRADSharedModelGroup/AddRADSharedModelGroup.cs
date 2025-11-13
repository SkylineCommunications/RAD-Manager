using System;
using System.Linq;
using AddRadSharedModelGroup;
using RadWidgets;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using Skyline.DataMiner.Utils.RadToolkit;

public class Script
{
	private InteractiveController _app;
	private RadHelper _radHelper;

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
			_app = new InteractiveController(engine);
			_radHelper = RadWidgets.Utils.GetRadHelper(engine);

			var dialog = new AddSharedModelGroupDialog(engine, _radHelper);
			dialog.Accepted += Dialog_Accepted;
			dialog.Cancelled += Dialog_Cancelled;

			_app.ShowDialog(dialog);
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
		_app.Engine.ExitSuccess("Adding relational anomaly group cancelled");
	}

	private void Dialog_Accepted(object sender, EventArgs e)
	{
		var dialog = sender as AddSharedModelGroupDialog;
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		dialog.GetSettings(out var settings, out var trainingConfiguration);
		try
		{
			if (_radHelper.TrainingConfigInAddGroupMessageAvailable)
			{
				_radHelper.AddParameterGroup(settings, trainingConfiguration);
			}
			else
			{
				_radHelper.AddParameterGroup(settings);
				if (trainingConfiguration != null)
				{
					try
					{
						Utils.Retrain(_radHelper, settings.GroupName, trainingConfiguration.TimeRanges, trainingConfiguration.ExcludedSubgroups.Select(i => settings.Subgroups[i]).ToList());
					}
					catch (Exception ex)
					{
						_app.Engine.GenerateInformation($"Failed to retrain relational anomaly group '{settings.GroupName}' after adding it: {ex}");
						Utils.ShowExceptionDialog(_app, $"Failed to retrain group with name {settings.GroupName} after adding it", ex, dialog);
					}
				}
			}
		}
		catch (Exception ex)
		{
			_app.Engine.GenerateInformation($"Failed to add relational anomaly group '{settings.GroupName}': {ex}");
			Utils.ShowExceptionDialog(_app, $"Failed to create group with name {settings.GroupName}", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully added relational anomaly group to RAD configuration");
	}
}