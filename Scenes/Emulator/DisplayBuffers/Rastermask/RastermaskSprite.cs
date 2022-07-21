using Godot;
using System;

public class RastermaskSprite : Sprite
{
    private Vector2 _raster;

    public void ResetRaster()
    {
        _raster = Vector2.Zero;
    }

    public void AdvanceRaster(Int32 pixels)
    {
        var rasterW = Texture.GetWidth();
        var rasterH = Texture.GetHeight();

        while (pixels > 0)
        {
            var advanced = Math.Min(rasterW - (Int32)_raster.x, pixels);
            _raster.x += advanced;
            pixels -= advanced;

            if (pixels > 0) 
            {
                if (_raster.y + 1 >= rasterH)
                    return;

                _raster.x = 0;
                _raster.y++;
            }
        }

        Console.WriteLine(_raster.x);
        Console.WriteLine(_raster.y);

        Update();
    }

    public override void _Draw()
    {
        var rasterW = Texture.GetWidth();
        var rasterH = Texture.GetHeight();
        var rasterX = Math.Max(0, Math.Min((Int32)_raster.x, rasterW));
        var rasterY = Math.Max(0, Math.Min((Int32)_raster.y, rasterH));

        DrawRect(new Rect2(0, 0, rasterW, rasterH), Colors.Black);

        var remaining = (rasterY * rasterW) + rasterX;

        var needsFill = remaining >= rasterW;
        if (needsFill)
        {
            var fillX = 0;
            var fillY = 0;
            var fillW = rasterW;
            var fillH = rasterY;
            DrawRect(new Rect2(fillX, fillY, fillW, fillH), Colors.White);

            remaining -= rasterW * fillH;
        }

        var needsFinalLine = remaining > 0;
        if (needsFinalLine)
        {
            var lineX = 0;
            var lineY = rasterY;
            var lineW = remaining;
            var lineH = 1;
            DrawRect(new Rect2(lineX, lineY, lineW, lineH), Colors.White);
        }
    }
}
