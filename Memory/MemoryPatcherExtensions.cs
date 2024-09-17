using Windows.Win32.Foundation;
using ProcessHandler;
using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace Memory
{
    /// <summary>
    /// Provides extension methods for writing memory to a process.
    /// </summary>
    internal unsafe static class MemoryPatcherExtensions
    {
        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> to the specified memory address in the given process.
        /// </summary>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        /// <param name="process">The process to which to write the memory.</param>
        /// <param name="address">The address to which to write the value.</param>
        /// <param name="data">The value to write to the memory address.</param>
        public static void WriteMemory<T>(this Process process, nint address, T data)
        {
            byte[] buffer = GetBytes(data, out uint size);
            Patch((HANDLE)process.ProcessHandle.DangerousGetHandle(), address, buffer, size);
        }

        /// <summary>
        /// Converts the specified data into a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the data to convert.</typeparam>
        /// <param name="data">The data to convert.</param>
        /// <param name="size">The size of the resulting byte array.</param>
        /// <returns>A byte array representing the data.</returns>
        /// <exception cref="ArgumentException">Thrown if the data type is not supported.</exception>
        private static byte[] GetBytes<T>(T data, out uint size)
        {
            byte[] buffer;

            switch (data)
            {
                case int integer:
                    buffer = BitConverter.GetBytes(integer);
                    size = sizeof(int);
                    break;
                case long longer:
                    buffer = BitConverter.GetBytes(longer);
                    size = sizeof(long);
                    break;
                case double doubler:
                    buffer = BitConverter.GetBytes(doubler);
                    size = sizeof(double);
                    break;
                case byte[] bytes:
                    buffer = bytes;
                    size = (uint)buffer.Length;
                    break;
                default:
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
        private static BOOL Patch(HANDLE handle, nint address, byte[] newBytes, uint size)
        {
            fixed (byte* ptr = newBytes)
            {
                return WriteProcessMemory(handle, address.ToPointer(), ptr, size);
            }
        }
    }
}