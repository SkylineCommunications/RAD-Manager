namespace RADPlaywright.Apps
{
	using Microsoft.Playwright;

	public class RADManagerApp :LowCodeApp
	{
		public RADManagerApp(IBrowserContext browserContext)
			: base(browserContext, AppIDs.RADManager)
		{
		}
	}
}
