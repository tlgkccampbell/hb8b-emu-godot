using System;

namespace Hb8b.Emulation
{
    /// <summary>
    /// Contains methods for allocating blocks of memory.
    /// </summary>
    public static class MemoryAllocator
    {
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Retrieves a randomized byte value.
        /// </summary>
        /// <returns>The random value that was generated.</returns>
        public static Byte GetRandomByte() => (Byte)_rng.Next(0, 256);

        /// <summary>
        /// Fills the specified buffer with zeros.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        public static void FillWithZeros(Byte[] buffer)
        {
            FillWithByte(buffer, 0x00);
        }

        /// <summary>
        /// Fills the specified buffer with a given value.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        /// <param name="value">The value with which to fill the buffer.</param>
        public static void FillWithByte(Byte[] buffer, Byte value)
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = value;
        }

        /// <summary>
        /// Fills the specified buffer with random bytes.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        public static void FillWithRandomBytes(Byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = (Byte)_rng.Next(0, 256);
        }
    }
}