using System.Runtime.CompilerServices;

namespace AobScan
{
    /// <summary>
    /// Provides methods for matching byte patterns in memory.
    /// </summary>
    internal class Matching
    {
        /// <summary>
        /// Checks if a given pattern matches a segment of memory with a specified mask.
        /// </summary>
        /// <param name="memory">The memory segment to check.</param>
        /// <param name="offset">The offset within the memory segment to start checking.</param>
        /// <param name="pattern">The byte pattern to match.</param>
        /// <param name="mask">The mask to apply to the memory and pattern for matching.</param>
        /// <returns>True if the pattern matches, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool CheckMatch(Span<byte> memory, int offset, Span<byte> pattern, ReadOnlySpan<byte> mask)
        {
            if (offset + pattern.Length > memory.Length) return false;

            fixed (byte* memoryPtr = memory)
            fixed (byte* patternPtr = pattern)
            fixed (byte* maskPtr = mask)
            {
                byte* memOffset = memoryPtr + offset;
                for (int i = 0; i < pattern.Length; i++)
                {
                    if ((memOffset[i] & maskPtr[i]) != (patternPtr[i] & maskPtr[i]))
                        return false;
                }
            }

            return true;
        }
    }
}
