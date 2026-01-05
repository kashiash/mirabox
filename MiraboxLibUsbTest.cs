using System;
using System.Drawing;
using System.Threading;

namespace mirabox;

/// <summary>
/// Test programowania przycisków przez LibUSB
/// </summary>
public class MiraboxLibUsbTest
{
    public static void Main()
    {
        Console.WriteLine("=== TEST PROGRAMOWANIA PRZYCISKÓW PRZEZ LIBUSB ===\n");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        Console.WriteLine("Krok 1: Łączenie z urządzeniem...");
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono!\n");
        
        var reader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        
        // Test 1: Programowanie jednego przycisku
        Console.WriteLine("Krok 2: Programowanie przycisku 1 z prostym kształtem...");
        var imageData = MiraboxImageGenerator.GenerateSimpleShape(
            shapeType: 1,  // Kółko
            backgroundColor: Color.Blue,
            shapeColor: Color.White
        );
        
        if (programmer.ProgramButton(1, imageData, packetSize: 512))
        {
            Console.WriteLine("✓ Przycisk 1 zaprogramowany!\n");
        }
        else
        {
            Console.WriteLine("✗ Błąd programowania przycisku 1\n");
            return;
        }
        
        Thread.Sleep(500);
        
        // Test 2: Programowanie kilku przycisków z różnymi kształtami
        Console.WriteLine("Krok 3: Programowanie przycisków 1-5 z różnymi kształtami...");
        for (int i = 1; i <= 5; i++)
        {
            var shapeData = MiraboxImageGenerator.GenerateSimpleShape(
                shapeType: i,
                backgroundColor: Color.Black,
                shapeColor: Color.White
            );
            
            Console.WriteLine($"Programowanie przycisku {i}...");
            if (programmer.ProgramButton(i, shapeData, packetSize: 512))
            {
                Console.WriteLine($"✓ Przycisk {i} zaprogramowany");
            }
            else
            {
                Console.WriteLine($"✗ Błąd programowania przycisku {i}");
            }
            
            Thread.Sleep(200);
        }
        
        Console.WriteLine("\n✓ Test zakończony pomyślnie!");
        Console.WriteLine("\nJeśli widzisz ikony na urządzeniu, kod działa poprawnie!");
    }
}

