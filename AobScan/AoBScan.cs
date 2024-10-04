using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Windows.Win32.System.Memory;
using AobScan.ScanMethods;
using ProcessHandler;

using static Windows.Win32.PInvoke;
using CoolHook.Memory;
using CoolHook.Logger;

#pragma warning disable CA1416 // Checks for platform compatibility
namespace AobScan
{
    /// <summary>
    /// Performs an array of bytes (AoB) scan within a process's memory.
    /// </summary>
    public unsafe class AoBScan : IMemoryProcessHandle
    {
        private readonly IScanMethod _scanMethod;
        private readonly ILogger _logger;

        public SafeHandle ProcessHandle { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process and scanning method.
        /// </summary>
        /// <param name="process">The process to scan.</param>
        /// <param name="scanMethod">The method to use for scanning.</param>
        public AoBScan(Process process, IScanMethod scanMethod, ILogger logger = null)
        {
            ProcessHandle = process.ProcessHandle;
            _scanMethod = scanMethod;
            _logger = logger;
            _logger?.Log($"AoBScan initialized for process: {process.CurrentProcess.ProcessName}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process, using an automatic scan method selection.
        /// </summary>
        /// <param name="process">The process to scan.</param>
        public AoBScan(Process process, ILogger logger = null)
            : this(process, GetScanMethod(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process and scanning method.
        /// </summary>
        /// <param name="procName">The process name to scan.</param>
        /// <param name="scanMethod">The method to use for scanning.</param>
        public AoBScan(string procName, IScanMethod scanMethod, ILogger logger = null)
            : this(new Process(procName), scanMethod, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process, using an automatic scan method selection.
        /// </summary>
        /// <param name="procName">The process name to scan.</param>
        public AoBScan(string procName, ILogger logger = null)
            : this(new Process(procName), GetScanMethod(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process and scanning method.
        /// </summary>
        /// <param name="processID">The process ID to scan.</param>
        /// <param name="scanMethod">The method to use for scanning.</param>
        public AoBScan(int processID, IScanMethod scanMethod, ILogger logger = null)
            : this(new Process(processID), scanMethod, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AoBScan"/> class with a specified process, using an automatic scan method selection.
        /// </summary>
        /// <param name="processID">The process ID to scan.</param>
        public AoBScan(int processID, ILogger logger = null)
            : this(new Process(processID), GetScanMethod(), logger)
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
            _logger?.Log($"Starting AoBScan with pattern: {pattern}, Range: {startAddr:X}-{endAddr:X}");

            var (aobPattern, mask) = PreparePatternAndMask(pattern);
            var memoryRegions = GetMemoryRegions(startAddr, endAddr, readable, writable, executable);
            return ScanAllMemoryRegions(memoryRegions, aobPattern, mask);
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

        public Task<List<IntPtr>> AobScan(string pattern, bool readable, bool writable = true, long startAddr = 0x0000000000010000, long endAddr = 0x00007ffffffeffff)
        {
            return AobScan(pattern, readable, writable, true, startAddr, endAddr);
        }

        private unsafe Task<List<IntPtr>> ScanAllMemoryRegions(List<MemoryRegion> memoryRegions, byte[] aobPattern, byte[] mask)
        {
            _logger?.Log("Scanning all memory regions...");

            ConcurrentBag<IntPtr> results = new ConcurrentBag<IntPtr>();

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

                _logger?.Log($"Memory scan completed with {results.Count} matches.");
                return results.ToList();
            });
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

            _logger?.Log("Pattern and mask prepared.");
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
            _logger?.Log($"Retrieving memory regions in range: {startAddr:X}-{endAddr:X}, Readable: {readable}, Writable: {writable}, Executable: {executable}");
            var regions = new List<MemoryRegion>();
            var address = new IntPtr(startAddr);
            while (address.ToInt64() < endAddr)
            {
                MEMORY_BASIC_INFORMATION memInfo = default;
                if (VirtualQueryEx(ProcessHandle, address.ToPointer(), out memInfo, (uint)sizeof(MEMORY_BASIC_INFORMATION)) == 0)
                {
                    _logger?.LogError($"VirtualQueryEx failed at address: {address.ToInt64():X}");
                    break;
                }
                _logger?.Log($"Region found: BaseAddress = {new IntPtr(memInfo.BaseAddress):X}, RegionSize = {memInfo.RegionSize}, State = {memInfo.State}, Protect = {memInfo.Protect}");

                bool isReadable = (memInfo.Protect & (PAGE_PROTECTION_FLAGS.PAGE_READONLY |
                                                      PAGE_PROTECTION_FLAGS.PAGE_READWRITE |
                                                      PAGE_PROTECTION_FLAGS.PAGE_WRITECOPY |
                                                      PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READ |
                                                      PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE |
                                                      PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_WRITECOPY)) != 0;

                bool isWritable = (memInfo.Protect & (PAGE_PROTECTION_FLAGS.PAGE_READWRITE |
                                                      PAGE_PROTECTION_FLAGS.PAGE_WRITECOPY |
                                                      PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE |
                                                      PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_WRITECOPY)) != 0;

                bool isExecutable = (memInfo.Protect & (PAGE_PROTECTION_FLAGS.PAGE_EXECUTE |
                                                        PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READ |
                                                        PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE |
                                                        PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_WRITECOPY)) != 0;

                if (memInfo.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT &&
                    (!readable || isReadable) &&
                    (!writable || isWritable) &&
                    (!executable || isExecutable))
                {
                    regions.Add(new MemoryRegion(new IntPtr(memInfo.BaseAddress), (int)memInfo.RegionSize));
                }
                address = (IntPtr)memInfo.BaseAddress + (IntPtr)memInfo.RegionSize;
            }
            _logger?.Log($"Memory regions retrieval completed. {regions.Count} regions found.");
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
            _logger?.Log($"{_scanMethod.GetType().FullName} scanning region.");

            var matches = new List<int>();
            _scanMethod.ScanRegion(memory, pattern, mask, matches);
            _logger?.Log($"ScanRegion completed. Matches found: {matches.Count}");
            return matches;
        }
    }
}
