using System;

namespace Hb8b.Emulation
{
    public partial class Hb8bVideoCircuit
    {
        /// <summary>
        /// Contains the frame timings used by the video generation circuit.
        /// </summary>
        public static class Timings
        {
            /// <summary>
            /// The number of pixel clock cycles per each system clock cycle.
            /// </summary>
            public const Int32 PixelClocksPerSystemClock = 4;

            /// <summary>
            /// The total number of pixels on a scanline, including the front and back porches.
            /// </summary>
            public const Int32 TotalPixelsPerScanline = 800;

            /// <summary>
            /// The number of pixels in the horizontal front porch.
            /// </summary>
            public const Int32 TotalPixelsInHorizontalFrontPorch = 16;

            /// <summary>
            /// The number of pixels in the horizontal sync pulse.
            /// </summary>
            public const Int32 TotalPixelsInHorizontalSync = 96;

            /// <summary>
            /// The number of pixels in the horizontal back porch.
            /// </summary>
            public const Int32 TotalPixelsInHorizontalBackPorch = 48;

            /// <summary>
            /// The total number of visible pixels in a scanline.
            /// </summary>
            public const Int32 VisiblePixelsPerScanline = TotalPixelsPerScanline -
                (TotalPixelsInHorizontalFrontPorch + TotalPixelsInHorizontalSync + TotalPixelsInHorizontalBackPorch);

            /// <summary>
            /// The total number of scanlines in a frame.
            /// </summary>
            public const Int32 TotalScanlinesPerFrame = 525;

            /// <summary>
            /// The total number of scanlines in the vertical front porch.
            /// </summary>
            public const Int32 TotalScanlinesInVerticalFrontPorch = 10;

            /// <summary>
            /// The total number of scalines in the vertical sync pulse.
            /// </summary>
            public const Int32 TotalScanlinesInVerticalSync = 2;

            /// <summary>
            /// The total number of scanlines in the vertical back porch.
            /// </summary>
            public const Int32 TotalScanlinesInVerticalBackPorch = 33;

            /// <summary>
            /// The total number of visible scanlines in a frame.
            /// </summary>
            public const Int32 VisibleScanlinesPerFrame = TotalScanlinesPerFrame -
                (TotalScanlinesInVerticalFrontPorch + TotalScanlinesInVerticalSync + TotalScanlinesInVerticalBackPorch);

            /// <summary>
            /// The total number of pixels in a frame.
            /// </summary>
            public const Int32 TotalPixelsPerFrame = TotalPixelsPerScanline * TotalScanlinesPerFrame;

            /// <summary>
            /// The total number of system clocks in each frame.
            /// </summary>
            public const Int32 TotalSystemClocksPerFrame = TotalPixelsPerFrame / PixelClocksPerSystemClock;
        }
    }
}