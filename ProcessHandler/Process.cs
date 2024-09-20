﻿using Windows.Win32.System.Threading;
using System.Runtime.InteropServices;

using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace ProcessHandler
{
    /// <summary>
    /// Represents a process in the system and provides functionality to interact with it.
    /// </summary>
    public class Process
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class with the specified process name.
        /// </summary>
        /// <param name="processName">The name of the process to interact with. The name should not include the ".exe" extension.</param>
        public Process(string processName)
        {
            if (processName == null)
                throw new ArgumentNullException("Process Name is null");


            ProcessName = processName;
            ProcessHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, true, GetProcessID());
            CurrentProcess = GetProcess();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class using the process ID.
        /// </summary>
        /// <param name="processID">ID of the process to interact with.</param>
        public Process(int processID)
        {
            if (processID == 0)
                throw new ArgumentException("Process ID is null.");

            ProcessHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, true, (uint)processID);
            CurrentProcess = GetProcess();
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
            var process = GetProcess();

            if (process == null)
                throw new NullReferenceException("Process class is null");

            return (uint)process.Id;
        }

        /// <summary>
        /// Method for get the process by name. 
        /// </summary>
        /// <returns>process class with System.Diagnostics.Process type.</returns>
        private System.Diagnostics.Process GetProcess()
        {
            string procName = ProcessName.Replace(".exe", "");
            var process = System.Diagnostics.Process.GetProcessesByName(procName)[0];

            return process;
        }
    }
}
