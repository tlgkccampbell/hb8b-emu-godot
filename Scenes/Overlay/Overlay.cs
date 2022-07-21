using Godot;
using System;

public class Overlay : Control
{
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_overlay_toggle"))
        {
            this.Visible = !this.Visible;
        }
    }
}
