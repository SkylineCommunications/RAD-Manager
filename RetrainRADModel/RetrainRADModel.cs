using System;
using System.Linq;
using RadUtils;
using RadWidgets;
using RetrainRADModel;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

public class Script
{
	private InteractiveController _app;

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

			var groupNamesAndIds = RadWidgets.Utils.GetGroupNameAndDataMinerID(_app);
			if (groupNamesAndIds.Count == 0)
			{
				RadWidgets.Utils.ShowMessageDialog(_app, "No parameter group selected", "Please select the parameter group you want to retrain first");
				return;
			}
			else if (groupNamesAndIds.Count > 1)
			{
				RadWidgets.Utils.ShowMessageDialog(_app, "Multiple parameter groups selected", "Please select a single parameter group you want to retrain");
				return;
			}

			var dialog = new RetrainRadModelDialog(engine, groupNamesAndIds[0].Item2, groupNamesAndIds[0].Item1);
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
		_app.Engine.ExitSuccess("Removing parameter group cancelled");
	}

	private void Dialog_Accepted(object sender, EventArgs e)
	{
		var dialog = sender as RetrainRadModelDialog;
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			RadMessageHelper.RetrainParameterGroup(_app.Engine, dialog.DataMinerID, dialog.GroupName, dialog.GetSelectedTimeRanges());
		}
		catch (Exception ex)
		{
			RadWidgets.Utils.ShowExceptionDialog(_app, "Failed to retrain parameter group", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully retrained RAD model");
	}
}
