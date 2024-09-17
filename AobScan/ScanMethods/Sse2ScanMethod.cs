using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace AobScan.ScanMethods
{
    /// <summary>
    /// Provides scanning functionality using SSE2 SIMD instructions for pattern matching.
    /// </summary>
    internal unsafe class Sse2ScanMethod : IScanMethod
    {
        /// <summary>
        /// Scans a memory buffer for a pattern using SSE2 instructions.
        /// </summary>
        /// <param name="memory">The memory buffer to scan.</param>
        /// <param name="pattern">The byte pattern to match.</param>
        /// <param name="mask">The mask to apply during the scan.</param>
        /// <param name="matches">A list to store the offsets where the pattern was found.</param>
        public void ScanRegion(byte[] memory, byte[] pattern, byte[] mask, List<int> matches)
        {
            int memoryLength = memory.Length;
            int patternLength = pattern.Length;

            fixed (byte* pMemory = memory)
            fixed (byte* pPattern = pattern)
            fixed (byte* pMask = mask)
            {
                Vector128<byte> vPattern = Vector128.Create(pPattern[0]);
                Vector128<byte> vMask = Vector128.Create(pMask[0]);

                for (int i = 0; i <= memoryLength - 16; i += 16)
                {
                    Vector128<byte> vMemory = Sse2.LoadVector128(pMemory + i);
                    Vector128<byte> vResult = Sse2.And(vMemory, vMask);
                    Vector128<byte> vCmp = Sse2.CompareEqual(vResult, vPattern);

                    ushort matchMask = (ushort)Sse2.MoveMask(vCmp);

                    while (matchMask != 0)
                    {
                        int index = i + BitOperations.TrailingZeroCount(matchMask);
                        if (Matching.CheckMatch(memory, index, pattern, mask)) matches.Add(index);
                        matchMask = (ushort)(matchMask & (matchMask - 1));
                    }
                }

                for (int i = memoryLength - 16; i <= memoryLength - patternLength; i++)
                    if (Matching.CheckMatch(memory, i, pattern, mask)) matches.Add(i);
            }
        }
    }
}
