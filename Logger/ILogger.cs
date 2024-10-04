namespace CoolHook.Logger
{
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
        void LogWarning(string message);
    }
}
