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
    private Int32 _lineSpacing;
    private Int32 _hexAddrWidth;
	private Int32 _hexPairWidth;
	private Int32 _page = 0;

	public override void _Ready()
	{
		_emulatedDevice = Engine.EditorHint ? null : GetNode<EmulatedDevice>("/root/EmulatedDevice");

		// Assume a monospace font
		_font = GetFont("font");
		_fontHeight = (Int32)Math.Ceiling(_font.GetHeight());
		_elementSpacing = (Int32)Math.Ceiling(_font.GetStringSize(" ").x);
        _lineSpacing = _elementSpacing / 2;
		_hexAddrWidth = (Int32)Math.Ceiling(_font.GetStringSize("$FFFF").x);
        _hexPairWidth = (Int32)Math.Ceiling(_font.GetStringSize("$FF").x);
	}

	public override void _Draw()
	{
		var font = _font!;
        var fontAscent = font.GetAscent();

		// Draw hex offsets at the top row.
        var position = new Vector2(_hexAddrWidth + _elementSpacing, fontAscent);
        for (var i = 0; i < 16; i++)
        {
            DrawString(_font, position, $"${i:X2}", Colors.Yellow);
            position.x += _hexPairWidth + _elementSpacing;
        }

        position.x = 0;
        position.y += _fontHeight + _lineSpacing;

        // Draw the memory table.
        var address = (UInt16)(256 * _page);
		for (var y = 0; y < RowsPerPage; y++)
		{
			DrawString(_font, position, $"${address:X4}", Colors.Yellow);
			position.x += _hexAddrWidth + _elementSpacing;

			for (var x = 0; x < ItemsPerRow; x++)
			{
                if (_emulatedDevice == null)
                {
					// Draw all $00 in the editor.
                    var value = 0;
                    DrawString(_font, position, $"${value:X2}");
                }
                else
                {
                    const Int32 ValueBoundsPadding = 2;
                    var valueBounds = new Rect2(position.x - ValueBoundsPadding, position.y - (fontAscent + ValueBoundsPadding),
						_hexPairWidth + (ValueBoundsPadding * 2), _fontHeight + (ValueBoundsPadding * 2));

                    // Highlight the program counter address.
                    if (address == _emulatedDevice.Bus.Cpu.ProgramCounter)
                        DrawRect(valueBounds, Colors.Cornflower);

                    // Draw the memory value.
                    var value = _emulatedDevice.Bus.Read(address);
					DrawString(_font, position, $"${value:X2}");
                }

				position.x += _hexPairWidth + _elementSpacing;
				address++;
			}

			position.x = 0;
            position.y = position.y + _fontHeight + _lineSpacing;
        }
	}

    public override void _Process(float delta)
    {
        if (!Engine.EditorHint)
            Update();
    }

    public override Vector2 _GetMinimumSize()
	{
        var rows = RowsPerPage + 1;
        var w = _hexAddrWidth + (ItemsPerRow * (_hexPairWidth + _elementSpacing));
		var h = (rows * _fontHeight) + ((rows - 1) * (_elementSpacing / 2));
		return new Vector2(w, h);
	}

	private void _on_SpinBox_value_changed(float value)
	{
		_page = Math.Max(0, Math.Min(255, (Int32)value));
		Update();
	}
}
