using System;
using Hb8b.Emulation;
using Godot;

public class MemoryPageViewer : VBoxContainer
{
    private EmulatedDevice? _emulatedDevice;
    private FileDialog? _romFileDialog;
    private Label? _memoryPageLabel;

    public override void _Ready()
    {
        _emulatedDevice = GetNode<EmulatedDevice>("/root/EmulatedDevice");
        _romFileDialog = GetNode<FileDialog>("RomFileDialog");
        _memoryPageLabel = GetNode<Label>("HBoxContainer/MemoryPageLabel");

        UpdateMemoryPageLabel(Hb8bMemoryBlock.WorkRam);
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

    public void _on_MemoryPageDisplay_PageChanged(Int32 page)
    {
        var block = Hb8bSystemBus.GetMemoryBlock((UInt16)(page << 8));
        UpdateMemoryPageLabel(block);
    }

    private void UpdateMemoryPageLabel(Hb8bMemoryBlock block)
    {
        switch (block)
        {
            case Hb8bMemoryBlock.WorkRam:
                _memoryPageLabel!.Text = "Work RAM";
                break;

            case Hb8bMemoryBlock.VideoRam:
                _memoryPageLabel!.Text = "Video RAM";
                break;

            case Hb8bMemoryBlock.IO0:
                _memoryPageLabel!.Text = "I/O Device 0";
                break;

            case Hb8bMemoryBlock.IO1:
                _memoryPageLabel!.Text = "I/O Device 1";
                break;

            case Hb8bMemoryBlock.IO2:
                _memoryPageLabel!.Text = "I/O Device 2";
                break;

            case Hb8bMemoryBlock.IO3:
                _memoryPageLabel!.Text = "I/O Device 3";
                break;

            case Hb8bMemoryBlock.Rom:
                _memoryPageLabel!.Text = "Cartridge ROM";
                break;
        }
    }
}
