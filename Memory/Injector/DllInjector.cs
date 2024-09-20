using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using System.Runtime.InteropServices;
using ProcessHandler;
using static Windows.Win32.PInvoke;

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
        /// <param name="process">The target process into which the DLL will be injected.</param>
        /// <param name="dllPath">The path to the DLL file to be injected.</param>
        /// <returns>Returns true if the DLL injection is successful, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the process is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified DLL file is not found.</exception>
        public static bool InjectDLL(Process process, string dllPath)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));
            if (!File.Exists(dllPath))
                throw new FileNotFoundException(nameof(dllPath));

            byte[] dllData = File.ReadAllBytes(dllPath);

            void* allocatedMemory = VirtualAllocEx(
                process.ProcessHandle,
                null,
                (nuint)dllPath.Length,
                VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
                PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE);

            fixed (void* ptr = dllData)
                WriteProcessMemory(process.ProcessHandle, allocatedMemory, ptr, (nuint)dllData.Length, null);

            var remoteThread = CreateRemoteThread(process.ProcessHandle, null, 0, LoadLibraryA, allocatedMemory, 0, null);

            var waitEvent = WaitForSingleObject(remoteThread, 10000);

            if (waitEvent == WAIT_EVENT.WAIT_ABANDONED || waitEvent == WAIT_EVENT.WAIT_TIMEOUT)
            {
                if (remoteThread != null)
                    CloseHandle((HANDLE)remoteThread.DangerousGetHandle());

                return false;
            }

            VirtualFreeEx(process.ProcessHandle, allocatedMemory, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);

            if (remoteThread != null)
                CloseHandle((HANDLE)remoteThread.DangerousGetHandle());

            return true;
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
