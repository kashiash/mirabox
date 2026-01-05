using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mirabox;

public class MiraboxButtonProgrammer
{
    private readonly IMiraboxReader _reader;

    public MiraboxButtonProgrammer(IMiraboxReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>
    /// Wysyła komendę inicjalizacji urządzenia (DIS - Display/Wake)
    /// </summary>
    private bool SendInitCommand(int packetSize = 512)
    {
        var command = new List<byte>
        {
            0x00, // Report ID
            0x43, 0x52, 0x54, 0x00, 0x00, // CRT prefix
            0x44, 0x49, 0x53 // DIS command
        };
        
        // Dopełnienie do rozmiaru pakietu (Report ID już wliczony)
        while (command.Count < packetSize)
        {
            command.Add(0x00);
        }
        
        return _reader.WriteData(command.ToArray(), false);
    }
    
    /// <summary>
    /// Wysyła komendę odświeżenia ekranu (STP - Stop/Refresh)
    /// </summary>
    private bool SendRefreshCommand(int packetSize = 512)
    {
        var command = new List<byte>
        {
            0x00, // Report ID
            0x43, 0x52, 0x54, 0x00, 0x00, // CRT prefix
            0x53, 0x54, 0x50 // STP command
        };
        
        // Dopełnienie do rozmiaru pakietu (Report ID już wliczony)
        while (command.Count < packetSize)
        {
            command.Add(0x00);
        }
        
        return _reader.WriteData(command.ToArray(), false);
    }
    
    /// <summary>
    /// Konwertuje rozmiar na 4 bajty hex (zgodnie z protokołem Node.js)
    /// </summary>
    private byte[] SizeBytes(int size, int bytes = 4)
    {
        // Konwertuj rozmiar na hex string z paddingiem
        string hexString = size.ToString("X").PadLeft(bytes * 2, '0');
        
        // Konwertuj hex string na bajty
        byte[] result = new byte[bytes];
        for (int i = 0; i < bytes; i++)
        {
            result[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }
        return result;
    }
    
    /// <summary>
    /// Tworzy komendę BAT (Button Image) do programowania przycisku z obrazem
    /// Zgodnie z protokołem Mirabox: pakiety mają 512 lub 1024 bajtów
    /// Na podstawie: https://github.com/4ndv/mirajazz i https://github.com/rigor789/mirabox-streamdock-node
    /// </summary>
    public byte[] CreateButtonProgramCommand(int buttonNumber, byte[] imageData, int packetSize = 512)
    {
        // Format komendy BAT dla Mirabox:
        // [0x00] - Report ID
        // [0x43, 0x52, 0x54, 0x00, 0x00] - CRT prefix
        // [0x42, 0x41, 0x54] - BAT command (Button Image)
        // [4 bajty rozmiaru] - rozmiar obrazu jako hex string (4 bajty)
        // [buttonNumber] - numer przycisku (1-based)
        
        var command = new List<byte>
        {
            0x00, // Report ID
            0x43, 0x52, 0x54, 0x00, 0x00, // CRT prefix
            0x42, 0x41, 0x54, // BAT command
        };
        
        // Rozmiar obrazu (4 bajty jako hex string)
        var sizeBytes = SizeBytes(imageData.Length, 4);
        command.AddRange(sizeBytes);
        
        // Numer przycisku (1-based: przycisk 1 = 1)
        command.Add((byte)buttonNumber);
        
        // Dopełnienie do rozmiaru pakietu (Report ID już wliczony)
        while (command.Count < packetSize)
        {
            command.Add(0x00);
        }

        return command.ToArray();
    }
    
    /// <summary>
    /// Wysyła dane obrazu do przycisku w pakietach
    /// DOKŁADNIE JAK W NODE.JS: sendBytes wysyła chunki po 512 bajtów BEZ prefiksu CRT
    /// </summary>
    private bool SendImageData(byte[] imageData, int packetSize = 512)
    {
        // Node.js: sendBytes wysyła dane w chunkach po 512 bajtów
        // Każdy chunk jest dopełniony do 512 bajtów zerami
        // send(chunk, []) - PUSTY prefix (nie CRT!)
        
        var bytesRemaining = imageData.Length;
        var offset = 0;
        var packetNum = 0;
        
        while (bytesRemaining > 0)
        {
            // Ile bajtów danych w tym pakiecie
            var thisLength = Math.Min(bytesRemaining, packetSize);
            
            // Utwórz pakiet 512 bajtów
            var packet = new byte[packetSize];
            
            // Skopiuj dane obrazu
            Array.Copy(imageData, offset, packet, 0, thisLength);
            
            // Reszta pakietu to zera (już jest, bo new byte[] inicjalizuje zerami)
            
            // WAŻNE: Dodaj Report ID 0x00 na początku dla LibUSB
            var packetWithReportId = new byte[packetSize + 1];
            packetWithReportId[0] = 0x00; // Report ID
            Array.Copy(packet, 0, packetWithReportId, 1, packetSize);
            
            // Wyślij pakiet
            Console.WriteLine($"  Wysyłanie pakietu danych {++packetNum} ({thisLength} bajtów danych, {packetSize} bajtów pakietu)");
            if (!_reader.WriteData(packetWithReportId, false))
            {
                Console.WriteLine($"  ✗ Błąd podczas wysyłania danych obrazu (offset: {offset})");
                return false;
            }
            
            bytesRemaining -= thisLength;
            offset += thisLength;
        }
        
        return true;
    }

    /// <summary>
    /// Programuje przycisk z obrazem
    /// Używa protokołu Mirabox z pakietami 512 lub 1024 bajtów
    /// </summary>
    public bool ProgramButton(int buttonNumber, byte[] imageData, int packetSize = 512)
    {
        if (buttonNumber < 1 || buttonNumber > 15)
        {
            Console.WriteLine($"Nieprawidłowy numer przycisku: {buttonNumber}. Musi być między 1 a 15.");
            return false;
        }

        if (imageData == null || imageData.Length == 0)
        {
            Console.WriteLine("Brak danych obrazu do wysłania");
            return false;
        }

        Console.WriteLine($"\nProgramowanie przycisku {buttonNumber}...");
        Console.WriteLine($"Rozmiar obrazu: {imageData.Length} bajtów");
        Console.WriteLine($"Rozmiar pakietu: {packetSize} bajtów");

        // 1. Wyślij komendę BAT (Button Image)
        var batCommand = CreateButtonProgramCommand(buttonNumber, imageData, packetSize);
        Console.WriteLine($"Wysyłanie komendy BAT: {BitConverter.ToString(batCommand.Take(16).ToArray())}...");
        
        if (!_reader.WriteData(batCommand, false))
        {
            Console.WriteLine($"✗ Błąd podczas wysyłania komendy BAT");
            return false;
        }
        
        System.Threading.Thread.Sleep(10);
        
        // 2. Wyślij dane obrazu w pakietach
        Console.WriteLine($"Wysyłanie danych obrazu ({imageData.Length} bajtów)...");
        if (!SendImageData(imageData, packetSize))
        {
            Console.WriteLine($"✗ Błąd podczas wysyłania danych obrazu");
            return false;
        }
        
        System.Threading.Thread.Sleep(10);
        
        // 3. Wyślij komendę odświeżenia (STP)
        Console.WriteLine($"Wysyłanie komendy STP (odświeżenie)...");
        if (!SendRefreshCommand(packetSize))
        {
            Console.WriteLine($"✗ Błąd podczas wysyłania komendy STP");
            return false;
        }
        
        Console.WriteLine($"✓ Przycisk {buttonNumber} zaprogramowany pomyślnie");
        return true;
    }

    /// <summary>
    /// Programuje wszystkie przyciski z różnymi obrazami
    /// </summary>
    public void ProgramAllButtons(Func<int, byte[]> imageGenerator, int packetSize = 512)
    {
        Console.WriteLine("\n=== PROGRAMOWANIE WSZYSTKICH PRZYCISKÓW ===");
        Console.WriteLine($"Protokół: Mirabox CRT (pakiety {packetSize} bajtów)");
        
        // Wyślij komendę inicjalizacji
        Console.WriteLine("\nInicjalizacja urządzenia (DIS)...");
        if (!SendInitCommand(packetSize))
        {
            Console.WriteLine("⚠ Ostrzeżenie: Nie udało się wysłać komendy inicjalizacji");
        }
        System.Threading.Thread.Sleep(100);
        
        // Programuj wszystkie przyciski (15 przycisków: 3 rzędy × 5 kolumn)
        for (int i = 1; i <= 15; i++)
        {
            var imageData = imageGenerator(i);
            ProgramButton(i, imageData, packetSize);
            
            // Przerwa między przyciskami
            System.Threading.Thread.Sleep(50);
        }
        
        Console.WriteLine("\n✓ Zakończono programowanie wszystkich przycisków");
    }
}

