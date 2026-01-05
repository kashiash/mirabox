using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;

namespace mirabox;

/// <summary>
/// Klasa do ustawiania tła ekranu Mirabox
/// </summary>
public class MiraboxBackgroundSetter
{
    private readonly IMiraboxReader _reader;

    public MiraboxBackgroundSetter(IMiraboxReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>
    /// Ustawia tło ekranu (wallpaper) - próba 1: przez komendę CLE z obrazem
    /// </summary>
    public bool SetBackground(byte[] imageData, int packetSize = 512)
    {
        Console.WriteLine("\n=== PRÓBA USTAWIENIA TŁA EKRANU ===");
        Console.WriteLine("UWAGA: To może nie działać - protokół może nie obsługiwać tła ekranu");
        
        // Próba 1: Wyczyść ekran i wyślij obraz jako tło
        Console.WriteLine("\n1. Czyszczenie ekranu (CLE)...");
        var cleCommand = new byte[512];
        cleCommand[0] = 0x00; // Report ID
        cleCommand[1] = 0x43; // C
        cleCommand[2] = 0x52; // R
        cleCommand[3] = 0x54; // T
        cleCommand[4] = 0x00;
        cleCommand[5] = 0x00;
        cleCommand[6] = 0x43; // C
        cleCommand[7] = 0x4C; // L
        cleCommand[8] = 0x45; // E
        cleCommand[9] = 0x00;
        cleCommand[10] = 0x00;
        cleCommand[11] = 0x00; // Clear all
        
        if (!_reader.WriteData(cleCommand, false))
        {
            Console.WriteLine("✗ Błąd wysyłania komendy CLE");
            return false;
        }
        
        System.Threading.Thread.Sleep(100);
        
        // Próba 2: Wyślij obraz jako "przycisk 0" (może być tło ekranu)
        Console.WriteLine("\n2. Próba wysłania obrazu jako tło (przycisk 0)...");
        var batCommand = CreateBackgroundCommand(imageData, packetSize);
        
        if (!_reader.WriteData(batCommand, false))
        {
            Console.WriteLine("✗ Błąd wysyłania komendy BAT dla tła");
            return false;
        }
        
        System.Threading.Thread.Sleep(10);
        
        // Wyślij dane obrazu
        if (!SendImageData(imageData, packetSize))
        {
            Console.WriteLine("✗ Błąd wysyłania danych obrazu");
            return false;
        }
        
        System.Threading.Thread.Sleep(10);
        
        // Odśwież ekran
        Console.WriteLine("\n3. Odświeżanie ekranu (STP)...");
        var stpCommand = new byte[512];
        stpCommand[0] = 0x00; // Report ID
        stpCommand[1] = 0x43; // C
        stpCommand[2] = 0x52; // R
        stpCommand[3] = 0x54; // T
        stpCommand[4] = 0x00;
        stpCommand[5] = 0x00;
        stpCommand[6] = 0x53; // S
        stpCommand[7] = 0x54; // T
        stpCommand[8] = 0x50; // P
        
        if (!_reader.WriteData(stpCommand, false))
        {
            Console.WriteLine("✗ Błąd wysyłania komendy STP");
            return false;
        }
        
        Console.WriteLine("\n✓ Komendy wysłane - sprawdź czy tło się zmieniło");
        Console.WriteLine("UWAGA: Jeśli tło się nie zmieniło, urządzenie może nie obsługiwać tej funkcji");
        
        return true;
    }

    /// <summary>
    /// Tworzy komendę BAT dla tła (przycisk 0)
    /// </summary>
    private byte[] CreateBackgroundCommand(byte[] imageData, int packetSize)
    {
        var command = new List<byte>
        {
            0x00, // Report ID
            0x43, 0x52, 0x54, 0x00, 0x00, // CRT prefix
            0x42, 0x41, 0x54 // BAT command
        };
        
        // Rozmiar obrazu (4 bajty, big-endian)
        uint imageSize = (uint)imageData.Length;
        command.Add((byte)((imageSize >> 24) & 0xFF));
        command.Add((byte)((imageSize >> 16) & 0xFF));
        command.Add((byte)((imageSize >> 8) & 0xFF));
        command.Add((byte)(imageSize & 0xFF));
        
        // Numer przycisku = 0 (może oznaczać tło ekranu)
        command.Add(0x00);
        
        // Dopełnienie do rozmiaru pakietu
        while (command.Count < packetSize)
        {
            command.Add(0x00);
        }
        
        return command.ToArray();
    }

    /// <summary>
    /// Wysyła dane obrazu w chunkach po 512 bajtów
    /// </summary>
    private bool SendImageData(byte[] imageData, int packetSize)
    {
        int totalChunks = (int)Math.Ceiling((double)imageData.Length / (packetSize - 1));
        
        for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            int offset = chunkIndex * (packetSize - 1);
            int remainingBytes = imageData.Length - offset;
            int chunkSize = Math.Min(packetSize - 1, remainingBytes);
            
            var chunk = new byte[packetSize];
            chunk[0] = 0x00; // Report ID
            Array.Copy(imageData, offset, chunk, 1, chunkSize);
            
            // Reszta jest już wypełniona zerami
            
            if (!_reader.WriteData(chunk, false))
            {
                Console.WriteLine($"✗ Błąd wysyłania chunku {chunkIndex + 1}/{totalChunks}");
                return false;
            }
            
            System.Threading.Thread.Sleep(5);
        }
        
        return true;
    }

    /// <summary>
    /// Generuje obraz tła z koloru
    /// </summary>
    public static byte[] GenerateBackgroundImage(Color backgroundColor, int width = 320, int height = 240)
    {
        // Tło ekranu może być większe niż przyciski (320x240 lub cały ekran)
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        graphics.Clear(backgroundColor);
        
        // Konwersja do JPEG quality 100
        using var ms = new MemoryStream();
        var jpegEncoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
        
        if (jpegEncoder == null)
        {
            throw new Exception("Nie znaleziono kodera JPEG");
        }
        
        var encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
        encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(
            System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);
        
        return ms.ToArray();
    }
}

