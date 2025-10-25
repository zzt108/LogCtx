// NLogShared.Tests/DisposalAndFlushTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests validating CtxLogger.Dispose flushes logs without shutting down LogManager and allows subsequent logger instances

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
    public class DisposalAndFlushTests
    {
        private MemoryTarget _memoryTarget;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;

            // Create in-memory MemoryTarget for deterministic log capture
            _memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${level:uppercase=true}|${message}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(_memoryTarget);
            config.AddRuleForAllLevels(_memoryTarget);
            LogManager.Configuration = config;
        }

        [TearDown]
        public void TearDown()
        {
            _memoryTarget.Dispose();
            // Flush and reset NLog
            LogManager.Flush();
            LogManager.Configuration = null;
        }

        // ────────────────────────────────────────────────────────────────
        // Dispose Flushes Logs Without Shutting Down LogManager
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_Flushes_Logs_And_Does_Not_Shutdown_LogManager()
        {
            // Arrange
            var logger1 = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger1.Info("log from logger1");
            logger1.Dispose(); // Should flush, not shutdown

            var logger2 = new CtxLogger();
            logger2.Info("log from logger2");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("log from logger1");
            _memoryTarget.Logs[1].ShouldContain("log from logger2");
            logger2.Dispose();
        }

        [Test]
        public void Dispose_Ensures_Logs_Are_Written_Before_Return()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Debug("async log message");
            logger.Dispose();

            // Assert
            // After Dispose, logs should be flushed
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("async log message");
        }

        [Test]
        public void Dispose_Multiple_Loggers_Do_Not_Interfere()
        {
            // Arrange
            var logger1 = new CtxLogger();
            var logger2 = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger1.Info("message from logger1");
            logger2.Info("message from logger2");
            logger1.Dispose();
            logger2.Info("message after logger1 disposed");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(3);
            _memoryTarget.Logs[0].ShouldContain("message from logger1");
            _memoryTarget.Logs[1].ShouldContain("message from logger2");
            _memoryTarget.Logs[2].ShouldContain("message after logger1 disposed");
            logger2.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Multiple Dispose Calls
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_Can_Be_Called_Multiple_Times_Safely()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act & Assert
            Should.NotThrow(() =>
            {
                logger.Dispose();
                logger.Dispose();
                logger.Dispose();
            });
        }

        [Test]
        public void Dispose_Multiple_Times_Does_Not_Throw_Or_Block()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            logger.Info("test message");

            // Act
            logger.Dispose();
            logger.Dispose();
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("test message");
        }

        // ────────────────────────────────────────────────────────────────
        // Logging After Dispose
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Logging_After_Dispose_From_New_Logger_Still_Works()
        {
            // Arrange
            var logger1 = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger1.Info("before dispose");
            logger1.Dispose();

            var logger2 = new CtxLogger();
            logger2.Info("after dispose");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("before dispose");
            _memoryTarget.Logs[1].ShouldContain("after dispose");
            logger2.Dispose();
        }

        [Test]
        public void Dispose_Does_Not_Break_LogManager_Configuration()
        {
            // Arrange
            var logger1 = new CtxLogger();
            var originalConfig = LogManager.Configuration;

            // Act
            logger1.Dispose();

            // Assert
            LogManager.Configuration.ShouldBe(originalConfig);
            LogManager.Configuration.ShouldNotBeNull();
        }

        // ────────────────────────────────────────────────────────────────
        // Flush Behavior
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_Calls_LogManager_Flush()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Debug("message to flush");
            logger.Dispose();

            // Assert
            // Flush ensures the log reaches the target
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("message to flush");
        }

        [Test]
        public void Dispose_Flushes_All_Pending_Logs()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            for (int i = 0; i < 10; i++)
            {
                logger.Info($"message {i}");
            }
            logger.Dispose();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(10);
            for (int i = 0; i < 10; i++)
            {
                _memoryTarget.Logs[i].ShouldContain($"message {i}");
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Dispose and Resource Cleanup
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_With_Using_Statement_Flushes_Logs()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            using (var logger = new CtxLogger())
            {
                logger.Warn("inside using block");
            }
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("inside using block");
        }

        [Test]
        public void Multiple_Loggers_With_Using_Statements_All_Flush()
        {
            // Arrange
            _memoryTarget.Logs.Clear();

            // Act
            using (var logger1 = new CtxLogger())
            {
                logger1.Info("logger1 message");
            }

            using (var logger2 = new CtxLogger())
            {
                logger2.Info("logger2 message");
            }
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("logger1 message");
            _memoryTarget.Logs[1].ShouldContain("logger2 message");
        }

        // ────────────────────────────────────────────────────────────────
        // Dispose Does Not Call LogManager.Shutdown
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_Does_Not_Shutdown_LogManager()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Info("before dispose");
            logger.Dispose();

            // Assert
            // LogManager should still be active
            var testLogger = LogManager.GetCurrentClassLogger();
            testLogger.Info("after dispose");
            LogManager.Flush();

            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("before dispose");
            _memoryTarget.Logs[1].ShouldContain("after dispose");
        }

        [Test]
        public void LogManager_Configuration_Remains_Active_After_Dispose()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            logger.Dispose();

            // Assert
            LogManager.Configuration.ShouldNotBeNull();
            LogManager.Configuration.AllTargets.ShouldContain(t => t is MemoryTarget);
        }

        // ────────────────────────────────────────────────────────────────
        // Edge Cases
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_On_Logger_That_Never_Logged_Does_Not_Throw()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act & Assert
            Should.NotThrow(() => logger.Dispose());
        }

        [Test]
        public void Dispose_After_Exception_During_Logging_Still_Flushes()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Info("message one");
            try
            {
                // Simulate an exception after logging
                throw new InvalidOperationException("test exception");
            }
            catch
            {
                // ignored
            }
            logger.Info("message two");
            logger.Dispose();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("message one");
            _memoryTarget.Logs[1].ShouldContain("message two");
        }

        [Test]
        public void Dispose_On_Multiple_Loggers_In_Rapid_Succession_Does_Not_Throw()
        {
            // Arrange
            var loggers = new CtxLogger[10];
            for (int i = 0; i < 10; i++)
            {
                loggers[i] = new CtxLogger();
                loggers[i].Info($"message {i}");
            }

            // Act & Assert
            Should.NotThrow(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    loggers[i].Dispose();
                }
            });
        }
    }
}
