using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hb8b.Emulation
{
    /// <summary>
    /// The HB8B system bus.
    /// </summary>
    public class Hb8bSystemBus
    {
        private readonly HashSet<Hb8bPeripheral> _irqs = new HashSet<Hb8bPeripheral>();
        private Boolean _nmiRaised;
        private UInt32 _clockCyclesUntilNewFrame;
        private UInt64 _clockCyclesTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bSystemBus"/> class.
        /// </summary>
        public Hb8bSystemBus()
        {
            this.Cpu = new Hb8bCpu(this);
            this.SystemRam = new Hb8bSystemMemory(this, 0x0000, 0x4000);
            this.SystemRom = new Hb8bSystemMemory(this, 0xC000, 0x4000, fill: 0xEA);
            this.SystemRom.Memory[0xFFFC - SystemRom.Offset] = 0x00;
            this.SystemRom.Memory[0xFFFD - SystemRom.Offset] = 0xE0;
            this.SystemRom.Memory[0xE000 - SystemRom.Offset] = 0x4C;
            this.SystemRom.Memory[0xE001 - SystemRom.Offset] = 0x00;
            this.SystemRom.Memory[0xE002 - SystemRom.Offset] = 0xE0;
            this.Via0 = new Hb8bVia(this);
            this.Via1 = new Hb8bVia(this);
            this.Video = new Hb8bVideoCircuit(this, 0x4000);
            this.Disassembler = new Disassembler(this);
            this.Reset();            
        }

        /// <summary>
        /// Loads a ROM into the system's address space.
        /// </summary>
        /// <param name="path"></param>
        public void LoadRom(String path)
        {
            var data = System.IO.File.ReadAllBytes(path);

            // ROM contains RAM + ROM
            if (data.Length == SystemRam.Memory.Length + SystemRom.Memory.Length)
            {
                Array.Copy(data, SystemRam.Memory, SystemRam.Memory.Length);
                Array.Copy(data, SystemRam.Memory.Length, SystemRom.Memory, 0, SystemRom.Memory.Length);

                return;
            }

            // ROM contains ROM only
            if (data.Length == SystemRom.Memory.Length)
            {
                Array.Copy(data, SystemRom.Memory, data.Length);

                return;
            }

            throw new InvalidOperationException("ROM size mismatch.");
        }

        /// <summary>
        /// Resets the state of the bus and all of its attached peripherals.
        /// </summary>
        public void Reset()
        {
            _irqs.Clear();
            _nmiRaised = false;

            Cpu.Reset();
            SystemRam.Reset();
            SystemRom.Reset();
            Video.Reset();

            _clockCyclesUntilNewFrame = Hb8bVideoCircuit.Timings.TotalSystemClocksPerFrame;
            _clockCyclesTotal = 0;
        }

        /// <summary>
        /// Advances the system clock by the specified number of cycles.
        /// </summary>
        /// <param name="cycles">The number of cycles by which the system clock was advanced.</param>
        /// <param name="stepped">A value indicating whether the system clock is being manually stepped forward.</param>
        public void Clock(UInt32 cycles, Boolean stepped)
        {
            var executed = Cpu.Clock(cycles, stepped);

            _clockCyclesTotal += executed;
            _clockCyclesUntilNewFrame -= executed;

            while (_clockCyclesUntilNewFrame <= 0)
                _clockCyclesUntilNewFrame += Hb8bVideoCircuit.Timings.TotalSystemClocksPerFrame;
        }

        /// <summary>
        /// Clocks the bus' (non-CPU) peripherals by the specified number of cycles.
        /// </summary>
        /// <param name="cycles">The maximum number of cycles to clock.</param>
        /// <returns>The number of cycles that were clocked.</returns>
        public UInt32 ClockPeripherals(UInt32 cycles)
        {
            Video.Clock(cycles);
            Via0.Clock(cycles);
            Via1.Clock(cycles);

            return cycles;
        }

        /// <summary>
        /// Clocks the bus' (non-CPU) peripherals until an interrupt is raised.
        /// </summary>
        /// <param name="cycles">The maximum number of cycles to clock.</param>
        /// <returns>The number of cycles that were clocked.</returns>
        public UInt32 ClockPeripheralsUntilInterrupt(UInt32 cycles)
        {
            var reqCyclesVideo = Video.GetCyclesUntilNextInterrupt(cycles);
            var reqCyclesVia0 = Via0.GetCyclesUntilNextInterrupt(cycles);
            var reqCyclesVia1 = Via1.GetCyclesUntilNextInterrupt(cycles);
            var reqCycles = Math.Min(reqCyclesVideo, Math.Min(reqCyclesVia0, reqCyclesVia1));
            if (reqCycles <= cycles)
            {
                Video.Clock(reqCycles);
                Via0.Clock(reqCycles);
                Via1.Clock(reqCycles);

                return reqCycles;
            }
            else
            {
                return ClockPeripherals(cycles);
            }
        }

        /// <summary>
        /// Advances the system clock until the next video frame is available.
        /// </summary>
        public void ClockUntilNextFrame()
        {
            Clock(_clockCyclesUntilNewFrame, false);
        }

        /// <summary>
        /// Advances the system clock by one cycle.
        /// </summary>
        public void SingleStepCycle()
        {
            Clock(1, true);
        }

        /// <summary>
        /// Advances the system clock until the start of the next instruction.
        /// </summary>
        public void SingleStepInstruction()
        {
            Clock(0, true);
        }

        /// <summary>
        /// Reads a single byte at the specified address.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <param name="peek">A value indicating whether the read should avoid modifying peripheral state.</param>
        /// <returns>The byte that was read from the specified address.</returns>
        public Byte Read(UInt16 address, Boolean peek = false)
        {
            var device = GetDeviceNumber(address);
            switch (device)
            {
                case 0:
                    if (IsRegisterSpace(address))
                    {
                        switch (GetRegisterDeviceNumber(address))
                        {
                            case 0:
                                OpenBusValue = Via0.Read((Byte)(address & 0xF), peek);
                                break;

                            case 1:
                                OpenBusValue = Via1.Read((Byte)(address & 0xF), peek);
                                break;
                        }
                    }
                    else
                    {
                        OpenBusValue = SystemRam.Memory[address];
                    }
                    break;

                case 1:
                    OpenBusValue = SystemRam.Memory[address];
                    break;

                case 2:
                case 3:
                    OpenBusValue = Video.Memory[address - Video.Offset];
                    break;

                case 6:
                case 7:
                    OpenBusValue = SystemRom.Memory[address - SystemRom.Offset];
                    break;
            }

            return OpenBusValue;
        }

        /// <summary>
        /// Reads a 16-bit value at the specified address.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <returns>The 16-bit value that was read from the specified address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 Read16(UInt16 address)
        {
            var addrLo = Read(address);
            var addrHi = Read((UInt16)(address + 1));
            return (UInt16)((addrHi << 8) | addrLo);
        }

        /// <summary>
        /// Reads a 16-bit value at the specified address, and advances the value of the <param ref="address"/> parameter.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <returns>The 16-bit value that was read from the specified address.</returns>
        public UInt16 Read16(ref UInt16 address)
        {
            var addrLo = Read(address);
            var addrHi = Read((UInt16)(address + 1));
            address += 2;
            return (UInt16)((addrHi << 8) | addrLo);
        }

        /// <summary>
        /// Reads a 16-bit value from an address that is constrained to the zero page.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <returns>The 16-bit value that was read from the specified address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 Read16ZeroPage(Byte address)
        {
            var addrLo = Read((Byte)address);
            var addrHi = Read((Byte)(address + 1));
            return (UInt16)((addrHi << 8) | addrLo);
        }

        /// <summary>
        /// Reads an entire page of 256 bytes into the specified buffer.
        /// </summary>
        /// <param name="page">The index of the page to read.</param>
        /// <param name="buffer">The buffer to populate with data from the page.</param>
        /// <param name="peek">A value indicating whether the read should avoid modifying peripheral state.</param>
        public void ReadMemoryPage(Byte page, Byte[] buffer, Boolean peek = false)
        {
            var address = (UInt16)(page * 256);
            var device = GetDeviceNumber(address);
            switch (device)
            {
                case 0:
                    for (var i = 0; i < buffer.Length; i++)
                        buffer[i] = Read((UInt16)(address + i), peek);
                    break;

                case 1:
                    Array.Copy(SystemRam.Memory, address, buffer, 0, 256);
                    break;

                case 2:
                case 3:
                    Array.Copy(Video.Memory, address - Video.Offset, buffer, 0, 256);
                    break;

                case 6:
                case 7:
                    Array.Copy(SystemRom.Memory, address - SystemRom.Offset, buffer, 0, 256);
                    break;
            }

            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = OpenBusValue;
        }

        /// <summary>
        /// Writes a single byte to the specified address.
        /// </summary>
        /// <param name="address">The address to which to write a value.</param>
        /// <param name="value">The value to write to the specified address.</param>
        public void Write(UInt16 address, Byte value)
        {
            OpenBusValue = value;

            var device = GetDeviceNumber(address);
            switch (device)
            {
                case 0:
                    if (IsRegisterSpace(address))
                    {
                        switch (GetRegisterDeviceNumber(address))
                        {
                            case 0:
                                Via0.Write((Byte)(address & 0xF), value);
                                break;

                            case 1:
                                Via1.Write((Byte)(address & 0xF), value);
                                break;
                        }
                    }
                    else
                    {
                        SystemRam.Memory[address] = value;
                    }
                    break;

                case 1:
                    SystemRam.Memory[address] = value;
                    break;

                case 2:
                case 3:
                    Video.Memory[address - Video.Offset] = value;
                    break;

                case 6:
                case 7:
                    SystemRom.Memory[address - SystemRom.Offset] = value;
                    break;
            }
        }

        /// <summary>
        /// Asserts an interrupt request from the specified peripheral.
        /// </summary>
        /// <param name="peripheral">The peripheral that is asserting an interrupt request.</param>
        /// <returns><see langword="true"/> if the interrupt request was asserted; otherwise, <see langword="false"/>.</returns>
        public Boolean AssertIrq(Hb8bPeripheral peripheral)
        {
            return _irqs.Add(peripheral);
        }

        /// <summary>
        /// Releases an interrupt request from the specified peripheral.
        /// </summary>
        /// <param name="peripheral">The peripheral that is releasing an interrupt request.</param>
        /// <returns><see langword="true"/> if the interrupt request was released; otherwise, <see langword="false"/>.</returns>
        public Boolean ReleaseIrq(Hb8bPeripheral peripheral)
        {
            return _irqs.Remove(peripheral);
        }

        /// <summary>
        /// Raises a non-maskable interrupt.
        /// </summary>
        /// <returns><see langword="true"/> if an interrupt was raised; otherwise, <see langword="false"/>.</returns>
        public Boolean RaiseNmi()
        {
            if (_nmiRaised)
                return false;

            _nmiRaised = true;
            return true;
        }

        /// <summary>
        /// Clears a non-maskable interrupt.
        /// </summary>
        /// <returns><see langword="true"/> if an interrupt was cleared; otherwise, <see langword="false"/>.</returns>
        public Boolean ClearNmi()
        {
            if (_nmiRaised)
            {
                _nmiRaised = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the peripheral that represents the system's central processing unit.
        /// </summary>
        public Hb8bCpu Cpu { get; }

        /// <summary>
        /// Gets the peripheral that represents the main system RAM.
        /// </summary>
        public Hb8bSystemMemory SystemRam { get; }

        /// <summary>
        /// Gets the peripheral that represents the main system ROM.
        /// </summary>
        public Hb8bSystemMemory SystemRom { get; }

        /// <summary>
        /// Gets the first VIA chip.
        /// </summary>
        public Hb8bVia Via0 { get; }

        /// <summary>
        /// Gets the second VIA chip.
        /// </summary>
        public Hb8bVia Via1 { get; }

        /// <summary>
        /// Gets the peripheral that represents the system's video generation circuit.
        /// </summary>
        public Hb8bVideoCircuit Video { get; }

        /// <summary>
        /// Gets the bus' disassembler.
        /// </summary>
        public Disassembler Disassembler { get; }

        /// <summary>
        /// Gets or sets the value that is read when reading from an open bus.
        /// </summary>
        public Byte OpenBusValue { get; set; } = MemoryAllocator.GetRandomByte();

        /// <summary>
        /// Gets a value indicating whether any interrupts have been requested.
        /// </summary>
        public Boolean IsAnyInterruptRequested => IsIrqAsserted || IsNmiRaised;

        /// <summary>
        /// Gets a value indicating whether any peripherals have requested an interrupt.
        /// </summary>
        public Boolean IsIrqAsserted => _irqs.Count > 0;

        /// <summary>
        /// Gets a value indicating whether any peripherals have raised a non-maskable interrupt.
        /// </summary>
        public Boolean IsNmiRaised => _nmiRaised;

        /// <summary>
        /// Gets the total number of clock cycles that the system has executed since reset.
        /// </summary>
        public UInt64 TotalClockCyclesExecuted => _clockCyclesTotal;

        /// <summary>
        /// Gets a value indicating whether the specified address is in register space.
        /// Note that this method DOES NOT check whether the address is in device block 0.
        /// </summary>
        /// <param name="address">The memory address to evaluate.</param>
        /// <returns><see langword="true"/> if the specified memory address is in register space; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Boolean IsRegisterSpace(UInt16 address)
        {
            return (address & 0x380) == 0x80;
        }

        /// <summary>
        /// Gets the device number associated with the specified memory address.
        /// </summary>
        /// <param name="address">The memory address to evaluate.</param>
        /// <returns>The device number associated with the specified memory address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Int32 GetDeviceNumber(UInt16 address)
        {
            return (address & 0xE000) >> 13;
        }

        /// <summary>
        /// Gets the register device number associated with the specified memory address.
        /// </summary>
        /// <param name="address">The memory address to evaluate.</param>
        /// <returns>The register device number associated with the specified memory address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Int32 GetRegisterDeviceNumber(UInt16 address)
        {
            return (address & 0x70) >> 4;
        }
    }
}