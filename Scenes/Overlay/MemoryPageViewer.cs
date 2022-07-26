using Godot;
using System;
using System.IO;

public class MemoryPageViewer : VBoxContainer
{
    private EmulatedDevice? _emulatedDevice;
    private FileDialog? _romFileDialog;

    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
        _romFileDialog = GetNode<FileDialog>("RomFileDialog");
    }

    public void _on_RomLoadButton_pressed()
    {
        _romFileDialog!.PopupCentered();
    }

    public void _on_RomFileDialog_file_selected(String path)
    {
        var device = _emulatedDevice!;
        device.Bus.Cpu.IsSuspended = true;

        _emulatedDevice!.Bus.LoadRom(path);

        device.Bus.Reset();
        device.Bus.Cpu.IsSuspended = true;
    }
}
