using Godot;
using System;

public class Overlay : Control
{
    private EmulatedDevice? _emulatedDevice;
    private GotoDialog? _gotoDialog;

    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
        _gotoDialog = GetNode<GotoDialog>("GotoDialog");
    }

    public override void _Input(InputEvent @event)
    {
        var emulatedDevice = _emulatedDevice!;

        if (@event.IsActionPressed("ui_overlay_toggle"))
        {
            this.Visible = !this.Visible;
        }

        if (@event.IsActionPressed("emu_toggle_suspended"))
        {
            emulatedDevice.Bus.Cpu.IsSuspended = !emulatedDevice.Bus.Cpu.IsSuspended;
        }

        if (emulatedDevice.Bus.Cpu.IsSuspended)
        {
            if (@event.IsActionPressed("open_goto") && !_gotoDialog!.Visible)
            {
                GetTree().SetInputAsHandled();
                _gotoDialog.PopupCentered();                    
            }

            if (@event.IsActionPressed("emu_step_cycle"))
            {
                emulatedDevice.Bus.SingleStepCycle();
            }

            if (@event.IsActionPressed("emu_step_instruction"))
            {
                emulatedDevice.Bus.SingleStepInstruction();
            }
        }
    }

    public void _on_GotoDialog_Confirmed(UInt16 address)
    {
        _emulatedDevice!.Bus.Cpu.ProgramCounter = address;
    }
}
