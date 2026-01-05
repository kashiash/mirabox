using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Xunit;

namespace mirabox;

public class MiraboxHidTest
{
    [Fact]
    public void TestHidConnection()
    {
        Console.WriteLine("\n=== TEST POŁĄCZENIA PRZEZ HID (BEZ WINUSB) ===");
        
        using var hidTransfer = new MiraboxHidTransfer();
        
        if (!hidTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się przez HID");
            Console.WriteLine("Możliwe przyczyny:");
            Console.WriteLine("1. Urządzenie nie jest podłączone");
            Console.WriteLine("2. Sterownik WinUSB jest zainstalowany (HID nie działa z WinUSB)");
            Console.WriteLine("3. Urządzenie nie obsługuje HID");
            return;
        }
        
        Console.WriteLine("✓ Połączono przez HID!");
        
        var reader = new MiraboxHidButtonReader(hidTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        
        // Test: Wyślij prostą komendę DIS (wake screen)
        Console.WriteLine("\nTest 1: Wysyłanie komendy DIS...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; disCommand[1] = 0x52; disCommand[2] = 0x54;
        disCommand[5] = 0x44; disCommand[6] = 0x49; disCommand[7] = 0x53;
        
        if (reader.WriteData(disCommand, false))
        {
            Console.WriteLine("✓ Komenda DIS wysłana");
        }
        else
        {
            Console.WriteLine("✗ Błąd wysyłania komendy DIS");
            return;
        }
        
        System.Threading.Thread.Sleep(100);
        
        // Test: Zaprogramuj jeden przycisk
        Console.WriteLine("\nTest 2: Programowanie przycisku 1...");
        var imageData = MiraboxImageGenerator.GenerateSimpleShape(
            1, 
            Color.Blue, 
            Color.White
        );
        
        if (programmer.ProgramButton(1, imageData, 512))
        {
            Console.WriteLine("✓ Przycisk zaprogramowany przez HID!");
        }
        else
        {
            Console.WriteLine("✗ Błąd programowania przycisku");
        }
    }
    
    [Fact]
    public void ProgramAllButtonsViaHid()
    {
        Console.WriteLine("\n=== PROGRAMOWANIE WSZYSTKICH PRZYCISKÓW PRZEZ HID ===");
        
        using var hidTransfer = new MiraboxHidTransfer();
        
        if (!hidTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się przez HID");
            return;
        }
        
        var reader = new MiraboxHidButtonReader(hidTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        
        // Inicjalizacja
        Console.WriteLine("\nInicjalizacja urządzenia...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; disCommand[1] = 0x52; disCommand[2] = 0x54;
        disCommand[5] = 0x44; disCommand[6] = 0x49; disCommand[7] = 0x53;
        reader.WriteData(disCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Załaduj ikony z plików
        var imagesDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
        var imageFiles = Directory.GetFiles(imagesDirectory, "shape_*.jpg")
            .OrderBy(f => f)
            .Take(15)
            .ToArray();
        
        if (imageFiles.Length == 0)
        {
            Console.WriteLine("✗ Brak plików ikon");
            return;
        }
        
        Console.WriteLine($"Znaleziono {imageFiles.Length} ikon\n");
        
        // Programuj przyciski
        int buttonNumber = 1;
        foreach (var imageFile in imageFiles)
        {
            var fileName = Path.GetFileName(imageFile);
            Console.WriteLine($"Programowanie przycisku {buttonNumber}: {fileName}");
            
            var imageData = File.ReadAllBytes(imageFile);
            programmer.ProgramButton(buttonNumber, imageData, 512);
            buttonNumber++;
            System.Threading.Thread.Sleep(50);
        }
        
        Console.WriteLine("\n✓ Zakończono programowanie przez HID!");
    }
}
