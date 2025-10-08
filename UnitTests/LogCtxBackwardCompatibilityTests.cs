// ✅ FULL FILE VERSION
// File: UnitTests/LogCtxBackwardCompatibilityTests.cs
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared; // For test initialization only
using System;

namespace UnitTests.LogCtx
{
    /// <summary>
    /// Ensures backward compatibility is maintained during LogCtx fluent API upgrade
    /// Tests against the logger-agnostic LogCtxShared implementation
    /// </summary>
    [TestFixture]
    public class LogCtxFluentApiTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // ✅ EXISTING PATTERN - Initialize LogCtx once per test fixture
            FailsafeLogger.Initialize("NLog.config");
        }

        [Test]
        public void LogCtxSet_TraditionalUsage_ShouldWorkUnchanged()
        {
            // Arrange & Act - Traditional API usage
            using var ctx = LogCtx.Set();
            ctx.AddProperty("Operation", "TestOperation");
            ctx.AddProperty("UserId", 12345);
            
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("Operation", "TestOperation");
            ctx.Properties.ShouldContainKeyAndValue("UserId", 12345);
            ctx.Properties.ShouldContainKey("MemberName"); // Auto-added
            ctx.Properties.ShouldContainKey("SourceFile"); // Auto-added
            ctx.Properties.ShouldContainKey("LineNumber"); // Auto-added
        }

        [Test]
        public void LogCtxSet_FluentUsage_ShouldChainProperly()
        {
            // Arrange & Act - New fluent API
            using var ctx = LogCtx.Set()
                .With("Operation", "FluentTest")
                .With("RequestId", Guid.NewGuid())
                .With("Timestamp", DateTime.UtcNow);
            
            // Assert
            ctx.Properties.Count.ShouldBeGreaterThan(3); // Base properties + added ones
            ctx.Properties.ShouldContainKeyAndValue("Operation", "FluentTest");
            ctx.ShouldBeOfType<LogCtx>(); // Ensures chaining returns correct type
        }

        [Test] 
        public void LogCtx_MixedApiUsage_ShouldWorkSeamlessly()
        {
            // Arrange & Act - Mix traditional and fluent APIs
            using var ctx = LogCtx.Set()
                .With("FluentProperty", "FluentValue"); // Fluent API
            
            ctx.AddProperty("TraditionalProperty", "TraditionalValue"); // Traditional API
            
            ctx.With("AnotherFluentProperty", "AnotherFluentValue"); // Back to fluent
            
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("FluentProperty", "FluentValue");
            ctx.Properties.ShouldContainKeyAndValue("TraditionalProperty", "TraditionalValue");
            ctx.Properties.ShouldContainKeyAndValue("AnotherFluentProperty", "AnotherFluentValue");
        }

        [Test]
        public void LogCtx_WithAnonymousObject_ShouldAddAllProperties()
        {
            // Arrange
            var testData = new 
            { 
                UserId = 123,
                UserName = "TestUser",
                IsActive = true,
                LastLogin = DateTime.UtcNow
            };
            
            // Act
            using var ctx = LogCtx.Set()
                .With(testData);
            
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("UserId", 123);
            ctx.Properties.ShouldContainKeyAndValue("UserName", "TestUser");
            ctx.Properties.ShouldContainKeyAndValue("IsActive", true);
            ctx.Properties.ShouldContainKey("LastLogin");
        }

        [Test]
        public void LogCtx_WithException_ShouldAddExceptionContext()
        {
            // Arrange
            var testException = new InvalidOperationException("Test exception message");
            
            // Act
            using var ctx = LogCtx.Set()
                .WithException(testException);
            
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("ErrorType", "InvalidOperationException");
            ctx.Properties.ShouldContainKeyAndValue("ErrorMessage", "Test exception message");
            ctx.Properties.ShouldContainKey("ErrorStackTrace");
        }

        [Test]
        public void LogCtx_ConditionalProperties_ShouldAddWhenTrue()
        {
            // Arrange & Act
            using var ctx = LogCtx.Set()
                .WithIf(true, "ConditionalTrue", "Added")
                .WithIf(false, "ConditionalFalse", "NotAdded");
            
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("ConditionalTrue", "Added");
            ctx.Properties.ShouldNotContainKey("ConditionalFalse");
        }

        [Test]
        public void LogCtx_FluentLogging_ShouldReturnContextForChaining()
        {
            // Arrange & Act
            using var ctx = LogCtx.Set()
                .With("Operation", "ChainTest")
                .LogInfo("Test message")  // Should return LogCtx for continued chaining
                .With("AfterLog", "AfterLogValue");
                
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("Operation", "ChainTest");
            ctx.Properties.ShouldContainKeyAndValue("AfterLog", "AfterLogValue");
        }

        [Test]
        public void LogCtx_Logger_ShouldBeNullSafeForFluent()
        {
            // Arrange - temporarily clear logger
            var originalLogger = LogCtx.Logger;
            LogCtx.Logger = null;
            
            try
            {
                // Act - should not throw even with null logger
                using var ctx = LogCtx.Set()
                    .With("Test", "Value")
                    .LogInfo("This should not crash")  // Null-safe
                    .LogError("Error test")            // Null-safe
                    .With("After", "Logging");
                
                // Assert - context properties should still work
                ctx.Properties.ShouldContainKeyAndValue("Test", "Value");
                ctx.Properties.ShouldContainKeyAndValue("After", "Logging");
            }
            finally
            {
                // Restore logger
                LogCtx.Logger = originalLogger;
            }
        }

        [Test]
        public void LogCtx_WithTiming_ShouldAddTimingProperties()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(1500);
            
            // Act
            using var ctx = LogCtx.Set()
                .WithTiming("ProcessData", duration);
            
            // Assert
            ctx.Properties.ShouldContainKeyAndValue("ProcessDataDurationMs", 1500.0);
            ctx.Properties.ShouldContainKey("ProcessDataDuration");
        }
    }
}
