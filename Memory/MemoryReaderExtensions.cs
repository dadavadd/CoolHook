﻿using System.Runtime.InteropServices;
using System.Text;
using AobScan;
using ProcessHandler;
using Windows.Win32.Foundation;

using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 // Checks for platform compatibility

namespace Memory
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
        /// <param name="process">The process from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the value.</param>
        /// <returns>The value read from the specified address.</returns>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not a value type.</exception>
        public static T ReadMemory<T>(this Process process, IntPtr readAddress) where T : struct
        {
            return ((HANDLE)process.ProcessHandle.DangerousGetHandle()).ReadMemory<T>(readAddress);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the specified memory address in the given process.
        /// </summary>
        /// <typeparam name="T">The type of the value to read. Must be a value type.</typeparam>
        /// <param name="aobscan">The aobscan from which to get processHandle, and after read the memory.</param>
        /// <param name="readAddress">The address from which to read the value.</param>
        /// <returns>The value read from the specified address.</returns>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not a value type.</exception>
        public static T ReadMemory<T>(this AoBScan aobscan, IntPtr readAddress) where T : struct
        {
            return ((HANDLE)aobscan.ProcessHandle.DangerousGetHandle()).ReadMemory<T>(readAddress);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the specified memory address in the given process.
        /// </summary>
        /// <typeparam name="T">The type of the value to read. Must be a value type.</typeparam>
        /// <param name="handle">The process handle from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the value.</param>
        /// <returns>The value read from the specified address.</returns>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not a value type.</exception>
        private static T ReadMemory<T>(this HANDLE handle, IntPtr readAddress) where T : struct
        {
            if (!typeof(T).IsValueType)
                throw new ArgumentException("Generic type must be a value type");

            int size = sizeof(T);
            byte[] buffer = new byte[size];

            fixed (byte* ptr = buffer)
                ReadProcessMemory(handle, readAddress.ToPointer(), ptr, (UIntPtr)size);

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
        /// <param name="process">The process class from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the string.</param>
        /// <param name="length">The length of the string to read.</param>
        /// <returns>The string read from the specified address.</returns>
        public static string ReadString(this Process process, IntPtr readAddress, int length)
        {
            return ((HANDLE)process.ProcessHandle.DangerousGetHandle()).ReadString(readAddress, length);
        }

        /// <summary>
        /// Reads a null-terminated string from the specified memory address in the given process.
        /// </summary>
        /// <param name="aobscan">The aobscan class from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the string.</param>
        /// <param name="length">The length of the string to read.</param>
        /// <returns>The string read from the specified address.</returns>
        public static string ReadString(this AoBScan aobscan, IntPtr readAddress, int length)
        {
            return ((HANDLE)aobscan.ProcessHandle.DangerousGetHandle()).ReadString(readAddress, length);
        }


        /// <summary>
        /// Reads a null-terminated string from the specified memory address in the given process.
        /// </summary>
        /// <param name="handle">The process handle from which to read the memory.</param>
        /// <param name="readAddress">The address from which to read the string.</param>
        /// <param name="length">The length of the string to read.</param>
        /// <returns>The string read from the specified address.</returns>v
        private static string ReadString(this HANDLE handle, IntPtr readAddress, int length)
        {
            byte[] buffer = new byte[length];
            fixed (byte* ptr = buffer)
                ReadProcessMemory(handle, readAddress.ToPointer(), ptr, (UIntPtr)length);
            return Encoding.UTF8.GetString(buffer).TrimEnd('\0'); // Assumes null-terminated string
        }
    }
}
