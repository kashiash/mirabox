using System;
using System.Drawing;
using System.Threading;

namespace mirabox;

/// <summary>
/// Interaktywny test - programuje wszystkie przyciski i odczytuje naciśnięcia
/// </summary>
public class MiraboxInteractiveTest
{
    // Mapowanie numeru przycisku na nazwę figury
    private static readonly string[] ShapeNames = new[]
    {
        "", // 0 - nieużywane
        "Kółko",      // 1
        "Kwadrat",    // 2
        "Trójkąt",    // 3
        "Romb",       // 4
        "Gwiazda",    // 5
        "Plus",       // 6
        "Krzyżyk",   // 7
        "Serce",      // 8
        "Strzałka w górę",    // 9
        "Strzałka w prawo",   // 10
        "Kółko (zielone)",    // 11 - powtórzenie z kolorami
        "Kwadrat (niebieski)", // 12
        "Trójkąt (czerwony)",  // 13
        "Romb (żółty)",        // 14
        "Gwiazda (fioletowa)"  // 15
    };
    
    // Kolory dla przycisków 11-15
    private static readonly Color[] ShapeColors = new[]
    {
        Color.White,  // 0 - nieużywane
        Color.White,  // 1
        Color.White,  // 2
        Color.White,  // 3
        Color.White,  // 4
        Color.White,  // 5
        Color.White,  // 6
        Color.White,  // 7
        Color.White,  // 8
        Color.White,  // 9
        Color.White,  // 10
        Color.Green,  // 11
        Color.Blue,   // 12
        Color.Red,    // 13
        Color.Yellow, // 14
        Color.Purple  // 15
    };
    
    public static void Main()
    {
        Console.WriteLine("=== INTERAKTYWNY TEST MIRABOX ===\n");
        Console.WriteLine("Programowanie wszystkich 15 przycisków z figurami...\n");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        // Połącz z urządzeniem
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            Console.WriteLine("Sprawdź, czy urządzenie jest podłączone i czy ma sterownik WinUSB");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem!\n");
        
        var reader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        
        // Programuj wszystkie 15 przycisków
        Console.WriteLine("Programowanie przycisków...\n");
        
        for (int buttonNumber = 1; buttonNumber <= 15; buttonNumber++)
        {
            int shapeType = buttonNumber;
            Color shapeColor = ShapeColors[buttonNumber];
            
            // Dla przycisków 11-15 użyj kształtów 1-5 z kolorami
            if (buttonNumber > 10)
            {
                shapeType = buttonNumber - 10; // 1-5
            }
            
            var imageData = MiraboxImageGenerator.GenerateSimpleShape(
                shapeType: shapeType,
                backgroundColor: Color.Black,
                shapeColor: shapeColor
            );
            
            Console.Write($"Przycisk {buttonNumber:D2}: {ShapeNames[buttonNumber]}... ");
            
            if (programmer.ProgramButton(buttonNumber, imageData, packetSize: 512))
            {
                Console.WriteLine("✓");
            }
            else
            {
                Console.WriteLine("✗");
            }
            
            Thread.Sleep(100); // Krótka przerwa między przyciskami
        }
        
        Console.WriteLine("\n✓ Wszystkie przyciski zaprogramowane!\n");
        
        // Wyświetl mapowanie przycisków
        Console.WriteLine("=== MAPOWANIE PRZYCISKÓW ===");
        for (int i = 1; i <= 15; i++)
        {
            Console.WriteLine($"  Przycisk {i:D2}: {ShapeNames[i]}");
        }
        Console.WriteLine();
        
        // Odczytywanie naciśnięć przycisków
        Console.WriteLine("=== ODCZYTYWANIE NACISNIĘĆ ===");
        Console.WriteLine("Naciśnij przycisk na urządzeniu, aby zobaczyć jego nazwę.");
        Console.WriteLine("Naciśnij Ctrl+C, aby zakończyć.\n");
        
        var lastButton = -1;
        var lastState = "";
        
        while (true)
        {
            var buttonPress = reader.ReadButtonPress();
            
            if (buttonPress != null)
            {
                // Wyświetl tylko jeśli zmienił się stan lub przycisk
                if (buttonPress.ButtonNumber != lastButton || buttonPress.State != lastState)
                {
                    if (buttonPress.State == "pressed")
                    {
                        if (buttonPress.ButtonNumber >= 1 && buttonPress.ButtonNumber <= 15)
                        {
                            var shapeName = ShapeNames[buttonPress.ButtonNumber];
                            Console.WriteLine($"🎯 NACISNIĘTO PRZYCISK {buttonPress.ButtonNumber:D2}: {shapeName}");
                        }
                        else
                        {
                            Console.WriteLine($"🎯 NACISNIĘTO PRZYCISK {buttonPress.ButtonNumber}");
                        }
                    }
                    else if (buttonPress.State == "released")
                    {
                        if (buttonPress.ButtonNumber >= 1 && buttonPress.ButtonNumber <= 15)
                        {
                            var shapeName = ShapeNames[buttonPress.ButtonNumber];
                            Console.WriteLine($"   Zwolniono przycisk {buttonPress.ButtonNumber:D2}: {shapeName}");
                        }
                        else
                        {
                            Console.WriteLine($"   Zwolniono przycisk {buttonPress.ButtonNumber}");
                        }
                    }
                    
                    lastButton = buttonPress.ButtonNumber;
                    lastState = buttonPress.State;
                }
            }
            
            Thread.Sleep(10); // Krótka przerwa, żeby nie obciążać CPU
        }
    }
}

