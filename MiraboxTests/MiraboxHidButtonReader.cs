using System;

namespace mirabox;

/// <summary>
/// Adapter dla HID - kompatybilny z MiraboxButtonProgrammer
/// </summary>
public class MiraboxHidButtonReader : IMiraboxReader
{
    private readonly MiraboxHidTransfer _hidTransfer;
    
    public MiraboxHidButtonReader(MiraboxHidTransfer hidTransfer)
    {
        _hidTransfer = hidTransfer;
    }
    
    public bool WriteData(byte[] data, bool removeReportId)
    {
        return _hidTransfer.WriteData(data, removeReportId);
    }
    
    public ButtonPress? ReadButtonPress()
    {
        var data = _hidTransfer.ReadData(timeout: 100);
        
        if (data == null || data.Length < 11)
        {
            return null;
        }
        
        // Format zgodny z Node.js
        var buttonNumber = data[9];
        var state = data[10];
        
        return new ButtonPress
        {
            ButtonNumber = buttonNumber,
            State = state == 1 ? "pressed" : "released"
        };
    }
}

public class ButtonPress
{
    public int ButtonNumber { get; set; }
    public string State { get; set; } = "";
}
