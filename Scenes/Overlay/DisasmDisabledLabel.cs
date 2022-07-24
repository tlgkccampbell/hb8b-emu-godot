using Godot;
using System;

public class DisasmDisabledLabel : Label
{
    private EmulatedDevice? _emulatedDevice;

    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
    }

    public override void _Process(float delta)
    {
        Visible = !_emulatedDevice!.Bus.Cpu.IsSuspended;
    }
}
