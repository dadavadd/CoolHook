using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;
using CoolHook.Logger;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace CoolHook.Memory
{
    /// <summary>
    /// Provides extension methods for writing memory to a process.
    /// </summary>
    public unsafe static class MemoryPatcherExtensions
    {
        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> to the specified memory address in the given process.
        /// </summary>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        /// <param name="process">The process interface to which to write the memory.</param>
        /// <param name="address">The address to which to write the value.</param>
        /// <param name="data">The value to write to the memory address.</param>
        public static void WriteMemory<T>(this IMemoryProcessHandle process, nint address, T data, ILogger logger = null)
        {
            logger?.Log($"Attempting to write memory at address: {address.ToString("X")}, Type: {typeof(T).Name}");

            try
            {
                byte[] buffer = GetBytes(data, out uint size, logger);
                var result = Patch((HANDLE)process.ProcessHandle.DangerousGetHandle(), address, buffer, size, logger);

                if (result)
                {
                    logger?.Log($"Successfully wrote {size} bytes to address: {address.ToString("X")}");
                }
                else
                {
                    logger?.LogError($"Failed to write memory at address: {address.ToString("X")}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error writing memory at address: {address.ToString("X")}, Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts the specified data into a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the data to convert.</typeparam>
        /// <param name="data">The data to convert.</param>
        /// <param name="size">The size of the resulting byte array.</param>
        /// <returns>A byte array representing the data.</returns>
        /// <exception cref="ArgumentException">Thrown if the data type is not supported.</exception>
        private static byte[] GetBytes<T>(T data, out uint size, ILogger logger = null)
        {
            byte[] buffer;

            switch (data)
            {
                case int integer:
                    buffer = BitConverter.GetBytes(integer);
                    size = sizeof(int);
                    logger?.Log($"Converting int to bytes, Size: {size}");
                    break;
                case long longer:
                    buffer = BitConverter.GetBytes(longer);
                    size = sizeof(long);
                    logger?.Log($"Converting long to bytes, Size: {size}");
                    break;
                case double doubler:
                    buffer = BitConverter.GetBytes(doubler);
                    size = sizeof(double);
                    logger?.Log($"Converting double to bytes, Size: {size}");
                    break;
                case byte[] bytes:
                    buffer = bytes;
                    size = (uint)buffer.Length;
                    logger?.Log($"Converting byte array to bytes, Size: {size}");
                    break;
                default:
                    logger?.LogError("Unsupported data type encountered.");
                    throw new ArgumentException("Unsupported data type.");
            }

            return buffer;
        }

        /// <summary>
        /// Writes a byte array to the specified memory address in the given process.
        /// </summary>
        /// <param name="handle">The handle to the process.</param>
        /// <param name="address">The address to which to write the data.</param>
        /// <param name="newBytes">The byte array to write.</param>
        /// <param name="size">The size of the byte array.</param>
        /// <returns>A <see cref="BOOL"/> indicating success or failure.</returns>
        private static BOOL Patch(HANDLE handle, nint address, byte[] newBytes, uint size, ILogger logger = null)
        {
            logger?.Log($"Patching memory at address: {address.ToString("X")}, Size: {size} bytes");

            fixed (byte* ptr = newBytes)
            {
                BOOL result = WriteProcessMemory(handle, address.ToPointer(), ptr, size);
                if (!result)
                {
                    logger?.LogError($"Failed to patch memory at address: {address.ToString("X")}");
                }
                return result;
            }
        }
    }
}