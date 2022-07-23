using Godot;
using System;

public class EmulatorCore : Node
{
    public Hb8b.Emulation.Hb8bSystemBus Bus { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        
    }
}
