using System;

public abstract class Hb8bBusPeripheral
{
    protected Hb8bBusPeripheral(Hb8bBus bus)
    {
        this.Bus = bus;
    }

    public Hb8bBus Bus { get; }

    protected void Write(UInt16 address, Byte data)
    {
        Bus.Write(this, address, data);
    }

    protected void Write(Int32 address, Int32 data)
    {
        Bus.Write(this, (UInt16)address, (Byte)data);
    }

    protected Byte Read(UInt16 address)
    {
        var data = Bus.Read(this, address);
        return data;
    }

    protected Byte Read(Int32 address)
    {
        var data = Bus.Read(this, (UInt16)address);
        return data;
    }

    protected UInt16 Read16(UInt16 address)
    {
        var lo = Bus.Read(this, (UInt16)(address + 0));
        var hi = Bus.Read(this, (UInt16)(address + 1));
        return (UInt16)((hi << 8) | lo);
    }

    protected UInt16 Read16(Int32 address)
    {
        var lo = Bus.Read(this, (UInt16)(address + 0));
        var hi = Bus.Read(this, (UInt16)(address + 1));
        return (UInt16)((hi << 8) | lo);
    }
}