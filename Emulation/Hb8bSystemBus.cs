using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#nullable enable

namespace Hb8b.Emulation
{
    /// <summary>
    /// The HB8B system bus.
    /// </summary>
    public class Hb8bSystemBus
    {
        private readonly HashSet<Hb8bPeripheral> _irqs = new HashSet<Hb8bPeripheral>();
        private Boolean _nmiRaised;
        private Int32 _clockCyclesUntilNewFrame;
        private Int32 _clockCyclesTotal;
        private Byte _openBusValue = MemoryAllocator.GetRandomByte();

        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bSystemBus"/> class.
        /// </summary>
        public Hb8bSystemBus()
        {
            this.Cpu = new Hb8bCpu(this);
            this.SystemRam = new Hb8bSystemMemory(this, 0x0000, 0x2000);
            this.SystemRom = new Hb8bSystemMemory(this, 0xE000, 0x2000, fill: 0xEA);
            this.SystemRom.Memory[0xFFFC - 0xE000] = 0x00;
            this.SystemRom.Memory[0xFFFD - 0xE000] = 0xE0;
            this.SystemRom.Memory[0xE000 - 0xE000] = 0x4C;
            this.SystemRom.Memory[0xE001 - 0xE000] = 0x00;
            this.SystemRom.Memory[0xE002 - 0xE000] = 0xE0;
            this.Video = new Hb8bVideoCircuit(this);
            this.Reset();            
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
        /// Advances the system clock until the next video frame is available.
        /// </summary>
        public void ClockUntilNextFrame()
        {
            Cpu.Clock(_clockCyclesUntilNewFrame);

            _clockCyclesTotal += _clockCyclesUntilNewFrame;
            _clockCyclesUntilNewFrame = Hb8bVideoCircuit.Timings.TotalSystemClocksPerFrame;
        }

        /// <summary>
        /// Reads a single byte at the specified address.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <returns>The byte that was read from the specified address.</returns>
        public Byte Read(UInt16 address)
        {
            var device = GetDeviceNumber(address);
            switch (device)
            {
                case 0:
                    _openBusValue = SystemRam.Memory[address];
                    break;

                case 7:
                    _openBusValue = SystemRom.Memory[address - SystemRom.Offset];
                    break;
            }

            return _openBusValue;
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
        /// Reads a 16-bit value from an address that is constrained to the zero page.
        /// </summary>
        /// <param name="address">The address from which to read a value.</param>
        /// <returns>The 16-bit value that was read from the specified address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16 Read16ZeroPage(Byte address)
        {
            var sram = SystemRam.Memory;
            var addrLo = sram[(Byte)address];
            var addrHi = sram[(Byte)(address + 1)];
            return (UInt16)((addrHi << 8) | addrLo);
        }

        /// <summary>
        /// Reads an entire page of 256 bytes into the specified buffer.
        /// </summary>
        /// <param name="page">The index of the page to read.</param>
        /// <param name="buffer">The buffer to populate with data from the page.</param>
        public void ReadMemoryPage(Byte page, Byte[] buffer)
        {
            var address = (UInt16)(page * 256);
            var device = GetDeviceNumber(address);
            switch (device)
            {
                case 0:
                    Array.Copy(SystemRam.Memory, address, buffer, 0, 256);
                    break;

                case 7:
                    Array.Copy(SystemRom.Memory, address - SystemRom.Offset, buffer, 0, 256);
                    break;
            }

            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = _openBusValue;
        }

        /// <summary>
        /// Writes a single byte to the specified address.
        /// </summary>
        /// <param name="address">The address to which to write a value.</param>
        /// <param name="value">The value to write to the specified address.</param>
        public void Write(UInt16 address, Byte value)
        {
            _openBusValue = value;

            var device = GetDeviceNumber(address);
            switch (device)
            {
                case 0:
                    SystemRam.Memory[address] = value;
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
        /// Gets the peripheral that represents the system's video generation circuit.
        /// </summary>
        public Hb8bVideoCircuit Video { get; }

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
        /// Gets the device number associated with the specified memory address.
        /// </summary>
        /// <param name="address">The memory address to evaluate.</param>
        /// <returns>The device number associated with the specified memory address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Int32 GetDeviceNumber(UInt16 address)
        {
            return (address & 0xE000) >> 13;
        }
    }
}