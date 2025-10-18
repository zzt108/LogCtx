// ✅ FULL FILE VERSION
// tests/LogCtxShared.Tests/FakeScopeContext.cs

using LogCtxShared;
using System.Collections.Generic;

namespace SeriLogShared.Tests.Infra
{
    /// <summary>
    /// Deterministic IScopeContext fake for testing LogCtx interactions without Serilog dependency.
    /// Stores Clear calls and PushProperty key/values in-memory for assertions.
    /// </summary>
    public sealed class FakeScopeContext : IScopeContext
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
            Pushed.Add(new KeyValuePair<string, object>(key, value));
        }
    }
}
