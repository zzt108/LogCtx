using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LogCtxShared
{
    /// <summary>
    /// Utilities for capturing and formatting source code context information.
    /// Supports CallerInfo attributes and stack trace filtering.
    /// </summary>
    public static class SourceContext
    {
        /// <summary>
        /// Builds filtered stack trace excluding framework noise.
        /// Filters out System, NUnit, NLog, TechTalk, and Microsoft.Extensions.Logging frames.
        /// </summary>
        /// <param name="fileName">Source file name (without path)</param>
        /// <param name="methodName">Method name</param>
        /// <param name="lineNumber">Line number</param>
        /// <returns>Formatted stack trace string with filtered frames</returns>
        public static string BuildStackTrace(
            string fileName,
            string methodName,
            int lineNumber)
        {
            var strace = $"{fileName}::{methodName}::{lineNumber}\r\n";
            var tr = new StackTrace(true);
            var frames = tr.ToString().Split('\n');

            bool isFirst = true;
            foreach (var frame in frames)
            {
                if (isFirst)
                {
                    isFirst = false;
                    continue; // Skip first frame (this method itself)
                }

                if (!ShouldFilterFrame(frame))
                {
                    strace += $"--{frame}\n";
                }
            }

            return strace;
        }

        /// <summary>
        /// Determines if a stack frame should be filtered from output.
        /// Filters framework and testing infrastructure frames.
        /// </summary>
        private static bool ShouldFilterFrame(string frame)
        {
            var trimmed = frame.Trim();
            return trimmed.StartsWith("at System.") ||
                   trimmed.StartsWith("at NUnit.") ||
                   trimmed.StartsWith("at NLog.") ||
                   trimmed.StartsWith("at TechTalk.") ||
                   trimmed.StartsWith("at Microsoft.Extensions.Logging.");
        }

        /// <summary>
        /// Builds compact source location string: FileName.MethodName.LineNumber
        /// </summary>
        /// <param name="memberName">Auto-captured caller method name</param>
        /// <param name="sourceFilePath">Auto-captured source file path</param>
        /// <param name="sourceLineNumber">Auto-captured source line number</param>
        /// <returns>Formatted source string</returns>
        /// <example>
        /// var src = SourceContext.BuildSource();
        /// // Returns: "MyClass.MyMethod.42"
        /// </example>
        public static string BuildSource(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            return $"{fileName}.{memberName}.{sourceLineNumber}";
        }
    }
}