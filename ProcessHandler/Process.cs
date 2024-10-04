using Windows.Win32.System.Threading;
using System.Runtime.InteropServices;
using CoolHook.Memory;
using CoolHook.Logger;

using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace ProcessHandler
{
    /// <summary>
    /// Represents a process in the system and provides functionality to interact with it.
    /// </summary>
    public class Process : IMemoryProcessHandle
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class with the specified process name.
        /// </summary>
        /// <param name="processName">The name of the process to interact with. The name should not include the ".exe" extension.</param>
        public Process(string processName, ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log($"Initializing process with name: {processName}");

            if (processName == null)
            {
                _logger?.LogError("Process name is null.");
                throw new ArgumentNullException("Process Name is null");
            }

            ProcessName = processName;

            try
            {
                ProcessHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, true, GetProcessID());
                CurrentProcess = GetProcess();
                _logger?.Log($"Successfully opened process: {processName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to open process {processName}. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class using the process ID.
        /// </summary>
        /// <param name="processID">ID of the process to interact with.</param>
        public Process(int processID, ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log($"Initializing process with ID: {processID}");

            if (processID == 0)
            {
                _logger?.LogError("Process ID is null.");
                throw new ArgumentException("Process ID is null.");
            }

            try
            {
                ProcessHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, true, (uint)processID);
                CurrentProcess = GetProcess();
                _logger?.Log($"Successfully opened process with ID: {processID}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to open process with ID: {processID}. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Gets the safe handle to the process.
        /// </summary>
        public SafeHandle ProcessHandle { get; }

        /// <summary>
        /// Get the current process with class System.Diagnostics.Process.
        /// </summary>
        public System.Diagnostics.Process CurrentProcess { get; }

        /// <summary>
        /// Retrieves the process ID for the process with the specified name.
        /// </summary>
        /// <returns>The process ID of the specified process.</returns>
        private uint GetProcessID()
        {
            _logger?.Log($"Getting process ID for process name: {ProcessName}");

            var process = GetProcess();
            if (process == null)
            {
                _logger?.LogError("Process class is null. Failed to retrieve process ID.");
                throw new NullReferenceException("Process class is null");
            }

            _logger?.Log($"Successfully retrieved process ID: {process.Id}");
            return (uint)process.Id;
        }

        /// <summary>
        /// Method for get the process by name. 
        /// </summary>
        /// <returns>process class with System.Diagnostics.Process type.</returns>
        private System.Diagnostics.Process GetProcess()
        {
            _logger?.Log($"Attempting to get process by name: {ProcessName}");

            string procName = ProcessName.Replace(".exe", "");
            var process = System.Diagnostics.Process.GetProcessesByName(procName)[0];

            _logger?.Log($"Successfully retrieved process: {procName}");
            return process;
        }
    }
}
