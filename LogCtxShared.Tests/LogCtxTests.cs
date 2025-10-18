// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogCtxShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class LogCtxTests
    {
        private readonly CtxLogger Log = new CtxLogger();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Dispose Log to satisfy NUnit1032
            Log.Dispose();
        }

        [Test]
        public void SetWithPropsClearsScopePushesCtxStraceAndPropsReturnsEnrichedProps()
        {
            // Arrange
            var scope = new FakeScopeContext();
            var props = new Props("A", "B");

            // Act
            var enriched = Log.Ctx.Set(props);

            // Assert
            scope.Cleared.ShouldBeTrue();
            scope.Pushed.ShouldContain(kv => kv.Key == "CTXSTRACE" && kv.Value is string && !string.IsNullOrWhiteSpace((string)kv.Value));
            scope.Pushed.ShouldContain(kv => kv.Key == "P00" && kv.Value != null && kv.Value.ToString() == "A".AsJson(true));
            scope.Pushed.ShouldContain(kv => kv.Key == "P00" && kv.Value != null && kv.Value.ToString() == "B".AsJson(true));
            enriched.ShouldNotBeNull();
            enriched.ContainsKey("CTXSTRACE").ShouldBeTrue();
            enriched["P00"].ShouldBe("\"A\"");
            enriched["P01"].ShouldBe("\"B\"");
        }

        [Test]
        public void SetWithNullPropsStillCreatesCtxStraceAndReturnsProps()
        {
            // Arrange
            var scope = new FakeScopeContext();

            // Act
            var enriched = Log.Ctx.Set(null);

            // Assert
            scope.Cleared.ShouldBeTrue();
            scope.Pushed.ShouldContain(kv => kv.Key == "CTXSTRACE");
            enriched.ShouldNotBeNull();
            enriched.ContainsKey("CTXSTRACE").ShouldBeTrue();
            (enriched["CTXSTRACE"] as string).ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void SetCtxStraceFormatBeginsWithFileMethodLineAndFiltersTestNoise()
        {
            // Arrange
            var scope = new FakeScopeContext();

            // Act
            var enriched = Log.Ctx.Set(new Props("X"));

            // Assert
            var s = enriched["CTXSTRACE"] as string;
            s.ShouldNotBeNullOrWhiteSpace();
            // Expect pattern like "FileName.csMethodName123"
            Regex.IsMatch(s, @"\w+\.cs\w+\d+").ShouldBeTrue($"Unexpected CTXSTRACE header: {s}");
            // Heuristic filter checks: avoid common framework noise lines when possible
            s.IndexOf(" at NUnit.", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        }

        [Test]
        public void SrcReturnsFileMethodLineToken()
        {
            // Act
            var src = Log.Ctx.Src("Message parameter");

            // Assert
            src.ShouldNotBeNullOrWhiteSpace();
            // Example: "FileName.MethodName.123"
            Regex.IsMatch(src, @"[A-Za-z0-9_\-\.?]+\.[A-Za-z0-9_?]+\.\d+").ShouldBeTrue($"Unexpected Src format: {src}");
        }

        [Test]
        public void SetPushesAllProvidedKeysAsStringsInScope()
        {
            // Arrange
            var scope = new FakeScopeContext();
            var props = new Props();
            props.Add("P00", 123);
            props.Add("P01", true);
            props.Add("Custom", "Z");

            // Act
            var enriched = Log.Ctx.Set(props);

            // Assert
            scope.Pushed.ShouldContain(kv => kv.Key == "P00" && kv.Value != null && kv.Value.ToString() == "A".AsJson(true));
            scope.Pushed.ShouldContain(kv => kv.Key == "P01" && kv.Value != null && kv.Value.ToString() == "A".AsJson(true));
            scope.Pushed.ShouldContain(kv => kv.Key == "Custom" && kv.Value != null && kv.Value.ToString() == "A".AsJson(true));
            enriched["P00"].ShouldBe(123);
            enriched["P01"].ShouldBe(true);
            enriched["Custom"].ShouldBe("Z");
        }

        [Test]
        public void SetDoesNotMutateOriginalPropsInstanceReference()
        {
            // Arrange
            var scope = new FakeScopeContext();
            var original = new Props("one");

            // Act
            var enriched = Log.Ctx.Set(original);

            // Assert
            ReferenceEquals(enriched, original).ShouldBeTrue("Set should enrich and return the same Props instance");
            enriched.ContainsKey("CTXSTRACE").ShouldBeTrue();
        }

        private sealed class FakeScopeContext : IScopeContext
        {
            public bool Cleared { get; private set; }
            public List<KeyValuePair<string, object>> Pushed { get; } = new List<KeyValuePair<string, object>>();

            public void Clear()
            {
                Cleared = true;
                Pushed.Clear();
            }

            public void PushProperty(string key, object value)
            {
                // Simulate behavior that scope stores string representations
                Pushed.Add(new KeyValuePair<string, object>(key, value?.ToString()));
            }
        }
    }
}
