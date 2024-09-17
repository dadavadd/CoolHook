namespace AobScan.ScanMethods
{
    /// <summary>
    /// Defines the interface for scanning methods used to find byte patterns in memory.
    /// </summary>
    internal interface IScanMethod
    {
        /// <summary>
        /// Scans a memory buffer for a pattern.
        /// </summary>
        /// <param name="memory">The memory buffer to scan.</param>
        /// <param name="pattern">The byte pattern to match.</param>
        /// <param name="mask">The mask to apply during the scan.</param>
        /// <param name="matches">A list to store the offsets where the pattern was found.</param>
        void ScanRegion(byte[] memory, byte[] pattern, byte[] mask, List<int> matches);
    }
}
