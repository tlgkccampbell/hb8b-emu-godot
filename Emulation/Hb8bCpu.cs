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

                                var brkVectorLo = Bus.Read(VectorAddressIrq);
                                var brkVectorHi = Bus.Read(VectorAddressIrq + 1);
                                _pc = (UInt16)((brkVectorHi << 8) | brkVectorLo);
                            }
                            break;

                        case 0x01: // ORA, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0x05: // ORA, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x06: // ASL, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (UInt16)(data << 1);

                                SetStatusCZN(result);

                                Bus.Write(addrZp, (Byte)result);
                            }
                            break;

                        case 0x07: // RMB0, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 0);

                                Bus.Write(addrZp, data);
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
                                _acc = (Byte)(_acc | data);

                                SetStatusCZN(_acc);
                            }
                            break;

                        case 0x0A: // ASL, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var temp = (UInt16)(data << 1);
                                var result = (Byte)temp;
                                _acc = result;

                                SetStatusCZN(temp);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);

                                Bus.Write(addrAbs, data);
                            }
                            break;

                        case 0x0D: // ORA, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x0E: // ASL, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var temp = (UInt16)(data << 1);
                                var result = (Byte)temp;

                                SetStatusCZN(temp);

                                Bus.Write(addrAbs, result);
                            }
                            break;

                        case 0x0F: // BBR0, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 0);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x10: // BPL, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.N);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x11: // ORA, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x12: // ORA, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var temp = (Byte)(data & _acc);
                                var result = (Byte)(data & ~_acc);

                                SetStatusZ(temp);

                                Bus.Write(addrZp, result);
                            }
                            break;

                        case 0x15: // ORA, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x16: // ASL, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                var temp = (UInt16)(data << 1);
                                var result = (Byte)temp;

                                SetStatusCZN(temp);

                                Bus.Write(addrZp, result);
                            }
                            break;

                        case 0x17: // RMB1, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 1);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x1A: // INC, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _acc++;

                                SetStatusZN(_acc);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var temp = (Byte)(data & _acc);
                                var result = (Byte)(data & ~_acc);

                                SetStatusZ(temp);

                                Bus.Write(addrAbs, result);
                            }
                            break;

                        case 0x1D: // ORA, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                _acc = (Byte)(_acc | data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x1E: // ASL, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 6U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                var result = (UInt16)(data << 1);

                                SetStatusCZN(result);

                                Bus.Write(addrAbsOffset, (Byte)result);
                            }
                            break;

                        case 0x1F: // BBR1, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 1);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x20: // JSR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                StackPush(_pc - 1);
                                _pc = addrAbs;
                            }
                            break;

                        case 0x21: // AND, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);
                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.N, (data & (1 << 7)) != 0);
                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V, (data & (1 << 6)) == 0);
                            }
                            break;

                        case 0x25: // AND, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x26: // ROL, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var carry = (UInt16)(Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0);
                                var result = (UInt16)((data << 1) | carry);

                                SetStatusCZN(result);

                                Bus.Write(addrZp, (Byte)result);
                            }
                            break;

                        case 0x27: // RMB2, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 2);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0x28: // PLP, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                StackPop(out _status);
                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.B);
                                Bitwise.ClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.U);
                            }
                            break;

                        case 0x29: // AND, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x2A: // ROL, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var carry = (UInt16)(Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0);
                                var result = (UInt16)((data << 1) | carry);

                                SetStatusCZN(result);

                                Bus.Write(_acc, (Byte)result);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);
                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.N, (data & (1 << 7)) != 0);
                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V, (data & (1 << 6)) == 0);
                            }
                            break;

                        case 0x2D: // AND, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbsLo = (UInt16)Bus.Read(_pc++);
                                var addrAbsHi = (UInt16)Bus.Read(_pc++);
                                var addrAbs = (UInt16)((addrAbsHi << 8) | addrAbsLo);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x2E: // ROL, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var carry = (UInt16)(Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0);
                                var result = (UInt16)((data << 1) | carry);

                                SetStatusCZN(result);

                                Bus.Write(addrAbs, (Byte)result);
                            }
                            break;

                        case 0x2F: // BBR2, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 2);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x30: // BMI, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.N);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x31: // AND, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x32: // AND, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x33: // ??? (NOP)
                            {
                                
                            }
                            break;

                        case 0x34: // BIT, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);
                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.N, (data & (1 << 7)) != 0);
                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V, (data & (1 << 6)) == 0);
                            }
                            break;

                        case 0x35: // AND, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x36: // ROL, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                var carry = (UInt16)(Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0);
                                var result = (UInt16)((data << 1) | carry);

                                SetStatusCZN(result);

                                Bus.Write(addrZp, (Byte)result);
                            }
                            break;

                        case 0x37: // RMB3, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 3);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x3A: // DEC, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _acc--;

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x3B: // ??? (NOP)
                            {
                                
                            }
                            break;

                        case 0x3C: // BIT, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);
                            }
                            break;

                        case 0x3D: // AND, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                _acc = (Byte)(_acc & data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x3E: // ROL, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                var carry = (UInt16)(Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0);
                                var result = (UInt16)((data << 1) | carry);

                                SetStatusCZN(result);

                                Bus.Write(addrAbsOffset, (Byte)result);
                            }
                            break;

                        case 0x3F: // BBR3, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 3);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x40: // RTI, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                StackPop(out _status);
                                Bitwise.ClrMask(ref _status, (Byte)(Hb8bCpuStatusFlags.B | Hb8bCpuStatusFlags.I));

                                StackPop16(out _pc);
                            }
                            break;

                        case 0x41: // EOR, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x46: // LSR, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (Byte)(data >> 1);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) == 1);
                                SetStatusZN(result);

                                Bus.Write(addrZp, result);
                            }
                            break;

                        case 0x47: // RMB4, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 4);

                                Bus.Write(addrZp, data);
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
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x4A: // LSR, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var result = (Byte)(data >> 1);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) == 1);
                                SetStatusZN(_acc);

                                _acc = result;
                            }
                            break;

                        case 0x4B: // ??? (NOP)
                            {
                                
                            }
                            break;

                        case 0x4C: // JMP, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);

                                _pc = addrAbs;
                            }
                            break;

                        case 0x4D: // EOR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x4E: // LSR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var result = (Byte)(data >> 1);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) == 1);
                                SetStatusZN(result);

                                Bus.Write(addrAbs, result);
                            }
                            break;

                        case 0x4F: // BBR4, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 4);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x50: // BVC, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.V);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x51: // EOR, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x52: // EOR, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
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

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x56: // LSR, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                var result = (Byte)(data >> 1);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) == 1);
                                SetStatusZN(result);

                                Bus.Write(addrZp, result);
                            }
                            break;

                        case 0x57: // RMB5, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 5);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
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
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var data = Bus.Read(addrAbsOffset);
                                _acc = (Byte)(_acc ^ data);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0x5E: // LSR, ABX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var data = Bus.Read(addrAbsOffset);
                                var result = (Byte)(data >> 1);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) == 1);
                                SetStatusZN(result);

                                Bus.Write(addrAbsOffset, result);
                            }
                            break;

                        case 0x5F: // BBR5, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 5);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
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

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                var m = _acc;
                                var n = Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);

                                Bus.Write(addrZp, (Byte)0x00);
                            }
                            break;

                        case 0x65: // ADC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var m = _acc;
                                var n = Bus.Read(addrZp);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0x66: // ROR, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var carry = (Byte)((Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0) << 7);
                                var result = (Byte)((data >> 1) | carry);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) != 0);
                                SetStatusZN(result);

                                Bus.Write(addrZp, result);
                            }
                            break;

                        case 0x67: // RMB6, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 6);

                                Bus.Write(addrZp, data);
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

                                var m = _acc;
                                var n = Bus.Read(_pc++);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0x6A: // ROR, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = _acc;
                                var carry = (Byte)((Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0) << 7);
                                var result = (Byte)((data >> 1) | carry);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) != 0);
                                SetStatusZN(result);

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
                                var addrAbs = Bus.Read(addrInd);

                                _pc = addrAbs;
                            }
                            break;

                        case 0x6D: // ADC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var m = _acc;
                                var n = Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0x6E: // ROR, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var carry = (Byte)((Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0) << 7);
                                var result = (Byte)((data >> 1) | carry);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) != 0);
                                SetStatusZN(result);

                                Bus.Write(addrAbs, result);
                            }
                            break;

                        case 0x6F: // BBR6, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 6);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x70: // BVS, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.V);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x71: // ADC, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0x72: // ADC, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                var m = _acc;
                                var n = Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
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

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);

                                Bus.Write(addrZp, (Byte)0x00);
                            }
                            break;

                        case 0x75: // ADC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var m = _acc;
                                var n = Bus.Read(addrZp);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0x76: // ROR, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                var carry = (Byte)((Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0) << 7);
                                var result = (Byte)((data >> 1) | carry);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) != 0);
                                SetStatusZN(result);

                                Bus.Write(addrZp, result);
                            }
                            break;

                        case 0x77: // RMB7, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Clr(ref data, 7);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var data = Bus.Read(addrAbsOffset);

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = data;
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = Bus.Read(addrAbsOffset);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0x7E: // ROR, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 6U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                var carry = (Byte)((Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C) ? 1 : 0) << 7);
                                var result = (Byte)((data >> 1) | carry);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, (data & 0x01) != 0);
                                SetStatusZN(result);

                                Bus.Write(addrAbsOffset, result);
                            }
                            break;

                        case 0x7F: // BBR7, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClr(ref dataTestZp, 7);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x80: // BRA, REL
                            {
                                var extraCycles = 1U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
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

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                Bus.Write(addrAbs, _acc);
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

                                var addrZp = Bus.Read(_pc++);

                                Bus.Write(addrZp, _y);
                            }
                            break;

                        case 0x85: // STA, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                Bus.Write(addrZp, _acc);
                            }
                            break;

                        case 0x86: // STX, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                Bus.Write(addrZp, _x);
                            }
                            break;

                        case 0x87: // SMB0, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 0);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0x88: // DEY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _y--;

                                SetStatusZN(_y);
                            }
                            break;

                        case 0x89: // BIT, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                var result = (Byte)(_acc & data);

                                SetStatusZ(result);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                Bus.Write(addrAbs, _y);
                            }
                            break;

                        case 0x8D: // STA, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                Bus.Write(addrAbs, _acc);
                            }
                            break;

                        case 0x8E: // STX, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                Bus.Write(addrAbs, _x);
                            }
                            break;

                        case 0x8F: // BBS0, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 0);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0x90: // BCC, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0x91: // STA, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 6U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addrAbs, _acc);
                            }
                            break;

                        case 0x92: // STA, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                Bus.Write(addrAbs, _acc);
                            }
                            break;

                        case 0x93: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0x94: // STY (ZPX)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                Bus.Write(addrZp, (Byte)0x00);
                            }
                            break;

                        case 0x95: // STA (ZPX)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                Bus.Write(addrZp, _acc);
                            }
                            break;

                        case 0x96: // STX (ZPY)
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _y);

                                Bus.Write(addrZp, _x);
                            }
                            break;

                        case 0x97: // SMB1, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 1);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addrAbsOffset, _acc);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                Bus.Write(addrAbs, (Byte)0x00);
                            }
                            break;

                        case 0x9D: // STA, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addrAbsOffset, _acc);
                            }
                            break;

                        case 0x9E: // STZ, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                Bus.Write(addrAbsOffset, (Byte)0x00);
                            }
                            break;

                        case 0x9F: // BBS1, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 1);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0xA0: // LDY, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _y = Bus.Read(_pc++);

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xA1: // LDA, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                _acc = Bus.Read(addrAbs);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xA2: // LDX, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x = Bus.Read(_pc++);

                                SetStatusZN(_x);
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

                                var addrZp = Bus.Read(_pc++);

                                _y = Bus.Read(addrZp);

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xA5: // LDA, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                _acc = Bus.Read(addrZp);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xA6: // LDX, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                _x = Bus.Read(addrZp);

                                SetStatusZN(_x);
                            }
                            break;

                        case 0xA7: // SMB2, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 2);

                                Bus.Write(addrZp, data);
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

                                _acc = Bus.Read(_pc++);

                                SetStatusZN(_acc);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                _y = Bus.Read(addrAbs);

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xAD: // LDA, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                _acc = Bus.Read(addrAbs);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xAE: // LDX, ABS 
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                _x = Bus.Read(addrAbs);

                                SetStatusZN(_x);
                            }
                            break;

                        case 0xAF: // BBS2, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 2);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0xB0: // BCS, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.C);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0xB1: // LDA, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                _acc = Bus.Read(addrAbs);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xB2: // LDA, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                _acc = Bus.Read(addrAbs);

                                SetStatusZN(_acc);
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

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                _y = Bus.Read(addrZp);

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xB5: // LDA, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                _acc = Bus.Read(addrZp);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xB6: // LDX, ZPY
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _y);

                                _x = Bus.Read(addrZp);

                                SetStatusZN(_x);
                            }
                            break;

                        case 0xB7: // SMB3, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 3);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                _acc = Bus.Read(addrAbsOffset);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xBA: // TSX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x = _stkp;
                            }
                            break;

                        case 0xBB: // ??? (NOP)
                            {
                                // 1-cycle NOP
                            }
                            break;

                        case 0xBC: // LDY, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                _y = Bus.Read(addrAbsOffset);

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xBD: // LDA, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                _acc = Bus.Read(addrAbsOffset);

                                SetStatusZN(_acc);
                            }
                            break;

                        case 0xBE: // LDX, ABY
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                _y = Bus.Read(addrAbsOffset);

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xBF: // BBS3, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 3);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0xC0: // CPY, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                var result = (UInt16)(_y - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _y >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xC1: // CMP, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (UInt16)(_y - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _y >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xC5: // CMP, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xC6: // DEC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                data--;

                                SetStatusZN(data);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0xC7: // SMB4, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 4);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0xC8: // INY, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _y++;

                                SetStatusZN(_y);
                            }
                            break;

                        case 0xC9: // CMP, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xCA: // DEX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x--;

                                SetStatusZN(_x);
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var result = (UInt16)(_y - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _y >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xCD: // CMP, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xCE: // DEC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = (Byte)(Bus.Read(addrAbs) - 1);

                                SetStatusZN(data);

                                Bus.Write(addrAbs, data);
                            }
                            break;

                        case 0xCF: // BBS4, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 4);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0xD0: // BNE, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0xD1: // CMP, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbs);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xD2: // CMP, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                var data = Bus.Read(addrAbs);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
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

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xD6: // DEC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = (Byte)(Bus.Read(addrZp) - 1);

                                SetStatusZN(data);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0xD7: // SMB5, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 5);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                var result = (UInt16)(_acc - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _acc >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xDE: // DEC, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 7U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                data--;

                                SetStatusZN(data);

                                Bus.Write(addrAbsOffset, data);
                            }
                            break;

                        case 0xDF: // BBS5, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 5);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0xE0: // CPX, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var data = Bus.Read(_pc++);
                                var result = (UInt16)(_x - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _x >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xE1: // SBC, IZX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 6, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++) + _x));
                                var addrAbs = Bus.Read16(addrIdx);

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
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

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                var result = (UInt16)(_x - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _x >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xE5: // SBC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 3, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrZp);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0xE6: // INC, ZP0
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = (Byte)(Bus.Read(addrZp) + 1);

                                SetStatusZN(data);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0xE7: // SMB6, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 6);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0xE8: // INX, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                _x++;

                                SetStatusZN(_x);
                            }
                            break;

                        case 0xE9: // SBC, IMM
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = (Byte)~Bus.Read(_pc++);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0xEA: // NOP, IMP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 2, out instrCycles))
                                    break;
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

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var data = Bus.Read(addrAbs);
                                var result = (UInt16)(_x - data);

                                Bitwise.SetOrClrMask(ref _status, (Byte)Hb8bCpuStatusFlags.C, _x >= data);
                                SetStatusZN((Byte)result);
                            }
                            break;

                        case 0xED: // SBC, ABS
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 4, out instrCycles))
                                    break;

                                var addrAbs = Bus.Read16(_pc);
                                _pc += 2;

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0xEF: // BBS6, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 6);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
                            }
                            break;

                        case 0xF0: // BEQ, REL
                            {
                                var extraCycles = 0U;

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var addrJmp = (UInt16)(_pc + addrRel);
                                var addrJmpIsInDifferentPage = (addrJmp & 0xFF00) != (_pc & 0xFF00);
                                if (addrJmpIsInDifferentPage)
                                    extraCycles++;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSetMask(ref _status, (Byte)Hb8bCpuStatusFlags.Z);
                                if (branchIsTaken)
                                {
                                    extraCycles++;
                                    branchTarget = addrJmp;
                                }

                                if (CheckTruncatedInstruction(remainingClockCycles, 2U + extraCycles, out instrCycles))
                                    break;

                                _pc = branchTarget;
                            }
                            break;

                        case 0xF1: // SBC, IZY
                            {
                                var addrIdx = (UInt16)(Bus.Read16ZeroPage((Byte)(Bus.Read(_pc++))) + _y);
                                var addrAbs = Bus.Read16(addrIdx);

                                var extraCycles = (addrAbs & 0xFF00) != (addrIdx & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 5U + extraCycles, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0xF2: // SBC, IZP
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrIdx = Bus.Read16ZeroPage(Bus.Read(_pc++));
                                var addrAbs = Bus.Read16(addrIdx);

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrAbs);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
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

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrZp);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0xF6: // INC, ZPX
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = (Byte)(Bus.Read(_pc++) + _x);

                                var data = Bus.Read(addrZp);
                                data++;

                                SetStatusZN(data);

                                Bus.Write(addrZp, data);
                            }
                            break;

                        case 0xF7: // SMB7, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrZp = Bus.Read(_pc++);

                                var data = Bus.Read(addrZp);
                                Bitwise.Set(ref data, 7);

                                Bus.Write(addrZp, data);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _y);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrAbsOffset);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
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
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 4U + extraCycles, out instrCycles))
                                    break;

                                var m = _acc;
                                var n = (Byte)~Bus.Read(addrAbsOffset);
                                var r = (UInt16)(m + n + (_status & 0x1));
                                _acc = (Byte)r;

                                SetStatusV(m, n, _acc);
                                SetStatusCZN(r);
                            }
                            break;

                        case 0xFE: // INC, ABX
                            {
                                var addrAbs = Bus.Read16(_pc);
                                var addrAbsOffset = (UInt16)(addrAbs + _x);
                                _pc += 2;

                                var extraCycles = (addrAbs & 0xFF00) != (addrAbsOffset & 0xFF00) ? 1U : 0U;

                                if (CheckTruncatedInstruction(remainingClockCycles, 7U + extraCycles, out instrCycles))
                                    break;

                                var data = Bus.Read(addrAbsOffset);
                                data++;

                                SetStatusZN(data);

                                Bus.Write(addrAbsOffset, data);
                            }
                            break;

                        case 0xFF: // BBS7, ZPREL
                            {
                                if (CheckTruncatedInstruction(remainingClockCycles, 5, out instrCycles))
                                    break;

                                var addrTestZp = (UInt16)Bus.Read(_pc++);
                                var dataTestZp = Bus.Read(addrTestZp);

                                var addrRel = (UInt16)Bus.Read(_pc++);
                                addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

                                var branchTarget = _pc;
                                var branchIsTaken = Bitwise.IsSet(ref dataTestZp, 7);
                                if (branchIsTaken)
                                    branchTarget = (UInt16)(_pc + addrRel);

                                _pc = branchTarget;
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

            return cycles;
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
            value = Bus.Read((Byte)(0x0100 + _stkp));
        }

        /// <summary>
        /// Pops an 16-bit value off of the processor stack.
        /// </summary>
        /// <param name="value">The value that was popped off of the stack.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StackPop16(out UInt16 value)
        {
            _stkp++;
            var valueLo = Bus.Read((Byte)(0x0100 + _stkp));            
            _stkp++;
            var valueHi = Bus.Read((Byte)(0x0100 + _stkp));
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