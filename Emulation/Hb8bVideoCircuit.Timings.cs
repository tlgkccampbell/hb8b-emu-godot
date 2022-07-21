using System;

public partial class Hb8bVideoCircuit : Hb8bBusPeripheral
{
    public class Timings
    {
        public const Int32 ClocksPerPixel = 1;

        public const Int32 ScanlinesPerPixel = 1;

        public const Int32 TotalPixelsPerScanline = 800;

        public const Int32 TotalPixelsInHorizontalFrontPorch = 16;

        public const Int32 TotalPixelsInHorizontalSync = 96;

        public const Int32 TotalScanlinesPerFrame = 525;

        public const Int32 TotalScanlinesInVerticalFrontPorch = 10;

        public const Int32 TotalScanlinesInVerticalSync = 2;

        public const Int32 VisiblePixelsPerScanline = 640;

        public const Int32 VisibleScanlinesPerFrame = 480;

        public const Int32 VisibleWidthInPixels = VisiblePixelsPerScanline;

        public const Int32 VisibleHeightInPixels = VisibleScanlinesPerFrame / ScanlinesPerPixel;
    }
}