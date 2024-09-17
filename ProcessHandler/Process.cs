using Windows.Win32.System.Threading;
using System.Runtime.InteropServices;

using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace ProcessHandler
{
    /// <summary>
    /// Represents a process in the system and provides functionality to interact with it.
    /// </summary>
    internal class Process
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class with the specified process name.
        /// </summary>
        /// <param name="processName">The name of the process to interact with. The name should not include the ".exe" extension.</param>
        public Process(string processName)
        {
            ProcessName = processName;
            ProcessHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, GetProcessID());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class using the process ID.
        /// </summary>
        /// <param name="processID">ID of the process to interact with.</param>
        public Process(int processID)
            => ProcessHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, (uint)processID);

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Gets the safe handle to the process.
        /// </summary>
        public SafeHandle ProcessHandle { get; }

        /// <summary>
        /// Retrieves the process ID for the process with the specified name.
        /// </summary>
        /// <returns>The process ID of the specified process.</returns>
        private uint GetProcessID()
        {
            string procName = ProcessName.Replace(".exe", "");
            return (uint)System.Diagnostics.Process.GetProcessesByName(procName)[0].Id;
        }
    }
}
