using Godot;
using System;

[Tool]
public class MemoryPageDisplay : Control
{
	private const Int32 ItemsPerRow = 16;
	private const Int32 RowsPerPage = 16;

	private EmulatedDevice? _emulatedDevice;

	private Font? _font;
	private Int32 _fontHeight;
	private Int32 _elementSpacing;
	private Int32 _hexAddrWidth;
	private Int32 _hexPairWidth;
	private Int32 _page = 0;

	public override void _Ready()
	{
		_emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");

		// Assume a monospace font
		_font = GetFont("font");
		_fontHeight = (Int32)Math.Ceiling(_font.GetHeight());
		_elementSpacing = (Int32)Math.Ceiling(_font.GetStringSize(" ").x);
		_hexPairWidth = (Int32)Math.Ceiling(_font.GetStringSize("$FF").x);
		_hexAddrWidth = (Int32)Math.Ceiling(_font.GetStringSize("$FFFF:").x);
	}

	public override void _Draw()
	{
		var font = _font!;

		var address = (UInt16)(256 * _page);
		var position = new Vector2(0, font.GetAscent());
		for (var y = 0; y < RowsPerPage; y++)
		{
			DrawString(_font, position, $"${address:X4}:");
			position.x += _hexAddrWidth + _elementSpacing;

			for (var x = 0; x < ItemsPerRow; x++)
			{
				var value = _emulatedDevice?.Bus.Read((UInt16)address) ?? 0;
				DrawString(_font, position, $"${value:X2}");
				position.x += _hexPairWidth + _elementSpacing;
				address++;
			}

			position.x = 0;
			position.y = position.y + _fontHeight + (_elementSpacing / 2);
		}
	}

    public override void _Process(float delta)
    {
        if (!Engine.EditorHint)
            Update();
    }

    public override Vector2 _GetMinimumSize()
	{
		var w = _hexAddrWidth + (ItemsPerRow * (_hexPairWidth + _elementSpacing));
		var h = (RowsPerPage * _fontHeight) + ((RowsPerPage - 1) * (_elementSpacing / 2));
		return new Vector2(w, h);
	}

	private void _on_SpinBox_value_changed(float value)
	{
		_page = Math.Max(0, Math.Min(255, (Int32)value));
		Update();
	}
}
