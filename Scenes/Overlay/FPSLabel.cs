using Godot;
using System;

public class FPSLabel : Label
{
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        Text = Engine.GetFramesPerSecond().ToString() + " FPS";
    }
}
