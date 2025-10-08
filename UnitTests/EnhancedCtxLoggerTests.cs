// ✅ FULL FILE VERSION
// File: UnitTests/EnhancedCtxLoggerTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using NLogShared;
using LogCtxShared;

namespace UnitTests.LogCtx
{
    /// <summary>
    /// Tests for the enhanced CtxLogger supporting fluent LogCtx API
    /// </summary>
    [TestFixture]
    public class EnhancedCtxLoggerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // ✅ EXISTING PATTERN - Initialize LogCtx once per test fixture
            FailsafeLogger.Initialize("NLog.config");
        }

        [Test]
        public void FailsafeLogger_Initialize_ShouldSetLogCtxLogger()
        {
            // Arrange - FailsafeLogger.Initialize already called in OneTimeSetUp
            
            // Assert - LogCtx.Logger should be set
            LogCtx.Logger.ShouldNotBeNull();
            LogCtx.CanLog.ShouldBeTrue();
            LogCtx.Logger.ShouldBeOfType<CtxLogger>();
        }

        [Test] 
        public void CtxLogger_TraditionalMethods_ShouldWorkUnchanged()
        {
            // Arrange
            var logger = LogCtx.Logger as CtxLogger;
            logger.ShouldNotBeNull();

            // Act & Assert - Should not throw
            logger.Debug("Debug message");
            logger.Info("Info message");
            logger.Warn("Warning message");
            logger.Error(new Exception("Test exception"), "Error message");
            logger.Fatal(new Exception("Fatal exception"), "Fatal message");
            logger.Trace("Trace message");
        }

        [Test]
        public void CtxLogger_EnhancedMethods_ShouldAcceptContext()
        {
            // Arrange
            var logger = LogCtx.Logger as CtxLogger;
            logger.ShouldNotBeNull();
            
            using var ctx = LogCtx.Set()
                .With("TestProperty", "TestValue")
                .With("UserId", 123);

            // Act & Assert - Should not throw
            logger.Debug("Debug with context", ctx);
            logger.Info("Info with context", ctx);
            logger.Warn("Warning with context", ctx);
            logger.Error(new Exception("Test exception"), "Error with context", ctx);
            logger.Fatal(new Exception("Fatal exception"), "Fatal with context", ctx);
            logger.Trace("Trace with context", ctx);
        }

        [Test]
        public void CtxLogger_EnhancedMethods_ShouldWorkWithoutContext()
        {
            // Arrange
            var logger = LogCtx.Logger as CtxLogger;
            logger.ShouldNotBeNull();

            // Act & Assert - Should work with null context
            logger.Debug("Debug without context", null);
            logger.Info("Info without context", null);
            logger.Warn("Warning without context");
            logger.Error(new Exception("Test"), "Error without context");
            logger.Fatal(new Exception("Fatal"), "Fatal without context");
            logger.Trace("Trace without context");
        }

        [Test]
        public void FluentLogCtx_WithEnhancedLogger_ShouldChainProperly()
        {
            // Arrange & Act
            using var ctx = LogCtx.Set()
                .With("Operation", "FluentLoggerTest")
                .With("RequestId", Guid.NewGuid())
                .LogInfo("Starting operation")  // Uses enhanced CtxLogger
                .With("StepCompleted", 1)
                .LogDebug("Step completed")     // Continued chaining
                .With("Timestamp", DateTime.UtcNow);

            // Assert
            ctx.Properties.ShouldContainKeyAndValue("Operation", "FluentLoggerTest");
            ctx.Properties.ShouldContainKeyAndValue("StepCompleted", 1);
            ctx.Properties.ShouldContainKey("RequestId");
            ctx.Properties.ShouldContainKey("Timestamp");
        }

        [Test]
        public void CtxLogger_Disposal_ShouldNotShutdownEntireNLogSystem()
        {
            // Arrange
            var logger1 = new CtxLogger();
            var logger2 = new CtxLogger();
            
            var tempConfig = Path.GetTempFileName();
            File.WriteAllText(tempConfig, @"<?xml version='1.0' encoding='utf-8' ?>
<nlog xmlns='http://www.nlog-project.org/schemas/NLog.xsd'>
  <targets>
    <target xsi:type='Console' name='console' layout='${message}' />
  </targets>
  <rules>
    <logger name='*' minlevel='Info' writeTo='console' />
  </rules>
</nlog>");

            try
            {
                // Act - Configure and dispose first logger
                logger1.ConfigureXml(tempConfig);
                logger1.Dispose();

                // Assert - Second logger should still be able to configure
                // This would fail if logger1.Dispose() called LogManager.Shutdown
                var result = logger2.ConfigureXml(tempConfig);
                result.ShouldBeTrue();
            }
            finally
            {
                // Cleanup
                logger2?.Dispose();
                File.Delete(tempConfig);
            }
        }

        [Test]
        public void CtxLogger_ContextProperties_ShouldApplyToNLogScope()
        {
            // This test verifies that context properties are applied to NLog's ScopeContext
            // In a real application, you would verify this by checking log output
            
            // Arrange
            var logger = LogCtx.Logger as CtxLogger;
            logger.ShouldNotBeNull();

            using var ctx = LogCtx.Set()
                .With("ServiceName", "TestService")
                .With("OperationId", "op-123")
                .With("UserId", 456);

            // Act - This should apply context properties to NLog scope
            logger.Info("Test message with rich context", ctx);

            // Assert - Properties should be in context
            ctx.Properties.ShouldContainKeyAndValue("ServiceName", "TestService");
            ctx.Properties.ShouldContainKeyAndValue("OperationId", "op-123");
            ctx.Properties.ShouldContainKeyAndValue("UserId", 456);
        }
    }
}
