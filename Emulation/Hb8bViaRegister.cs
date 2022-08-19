namespace Hb8b.Emulation
{
    /// <summary>
    /// The addressable registers of a 65C22 VIA chip.
    /// </summary>
    public enum Hb8bViaRegister : byte
    {
        T1CL = 0x4,
        T1CH = 0x5,
        T1LL = 0x6,
        T1LH = 0x7,
        T2CL = 0x8,
        T2CH = 0x9,
        ACR = 0xB,
        PCR = 0xC,
        IFR = 0xD,
        IER = 0xE,
    }
}