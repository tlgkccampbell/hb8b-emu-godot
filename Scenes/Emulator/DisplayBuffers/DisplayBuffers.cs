using Godot;
using System;

public class DisplayBuffers : Node
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    private static readonly Random _rng = new Random();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var tilemap1 = GetNode<Viewport>("Framebuffer1").GetNode<TileMap>("TileMap");
        var tilemap2 = GetNode<Viewport>("Framebuffer2").GetNode<TileMap>("TileMap");

        Console.WriteLine(tilemap2.GetCell(0, 0));

        for (var y = 0; y < 60; y++) 
        {
            for (var x = 0; x < 80; x++)
            {
                tilemap1.SetCell(x, y, 0, autotileCoord: Vector2.Zero);
                tilemap2.SetCell(x, y, 0, autotileCoord: Vector2.Zero);
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        var tilemap1 = GetNode<Viewport>("Framebuffer1").GetNode<TileMap>("TileMap");
        var tilemap2 = GetNode<Viewport>("Framebuffer2").GetNode<TileMap>("TileMap");

        for (var y = 0; y < 60; y++) 
        {
            for (var x = 0; x < 80; x++)
            {
                tilemap1.SetCell(x, y, 0, autotileCoord: new Vector2(1, 0));
                tilemap2.SetCell(x, y, 0, autotileCoord: new Vector2(1, 1));
            }
        }
    }
}
