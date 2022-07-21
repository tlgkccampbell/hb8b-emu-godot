using System;

public partial class Hb8bVideoCircuit : Hb8bBusPeripheral
{
    private const Int32 VideoCyclesPerVideoFrame = Timings.TotalPixelsPerScanline * Timings.TotalScanlinesPerFrame;
    private const Int32 VideoCyclesPerSystemClock = 8;

    private UInt32 _cycleCounter;

    public Hb8bVideoCircuit(Hb8bBus bus)
        : base(bus)
    { }

    public void HandleFrameDrawn()
    {
        if (!FrameIsReady)
            return;

        FrameIsReady = false;
        FramesElapsed++;
    }

    public void Clock()
    {
        GeneratePixels();

        _cycleCounter += VideoCyclesPerSystemClock;           
        if (_cycleCounter >= VideoCyclesPerVideoFrame)
            _cycleCounter = 0;
    }

    public void Reset()
    {
        _cycleCounter = 0;

        this.FramesElapsed = 0;
        this.PixelX = 0;
        this.PixelY = 0;
        this.Scanline = 0;

        UpdateIntervals();
    }
    
    public UInt64 FramesElapsed { get; private set; }

    public UInt16 PixelX { get; private set; }

    public UInt16 PixelY { get; private set; }
    
    public UInt16 Scanline { get; private set; }

    public Boolean IsVBlank { get; private set; }

    public Boolean IsVSync { get; private set; }

    public Boolean IsHBlank { get; private set; }

    public Boolean IsHSync { get; private set; }

    public Boolean IsVisible { get; private set; }

    public Boolean FrameIsReady { get; private set; }

    private void GeneratePixels()
    {
        var fblank = Bus.Cpu.IsAccessingVram ? 0xFFFFFFFF : 0x00000000;
        this.PixelX += 2;

        if (this.PixelX >= Timings.TotalPixelsPerScanline)
        {
            this.Scanline++;
            this.PixelX = 0;
            this.PixelY += (UInt16)(1 - (this.Scanline % 2));

            if (this.Scanline >= Timings.TotalScanlinesPerFrame)
            {
                this.PixelX = 0;
                this.PixelY = 0;
                this.Scanline = 0;
            }
        }

        if (UpdateIntervals())
        {
            this.FrameIsReady = true;
            this.Bus.Cpu.QueueNonMaskableInterrupt();
        }
    }
    
    private Boolean UpdateIntervals()
    {
        var wasInVBlank = IsVBlank;

        IsVisible = true;

        IsHBlank = PixelX >= Timings.VisiblePixelsPerScanline;
        if (IsHBlank)
        {
            var hsyncStart = Timings.VisiblePixelsPerScanline + Timings.TotalPixelsInHorizontalFrontPorch;
            var hsyncEnd = hsyncStart + Timings.TotalPixelsInHorizontalSync;
            IsHSync = PixelX >= hsyncStart && PixelX < hsyncEnd;
            IsVisible = false;
        }
        else
        {
            IsHSync = false;
        }

        IsVBlank = Scanline >= Timings.VisibleScanlinesPerFrame;
        if (IsVBlank)
        {
            var vsyncStart = Timings.VisibleScanlinesPerFrame + Timings.TotalScanlinesInVerticalFrontPorch;
            var vsyncEnd = vsyncStart + Timings.TotalScanlinesInVerticalSync;
            IsVSync = Scanline >= vsyncStart && Scanline < vsyncEnd;
            IsVisible = false;
        }
        else
        {
            IsVSync = false;
        }

        return !wasInVBlank && IsVBlank;
    }
}