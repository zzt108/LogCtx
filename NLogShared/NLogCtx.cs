using LogCtxShared;
using NLog;

namespace NLogAdapter
{
    public class NLogCtx 
    {
        private Logger? Logger;
        public bool CanLog { get; set; }
        public string Config { get; set; }
        public bool Init()
        {
            Logger = CanLog ? LogManager.GetCurrentClassLogger() : null;
            return Logger is not null;
        }
    }

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
