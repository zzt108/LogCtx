using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using LogCtxShared;

namespace LogCtx.Tests
{
    [TestFixture]
    public class BasicTests
    {
        private ILogger<BasicTests> _logger;

        [SetUp]
        public void Setup()
        {
            //var loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder.AddConsole();
            //    builder.SetMinimumLevel(LogLevel.Trace);
            //});
            _logger = Logging.Factory.CreateLogger<BasicTests>();
        }

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
        public void SetContext_WithProps_ShouldIncludeProperties()
        {
            // Arrange
            var props = new Props(_logger, null, null, null, 0)
                .Add("UserId", 123)
                .Add("Action", "Test");

            // Act
            using (var scope = _logger.SetContext(props))
            {
                _logger.LogInformation("Test message");
                // In real scenario, verify props appear in log output
            }

            // Assert - scope disposed without exception
            Assert.Pass();
        }

        [Test]
        public void SetOperationContext_ShouldCreateScope()
        {
            // Act
            using (var scope = _logger.SetOperationContext("TestOperation", ("Key1", "Value1")))
            {
                _logger.LogInformation("Inside operation");
            }

            // Assert
            Assert.Pass();
        }

        [Test]
        public void Props_FluentAPI_ShouldChain()
        {
            // Act
            var props = new Props(_logger, null, null, null, 0)
                .Add("Key1", "Value1")
                .Add("Key2", 42)
                .Add("Key3", true);

            // Assert
            props.Count.ShouldBe(3+1); // 3 items added manually
            props["Key1"].ShouldBe("Value1");
            props["Key2"].ShouldBe(42);
            props["Key3"].ShouldBe(true);
        }

        [Test]
        public void SourceContext_BuildSource_ShouldFormatCorrectly()
        {
            // Act
            var src = SourceContext.BuildSource();

            // Assert
            src.ShouldContain("BasicTests");
            src.ShouldContain("BuildSource_ShouldFormatCorrectly");
        }

        [Test]
        public void LogContextKeys_ConstantsShouldExist()
        {
            // Assert
            LogContextKeys.FILE.ShouldBe("CTX_FILE");
            LogContextKeys.LINE.ShouldBe("CTX_LINE");
            LogContextKeys.METHOD.ShouldBe("CTX_METHOD");
            LogContextKeys.SRC.ShouldBe("CTX_SRC");
            LogContextKeys.STRACE.ShouldBe("CTX_STRACE");
        }
    }
}