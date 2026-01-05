using System;
using System.Linq;

namespace mirabox;

/// <summary>
/// Adapter dla MiraboxButtonReader używający LibUSB zamiast HID
/// </summary>
public class MiraboxLibUsbButtonReader : MiraboxButtonReader, IMiraboxReader
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
    
    /// <summary>
    /// Odczytuje naciśnięcie przycisku z urządzenia
    /// </summary>
    public ButtonPress? ReadButtonPress()
    {
        var data = _usbTransfer.ReadData(bufferSize: 512, timeout: 100);
        
        if (data == null || data.Length < 11)
        {
            return null;
        }
        
        // Format zgodny z Node.js - sprawdź różne możliwe formaty
        // W zależności od urządzenia, dane mogą być w różnych miejscach
        int buttonNumber = 0;
        int state = 0;
        
        // Próba 1: Standardowy format (bajty 9-10)
        if (data.Length >= 11)
        {
            buttonNumber = data[9];
            state = data[10];
        }
        
        // Próba 2: Jeśli buttonNumber jest 0, spróbuj innych pozycji
        if (buttonNumber == 0 && data.Length >= 3)
        {
            // Może być w pierwszych bajtach
            buttonNumber = data[1];
            state = data[2];
        }
        
        // Próba 3: Sprawdź czy któryś bajt zawiera numer przycisku (1-15)
        if (buttonNumber == 0 || buttonNumber > 15)
        {
            for (int i = 0; i < Math.Min(data.Length, 64); i++)
            {
                if (data[i] >= 1 && data[i] <= 15)
                {
                    buttonNumber = data[i];
                    // Następny bajt może być stanem
                    if (i + 1 < data.Length)
                    {
                        state = data[i + 1];
                    }
                    break;
                }
            }
        }
        
        if (buttonNumber == 0 || buttonNumber > 15)
        {
            return null; // Nieprawidłowy numer przycisku
        }
        
        return new ButtonPress
        {
            ButtonNumber = buttonNumber,
            State = state == 1 || state == 0xFF ? "pressed" : "released"
        };
    }
    
    // Implementacja interfejsu IMiraboxReader
    bool IMiraboxReader.WriteData(byte[] data, bool removeReportId)
    {
        // Dla LibUSB, removeReportId jest ignorowane - zawsze usuwamy Report ID
        return WriteData(data, useFeatureReport: false);
    }
}
