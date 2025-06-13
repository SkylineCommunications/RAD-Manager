using System;
using RAD_Manager;
using Skyline.AppInstaller;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.AppPackages;

public class GqiNotEnabledException : Exception
{
	public GqiNotEnabledException(string message) : base(message)
	{
	}
}

/// <summary>
/// DataMiner Script Class.
/// </summary>
internal class Script
{
	/// <summary>
	/// The script entry point.
	/// </summary>
	/// <param name="engine">Provides access to the Automation engine.</param>
	/// <param name="context">Provides access to the installation context.</param>
	[AutomationEntryPoint(AutomationEntryPointType.Types.InstallAppPackage)]
	public void Install(IEngine engine, AppInstallContext context)
	{
		try
		{
			engine.Timeout = new TimeSpan(0, 10, 0);
			engine.GenerateInformation("Starting installation");

			var gqiEnabled = GqiUtils.IsGqiDxmEnabled();
			if (!gqiEnabled)
			{
				throw new GqiNotEnabledException("GQI DxM is not enabled. RAD Manager requires the GQI DxM from version 3.0.0 onwards. " +
					"Please make sure the GQI DxM is enabled (see https://aka.dataminer.services/gqi-dxm), or use RAD Manager 2.0.4.");
			}

			var installer = new AppInstaller(Engine.SLNetRaw, context);
			installer.InstallDefaultContent();

			// Custom installation logic can be added here for each individual install package.
		}
		catch (GqiNotEnabledException ex)
		{
			engine.ExitFail(ex.Message);
		}
		catch (Exception e)
		{
			engine.ExitFail($"Exception encountered during installation: {e}");
		}
	}
}