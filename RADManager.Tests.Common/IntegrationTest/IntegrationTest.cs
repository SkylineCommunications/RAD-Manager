namespace Skyline.DataMiner.RADManager.Tests.Common.IntegrationTest
{
	using System;
	using System.Collections.Generic;

	using Microsoft.Extensions.Logging;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using QAPortalAPI.Models.ReportingModels;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public abstract class IntegrationTest
	{
		public abstract string Name { get; }

		public abstract IConnection Connection { get; }

		public abstract string Description { get; }

		public abstract TestCase[] TestCases { get; }

		public ILogger Logger { get; set; }

		private static string Contact => "adelina.spatariu@skyline.be";

		private static List<int> ProjectIds => new List<int> { 16180 };

		public abstract void Initialize();

		public abstract void Cleanup();

		public void RunIntegrationTest()
		{
			Initialize();
			foreach (var testCase in TestCases)
			{
				if (!testCase.RunTestCase())
				{
					break;
				}
			}

			Cleanup();
		}

		public void AssertCases()
		{
			foreach (var testCase in TestCases)
			{
				if (!testCase.IsSuccess.HasValue)
				{
					Assert.Fail($"Test case '{testCase.Name}' was not executed.");
				}

				testCase.AssertResult();
			}
		}

		public void AddReportToOutput(IEngine engine)
		{
			var report = new TestReport(
				new TestInfo(Name, Contact, ProjectIds, Description),
				new TestSystemInfo(GetAgentWhereScriptIsRunning(engine)));

			bool reportedFailedCase = false;
			foreach (var testCase in TestCases)
			{
				if (!testCase.IsSuccess.HasValue)
				{
					if (!reportedFailedCase)
					{
						engine.ExitFail($"RT Exception - Test case '{testCase.Name}' was not executed.");
					}

					break; // Skip cases that were not executed.
				}

				var caseReport = testCase.GetTestCaseReport();
				if (!report.TryAddTestCase(caseReport, out string errorMessage))
				{
					engine.ExitFail($"RT Exception - Could not add test case report for test case '{testCase.Name}' to the test report: {errorMessage}");
				}

				reportedFailedCase = reportedFailedCase || testCase.GetTestCaseReport().TestCaseResult == QAPortalAPI.Enums.Result.Failure;
				if (testCase.TryGetPerfomanceReport(out PerformanceTestCaseReport performanceReport))
				{
					report.PerformanceTestCases.Add(performanceReport);
				}
			}

			engine.AddScriptOutput("report", report.ToJson());
		}

		private static string GetAgentWhereScriptIsRunning(IEngine engine)
        {
            string agentName = null;
            try
            {
                var message = new GetInfoMessage(-1, InfoType.LocalDataMinerInfo);
                var response = (GetDataMinerInfoResponseMessage)engine.SendSLNetSingleResponseMessage(message);
                agentName = response?.AgentName ?? throw new NullReferenceException("No valid agent name was returned by SLNET.");
            }
            catch (Exception e)
            {
                engine.ExitFail("RT Exception - Could not retrieve local agent name: " + e);
            }

            return agentName ?? string.Empty;
        }
	}
}