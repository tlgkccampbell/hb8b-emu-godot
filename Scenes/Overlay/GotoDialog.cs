using Godot;
using System;
using System.Globalization;

public class GotoDialog : WindowDialog
{
    private LineEdit? _gotoAddressLineEdit;
    private Button? _goButton;
    private UInt16? _address;

    [Signal]
    public delegate void Confirmed(UInt16 address);

    public override void _Ready()
    {
        _gotoAddressLineEdit = GetNode<LineEdit>("MarginContainer/VBoxContainer/CenterContainer/HBoxContainer/GotoAddressLineEdit");
        _goButton = GetNode<Button>("MarginContainer/VBoxContainer/GoButton");
    }

    public void Confirm()
    {
        if (_address != null)
            EmitSignal(nameof(Confirmed), _address);

        Hide();
    }

    public void _on_GotoDialog_about_to_show()
    {
        _gotoAddressLineEdit!.Text = String.Empty;
        _goButton!.Disabled = true;
    }

    public void _on_GotoAddressLineEdit_text_changed(String newText)
    {
        _address = null;
        if (UInt16.TryParse(newText, NumberStyles.AllowHexSpecifier, null, out var value))
        {
            _address = value;
        }
        _goButton!.Disabled = (_address == null);
    }

    public void _on_GotoAddressLineEdit_text_entered(String newText)
    {
        if (_address != null)
            Confirm();
    }
    
    public void _on_GoButton_pressed()
    {
        if (_address != null)
            Confirm();
    }
}
