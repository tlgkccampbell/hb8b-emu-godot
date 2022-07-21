using Godot;
using System;

public class EmulatorFramework : Node
{
    private Hb8bBus _bus;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _bus = new Hb8bBus();
        _bus.Reset();
        
        GetNode<EmulatorCore>("EmulatorCore").Bus = _bus;
        GetNode<EmulatorVideo>("EmulatorVideo").Bus = _bus;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        _bus.ClockUntilFrameIsReady();
    }
}
