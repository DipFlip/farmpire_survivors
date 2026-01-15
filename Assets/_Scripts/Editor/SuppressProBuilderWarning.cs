using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class SuppressProBuilderWarning
{
    private class FilteredLogger : ILogHandler
    {
        private readonly ILogHandler defaultHandler;

        public FilteredLogger(ILogHandler defaultHandler)
        {
            this.defaultHandler = defaultHandler;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            string message = string.Format(format, args);
            if (logType == LogType.Warning && message.Contains("ProBuilder") && message.Contains("SerializeReference"))
                return;

            defaultHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(System.Exception exception, Object context)
        {
            defaultHandler.LogException(exception, context);
        }
    }

    static SuppressProBuilderWarning()
    {
        Debug.unityLogger.logHandler = new FilteredLogger(Debug.unityLogger.logHandler);
    }
}
