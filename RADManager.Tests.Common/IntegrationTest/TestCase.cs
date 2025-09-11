namespace Skyline.DataMiner.RADManager.Tests.Common.IntegrationTest
{
	using System;
	using System.Diagnostics;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using QAPortalAPI.Models.ReportingModels;

	public abstract class TestCase
	{
		/// <summary>
		/// Gets the name of the test case.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the maximum allowed execution time for this test case.
		/// </summary>
		public abstract TimeSpan MaxExecutionTime { get; }

		/// <summary>
		/// Gets a value indicating whether the test case succeeded. Null if not executed yet.
		/// </summary>
		public bool? IsSuccess { get; private set; }

		/// <summary>
		/// Gets the execution time of the test case. Null if the test case did not succeed or was not executed yet.
		/// </summary>
		public TimeSpan? ExecutionTime { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the test case executed within the allowed time. False if the test case did not succeed or was not executed yet.
		/// </summary>
		public bool IsInTime { get; private set; }

		/// <summary>
		/// Gets or sets information about the failure if the test case did not succeed.
		/// </summary>
		public string FailInfo { get; set; }

		/// <summary>
		/// Return false or throw exception on failure.
		/// </summary>
		/// <returns>Whether the test succeeded.</returns>
		public abstract bool Execute();

		/// <summary>
		/// Runs the test case and measures execution time.
		/// </summary>
		/// <returns>Whether the test succeeded.</returns>
		public bool RunTestCase()
		{
			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				IsSuccess = Execute();
				stopwatch.Stop();

				if (IsSuccess.Value)
				{
					ExecutionTime = stopwatch.Elapsed;
					IsInTime = ExecutionTime < MaxExecutionTime;
				}
			}
			catch (Exception ex)
			{
				IsSuccess = false;
				FailInfo = ex.ToString();
			}

			return IsSuccess.Value;
		}

		public TestCaseReport GetTestCaseReport()
		{
			if (!IsSuccess.HasValue)
			{
				throw new InvalidOperationException("Test case has not been executed yet.");
			}

			return new TestCaseReport(Name, IsSuccess.Value ? QAPortalAPI.Enums.Result.Success : QAPortalAPI.Enums.Result.Failure, FailInfo);
		}

		public bool TryGetPerfomanceReport(out PerformanceTestCaseReport performanceReport)
		{
			performanceReport = null;
			if (!ExecutionTime.HasValue)
			{
				return false;
			}

			var failinfo = string.Empty;
			if (!IsInTime)
			{
				failinfo = $"Test exceeded maximum execution time of {MaxExecutionTime.TotalMilliseconds} ms (execution time: {ExecutionTime.Value.TotalMilliseconds} ms).";
			}

			performanceReport = new PerformanceTestCaseReport(
				Name,
				IsInTime ? QAPortalAPI.Enums.Result.Success : QAPortalAPI.Enums.Result.Failure,
				failinfo,
				QAPortalAPI.Enums.ResultUnit.Millisecond,
				ExecutionTime.Value.TotalMilliseconds);
			return true;
		}

		public void AssertResult()
        {
            Assert.IsTrue(IsSuccess, FailInfo);
            if (!Debugger.IsAttached)
            {
                Assert.IsTrue(IsInTime && ExecutionTime.HasValue, $"Test exceeded maximum execution time of {MaxExecutionTime.TotalMilliseconds} ms (execution time: {(ExecutionTime.HasValue ? ExecutionTime.Value.TotalMilliseconds : 0)} ms).");
            }
        }
	}
}