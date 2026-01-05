using System;
using System.Threading;
using Xunit;

namespace mirabox;

public class MiraboxAnimationTest
{
    [Fact]
    public void AnimateRandomColors()
    {
        Console.WriteLine("\n=== ANIMACJA LOSOWYCH KOLORÓW ===");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        var random = new Random();
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromMinutes(1);
        
        Console.WriteLine($"\nAnimacja przez {duration.TotalSeconds} sekund...");
        Console.WriteLine("Naciśnij Ctrl+C aby przerwać\n");
        
        int iteration = 0;
        while (DateTime.Now - startTime < duration)
        {
            // Losuj przycisk (1-15)
            int buttonNumber = random.Next(1, 16);
            
            // Generuj LOSOWY kolor (nie stały dla numeru przycisku!)
            var imageData = MiraboxImageGenerator.GenerateRandomColor();
            
            // Wyślij do przycisku
            Console.WriteLine($"[{iteration++}] Zmiana koloru przycisku {buttonNumber}");
            programmer.ProgramButton(buttonNumber, imageData, 512);
            
            // Krótka przerwa między zmianami (100-500ms)
            Thread.Sleep(random.Next(100, 500));
        }
        
        Console.WriteLine("\n✓ Animacja zakończona!");
    }
}
