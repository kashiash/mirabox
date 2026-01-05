using System;
using System.Drawing;
using System.Threading;

namespace mirabox;

/// <summary>
/// Szybki test komunikacji z Mirabox przez HID
/// </summary>
public class MiraboxQuickTest
{
    public static void Main()
    {
        Console.WriteLine("=== SZYBKI TEST MIRABOX (HID) ===\n");
        
        // Test 1: Połączenie
        Console.WriteLine("Test 1: Połączenie z urządzeniem...");
        using var hidTransfer = new MiraboxHidTransfer();
        
        if (!hidTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ BŁĄD: Nie można połączyć się z urządzeniem");
            Console.WriteLine("\nSprawdź:");
            Console.WriteLine("1. Czy urządzenie jest podłączone");
            Console.WriteLine("2. Czy urządzenie jest widoczne jako HID:");
            Console.WriteLine("   dotnet run --project . CheckHidDevices");
            Console.WriteLine("3. Czy urządzenie nie używa sterownika WinUSB");
            return;
        }
        
        Console.WriteLine("✓ Połączono!\n");
        
        // Test 2: Programowanie jednego przycisku
        Console.WriteLine("Test 2: Programowanie przycisku 1 z prostym kształtem...");
        var reader = new MiraboxHidButtonReader(hidTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        
        var imageData = MiraboxImageGenerator.GenerateSimpleShape(
            shapeType: 1,  // Kółko
            backgroundColor: Color.Blue,
            shapeColor: Color.White
        );
        
        if (programmer.ProgramButton(1, imageData, packetSize: 512))
        {
            Console.WriteLine("✓ Przycisk zaprogramowany!\n");
        }
        else
        {
            Console.WriteLine("✗ BŁĄD: Nie udało się zaprogramować przycisku\n");
            return;
        }
        
        // Test 3: Odczytywanie przycisków (5 sekund)
        Console.WriteLine("Test 3: Odczytywanie naciśnięć przycisków (5 sekund)...");
        Console.WriteLine("Naciśnij przycisk na urządzeniu...\n");
        
        var startTime = DateTime.Now;
        var buttonPressed = false;
        
        while ((DateTime.Now - startTime).TotalSeconds < 5)
        {
            var buttonPress = reader.ReadButtonPress();
            
            if (buttonPress != null)
            {
                Console.WriteLine($"✓ Odebrano: Przycisk {buttonPress.ButtonNumber} - {buttonPress.State}");
                buttonPressed = true;
            }
            
            Thread.Sleep(10);
        }
        
        if (!buttonPressed)
        {
            Console.WriteLine("⚠ Nie odebrano żadnych naciśnięć (może być normalne jeśli nie nacisnąłeś przycisku)");
        }
        
        Console.WriteLine("\n=== TEST ZAKOŃCZONY ===");
        Console.WriteLine("\nJeśli wszystkie testy przeszły pomyślnie, kod działa poprawnie!");
    }
}

