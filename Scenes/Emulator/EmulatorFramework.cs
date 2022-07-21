using Godot;
using System;

public class EmulatorFramework : Node
{
    private Hb8bEmulator _emulator;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _emulator = new Hb8bEmulator();
        GetNode<EmulatorCore>("EmulatorCore").Emulator = _emulator;
        GetNode<EmulatorVideo>("EmulatorVideo").Emulator = _emulator;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {

    }
}
