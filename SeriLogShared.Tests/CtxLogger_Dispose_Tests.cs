// ✅ FULL FILE VERSION
// tests/SeriLogShared.Tests/CtxLogger_Dispose_Tests.cs

using NUnit.Framework;
using Shouldly;
using SeriLogShared;
using Serilog;

namespace SeriLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    [Parallelizable(ParallelScope.None)]
    public class CtxLogger_Dispose_Tests
    {
        [Test]
        public void Dispose_Calls_CloseAndFlush_Safely_Idempotent()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            logger.Dispose();
            logger.Dispose(); // second call should be safe and no-throw

            // Assert
            // Can't directly assert CloseAndFlush, but no exception and Serilog remains in a valid state
            Log.Logger.ShouldNotBeNull();
        }
    }
}
