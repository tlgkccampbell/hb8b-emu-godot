using System;

namespace Hb8b.Emulation
{
    /// <summary>
    /// Contains methods for disassembling machine code.
    /// </summary>
    public class Disassembler
    {
        private UInt16 _address;

        /// <summary>
        /// Initializes a new instance of the <see cref="Disassembler"/> class.
        /// </summary>
        /// <param name="bus">The system bus from which to read data.</param>
        public Disassembler(Hb8bSystemBus bus)
        {
            this.Bus = bus;
        }

        /// <summary>
        /// Disassembles the next instruction at <see cref="Address"/> and advances the address value.
        /// </summary>
        /// <returns>The disassembled instruction text.</returns>
        public String Disassemble()
        {
            var opcode = Bus.Read(_address++);
            switch (opcode)
            {
                case 0x00: return IMP("BRK");
                case 0x01: return IZX("ORA");
                case 0x02: return UND(2);
                case 0x03: return UND(1);
                case 0x04: return ZP0("TSB");
                case 0x05: return ZP0("ORA");
                case 0x06: return ZP0("ASL");
                case 0x07: return ZP0("RMB0");
                case 0x08: return IMP("PHP");
                case 0x09: return IMM("ORA");
                case 0x0A: return IMP("ASL");
                case 0x0B: return UND(1);
                case 0x0C: return ABS("TSB");
                case 0x0D: return ABS("ORA");
                case 0x0E: return ABS("ASL");
                case 0x0F: return ZPREL("BBR0");
                case 0x10: return REL("BPL");
                case 0x11: return IZY("ORA");
                case 0x12: return IZP("ORA");
                case 0x13: return UND(1);
                case 0x14: return ZP0("TRB");
                case 0x15: return ZPX("ORA");
                case 0x16: return ZPX("ASL");
                case 0x17: return ZP0("RMB1");
                case 0x18: return IMP("CLC");
                case 0x19: return ABY("ORA");
                case 0x1A: return IMP("INC");
                case 0x1B: return UND(1);
                case 0x1C: return ABS("TRB");
                case 0x1D: return ABX("ORA");
                case 0x1E: return ABX("ASL");
                case 0x1F: return ZPREL("BBR1");
                case 0x20: return ABS("JSR");
                case 0x21: return IZX("AND");
                case 0x22: return UND(2);
                case 0x23: return UND(1);
                case 0x24: return ZP0("BIT");
                case 0x25: return ZP0("AND");
                case 0x26: return ZP0("ROL");
                case 0x27: return ZP0("RMB2");
                case 0x28: return IMP("PLP");
                case 0x29: return IMM("AND");
                case 0x2A: return IMP("ROL");
                case 0x2B: return UND(1);
                case 0x2C: return ABS("BIT");
                case 0x2D: return ABS("AND");
                case 0x2E: return ABS("ROL");
                case 0x2F: return ZPREL("BBR2");
                case 0x30: return REL("BMI");
                case 0x31: return IZY("AND");
                case 0x32: return IZP("AND");
                case 0x33: return UND(1);
                case 0x34: return ZPX("BIT");
                case 0x35: return ZPX("AND");
                case 0x36: return ZPX("ROL");
                case 0x37: return ZP0("RMB3");
                case 0x38: return IMP("SEC");
                case 0x39: return ABY("AND");
                case 0x3A: return IMP("DEC");
                case 0x3B: return UND(1);
                case 0x3C: return ABX("BIT");
                case 0x3D: return ABX("AND");
                case 0x3E: return ABX("ROL");
                case 0x3F: return ZPREL("BBR3");
                case 0x40: return IMP("RTI");
                case 0x41: return IZX("EOR");
                case 0x42: return UND(2);
                case 0x43: return UND(1);
                case 0x44: return UND(2);
                case 0x45: return ZP0("EOR");
                case 0x46: return ZP0("LSR");
                case 0x47: return ZP0("RMB4");
                case 0x48: return IMP("PHA");
                case 0x49: return IMM("EOR");
                case 0x4A: return IMP("LSR");
                case 0x4B: return UND(1);
                case 0x4C: return ABS("JMP");
                case 0x4D: return ABS("EOR");
                case 0x4E: return ABS("LSR");
                case 0x4F: return ZPREL("BBR4");
                case 0x50: return REL("BVC");
                case 0x51: return IZY("EOR");
                case 0x52: return IZP("EOR");
                case 0x53: return UND(1);
                case 0x54: return UND(2);
                case 0x55: return ZPX("EOR");
                case 0x56: return ZPX("LSR");
                case 0x57: return ZP0("RMB5");
                case 0x58: return IMP("CLI");
                case 0x59: return ABY("EOR");
                case 0x5A: return IMP("PHY");
                case 0x5B: return UND(1);
                case 0x5C: return UND(3);
                case 0x5D: return ABX("EOR");
                case 0x5E: return ABX("LSR");
                case 0x5F: return ZPREL("BBR5");
                case 0x60: return IMP("RTS");
                case 0x61: return IZX("ADC");
                case 0x62: return UND(2);
                case 0x63: return UND(1);
                case 0x64: return ZP0("STZ");
                case 0x65: return ZP0("ADC");
                case 0x66: return ZP0("ROR");
                case 0x67: return ZP0("RMB6");
                case 0x68: return IMP("PLA");
                case 0x69: return IMM("ADC");
                case 0x6A: return IMP("ROR");
                case 0x6B: return UND(1);
                case 0x6C: return IND("JMP");
                case 0x6D: return ABS("ADC");
                case 0x6E: return ABS("ROR");
                case 0x6F: return ZPREL("BBR6");
                case 0x70: return REL("BVS");
                case 0x71: return IZY("ADC");
                case 0x72: return IZP("ADC");
                case 0x73: return UND(1);
                case 0x74: return ZPX("STZ");
                case 0x75: return ZPX("ADC");
                case 0x76: return ZPX("ROR");
                case 0x77: return ZP0("RMB7");
                case 0x78: return IMP("SEI");
                case 0x79: return ABY("ADC");
                case 0x7A: return IMP("PLY");
                case 0x7B: return UND(1);
                case 0x7C: return INDX("JMP");
                case 0x7D: return ABX("ADC");
                case 0x7E: return ABX("ROR");
                case 0x7F: return ZPREL("BBR7");
                case 0x80: return REL("BRA");
                case 0x81: return IZX("STA");
                case 0x82: return UND(2);
                case 0x83: return UND(1);
                case 0x84: return ZP0("STY");
                case 0x85: return ZP0("STA");
                case 0x86: return ZP0("STX");
                case 0x87: return ZP0("SMB0");
                case 0x88: return IMP("DEY");
                case 0x89: return IMM("BIT");
                case 0x8A: return IMP("TXA");
                case 0x8B: return UND(1);
                case 0x8C: return ABS("STY");
                case 0x8D: return ABS("STA");
                case 0x8E: return ABS("STX");
                case 0x8F: return ZPREL("BBS0");
                case 0x90: return REL("BCC");
                case 0x91: return IZY("STA");
                case 0x92: return IZP("STA");
                case 0x93: return UND(1);
                case 0x94: return ZPX("STA");
                case 0x95: return ZPX("STA");
                case 0x96: return ZPY("STX");
                case 0x97: return ZP0("SMB1");
                case 0x98: return IMP("TYA");
                case 0x99: return ABY("STA");
                case 0x9A: return IMP("TXS");
                case 0x9B: return UND(1);
                case 0x9C: return ABX("STZ");
                case 0x9D: return ABX("STA");
                case 0x9E: return ABX("STZ");
                case 0x9F: return ZPREL("BBS1");
                case 0xA0: return IMM("LDY");
                case 0xA1: return IZX("LDA");
                case 0xA2: return IMM("LDX");
                case 0xA3: return UND(1);
                case 0xA4: return ZP0("LDY");
                case 0xA5: return ZP0("LDA");
                case 0xA6: return ZP0("LDX");
                case 0xA7: return ZP0("SMB2");
                case 0xA8: return IMP("TAY");
                case 0xA9: return IMM("LDA");
                case 0xAA: return IMP("TAX");
                case 0xAB: return UND(1);
                case 0xAC: return ABS("LDY");
                case 0xAD: return ABS("LDA");
                case 0xAE: return ABS("LDX");
                case 0xAF: return ZPREL("BBS2");
                case 0xB0: return REL("BCS");
                case 0xB1: return IZY("LDA");
                case 0xB2: return IZP("LDA");
                case 0xB3: return UND(1);
                case 0xB4: return ZPX("LDY");
                case 0xB5: return ZPX("LDA");
                case 0xB6: return ZPY("LDX");
                case 0xB7: return ZP0("SMB3");
                case 0xB8: return IMP("CLV");
                case 0xB9: return ABY("LDA");
                case 0xBA: return IMP("TSX");
                case 0xBB: return UND(1);
                case 0xBC: return ABX("LDY");
                case 0xBD: return ABX("LDA");
                case 0xBE: return ABY("LDX");
                case 0xBF: return ZPREL("BBS3");
                case 0xC0: return IMM("CPY");
                case 0xC1: return IZX("CMP");
                case 0xC2: return UND(2);
                case 0xC3: return UND(1);
                case 0xC4: return ZP0("CPY");
                case 0xC5: return ZP0("CMP");
                case 0xC6: return ZP0("DEC");
                case 0xC7: return ZP0("SMB4");
                case 0xC8: return IMP("INY");
                case 0xC9: return IMM("CMP");
                case 0xCA: return IMP("DEX");
                case 0xCB: return IMP("WAI");
                case 0xCC: return ABS("CPY");
                case 0xCD: return ABS("CMP");
                case 0xCE: return ABS("DEC");
                case 0xCF: return ZPREL("BBS4");
                case 0xD0: return REL("BNE");
                case 0xD1: return IZY("CMP");
                case 0xD2: return IZP("CMP");
                case 0xD3: return UND(1);
                case 0xD4: return UND(2);
                case 0xD5: return ZPX("CMP");
                case 0xD6: return ZPX("DEC");
                case 0xD7: return ZP0("SMB5");
                case 0xD8: return IMP("CLD");
                case 0xD9: return ABY("CMP");
                case 0xDA: return IMP("PHX");
                case 0xDB: return IMP("STP");
                case 0xDC: return UND(3);
                case 0xDD: return ABX("CMP");
                case 0xDE: return ABX("DEC");
                case 0xDF: return ZPREL("BBS5");
                case 0xE0: return IMM("CPX");
                case 0xE1: return IZX("SBC");
                case 0xE2: return UND(2);
                case 0xE3: return UND(1);
                case 0xE4: return ZP0("CPX");
                case 0xE5: return ZP0("SBC");
                case 0xE6: return ZP0("INC");
                case 0xE7: return ZPREL("SMB6");
                case 0xE8: return IMP("INX");
                case 0xE9: return IMM("SBC");
                case 0xEA: return NOP();
                case 0xEB: return UND(1);
                case 0xEC: return ABS("CPX");
                case 0xED: return ABS("SBC");
                case 0xEF: return ZPREL("BBS6");
                case 0xF0: return REL("BEQ");
                case 0xF1: return IZY("SBC");
                case 0xF2: return IZP("SBC");
                case 0xF3: return UND(1);
                case 0xF4: return UND(2);
                case 0xF5: return ZPX("SBC");
                case 0xF6: return ZPX("INC");
                case 0xF7: return ZPREL("SMB7");
                case 0xF8: return IMP("SED");
                case 0xF9: return ABY("SEC");
                case 0xFA: return IMP("PLX");
                case 0xFB: return UND(1);
                case 0xFC: return UND(3);
                case 0xFD: return ABX("SBC");
                case 0xFE: return ABX("INC");
                case 0xFF: return ZPREL("BBS7");
            }

            return $"{M("???")} (${opcode:X2})";
        }

        /// <summary>
        /// Gets the system bus from which the disassembler reads data.
        /// </summary>
        public Hb8bSystemBus Bus { get; }

        /// <summary>
        /// Gets or sets the memory address that will be disassembled next.
        /// </summary>
        public UInt16 Address
        {
            get => _address;
            set => _address = value;
        }

        /// <summary>
        /// Outputs a padded mnemonic.
        /// </summary>
        private static String M(String mnemonic) => mnemonic.PadLeft(4, ' ');

        /// <summary>
        /// Disassembles an IMP addressing mode instruction.
        /// </summary>
        private String IMP(String mnemonic) => M(mnemonic);

        /// <summary>
        /// Disassembles an undefined (NOP) instruction.
        /// </summary>
        private String UND(Int32 bytes)
        {
            _address = (UInt16)(_address + bytes - 1);
            return $"{M("???")} [NOP]";
        }

        /// <summary>
        /// Disassembles a NOP instruction.
        /// </summary>
        private String NOP() => M("NOP");

        /// <summary>
        /// Disassembles an IMM addressing mode instruction.
        /// </summary>
        private String IMM(String mnemonic) => $"{M(mnemonic)} #${Bus.Read(_address++):X2}";

        /// <summary>
        /// Disassembles an ABS addressing mode instruction.
        /// </summary>
        private String ABS(String mnemonic) => $"{M(mnemonic)} ${Bus.Read16(ref _address):X4}";

        /// <summary>
        /// Disassembles an ABX addressing mode instruction.
        /// </summary>
        private String ABX(String mnemonic) => $"{M(mnemonic)} ${Bus.Read16(ref _address):X4}, X";

        /// <summary>
        /// Disassembles an ABY addressing mode instruction.
        /// </summary>
        private String ABY(String mnemonic) => $"{M(mnemonic)} ${Bus.Read16(ref _address):X4}, Y";

        /// <summary>
        /// Disassembles a REL addressing mode instruction.
        /// </summary>
        private String REL(String mnemonic)
        {
            var addrRel = (UInt16)Bus.Read(_address++);
            addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;
            
            var addrAbs = (UInt16)(_address + addrRel);
            return $"{M(mnemonic)} ${(Byte)addrRel:X2} [${addrAbs:X4}]";
        }

        /// <summary>
        /// Disassembles an IND addressing mode instruction.
        /// </summary>
        private String IND(String mnemonic) => $"{M(mnemonic)} (${Bus.Read16(ref _address):X4})";

        /// <summary>
        /// Disassembles an INDX addressing mode instruction.
        /// </summary>
        private String INDX(String mnemonic) => $"{M(mnemonic)} (${Bus.Read16(ref _address):X4}, X)";

        /// <summary>
        /// Disassembles a ZP0 addressing mode instruction.
        /// </summary>
        private String ZP0(String mnemonic) => $"{M(mnemonic)} ${Bus.Read(_address++):X2}";

        /// <summary>
        /// Disassembles an ZPX addressing mode instruction.
        /// </summary>
        private String ZPX(String mnemonic) => $"{M(mnemonic)} ${Bus.Read(_address++):X2}, X";

        /// <summary>
        /// Disassembles an ZPY addressing mode instruction.
        /// </summary>
        private String ZPY(String mnemonic) => $"{M(mnemonic)} ${Bus.Read(_address++):X2}, Y";

        /// <summary>
        /// Disassembles an IZX addressing mode instruction.
        /// </summary>
        private String IZX(String mnemonic) => $"{M(mnemonic)} (${Bus.Read(_address++):X2}, X)";

        /// <summary>
        /// Disassembles an IZY addressing mode instruction.
        /// </summary>
        private String IZY(String mnemonic) => $"{M(mnemonic)} (${Bus.Read(_address++):X2}), Y";

        /// <summary>
        /// Disassembles an IZP addressing mode instruction.
        /// </summary>
        private String IZP(String mnemonic) => $"{M(mnemonic)} (${Bus.Read(_address++):X2})";

        /// <summary>
        /// Disassembles an ZPREL addressing mode instruction.
        /// </summary>
        private String ZPREL(String mnemonic)
        {
            var addrZp = Bus.Read(_address++);
            var addrRel = (UInt16)Bus.Read(_address++);
            addrRel |= ((addrRel & 0x80) != 0) ? (UInt16)0xFF00 : (UInt16)0x0000;

            var addrAbs = (UInt16)(_address + addrRel);
            return $"{M(mnemonic)} ${addrZp:X2}, ${addrRel:X2} [${addrAbs:X4}]";
        }
    }
}