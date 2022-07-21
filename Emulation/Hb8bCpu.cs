using System;

public partial class Hb8bCpu : Hb8bBusPeripheral
{
    private readonly Hb8bInstructionMetadata[] _instructions;

    private UInt16 _pc;
    private Byte _status;
    private Byte _stkp;
    private Byte _a;
    private Byte _x;
    private Byte _y;

    private Hb8bInstructionMetadata _currentInstruction;
    private Byte _currentOpcode;
    private Byte _opData;
    private UInt16 _opAddrAbs;
    private UInt16 _opAddrRel;
    private Boolean _queuedNmi;

    private UInt64 _cyclesElapsed;
    private UInt16 _cyclesForInstruction;
    
    public Hb8bCpu(Hb8bBus bus)
        : base(bus)
    {
        _instructions = InitializeInstructionSet();
    }
 
    public void Clock()
    {
        if (this.IsStopped)
            return;

        if (IsPendingInstructionFetch)
        {
            this.IsAccessingVram = false;

            var needsNmi = _queuedNmi;
            if (needsNmi)
                RaiseNonMaskableInterrupt();

            var needsIrq = Bus.IsInterruptAsserted && !needsNmi;
            if (needsIrq)
                RaiseInterruptRequest();

            if (!this.IsWaitingForInterrupt)
            {
                _currentOpcode = Read(_pc++);
                _currentInstruction = _instructions[_currentOpcode];

                var extraCycle = _currentInstruction.Operation();
                _cyclesForInstruction = (UInt16)(_currentInstruction.Cycles + extraCycle);

                if (_currentInstruction.HaltsUntilInterrupt)
                {
                    _cyclesElapsed += _cyclesForInstruction;
                    _cyclesForInstruction = 0;
                }

                _currentInstruction = null;
            }
        }

        if (_cyclesForInstruction > 0)
            _cyclesForInstruction--;

        _cyclesElapsed++;
    }

    public void Reset()
    {
        // Set the program counter to the reset vector.
        _opAddrAbs = 0xFFFC;
        var resetVectorLo = (UInt16)Read(_opAddrAbs + 0);
        var resetVectorHi = (UInt16)Read(_opAddrAbs + 1);
        _pc = (UInt16)((resetVectorHi << 8) | resetVectorLo);

        // Reset the CPU registers.        
        _a = 0;
        _x = 0;
        _y = 0;
        _stkp = MemoryAllocator.GetRandomByte();
        _status = (Byte)Hb8b6502StatusFlag.U;

        // Reset helpers.
        _queuedNmi = false;
        _opAddrAbs = 0x0000;
        _opAddrRel = 0x0000;
        _opData = 0x00;

        // Cycle count.
        _cyclesForInstruction = 8;
        _cyclesElapsed = 0;

        // Clear VRAM, WAI, STP
        this.IsAccessingVram = false;
        this.IsWaitingForInterrupt = false;
        this.IsStopped = false;
    }

    public void RaiseInterruptRequest()
    {
        if (GetStatusFlag(Hb8b6502StatusFlag.I) && !this.IsWaitingForInterrupt)
            return;

        StackPush16(_pc);
        StackPush(_status | (Byte)Hb8b6502StatusFlag.U);

        SetStatusFlag(Hb8b6502StatusFlag.D, false);
        SetStatusFlag(Hb8b6502StatusFlag.I, true);

        _opAddrAbs = 0xFFFE;
        var vectorLo = Read(_opAddrAbs + 0);
        var vectorHi = Read(_opAddrAbs + 1);
        _pc = (UInt16)((vectorHi << 8) | vectorLo);

        _cyclesForInstruction = 7;

        this.IsWaitingForInterrupt = false;
    }

    public void RaiseNonMaskableInterrupt()
    {
        StackPush16(_pc);
        StackPush(_status | (Byte)Hb8b6502StatusFlag.U);

        SetStatusFlag(Hb8b6502StatusFlag.D, false);
        SetStatusFlag(Hb8b6502StatusFlag.I, true);

        _opAddrAbs = 0xFFFA;
        var vectorLo = Read(_opAddrAbs + 0);
        var vectorHi = Read(_opAddrAbs + 1);
        _pc = (UInt16)((vectorHi << 8) | vectorLo);

        _cyclesForInstruction = 8;
        _queuedNmi = false;

        this.IsWaitingForInterrupt = false;
    }

    public void QueueNonMaskableInterrupt()
    {
        _queuedNmi = true;
    }

    public Boolean GetStatusFlag(Hb8b6502StatusFlag flag)
    {
        return (_status & (Byte)flag) == (Byte)flag;
    }

    public void SetStatusFlag(Hb8b6502StatusFlag flag, Boolean value)
    {
        if (flag == Hb8b6502StatusFlag.U || flag == Hb8b6502StatusFlag.B)
            return;

        Bitwise.SetOrClearMask(ref _status, (Byte)flag, value);
    }

    public void StackPush(Int32 value)
    {
        Write(0x0100 + _stkp, (Byte)value);
        _stkp--;
    }

    public void StackPush16(UInt16 value)
    {
        StackPush((Byte)((value >> 8) & 0xFF));
        StackPush((Byte)(value & 0xFF));
    }

    public Byte StackPop()
    {
        _stkp++;
        var result = Read(0x0100 + _stkp);
        return result;
    }

    public UInt16 StackPop16()
    {
        return (UInt16)(StackPop() | (StackPop() << 8));
    }

    public UInt64 CyclesElapsed => _cyclesElapsed;

    public UInt16 ProgramCounter { get => _pc; set => _pc = value; }

    public Byte Status { get => _status; set => _status = value; }

    public Byte StackPointer { get => _stkp; set => _stkp = value; }

    public Byte Accumulator { get => _a; set => _a = value; }

    public Byte X { get => _x; set => _x = value; }

    public Byte Y { get => _y; set => _y = value; }

    public Boolean IsPendingInstructionFetch => _cyclesForInstruction <= 0;

    public Boolean IsWaitingForInterrupt { get; set; }

    public Boolean IsStopped { get; set; }

    public Boolean IsAccessingVram { get; set; }
}