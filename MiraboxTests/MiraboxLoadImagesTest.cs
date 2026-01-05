using System;
using System.IO;
using System.Linq;
using Xunit;

namespace mirabox;

public class MiraboxLoadImagesTest
{
    [Fact]
    public void ProgramButtonsFromImageFiles()
    {
        Console.WriteLine("\n=== PROGRAMOWANIE PRZYCISKÓW Z PLIKÓW JPG ===");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Ścieżka do katalogu z obrazami
        var imagesDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
        
        if (!Directory.Exists(imagesDirectory))
        {
            Console.WriteLine($"✗ Katalog nie istnieje: {imagesDirectory}");
            return;
        }
        
        // Załaduj wszystkie pliki JPG z katalogu
        var imageFiles = Directory.GetFiles(imagesDirectory, "shape_*.jpg")
            .OrderBy(f => f)
            .Take(15) // Maksymalnie 15 przycisków
            .ToArray();
        
        if (imageFiles.Length == 0)
        {
            Console.WriteLine("✗ Nie znaleziono plików JPG w katalogu");
            return;
        }
        
        Console.WriteLine($"Znaleziono {imageFiles.Length} plików JPG");
        
        // Wyślij inicjalizację
        Console.WriteLine("\nInicjalizacja urządzenia...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; disCommand[1] = 0x52; disCommand[2] = 0x54;
        disCommand[5] = 0x44; disCommand[6] = 0x49; disCommand[7] = 0x53;
        libUsbReader.WriteData(disCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Programuj przyciski obrazami z plików
        int buttonNumber = 1;
        foreach (var imageFile in imageFiles)
        {
            var fileName = Path.GetFileName(imageFile);
            Console.WriteLine($"\nProgramowanie przycisku {buttonNumber}: {fileName}");
            
            try
            {
                var imageData = File.ReadAllBytes(imageFile);
                Console.WriteLine($"  Załadowano: {imageData.Length} bajtów");
                
                programmer.ProgramButton(buttonNumber, imageData, 512);
                buttonNumber++;
                System.Threading.Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Błąd: {ex.Message}");
            }
        }
        
        Console.WriteLine("\n✓ Zakończono programowanie przycisków z plików JPG!");
        Console.WriteLine($"Zaprogramowano {buttonNumber - 1} przycisków");
    }
}
