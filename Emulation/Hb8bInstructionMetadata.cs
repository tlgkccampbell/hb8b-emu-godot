using System;

public enum AddressingMode { IMP, IMM, ZP0, ZPX, ZPY, REL, ABS, ABX, ABY, IND, IZX, IZY, IZP, ZPREL, INDX };

public class Hb8bInstructionMetadata
{
    public Hb8bInstructionMetadata(string mnemonic, AddressingMode addressingMode, Func<Int32> operation, UInt16 cycles, Boolean HaltsUntilInterrupt = false)
    {
        this.Mnemonic = mnemonic;
        this.AddressingMode = addressingMode;
        this.Operation = operation;
        this.Cycles = cycles;
        this.HaltsUntilInterrupt = HaltsUntilInterrupt;
    }

    public String Mnemonic { get; }

    public AddressingMode AddressingMode { get; }

    public Func<Int32> Operation { get; }

    public UInt16 Cycles { get; }

    public Boolean HaltsUntilInterrupt { get; }
}