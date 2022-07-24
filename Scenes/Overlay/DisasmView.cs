using Godot;
using System;

[Tool]
public class DisasmView : Control
{
    private EmulatedDevice? _emulatedDevice;
    private Font? _font;
	private Int32 _fontHeight;
    private Int32 _elementSpacing;
    private Int32 _lineSpacing;
	private Int32 _longestMnemonicWidth;
    private Int32 _longestArgumentWidth;
	private Int32 _hexAddrWidth;
    private Int32 _linesDisplayed;

    [Export(PropertyHint.Range, "1,100")]
    private Int32 LinesDisplayed
    {
        get { return _linesDisplayed; }
        set
        {
            _linesDisplayed = value;
            MinimumSizeChanged();
        }
    }

    public override void _Ready()
    {
        _emulatedDevice = Engine.EditorHint ? null : GetNode<EmulatedDevice>("/root/EmulatedDevice");

        // Assume a monospace font
		_font = GetFont("font");
		_fontHeight = (Int32)Math.Ceiling(_font.GetHeight());
		_elementSpacing = (Int32)Math.Ceiling(_font.GetStringSize(" ").x);
        _lineSpacing = _elementSpacing / 2;
        _longestMnemonicWidth = (Int32)Math.Ceiling(_font.GetStringSize("XXXX").x);
        _longestArgumentWidth = (Int32)Math.Ceiling(_font.GetStringSize("XXXXXXXXXXXXXXXXXXXXXXXX").x);
		_hexAddrWidth = (Int32)Math.Ceiling(_font.GetStringSize("$FFFF:").x);
    }

    public override void _Draw()
    {
        if (!Engine.EditorHint && !_emulatedDevice!.Bus.Cpu.IsSuspended)
            return;

        var font = _font!;
        var position = new Vector2(0, font.GetAscent());

        var disasm = _emulatedDevice?.Bus.Disassembler;
        if (disasm != null)
            disasm.Address = _emulatedDevice!.Bus.Cpu.ProgramCounter;
 
        for (var i = 0; i < LinesDisplayed; i++)
        {
            var disasmAddr = disasm?.Address ?? 0;
            var disasmText = disasm?.Disassemble() ?? "LDA $FF";
            DrawString(_font, position, $"${disasmAddr:X4}: {disasmText}", (i == 0) ? Colors.Yellow : (Color?)null);
            position.y += _fontHeight + _lineSpacing;
        }
    }

    public override void _Process(float delta)
    {
        if (!Engine.EditorHint)
            Update();
    }
    
    public override Vector2 _GetMinimumSize()
	{
        var w = _hexAddrWidth + _longestMnemonicWidth + _longestArgumentWidth + (_elementSpacing * 2);
        var h = (_fontHeight * LinesDisplayed) + (_lineSpacing * (LinesDisplayed - 1));
        return new Vector2(w, h);
	}
}
