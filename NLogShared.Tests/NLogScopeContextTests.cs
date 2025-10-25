// ✅ FULL FILE VERSION
// NLogShared.Tests/NLogScopeContextTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests for NLogScopeContext covering Clear, PushProperty, and layout renderer integration

using NUnit.Framework;
using Shouldly;
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
    public class NLogScopeContextTests
    {
        private MemoryTarget _memoryTarget;
        private Logger _logger;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;

            // Create in-memory MemoryTarget with layout that includes scope properties
            _memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${level:uppercase=true}|${message}|${scopeproperty:TestKey}|${scopeproperty:K1}|${scopeproperty:K2}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(_memoryTarget);
            config.AddRuleForAllLevels(_memoryTarget);
            LogManager.Configuration = config;

            _logger = LogManager.GetCurrentClassLogger();
        }

        [TearDown]
        public void TearDown()
        {
            // Flush and reset NLog
            LogManager.Flush();
            LogManager.Configuration = null;
            _memoryTarget.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Clear Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Clear_Removes_All_Scope_Properties()
        {
            // Arrange
            var scope = new NLogScopeContext();
            scope.PushProperty("TestKey", "TestValue");
            _memoryTarget.Logs.Clear();

            // Act
            scope.Clear();
            _logger.Info("after clear");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("INFO|after clear");
            logLine.ShouldNotContain("TestValue");
            // Layout renders empty for missing scope properties
            logLine.ShouldEndWith("||");
        }

        [Test]
        public void Clear_Can_Be_Called_Multiple_Times_Safely()
        {
            // Arrange
            var scope = new NLogScopeContext();
            scope.PushProperty("Key1", "Value1");

            // Act & Assert
            Should.NotThrow(() =>
            {
                scope.Clear();
                scope.Clear();
                scope.Clear();
            });
        }

        [Test]
        public void Clear_After_Multiple_PushProperty_Removes_All()
        {
            // Arrange
            var scope = new NLogScopeContext();
            scope.PushProperty("K1", "V1");
            scope.PushProperty("K2", "V2");
            scope.PushProperty("K3", "V3");
            _memoryTarget.Logs.Clear();

            // Act
            scope.Clear();
            _logger.Info("cleared");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldNotContain("V1");
            logLine.ShouldNotContain("V2");
            logLine.ShouldNotContain("V3");
        }

        // ────────────────────────────────────────────────────────────────
        // PushProperty Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PushProperty_Makes_Value_Visible_In_Log()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("TestKey", "MyValue");
            _logger.Info("test message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("INFO|test message");
            logLine.ShouldContain("MyValue");
        }

        [Test]
        public void PushProperty_With_String_Value_Renders_Correctly()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", "StringValue");
            _logger.Debug("debug msg");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("StringValue");
        }

        [Test]
        public void PushProperty_With_Integer_Value_Renders_As_String()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", 42);
            _logger.Info("number test");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("42");
        }

        [Test]
        public void PushProperty_With_Boolean_Value_Renders_As_String()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", true);
            _logger.Warn("bool test");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("True");
        }

        [Test]
        public void PushProperty_With_Null_Value_Renders_As_Empty()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", null);
            _logger.Info("null test");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            // NLog renders missing/null scope properties as empty strings
            logLine.ShouldContain("INFO|null test|");
        }

        [Test]
        public void PushProperty_Multiple_Keys_All_Render_In_Log()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", "Value1");
            scope.PushProperty("K2", "Value2");
            _logger.Info("multi key");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("Value1");
            logLine.ShouldContain("Value2");
        }

        [Test]
        public void PushProperty_Same_Key_Multiple_Times_Keeps_Latest()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", "First");
            scope.PushProperty("K1", "Second");
            scope.PushProperty("K1", "Third");
            _logger.Info("overwrite test");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            // NLog ScopeContext stacks values, so all pushed values may appear
            // However, layout renderer ${scopeproperty:K1} returns the most recent
            logLine.ShouldContain("Third");
        }

        // ────────────────────────────────────────────────────────────────
        // Integration Tests with Logger
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PushProperty_And_Clear_Isolates_Logs()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", "FirstValue");
            _logger.Info("log one");

            scope.Clear();
            scope.PushProperty("K1", "SecondValue");
            _logger.Info("log two");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("FirstValue");
            _memoryTarget.Logs[0].ShouldNotContain("SecondValue");
            _memoryTarget.Logs[1].ShouldContain("SecondValue");
            _memoryTarget.Logs[1].ShouldNotContain("FirstValue");
        }

        [Test]
        public void PushProperty_Persists_Across_Multiple_Log_Calls()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", "PersistentValue");
            _logger.Info("log one");
            _logger.Debug("log two");
            _logger.Warn("log three");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(3);
            _memoryTarget.Logs[0].ShouldContain("PersistentValue");
            _memoryTarget.Logs[1].ShouldContain("PersistentValue");
            _memoryTarget.Logs[2].ShouldContain("PersistentValue");
        }

        [Test]
        public void Clear_Between_Logs_Removes_Properties_For_Subsequent_Logs()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("K1", "BeforeClear");
            _logger.Info("before");

            scope.Clear();
            _logger.Info("after");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("BeforeClear");
            _memoryTarget.Logs[1].ShouldNotContain("BeforeClear");
        }

        // ────────────────────────────────────────────────────────────────
        // Layout Renderer Integration Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void ScopeProperty_Layout_Renderer_Works_With_PushProperty()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Layout = "${scopeproperty:CustomKey}";
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("CustomKey", "CustomValue");
            _logger.Info("message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldBe("CustomValue");
        }

        [Test]
        public void ScopeProperty_Layout_Renderer_Returns_Empty_After_Clear()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Layout = "${scopeproperty:Key}|${message}";
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("Key", "Value");
            _logger.Info("before");

            scope.Clear();
            _logger.Info("after");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldBe("Value|before");
            _memoryTarget.Logs[1].ShouldBe("|after");
        }

        [Test]
        public void Multiple_ScopeProperty_Renderers_All_Resolve()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Layout = "${scopeproperty:A}|${scopeproperty:B}|${scopeproperty:C}";
            _memoryTarget.Logs.Clear();

            // Act
            scope.PushProperty("A", "1");
            scope.PushProperty("B", "2");
            scope.PushProperty("C", "3");
            _logger.Info("test");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldBe("1|2|3");
        }

        // ────────────────────────────────────────────────────────────────
        // Edge Cases
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PushProperty_With_Empty_String_Key_Does_Not_Throw()
        {
            // Arrange
            var scope = new NLogScopeContext();

            // Act & Assert
            Should.NotThrow(() => scope.PushProperty("", "value"));
        }

        [Test]
        public void PushProperty_With_Whitespace_Key_Does_Not_Throw()
        {
            // Arrange
            var scope = new NLogScopeContext();

            // Act & Assert
            Should.NotThrow(() => scope.PushProperty("   ", "value"));
        }

        [Test]
        public void PushProperty_With_Complex_Object_Renders_ToString()
        {
            // Arrange
            var scope = new NLogScopeContext();
            _memoryTarget.Layout = "${scopeproperty:Obj}";
            _memoryTarget.Logs.Clear();
            var obj = new { Name = "Test", Value = 42 };

            // Act
            scope.PushProperty("Obj", obj);
            _logger.Info("message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            // NLog will call ToString() on the object
            _memoryTarget.Logs[0].ShouldContain("Name");
            _memoryTarget.Logs[0].ShouldContain("Value");
        }

        [Test]
        public void Clear_On_Empty_Scope_Does_Not_Throw()
        {
            // Arrange
            var scope = new NLogScopeContext();

            // Act & Assert
            Should.NotThrow(() => scope.Clear());
        }
    }
}
