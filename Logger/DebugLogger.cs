using System.Diagnostics;

namespace CoolHook.Logger
{
    internal class DebugLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.WriteLine($"INFO: {message}");
        }

        public void LogError(string message)
        {
            Debug.WriteLine($"ERROR: {message}");
        }

        public void LogWarning(string message)
        {
            Debug.WriteLine($"WARNING: {message}");
        }
    }
}
