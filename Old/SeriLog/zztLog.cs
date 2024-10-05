using Newtonsoft.Json;
using Serilog.Context;
using Serilog.Events;
using System;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

namespace AIA_Test.Common
{
    public class TimeOut
    {
        protected DateTime endTimeOut;
        protected DateTime start;
        protected DateTime lapTime;
        protected int lastIntervalMs;
        protected Func<bool>? condition;
        protected bool throwException = false;

        /// <summary>
        /// Creates a timeot
        /// </summary>
        /// <param name="intervalMs">If empty or -1 then the Timeout is disabled</param>
        /// <param name="condition"></param>
        /// <param name="throwException"></param>
        public TimeOut(int intervalMs = -1,  Func<bool>? condition = null, bool throwException = false)
        {
            Reset(intervalMs, condition);
            this.throwException = throwException;
        }
        /// <summary>
        /// reset timeout
        /// </summary>
        /// <param name="intervalMs">If interval < 1 then disable timeout</param>
        /// <param name="condition"></param>
        public void Reset(int intervalMs, Func<bool>? condition = null)
        {
            Disabled = intervalMs < 1;
            this.condition = condition;
            lastIntervalMs = intervalMs;
            start = DateTime.Now;
            Lap();
            endTimeOut = start.AddMilliseconds(intervalMs);
        }
        /// <summary>
        /// Resets with last timeout interval
        /// </summary>
        public void Reset()
        {
            start = DateTime.Now;
            endTimeOut = start.AddMilliseconds(lastIntervalMs);
        }

        private TimeOut Lap()
        {
            lapTime = DateTime.Now;
            return this;
        }

        public bool IsOver
        {
            get
            {
                bool isOver = Lap().endTimeOut < DateTime.Now && !Disabled;
                if (isOver && throwException)
                {
                    throw new TimeoutException($"Timeout after {lastIntervalMs} ms");
                }
                return isOver || (condition != null && condition());
            }
        }

        public TimeSpan Elapsed
        {
            get => (lapTime - start);
        }
        public int LastIntervalMs 
        {
            get => lastIntervalMs;
        }

        public DateTime StartTime => start;

        public bool Disabled = false;
    }

    public static class LogCtx
    {
        public class Props : Dictionary<string, object>
        {
            public Props()
            {

            }

            public Props(params object[] args)
            {
                int i = 0;
                foreach (var item in args)
                {
                    AddJson($"P{i:x2}", item);
                }
            }

            public Props AddJson(string key, object value)
            {
                Add(key, JsonConvert.SerializeObject(value));
                return this;
            }
        }

        public const string STRACE = "CTX_STRACE";
        public const string SRC = "CTX_SRC";
        public const string FILE = "CTX_FILE";
        public const string METHOD = "CTX_METHOD";
        public const string LINE = "CTX_LINE";
        public static string Src(
            this string message,
           [CallerMemberName] string memberName = "",
           [CallerFilePath] string sourceFilePath = "",
           [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var methodName = memberName;

            return $"{fileName}.{methodName}.{sourceLineNumber}";
        }

        [Obsolete("Use JsonConvert.SerializeObject(obj) extension method AsJson")]
        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static string AsJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        [Obsolete("Use JsonConvert.DeserializeObject<T>(value)")]
        public static T FromJson<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public static string Link(
           [CallerMemberName] string memberName = "",
           [CallerFilePath] string sourceFilePath = "",
           [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            //var methodName = memberName;

            //return $"{fileName}({sourceLineNumber}):";
            return $"{sourceFilePath}({sourceLineNumber}):WT@F";
        }

        public static IDisposable Set(
            Props? dict = null,
            LogEventLevel methodNameLogLevel = LogEventLevel.Verbose,
           [CallerMemberName] string memberName = "",
           [CallerFilePath] string sourceFilePath = "",
           [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            if (dict != null)
            {
                LogContext.Reset();
                foreach (var key in dict.Keys)
                {
                    LogContext.PushProperty(key, dict[key]);
                }
            }
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var methodName = memberName;
            var strace = $"{fileName}::{methodName}::{sourceLineNumber}\r\n";
            var strace2 = $"{fileName}::{methodName}::{sourceLineNumber}\r\n";
            var tr = new System.Diagnostics.StackTrace();
            var sr = tr.ToString().Split('\n');
            var fr = tr.GetFrames();
            foreach (var frame in fr)
            {
                if (!frame.GetMethod().DeclaringType.FullName.StartsWith("System") &&
                    !frame.GetMethod().DeclaringType.FullName.StartsWith("NUnit") &&
                    !frame.GetMethod().DeclaringType.FullName.StartsWith("Serilog") &&
                    !(frame == fr[0] ) &&
                    !(frame == fr[1] ) 
                    ) {
                    strace += $"--{frame.GetMethod()}\n";
                }
            }
            foreach (var frame in sr)
            {
                if (!frame.Trim().StartsWith("at System.") &&
                    !frame.Trim().StartsWith("at NUnit.") &&
                    !frame.Trim().StartsWith("at Serilog.") &&
                    !(frame == sr[0] ) 
                    ) {
                    strace2 += $"--{frame}\n";
                }
            }
            CompositeDisposable ctxList = new()
            {
                //LogContext.PushProperty(SRC, $"{fileName}.{methodName}.{sourceLineNumber}"),

                //LogContext.PushProperty(FILE, fileName),
                //LogContext.PushProperty(METHOD, methodName),
                //LogContext.PushProperty(LINE, sourceLineNumber),

                //LogContext.PushProperty(FILE, tr.ToString()),
                LogContext.PushProperty(FILE, strace2),
                //LogContext.PushProperty(STRACE, strace),
            };
            if (methodNameLogLevel != LogEventLevel.Verbose)
                Log.Write(methodNameLogLevel, $"---> {methodName}.{sourceLineNumber}");
            return ctxList;
        }
    }
    /* SEQ Signals for low level log
{
  "Title": "1 Verbose",
  "Description": "SeriLog",
  "Filters": [
    {
      "Description": null,
      "DescriptionIsExcluded": false,
      "Filter": "@Level = 'Verbose' ci",
      "FilterNonStrict": "@Level == 'Verbose' ci"
    }
  ],
  "Columns": [],
  "IsProtected": false,
  "Grouping": "Explicit",
  "ExplicitGroupName": "@Level",
  "OwnerId": null,
  "Id": "signal-36",
  "Links": {
    "Self": "api/signals/signal-36?version=6",
    "Group": "api/signals/resources"
  }
}

{
  "Title": "2 Debug",
  "Description": "SeriLog",
  "Filters": [
    {
      "Description": null,
      "DescriptionIsExcluded": false,
      "Filter": "@Level = 'Debug' ci",
      "FilterNonStrict": "@Level == 'Debug' ci"
    }
  ],
  "Columns": [],
  "IsProtected": false,
  "Grouping": "Explicit",
  "ExplicitGroupName": "@Level",
  "OwnerId": null,
  "Id": "signal-37",
  "Links": {
    "Self": "api/signals/signal-37?version=5",
    "Group": "api/signals/resources"
  }
}

{
  "Title": "3 Information",
  "Description": "Automatically created",
  "Filters": [
    {
      "Description": null,
      "DescriptionIsExcluded": false,
      "Filter": "@Level = 'Information' ci",
      "FilterNonStrict": "@Level == 'Information' ci"
    }
  ],
  "Columns": [],
  "IsProtected": false,
  "Grouping": "Explicit",
  "ExplicitGroupName": "@Level",
  "OwnerId": null,
  "Id": "signal-38",
  "Links": {
    "Self": "api/signals/signal-38?version=2",
    "Group": "api/signals/resources"
  }
}

{
  "Title": "5 Errors",
  "Description": "Automatically created",
  "Filters": [
    {
      "Description": null,
      "DescriptionIsExcluded": false,
      "Filter": "@Level in ['f', 'fa', 'fat', 'ftl', 'fata', 'fatl', 'Fatal', 'c', 'cr', 'cri', 'crt', 'crit', 'critical', 'alert', 'emerg', 'panic', 'e', 'er', 'err', 'eror', 'erro', 'error'] ci",
      "FilterNonStrict": "@Level in ['f', 'fa', 'fat', 'ftl', 'fata', 'fatl', 'Fatal', 'c', 'cr', 'cri', 'crt', 'crit', 'critical', 'alert', 'emerg', 'panic', 'e', 'er', 'err', 'eror', 'erro', 'error'] ci"
    }
  ],
  "Columns": [],
  "IsProtected": false,
  "Grouping": "Explicit",
  "ExplicitGroupName": "@Level",
  "OwnerId": null,
  "Id": "signal-m33301",
  "Links": {
    "Self": "api/signals/signal-m33301?version=3",
    "Group": "api/signals/resources"
  }
}
    
     */
}
