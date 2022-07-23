using System;
#nullable enable

namespace Hb8b.Emulation
{
    /// <summary>
    /// A block of the bus' 16-bit address space.
    /// </summary>
    public class MemoryBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryBlock"/> class.
        /// </summary>
        /// <param name="start">The block's starting position within the 16-bit address space.</param>
        /// <param name="size">The block's size in bytes.</param>
        /// <param name="isReadOnly">A value indicating whether this block is marked as read-only.</param>
        /// <param name="isPresent">A value indicating whether this block is present in the device by default.</param>
        public MemoryBlock(UInt16 start, UInt16 size, Boolean isReadOnly = false, Boolean isPresent = true)
        {
            this.Data = new Byte[size];
            this.Start = start;
            this.End = (UInt16)(start + size);
            this.IsReadOnly = isReadOnly;
            this.IsPresent = isPresent;

            if (IsReadOnly)
                MemoryAllocator.FillWithByte(Data, 0xFF);
            else
                MemoryAllocator.FillWithRandomBytes(Data);
        }

        /// <summary>
        /// Gets a value indicating whether the block contains the specified address.
        /// </summary>
        /// <param name="address">The address to evaluate.</param>
        /// <returns><see langword="true"/> if the block contains the address; otherwise, <see langword="false"/>.</returns>
        public Boolean Contains(UInt16 address)
        {
            return IsPresent && address >= Start && address < End;
        }

        /// <summary>
        /// Reads a single byte at the specified address.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <returns>The byte that was read from the specified address.</returns>
        public Byte Read(UInt16 address)
        {
            return Data[address - Start];
        }

        /// <summary>
        /// Reads a sequence of bytes starting at the specified address.
        /// </summary>
        /// <param name="address">The first address to read.</param>
        /// <param name="buffer">The buffer to populate with data.</param>
        /// <param name="length">The number of bytes to read into the buffer.</param>
        public void Read(UInt16 address, Byte[] buffer, UInt16 length)
        {
            unsafe
            {
                fixed (Byte* fBuffer = buffer)
                fixed (Byte* fData = &Data[address - Start])
                {
                    var pBuffer = fBuffer;
                    var pData = fData;
                    for (var i = 0; i < length; i++)
                    {
                        *pBuffer++ = *pData++;
                    }
                }
            }
        }

        /// <summary>
        /// Writes a single byte to the specified address.
        /// </summary>
        /// <param name="address">The address to which to write a value.</param>
        /// <param name="value">The value to write to the specified address.</param>
        public void Write(UInt16 address, Byte value)
        {
            Data[address - Start] = value;
        }

        /// <summary>
        /// Writes a sequence of bytes starting at the specified address.
        /// </summary>
        /// <param name="address">The first address to write.</param>
        /// <param name="buffer">The buffer that contains the data to write.</param>
        /// <param name="length">The number of bytes to write out of the buffer.</param>
        public void Write(UInt16 address, Byte[] buffer, UInt16 length)
        {
            unsafe
            {
                fixed (Byte* fBuffer = buffer)
                fixed (Byte* fData = &Data[address - Start])
                {
                    var pBuffer = fBuffer;
                    var pData = fData;
                    for (var i = 0; i < length; i++)
                    {
                        *pData++ = *pBuffer++;
                    }
                }
            }
        }

        /// <summary>
        /// Gets an array that contains the block's raw data.
        /// </summary>
        public Byte[] Data { get; }

        /// <summary>
        /// Gets the block's starting position within the 16-bit address space.
        /// </summary>
        public UInt16 Start { get; }

        /// <summary>
        /// Gets the block's ending position (exclusive) within the 16-bit address space.
        /// </summary>
        public UInt16 End { get; }

        /// <summary>
        /// Gets a value indicating whether this block is marked as read-only.
        /// </summary>
        public Boolean IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether this block is currently present. If not, it will never respond to bus requests.
        /// </summary>
        public Boolean IsPresent { get; set; }
    }
}