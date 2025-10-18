// NLogShared.Tests/StructuredPropsTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests validating LogCtx.Set propagates CTX_STRACE and Pxx event properties into NLog events via MemoryTarget

using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class StructuredPropsTests
    {
        private MemoryTarget _memoryTarget;
        private CtxLogger _logger;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;

            // Create in-memory MemoryTarget with layout that includes CTX_STRACE and Pxx event properties
            _memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${event-properties:P00}|${event-properties:P01}|${event-properties:P02}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(_memoryTarget);
            config.AddRuleForAllLevels(_memoryTarget);
            LogManager.Configuration = config;

            _logger = new CtxLogger();
        }

        [TearDown]
        public void TearDown()
        {
            // Flush and reset NLog
            LogManager.Flush();
            LogManager.Configuration = null;
            _memoryTarget.Dispose();
            _logger?.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // CTX_STRACE Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Set_Adds_CTX_STRACE_To_ScopeProperty()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("A"));
            _logger.Info("test message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("INFO|test message");
            // CTX_STRACE format: FileName::MethodName::LineNumber
            logLine.ShouldContain("::");
            logLine.ShouldContain("StructuredPropsTests");
        }

        [Test]
        public void Set_CTX_STRACE_Includes_FileName_MethodName_LineNumber()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CTX_STRACE}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props());
            _logger.Debug("debug");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var strace = _memoryTarget.Logs[0];
            strace.ShouldContain("StructuredPropsTests");
            strace.ShouldContain("::");
            strace.ShouldMatch(@"\d+"); // Line number pattern
        }

        [Test]
        public void Set_CTX_STRACE_Filters_Out_System_NUnit_NLog_Frames()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CTX_STRACE}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("X"));
            _logger.Info("filtered");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var strace = _memoryTarget.Logs[0];
            strace.ShouldNotContain("at System.");
            strace.ShouldNotContain("at NUnit.");
            strace.ShouldNotContain("at NLog.");
            strace.ShouldNotContain("at TechTalk.");
        }

        // ────────────────────────────────────────────────────────────────
        // Pxx Event Properties Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Set_Adds_P00_To_Event_Properties()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("ValueA"));
            _logger.Info("test");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("\"ValueA\""); // Props serializes as JSON
        }

        [Test]
        public void Set_Adds_Multiple_Pxx_To_Event_Properties()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("A", "B", "C"));
            _logger.Debug("multi prop");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("\"A\""); // P00
            logLine.ShouldContain("\"B\""); // P01
            logLine.ShouldContain("\"C\""); // P02
        }

        [Test]
        public void Set_Pxx_Properties_Render_As_JSON()
        {
            // Arrange
            _memoryTarget.Layout = "${event-properties:P00}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props(42));
            _logger.Info("number prop");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var p00Value = _memoryTarget.Logs[0];
            // Props constructor calls AsJson(item, true) which adds formatting
            p00Value.ShouldContain("42");
        }

        [Test]
        public void Set_With_Complex_Object_Serializes_To_JSON()
        {
            // Arrange
            _memoryTarget.Layout = "${event-properties:P00}";
            _memoryTarget.Logs.Clear();
            var obj = new { Name = "Test", Value = 123 };

            // Act
            _logger.Ctx.Set(new Props(obj));
            _logger.Info("complex");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var p00 = _memoryTarget.Logs[0];
            p00.ShouldContain("Name");
            p00.ShouldContain("Test");
            p00.ShouldContain("Value");
            p00.ShouldContain("123");
        }

        // ────────────────────────────────────────────────────────────────
        // CTX_STRACE + Pxx Combined Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Set_Adds_Both_CTX_STRACE_And_Pxx_To_Event()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("Alpha", "Beta"));
            _logger.Info("combined");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            // CTX_STRACE in scope
            logLine.ShouldContain("StructuredPropsTests");
            logLine.ShouldContain("::");
            // Pxx in event properties
            logLine.ShouldContain("\"Alpha\"");
            logLine.ShouldContain("\"Beta\"");
        }

        [Test]
        public void Set_Called_Multiple_Times_Replaces_CTX_STRACE_And_Pxx()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("First"));
            _logger.Info("log one");

            _logger.Ctx.Set(new Props("Second"));
            _logger.Info("log two");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("\"First\"");
            _memoryTarget.Logs[0].ShouldNotContain("\"Second\"");
            _memoryTarget.Logs[1].ShouldContain("\"Second\"");
            _memoryTarget.Logs[1].ShouldNotContain("\"First\"");
        }

        [Test]
        public void Set_With_Empty_Props_Still_Adds_CTX_STRACE()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CTX_STRACE}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props());
            _logger.Warn("empty props");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var strace = _memoryTarget.Logs[0];
            strace.ShouldNotBeNullOrWhiteSpace();
            strace.ShouldContain("StructuredPropsTests");
        }

        [Test]
        public void Set_With_Null_Props_Creates_Default_Props_With_CTX_STRACE()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CTX_STRACE}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(null);
            _logger.Info("null props");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var strace = _memoryTarget.Logs[0];
            strace.ShouldNotBeNullOrWhiteSpace();
            strace.ShouldContain("::");
        }

        // ────────────────────────────────────────────────────────────────
        // Custom Property Tests (Beyond Pxx)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Set_With_Custom_Keys_Pushes_To_Scope()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CustomKey}|${event-properties:P00}";
            _memoryTarget.Logs.Clear();
            var props = new Props("A");
            props.Add("CustomKey", "CustomValue");

            // Act
            _logger.Ctx.Set(props);
            _logger.Debug("custom key");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("CustomValue");
            logLine.ShouldContain("\"A\"");
        }

        [Test]
        public void Set_Removes_Existing_CTX_STRACE_From_Props_Before_Adding_New()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CTX_STRACE}";
            _memoryTarget.Logs.Clear();
            var props = new Props();
            props.Add("CTX_STRACE", "OldStackTrace");

            // Act
            _logger.Ctx.Set(props);
            _logger.Info("strace replace");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var strace = _memoryTarget.Logs[0];
            strace.ShouldNotContain("OldStackTrace");
            strace.ShouldContain("StructuredPropsTests");
            strace.ShouldContain("::");
        }

        // ────────────────────────────────────────────────────────────────
        // SEQ Query Compatibility Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Set_Properties_Are_Queryable_By_SEQ_Filter_Syntax()
        {
            // Arrange
            _memoryTarget.Layout = "${event-properties:P00}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props("SEQTestValue"));
            _logger.Info("seq filter");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var p00 = _memoryTarget.Logs[0];
            // SEQ queries like "P00 = 'SEQTestValue'" should work if P00 is a structured property
            p00.ShouldContain("SEQTestValue");
        }

        [Test]
        public void Set_CTX_STRACE_Is_Queryable_In_SEQ()
        {
            // Arrange
            _memoryTarget.Layout = "${scopeproperty:CTX_STRACE}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props());
            _logger.Info("seq strace");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var strace = _memoryTarget.Logs[0];
            // SEQ can query CTX_STRACE with filters like "CTX_STRACE like '%MethodName%'"
            strace.ShouldContain("StructuredPropsTests");
        }

        // ────────────────────────────────────────────────────────────────
        // Edge Cases
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Set_With_Null_Value_In_Props_Renders_As_Null()
        {
            // Arrange
            _memoryTarget.Layout = "${event-properties:P00}";
            _memoryTarget.Logs.Clear();
            var props = new Props();
            props.Add("P00", null);

            // Act
            _logger.Ctx.Set(props);
            _logger.Info("null value");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var p00 = _memoryTarget.Logs[0];
            // Props.Add converts null to "null value"
            p00.ShouldBe("null value");
        }

        [Test]
        public void Set_With_Boolean_True_Renders_As_String()
        {
            // Arrange
            _memoryTarget.Layout = "${event-properties:P00}";
            _memoryTarget.Logs.Clear();

            // Act
            _logger.Ctx.Set(new Props(true));
            _logger.Info("bool");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var p00 = _memoryTarget.Logs[0];
            p00.ShouldContain("true");
        }

        [Test]
        public void Set_With_Array_Serializes_To_JSON_Array()
        {
            // Arrange
            _memoryTarget.Layout = "${event-properties:P00}";
            _memoryTarget.Logs.Clear();
            var arr = new[] { 1, 2, 3 };

            // Act
            _logger.Ctx.Set(new Props(arr));
            _logger.Info("array");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var p00 = _memoryTarget.Logs[0];
            p00.ShouldContain("[");
            p00.ShouldContain("1");
            p00.ShouldContain("2");
            p00.ShouldContain("3");
            p00.ShouldContain("]");
        }
    }
}
