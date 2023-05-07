namespace Hb8b.Emulation
{
    /// <summary>
    /// Represents the subdivisions of the system's address space.
    /// </summary>
    public enum Hb8bMemoryBlock
    {
        /// <summary>
        /// The system's work RAM.
        /// </summary>
        WorkRam,

        /// <summary>
        /// The system's video RAM.
        /// </summary>
        VideoRam,

        /// <summary>
        /// The system's first IO device.
        /// </summary>
        IO0,

        /// <summary>
        /// The system's second IO device.
        /// </summary>
        IO1,

        /// <summary>
        /// The system's third IO device.
        /// </summary>
        IO2,

        /// <summary>
        /// The system's fourth IO device.
        /// </summary>
        IO3,

        /// <summary>
        /// The system's ROM.
        /// </summary>
        Rom,
    }
}
