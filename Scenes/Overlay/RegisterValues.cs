using Godot;
using System;

public class RegisterValues : VBoxContainer
{
    private EmulatedDevice? _emulatedDevice;
    private Label? _aValueLabel;
    private Label? _xValueLabel;
    private Label? _yValueLabel;
    private Label? _stkpValueLabel;
    private Label? _pcValueLabel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
        _aValueLabel = GetNode<Label>("ARegisterValue");
        _xValueLabel = GetNode<Label>("XRegisterValue");
        _yValueLabel = GetNode<Label>("YRegisterValue");
        _stkpValueLabel = GetNode<Label>("StackPointerValue");
        _pcValueLabel = GetNode<Label>("ProgramCounterValue");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        _aValueLabel!.Text = $"${_emulatedDevice!.Bus.Cpu.Accumulator:X2}";
        _xValueLabel!.Text = $"${_emulatedDevice!.Bus.Cpu.XRegister:X2}";
        _yValueLabel!.Text = $"${_emulatedDevice!.Bus.Cpu.YRegister:X2}";
        _stkpValueLabel!.Text = $"${_emulatedDevice!.Bus.Cpu.StackPointer:X2}";
        _pcValueLabel!.Text = $"${_emulatedDevice!.Bus.Cpu.ProgramCounter:X2}";
    }
}
