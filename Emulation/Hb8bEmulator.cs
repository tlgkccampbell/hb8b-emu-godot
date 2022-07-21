using System;

public class Hb8bEmulator
{
    private readonly Byte[] _vram = new Byte[8 * 1024];

    public Hb8bEmulator()
    {
        var rng = new Random();
        rng.NextBytes(_vram);   
    }

    public Byte[] GetVideoRam()
    {
        return _vram;
    }
}