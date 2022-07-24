using Godot;
using System;

public class EmulatedDevice : Node
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Bus.Reset();
    }

    public Hb8b.Emulation.Hb8bSystemBus Bus { get; } = new Hb8b.Emulation.Hb8bSystemBus();
}
