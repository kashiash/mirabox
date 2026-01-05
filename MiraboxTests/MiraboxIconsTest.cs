using System;
using System.Drawing;
using System.Linq;
using Xunit;

namespace mirabox;

public class MiraboxIconsTest
{
    [Fact]
    public void ProgramButtonsWithSvgIcons()
    {
        Console.WriteLine("\n=== PROGRAMOWANIE PRZYCISKÓW Z IKONAMI SVG ===");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Ładuj wszystkie ikony SVG
        var iconsDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
        var backgroundColor = Color.FromArgb(40, 40, 40); // Ciemnoszare tło
        
        var icons = MiraboxImageGenerator.LoadAllIcons(iconsDirectory, backgroundColor);
        
        if (icons.Count == 0)
        {
            Console.WriteLine("✗ Nie załadowano żadnych ikon");
            return;
        }
        
        Console.WriteLine($"\n✓ Załadowano {icons.Count} ikon");
        
        // Wyślij inicjalizację
        Console.WriteLine("\nInicjalizacja urządzenia...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; disCommand[1] = 0x52; disCommand[2] = 0x54;
        disCommand[5] = 0x44; disCommand[6] = 0x49; disCommand[7] = 0x53;
        libUsbReader.WriteData(disCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Programuj przyciski ikonami
        int buttonNumber = 1;
        foreach (var icon in icons.Take(15)) // Maksymalnie 15 przycisków
        {
            Console.WriteLine($"\nProgramowanie przycisku {buttonNumber} ikoną: {icon.Key}");
            programmer.ProgramButton(buttonNumber, icon.Value, 512);
            buttonNumber++;
            System.Threading.Thread.Sleep(50);
        }
        
        Console.WriteLine("\n✓ Zakończono programowanie przycisków z ikonami!");
        Console.WriteLine("Sprawdź urządzenie - każdy przycisk powinien wyświetlać swoją ikonę");
    }
}
