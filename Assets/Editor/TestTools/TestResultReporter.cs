using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Ghi kết quả lần chạy test gần nhất ra Logs/LastTestResults.xml (+ tóm tắt .txt) — kể cả khi chạy
    /// PlayMode test trong Editor đang mở (đăng ký lại sau mỗi domain reload nên không mất callback).
    /// Giúp công cụ tự động (Unity MCP, CI) đọc được kết quả mà không cần đóng Editor để chạy batch.
    /// </summary>
    [InitializeOnLoad]
    public static class TestResultReporter
    {
        private const string XmlPath = "Logs/LastTestResults.xml";
        private const string SummaryPath = "Logs/LastTestResults.txt";

        static TestResultReporter()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        }

        /// <summary>Chạy toàn bộ PlayMode test (dùng được từ Unity MCP RunCommand).</summary>
        public static void RunPlayModeTests() => RunPlayModeTestGroup(null);

        /// <summary>
        /// Chạy một lớp test, vd "PrehistoricTribe.Tests.Milestone5CombatTests" (null = tất cả).
        /// Chạy từng nhóm nhẹ bộ nhớ hơn — hữu ích trên máy ít RAM.
        /// </summary>
        public static void RunPlayModeTestGroup(string groupName)
        {
            if (File.Exists(SummaryPath)) File.Delete(SummaryPath);
            var filter = new Filter { testMode = TestMode.PlayMode };
            if (!string.IsNullOrEmpty(groupName)) filter.groupNames = new[] { groupName };

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(filter));
        }

        private class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                TestRunnerApi.SaveResultToFile(result, XmlPath);
                File.WriteAllText(SummaryPath,
                    $"total={result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount} " +
                    $"passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}\n");
            }
        }
    }
}
