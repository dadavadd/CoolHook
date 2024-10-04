using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using System.Runtime.InteropServices;
using ProcessHandler;
using static Windows.Win32.PInvoke;
using CoolHook.Logger;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace CoolHook.Memory.Injector
{
    /// <summary>
    /// The DllInjector class provides methods for injecting a DLL into a target process using Windows API.
    /// </summary>
    public unsafe class DllInjector
    {
        /// <summary>
        /// Injects a DLL into the specified process.
        /// </summary>
        /// <param name="process">The target process interface into which the DLL will be injected.</param>
        /// <param name="dllPath">The path to the DLL file to be injected.</param>
        /// <param name="logger">The ILogger interface for logging any happened in code.</param>
        /// <returns>Returns true if the DLL injection is successful, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the process is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified DLL file is not found.</exception>
        public static bool InjectDLL(IMemoryProcessHandle process, string dllPath, ILogger logger = null)
        {
            logger?.Log($"Attempting to inject DLL: {dllPath}");

            if (process == null)
            {
                logger?.LogError("Process handle is null.");
                throw new ArgumentNullException(nameof(process));
            }

            if (!File.Exists(dllPath))
            {
                logger?.LogError($"DLL file not found: {dllPath}");
                throw new FileNotFoundException(nameof(dllPath));
            }

            try
            {
                byte[] dllData = File.ReadAllBytes(dllPath);
                logger?.Log($"Read DLL data from path: {dllPath}, Size: {dllData.Length} bytes");

                void* allocatedMemory = VirtualAllocEx(
                    process.ProcessHandle,
                    null,
                    (nuint)dllPath.Length,
                    VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
                    PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE);

                if (allocatedMemory == null)
                {
                    logger?.LogError("Failed to allocate memory in target process.");
                    return false;
                }

                logger?.Log($"Allocated memory in target process at address: {((nint)allocatedMemory).ToString("X")}");

                fixed (void* ptr = dllData)
                {
                    if (!WriteProcessMemory(process.ProcessHandle, allocatedMemory, ptr, (nuint)dllData.Length, null))
                    {
                        logger?.LogError("Failed to write DLL data to target process memory.");
                        return false;
                    }
                }

                logger?.Log("DLL data successfully written to target process memory.");

                var remoteThread = CreateRemoteThread(process.ProcessHandle, null, 0, LoadLibraryA, allocatedMemory, 0, null);
                if (remoteThread == null)
                {
                    logger?.LogError("Failed to create remote thread in target process.");
                    return false;
                }

                logger?.Log($"Remote thread created in target process. Handle: {remoteThread.DangerousGetHandle().ToInt64():X}");

                var waitEvent = WaitForSingleObject(remoteThread, 10000);

                if (waitEvent == WAIT_EVENT.WAIT_ABANDONED || waitEvent == WAIT_EVENT.WAIT_TIMEOUT)
                {
                    logger?.LogError("Remote thread wait timed out or was abandoned.");

                    if (remoteThread != null)
                        CloseHandle((HANDLE)remoteThread.DangerousGetHandle());

                    return false;
                }

                VirtualFreeEx(process.ProcessHandle, allocatedMemory, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
                logger?.Log($"Memory at address {((nint)allocatedMemory).ToString("X")} has been released.");

                if (remoteThread != null)
                {
                    CloseHandle((HANDLE)remoteThread.DangerousGetHandle());
                    logger?.Log("Remote thread handle closed.");
                }

                logger?.Log("DLL injection completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error during DLL injection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// A method executed in the remote process to load the DLL.
        /// </summary>
        /// <param name="parameter">The pointer to the DLL path allocated in the remote process memory.</param>
        /// <returns>Returns 0 when the DLL is loaded.</returns>
        public unsafe static uint LoadLibraryA(void* parameter)
        {
            string dllPath = Marshal.PtrToStringAnsi((IntPtr)parameter);
            LoadLibrary(dllPath);
            return 0;
        }
    }
}
