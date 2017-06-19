using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Hook
{
    public static class TraceSourceHook
    {
        private static readonly ConcurrentDictionary<string, Lazy<TraceSource>> Sources
            = new ConcurrentDictionary<string, Lazy<TraceSource>>();

        public static void TraceWrite(string assemblyName, string methodName, params object[] p)
        {
            var lazy = Sources.GetOrAdd(assemblyName, new Lazy<TraceSource>(() => new TraceSource(assemblyName)));
            var traceSource = lazy.Value;

            if ((traceSource.Switch.Level & SourceLevels.Information) > 0)
            {
                traceSource.TraceInformation(methodName);
            }
            
            if ((traceSource.Switch.Level & SourceLevels.Verbose) > 0)
            {
                traceSource.TraceEvent(TraceEventType.Verbose, 1000, "");
            }
        }
    }
}
