using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace mirabox;

/// <summary>
/// Prosty przykład użycia biblioteki Mirabox bez dodatkowych sterowników
/// Używa HID (standardowy sterownik Windows) - nie wymaga WinUSB ani libusb-win32
/// </summary>
public class MiraboxSimpleExample
{
    public static void Main()
    {
        Console.WriteLine("=== PRZYKŁAD UŻYCIA MIRABOX (HID - BEZ DODATKOWYCH STEROWNIKÓW) ===\n");
        
        // 1. Połącz z urządzeniem przez HID (standardowy sterownik Windows)
        using var hidTransfer = new MiraboxHidTransfer();
        
        Console.WriteLine("Krok 1: Łączenie z urządzeniem...");
        if (!hidTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem Mirabox");
            Console.WriteLine("\nMożliwe przyczyny:");
            Console.WriteLine("1. Urządzenie nie jest podłączone");
            Console.WriteLine("2. Urządzenie używa sterownika WinUSB (zainstalowanego przez Zadig)");
            Console.WriteLine("   → W takim przypadku użyj MiraboxLibUsbTransfer zamiast MiraboxHidTransfer");
            Console.WriteLine("3. Urządzenie nie jest widoczne jako HID w systemie");
            Console.WriteLine("\nSprawdź urządzenia HID:");
            Console.WriteLine("   dotnet run --project . CheckHidDevices");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem!\n");
        
        // 2. Utwórz reader i programmer
        var reader = new MiraboxHidButtonReader(hidTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        
        // 3. Wyświetl ikonki na przyciskach
        Console.WriteLine("Krok 2: Programowanie przycisków z ikonkami...\n");
        
        // Przykład 1: Proste kształty geometryczne
        for (int i = 1; i <= 10; i++)
        {
            var imageData = MiraboxImageGenerator.GenerateSimpleShape(
                shapeType: i,  // 1=kółko, 2=kwadrat, 3=trójkąt, itd.
                backgroundColor: Color.Black,
                shapeColor: Color.White
            );
            
            Console.WriteLine($"Programowanie przycisku {i}...");
            programmer.ProgramButton(i, imageData, packetSize: 512);
            Thread.Sleep(50); // Krótka przerwa między przyciskami
        }
        
        // Przykład 2: Ładowanie ikon z plików (jeśli istnieją)
        var imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        if (Directory.Exists(imagesDirectory))
        {
            var imageFiles = Directory.GetFiles(imagesDirectory, "*.jpg")
                .OrderBy(f => f)
                .Take(15)
                .ToArray();
            
            if (imageFiles.Length > 0)
            {
                Console.WriteLine($"\nŁadowanie {imageFiles.Length} ikon z plików...\n");
                for (int i = 0; i < imageFiles.Length && i < 15; i++)
                {
                    var imageData = File.ReadAllBytes(imageFiles[i]);
                    Console.WriteLine($"Programowanie przycisku {i + 1} z pliku: {Path.GetFileName(imageFiles[i])}");
                    programmer.ProgramButton(i + 1, imageData, packetSize: 512);
                    Thread.Sleep(50);
                }
            }
        }
        
        Console.WriteLine("\n✓ Wszystkie przyciski zaprogramowane!\n");
        
        // 4. Odczytywanie naciśnięć przycisków
        Console.WriteLine("Krok 3: Odczytywanie naciśnięć przycisków...");
        Console.WriteLine("(Naciśnij przycisk na urządzeniu lub Ctrl+C aby zakończyć)\n");
        
        var lastButton = -1;
        var lastState = "";
        
        while (true)
        {
            var buttonPress = reader.ReadButtonPress();
            
            if (buttonPress != null)
            {
                // Wyświetl tylko jeśli zmienił się stan
                if (buttonPress.ButtonNumber != lastButton || buttonPress.State != lastState)
                {
                    Console.WriteLine($"Przycisk {buttonPress.ButtonNumber}: {buttonPress.State}");
                    lastButton = buttonPress.ButtonNumber;
                    lastState = buttonPress.State;
                }
            }
            
            Thread.Sleep(10); // Krótka przerwa, żeby nie obciążać CPU
        }
    }
}

