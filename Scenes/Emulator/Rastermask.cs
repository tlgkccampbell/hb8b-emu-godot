using Godot;
using System;

public class Rastermask : Viewport
{
    public void ResetRaster()
    {
        GetNode<RastermaskSprite>("RastermaskSprite").ResetRaster();
    }

    public void AdvanceRaster(Int32 pixels)
    {
        GetNode<RastermaskSprite>("RastermaskSprite").AdvanceRaster(pixels);
    }
}
