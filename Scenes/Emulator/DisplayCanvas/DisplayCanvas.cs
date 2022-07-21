using Godot;
using System;

public class DisplayCanvas : CanvasLayer
{
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_overlay_toggle"))
        {
            var overlay = GetNode<Node2D>("Overlay");
            overlay.Visible = !overlay.Visible;
        }
    }
}
