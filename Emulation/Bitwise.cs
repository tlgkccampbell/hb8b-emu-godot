using System;
using System.Runtime.CompilerServices;

namespace Hb8b.Emulation
{
    /// <summary>
    /// Contains helper methods for performing bitwise operations.
    /// </summary>
    public static class Bitwise
    {
        /// <summary>
        /// Gets a value indicating whether a particular bit is set within a byte.
        /// </summary>
        /// <param name="value">The byte to evaluate.</param>
        /// <param name="bit">The index of the bit to evaluate.</param>
        /// <returns><see langword="true"/> if the specified bit is set; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean IsSet(ref Byte value, Int32 bit)
        {
            return (value & (1 << bit)) != 0;
        }

        /// <summary>
        /// Gets a value indicating whether a particular bit is cleared within a byte.
        /// </summary>
        /// <param name="value">The byte to evaluate.</param>
        /// <param name="bit">The index of the bit to evaluate.</param>
        /// <returns><see langword="true"/> if the specified bit is cleared; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean IsClr(ref Byte value, Int32 bit)
        {
            return (value & (1 << bit)) == 0;
        }

        /// <summary>
        /// Toggles the value of a particular bit within a byte.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="bit">The index of the bit to toggle.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Toggle(ref Byte value, Int32 bit)
        {
            value ^= (Byte)(1 << bit);
        }

        /// <summary>
        /// Clears the value of a particular bit within a byte.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="bit">The index of the bit to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clr(ref Byte value, Int32 bit)
        {
            value &= (Byte)~(1 << bit);
        }
        
        /// <summary>
        /// Clears all of the bits in <paramref name="value"/> specified by <paramref name="mask"/>.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="mask">A mask specifying which bits to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClrMask(ref Byte value, Byte mask)
        {
            value &= (Byte)~mask;
        }

        /// <summary>
        /// Sets the value of a particular bit within a byte.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="bit">The index of the bit to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(ref Byte value, Int32 bit)
        {
            value |= (Byte)(1 << bit);
        }

        /// <summary>
        /// Sets all of the bits in <paramref name="value"/> specified by <paramref name="mask"/>.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="mask">A mask specifying which bits to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMask(ref Byte value, Byte mask)
        {
            value |= mask;
        }

        /// <summary>
        /// Sets or clears a particular bit within a byte.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="bit">The index of the bit to set or clear.</param>
        /// <param name="state"><see langword="true"/> to set the bit; <see langword="false"/> to clear it.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOrClr(ref Byte value, Int32 bit, Boolean state)
        {
            if (state)
                Set(ref value, bit);
            else
                Clr(ref value, bit);
        }

        /// <summary>
        /// Sets or clears all of the bits in <paramref name="value"/> specified by <paramref name="mask"/>.
        /// </summary>
        /// <param name="value">The byte to modify.</param>
        /// <param name="mask">A mask specifying which bits to set or clear.</param>
        /// <param name="state"><see langword="true"/> to set the bits; <see langword="false"/> to clear them.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOrClrMask(ref Byte value, Byte mask, Boolean state)
        {
            if (state)
                SetMask(ref value, mask);
            else
                ClrMask(ref value, mask);
        }
    }
}