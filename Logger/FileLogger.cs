namespace CoolHook.Logger
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            WriteToFile($"INFO: {message}");
        }

        public void LogError(string message)
        {
            WriteToFile($"ERROR: {message}");
        }

        public void LogWarning(string message)
        {
            WriteToFile($"WARNING: {message}");
        }

        private void WriteToFile(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
        }
    }
}
