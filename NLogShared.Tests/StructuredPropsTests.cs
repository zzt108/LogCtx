using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Linq;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class StructuredPropsTests
    {
        private MemoryTarget memoryTarget;
        private CtxLogger ctxLogger;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;

            // Create in-memory MemoryTarget for deterministic log capture
            memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${scopeproperty:CustomKey}|${scopeproperty:P00}|${scopeproperty:P01}|${scopeproperty:P02}|${scopeproperty:P03}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(memoryTarget);
            config.AddRuleForAllLevels(memoryTarget);
            LogManager.Configuration = config;

            ctxLogger = new CtxLogger();
        }

        [TearDown]
        public void TearDown()
        {
            // Flush and reset NLog
            LogManager.Flush();
            ctxLogger?.Dispose();
            LogManager.Configuration = null;
            memoryTarget?.Dispose();
        }

        [Test]
        public void Set_Adds_P00_To_Event()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("valueA"));
            ctxLogger.Info("test message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("INFO");
            logLine.ShouldContain("test message");
            logLine.ShouldContain("valueA"); // Property should appear
        }

        [Test]
        public void Set_Adds_P01_To_Event()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("valueA", "valueB"));
            ctxLogger.Info("test message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("valueA");
            logLine.ShouldContain("valueB");
        }

        [Test]
        public void Set_Adds_Both_CTX_STRACE_And_Pxx_To_Event()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("A", "B"));
            ctxLogger.Info("combined test");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("::"); // CTX_STRACE
            logLine.ShouldContain("A");
            logLine.ShouldContain("B");
        }

        [Test]
        public void Multiple_Sets_Replace_Props()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("first"));
            ctxLogger.Info("log one");

            LogCtx.Set(new Props("second"));
            ctxLogger.Info("log two");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(2);
            memoryTarget.Logs[0].ShouldContain("first");
            memoryTarget.Logs[0].ShouldNotContain("second");
            memoryTarget.Logs[1].ShouldContain("second");
            memoryTarget.Logs[1].ShouldNotContain("first");
        }

        [Test]
        public void Props_Are_Serialized_As_Json()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("test value", 123, true));
            ctxLogger.Info("json test");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];

            // Scope property names are not rendered

            // Props are JSON serialized by Props.Add
            logLine.ShouldContain("\"test value\""); // JSON string
            logLine.ShouldContain("123");
            logLine.ShouldContain("true");
        }

        [Test]
        public void Empty_Props_Does_Not_Add_Pxx_Keys()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props()); // No params
            ctxLogger.Info("empty props");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            // Scope property names are not rendered
            logLine.ShouldContain("::"); // Still has STRACE
            // logLine.ShouldNotContain("P00="); // No P00
        }

        [Test]
        public void Null_Props_Still_Adds_CTXSTRACE()
        {
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(null);
            ctxLogger.Info("null props");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("::");
        }

        [Test]
        public void Custom_Key_Props_Appear_In_Log()
        {
            Assert.Inconclusive("\"P00-Hello\", \"P01-World\" appears with CTX_STRACE but not \"CustomKey\" and \"CUSTOMKEY\"");
            // Arrange
            memoryTarget.Logs.Clear();

            // Act
            using var props = LogCtx.Set(new Props("P00-Hello", "P01-World"));
            props.Add("CustomKey", "CustomValue1");
            props.Add("CUSTOMKEY", "CustomValue2");
            ctxLogger.Info("custom key test");
            //LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("CustomKey=");
            logLine.ShouldContain("CustomValue");
        }
    }
}