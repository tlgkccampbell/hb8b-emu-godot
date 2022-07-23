using System;
#nullable enable

namespace Hb8b.Emulation
{
    /// <summary>
    /// A peripheral that represents the system's main memory.
    /// </summary>
    public class Hb8bSystemMemory : Hb8bPeripheral
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bSystemMemory"/> class.
        /// </summary>
        /// <param name="bus">The system bus to which the system memory is attached.</param>
        /// <param name="offset">The memory's offset within the system's memory map.</param>
        /// <param name="size">The memory's size in bytes.
        /// <param name="fill">The value with which to fill the memory, or <see langword="null"/> to randomize the memory's contents.</param>
        public Hb8bSystemMemory(Hb8bSystemBus bus, UInt16 offset, UInt16 size, Byte? fill = null)
            : base(bus)
        {
            this.Offset = offset;
            this.Size = size;
            this.Memory = new Byte[Size];

            if (fill == null)
                MemoryAllocator.FillWithRandomBytes(this.Memory);
            else
                MemoryAllocator.FillWithByte(this.Memory, fill.Value);
        }

        /// <summary>
        /// Gets the memory's offset within the system's memory map.
        /// </summary>
        public UInt16 Offset { get; }

        /// <summary>
        /// Gets the memory's size in bytes.
        /// </summary>
        public UInt16 Size { get; }

        /// <summary>
        /// Gets the array that represents the peripheral's memory.
        /// </summary>
        public readonly Byte[] Memory;
    }
}