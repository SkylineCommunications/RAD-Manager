namespace PlaywrightTests
{
	using Microsoft.Playwright;
	using PlaywrightTests.TestCases;
	using RADManager.Tests.Common.Playwright.Apps;
	using Skyline.DataMiner.RADManager.Tests.Common.IntegrationTest;
	using Skyline.DataMiner.RADManager.Tests.Common.Playwright.IntegrationTest;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public class RunUserInterfaceValidationTests : PlaywrightIntegrationTest
	{
		private TestCase[] _testCases;
		private RADManagerApp _app;

		public IPage Page { get; set; }

		public RunUserInterfaceValidationTests(TestContext testContext)
			: base(testContext)
		{
		}

		public override string Name => "Connect to RADManager";

		public override string Description => "This is mainly for testing";

		public override TestCase[] TestCases
		{
			get
			{
				if (_testCases == null)
				{
					CreateTestCases();
				}

				return _testCases;
			}
		}

		public RADManagerApp RADManagerApp
		{
			get
			{
				if (_app == null)
				{

					_app = new RADManagerApp(Context, Config);
				}

				return _app;
			}
		}

		[TestMethod]
		public void ValidateUserInterface()
		{
			RunIntegrationTest();
			AssertCases();
		}

		private void CreateTestCases()
		{
			_testCases =
			[
				new RAD_UserInterface_Validation( RADManagerApp),
	        ];
		}
	}
}
