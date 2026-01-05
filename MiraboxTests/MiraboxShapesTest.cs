using System;
using System.Drawing;
using Xunit;

namespace mirabox;

public class MiraboxShapesTest
{
    [Fact]
    public void ProgramButtonsWithSimpleShapes()
    {
        Console.WriteLine("\n=== PROGRAMOWANIE PRZYCISKÓW Z PROSTYMI KSZTAŁTAMI ===");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Kolory tła i kształtów
        var backgrounds = new[]
        {
            Color.FromArgb(30, 30, 30),    // Ciemnoszary
            Color.FromArgb(0, 50, 100),    // Ciemnoniebieski
            Color.FromArgb(50, 0, 50),     // Ciemnofioletowy
            Color.FromArgb(0, 50, 0),      // Ciemnozielony
            Color.FromArgb(50, 25, 0)      // Ciemnobrązowy
        };
        
        var shapeColors = new[]
        {
            Color.White,
            Color.Yellow,
            Color.Cyan,
            Color.LimeGreen,
            Color.Orange
        };
        
        // Wyślij inicjalizację
        Console.WriteLine("\nInicjalizacja urządzenia...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; disCommand[1] = 0x52; disCommand[2] = 0x54;
        disCommand[5] = 0x44; disCommand[6] = 0x49; disCommand[7] = 0x53;
        libUsbReader.WriteData(disCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Programuj przyciski z różnymi kształtami
        var shapeNames = new[]
        {
            "Kółko", "Kwadrat", "Trójkąt", "Romb", "Gwiazda",
            "Plus", "Krzyżyk", "Serce", "Strzałka↑", "Strzałka→",
            "Kółko", "Kwadrat", "Trójkąt", "Romb", "Gwiazda"
        };
        
        for (int i = 1; i <= 15; i++)
        {
            int shapeType = i % 10 == 0 ? 10 : i % 10;
            var bgColor = backgrounds[(i - 1) % backgrounds.Length];
            var shapeColor = shapeColors[(i - 1) % shapeColors.Length];
            
            Console.WriteLine($"\nProgramowanie przycisku {i}: {shapeNames[i - 1]}");
            var imageData = MiraboxImageGenerator.GenerateSimpleShape(shapeType, bgColor, shapeColor);
            programmer.ProgramButton(i, imageData, 512);
            System.Threading.Thread.Sleep(50);
        }
        
        Console.WriteLine("\n✓ Zakończono programowanie przycisków z kształtami!");
        Console.WriteLine("Sprawdź urządzenie - każdy przycisk powinien wyświetlać swój kształt");
    }
}
