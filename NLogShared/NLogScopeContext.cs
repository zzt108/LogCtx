using LogCtxShared;

namespace NLogShared
{
    public class NLogScopeContext : IScopeContext
    {
        public void Clear()
        {
            NLog.ScopeContext.Clear();
        }

        public void PushProperty(string key, object value)
        {
            NLog.ScopeContext.PushProperty(key, value);
        }
    }
}