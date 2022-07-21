using System;
using System.Linq;

public partial class Hb8bCpu
{
    private Hb8bInstructionMetadata[] InitializeInstructionSet()
    {
        var isa = new Hb8bInstructionMetadata[256];

        ref Hb8bInstructionMetadata Opcode(Byte opcode) 
        {
            if (isa[opcode] != null)
                throw new InvalidOperationException($"Opcode {opcode:X2} has already been specified.");

            return ref isa[opcode]; 
        }

        // Unused
        Opcode(0x02) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);
        Opcode(0x22) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);
        Opcode(0x42) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);
        Opcode(0x62) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);
        Opcode(0x82) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);
        Opcode(0xC2) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);
        Opcode(0xE2) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 2);

        Opcode(0x03) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x13) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x23) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x33) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x43) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x53) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x63) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x73) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x83) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x93) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xA3) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xB3) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xC3) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xD3) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xE3) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xF3) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);

        Opcode(0x44) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 3);
        Opcode(0x54) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 4);
        Opcode(0xD4) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 4);
        Opcode(0xF4) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(2), 4);

        Opcode(0x0B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x1B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x2B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x3B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x4B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x5B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x6B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x7B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x8B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0x9B) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xAB) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xBB) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xEB) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);
        Opcode(0xFB) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(1), 1);

        Opcode(0x5C) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(3), 8);
        Opcode(0xDC) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(3), 4);
        Opcode(0xFC) = new Hb8bInstructionMetadata("???", AddressingMode.IMP, () => NOP(3), 4);

        // ADC
        Opcode(0x69) = new Hb8bInstructionMetadata("ADC", AddressingMode.IMM, () => ADC(IMM), 2);
        Opcode(0x65) = new Hb8bInstructionMetadata("ADC", AddressingMode.ZP0, () => ADC(ZP0), 3);
        Opcode(0x75) = new Hb8bInstructionMetadata("ADC", AddressingMode.ZPX, () => ADC(ZPX), 4);
        Opcode(0x6D) = new Hb8bInstructionMetadata("ADC", AddressingMode.ABS, () => ADC(ABS), 4);
        Opcode(0x7D) = new Hb8bInstructionMetadata("ADC", AddressingMode.ABX, () => ADC(ABX), 4);
        Opcode(0x79) = new Hb8bInstructionMetadata("ADC", AddressingMode.ABY, () => ADC(ABY), 4);
        Opcode(0x61) = new Hb8bInstructionMetadata("ADC", AddressingMode.IZX, () => ADC(IZX), 6);
        Opcode(0x71) = new Hb8bInstructionMetadata("ADC", AddressingMode.IZY, () => ADC(IZY), 5);
        Opcode(0x72) = new Hb8bInstructionMetadata("ADC", AddressingMode.IZP, () => ADC(IZP), 5);

        // AND
        Opcode(0x29) = new Hb8bInstructionMetadata("AND", AddressingMode.IMM, () => AND(IMM), 2);
        Opcode(0x25) = new Hb8bInstructionMetadata("AND", AddressingMode.ZP0, () => AND(ZP0), 3);
        Opcode(0x35) = new Hb8bInstructionMetadata("AND", AddressingMode.ZPX, () => AND(ZPX), 4);
        Opcode(0x2D) = new Hb8bInstructionMetadata("AND", AddressingMode.ABS, () => AND(ABS), 4);
        Opcode(0x3D) = new Hb8bInstructionMetadata("AND", AddressingMode.ABX, () => AND(ABX), 4);
        Opcode(0x39) = new Hb8bInstructionMetadata("AND", AddressingMode.ABY, () => AND(ABY), 4);
        Opcode(0x21) = new Hb8bInstructionMetadata("AND", AddressingMode.IZX, () => AND(IZX), 6);
        Opcode(0x31) = new Hb8bInstructionMetadata("AND", AddressingMode.IZY, () => AND(IZY), 5);
        Opcode(0x32) = new Hb8bInstructionMetadata("AND", AddressingMode.IZP, () => AND(IZP), 5);

        // ASL
        Opcode(0x0A) = new Hb8bInstructionMetadata("ASL", AddressingMode.IMP, () => ASL(IMP), 2);
        Opcode(0x06) = new Hb8bInstructionMetadata("ASL", AddressingMode.ZP0, () => ASL(ZP0), 5);
        Opcode(0x16) = new Hb8bInstructionMetadata("ASL", AddressingMode.ZPX, () => ASL(ZPX), 6);
        Opcode(0x0E) = new Hb8bInstructionMetadata("ASL", AddressingMode.ABS, () => ASL(ABS), 6);
        Opcode(0x1E) = new Hb8bInstructionMetadata("ASL", AddressingMode.ABX, () => ASL(ABX), 6);

        // BBR
        Opcode(0x0F) = new Hb8bInstructionMetadata("BBR0", AddressingMode.ZPREL, () => BBR(0), 5);
        Opcode(0x1F) = new Hb8bInstructionMetadata("BBR1", AddressingMode.ZPREL, () => BBR(1), 5);
        Opcode(0x2F) = new Hb8bInstructionMetadata("BBR2", AddressingMode.ZPREL, () => BBR(2), 5);
        Opcode(0x3F) = new Hb8bInstructionMetadata("BBR3", AddressingMode.ZPREL, () => BBR(3), 5);
        Opcode(0x4F) = new Hb8bInstructionMetadata("BBR4", AddressingMode.ZPREL, () => BBR(4), 5);
        Opcode(0x5F) = new Hb8bInstructionMetadata("BBR5", AddressingMode.ZPREL, () => BBR(5), 5);
        Opcode(0x6F) = new Hb8bInstructionMetadata("BBR6", AddressingMode.ZPREL, () => BBR(6), 5);
        Opcode(0x7F) = new Hb8bInstructionMetadata("BBR7", AddressingMode.ZPREL, () => BBR(7), 5);

        // BBS
        Opcode(0x8F) = new Hb8bInstructionMetadata("BBS0", AddressingMode.ZPREL, () => BBS(0), 5);
        Opcode(0x9F) = new Hb8bInstructionMetadata("BBS1", AddressingMode.ZPREL, () => BBS(1), 5);
        Opcode(0xAF) = new Hb8bInstructionMetadata("BBS2", AddressingMode.ZPREL, () => BBS(2), 5);
        Opcode(0xBF) = new Hb8bInstructionMetadata("BBS3", AddressingMode.ZPREL, () => BBS(3), 5);
        Opcode(0xCF) = new Hb8bInstructionMetadata("BBS4", AddressingMode.ZPREL, () => BBS(4), 5);
        Opcode(0xDF) = new Hb8bInstructionMetadata("BBS5", AddressingMode.ZPREL, () => BBS(5), 5);
        Opcode(0xEF) = new Hb8bInstructionMetadata("BBS6", AddressingMode.ZPREL, () => BBS(6), 5);
        Opcode(0xFF) = new Hb8bInstructionMetadata("BBS7", AddressingMode.ZPREL, () => BBS(7), 5);

        // BCC
        Opcode(0x90) = new Hb8bInstructionMetadata("BCC", AddressingMode.REL, BCC, 2);

        // BCS
        Opcode(0xB0) = new Hb8bInstructionMetadata("BCS", AddressingMode.REL, BCS, 2);

        // BEQ
        Opcode(0xF0) = new Hb8bInstructionMetadata("BEQ", AddressingMode.REL, BEQ, 2);

        // BIT
        Opcode(0x24) = new Hb8bInstructionMetadata("BIT", AddressingMode.ZP0, () => BIT(ZP0), 3);
        Opcode(0x2C) = new Hb8bInstructionMetadata("BIT", AddressingMode.ABS, () => BIT(ABS), 4);
        Opcode(0x89) = new Hb8bInstructionMetadata("BIT", AddressingMode.IMM, () => BIT(IMM), 2);
        Opcode(0x34) = new Hb8bInstructionMetadata("BIT", AddressingMode.ZPX, () => BIT(ZPX), 4);
        Opcode(0x3C) = new Hb8bInstructionMetadata("BIT", AddressingMode.ABX, () => BIT(ABX), 4);

        // BMI
        Opcode(0x30) = new Hb8bInstructionMetadata("BMI", AddressingMode.REL, BMI, 2);

        // BNE
        Opcode(0xD0) = new Hb8bInstructionMetadata("BNE", AddressingMode.REL, BNE, 2);

        // BPL
        Opcode(0x10) = new Hb8bInstructionMetadata("BPL", AddressingMode.REL, BPL, 2);

        // BRA
        Opcode(0x80) = new Hb8bInstructionMetadata("BRA", AddressingMode.REL, BRA, 3);

        // BRK
        Opcode(0x00) = new Hb8bInstructionMetadata("BRK", AddressingMode.IMP, BRK, 7);

        // BVC
        Opcode(0x50) = new Hb8bInstructionMetadata("BVC", AddressingMode.REL, BVC, 2);

        // BVS
        Opcode(0x70) = new Hb8bInstructionMetadata("BVS", AddressingMode.REL, BVS, 2);

        // CLC
        Opcode(0x18) = new Hb8bInstructionMetadata("CLC", AddressingMode.IMP, CLC, 2);

        // CLD
        Opcode(0xD8) = new Hb8bInstructionMetadata("CLD", AddressingMode.IMP, CLD, 2);

        // CLI
        Opcode(0x58) = new Hb8bInstructionMetadata("CLI", AddressingMode.IMP, CLI, 2);

        // CLV
        Opcode(0xB8) = new Hb8bInstructionMetadata("CLV", AddressingMode.IMP, CLV, 2);

        // CMP
        Opcode(0xC9) = new Hb8bInstructionMetadata("CMP", AddressingMode.IMM, () => CMP(IMM), 2);
        Opcode(0xC5) = new Hb8bInstructionMetadata("CMP", AddressingMode.ZP0, () => CMP(ZP0), 3);
        Opcode(0xD5) = new Hb8bInstructionMetadata("CMP", AddressingMode.ZPX, () => CMP(ZPX), 4);
        Opcode(0xCD) = new Hb8bInstructionMetadata("CMP", AddressingMode.ABS, () => CMP(ABS), 4);
        Opcode(0xDD) = new Hb8bInstructionMetadata("CMP", AddressingMode.ABX, () => CMP(ABX), 4);
        Opcode(0xD9) = new Hb8bInstructionMetadata("CMP", AddressingMode.ABY, () => CMP(ABY), 4);
        Opcode(0xC1) = new Hb8bInstructionMetadata("CMP", AddressingMode.IZX, () => CMP(IZX), 6);
        Opcode(0xD1) = new Hb8bInstructionMetadata("CMP", AddressingMode.IZY, () => CMP(IZY), 5);
        Opcode(0xD2) = new Hb8bInstructionMetadata("CMP", AddressingMode.IZP, () => CMP(IZP), 5);

        // CPX
        Opcode(0xE0) = new Hb8bInstructionMetadata("CPX", AddressingMode.IMM, () => CPX(IMM), 2);
        Opcode(0xE4) = new Hb8bInstructionMetadata("CPX", AddressingMode.ZP0, () => CPX(ZP0), 3);
        Opcode(0xEC) = new Hb8bInstructionMetadata("CPX", AddressingMode.ABS, () => CPX(ABS), 4);

        // CPY
        Opcode(0xC0) = new Hb8bInstructionMetadata("CPY", AddressingMode.IMM, () => CPY(IMM), 2);
        Opcode(0xC4) = new Hb8bInstructionMetadata("CPY", AddressingMode.ZP0, () => CPY(ZP0), 3);
        Opcode(0xCC) = new Hb8bInstructionMetadata("CPY", AddressingMode.ABS, () => CPY(ABS), 4);

        // DEC
        Opcode(0xC6) = new Hb8bInstructionMetadata("DEC", AddressingMode.ZP0, () => DEC(ZP0), 5);
        Opcode(0xD6) = new Hb8bInstructionMetadata("DEC", AddressingMode.ZPX, () => DEC(ZPX), 6);
        Opcode(0xCE) = new Hb8bInstructionMetadata("DEC", AddressingMode.ABS, () => DEC(ABS), 6);
        Opcode(0xDE) = new Hb8bInstructionMetadata("DEC", AddressingMode.ABX, () => DEC(ABX), 7);
        Opcode(0x3A) = new Hb8bInstructionMetadata("DEC", AddressingMode.IMP, () => DEC(IMP), 2);

        // DEX
        Opcode(0xCA) = new Hb8bInstructionMetadata("DEX", AddressingMode.IMP, DEX, 2);

        // DEY
        Opcode(0x88) = new Hb8bInstructionMetadata("DEY", AddressingMode.IMP, DEY, 2);

        // EOR
        Opcode(0x49) = new Hb8bInstructionMetadata("EOR", AddressingMode.IMM, () => EOR(IMM), 2);
        Opcode(0x45) = new Hb8bInstructionMetadata("EOR", AddressingMode.ZP0, () => EOR(ZP0), 3);
        Opcode(0x55) = new Hb8bInstructionMetadata("EOR", AddressingMode.ZPX, () => EOR(ZPX), 4);
        Opcode(0x4D) = new Hb8bInstructionMetadata("EOR", AddressingMode.ABS, () => EOR(ABS), 4);
        Opcode(0x5D) = new Hb8bInstructionMetadata("EOR", AddressingMode.ABX, () => EOR(ABX), 4);
        Opcode(0x59) = new Hb8bInstructionMetadata("EOR", AddressingMode.ABY, () => EOR(ABY), 4);
        Opcode(0x41) = new Hb8bInstructionMetadata("EOR", AddressingMode.IZX, () => EOR(IZX), 6);
        Opcode(0x51) = new Hb8bInstructionMetadata("EOR", AddressingMode.IZY, () => EOR(IZY), 5);
        Opcode(0x52) = new Hb8bInstructionMetadata("EOR", AddressingMode.IZP, () => EOR(IZP), 5);

        // INC
        Opcode(0xE6) = new Hb8bInstructionMetadata("INC", AddressingMode.ZP0, () => INC(ZP0), 5);
        Opcode(0xF6) = new Hb8bInstructionMetadata("INC", AddressingMode.ZPX, () => INC(ZPX), 6);
        Opcode(0xEE) = new Hb8bInstructionMetadata("INC", AddressingMode.ABS, () => INC(ABS), 6);
        Opcode(0xFE) = new Hb8bInstructionMetadata("INC", AddressingMode.ABX, () => INC(ABX), 7);
        Opcode(0x1A) = new Hb8bInstructionMetadata("INC", AddressingMode.IMP, () => INC(IMP), 2);

        // INX
        Opcode(0xE8) = new Hb8bInstructionMetadata("INX", AddressingMode.IMP, INX, 2);

        // INY
        Opcode(0xC8) = new Hb8bInstructionMetadata("INY", AddressingMode.IMP, INY, 2);

        // JMP
        Opcode(0x4C) = new Hb8bInstructionMetadata("JMP", AddressingMode.ABS, () => JMP(ABS), 3);
        Opcode(0x6C) = new Hb8bInstructionMetadata("JMP", AddressingMode.IND, () => JMP(IND), 6);
        Opcode(0x7C) = new Hb8bInstructionMetadata("JMP", AddressingMode.INDX, () => JMP(INDX), 6);

        // JSR
        Opcode(0x20) = new Hb8bInstructionMetadata("JSR", AddressingMode.ABS, JSR, 6);

        // LDA
        Opcode(0xA9) = new Hb8bInstructionMetadata("LDA", AddressingMode.IMM, () => LDA(IMM), 2);
        Opcode(0xA5) = new Hb8bInstructionMetadata("LDA", AddressingMode.ZP0, () => LDA(ZP0), 3);
        Opcode(0xB5) = new Hb8bInstructionMetadata("LDA", AddressingMode.ZPX, () => LDA(ZPX), 4);
        Opcode(0xAD) = new Hb8bInstructionMetadata("LDA", AddressingMode.ABS, () => LDA(ABS), 4);
        Opcode(0xBD) = new Hb8bInstructionMetadata("LDA", AddressingMode.ABX, () => LDA(ABX), 4);
        Opcode(0xB9) = new Hb8bInstructionMetadata("LDA", AddressingMode.ABY, () => LDA(ABY), 4);
        Opcode(0xA1) = new Hb8bInstructionMetadata("LDA", AddressingMode.IZX, () => LDA(IZX), 6);
        Opcode(0xB1) = new Hb8bInstructionMetadata("LDA", AddressingMode.IZY, () => LDA(IZY), 5);
        Opcode(0xB2) = new Hb8bInstructionMetadata("LDA", AddressingMode.IZP, () => LDA(IZP), 5);

        // LDX
        Opcode(0xA2) = new Hb8bInstructionMetadata("LDX", AddressingMode.IMM, () => LDX(IMM), 2);
        Opcode(0xA6) = new Hb8bInstructionMetadata("LDX", AddressingMode.ZP0, () => LDX(ZP0), 3);
        Opcode(0xB6) = new Hb8bInstructionMetadata("LDX", AddressingMode.ZPY, () => LDX(ZPY), 4);
        Opcode(0xAE) = new Hb8bInstructionMetadata("LDX", AddressingMode.ABS, () => LDX(ABS), 4);
        Opcode(0xBE) = new Hb8bInstructionMetadata("LDX", AddressingMode.ABY, () => LDX(ABY), 4);

        // LDY
        Opcode(0xA0) = new Hb8bInstructionMetadata("LDY", AddressingMode.IMM, () => LDY(IMM), 2);
        Opcode(0xA4) = new Hb8bInstructionMetadata("LDY", AddressingMode.ZP0, () => LDY(ZP0), 3);
        Opcode(0xB4) = new Hb8bInstructionMetadata("LDY", AddressingMode.ZPX, () => LDY(ZPX), 4);
        Opcode(0xAC) = new Hb8bInstructionMetadata("LDY", AddressingMode.ABS, () => LDY(ABS), 4);
        Opcode(0xBC) = new Hb8bInstructionMetadata("LDY", AddressingMode.ABX, () => LDY(ABX), 4);

        // LSR
        Opcode(0x4A) = new Hb8bInstructionMetadata("LSR", AddressingMode.IMP, () => LSR(IMP), 2);
        Opcode(0x46) = new Hb8bInstructionMetadata("LSR", AddressingMode.ZP0, () => LSR(ZP0), 5);
        Opcode(0x56) = new Hb8bInstructionMetadata("LSR", AddressingMode.ZPX, () => LSR(ZPX), 6);
        Opcode(0x4E) = new Hb8bInstructionMetadata("LSR", AddressingMode.ABS, () => LSR(ABS), 6);
        Opcode(0x5E) = new Hb8bInstructionMetadata("LSR", AddressingMode.ABX, () => LSR(ABX), 6);

        // NOP
        Opcode(0xEA) = new Hb8bInstructionMetadata("NOP", AddressingMode.IMP, () => NOP(1), 2);
        
        // ORA
        Opcode(0x09) = new Hb8bInstructionMetadata("ORA", AddressingMode.IMM, () => ORA(IMM), 2);
        Opcode(0x05) = new Hb8bInstructionMetadata("ORA", AddressingMode.ZP0, () => ORA(ZP0), 3);
        Opcode(0x15) = new Hb8bInstructionMetadata("ORA", AddressingMode.ZPX, () => ORA(ZPX), 4);
        Opcode(0x0D) = new Hb8bInstructionMetadata("ORA", AddressingMode.ABS, () => ORA(ABS), 4);
        Opcode(0x1D) = new Hb8bInstructionMetadata("ORA", AddressingMode.ABX, () => ORA(ABX), 4);
        Opcode(0x19) = new Hb8bInstructionMetadata("ORA", AddressingMode.ABY, () => ORA(ABY), 4);
        Opcode(0x01) = new Hb8bInstructionMetadata("ORA", AddressingMode.IZX, () => ORA(IZX), 6);
        Opcode(0x11) = new Hb8bInstructionMetadata("ORA", AddressingMode.IZY, () => ORA(IZY), 5);
        Opcode(0x12) = new Hb8bInstructionMetadata("ORA", AddressingMode.IZP, () => ORA(IZP), 5);

        // PHA
        Opcode(0x48) = new Hb8bInstructionMetadata("PHA", AddressingMode.IMP, PHA, 2);

        // PHP
        Opcode(0x08) = new Hb8bInstructionMetadata("PHP", AddressingMode.IMP, PHP, 2);

        // PHX
        Opcode(0xDA) = new Hb8bInstructionMetadata("PHX", AddressingMode.IMP, PHX, 2);

        // PHY
        Opcode(0x5A) = new Hb8bInstructionMetadata("PHY", AddressingMode.IMP, PHY, 2);

        // PLA
        Opcode(0x68) = new Hb8bInstructionMetadata("PLA", AddressingMode.IMP, PLA, 2);

        // PLP
        Opcode(0x28) = new Hb8bInstructionMetadata("PLP", AddressingMode.IMP, PLP, 2);

        // PLX
        Opcode(0xFA) = new Hb8bInstructionMetadata("PLX", AddressingMode.IMP, PLX, 2);

        // PLY
        Opcode(0x7A) = new Hb8bInstructionMetadata("PLY", AddressingMode.IMP, PLY, 2);

        // RMB
        Opcode(0x07) = new Hb8bInstructionMetadata("RMB0", AddressingMode.ZP0, () => RMB(0), 5);
        Opcode(0x17) = new Hb8bInstructionMetadata("RMB1", AddressingMode.ZP0, () => RMB(1), 5);
        Opcode(0x27) = new Hb8bInstructionMetadata("RMB2", AddressingMode.ZP0, () => RMB(2), 5);
        Opcode(0x37) = new Hb8bInstructionMetadata("RMB3", AddressingMode.ZP0, () => RMB(3), 5);
        Opcode(0x47) = new Hb8bInstructionMetadata("RMB4", AddressingMode.ZP0, () => RMB(4), 5);
        Opcode(0x57) = new Hb8bInstructionMetadata("RMB5", AddressingMode.ZP0, () => RMB(5), 5);
        Opcode(0x67) = new Hb8bInstructionMetadata("RMB6", AddressingMode.ZP0, () => RMB(6), 5);
        Opcode(0x77) = new Hb8bInstructionMetadata("RMB7", AddressingMode.ZP0, () => RMB(7), 5);

        // ROL
        Opcode(0x2A) = new Hb8bInstructionMetadata("ROL", AddressingMode.IMP, () => ROL(IMP), 2);
        Opcode(0x26) = new Hb8bInstructionMetadata("ROL", AddressingMode.ZP0, () => ROL(ZP0), 5);
        Opcode(0x36) = new Hb8bInstructionMetadata("ROL", AddressingMode.ZPX, () => ROL(ZPX), 6);
        Opcode(0x2E) = new Hb8bInstructionMetadata("ROL", AddressingMode.ABS, () => ROL(ABS), 6);
        Opcode(0x3E) = new Hb8bInstructionMetadata("ROL", AddressingMode.ABX, () => ROL(ABX), 6);

        // ROR
        Opcode(0x6A) = new Hb8bInstructionMetadata("ROR", AddressingMode.IMP, () => ROR(IMP), 2);
        Opcode(0x66) = new Hb8bInstructionMetadata("ROR", AddressingMode.ZP0, () => ROR(ZP0), 5);
        Opcode(0x76) = new Hb8bInstructionMetadata("ROR", AddressingMode.ZPX, () => ROR(ZPX), 6);
        Opcode(0x6E) = new Hb8bInstructionMetadata("ROR", AddressingMode.ABS, () => ROR(ABS), 6);
        Opcode(0x7E) = new Hb8bInstructionMetadata("ROR", AddressingMode.ABX, () => ROR(ABX), 6);

        // RTI
        Opcode(0x40) = new Hb8bInstructionMetadata("RTI", AddressingMode.IMP, RTI, 6);

        // RTS
        Opcode(0x60) = new Hb8bInstructionMetadata("RTS", AddressingMode.IMP, RTS, 6);

        // SBC
        Opcode(0xE9) = new Hb8bInstructionMetadata("SBC", AddressingMode.IMM, () => SBC(IMM), 2);
        Opcode(0xE5) = new Hb8bInstructionMetadata("SBC", AddressingMode.ZP0, () => SBC(ZP0), 3);
        Opcode(0xF5) = new Hb8bInstructionMetadata("SBC", AddressingMode.ZPX, () => SBC(ZPX), 4);
        Opcode(0xED) = new Hb8bInstructionMetadata("SBC", AddressingMode.ABS, () => SBC(ABS), 4);
        Opcode(0xFD) = new Hb8bInstructionMetadata("SBC", AddressingMode.ABX, () => SBC(ABX), 4);
        Opcode(0xF9) = new Hb8bInstructionMetadata("SBC", AddressingMode.ABY, () => SBC(ABY), 4);
        Opcode(0xE1) = new Hb8bInstructionMetadata("SBC", AddressingMode.IZX, () => SBC(IZX), 6);
        Opcode(0xF1) = new Hb8bInstructionMetadata("SBC", AddressingMode.IZY, () => SBC(IZY), 5);
        Opcode(0xF2) = new Hb8bInstructionMetadata("SBC", AddressingMode.IZP, () => SBC(IZP), 5);

        // SEC
        Opcode(0x38) = new Hb8bInstructionMetadata("SEC", AddressingMode.IMP, SEC, 2);

        // SED
        Opcode(0xF8) = new Hb8bInstructionMetadata("SED", AddressingMode.IMP, SED, 2);

        // SEI
        Opcode(0x78) = new Hb8bInstructionMetadata("SEI", AddressingMode.IMP, SEI, 2);

        // SMB
        Opcode(0x87) = new Hb8bInstructionMetadata("SMB0", AddressingMode.ZP0, () => SMB(0), 5);
        Opcode(0x97) = new Hb8bInstructionMetadata("SMB1", AddressingMode.ZP0, () => SMB(1), 5);
        Opcode(0xA7) = new Hb8bInstructionMetadata("SMB2", AddressingMode.ZP0, () => SMB(2), 5);
        Opcode(0xB7) = new Hb8bInstructionMetadata("SMB3", AddressingMode.ZP0, () => SMB(3), 5);
        Opcode(0xC7) = new Hb8bInstructionMetadata("SMB4", AddressingMode.ZP0, () => SMB(4), 5);
        Opcode(0xD7) = new Hb8bInstructionMetadata("SMB5", AddressingMode.ZP0, () => SMB(5), 5);
        Opcode(0xE7) = new Hb8bInstructionMetadata("SMB6", AddressingMode.ZP0, () => SMB(6), 5);
        Opcode(0xF7) = new Hb8bInstructionMetadata("SMB7", AddressingMode.ZP0, () => SMB(7), 5);

        // STA
        Opcode(0x85) = new Hb8bInstructionMetadata("STA", AddressingMode.ZP0, () => STA(ZP0), 3);
        Opcode(0x95) = new Hb8bInstructionMetadata("STA", AddressingMode.ZPX, () => STA(ZPX), 4);
        Opcode(0x8D) = new Hb8bInstructionMetadata("STA", AddressingMode.ABS, () => STA(ABS), 4);
        Opcode(0x9D) = new Hb8bInstructionMetadata("STA", AddressingMode.ABX, () => STA(ABX), 5);
        Opcode(0x99) = new Hb8bInstructionMetadata("STA", AddressingMode.ABY, () => STA(ABY), 5);
        Opcode(0x81) = new Hb8bInstructionMetadata("STA", AddressingMode.IZX, () => STA(IZX), 6);
        Opcode(0x91) = new Hb8bInstructionMetadata("STA", AddressingMode.IZY, () => STA(IZY), 6);
        Opcode(0x92) = new Hb8bInstructionMetadata("STA", AddressingMode.IZP, () => STA(IZP), 5);

        // STP
        Opcode(0xDB) = new Hb8bInstructionMetadata("STP", AddressingMode.IMP, STP, 3, HaltsUntilInterrupt: true);

        // STX
        Opcode(0x86) = new Hb8bInstructionMetadata("STX", AddressingMode.ZP0, () => STX(ZP0), 3);
        Opcode(0x96) = new Hb8bInstructionMetadata("STX", AddressingMode.ZPY, () => STX(ZPY), 4);
        Opcode(0x8E) = new Hb8bInstructionMetadata("STX", AddressingMode.ABS, () => STX(ABS), 4);

        // STY
        Opcode(0x84) = new Hb8bInstructionMetadata("STY", AddressingMode.ZP0, () => STY(ZP0), 3);
        Opcode(0x94) = new Hb8bInstructionMetadata("STY", AddressingMode.ZPX, () => STY(ZPX), 4);
        Opcode(0x8C) = new Hb8bInstructionMetadata("STY", AddressingMode.ABS, () => STY(ABS), 4);

        // STZ
        Opcode(0x64) = new Hb8bInstructionMetadata("STZ", AddressingMode.ZP0, () => STZ(ZP0), 3);
        Opcode(0x74) = new Hb8bInstructionMetadata("STZ", AddressingMode.ZPX, () => STZ(ZPX), 4);
        Opcode(0x9C) = new Hb8bInstructionMetadata("STZ", AddressingMode.ABS, () => STZ(ABS), 4);
        Opcode(0x9E) = new Hb8bInstructionMetadata("STZ", AddressingMode.ABX, () => STZ(ABX), 5);

        // TAX
        Opcode(0xAA) = new Hb8bInstructionMetadata("TAX", AddressingMode.IMP, TAX, 2);

        // TAY
        Opcode(0xA8) = new Hb8bInstructionMetadata("TAY", AddressingMode.IMP, TAY, 2);

        // TRB
        Opcode(0x14) = new Hb8bInstructionMetadata("TRB", AddressingMode.ZP0, () => TRB(ZP0), 5);
        Opcode(0x1C) = new Hb8bInstructionMetadata("TRB", AddressingMode.ABS, () => TRB(ABS), 6);

        // TSB
        Opcode(0x04) = new Hb8bInstructionMetadata("TSB", AddressingMode.ZP0, () => TSB(ZP0), 5);
        Opcode(0x0C) = new Hb8bInstructionMetadata("TSB", AddressingMode.ABS, () => TSB(ABS), 6);

        // TSX
        Opcode(0xBA) = new Hb8bInstructionMetadata("TSX", AddressingMode.IMP, TSX, 2);

        // TXA
        Opcode(0x8A) = new Hb8bInstructionMetadata("TXA", AddressingMode.IMP, TXA, 2);

        // TXS
        Opcode(0x9A) = new Hb8bInstructionMetadata("TXS", AddressingMode.IMP, TXS, 2);

        // TYA
        Opcode(0x98) = new Hb8bInstructionMetadata("TYA", AddressingMode.IMP, TYA, 2);

        // WAI
        Opcode(0xCB) = new Hb8bInstructionMetadata("WAI", AddressingMode.IMP, WAI, 3, HaltsUntilInterrupt: true);

        // Validate ISA.
        var invalidOpcodes = isa.Where(x => x == null).Select((x, index) => index.ToString("X2"));
        if (invalidOpcodes.Any())
            throw new InvalidOperationException($"Incomplete ISA specification; missing instructions for opcodes {String.Join(", ", invalidOpcodes)}.");

        return isa;
    }

    /// <summary>
    /// Fetches the data used by the current instruction.
    /// </summary>
    private Byte FetchOperationData()
    {
        var addrMode = _currentInstruction.AddressingMode;
        if (addrMode != AddressingMode.IMP && addrMode != AddressingMode.ZPREL)
        {
            _opData = Read(_opAddrAbs);
        }
        return _opData;
    }

    /// <summary>
    /// Branches if the specified condition is mset.
    /// </summary>
    private void BranchIfConditionIsMet(Boolean condition)
    {
        if (!condition)
            return;

        _cyclesForInstruction++;
        _opAddrAbs = (UInt16)(_pc + _opAddrRel);

        if ((_opAddrAbs & 0xFF00) != (_pc & 0xFF00))
            _cyclesForInstruction++;

        _pc = _opAddrAbs;
    }

    /// <summary>
    /// Branches if the specified status flag is set.
    /// </summary>
    private void BranchIfStatusFlagIsSet(Hb8b6502StatusFlag flag)
    {
        var flagIsSet = GetStatusFlag(flag);
        BranchIfConditionIsMet(flagIsSet);
    }

    /// <summary>
    /// Branches if the specified status flag is clear.
    /// </summary>
    private void BranchIfStatusFlagIsClear(Hb8b6502StatusFlag flag)
    {
        var flagIsClear = !GetStatusFlag(flag);
        BranchIfConditionIsMet(flagIsClear);
    }

    /// <summary>
    /// Updates the CPU's Z flag based on the specified value.
    /// </summary>
    private void UpdateZ(UInt32 value)
    {
        SetStatusFlag(Hb8b6502StatusFlag.Z, (value & 0xFF) == 0);
    }

    /// <summary>
    /// Updates the CPU's N flag based on the specified value.
    /// </summary>
    private void UpdateN(UInt32 value)
    {
        SetStatusFlag(Hb8b6502StatusFlag.N, (value & 0x80) != 0);
    }

    /// <summary>
    /// Updates the CPU's C flag based on the specified value.
    /// </summary>
    private void UpdateC(UInt32 value, UInt32 cmp)
    {
        SetStatusFlag(Hb8b6502StatusFlag.C, value >= cmp);
    }

    /// <summary>
    /// Performs a write to the specified register if the instruction is in implied addressing mode; otherwise,
    /// writes to the specified bus address.
    /// </summary>
    private void ImpliedWrite(UInt16 addr, ref Byte register, Byte value)
    {
        if (_currentInstruction.AddressingMode == AddressingMode.IMP)
            register = value;
        else
            Write(addr, value);
    }

    #region 6502 Instructions

    /// <summary>
    /// Instruction: Add with Carry (ADC)
    /// </summary>
    private Int32 ADC(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var data = FetchOperationData();
        var temp = (UInt32)(_a + data + (UInt16)(GetStatusFlag(Hb8b6502StatusFlag.C) ? 1 : 0));
        var overflow = ~(_a ^ data) & (_a ^ (UInt16)temp) & 0x0080;

        SetStatusFlag(Hb8b6502StatusFlag.V, overflow != 0);
        UpdateC(temp, 256);
        UpdateZ(temp);
        UpdateN(temp);

        _a = (Byte)(temp & 0xFF);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Bitwise AND with Accumulator (AND)
    /// </summary>
    private Int32 AND(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        _a = (Byte)(_a & FetchOperationData());
        UpdateZ(_a);
        UpdateN(_a);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Arithmetic Shift Left (ASL)
    /// </summary>
    private Int32 ASL(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var temp = (UInt16)(FetchOperationData() << 1);
        UpdateC(temp, 256);
        UpdateZ(temp);
        UpdateN(temp);

        var result = (Byte)(temp & 0x00FF);
        ImpliedWrite(_opAddrAbs, ref _a, result);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Branch on Bit Reset
    /// </summary>
    private Int32 BBR(Int32 bit)
    {
        ZPREL();

        var zpaddr = FetchOperationData();
        var zpdata = Read(zpaddr);
        var bitIsClear = (zpdata & (1 << bit)) == 0;
        BranchIfConditionIsMet(bitIsClear);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Bit Set
    /// </summary>
    private Int32 BBS(Int32 bit)
    {
        ZPREL();

        var zpaddr = FetchOperationData();
        var zpdata = Read(zpaddr);
        var bitIsSet = (zpdata & (1 << bit)) != 0;
        BranchIfConditionIsMet(bitIsSet);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Carry Clear (BCC)
    /// </summary>
    private Int32 BCC()
    {
        REL();

        BranchIfStatusFlagIsClear(Hb8b6502StatusFlag.C);
        
        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Carry Set (BCS)
    /// </summary>
    private Int32 BCS()
    {
        REL();

        BranchIfStatusFlagIsSet(Hb8b6502StatusFlag.C);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Equal (BEQ)
    /// </summary>
    private Int32 BEQ()
    {
        REL();

        BranchIfStatusFlagIsSet(Hb8b6502StatusFlag.Z);

        return 0;
    }

    /// <summary>
    /// Instruction: Test Bits (BIT)
    /// </summary>
    private Int32 BIT(Func<Int32> addrMode)
    {
        addrMode();

        var data = FetchOperationData();
        var temp = (Byte)(_a & data);
        UpdateZ(temp);

        if (_currentInstruction.AddressingMode != AddressingMode.IMM)
        {
            SetStatusFlag(Hb8b6502StatusFlag.N, (data & (1 << 7)) != 0);
            SetStatusFlag(Hb8b6502StatusFlag.V, (data & (1 << 6)) != 0);
        }

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Minus (BMI)
    /// </summary>
    private Int32 BMI()
    {
        REL();

        BranchIfStatusFlagIsSet(Hb8b6502StatusFlag.N);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch if Not Equal (BNE)
    /// </summary>
    private Int32 BNE()
    {
        REL();

        BranchIfStatusFlagIsClear(Hb8b6502StatusFlag.Z);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Plus (BPL)
    /// </summary>
    private Int32 BPL()
    {
        REL();

        BranchIfStatusFlagIsClear(Hb8b6502StatusFlag.N);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch Always (BRA)
    /// </summary>
    private Int32 BRA()
    {
        REL();

        BranchIfConditionIsMet(true);

        return 0;
    }

    /// <summary>
    /// Instruction: Break (BRK)
    /// </summary>
    private Int32 BRK()
    {
        StackPush16((UInt16)(_pc + 1));

        StackPush(_status | (Byte)Hb8b6502StatusFlag.B | (Byte)Hb8b6502StatusFlag.U);

        SetStatusFlag(Hb8b6502StatusFlag.D, false);
        SetStatusFlag(Hb8b6502StatusFlag.I, true);

        _pc = Read16(0xFFFE);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Overflow Clear (BVC)
    /// </summary>
    private Int32 BVC()
    {
        REL();

        BranchIfStatusFlagIsClear(Hb8b6502StatusFlag.V);

        return 0;
    }

    /// <summary>
    /// Instruction: Branch on Overflow Set (BVS)
    /// </summary>
    private Int32 BVS()
    {
        REL();

        BranchIfStatusFlagIsSet(Hb8b6502StatusFlag.V);

        return 0;
    }

    /// <summary>
    /// Instruction: Clear Carry Flag (CLC)
    /// </summary>
    private Int32 CLC()
    {
        SetStatusFlag(Hb8b6502StatusFlag.C, false);

        return 0;
    }

    /// <summary>
    /// Instruction: Clear Decimal Flag (CLD)
    /// </summary>
    private Int32 CLD()
    {
        SetStatusFlag(Hb8b6502StatusFlag.D, false);

        return 0;
    }

    /// <summary>
    /// Instruction: Clear Interrupt Flag (CLI)
    /// </summary>
    private Int32 CLI()
    {
        SetStatusFlag(Hb8b6502StatusFlag.I, false);

        return 0;
    }

    /// <summary>
    /// Instruction: Clear Overflow Flag (CLV)
    /// </summary>
    private Int32 CLV()
    {
        SetStatusFlag(Hb8b6502StatusFlag.V, false);

        return 0;
    }

    /// <summary>
    /// Instruction: Compare Accumulator (CMP)
    /// </summary>
    private Int32 CMP(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var data = FetchOperationData();
        var temp = (UInt16)(_a - data);
        UpdateC(_a, data);
        UpdateZ(temp);
        UpdateN(temp);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Compare X Register (CPX)
    /// </summary>
    private Int32 CPX(Func<Int32> addrMode)
    {
        addrMode();

        var data = FetchOperationData();
        var temp = (UInt16)(_x - data);
        UpdateC(_x, data);
        UpdateZ(temp);
        UpdateN(temp);

        return 0;
    }

    /// <summary>
    /// Instruction: Compare Y Register (CPY)
    /// </summary>
    private Int32 CPY(Func<Int32> addrMode)
    {
        addrMode();

        var data = FetchOperationData();
        var temp = (UInt16)(_y - data);
        UpdateC(_y, data);
        UpdateZ(temp);
        UpdateN(temp);

        return 0;
    }

    /// <summary>
    /// Instruction: Decrement Accumulator (DEC)
    /// </summary>
    private Int32 DEC(Func<Int32> addrMode)
    {
        addrMode();

        var temp = (Byte)(FetchOperationData() - 1);
        UpdateZ(temp);
        UpdateN(temp);
        ImpliedWrite(_opAddrAbs, ref _a, (Byte)(temp & 0x00FF));

        return 0;
    }

    /// <summary>
    /// Instruction: Decrement X Register (DEX)
    /// </summary>
    private Int32 DEX()
    {
        _x--;
        UpdateZ(_x);
        UpdateN(_x);

        return 0;
    }

    /// <summary>
    /// Instruction: Decrement Y Register (DEY)
    /// </summary>
    private Int32 DEY()
    {
        _y--;
        UpdateZ(_y);
        UpdateN(_y);

        return 0;
    }

    /// <summary>
    /// Instruction: Bitwise Exclusive OR (EOR)
    /// </summary>
    private Int32 EOR(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        _a = (Byte)(_a ^ FetchOperationData());
        UpdateZ(_a);
        UpdateN(_a);

        return potentialExtraCycle;
    }    

    /// <summary>
    /// Instruction: Increment Accumulator (INC)
    /// </summary>
    private Int32 INC(Func<Int32> addrMode)
    {
        addrMode();

        var temp = (Byte)(FetchOperationData() + 1);
        UpdateZ(temp);
        UpdateN(temp);
        ImpliedWrite(_opAddrAbs, ref _a, (Byte)(temp & 0x00FF));

        return 0;
    }

    /// <summary>
    /// Instruction: Increment X Register (INX)
    /// </summary>
    private Int32 INX()
    {
        _x++;
        UpdateZ(_x);
        UpdateN(_x);

        return 0;
    }

    /// <summary>
    /// Instruction: Increment Y Register (INY)
    /// </summary>
    private Int32 INY()
    {
        _y++;
        UpdateZ(_y);
        UpdateN(_y);

        return 0;
    }

    /// <summary>
    /// Instruction: Jump to Location (JMP)
    /// </summary>
    private Int32 JMP(Func<Int32> addrMode)
    {
        addrMode();

        _pc = _opAddrAbs;

        return 0;
    }

    /// <summary>
    /// Instruction: Jump to Subroutine (JSR)
    /// </summary>
    private Int32 JSR()
    {
        ABS();

        _pc--;
        StackPush16(_pc);
        _pc = _opAddrAbs;

        return 0;
    }

    /// <summary>
    /// Instruction: Load the Accumulator (LDA)
    /// </summary>
    private Int32 LDA(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        _a = FetchOperationData();
        UpdateZ(_a);
        UpdateN(_a);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Load the X Register (LDX)
    /// </summary>
    private Int32 LDX(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        _x = FetchOperationData();
        UpdateZ(_x);
        UpdateN(_x);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Load the Y Register (LDY)
    /// </summary>
    private Int32 LDY(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        _y = FetchOperationData();
        UpdateZ(_y);
        UpdateN(_y);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Logical Shift Right (LSR)
    /// </summary>
    private Int32 LSR(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var data = FetchOperationData();
        UpdateC((UInt16)(data & 0x1), 1);

        var temp = (UInt16)(data >> 1);
        UpdateZ(temp);
        UpdateN(temp);

        var result = (Byte)(temp & 0xFF);
        ImpliedWrite(_opAddrAbs, ref _a, result);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: No Operation (NOP)
    /// </summary>
    private Int32 NOP(Byte bytes)
    {
        IMP();

        for (var i = 1; i < bytes; i++)
            Read(_pc++);

        return 0;
    }

    /// <summary>
    /// Instruction: Logical OR with Accumulator (ORA)
    /// </summary>
    private Int32 ORA(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        _a = (Byte)(_a | FetchOperationData());
        UpdateZ(_a);
        UpdateN(_a);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Push Accumulator (PHA)
    /// </summary>
    private Int32 PHA()
    {
        StackPush(_a);

        return 0;
    }

    /// <summary>
    /// Instruction: Push Processor Status (PHP)
    /// </summary>
    private Int32 PHP()
    {
        StackPush(_status | (Byte)Hb8b6502StatusFlag.B | (Byte)Hb8b6502StatusFlag.U);

        return 0;
    }

    /// <summary>
    /// Instruction: Push X Register (PHX)
    /// </summary>
    private Int32 PHX()
    {
        StackPush(_x);

        return 0;
    }

    /// <summary>
    /// Instruction: Push Y Register (PHY)
    /// </summary>
    private Int32 PHY()
    {
        StackPush(_y);

        return 0;
    }

    /// <summary>
    /// Instruction: Pull Accumulator (PLA)
    /// </summary>
    private Int32 PLA()
    {
        _a = StackPop();
        UpdateZ(_a);
        UpdateN(_a);

        return 0;
    }

    /// <summary>
    /// Instruction: Pull Processor Status (PLP)
    /// </summary>
    private Int32 PLP()
    {
        _status = StackPop();
        _status &= (Byte)~Hb8b6502StatusFlag.B;
        _status &= (Byte)~Hb8b6502StatusFlag.U;

        return 0;
    }

    /// <summary>
    /// Instruction: Pull X Register (PLX)
    /// </summary>
    private Int32 PLX()
    {
        _x = StackPop();
        UpdateZ(_x);
        UpdateN(_x);

        return 0;
    }

    /// <summary>
    /// Instruction: Pull Y Register (PLY)
    /// </summary>
    private Int32 PLY()
    {
        _y = StackPop();
        UpdateZ(_y);
        UpdateN(_y);

        return 0;
    }

    /// <summary>
    /// Instruction: Reset Memory Bit (RMB)
    /// </summary>
    private Int32 RMB(Int32 bit)
    {
        ZP0();

        var data = FetchOperationData();
        data &= (Byte)~(1 << bit);
        Write(_opAddrAbs, data);

        return 0;
    }

    /// <summary>
    /// Instruction: Rotate Left (ROL)
    /// </summary>
    private Int32 ROL(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var data = FetchOperationData();
        var temp = (UInt16)((data << 1) | (GetStatusFlag(Hb8b6502StatusFlag.C) ? 1 : 0));
        UpdateC(temp, 256);
        UpdateZ(temp);
        UpdateN(temp);

        var result = (Byte)(temp & 0x00FF);
        ImpliedWrite(_opAddrAbs, ref _a, result);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Rotate Right (ROR)
    /// </summary>
    private Int32 ROR(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var data = FetchOperationData();
        var temp = (UInt16)(((GetStatusFlag(Hb8b6502StatusFlag.C) ? 1 : 0) << 7) | (data >> 1));
        SetStatusFlag(Hb8b6502StatusFlag.C, (data & 0x01) != 0);
        UpdateZ(temp);
        UpdateN(temp);

        var result = (Byte)(temp & 0x00FF);
        ImpliedWrite(_opAddrAbs, ref _a, result);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Return from Interrupt (RTI)
    /// </summary>
    private Int32 RTI()
    {
        _status = StackPop();
        _status &= (Byte)~Hb8b6502StatusFlag.B;
        _status &= (Byte)~Hb8b6502StatusFlag.U;

        _pc = StackPop16();

        return 0;
    }

    /// <summary>
    /// Instruction: Return from Subroutine (RTS)
    /// </summary>
    private Int32 RTS()
    {
        _pc = (UInt16)(StackPop16() + 1);
        return 0;
    }

    /// <summary>
    /// Instruction: Subtract with Carry (SBC)
    /// </summary>
    private Int32 SBC(Func<Int32> addrMode)
    {
        var potentialExtraCycle = addrMode();

        var data = (UInt16)FetchOperationData();
        var value = (UInt16)(data ^ 0x00FF);
        data = (UInt16)(_a + value + (GetStatusFlag(Hb8b6502StatusFlag.C) ? 1 : 0));

        var overflow = ((data ^ _a) & (data ^ value) & 0x0080) != 0;
        SetStatusFlag(Hb8b6502StatusFlag.V, overflow);
        UpdateC(data, 256);
        UpdateZ(data);
        UpdateN(data);

        _a = (Byte)(data & 0x00FF);

        return potentialExtraCycle;
    }

    /// <summary>
    /// Instruction: Set Carry Flag (SEC)
    /// </summary>
    private Int32 SEC()
    {
        SetStatusFlag(Hb8b6502StatusFlag.C, true);

        return 0;
    }

    /// <summary>
    /// Instruction: Set Decimal Flag (SED)
    /// </summary>
    private Int32 SED()
    {
        SetStatusFlag(Hb8b6502StatusFlag.D, true);

        return 0;
    }

    /// <summary>
    /// Instruction: Set Interrupt Flag (SEI)
    /// </summary>
    private Int32 SEI()
    {
        SetStatusFlag(Hb8b6502StatusFlag.I, true);

        return 0;
    }

    /// <summary>
    /// Instruction: Set Memory Bit (SMB)
    /// </summary>
    private Int32 SMB(Int32 bit)
    {
        ZP0();

        var data = FetchOperationData();
        data |= (Byte)(1 << bit);
        Write(_opAddrAbs, data);

        return 0;
    }

    /// <summary>
    /// Instruction: Store the Accumulator (STA)
    /// </summary>
    private Int32 STA(Func<Int32> addrMode)
    {
        addrMode();

        Write(_opAddrAbs, _a);

        return 0;
    }

    /// <summary>
    /// Instruction: Stop the Processor (STP)
    /// </summary>
    private Int32 STP()
    {
        this.IsStopped = true;

        return 0;
    }

    /// <summary>
    /// Instruction: Store the X Register (STX)
    /// </summary>
    private Int32 STX(Func<Int32> addrMode)
    {
        addrMode();

        Write(_opAddrAbs, _x);

        return 0;
    }

    /// <summary>
    /// Instruction: Store the Y Register (STY)
    /// </summary>
    private Int32 STY(Func<Int32> addrMode)
    {
        addrMode();

        Write(_opAddrAbs, _y);

        return 0;
    }

    /// <summary>
    /// Instruction: Store Zero (STZ)
    /// </summary>
    private Int32 STZ(Func<Int32> addrMode)
    {
        addrMode();

        Write(_opAddrAbs, (Byte)0x00);

        return 0;
    }

    /// <summary>
    /// Instruction: Transfer Accumulator to X Register (TAX)
    /// </summary>
    private Int32 TAX()
    {
        _x = _a;
        UpdateZ(_x);
        UpdateN(_x);

        return 0;
    }

    /// <summary>
    /// Instruction: Transfer Accumulator to Y Register (TAY)
    /// </summary>
    private Int32 TAY()
    {
        _y = _a;
        UpdateZ(_y);
        UpdateN(_y);

        return 0;
    }

    /// <summary>
    /// Instruction: Test and Reset Bits (TRB)
    /// </summary>
    private Int32 TRB(Func<Int32> addrMode)
    {
        addrMode();

        var data = FetchOperationData();
        var temp = (Byte)(_a & data);
        UpdateZ(temp);

        data &= (Byte)~_a;
        Write(_opAddrAbs, data);

        return 0;
    }

    /// <summary>
    /// Instruction: Test and Set Bits (TSB)
    /// </summary>
    private Int32 TSB(Func<Int32> addrMode)
    {
        addrMode();

        var data = FetchOperationData();
        var temp = (Byte)(_a & data);
        UpdateZ(temp);

        data |= _a;
        Write(_opAddrAbs, data);

        return 0;
    }

    /// <summary>
    /// Instruction: Transfer Stack Pointer to X Register (TSX)
    /// </summary>
    private Int32 TSX()
    {
        _x = _stkp;
        UpdateZ(_x);
        UpdateN(_x);

        return 0;
    }

    /// <summary>
    /// Instruction: Transfer X Register to Accumulator (TXA)
    /// </summary>
    private Int32 TXA()
    {
        _a = _x;
        UpdateZ(_a);
        UpdateN(_a);

        return 0;
    }

    /// <summary>
    /// Instruction: Transfer X Register to Stack Pointer (TXS)
    /// </summary>
    private Int32 TXS()
    {
        _stkp = _x;

        return 0;
    }

    /// <summary>
    /// Instruction: Transfer Y Register to Accumulator (TYA)
    /// </summary>
    private Int32 TYA()
    {
        _a = _y;
        UpdateZ(_a);
        UpdateN(_a);

        return 0;
    }

    /// <summary>
    /// Instruction: Wait for Interrupt (WAI)
    /// </summary>
    private Int32 WAI()
    {
        this.IsWaitingForInterrupt = true;

        return 0;
    }

    #endregion

    #region 6502 Addressing Modes

    /// <summary>
    /// Address mode: Implied
    /// </summary>
    private Int32 IMP()
    {
        _opData = _a;
        return 0;
    }

    /// <summary>
    /// Address mode: Immediate
    /// </summary>
    /// <returns></returns>
    private Int32 IMM()
    {
        _opAddrAbs = _pc++;
        return 0;
    }

    /// <summary>
    /// Address mode: Zero Page
    /// </summary>
    private Int32 ZP0()
    {
        _opAddrAbs = Read(_pc++);
        _opAddrAbs &= 0x00FF;
        return 0;
    }

    /// <summary>
    /// Address mode: Zero Page with X Offset
    /// </summary>
    /// <returns></returns>
    private Int32 ZPX()
    {
        _opAddrAbs = (UInt16)(Read(_pc++) + _x);
        _opAddrAbs &= 0x00FF;
        return 0;
    }

    /// <summary>
    /// Address mode: Zero Page with Y Offset
    /// </summary>
    private Int32 ZPY()
    {
        _opAddrAbs = (UInt16)(Read(_pc++) + _y);
        _opAddrAbs &= 0x00FF;
        return 0;
    }

    /// <summary>
    /// Address mode: Relative
    /// </summary>
    /// <returns></returns>
    private Int32 REL()
    {
        _opAddrRel = Read(_pc++);

        if ((_opAddrRel & 0x80) != 0)
            _opAddrRel |= 0xFF00;

        return 0;
    }

    /// <summary>
    /// Address mode: Absolute
    /// </summary>
    private Int32 ABS()
    {
        _opAddrAbs = Read16(_pc);
        _pc += 2;

        return 0;
    }

    /// <summary>
    /// Address mode: Absolute with X Offset
    /// </summary>
    private Int32 ABX()
    {
        var lo = (UInt16)Read(_pc++);
        var hi = (UInt16)Read(_pc++);
        _opAddrAbs = (UInt16)((hi << 8) | lo);
        _opAddrAbs += _x;

        return ((_opAddrAbs & 0xFF00) != (hi << 8)) ? 1 : 0;
    }

    /// <summary>
    /// Address mode: Absolute with Y Offset
    /// </summary>
    private Int32 ABY()
    {
        var lo = (UInt16)Read(_pc++);
        var hi = (UInt16)Read(_pc++);
        _opAddrAbs = (UInt16)((hi << 8) | lo);
        _opAddrAbs += _y;

        return ((_opAddrAbs & 0xFF00) != (hi << 8)) ? 1 : 0;
    }

    /// <summary>
    /// Address mode: Indirect
    /// </summary>
    private Int32 IND()
    {
        var lo = (UInt16)Read(_pc++);
        var hi = (UInt16)Read(_pc++);            
        var ptr = (UInt16)((hi << 8) | lo);

        _opAddrAbs = (UInt16)((Read(ptr + 1) << 8) | Read(ptr + 0));

        return 0;
    }

    /// <summary>
    /// Address mode: Zero Page Indirect with X Offset
    /// </summary>
    private Int32 IZX()
    {
        var t = Read(_pc++);

        var lo = Read((UInt16)(t + _x) & 0x00FF);
        var hi = Read((UInt16)(t + _x + 1) & 0x00FF);
        _opAddrAbs = (UInt16)((hi << 8) | lo);

        return 0;
    }

    /// <summary>
    /// Address mode: Zero Page Indirect with Y Offset
    /// </summary>
    private Int32 IZY()
    {
        var t = Read(_pc++);

        var lo = (UInt16)Read(t & 0x00FF);
        var hi = (UInt16)Read((t + 1) & 0x00FF);

        _opAddrAbs = (UInt16)((hi << 8) | lo);
        _opAddrAbs += _y;

        return (_opAddrAbs & 0xFF00) != (hi << 8) ? 1 : 0;
    }

    /// <summary>
    /// Address mode: Zero Page Indirect
    /// </summary>
    private Int32 IZP()
    {
        var t = Read(_pc++);

        var lo = Read(t & 0x00FF);
        var hi = Read((t + 1) & 0x00FF);
        _opAddrAbs = (UInt16)((hi << 8) | lo);

        return 0;
    }

    /// <summary>
    /// Address mode: Relative with Zero Page Test
    /// </summary>
    private Int32 ZPREL()
    {
        _opData = Read(_pc++);
        _opAddrRel = Read(_pc++);

        if ((_opAddrRel & 0x80) != 0)
            _opAddrRel |= 0xFF00;

        return 0;
    }

    /// <summary>
    /// Address mode: Indirect with X Offset
    /// </summary>
    private Int32 INDX()
    {
        var lo = (UInt16)Read(_pc++);
        var hi = (UInt16)Read(_pc++);
        var ptr = (UInt16)(((hi << 8) | lo) + _x);

        _opAddrAbs = (UInt16)((Read(ptr + 1) << 8) | Read(ptr + 0));

        return 0;
    }

    #endregion
}