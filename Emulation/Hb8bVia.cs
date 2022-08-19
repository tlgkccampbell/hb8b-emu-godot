using System;
using System.Runtime.CompilerServices;

namespace Hb8b.Emulation
{
    /// <summary>
    /// One of the HB8B's VIA chips.
    /// </summary>
    public partial class Hb8bVia : Hb8bPeripheral
    {
        // Timer 1.
        private Boolean _timer1Running;
        private UInt16 _timer1Counter;
        private UInt16 _timer1Latch;

        // Timer 2.
        private Boolean _timer2Running;
        private UInt16 _timer2Counter;
        private UInt16 _timer2Latch;

        // Control registers.
        private Byte _acr;
        private Byte _pcr;

        // Interrupts.
        private Byte _ifr;
        private Byte _ier;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bVia" class.
        /// </summary>
        /// <param name="bus">The system bus to which the video circuit is attached.</param>
        /// <param name="offset">The video circuit's offset within the system's memory map.</param>
        public Hb8bVia(Hb8bSystemBus bus)
            : base(bus)
        { }

        /// <summary>
        /// Reads from one of the VIA's registers.
        /// </summary>
        /// <param name="register">The index of the register to read. This value wraps around to 0 if it exceeds 15.</param>
        /// <param name="peek">A value indicating whether the read should avoid modifying peripheral state.</param>
        /// <returns>The value read from the specified register.</returns>
        public Byte Read(Byte register, Boolean peek = false)
        {
            switch ((Hb8bViaRegister)(register & 0b1111))
            {
                case Hb8bViaRegister.T1CL:
                    // 8 bits from T1 low-order counter transferred to MPU. T1 interrupt
                    // flag IFR6 is reset.
                    if (!peek)
                    {
                        ClrInterrupt(Hb8bViaInterrupt.Timer1);
                    }
                    return (Byte)_timer1Counter;

                case Hb8bViaRegister.T1CH:
                    // 8 bits from T1 high-order counter transferred to MPU.
                    return (Byte)((_timer1Counter & (UInt16)0xFF00) >> 8);

                case Hb8bViaRegister.T1LL:
                    // 8 bits from T1 low-order latches transferred to MPU. Unlike reading the T1
                    // Low Order Register, this does not cause reset of T1 interrupt flag IFR6.
                    return (Byte)_timer1Latch;

                case Hb8bViaRegister.T1LH:
                    // 8 bits from T1 high-order counter transferred to MPU.
                    // NOTE: Documentation mistake?
                    return (Byte)((_timer1Latch & (UInt16)0xFF00) >> 8);

                case Hb8bViaRegister.T2CL:
                    // 8 bits from T2 low-order counter transferred to MPU. IFR5 is reset.
                    if (!peek)
                    {
                        ClrInterrupt(Hb8bViaInterrupt.Timer2);
                    }
                    return (Byte)_timer2Counter;

                case Hb8bViaRegister.T2CH:
                    // 8 bits from T2 high-order counter transferred to MPU.
                    return (Byte)((_timer2Counter & (UInt16)0xFF00) >> 8);

                case Hb8bViaRegister.ACR:
                    return _acr;

                case Hb8bViaRegister.PCR:
                    return _pcr;

                case Hb8bViaRegister.IER:
                    return (Byte)(_ier | 0x80);

                case Hb8bViaRegister.IFR:
                    return _ifr;

                default:
                    return 0xFF;
            }
        }
        
        /// <summary>
        /// Writes to one of the VIA's registers.
        /// </summary>
        /// <param name="register">The index of the register to write. This value wraps around to 0 if it exceeds 15.</param>
        /// <param name="value">The value written to the specified register.</param>
        public void Write(Byte register, Byte value)
        {
            switch ((Hb8bViaRegister)(register & 0b1111))
            {
                case Hb8bViaRegister.T1CL:
                    // 8 bits loaded into T1 low-order latches. Latch contents are transferred
                    // into low-order counter at the time the high-order counter is loaded.
                    _timer1Latch = (UInt16)((_timer1Latch & (UInt16)0xFF00) | value);
                    break;

                case Hb8bViaRegister.T1CH:
                    // 8 bits loaded into T1 high-order latches. Also, both high and low-order
                    // latches are transferred into T1 counter and this initiates T1 countdown.
                    // T1 interrupt flag IFR6 is reset.
                    _timer1Latch = (UInt16)((value << 8) | (Byte)_timer1Latch);
                    _timer1Counter = _timer1Latch;
                    _timer1Running = true;
                    ClrInterrupt(Hb8bViaInterrupt.Timer1);
                    break;

                case Hb8bViaRegister.T1LL:
                    // 8 bits loaded into T! low-order latches. The operation is no different than
                    // a write into the T1 Low Order Register.
                    _timer1Latch = (UInt16)((_timer1Latch & (UInt16)0xFF00) | value);
                    break;

                case Hb8bViaRegister.T1LH:
                    // 8 bits loaded into T1 high-order latches. Unlike writing to the T1 Low Order Register,
                    // no latch-to-counter transfers take place. T1 interrupt flag IFR6 is reset.
                    _timer1Latch = (UInt16)((value << 8) | (Byte)_timer1Latch);
                    ClrInterrupt(Hb8bViaInterrupt.Timer1);
                    break;
                
                case Hb8bViaRegister.T2CL:
                    // 8 bits loaded into T2 low-order latches.
                    _timer2Latch = (UInt16)((_timer2Latch & (UInt16)0xFF00) | value);
                    break;

                case Hb8bViaRegister.T2CH:
                    // 8 bits loaded into T2 high-order counter. Also, low-order latches are transferred to low
                    // order counter. IFR5 is reset.
                    _timer2Latch = (UInt16)((value << 8) | (Byte)_timer2Latch);
                    _timer2Counter = _timer2Latch;
                    _timer2Running = true;
                    ClrInterrupt(Hb8bViaInterrupt.Timer2);
                    break;

                case Hb8bViaRegister.ACR:
                    _acr = value;
                    break;

                case Hb8bViaRegister.PCR:
                    _pcr = value;
                    break;

                case Hb8bViaRegister.IFR:
                    {
                        if ((value & 0x80) != 0)
                        {
                            value = 0x7F;
                        }
                        _ifr &= (Byte)~value;
                        CalculateIrqFlag();
                    }
                    break;

                case Hb8bViaRegister.IER:
                    {
                        if ((value & 0x80) != 0)
                        {
                            _ier |= (Byte)(value & 0x7F);
                        }
                        else
                        {
                            _ier &= (Byte)~(value & 0x7F);
                        }
                        CalculateIrqFlag();
                    }
                    break;

                default:
                    throw new ArgumentException("Invalid register.");
            }
        }

        /// <summary>
        /// Clocks the VIA by the specified number of cycles.
        /// </summary>
        public void Clock(UInt32 cycles)
        {
            // Clock Timer 1.
            if (_timer1Running)
            {
                _timer1Counter = (UInt16)((cycles > _timer1Counter) ? 0 : _timer1Counter - cycles);
                if (_timer1Counter == 0)
                {
                    var timer1IsOneShot = (_acr & 0b01000000) == 0;
                    if (timer1IsOneShot)
                    {
                        _timer1Running = false;
                    }
                    else
                    {
                        _timer1Counter = _timer1Latch;
                    }
                    SetInterrupt(Hb8bViaInterrupt.Timer1);
                }
            }

            // Clock Timer 2.
            if (_timer2Running)
            {
                var timer2IsOneShot = (_acr & 0b00100000) == 0;
                if (timer2IsOneShot)
                {
                    _timer2Counter = (UInt16)((cycles > _timer2Counter) ? 0 : _timer2Counter - cycles);
                    if (_timer2Counter == 0)
                    {
                        SetInterrupt(Hb8bViaInterrupt.Timer2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of cycles that must be clocked until the peripheral, in its current configuration,
        /// would raise an interrupt.
        /// </summary>
        /// <param name="maxCyclesToEvaluate">The maximum number of cycles to consider.</param>
        /// <returns>The number of cycles until the next interrupt, or <see langword="UInt32.MaxValue"/> if no interrupt would be raised.</returns>
        public UInt32 GetCyclesUntilNextInterrupt(UInt32 maxCyclesToEvaluate)
        {
            var timer2IsOneShot = (_acr & 0b00100000) == 0;

            var timer = Math.Min(
                _timer1Running ? _timer1Counter : UInt32.MaxValue,
                _timer2Running && timer2IsOneShot ? _timer2Counter : UInt32.MaxValue);

            if (timer < maxCyclesToEvaluate)
            {
                return timer;
            }

            return UInt32.MaxValue;
        }

        /// <summary>
        /// Sets the specified interrupt flag.
        /// </summary>
        /// <param name="flag">The index of the interrupt flag to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetInterrupt(Hb8bViaInterrupt flag)
        {
            Bitwise.Set(ref _ifr, (Int32)flag);
            CalculateIrqFlag();
        }

        /// <summary>
        /// Clears the specified interrupt flag.
        /// </summary>
        /// <param name="flag">The index of the interrupt flag to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClrInterrupt(Hb8bViaInterrupt flag)
        {
            Bitwise.Clr(ref _ifr, (Int32)flag);
            CalculateIrqFlag();
        }

        /// <summary>
        /// Calculates the value of the IRQ flag based on the contents of IFR and IER.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateIrqFlag()
        {
            var irq = (_ifr & _ier & 0x7F) != 0;
            if (irq)
            {
                Bitwise.Set(ref _ifr, (Int32)Hb8bViaInterrupt.IRQ);
                Bus.AssertIrq(this);
            }
            else
            {
                Bitwise.Clr(ref _ifr, (Int32)Hb8bViaInterrupt.IRQ);
                Bus.ReleaseIrq(this);
            }
        }
    }
}