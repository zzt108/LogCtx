using NUnit.Framework;
using Shouldly;
using LogCtxShared;

namespace LogCtx.Tests
{
    [TestFixture]
    public class SourceContextTests
    {
        #region BuildStackTrace Tests

        [Test]
        public void BuildStackTrace_ShouldIncludeFileMethodLine()
        {
            // Arrange
            var fileName = "MyClass";
            var methodName = "MyMethod";
            var lineNumber = 42;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert
            trace.ShouldNotBeNullOrWhiteSpace();
            trace.ShouldContain(fileName);
            trace.ShouldContain(methodName);
            trace.ShouldContain(lineNumber.ToString());
        }

        [Test]
        public void BuildStackTrace_ShouldFilterSystemFrames()
        {
            // Arrange
            var fileName = "TestFile";
            var methodName = "TestMethod";
            var lineNumber = 10;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert - should not contain common framework noise
            trace.ShouldNotContain("at System.");
            trace.ShouldNotContain("at Microsoft.Extensions.Logging.");
        }

        [Test]
        public void BuildStackTrace_ShouldFilterNUnitFrames()
        {
            // Arrange
            var fileName = "TestFile";
            var methodName = "TestMethod";
            var lineNumber = 20;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert
            trace.ShouldNotContain("at NUnit.");
        }

        [Test]
        public void BuildStackTrace_ShouldFilterNLogFrames()
        {
            // Arrange
            var fileName = "AppClass";
            var methodName = "AppMethod";
            var lineNumber = 30;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert
            trace.ShouldNotContain("at NLog.");
        }

        [Test]
        public void BuildStackTrace_ShouldStartWithCallerInfo()
        {
            // Arrange
            var fileName = "StartFile";
            var methodName = "StartMethod";
            var lineNumber = 100;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert
            trace.ShouldStartWith($"{fileName}::{methodName}::{lineNumber}");
        }

        [Test]
        public void BuildStackTrace_WithEmptyFileName_ShouldHandleGracefully()
        {
            // Arrange
            var fileName = "";
            var methodName = "Method";
            var lineNumber = 1;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert
            trace.ShouldNotBeNullOrWhiteSpace();
            trace.ShouldContain(methodName);
        }

        #endregion BuildStackTrace Tests

        #region BuildSource Tests

        [Test]
        public void BuildSource_ShouldReturnFormattedString()
        {
            // Act
            var src = SourceContext.BuildSource();

            // Assert
            src.ShouldNotBeNullOrWhiteSpace();
            src.ShouldContain("SourceContextTests"); // File name
            src.ShouldContain("BuildSource_ShouldReturnFormattedString"); // Method name
            src.ShouldContain("."); // Separator
        }

        [Test]
        public void BuildSource_ShouldIncludeLineNumber()
        {
            // Act
            var src = SourceContext.BuildSource();

            // Assert - should contain digits (line number)
            src.ShouldMatch(@"\d+");
        }

        [Test]
        public void BuildSource_FormatShouldBeFileMethodLine()
        {
            // Act
            var src = SourceContext.BuildSource();

            // Assert - format: FileName.MethodName.LineNumber
            var parts = src.Split('.');
            parts.Length.ShouldBeGreaterThanOrEqualTo(3);
        }

        [Test]
        public void BuildSource_CalledFromDifferentMethods_ShouldReturnDifferentValues()
        {
            // Act
            var src1 = SourceContext.BuildSource();
            var src2 = HelperMethod();

            // Assert
            src1.ShouldNotBe(src2); // Different line numbers or method names
        }

        private string HelperMethod()
        {
            return SourceContext.BuildSource();
        }

        #endregion BuildSource Tests

        #region Stack Trace Filtering Integration Tests

        [Test]
        public void BuildStackTrace_RealStackTrace_ShouldExcludeFramework()
        {
            // Arrange & Act
            var trace = CaptureStackTraceFromMethod();

            // Assert - should contain test method but not framework
            trace.ShouldContain("SourceContextTests");
            trace.ShouldNotContain("at System.");
            trace.ShouldNotContain("at NUnit.");
        }

        private string CaptureStackTraceFromMethod()
        {
            var fileName = "SourceContextTests";
            var methodName = "CaptureStackTraceFromMethod";
            var lineNumber = 999;
            return SourceContext.BuildStackTrace(fileName, methodName, lineNumber);
        }

        [Test]
        public void BuildStackTrace_ShouldContainMultipleFrames()
        {
            // Arrange
            var fileName = "MultiFrame";
            var methodName = "TestMethod";
            var lineNumber = 50;

            // Act
            var trace = SourceContext.BuildStackTrace(fileName, methodName, lineNumber);

            // Assert - should have multiple lines (frames)
            var lines = trace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            lines.Length.ShouldBeGreaterThan(1); // At least caller + one more frame
        }

        #endregion Stack Trace Filtering Integration Tests
    }
}