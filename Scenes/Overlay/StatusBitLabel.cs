using Godot;
using System;

[Tool]
public class StatusBitLabel : Label
{
    private EmulatedDevice? _emulatedDevice;
    private Hb8b.Emulation.Hb8bCpuStatusFlags _statusBit;

    [Export(PropertyHint.Enum)]
    private Hb8b.Emulation.Hb8bCpuStatusFlags StatusBit
    {
        get => _statusBit;
        set
        {
            _statusBit = value;
            Text = value.ToString();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (!Engine.EditorHint)
        {
            var status = _emulatedDevice!.Bus.Cpu.StatusRegister;
            var set = (status & (Byte)_statusBit) != 0;
            AddColorOverride("font_color", set ? Colors.White : Colors.DimGray);
        }
    }
}
