using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace AobScan.ScanMethods
{
    /// <summary>
    /// Provides scanning functionality using AVX2 SIMD instructions for pattern matching.
    /// </summary>
    public unsafe class Avx2ScanMethod : IScanMethod
    {
        /// <summary>
        /// Scans a memory buffer for a pattern using AVX2 instructions.
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
                Vector256<byte> vPattern = Vector256.Create(pPattern[0]);
                Vector256<byte> vMask = Vector256.Create(pMask[0]);

                for (int i = 0; i <= memoryLength - 32; i += 32)
                {
                    Vector256<byte> vMemory = Avx.LoadVector256(pMemory + i);
                    Vector256<byte> vResult = Avx2.And(vMemory, vMask);
                    Vector256<byte> vCmp = Avx2.CompareEqual(vResult, vPattern);

                    uint matchMask = (uint)Avx2.MoveMask(vCmp);

                    while (matchMask != 0)
                    {
                        int index = i + BitOperations.TrailingZeroCount(matchMask);
                        if (Matching.CheckMatch(memory, index, pattern, mask))
                            matches.Add(index);
                        matchMask = matchMask & (matchMask - 1);
                    }
                }

                for (int i = memoryLength - 32; i <= memoryLength - patternLength; i++)
                {
                    if (Matching.CheckMatch(memory, i, pattern, mask))
                        matches.Add(i);
                }
            }
        }
    }
}
