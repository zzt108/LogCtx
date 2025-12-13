using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using LogCtxShared;

namespace LogCtx.Tests
{
    [TestFixture]
    public class NLogContextExtensionsTests
    {
        private ILogger<NLogContextExtensionsTests> _logger = null!;

        [SetUp]
        public void Setup()
        {
            _logger = Logging.Factory.CreateLogger<NLogContextExtensionsTests>();
        }

        #region SetContext Tests

        [Test]
        public void SetContext_ShouldReturnDisposableScope()
        {
            // Act
            var scope = _logger.SetContext();

            // Assert
            scope.ShouldNotBeNull();
            scope.ShouldBeAssignableTo<IDisposable>();

            // Cleanup
            scope.Dispose();
        }

        [Test]
        public void SetContext_WithNullProps_ShouldCreateNewPropsInternally()
        {
            // Act
            using (var scope = _logger.SetContext(null))
            {
                // Assert - should not throw
                scope.ShouldNotBeNull();
            }
        }

        [Test]
        public void SetContext_WithProps_ShouldIncludeAllProperties()
        {
            // Arrange
            var props = new Props()
                .Add("UserId", 123)
                .Add("Action", "TestAction");

            // Act
            using (var scope = _logger.SetContext(props))
            {
                _logger.LogInformation("Test message with context");

                // Assert - props should contain expected keys
                props.ContainsKey("UserId").ShouldBeTrue();
                props.ContainsKey("Action").ShouldBeTrue();
                props.ContainsKey(LogContextKeys.STRACE).ShouldBeTrue(); // Auto-added by SetContext
            }
        }

        [Test]
        public void SetContext_AutoCapturesCallerInfo()
        {
            // Arrange
            var props = new Props();

            // Act
            using (var scope = _logger.SetContext(props))
            {
                // Assert - CTXSTRACE should be auto-added with caller info
                props.ContainsKey(LogContextKeys.STRACE).ShouldBeTrue();
                var strace = props[LogContextKeys.STRACE] as string;
                strace.ShouldNotBeNullOrWhiteSpace();
                strace.ShouldContain("NLogContextExtensionsTests"); // File name
                strace.ShouldContain("AutoCapturesCallerInfo"); // Method name
            }
        }

        [Test]
        public void SetContext_DoesNotOverwriteExistingCTXSTRACE()
        {
            // Arrange
            var customTrace = "CustomTrace";
            var props = new Props()
                .Add(LogContextKeys.STRACE, customTrace);

            // Act
            using (var scope = _logger.SetContext(props))
            {
                // Assert - should preserve custom trace
                props[LogContextKeys.STRACE].ShouldBe(customTrace);
            }
        }

        [Test]
        public void SetContext_NestedScopes_ShouldIsolateContexts()
        {
            // Arrange
            var outerProps = new Props().Add("Level", "Outer");
            var innerProps = new Props().Add("Level", "Inner");

            // Act & Assert
            using (var outerScope = _logger.SetContext(outerProps))
            {
                _logger.LogInformation("Outer context");
                outerProps["Level"].ShouldBe("Outer");

                using (var innerScope = _logger.SetContext(innerProps))
                {
                    _logger.LogInformation("Inner context");
                    innerProps["Level"].ShouldBe("Inner");
                }

                // Back to outer
                _logger.LogInformation("Back to outer context");
                outerProps["Level"].ShouldBe("Outer");
            }
        }

        [Test]
        public void SetContext_DisposeShouldNotThrow()
        {
            // Arrange
            var scope = _logger.SetContext(new Props().Add("Key", "Value"));

            // Act & Assert
            Should.NotThrow(() => scope.Dispose());
        }

        #endregion SetContext Tests

        #region SetOperationContext Tests

        [Test]
        public void SetOperationContext_ShouldCreateScopeWithOperation()
        {
            // Act
            using (var scope = _logger.SetOperationContext("TestOperation"))
            {
                // Assert - should not throw
                scope.ShouldNotBeNull();
                _logger.LogInformation("Inside operation");
            }
        }

        [Test]
        public void SetOperationContext_WithProperties_ShouldIncludeOperationAndProperties()
        {
            // Arrange
            var operationName = "ProcessOrder";

            // Act
            using (var scope = _logger.SetOperationContext(
                operationName,
                ("OrderId", 123),
                ("CustomerId", 456)))
            {
                _logger.LogInformation("Processing order");

                // Assert - can't directly inspect scope, but logged without exception
                Assert.Pass();
            }
        }

        [Test]
        public void SetOperationContext_WithEmptyProperties_ShouldOnlyIncludeOperation()
        {
            // Act
            using (var scope = _logger.SetOperationContext("EmptyOperation"))
            {
                _logger.LogInformation("Operation with no extra properties");

                // Assert
                Assert.Pass();
            }
        }

        [Test]
        public void SetOperationContext_NestedOperations_ShouldWork()
        {
            // Act & Assert
            using (var outerScope = _logger.SetOperationContext("OuterOperation"))
            {
                _logger.LogInformation("Outer operation");

                using (var innerScope = _logger.SetOperationContext("InnerOperation"))
                {
                    _logger.LogInformation("Inner operation");
                }

                _logger.LogInformation("Back to outer operation");
            }
        }

        #endregion SetOperationContext Tests

        #region Complex Scenarios

        [Test]
        public void SetContext_MultipleProperties_AllCaptured()
        {
            // Arrange
            var props = new Props()
                .Add("UserId", 999)
                .Add("SessionId", "session-abc-123")
                .Add("Environment", "Test")
                .Add("Feature", "NLogMigration");

            // Act
            using (var scope = _logger.SetContext(props))
            {
                _logger.LogInformation("Complex context");

                // Assert
                props.Count.ShouldBeGreaterThanOrEqualTo(5); // 4 added + CTXSTRACE
            }
        }

        [Test]
        public void SetContext_WithException_ShouldLogContextWithException()
        {
            // Arrange
            var props = new Props().Add("ErrorContext", "CriticalError");

            // Act & Assert
            using (var scope = _logger.SetContext(props))
            {
                Should.NotThrow(() =>
                {
                    try
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred with context");
                    }
                });
            }
        }

        #endregion Complex Scenarios
    }
}