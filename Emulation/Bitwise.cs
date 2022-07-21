using System;

public static class Bitwise
{
    public static Boolean IsSet(ref Byte value, Int32 bit)
    {
        if (bit < 0 || bit > 7)
            throw new ArgumentOutOfRangeException(nameof(bit));

        return (value & (1 << bit)) != 0;
    }

    public static Boolean Toggle(ref Byte value, Int32 bit)
    {
        if (bit < 0 || bit > 7)
            throw new ArgumentOutOfRangeException(nameof(bit));

        if (IsSet(ref value, bit))
        {
            Clear(ref value, bit);
            return false;
        }
        else
        {
            Set(ref value, bit);
            return true;
        }
    }

    public static void Clear(ref Byte value, Int32 bit)
    {
        if (bit < 0 || bit > 7)
            throw new ArgumentOutOfRangeException(nameof(bit));

        value &= (Byte)~(1 << bit);
    }
    
    public static void ClearMask(ref Byte value, Byte mask)
    {
        value &= (Byte)~mask;
    }

    public static void Set(ref Byte value, Int32 bit)
    {
        if (bit < 0 || bit > 7)
            throw new ArgumentOutOfRangeException(nameof(bit));

        value |= (Byte)(1 << bit);
    }

    public static void SetMask(ref Byte value, Byte mask)
    {
        value |= mask;
    }

    public static void SetOrClear(ref Byte value, Int32 bit, Boolean state)
    {
        if (state)
            Set(ref value, bit);
        else
            Clear(ref value, bit);
    }

    public static void SetOrClearMask(ref Byte value, Byte mask, Boolean state)
    {
        if (state)
            SetMask(ref value, mask);
        else
            ClearMask(ref value, mask);
    }
}