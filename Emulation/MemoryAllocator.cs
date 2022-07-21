using System;

public static class MemoryAllocator
{
    private static readonly Random _rng = new Random();

    public static Byte GetRandomByte()
    {
        return (Byte)_rng.Next(0, 256);
    }

    public static void FillMemoryBytes(Byte[] buffer, Byte? fill = null)
    {
        if (fill.HasValue)
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = fill.Value;
        }
        else
        {
            _rng.NextBytes(buffer);
        }
    }
}