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
    /// 
    /// Format danych z urządzenia (64 bajty):
    /// Przykład: 41-43-4B-00-00-4F-4B-00-00-0D-00-00-00-00-00-00...
    /// 
    /// Bajt 0-2:  41-43-4B = "ACK" (ASCII) - potwierdzenie odbioru
    /// Bajt 3-4:  00-00    = Padding/Rezerwa
    /// Bajt 5-6:  4F-4B    = "OK" (ASCII) - status OK
    /// Bajt 7-8:  00-00    = Padding/Rezerwa
    /// Bajt 9:    0D       = Numer przycisku (1-15, tutaj 13)
    /// Bajt 10:   00       = Stan przycisku (0=released, 1=pressed)
    /// Bajt 11+:  00...    = Padding do 64 bajtów (reszta wypełniona zerami)
    /// 
    /// Struktura:
    /// [ACK (3 bajty)] [Padding (2 bajty)] [OK (2 bajty)] [Padding (2 bajty)] [Button (1 bajt)] [State (1 bajt)] [Padding (53 bajty)]
    /// </summary>
    public ButtonPress? ReadButtonPress()
    {
        var data = _usbTransfer.ReadData(bufferSize: 64, timeout: 10);
        
        if (data == null || data.Length == 0)
        {
            return null;
        }
        
        // Sprawdź czy to puste dane (same zera)
        bool allZeros = true;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] != 0)
            {
                allZeros = false;
                break;
            }
        }
        if (allZeros)
        {
            return null; // Puste dane - ignoruj
        }
        
        int buttonNumber = 0;
        int state = 0;
        
        // Format 1: ACK (0x41 0x43 0x4B) + OK (0x4F 0x4B) + numer przycisku (bajt 9) + stan (bajt 10)
        // Przykład: 41-43-4B-00-00-4F-4B-00-00-0D-00-00-00-00-00-00
        if (data.Length >= 11 && 
            data[0] == 0x41 && data[1] == 0x43 && data[2] == 0x4B && // ACK
            data[5] == 0x4F && data[6] == 0x4B) // OK
        {
            buttonNumber = data[9];
            state = data[10];
        }
        // Format 2: Standardowy format (bajty 9-10) - zgodnie z Node.js
        else if (data.Length >= 11)
        {
            buttonNumber = data[9];
            state = data[10];
        }
        // Format 3: Prosty format - numer przycisku w bajcie 0, stan w bajcie 1
        else if (data.Length >= 2)
        {
            buttonNumber = data[0];
            state = data[1];
        }
        
        // Sprawdź czy znaleziono poprawny numer przycisku
        if (buttonNumber >= 1 && buttonNumber <= 15)
        {
            // Określ stan - sprawdź różne możliwe wartości
            string buttonState;
            
            // Sprawdź czy to format ACK+OK - wtedy stan może być w innym miejscu
            bool isAckOkFormat = data.Length >= 11 && 
                                 data[0] == 0x41 && data[1] == 0x43 && data[2] == 0x4B && // ACK
                                 data[5] == 0x4F && data[6] == 0x4B; // OK
            
            if (isAckOkFormat)
            {
                // W formacie ACK+OK, stan jest w bajcie 10
                // 0x00 = released, 0x01 lub inna wartość = pressed
                if (state == 0 || state == 0x00)
                {
                    buttonState = "released";
                }
                else
                {
                    // Wszystkie inne wartości = pressed
                    buttonState = "pressed";
                }
            }
            else
            {
                // Standardowy format
                if (state == 1 || state == 0xFF || state == 0x01)
                {
                    buttonState = "pressed";
                }
                else if (state == 0 || state == 0x00)
                {
                    buttonState = "released";
                }
                else
                {
                    // Jeśli state ma inną wartość, traktuj jako pressed jeśli nie jest 0
                    buttonState = state != 0 ? "pressed" : "released";
                }
            }
            
            return new ButtonPress
            {
                ButtonNumber = buttonNumber,
                State = buttonState
            };
        }
        
        return null; // Nie znaleziono poprawnego numeru przycisku
    }
    
    // Implementacja interfejsu IMiraboxReader
    bool IMiraboxReader.WriteData(byte[] data, bool removeReportId)
    {
        // Dla LibUSB, removeReportId jest ignorowane - zawsze usuwamy Report ID
        return WriteData(data, useFeatureReport: false);
    }
}
