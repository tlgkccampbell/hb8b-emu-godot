using System;
using Godot;

public class EmulatorVideo : Node
{
    private EmulatedDevice? _emulatedDevice;
    private TileMap? _tilemap;
    private TileMap? _colormapBg;
    private TileMap? _colormapFg;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
        _tilemap = GetNode<TileMap>("TileMapViewport/TileMap");
        _colormapBg = GetNode<TileMap>("ColorMapBgViewport/ColorMapBg");
        _colormapFg = GetNode<TileMap>("ColorMapFgViewport/ColorMapFg");

        for (var y = 0; y < 60; y++) 
        {
            for (var x = 0; x < 80; x++)
            {
                _tilemap.SetCell(x, y, 0, autotileCoord: Vector2.Zero);
                _colormapFg.SetCell(x, y, 0, autotileCoord: Vector2.Zero);
                _colormapBg.SetCell(x, y, 0, autotileCoord: Vector2.Zero);
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        const Int32 TileMapWidth = 32;

        var vram = _emulatedDevice!.Bus.Video.Memory;
        var tilemap = _tilemap!;
        var colormapFg = _colormapFg!;
        var colormapBg = _colormapBg!;

        for (var y = 0; y < 30; y++) 
        {
            var addr = y * 128;

            for (var x = 0; x < 80; x++)
            {
                var tileAttrib = vram[addr + 0x2000];

                var tileColorFore = GetForegroundColor(tileAttrib);
                var tileColorForeX = tileColorFore % 16;
                colormapFg.SetCell(x, y, 0, autotileCoord: new Vector2(tileColorForeX, 0));

                var tileColorBack = GetBackgroundColor(tileAttrib);
                var tileColorBackX = tileColorBack % 16;
                colormapBg.SetCell(x, y, 0, autotileCoord: new Vector2(tileColorBackX, 0));

                var tileIndex = vram[addr];
                var tileIndexX = tileIndex % TileMapWidth;
                var tileIndexY = tileIndex / TileMapWidth;
                var tileCoord = new Vector2(tileIndexX, tileIndexY);

                tilemap.SetCell(x, y, 0, autotileCoord: tileCoord);

                addr++;
            }
        }
    }

    private static Int32 GetForegroundColor(Byte attrib) => (attrib & 0b11110000) >> 4;

    private static Int32 GetBackgroundColor(Byte attrib) => (attrib & 0b00001111);
}
