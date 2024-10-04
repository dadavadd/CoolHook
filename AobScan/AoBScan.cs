﻿using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Windows.Win32.System.Memory;
using AobScan.ScanMethods;
using ProcessHandler;

using static Windows.Win32.PInvoke;
using CoolHook.Memory;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace AobScan
{
    /// <summary>
    /// Performs an array of bytes (AoB) scan within a process's memory.
    /// </summary>
    public unsafe class AoBScan : IMemoryProcessHandle
    {
        private IScanMethod _scanMethod;

        public SafeHandle ProcessHandle { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process and scanning method.
        /// </summary>
        /// <param name="process">The process to scan.</param>
        /// <param name="scanMethod">The method to use for scanning.</param>
        public AoBScan(Process process, IScanMethod scanMethod)
        {
            ProcessHandle = process.ProcessHandle;
            _scanMethod = scanMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process, using an automatic scan method selection.
        /// </summary>
        /// <param name="process">The process to scan.</param>
        public AoBScan(Process process)
            : this(process, GetScanMethod())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process and scanning method.
        /// </summary>
        /// <param name="procName">The process name to scan.</param>
        /// <param name="scanMethod">The method to use for scanning.</param>
        public AoBScan(string procName, IScanMethod scanMethod)
            : this(new Process(procName), scanMethod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process, using an automatic scan method selection.
        /// </summary>
        /// <param name="procName">The process name to scan.</param>
        public AoBScan(string procName)
            : this(new Process(procName), GetScanMethod())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process and scanning method.
        /// </summary>
        /// <param name="processID">The process ID to scan.</param>
        /// <param name="scanMethod">The method to use for scanning.</param>
        public AoBScan(int processID, IScanMethod scanMethod)
            : this(new Process(processID), scanMethod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process, using an automatic scan method selection.
        /// </summary>
        /// <param name="processID">The process ID to scan.</param>
        public AoBScan(int processID)
            : this(new Process(processID), GetScanMethod())
        {
        }

        /// <summary>
        /// Determines the appropriate scan method based on the processor's capabilities.
        /// </summary>
        /// <returns>An instance of <see cref="IScanMethod"/>.</returns>
        private static IScanMethod GetScanMethod()
        {
            if (Avx2.IsSupported)
                return new Avx2ScanMethod();
            else if (Sse2.IsSupported)
                return new Sse2ScanMethod();
            else
                return new FallbackScanMethod();
        }

        /// <summary>
        /// Scans the process memory for occurrences of a specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern to search for.</param>
        /// <param name="startAddr">The starting address of the memory range to scan.</param>
        /// <param name="endAddr">The ending address of the memory range to scan.</param>
        /// <param name="executable">The parameter to scan for executable memory.</param>
        /// <param name="readable">The parameter to scan for readable memory. </param>
        /// <param name="writable">The parameter to scan for writeble memory. </param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of addresses where the pattern was found.</returns>
        public Task<List<IntPtr>> AobScan(string pattern, bool readable, bool writable, bool executable, long startAddr, long endAddr)
        {
            var (aobPattern, mask) = PreparePatternAndMask(pattern);
            var memoryRegions = GetMemoryRegions(startAddr, endAddr, readable, writable, executable);
            var results = new ConcurrentBag<IntPtr>();

            return Task.Run(() =>
            {
                Parallel.ForEach(memoryRegions, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, region =>
                {
                    var buffer = new byte[region.RegionSize];
                    nuint bytesRead = 0;
                    fixed (void* ptr = buffer)
                    {
                        if (ReadProcessMemory(ProcessHandle, region.BaseAddress.ToPointer(), ptr, (uint)region.RegionSize, &bytesRead))
                        {
                            var matches = ScanRegion(buffer, aobPattern, mask);

                            foreach (var offset in matches)
                                results.Add(region.BaseAddress + offset);
                        }
                    }
                });

                return results.ToList();
            });
        }

        /// <summary>
        /// The original AobScan method overloading without executable, writable and readable parameters.
        /// </summary>
        /// <param name="pattern">The pattern to search for.</param>
        /// <param name="startAddr">The starting address of the memory range to scan.</param>
        /// <param name="endAddr">The ending address of the memory range to scan.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of addresses where the pattern was found.</returns>
        public Task<List<IntPtr>> AobScan(string pattern, long startAddr = 0x0000000000010000, long endAddr = 0x00007ffffffeffff)
        {
            return AobScan(pattern, true, true, true, startAddr, endAddr);
        }

        /// <summary>
        /// Prepares the pattern and mask from a string representation.
        /// </summary>
        /// <param name="patternString">The pattern string to parse.</param>
        /// <returns>A tuple containing the byte array for the pattern and mask.</returns>
        private (byte[] pattern, byte[] mask) PreparePatternAndMask(string patternString)
        {
            var parts = patternString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var pattern = new byte[parts.Length];
            var mask = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "??" || parts[i] == "?")
                {
                    mask[i] = 0x00;
                    pattern[i] = 0x00;
                }
                else
                {
                    mask[i] = 0xFF;
                    pattern[i] = Convert.ToByte(parts[i], 16);
                }
            }
            return (pattern, mask);
        }

        /// <summary>
        /// Retrieves memory regions within the specified address range from a process.
        /// </summary>
        /// <param name="startAddr">The starting address of the range.</param>
        /// <param name="endAddr">The ending address of the range.</param>
        /// <param name="executable">The parameter to scan for executable memory.</param>
        /// <param name="readable">The parameter to scan for readable memory. </param>
        /// <param name="writable">The parameter to scan for writeble memory. </param>
        /// <returns>A list of <see cref="MemoryRegion"/> objects representing the memory regions.</returns>
        private List<MemoryRegion> GetMemoryRegions(long startAddr, long endAddr, bool readable, bool writable, bool executable)
        {
            var regions = new List<MemoryRegion>();
            var address = new IntPtr(startAddr);

            while (address.ToInt64() < endAddr)
            {
                MEMORY_BASIC_INFORMATION memInfo = default;

                if (VirtualQueryEx(ProcessHandle, address.ToPointer(), out memInfo, (uint)sizeof(MEMORY_BASIC_INFORMATION)) == 0) break;

                bool isReadable = (memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_READONLY) > 0;

                bool isWritable = ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_READWRITE) > 0) ||
                                  ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_WRITECOPY) > 0) ||
                                  ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE) > 0) ||
                                  ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_WRITECOPY) > 0);

                bool isExecutable = ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_EXECUTE) > 0) ||
                                    ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READ) > 0) ||
                                    ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE) > 0) ||
                                    ((memInfo.Protect & PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_WRITECOPY) > 0);

                if (memInfo.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT &&
                    (!readable || isReadable) &&
                    (!writable || isWritable) &&
                    (!executable || isExecutable))
                {
                    regions.Add(new MemoryRegion(new IntPtr(memInfo.BaseAddress), (int)memInfo.RegionSize));
                }

                address = (IntPtr)memInfo.BaseAddress + (IntPtr)memInfo.RegionSize;
            }

            return regions;
        }

        /// <summary>
        /// Scans a memory region for occurrences of a specified pattern.
        /// </summary>
        /// <param name="memory">The memory buffer to scan.</param>
        /// <param name="pattern">The byte pattern to match.</param>
        /// <param name="mask">The mask to apply during the scan.</param>
        /// <returns>A list of offsets where the pattern was found within the memory buffer.</returns>
        private List<int> ScanRegion(byte[] memory, byte[] pattern, byte[] mask)
        {
            var matches = new List<int>();
            _scanMethod.ScanRegion(memory, pattern, mask, matches);
            return matches;
        }
    }
}
