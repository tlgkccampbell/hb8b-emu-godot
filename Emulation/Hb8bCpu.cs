using System;
using System.Runtime.CompilerServices;

namespace Hb8b.Emulation
{
    /// <summary>
    /// The HB8B's 6502 CPU.
    /// </summary>
    public class Hb8bCpu : Hb8bPeripheral
    {
        private const UInt16 VectorAddressNmi = 0xFFFA;
        private const UInt16 VectorAddressReset = 0xFFFC;
        private const UInt16 VectorAddressIrq = 0xFFFE;

        // CPU registers.
        private UInt16 _pc;
        private Byte _status;
        private Byte _stkp = MemoryAllocator.GetRandomByte();
        private Byte _acc = MemoryAllocator.GetRandomByte();
        private Byte _x = MemoryAllocator.GetRandomByte();
        private Byte _y = MemoryAllocator.GetRandomByte();

        // Instruction truncation.
        private UInt32 _truncationCycles;
        private UInt16 _truncationAddr;
        private Byte _truncationOpenBus;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bCpu"/> class.
        /// </summary>
        /// <param name="bus">The system bus to which the CPU is attached.</param>
        public Hb8bCpu(Hb8bSystemBus bus)
            : base(bus)
        { }

        /// <inheritdoc/>
        public override void Reset()
        {
            // Reset processor status.
            _pc = Bus.Read16(VectorAddressReset);
            _status = (Byte)Hb8bCpuStatusFlags.U | (Byte)Hb8bCpuStatusFlags.I;

            // Reset execution status.
            this.IsWaitingForInterrupt = false;
            this.IsStopped = false;
        }

        /// <summary>
        /// Advances the CPU clock by the specified number of cycles.
        /// </summary>
        /// <param name="cycles">The number of cycles by which to advance the CPU clock. If negative, the CPU will clock a single instruction.</param>
        /// <param name="stepped">A value indicating whether the processor is being manually stepped forward.</param>
        /// <returns>The number of cycles that were actually executed.</returns>
        public UInt32 Clock(UInt32 cycles, Boolean stepped = false)
        {            
            if (IsSuspended && !stepped)
                return 0;

            var singleStepInstruction = cycles == 0;
            var remainingClockCycles = singleStepInstruction ? UInt32.MaxValue : cycles;

            while (remainingClockCycles > 0)
            {
                _truncationAddr = _pc;
                _truncationOpenBus = Bus.OpenBusValue;

                if (Bus.IsNmiRaised)
                {
                    var intCycles = HandleInterrupt(0xFFFA, ref remainingClockCycles);
                    IsWaitingForInterrupt = false;

                    if (singleStepInstruction)
                        return intCycles;

                    continue;
                }

                if (Bus.IsIrqAsserted && Bitwise.IsClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.I) && !IsWaitingForInterrupt)
                {
                    var intCycles = HandleInterrupt(0xFFFE, ref remainingClockCycles);
                    IsWaitingForInterrupt = false;

                    if (singleStepInstruction)
                        return intCycles;

                    continue;
                }

                if (!IsStopped && !IsWaitingForInterrupt)
                {
                    var instrOpcode = Bus.Read(_pc++);
                    var instrCycles = 0U;

                    switch (instrOpcode)
                    {
                        case 0x00: // BRK, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 7, out instrCycles))
                                    break;

                                StackPush16(_pc + 1);
                                StackPush(_status |
                                    (Byte)Hb8bCpuStatusFlags.B |
                                    (Byte)Hb8bCpuStatusFlags.U);

                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.D);
                                Bitwise.SetMask(ref _status, (Byte)Hb8bCpuStatusFlags.I);

                                _pc = Bus.Read16(VectorAddressIrq);
                            }
                            break;

                        case 0x01: // ORA, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x02: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x03: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x04: // TSB, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = TSB(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x05: // ORA, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x06: // ASL, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = ASL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x07: // RMB0, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 0);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x08: // PHP, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPush(_status |
                                    (Byte)Hb8bCpuStatusFlags.B |
                                    (Byte)Hb8bCpuStatusFlags.U);
                            }
                            break;

                        case 0x09: // ORA, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                ORA(data);
                            }
                            break;

                        case 0x0A: // ASL, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var result = ASL(data);
                                _acc = result;
                            }
                            break;

                        case 0x0B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x0C: // TSB, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = TSB(data);
                                
                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x0D: // ORA, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x0E: // ASL, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = ASL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x0F: // BBR0, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(0);
                            }
                            break;

                        case 0x10: // BPL, REL
                            {
                                var branchTarget = BranchOnStatusBitClear((Byte)Hb8bCpuStatusFlags.N, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x11: // ORA, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x12: // ORA, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();
                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x13: // ??? (NOP)
                            {
                                // 1-byte NOP
                            }
                            break;

                        case 0x14: // TRB, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = TRB(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x15: // ORA, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x16: // ASL, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                var result = ASL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x17: // RMB1, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 1);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x18: // CLC, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C);
                            }
                            break;

                        case 0x19: // ORA, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x1A: // INC, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _acc = INC(_acc);
                            }
                            break;

                        case 0x1B: // ??? (NOP)
                            {
                                // 1-byte NOP
                            }
                            break;

                        case 0x1C: // TRB, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = TRB(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x1D: // ORA, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                ORA(data);
                            }
                            break;

                        case 0x1E: // ASL, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 6U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                var result = ASL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x1F: // BBR1, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(1);
                            }
                            break;

                        case 0x20: // JSR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                StackPush16(_pc - 1);
                                _pc = addr;
                            }
                            break;

                        case 0x21: // AND, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x22: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x23: // ??? (NOP)
                            {
                                // 1-byte NOP
                            }
                            break;

                        case 0x24: // BIT, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                BIT(data);
                            }
                            break;

                        case 0x25: // AND, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x26: // ROL, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = ROL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x27: // RMB2, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 2);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x28: // PLP, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPop(out var newStatus);

                                var oldStatus = _status;
                                oldStatus &= 0b00110000;
                                newStatus &= 0b11001111;
                                _status = (Byte)(oldStatus | newStatus);
                            }
                            break;

                        case 0x29: // AND, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                AND(data);
                            }
                            break;

                        case 0x2A: // ROL, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var result = ROL(data);

                                _acc = result;
                            }
                            break;

                        case 0x2B: // ??? (NOP)
                            {
                                // 1-byte NOP
                            }
                            break;

                        case 0x2C: // BIT, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                BIT(data);
                            }
                            break;

                        case 0x2D: // AND, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x2E: // ROL, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = ROL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x2F: // BBR2, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(2);
                            }
                            break;

                        case 0x30: // BMI, REL
                            {
                                var branchTarget = BranchOnStatusBitSet((Byte)Hb8bCpuStatusFlags.N, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x31: // AND, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x32: // AND, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;


                                var addr = IZP();
                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x33: // ??? (NOP)
                            {
                                // 1-cycle NOP   
                            }
                            break;

                        case 0x34: // BIT, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                BIT(data);
                            }
                            break;

                        case 0x35: // AND, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x36: // ROL, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                var result = ROL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x37: // RMB3, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 3);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x38: // SEC, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.SetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C);
                            }
                            break;

                        case 0x39: // AND, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x3A: // DEC, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _acc = DEC(_acc);
                            }
                            break;

                        case 0x3B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x3C: // BIT, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                BIT(data);
                            }
                            break;

                        case 0x3D: // AND, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                AND(data);
                            }
                            break;

                        case 0x3E: // ROL, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                var result = ROL(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x3F: // BBR3, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(3);
                            }
                            break;

                        case 0x40: // RTI, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                StackPop(out var newStatus);
                                
                                var oldStatus = _status;
                                oldStatus &= 0b00110000;
                                newStatus &= 0b11001111;
                                _status = (Byte)(oldStatus | newStatus);

                                StackPop16(out _pc);
                            }
                            break;

                        case 0x41: // EOR, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x42: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x43: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x44: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x45: // EOR, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x46: // LSR, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = LSR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x47: // RMB4, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 4);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x48: // PHA, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPush(_acc);
                            }
                            break;

                        case 0x49: // EOR, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                EOR(data);
                            }
                            break;

                        case 0x4A: // LSR, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var result = LSR(data);

                                _acc = result;
                            }
                            break;

                        case 0x4B: // ??? (NOP)
                            {
                                // 1-cycle NOP   
                            }
                            break;

                        case 0x4C: // JMP, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                _pc = ABS();
                            }
                            break;

                        case 0x4D: // EOR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x4E: // LSR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = LSR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x4F: // BBR4, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(4);
                            }
                            break;

                        case 0x50: // BVC, REL
                            {
                                var branchTarget = BranchOnStatusBitClear((Byte)Hb8bCpuStatusFlags.V, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x51: // EOR, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x52: // EOR, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();
                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x53: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x54: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x55: // EOR, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x56: // LSR, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                var result = LSR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x57: // RMB5, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 5);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x58: // CLI, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.I);
                            }
                            break;

                        case 0x59: // EOR, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x5A: // PHY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPush(_y);
                            }
                            break;

                        case 0x5B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x5C: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 8, out instrCycles))
                                    break;

                                _pc += 2;
                            }
                            break;

                        case 0x5D: // EOR, ABX
                            {
                                var addr = ABX(out var extraCycles);
                                
                                if (CheckTruncatedInstruction(remainingClockCycles, 4 + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                EOR(data);
                            }
                            break;

                        case 0x5E: // LSR, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 6 + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                var result = LSR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x5F: // BBR5, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(5);
                            }
                            break;

                        case 0x60: // RTS, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                StackPop16(out _pc);
                                _pc++;
                            }
                            break;

                        case 0x61: // ADC, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x62: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x63: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x64: // STZ, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();

                                Bus.Write(addr, 0);
                            }
                            break;

                        case 0x65: // ADC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x66: // ROR, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = ROR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x67: // RMB6, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 6);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x68: // PLA, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPop(out _acc);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x69: // ADC, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                ADC(data);
                            }
                            break;

                        case 0x6A: // ROR, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var result = ROR(data);

                                _acc = result;
                            }
                            break;

                        case 0x6B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x6C: // JMP, IND
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrInd = Bus.Read16(_pc);
                                var addrAbs = Bus.Read16(addrInd);

                                _pc = addrAbs;
                            }
                            break;

                        case 0x6D: // ADC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x6E: // ROR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = ROR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x6F: // BBR6, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(6);
                            }
                            break;

                        case 0x70: // BVS, REL
                            {
                                var branchTarget = BranchOnStatusBitSet((Byte)Hb8bCpuStatusFlags.V, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x71: // ADC, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x72: // ADC, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();
                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x73: // ??? (NOP)
                            {
                                // 1-byte NOP
                            }
                            break;

                        case 0x74: // STZ, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                
                                Bus.Write(addr, 0);
                            }
                            break;

                        case 0x75: // ADC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x76: // ROR, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                var result = ROR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x77: // RMB7, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Clr(ref data, 7);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x78: // SEI, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.SetMask(ref _status, (Byte)Hb8bCpuStatusFlags.I);
                            }
                            break;

                        case 0x79: // ADC, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x7A: // PLY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPop(out _y);
                                SetStatusZN(_y);
                            }
                            break;

                        case 0x7B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x7C: // JMP, INDX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = (UInt16)(Bus.Read16(_pc) + _x);
                                var addrAbs = Bus.Read16(addrIdx);

                                _pc = addrAbs;
                            }
                            break;

                        case 0x7D: // ADC, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                ADC(data);
                            }
                            break;

                        case 0x7E: // ROR, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 6U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                var result = ROR(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0x7F: // BBR7, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBR(7);
                            }
                            break;

                        case 0x80: // BRA, REL
                            {
                                var extraCycles = 1U;

                                var addrJmp = REL();
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = addrJmp;

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x81: // STA, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x82: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0x83: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x84: // STY, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();

                                Bus.Write(addr, _y);
                            }
                            break;

                        case 0x85: // STA, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x86: // STX, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();

                                Bus.Write(addr, _x);
                            }
                            break;

                        case 0x87: // SMB0, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 0);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x88: // DEY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _y = DEC(_y);
                            }
                            break;

                        case 0x89: // BIT, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                BIT(data);
                            }
                            break;

                        case 0x8A: // TXA, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _acc = _x;
                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x8B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x8C: // STY, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();

                                Bus.Write(addr, _y);
                            }
                            break;

                        case 0x8D: // STA, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x8E: // STX, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();

                                Bus.Write(addr, _x);
                            }
                            break;

                        case 0x8F: // BBS0, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(0);
                            }
                            break;

                        case 0x90: // BCC, REL
                            {
                                var branchTarget = BranchOnStatusBitClear((Byte)Hb8bCpuStatusFlags.C, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x91: // STA, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 6U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x92: // STA, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x93: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x94: // STY, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();

                                Bus.Write(addr, _y);
                            }
                            break;

                        case 0x95: // STA, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x96: // STX, ZPY
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPY();

                                Bus.Write(addr, _x);
                            }
                            break;

                        case 0x97: // SMB1, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 1);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0x98: // TYA, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _acc = _y;
                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x99: // STA, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x9A: // TXS, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _stkp = _x;
                            }
                            break;

                        case 0x9B: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x9C: // STZ, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();

                                Bus.Write(addr, 0);
                            }
                            break;

                        case 0x9D: // STA, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addr, _acc);
                            }
                            break;

                        case 0x9E: // STZ, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addr, 0);
                            }
                            break;

                        case 0x9F: // BBS1, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(1);
                            }
                            break;

                        case 0xA0: // LDY, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                LDY(data);
                            }
                            break;

                        case 0xA1: // LDA, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xA2: // LDX, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                LDX(data);
                            }
                            break;

                        case 0xA3: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xA4: // LDY, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = Bus.Read(_pc++);
                                var data = Bus.Read(addr);
                                LDY(data);
                            }
                            break;

                        case 0xA5: // LDA, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = Bus.Read(_pc++);
                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xA6: // LDX, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = Bus.Read(_pc++);
                                var data = Bus.Read(addr);
                                LDX(data);
                            }
                            break;

                        case 0xA7: // SMB2, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 2);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0xA8: // TAY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _y = _acc;
                                SetStatusZN(_y);
                            }
                            break;

                        case 0xA9: // LDA, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                LDA(data);
                            }
                            break;

                        case 0xAA: // TAX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x = _acc;
                                SetStatusZN(_x);
                            }
                            break;

                        case 0xAB: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xAC: // LDY, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                LDY(data);
                            }
                            break;

                        case 0xAD: // LDA, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xAE: // LDX, ABS 
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                LDX(data);
                            }
                            break;

                        case 0xAF: // BBS2, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(2);
                            }
                            break;

                        case 0xB0: // BCS, REL
                            {
                                var branchTarget = BranchOnStatusBitSet((Byte)Hb8bCpuStatusFlags.C, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0xB1: // LDA, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xB2: // LDA, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();
                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xB3: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xB4: // LDY, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                LDY(data);
                            }
                            break;

                        case 0xB5: // LDA, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xB6: // LDX, ZPY
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPY();
                                var data = Bus.Read(addr);
                                LDX(data);
                            }
                            break;

                        case 0xB7: // SMB3, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 3);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0xB8: // CLV, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V);
                            }
                            break;

                        case 0xB9: // LDA, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xBA: // TSX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x = _stkp;
                                SetStatusZN(_x);
                            }
                            break;

                        case 0xBB: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xBC: // LDY, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                LDY(data);
                            }
                            break;

                        case 0xBD: // LDA, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                LDA(data);
                            }
                            break;

                        case 0xBE: // LDX, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                LDX(data);
                            }
                            break;

                        case 0xBF: // BBS3, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(3);
                            }
                            break;

                        case 0xC0: // CPY, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                CPY(data);
                            }
                            break;

                        case 0xC1: // CMP, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xC2: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0xC3:
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xC4: // CPY, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                CPY(data);
                            }
                            break;

                        case 0xC5: // CMP, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xC6: // DEC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = DEC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xC7: // SMB4, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 4);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0xC8: // INY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _y = INC(_y);
                            }
                            break;

                        case 0xC9: // CMP, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xCA: // DEX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x = DEC(_x);
                            }
                            break;

                        case 0xCB: // WAI, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                this.IsWaitingForInterrupt = true;
                            }
                            break;

                        case 0xCC: // CPY, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                CPY(data);
                            }
                            break;

                        case 0xCD: // CMP, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xCE: // DEC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = DEC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xCF: // BBS4, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(4);
                            }
                            break;

                        case 0xD0: // BNE, REL
                            {
                                var branchTarget = BranchOnStatusBitClear((Byte)Hb8bCpuStatusFlags.Z, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0xD1: // CMP, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xD2: // CMP, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();
                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xD3:
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xD4:
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0xD5: // CMP, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xD6: // DEC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                var result = DEC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xD7: // SMB5, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 5);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0xD8: // CLD, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.D);
                            }
                            break;

                        case 0xD9: // CMP, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xDA: // PHX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPush(_x);
                            }
                            break;

                        case 0xDB: // STP, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                this.IsStopped = true;
                            }
                            break;

                        case 0xDC: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                _pc += 2;
                            }
                            break;

                        case 0xDD: // CMP, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                CMP(_acc, data);
                            }
                            break;

                        case 0xDE: // DEC, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 7U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                var result = DEC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xDF: // BBS5, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(5);
                            }
                            break;

                        case 0xE0: // CPX, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                CMP(_x, data);
                            }
                            break;

                        case 0xE1: // SBC, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = IZX();
                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xE2: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0xE3: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xE4: // CPX, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                CMP(_x, data);
                            }
                            break;

                        case 0xE5: // SBC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xE6: // INC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                var result = INC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xE7: // SMB6, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 6);
                            }
                            break;

                        case 0xE8: // INX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x = INC(_x);
                            }
                            break;

                        case 0xE9: // SBC, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                SBC(data);
                            }
                            break;

                        case 0xEA: // NOP, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                // The one and only.
                            }
                            break;

                        case 0xEB: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xEC: // CPX, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                CMP(_x, data);
                            }
                            break;

                        case 0xED: // SBC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xEE: // INC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addr = ABS();
                                var data = Bus.Read(addr);
                                var result = INC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xEF: // BBS6, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(6);
                            }
                            break;

                        case 0xF0: // BEQ, REL
                            {
                                var branchTarget = BranchOnStatusBitSet((Byte)Hb8bCpuStatusFlags.Z, out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0xF1: // SBC, IZY
                            {
                                var addr = IZY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xF2: // SBC, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = IZP();
                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xF3: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xF4: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                _pc += 1;
                            }
                            break;

                        case 0xF5: // SBC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xF6: // INC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZPX();
                                var data = Bus.Read(addr);
                                var result = INC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xF7: // SMB7, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addr = ZP0();
                                var data = Bus.Read(addr);
                                Bitwise.Set(ref data, 7);

                                Bus.Write(addr, data);
                            }
                            break;

                        case 0xF8: // SED, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                Bitwise.SetMask(ref _status, (Byte)Hb8bCpuStatusFlags.D);
                            }
                            break;

                        case 0xF9: // SBC, ABY
                            {
                                var addr = ABY(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xFA: // PLX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPop(out _x);
                                SetStatusZN(_x);
                            }
                            break;

                        case 0xFB: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xFC: // ??? (NOP)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                _pc += 2;
                            }
                            break;

                        case 0xFD: // SBC, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                SBC(data);
                            }
                            break;

                        case 0xFE: // INC, ABX
                            {
                                var addr = ABX(out var extraCycles);

                                if (CheckTruncatedInstruction(remainingClockCycles, 7U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addr);
                                var result = INC(data);

                                Bus.Write(addr, result);
                            }
                            break;

                        case 0xFF: // BBS7, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                BBS(7);
                            }
                            break;

                        default:
                            throw new NotImplementedException(String.Format("Opcode not yet implemented: ${0:x2}", instrOpcode));
                    }

                    ClockPeripherals(instrCycles);
                    remainingClockCycles -= instrCycles;

                    if (singleStepInstruction)
                        return instrCycles;
                }
                else
                {
                    ClockPeripheralsUntilInterrupt(ref remainingClockCycles);
                }
            }

            return cycles - remainingClockCycles;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the processor's emulation is current suspended.
        /// While suspended, the processor will not execute cycles unless manually stepped forward.
        /// </summary>
        public Boolean IsSuspended { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the processor is currently stopped.
        /// While stopped, the processor will not execute any instructions. In normal execution,
        /// the processor will not restart unless reset.
        /// </summary>
        public Boolean IsStopped { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the processor is waiting for an interrupt signal.
        /// While waiting, the processor will not execute any instructions. Execution
        /// will resume when an interrupt is signaled.
        /// </summary>
        public Boolean IsWaitingForInterrupt { get; set; } = false;

        /// <summary>
        /// Gets the value in the accumulator.
        /// </summary>
        public Byte Accumulator => _acc;

        /// <summary>
        /// Gets the value in the X register.
        /// </summary>
        public Byte XRegister => _x;

        /// <summary>
        /// Gets the value in the Y register.
        /// </summary>
        public Byte YRegister => _y;

        /// <summary>
        /// Gets the value of the stack pointer.
        /// </summary>
        public Byte StackPointer => _stkp;

        /// <summary>
        /// Gets the value of the status register.
        /// </summary>
        public Byte StatusRegister => _status;

        /// <summary>
        /// Gets the address in the program counter register.
        /// </summary>
        public UInt16 ProgramCounter
        {
            get => _pc;
            set => _pc = value;
        }

        /// <summary>
        /// Implements a branch that is taken when a particular status bit is cleared.
        /// </summary>
        /// <param name="bit">The index of the status bit to examine.</param>
        /// <param name="extraCycles">The number of extra cycles required by the branch instruction.</param>
        /// <returns>The program counter's value after the branch executes, whether it was taken or not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 BranchOnStatusBitClear(Byte bit, out UInt32 extraCycles)
        {
            extraCycles = 0U;

            var addrJmp = REL();
            var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
            if (addrJmpIsInDifferentPage)
                extraCycles++;

            var branchTarget = _pc;
            var branchIsTaken = Bitwise.IsClrMask(ref _status, bit);
            if (branchIsTaken)
            {
                extraCycles++;
                branchTarget = addrJmp;
            }

            return branchTarget;
        }

        /// <summary>
        /// Implements a branch that is taken when a particular status bit is set.
        /// </summary>
        /// <param name="bit">The index of the status bit to examine.</param>
        /// <param name="extraCycles">The number of extra cycles required by the branch instruction.</param>
        /// <returns>The program counter's value after the branch executes, whether it was taken or not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 BranchOnStatusBitSet(Byte bit, out UInt32 extraCycles)
        {
            extraCycles = 0U;

            var addrJmp = REL();
            var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
            if (addrJmpIsInDifferentPage)
                extraCycles++;

            var branchTarget = _pc;
            var branchIsTaken = Bitwise.IsSetMask(ref _status, bit);
            if (branchIsTaken)
            {
                extraCycles++;
                branchTarget = addrJmp;
            }

            return branchTarget;
        }

        /// <summary>
        /// Implements the ABS addressing mode. An absolute address is retrieved from
        /// the current program counter location.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 ABS()
        {
            var addrAbs = Bus.Read16(_pc);
            _pc += 2;
            return addrAbs;
        }

        /// <summary>
        /// Implements the ABX addressing mode. An absolute address is retrieved from
        /// the current program counter location, then the content of the X register
        /// is added to it to produce the final address.
        /// </summary>
        /// <param name="extraCycles">The number of extra cycles that will be required by the instruction.</param>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 ABX(out UInt32 extraCycles)
        {
            var addrAbs = Bus.Read16(_pc);
            var addrAbsOffset = (UInt16)(addrAbs + _x);
            _pc += 2;

            extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

            return addrAbsOffset;
        }

        /// <summary>
        /// Implements the ABY addressing mode. An absolute address is retrieved from
        /// the current program counter location, then the content of the Y register
        /// is added to it to produce the final address.
        /// </summary>
        /// <param name="extraCycles">The number of extra cycles that will be required by the instruction.</param>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 ABY(out UInt32 extraCycles)
        {
            var addrAbs = Bus.Read16(_pc);
            var addrAbsOffset = (UInt16)(addrAbs + _y);
            _pc += 2;

            extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

            return addrAbsOffset;
        }

        /// <summary>
        /// Implements the REL addressing mode. A relative address is retrieved from
        /// the current program counter location, then added to the program counter.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 REL()
        {            
            var addrRel = (UInt16)Bus.Read(_pc++);
            addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;
            var addrAbs = (UInt16)(_pc + addrRel);
            return addrAbs;
        }

        /// <summary>
        /// Implements the ZP0 addressing mode. A zero-page address is retrieved from the
        /// current program counter location.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 ZP0()
        {
            return Bus.Read(_pc++);
        }

        /// <summary>
        /// Implements the ZPX addressing mode. A zero-page address is retrieved from the
        /// current program counter location and the content of the X register is added to it,
        /// wrapping to remain within the zero page if necessary.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 ZPX()
        {
            return (Byte)(Bus.Read(_pc++) + _x);
        }

        /// <summary>
        /// Implements the ZPY addressing mode. A zero-page address is retrieved from the
        /// current program counter location and the content of the Y register is added to it,
        /// wrapping to remain within the zero page if necessary.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 ZPY()
        {
            return (Byte)(Bus.Read(_pc++) + _y);
        }

        /// <summary>
        /// Implements the IZP addressing mode. A zero-page address is retrieved from the 
        /// current program counter location and the absolute address is read from that location.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 IZP()
        {
            var addrIdx = Bus.Read(_pc++);
            var addrAbs = Bus.Read16ZeroPage(addrIdx);
            return addrAbs;
        }

        /// <summary>
        /// Implements the IZX addressing mode. A zero-page address is retrieved from the 
        /// current program counter location, and the content of the X register is added
        /// to it. The absolute address is then read from the resulting address.
        /// </summary>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 IZX()
        {
            var addrIdx = (Byte)(Bus.Read(_pc++) + _x);
            var addrAbs = Bus.Read16ZeroPage(addrIdx);
            return addrAbs;
        }

        /// <summary>
        /// Implements the IZX addressing mode. A zero-page address is retrieved from the 
        /// current program counter location. The absolute address is then read from that
        /// address, and the content of the Y register is added to it.
        /// </summary>
        /// <param name="extraCycles">The number of extra cycles that will be required by the instruction.</param>
        /// <returns>The absolute address from which to retrieve data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt16 IZY(out UInt32 extraCycles)
        {
            var addrIdx = (Byte)Bus.Read(_pc++);
            var addrAbs = (UInt16)(Bus.Read16ZeroPage(addrIdx) + _y);
            extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;
            return addrAbs;
        }

        /// <summary>
        /// Implements the ADC (ADd with Carry) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ADC(Byte operand)
        {
            var a = (uint)_acc;
            var b = (uint)operand;

            if (a == 0 && b == 0)
                Console.WriteLine();

            var carry = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1U : 0U;
            var temp = a + b + carry;

            var bcd = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.D);
            if (bcd)
            {
                temp = (a & 0x0f) + (b & 0x0f) + carry;
                if (temp >= 10)
                    temp = (temp - 10) | 0x10;

                temp += (a & 0xf0) + (b & 0xf0);
                if (temp > 0x9f)
                    temp += 0x60;
            }

            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, temp > 0xFF);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V, ((a ^ temp) & (b ^ temp) & 0x80) != 0);

            _acc = (Byte)(temp & 0xFF);

            SetStatusZN(_acc);
        }

        /// <summary>
        /// Implements the AND (bitwise AND with accumulator) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AND(Byte operand)
        {
            _acc = (Byte)(_acc & operand);
            SetStatusZN(_acc);
        }

        /// <summary>
        /// Implements the ASL (Arithmetic Shift Left) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Byte ASL(Byte operand)
        {
            var result = (UInt16)(operand << 1);
            SetStatusCZN(result);
            return (Byte)result;
        }

        /// <summary>
        /// Implements the BIT (BIT test) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BIT(Byte operand)
        {
            var result = (Byte)(_acc & operand);
            SetStatusZ(result);
            _status = (Byte)((_status & 0b00111111) | (operand & 0b11000000));
        }

        /// <summary>
        /// Implements the BBR (Branch of Bit Reset) instruction.
        /// </summary>
        /// <param name="bit"/>The index of the bit to test when branching.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BBR(Int32 bit)
        {
            var addrTest = (UInt16)Bus.Read(_pc++);
            var addrJmp = REL();

            var dataTest = Bus.Read(addrTest);

            var branchTarget = _pc;
            var branchIsTaken = Bitwise.IsClr(ref dataTest, bit);
            if (branchIsTaken)
                branchTarget = addrJmp;

            _pc = branchTarget;
        }

        /// <summary>
        /// Implements the BBS (Branch of Bit Set) instruction.
        /// </summary>
        /// <param name="bit"/>The index of the bit to test when branching.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BBS(Int32 bit)
        {
            var addrTest = (UInt16)Bus.Read(_pc++);
            var addrJmp = REL();

            var dataTest = Bus.Read(addrTest);

            var branchTarget = _pc;
            var branchIsTaken = Bitwise.IsSet(ref dataTest, bit);
            if (branchIsTaken)
                branchTarget = addrJmp;

            _pc = branchTarget;
        }

        /// <summary>
        /// Implements the CMP (CoMPare with accumulator), CPX (ComPare with X register), and CPY (ComPare with Y register) instructions.
        /// </summary>
        /// <param name="register">The register with which to compare the operand.</param>
        /// <param name="operand">The instruction's operand.</param>
        private void CMP(Byte register, Byte operand)
        {
            var result = (UInt16)(register - operand);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, register >= operand);
            SetStatusZN((Byte)result);
        }

        /// <summary>
        /// Implements the CPY (ComPare with Y register) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CPY(Byte operand)
        {
            var result = (UInt16)(_y - operand);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _y >= operand);
            SetStatusZN((Byte)result);
        }

        /// <summary>
        /// Implements the DEC (DECrement) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Byte DEC(Byte operand)
        {
            var result = (Byte)(operand - 1);
            SetStatusZN(result);
            return result;
        }

        /// <summary>
        /// Implements the EOR (bitwise Exclusive OR) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        private void EOR(Byte operand)
        {
            _acc = (Byte)(_acc ^ operand);
            SetStatusZN(_acc);
        }

        /// <summary>
        /// Implements the INC (INCrement) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Byte INC(Byte operand)
        {
            var result = (Byte)(operand + 1);
            SetStatusZN(result);
            return result;
        }

        /// <summary>
        /// Implements the LSR (Logical Shift Right) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        private Byte LSR(Byte operand)
        {
            var result = (Byte)(operand >> 1);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (operand & 0x01) == 1);
            SetStatusZN(result);
            return result;
        }

        /// <summary>
        /// Implements the ORA (Logical OR with Accumulator) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ORA(Byte operand)
        {
            _acc |= operand;
            SetStatusZN(_acc);
        }

        /// <summary>
        /// Implements the LDA (Load Accumulator) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        private void LDA(Byte operand)
        {
            _acc = operand;
            SetStatusZN(_acc);
        }

        /// <summary>
        /// Implements the LDX (Load X register) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        private void LDX(Byte operand)
        {
            _x = operand;
            SetStatusZN(_x);
        }
        
        /// <summary>
        /// Implements the LDY (Load Y register) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        private void LDY(Byte operand)
        {
            _y = operand;
            SetStatusZN(_y);
        }

        /// <summary>
        /// Implements the ROL (ROtate Left) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Byte ROL(Byte operand)
        {
            var carry = (UInt16)(Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0);
            var result = (Byte)((operand << 1) | carry);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (operand & 0x80) != 0);
            SetStatusZN(result);
            return result;
        }

        /// <summary>
        /// Implements the ROR (ROtate Right) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        private Byte ROR(Byte operand)
        {
            var carry = (Byte)((Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0) << 7);
            var result = (Byte)((operand >> 1) | carry);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (operand & 0x01) != 0);
            SetStatusZN(result);
            return result;
        }

        /// <summary>
        /// Implements the SBC (SuBtract with Carry) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SBC(Byte operand)
        {
            var a = (uint)_acc;
            var b = (uint)operand;

            var carry = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1U : 0U;
            var temp = a - b - 1U + carry;
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V, ((a ^ temp) & (a ^ b) & 0x80) != 0);

            var bcd = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.D);
            if (bcd)
            {
                var lo = (a & 0x0f) - (b & 0x0f) - 1 + carry;
                var hi = (a >> 4) - (b >> 4);

                if ((lo & 0x10) != 0)
                {
                    lo -= 6;
                    hi--;
                }

                if ((hi & 0x10) != 0)
                    hi -= 6;

                _acc = (Byte)((hi << 4) | (lo & 0x0f));
            }
            else
            {
                _acc = (Byte)(temp & 0xFF);
            }

            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, temp < 0x100);
            SetStatusZN(_acc);
        }

        /// <summary>
        /// Implements the TRB (Test and Reset Bits) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Byte TRB(Byte operand)
        {
            var result = (Byte)(~_acc & operand);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z, (_acc & operand) == 0);
            return result;
        }

        /// <summary>
        /// Implements the TSB (Test and Set Bits) instruction.
        /// </summary>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>The instruction's resulting value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Byte TSB(Byte operand)
        {
            var result = (Byte)(_acc | operand);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z, (_acc & operand) == 0);
            return result;
        }

        /// <summary>
        /// Allocates cycles to the executing instruction, truncating it if it would pass outside of the total number
        /// of allowed cycles. Truncated instructions will be finished the next time the CPU is clocked.
        /// </summary>
        /// <param name="remaining">The total number of remaining cycles available for allocation.</param>
        /// <param name="required">The total number of cycles required by the instruction.</param>
        /// <param name="allocated">The number of cycles that were actually allocated to the instruction.</param>
        /// <returns><see langword="true"/> if the instruction was truncated; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Boolean CheckTruncatedInstruction(UInt32 remaining, UInt32 required, out UInt32 allocated)
        {
            required = _truncationCycles > 0 ? _truncationCycles : required;
            if (remaining < required)
            {
                Bus.OpenBusValue = _truncationOpenBus;
                _truncationCycles = required - remaining;
                _pc = _truncationAddr;
                allocated = remaining;
                return true;
            }
            _truncationCycles = 0;
            allocated = required;
            return false;
        }

        /// <summary>
        /// Handles an IRQ or NMI.
        /// </summary>
        /// <param name="addrVector"/>The address of the interrupt vector.</param>
        /// <param name="remainingClockCycles">The number of clock cycles that remain to be allocated to instructions.</param>
        /// <returns>The number of cycles that were required to execute the interrupt.</returns>
        private UInt32 HandleInterrupt(UInt16 addrVector, ref UInt32 remainingClockCycles)
        {
            if (CheckTruncatedInstruction(remainingClockCycles, 7, out var irqCycles))
            {
                remainingClockCycles = 0;
                return irqCycles;
            }

            StackPush16(_pc);
            StackPush(_status | (Byte)Hb8bCpuStatusFlags.U);

            Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.D);
            Bitwise.SetMask(ref _status, (Byte)Hb8bCpuStatusFlags.I);

            _pc = Bus.Read16(addrVector);

            remainingClockCycles -= irqCycles;
            return irqCycles;
        }

        /// <summary>
        /// Clocks the other peripherals alongside instruction execution.
        /// </summary>
        /// <param name="clockCycles">The number of cycles that were allocated to instructions.</param>
        private void ClockPeripherals(UInt32 clockCycles)
        {
            // TODO
        }

        /// <summary>
        /// Clocks the other peripherals until an interrupt is raised or the specified number of
        /// clock cycles has elapsed.
        /// </summary>
        /// <param name="remainingClockCycles">The number of remaining clock cycles that are available for execution.</param>
        private void ClockPeripheralsUntilInterrupt(ref UInt32 remainingClockCycles)
        {
            // TODO
        }

        /// <summary>
        /// Pushes an 8-bit value onto the processor stack.
        /// </summary>
        /// <param name="value">The value to push.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StackPush(Int32 value)
        {
            Bus.Write((UInt16)(0x0100 + _stkp), (Byte)(value & 0xFF));
            _stkp--;
        }

        /// <summary>
        /// Pushes a 16-bit value onto the processor stack.
        /// </summary>
        /// <param name="value">The value to push.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StackPush16(Int32 value)
        {
            Bus.Write((UInt16)(0x0100 + _stkp), (Byte)((value >> 8) & 0xFF));
            _stkp--;
            Bus.Write((UInt16)(0x0100 + _stkp), (Byte)(value & 0xFF));
            _stkp--;
        }

        /// <summary>
        /// Pops an 8-bit value off of the processor stack.
        /// </summary>
        /// <param name="value">The value that was popped off of the stack.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StackPop(out Byte value)
        {
            _stkp++;
            value = Bus.Read((UInt16)(0x0100 + _stkp));
        }

        /// <summary>
        /// Pops an 16-bit value off of the processor stack.
        /// </summary>
        /// <param name="value">The value that was popped off of the stack.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StackPop16(out UInt16 value)
        {
            _stkp++;
            var valueLo = Bus.Read((UInt16)(0x0100 + _stkp));            
            _stkp++;
            var valueHi = Bus.Read((UInt16)(0x0100 + _stkp));
            value = (UInt16)((valueHi << 8) | valueLo);
        }

        /// <summary>
        /// Sets the Z and N flags based on the specified value.
        /// </summary>
        /// <param name="value">The value from which to compute flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStatusV(Byte m, Byte n, Byte r)
        {
            var v = ((m ^ r) & (n ^ r) & 0x80) != 0;
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V, v);
        }

        /// <summary>
        /// Sets the Z flag based on the specified value.
        /// </summary>
        /// <param name="value">The value from which to compute flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStatusZ(Byte value)
        {
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z, (value & 0xFF) == 0);
        }

        /// <summary>
        /// Sets the Z and N flags based on the specified value.
        /// </summary>
        /// <param name="value">The value from which to compute flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStatusZN(Byte value)
        {
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z, (value & 0xFF) == 0);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.N, (value & 0x80) != 0);
        }

        /// <summary>
        /// Sets the C, Z and N flags based on the specified value.
        /// </summary>
        /// <param name="value">The value from which to compute flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStatusCZN(UInt16 value)
        {
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, value >= 256);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z, (value & 0xFF) == 0);
            Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.N, (value & 0x80) != 0);
        }
    }
}