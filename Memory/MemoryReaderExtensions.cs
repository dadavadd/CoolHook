using System.Runtime.InteropServices;
using System.Text;
using AobScan;
using CoolHook.Logger;
using ProcessHandler;
using Windows.Win32.Foundation;

using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 // Checks for platform compatibility

namespace CoolHook.Memory
{
    /// <summary>
    /// Provides extension methods for reading memory from a process.
    /// </summary>
    public static unsafe class MemoryReaderExtensions
    {
        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the specified memory address in the given process.
        /// </summary>
        /// <typeparam name="T">The type of the value to read. Must be a value type.</typeparam>
        /// <param name="process">The process interface from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the value.</param>
        /// <returns>The value read from the specified address.</returns>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not a value type.</exception>
        public static T ReadMemory<T>(this IMemoryProcessHandle process, IntPtr readAddress, ILogger logger = null) where T : struct
        {
            logger?.Log($"Attempting to read memory at address: {readAddress.ToString("X")}, Type: {typeof(T).Name}");

            try
            {
                var result = ((HANDLE)process.ProcessHandle.DangerousGetHandle()).ReadMemory<T>(readAddress, logger);
                logger?.Log($"Successfully read memory at address: {readAddress.ToString("X")}");
                return result;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error reading memory at address: {readAddress.ToString("X")}, Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the specified memory address in the given process.
        /// </summary>
        /// <typeparam name="T">The type of the value to read. Must be a value type.</typeparam>
        /// <param name="handle">The process handle from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the value.</param>
        /// <returns>The value read from the specified address.</returns>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not a value type.</exception>
        private static T ReadMemory<T>(this HANDLE handle, IntPtr readAddress, ILogger logger = null) where T : struct
        {
            if (!typeof(T).IsValueType)
            {
                logger?.LogError("Attempted to read non-value type.");
                throw new ArgumentException("Generic type must be a value type");
            }

            int size = sizeof(T);
            byte[] buffer = new byte[size];

            logger?.Log($"Reading {size} bytes from memory at address: {readAddress.ToString("X")}");

            fixed (byte* ptr = buffer)
            {
                if (!ReadProcessMemory(handle, readAddress.ToPointer(), ptr, (UIntPtr)size))
                {
                    logger?.LogError($"Failed to read memory at address: {readAddress.ToString("X")}");
                    throw new InvalidOperationException("Failed to read memory");
                }
            }

            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var bufferPtr = gcHandle.AddrOfPinnedObject();
                return (T)Marshal.PtrToStructure(bufferPtr, typeof(T));
            }
            finally
            {
                gcHandle.Free();
            }
        }

        /// <summary>
        /// Reads a null-terminated string from the specified memory address in the given process.
        /// </summary>
        /// <param name="process">The process interface class from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the string.</param>
        /// <param name="length">The length of the string to read.</param>
        /// <returns>The string read from the specified address.</returns>
        public static string ReadString(this IMemoryProcessHandle process, IntPtr readAddress, int length, ILogger logger = null)
        {
            logger?.Log($"Attempting to read string from memory at address: {readAddress.ToString("X")}, Length: {length}");

            try
            {
                var result = ((HANDLE)process.ProcessHandle.DangerousGetHandle()).ReadString(readAddress, length, logger);
                logger?.Log($"Successfully read string from memory at address: {readAddress.ToString("X")}");
                return result;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error reading string from memory at address: {readAddress.ToString("X")}, Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reads a null-terminated string from the specified memory address in the given process.
        /// </summary>
        /// <param name="handle">The process handle from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the string.</param>
        /// <param name="length">The length of the string to read.</param>
        /// <returns>The string read from the specified address.</returns>v
        private static string ReadString(this HANDLE handle, IntPtr readAddress, int length, ILogger logger = null)
        {
            byte[] buffer = new byte[length];

            logger?.Log($"Reading {length} bytes from memory at address: {readAddress.ToString("X")}");

            fixed (byte* ptr = buffer)
            {
                if (!ReadProcessMemory(handle, readAddress.ToPointer(), ptr, (UIntPtr)length))
                {
                    logger?.LogError($"Failed to read string at address: {readAddress.ToString("X")}");
                    throw new InvalidOperationException("Failed to read memory");
                }
            }

            string result = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            logger?.Log($"Read string: {result}");
            return result;
        }
    }
}
