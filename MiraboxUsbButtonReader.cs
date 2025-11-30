using System;

namespace mirabox;

/// <summary>
/// Adapter, który pozwala używać MiraboxUsbTransfer jako MiraboxButtonReader
/// </summary>
public class MiraboxUsbButtonReader : MiraboxButtonReader
{
    private readonly MiraboxUsbTransfer _usbTransfer;

    public MiraboxUsbButtonReader(MiraboxUsbTransfer usbTransfer)
    {
        _usbTransfer = usbTransfer ?? throw new ArgumentNullException(nameof(usbTransfer));
    }

    public new bool IsConnected => _usbTransfer.IsConnected;

    public new bool WriteData(byte[] data, bool useFeatureReport = true)
    {
        // Dla USB bulk transfer, ignoruj useFeatureReport
        return _usbTransfer.WriteData(data);
    }
}

