using System;
using System.IO;

public class Hb8bMemoryBlock
{
    private readonly Byte[] _data;

    public Hb8bMemoryBlock(UInt16 position, UInt16 size, Boolean isReadOnly)
    {
        this.Position = position;
        this.Size = size;
        this.IsReadOnly = isReadOnly;

        this._data = new Byte[size];
        MemoryAllocator.FillMemoryBytes(this._data, isReadOnly ? 0xFF : (Byte?)null);
    }

    public Boolean ContainsAddress(UInt16 address) => address >= Position && address < Position + Size;

    public void LoadFromBuffer(Byte[] buffer)
    {
        LoadFromBuffer(buffer, 0, buffer.Length);
    }

    public void LoadFromBuffer(Byte[] buffer, Int32 offset, Int32 count)
    {
        Array.Copy(buffer, offset, _data, 0, Math.Min(count, _data.Length));
    }

    public void LoadFromFile(String path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        var data = File.ReadAllBytes(path);
        LoadFromBuffer(data);
    }

    public void Randomize()
    {
        if (IsReadOnly)
            return;

        MemoryAllocator.FillMemoryBytes(_data);
    }

    public Byte this[Int32 offset]
    {
        get => _data[offset];
        set => _data[offset] = value;
    }

    public Byte[] Data => _data;

    public UInt16 Position { get; }

    public UInt16 Size { get; }

    public Boolean IsReadOnly { get; }
}