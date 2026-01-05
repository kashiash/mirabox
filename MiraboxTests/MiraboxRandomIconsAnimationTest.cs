using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace mirabox;

public class MiraboxRandomIconsAnimationTest
{
    [Fact]
    public void AnimateRandomIconsForOneMinute()
    {
        Console.WriteLine("\n=== ANIMACJA LOSOWYCH IKON PRZEZ 1 MINUTĘ ===");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Załaduj wszystkie pliki JPG z katalogu
        var imagesDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
        var imageFiles = Directory.GetFiles(imagesDirectory, "shape_*.jpg")
            .OrderBy(f => f)
            .ToArray();
        
        if (imageFiles.Length == 0)
        {
            Console.WriteLine("✗ Nie znaleziono plików JPG w katalogu");
            return;
        }
        
        Console.WriteLine($"Załadowano {imageFiles.Length} ikon");
        
        // Załaduj wszystkie obrazy do pamięci
        var images = imageFiles.Select(f => new
        {
            Name = Path.GetFileNameWithoutExtension(f),
            Data = File.ReadAllBytes(f)
        }).ToArray();
        
        Console.WriteLine($"\nAnimacja przez 60 sekund...");
        Console.WriteLine("Losowe przypisywanie ikon do przycisków\n");
        
        var random = new Random();
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromMinutes(1);
        
        int iteration = 0;
        while (DateTime.Now - startTime < duration)
        {
            // Losuj przycisk (1-15)
            int buttonNumber = random.Next(1, 16);
            
            // Losuj ikonę
            var randomImage = images[random.Next(images.Length)];
            
            // Wyślij do przycisku
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"[{iteration++}] {elapsed:F1}s - Przycisk {buttonNumber}: {randomImage.Name}");
            
            programmer.ProgramButton(buttonNumber, randomImage.Data, 512);
            
            // Krótka przerwa między zmianami (50-300ms)
            Thread.Sleep(random.Next(50, 300));
        }
        
        Console.WriteLine($"\n✓ Animacja zakończona!");
        Console.WriteLine($"Wykonano {iteration} zmian w ciągu 60 sekund");
    }
}
