using System;
using System.Collections.Generic;

public class Hb8bBus
{
    private const UInt16 TotalAddressableMemory = UInt16.MaxValue;
    private const UInt16 SystemRamLocation = 0x0000;
    private const UInt16 SystemRamSize = 0x2000;
    private const UInt16 VideoRamLocation = SystemRamLocation + SystemRamSize;
    private const UInt16 VideoRamSize = 0x2000;
    private const UInt16 SystemRomSize = 0x2000;
    private const UInt16 SystemRomLocation = (TotalAddressableMemory + 1) - SystemRomSize;

    private readonly HashSet<Hb8bBusPeripheral> _irqAssertions = new HashSet<Hb8bBusPeripheral>();

    public Hb8bBus()
    {
        this.Cpu = new Hb8bCpu(this);
        this.VideoCircuit = new Hb8bVideoCircuit(this);
    }
    
    public void Clock()
    {
        this.Cpu.Clock();
        this.VideoCircuit.Clock();
    }

    public void ClockUntilFrameIsReady()
    {
        while (!this.VideoCircuit.FrameIsReady)
            Clock();
    }

    public void ClockUntilNextInstruction()
    {
        do
        {
            Clock();
        }
        while (!this.Cpu.IsPendingInstructionFetch);
    }

    public void Reset()
    {
        _irqAssertions.Clear();

        this.Cpu.Reset();
        this.VideoCircuit.Reset();    
    }

    public void AssertInterruptRequest(Hb8bBusPeripheral peripheral)
    {
        if (peripheral == null)
            throw new ArgumentNullException(nameof(peripheral));

        _irqAssertions.Add(peripheral);
    }

    public void ReleaseInterruptRequest(Hb8bBusPeripheral peripheral)
    {
        if (peripheral == null)
            throw new ArgumentNullException(nameof(peripheral));

        _irqAssertions.Remove(peripheral);
    }

    public void Write(Hb8bBusPeripheral writer, UInt16 address, Byte data)
    {
        if (SystemRam.ContainsAddress(address))
            SystemRam[address - SystemRam.Position] = data;

        if (VideoRam.ContainsAddress(address))
            VideoRam[address - VideoRam.Position] = data;
    }

    public Byte Read(Hb8bBusPeripheral reader, UInt16 address)
    {
        if (SystemRam.ContainsAddress(address))
            return SystemRam[address - SystemRam.Position];

        if (SystemRom.ContainsAddress(address))
            return SystemRom[address - SystemRom.Position];

        return 0x00;
    }
    
    public Hb8bCpu Cpu { get; }

    public Hb8bVideoCircuit VideoCircuit { get; }
    
    public Hb8bMemoryBlock SystemRam { get; } = new Hb8bMemoryBlock(SystemRamLocation, SystemRamSize, false);

    public Hb8bMemoryBlock VideoRam { get; } = new Hb8bMemoryBlock(VideoRamLocation, VideoRamSize, false);

    public Hb8bMemoryBlock SystemRom { get; } = new Hb8bMemoryBlock(SystemRomLocation, SystemRomSize, true);
    
    public Boolean IsInterruptAsserted => _irqAssertions.Count > 0;
}