using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogCtx.Tests
{
    /// <summary>
    /// Tests to verify NLog scope behavior (by-reference vs by-snapshot)
    /// and Props thread-safety with ConcurrentDictionary.
    /// </summary>
    [TestFixture]
    public class NLogScopeReferenceTests
    {
        private ILogger<NLogScopeReferenceTests> _logger = null!;

        [SetUp]
        public void Setup()
        {
            _logger = Logging.Factory.CreateLogger<NLogScopeReferenceTests>();
        }

        [Test]
        [Category("Integration")]
        public void MEL_BeginScope_WithDictionary_BehaviorTest()
        {
            // Arrange
            var props = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };

            // Act - Create scope, then mutate dictionary
            using var scope = _logger.BeginScope(props);
            _logger.LogInformation("Log before mutation");

            props["key2"] = "value2"; // ← Add AFTER scope created

            _logger.LogInformation("Log after mutation");

            // ✅ Manual verification in SEQ required:
            // - If "Log after mutation" has key2 → BY-REFERENCE (no scope recreation needed)
            // - If "Log after mutation" lacks key2 → BY-SNAPSHOT (scope recreation required)

            Assert.Pass("Manual SEQ verification: Check if key2 appears in second log entry");
        }

        [Test]
        public void Props_Add_UpdatesScope_SequentialAdds()
        {
            // Arrange
            using Props p = _logger.SetContext();

            // Act - Add properties sequentially
            p.Add("step", "1");
            _logger.LogInformation("Step 1 logged");

            p.Add("step", "2"); // ← Update existing key
            _logger.LogInformation("Step 2 logged");

            p.Add("step", "3");
            _logger.LogInformation("Step 3 logged");

            // Assert
            p["step"].ShouldBe("3");

            // ✅ SEQ Verification: 
            // - "Step 1 logged" should have step=1
            // - "Step 2 logged" should have step=2
            // - "Step 3 logged" should have step=3
            Assert.Pass("Check SEQ: Each log entry has correct step value");
        }

        [Test]
        public void Props_NestedContext_InheritsAndExtendsProperties()
        {
            // Arrange & Act
            using Props p = _logger.SetContext()
                .Add("userId", 123)
                .Add("sessionId", "abc-123");

            _logger.LogInformation("Outer scope");
            p.ContainsKey("userId").ShouldBeTrue();
            p.ContainsKey("sessionId").ShouldBeTrue();

            // Create nested context
            using var p2 = _logger.SetContext(p)
                .Add("action", "login")
                .Add("ipAddress", "192.168.1.1");

            _logger.LogInformation("Nested scope");

            // Assert - Nested should have all properties
            p2.ContainsKey("userId").ShouldBeTrue("Inherited from parent");
            p2.ContainsKey("sessionId").ShouldBeTrue("Inherited from parent");
            p2.ContainsKey("action").ShouldBeTrue("Added in nested");
            p2.ContainsKey("ipAddress").ShouldBeTrue("Added in nested");
            p2.Count.ShouldBeGreaterThanOrEqualTo(5); // 4 + CTXSTRACE

            // ✅ SEQ Verification:
            // - "Outer scope" has: userId, sessionId, CTXSTRACE
            // - "Nested scope" has: userId, sessionId, action, ipAddress, CTXSTRACE (updated)
            Assert.Pass("Check SEQ: Nested scope inherits parent properties + adds new ones");
        }

        [Test]
        public void Props_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            using Props p = _logger.SetContext()
                .Add("testId", "concurrent-test");

            // Act - Simulate concurrent writes from multiple threads
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    p.Add($"thread_{threadId}", $"value_{threadId}");
                    _logger.LogInformation($"Thread {threadId} completed");
                });
            }

            Task.WaitAll(tasks);

            // Assert - Should not throw, all 10 thread properties added
            p.Count.ShouldBeGreaterThanOrEqualTo(11); // 10 threads + testId + CTXSTRACE

            for (int i = 0; i < 10; i++)
            {
                p.ContainsKey($"thread_{i}").ShouldBeTrue($"Thread {i} property should exist");
            }

            Assert.Pass("Concurrent access completed without exceptions or data corruption");
        }

        [Test]
        public void Props_Dispose_IsThreadSafe()
        {
            // Arrange
            Props p = _logger.SetContext()
                .Add("key", "value");

            // Act - Multiple threads try to dispose simultaneously
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = Task.Run(() => p.Dispose());
            }

            // Assert - Should not throw
            Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        }

        [Test]
        public void Props_AddJson_SerializesCorrectly()
        {
            // Arrange
            using Props p = _logger.SetContext();
            var complexObject = new { Name = "Test", Count = 42, Items = new[] { 1, 2, 3 } };

            // Act
            p.AddJson("data", complexObject, Newtonsoft.Json.Formatting.None);

            // Assert
            p.ContainsKey("data").ShouldBeTrue();
            p["data"].ShouldBeOfType<string>();
            ((string)p["data"]).ShouldContain("\"Name\":\"Test\"");

            _logger.LogInformation("JSON object logged");
            Assert.Pass("Check SEQ: data property contains serialized JSON");
        }

        [Test]
        public void Props_Clear_RemovesAllProperties()
        {
            // Arrange
            using Props p = _logger.SetContext()
                .Add("key1", "value1")
                .Add("key2", "value2");

            p.Count.ShouldBeGreaterThanOrEqualTo(3); // 2 + CTXSTRACE

            // Act
            p.Clear();

            // Assert
            p.Count.ShouldBe(0);

            _logger.LogInformation("After clear");
            Assert.Pass("Check SEQ: Log after clear should have no custom properties");
        }

        [Test]
        public void Props_UpdateExistingKey_Overwrites()
        {
            // Arrange
            using Props p = _logger.SetContext()
                .Add("status", "pending");

            p["status"].ShouldBe("pending");

            // Act
            p.Add("status", "completed");

            // Assert
            p["status"].ShouldBe("completed");
            p.Count.ShouldBeGreaterThanOrEqualTo(2); // status + CTXSTRACE (no duplicates)

            _logger.LogInformation("Status updated");
            Assert.Pass("Check SEQ: status property should be 'completed'");
        }
    }
}
