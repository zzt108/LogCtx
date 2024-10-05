using LogCtx;
using Serilog.Context;
using Serilog.Events;

namespace SeriLogAdapter
{

    public class SeriLogCtx 
    {
        public bool CanLog { get; set; }
        public string Config { get; set; }

        public bool Init()
        {
            //Logger = CanLog ? LogManager.GetCurrentClassLogger() : null;
            //return Logger is not null;
            return false;
        }
    }

    public class SeriLogScopeContext : LogCtx.IScopeContext
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
