// NLogShared.Tests/ConcurrencySmokeTests.cs
// Project: NLogShared.Tests
// Purpose: Smoke tests for multi-threaded logging, validating thread-safety of NLog targets, ScopeContext, and CtxLogger under concurrent load

using NUnit.Framework;
using Shouldly;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogCtxShared;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("smoke")]
    public class ConcurrencySmokeTests
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
                Layout = "${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${event-properties:P00}"
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
        // Basic Parallel Logging Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Parallel_Logging_Captures_All_Events()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 10;

            // Act
            Parallel.For(0, threadCount, i =>
            {
                logger.Info($"message {i}");
            });
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                _memoryTarget.Logs.ShouldContain(log => log.Contains($"message {i}"));
            }
            logger.Dispose();
        }

        [Test]
        public void Parallel_Logging_No_Exceptions_Thrown()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 50;
            var exceptions = new List<Exception>();

            // Act
            Parallel.For(0, threadCount, i =>
            {
                try
                {
                    logger.Debug($"debug {i}");
                    logger.Info($"info {i}");
                    logger.Warn($"warn {i}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            LogManager.Flush();

            // Assert
            exceptions.Count.ShouldBe(0);
            _memoryTarget.Logs.Count.ShouldBe(threadCount * 3);
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Scope Context Thread-Safety Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Parallel_Logging_With_Scope_Props_Captures_All_Events()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 20;

            // Act
            Parallel.For(0, threadCount, i =>
            {
                LogCtx.Set(new Props($"thread-{i}"));
                logger.Info($"message {i}");
            });
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                _memoryTarget.Logs.ShouldContain(log => log.Contains($"message {i}"));
            }
            logger.Dispose();
        }

        [Test]
        public void Parallel_Ctx_Set_No_Exceptions()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 100;
            var exceptions = new List<Exception>();

            // Act
            Parallel.For(0, threadCount, i =>
            {
                try
                {
                    LogCtx.Set(new Props($"value-{i}"));
                    logger.Info($"log {i}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            LogManager.Flush();

            // Assert
            exceptions.Count.ShouldBe(0);
            _memoryTarget.Logs.Count.ShouldBe(threadCount);
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Multiple Loggers Concurrent Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Multiple_Loggers_Parallel_Logging_All_Events_Captured()
        {
            // Arrange
            _memoryTarget.Logs.Clear();
            const int loggerCount = 5;
            const int logsPerLogger = 10;

            // Act
            Parallel.For(0, loggerCount, i =>
            {
                using var logger = new CtxLogger();
                for (int j = 0; j < logsPerLogger; j++)
                {
                    logger.Info($"logger-{i}-message-{j}");
                }
            });
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(loggerCount * logsPerLogger);
            for (int i = 0; i < loggerCount; i++)
            {
                for (int j = 0; j < logsPerLogger; j++)
                {
                    _memoryTarget.Logs.ShouldContain(log => log.Contains($"logger-{i}-message-{j}"));
                }
            }
        }

        [Test]
        public void Multiple_Loggers_Parallel_Dispose_No_Exceptions()
        {
            // Arrange
            const int loggerCount = 20;
            var exceptions = new List<Exception>();
            var loggers = Enumerable.Range(0, loggerCount).Select(_ => new CtxLogger()).ToArray();

            // Act
            Parallel.For(0, loggerCount, i =>
            {
                try
                {
                    loggers[i].Info($"message from logger {i}");
                    loggers[i].Dispose();
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // Assert
            exceptions.Count.ShouldBe(0);
        }

        // ────────────────────────────────────────────────────────────────
        // High Contention Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void High_Contention_Logging_All_Events_Captured()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 50;
            const int logsPerThread = 20;
            var expectedCount = threadCount * logsPerThread;

            // Act
            Parallel.For(0, threadCount, i =>
            {
                for (int j = 0; j < logsPerThread; j++)
                {
                    logger.Info($"thread-{i}-log-{j}");
                }
            });
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(expectedCount);
            logger.Dispose();
        }

        [Test]
        public void High_Contention_With_Scope_Context_No_Exceptions()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 30;
            const int logsPerThread = 10;
            var exceptions = new List<Exception>();

            // Act
            Parallel.For(0, threadCount, i =>
            {
                try
                {
                    for (int j = 0; j < logsPerThread; j++)
                    {
                        LogCtx.Set(new Props($"thread-{i}-iteration-{j}"));
                        logger.Debug($"thread-{i}-log-{j}");
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            LogManager.Flush();

            // Assert
            exceptions.Count.ShouldBe(0);
            _memoryTarget.Logs.Count.ShouldBe(threadCount * logsPerThread);
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Mixed Log Levels Under Concurrency
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Parallel_Mixed_Log_Levels_All_Captured()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 10;

            // Act
            Parallel.For(0, threadCount, i =>
            {
                logger.Trace($"trace-{i}");
                logger.Debug($"debug-{i}");
                logger.Info($"info-{i}");
                logger.Warn($"warn-{i}");
                logger.Error(new Exception($"error-{i}"), $"error-{i}");
                logger.Fatal(new Exception($"fatal-{i}"), $"fatal-{i}");
            });
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(threadCount * 6);
            _memoryTarget.Logs.Count(log => log.Contains("TRACE")).ShouldBe(threadCount);
            _memoryTarget.Logs.Count(log => log.Contains("DEBUG")).ShouldBe(threadCount);
            _memoryTarget.Logs.Count(log => log.Contains("INFO")).ShouldBe(threadCount);
            _memoryTarget.Logs.Count(log => log.Contains("WARN")).ShouldBe(threadCount);
            _memoryTarget.Logs.Count(log => log.Contains("ERROR")).ShouldBe(threadCount);
            _memoryTarget.Logs.Count(log => log.Contains("FATAL")).ShouldBe(threadCount);
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Task-Based Async Concurrency Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public async Task Async_Parallel_Logging_All_Events_Captured()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int taskCount = 20;

            // Act
            var tasks = Enumerable.Range(0, taskCount).Select(async i =>
            {
                await Task.Delay(1); // Simulate async work
                logger.Info($"async-message-{i}");
            });
            await Task.WhenAll(tasks);
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(taskCount);
            for (int i = 0; i < taskCount; i++)
            {
                _memoryTarget.Logs.ShouldContain(log => log.Contains($"async-message-{i}"));
            }
            logger.Dispose();
        }

        [Test]
        public async Task Async_With_Scope_Context_No_Exceptions()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int taskCount = 15;
            var exceptions = new List<Exception>();

            // Act
            var tasks = Enumerable.Range(0, taskCount).Select(async i =>
            {
                try
                {
                    await Task.Delay(1);
                    LogCtx.Set(new Props($"async-value-{i}"));
                    logger.Debug($"async-log-{i}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            await Task.WhenAll(tasks);
            LogManager.Flush();

            // Assert
            exceptions.Count.ShouldBe(0);
            _memoryTarget.Logs.Count.ShouldBe(taskCount);
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Thread Safety of MemoryTarget
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void MemoryTarget_Thread_Safe_Under_Parallel_Writes()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 100;

            // Act
            Parallel.For(0, threadCount, i =>
            {
                logger.Info($"concurrent-{i}");
            });
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(threadCount);
            var uniqueMessages = _memoryTarget.Logs.Select(log => log.Split('|')[1]).Distinct().Count();
            uniqueMessages.ShouldBe(threadCount);
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Stress Test
        // ────────────────────────────────────────────────────────────────

        [Test]
        [Category("stress")]
        public void Stress_Test_1000_Threads_No_Failures()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            const int threadCount = 1000;
            var exceptions = new List<Exception>();

            // Act
            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = 50 }, i =>
            {
                try
                {
                    LogCtx.Set(new Props($"stress-{i}"));
                    logger.Info($"stress-message-{i}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            LogManager.Flush();

            // Assert
            exceptions.Count.ShouldBe(0);
            _memoryTarget.Logs.Count.ShouldBe(threadCount);
            logger.Dispose();
        }
    }
}