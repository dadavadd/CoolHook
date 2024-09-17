namespace AobScan
{
    /// <summary>
    /// Represents a memory region within a process.
    /// </summary>
    internal class MemoryRegion
    {
        /// <summary>
        /// Gets the base address of the memory region.
        /// </summary>
        public IntPtr BaseAddress { get; }

        /// <summary>
        /// Gets the size of the memory region in bytes.
        /// </summary>
        public int RegionSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryRegion"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address of the memory region.</param>
        /// <param name="regionSize">The size of the memory region in bytes.</param>
        public MemoryRegion(IntPtr baseAddress, int regionSize)
        {
            BaseAddress = baseAddress;
            RegionSize = regionSize;
        }
    }
}
