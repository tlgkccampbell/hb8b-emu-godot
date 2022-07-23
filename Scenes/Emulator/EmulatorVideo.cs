using Godot;
using System;

public class EmulatorVideo : Node
{
    public Hb8b.Emulation.Hb8bSystemBus Bus { get; set; }

    private TileMap _tilemap;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _tilemap = GetNode<TileMap>("TileMap");

        for (var y = 0; y < 60; y++) 
        {
            for (var x = 0; x < 80; x++)
            {
                _tilemap.SetCell(x, y, 0, autotileCoord: Vector2.Zero);
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        const Int32 TileMapWidth = 32;

        var vram = Bus.Video.Memory;

        for (var y = 0; y < 60; y++) 
        {
            for (var x = 0; x < 80; x++)
            {
                var tileIndex = vram[(y * 128) + x];
                var tileIndexX = tileIndex % TileMapWidth;
                var tileIndexY = tileIndex / TileMapWidth;
                var tileCoord = new Vector2(tileIndexX, tileIndexY);

                _tilemap.SetCell(x, y, 0, autotileCoord: tileCoord);
            }
        }
    }
}
