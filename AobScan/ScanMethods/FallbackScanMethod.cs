namespace AobScan.ScanMethods
{
    /// <summary>
    /// Provides a fallback scanning method for pattern matching when no advanced SIMD instructions are available.
    /// </summary>
    public class FallbackScanMethod : IScanMethod
    {
        /// <summary>
        /// Scans a memory buffer for a pattern using a basic scanning method.
        /// </summary>
        /// <param name="memory">The memory buffer to scan.</param>
        /// <param name="pattern">The byte pattern to match.</param>
        /// <param name="mask">The mask to apply during the scan.</param>
        /// <param name="matches">A list to store the offsets where the pattern was found.</param>
        public void ScanRegion(byte[] memory, byte[] pattern, byte[] mask, List<int> matches)
        {
            int memoryLength = memory.Length;
            int patternLength = pattern.Length;

            for (int i = 0; i <= memoryLength - patternLength; i++)
                if (Matching.CheckMatch(memory, i, pattern, mask))
                    matches.Add(i);
        }
    }
}
