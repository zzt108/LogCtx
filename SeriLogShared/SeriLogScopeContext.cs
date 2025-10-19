using LogCtxShared;
using Serilog.Context;
using System;

namespace SeriLogShared
{
    public class SeriLogScopeContext : IScopeContext
    {
        public void Clear()
        {
            LogContext.Reset();
        }

        public void PushProperty(string key, object value)
        {
            LogContext.PushProperty(key, value);
        }
    }
}
