using System;

namespace mirabox;

/// <summary>
/// Adapter dla MiraboxButtonReader używający LibUSB zamiast HID
/// </summary>
public class MiraboxLibUsbButtonReader : MiraboxButtonReader
{
    private readonly MiraboxLibUsbTransfer _usbTransfer;

    public MiraboxLibUsbButtonReader(MiraboxLibUsbTransfer usbTransfer)
    {
        _usbTransfer = usbTransfer ?? throw new ArgumentNullException(nameof(usbTransfer));
    }

    public override bool WriteData(byte[] data, bool useFeatureReport = true)
    {
        // LibUSB nie używa Feature Reports ani Report ID
        // Wysyłamy dane bezpośrednio przez interrupt transfer
        
        // Usuń Report ID jeśli jest na początku (0x00)
        byte[] dataToSend = data;
        if (data.Length > 0 && data[0] == 0x00)
        {
            // Pomiń pierwszy bajt (Report ID)
            dataToSend = new byte[data.Length - 1];
            Array.Copy(data, 1, dataToSend, 0, data.Length - 1);
        }
        
        // WAŻNE: Pakiety muszą mieć DOKŁADNIE 512 bajtów (bez Report ID)
        // Zgodnie z Node.js: if (data.length < 512) pad with zeros
        if (dataToSend.Length < 512)
        {
            var paddedData = new byte[512];
            Array.Copy(dataToSend, paddedData, dataToSend.Length);
            dataToSend = paddedData;
        }
        else if (dataToSend.Length > 512)
        {
            // Obetnij do 512 bajtów
            var truncatedData = new byte[512];
            Array.Copy(dataToSend, truncatedData, 512);
            dataToSend = truncatedData;
        }
        
        Console.WriteLine($"  Wysyłanie {dataToSend.Length} bajtów przez LibUSB (pierwsze bajty: {BitConverter.ToString(dataToSend.Take(16).ToArray())})");

        return _usbTransfer.WriteData(dataToSend);
    }

    public new byte[]? ReadData(int bufferSize = 512)
    {
        return _usbTransfer.ReadData(bufferSize);
    }
}
