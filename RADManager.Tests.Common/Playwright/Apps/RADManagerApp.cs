namespace RADManager.Tests.Common.Playwright.Apps
{
	using Microsoft.Playwright;
	using Skyline.DataMiner.RADManager.Tests.Common.Playwright;

	public class RADManagerApp : LowCodeApp
	{
		public static readonly Guid RADManagerId = Guid.Parse("289cad74-648d-4227-bd66-c8f88a4d826a");

		public RADManagerApp(IBrowserContext browserContext, Config config)
			: base(browserContext, config)
		{
		}

		public override Guid ID => RADManagerId;

		public override string Name => "RAD Manager";
	}
}
