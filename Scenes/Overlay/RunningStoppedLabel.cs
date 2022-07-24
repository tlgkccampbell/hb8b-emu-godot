using Godot;
using System;

public class RunningStoppedLabel : Label
{
    private EmulatedDevice? _emulatedDevice;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        var emulatedDevice = _emulatedDevice!;

        if (emulatedDevice.Bus.Cpu.IsSuspended)
        {
            Text = "Suspended";
        }
        else
        {
            Text = emulatedDevice.Bus.Cpu.IsStopped ? "Stopped" : "Running";
        }
    }
}
