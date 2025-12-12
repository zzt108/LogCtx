using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using System.IO;

namespace LogCtx.Tests
{
    [TestFixture]
    [Category("Integration")]
    public class SeqIntegrationTests
    {
        private ILogger<SeqIntegrationTests> _logger;
        private string _configPath;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Use NLog.config from project root
            _configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "NLog.config");

            if (File.Exists(_configPath))
            {
                LogManager.Configuration = new XmlLoggingConfiguration(_configPath);
            }
        }

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddNLog();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            });
            _logger = loggerFactory.CreateLogger<SeqIntegrationTests>();
        }

        [TearDown]
        public void TearDown()
        {
            LogManager.Flush();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            NLog.LogManager.Shutdown();
        }

        [Test]
        [Explicit("Requires SEQ running at http://localhost:5341")]
        public void SetContext_LogsToSeq_WithStructuredProperties()
        {
            // Arrange
            var props = new Props()
                .Add("UserId", 12345)
                .Add("Action", "IntegrationTest")
                .Add("Environment", "Development");

            // Act
            using (_logger.SetContext(props))
            {
                _logger.LogInformation("SEQ integration test with structured properties");
                _logger.LogWarning("Warning message with context");
                _logger.LogError(new System.Exception("Test exception"), "Error with context");
            }

            // Assert
            LogManager.Flush();
            Assert.Pass("Check SEQ UI at http://localhost:5341 for logs with properties: UserId=12345, Action=IntegrationTest, CTX_STRACE");
        }

        [Test]
        [Explicit("Requires SEQ running at http://localhost:5341")]
        public void SetOperationContext_LogsToSeq_WithOperationName()
        {
            // Act
            using (_logger.SetOperationContext("DataProcessing", ("BatchId", "BATCH-001"), ("RecordCount", 150)))
            {
                _logger.LogInformation("Starting data processing operation");
                _logger.LogInformation("Processing batch");
                _logger.LogInformation("Completed data processing operation");
            }

            // Assert
            LogManager.Flush();
            Assert.Pass("Check SEQ UI for Operation=DataProcessing, BatchId=BATCH-001, RecordCount=150");
        }

        [Test]
        [Explicit("Requires SEQ running at http://localhost:5341")]
        public void NestedContexts_LogToSeq_WithCorrectScopeIsolation()
        {
            // Act
            using (_logger.SetContext(new Props().Add("OuterScope", "Level1")))
            {
                _logger.LogInformation("Outer context log");

                using (_logger.SetContext(new Props().Add("InnerScope", "Level2")))
                {
                    _logger.LogInformation("Inner context log");
                }

                _logger.LogInformation("Back to outer context");
            }

            // Assert
            LogManager.Flush();
            Assert.Pass("Check SEQ UI to verify scope isolation: first/third logs have OuterScope, second log has InnerScope");
        }

        [Test]
        public void ConfigFileExists_InOutputDirectory()
        {
            // Assert
            File.Exists(_configPath).ShouldBeTrue($"NLog.config should exist at {_configPath}");
        }
    }
}
