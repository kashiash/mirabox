using System;
using System.Drawing;
using System.Threading;

namespace mirabox;

/// <summary>
/// Test połączenia z Mirabox - próbuje HID i LibUSB
/// </summary>
public class MiraboxConnectionTest
{
    public static void Main()
    {
        Console.WriteLine("=== TEST POŁĄCZENIA Z MIRABOX ===\n");
        
        // Sprawdź czy urządzenie jest widoczne w systemie
        Console.WriteLine("Krok 1: Sprawdzanie urządzeń w systemie...");
        var allDevices = MiraboxDeviceFinder.GetAllUsbDevices();
        var miraboxDevices = MiraboxDeviceFinder.FindMiraboxDevices();
        
        if (miraboxDevices.Count > 0)
        {
            Console.WriteLine($"✓ Znaleziono {miraboxDevices.Count} urządzeń Mirabox w systemie:");
            foreach (var device in miraboxDevices)
            {
                Console.WriteLine($"  - {device.Name}");
                Console.WriteLine($"    DeviceID: {device.DeviceId}");
                Console.WriteLine($"    Status: {device.Status}");
            }
        }
        else
        {
            Console.WriteLine("✗ Nie znaleziono urządzeń Mirabox w systemie");
            Console.WriteLine("  Sprawdź, czy urządzenie jest podłączone");
            return;
        }
        
        Console.WriteLine();
        
        // Test 1: Próba połączenia przez HID
        Console.WriteLine("Krok 2: Próba połączenia przez HID (bez dodatkowych sterowników)...");
        using var hidTransfer = new MiraboxHidTransfer();
        
        bool hidConnected = hidTransfer.Connect(0x5548, 0x6670);
        
        if (hidConnected)
        {
            Console.WriteLine("✓ Połączono przez HID!\n");
            
            // Test programowania przycisku przez HID
            Console.WriteLine("Test programowania przycisku przez HID...");
            var hidReader = new MiraboxHidButtonReader(hidTransfer);
            var hidProgrammer = new MiraboxButtonProgrammer(hidReader);
            
            var imageData = MiraboxImageGenerator.GenerateSimpleShape(
                shapeType: 1,
                backgroundColor: Color.Blue,
                shapeColor: Color.White
            );
            
            if (hidProgrammer.ProgramButton(1, imageData, packetSize: 512))
            {
                Console.WriteLine("✓ Przycisk zaprogramowany przez HID!\n");
            }
            else
            {
                Console.WriteLine("✗ Błąd programowania przycisku przez HID\n");
            }
        }
        else
        {
            Console.WriteLine("✗ Nie można połączyć się przez HID");
            Console.WriteLine("  Możliwe przyczyny:");
            Console.WriteLine("  - Urządzenie używa sterownika WinUSB (nie HID)");
            Console.WriteLine("  - Urządzenie nie jest widoczne jako HID w systemie\n");
        }
        
        // Test 2: Próba połączenia przez LibUSB (wymaga WinUSB)
        Console.WriteLine("Krok 3: Próba połączenia przez LibUSB (wymaga sterownika WinUSB)...");
        
        try
        {
            using var libUsbTransfer = new MiraboxLibUsbTransfer();
            bool libUsbConnected = libUsbTransfer.Connect(0x5548, 0x6670);
            
            if (libUsbConnected)
            {
                Console.WriteLine("✓ Połączono przez LibUSB!\n");
                
                // Test programowania przycisku przez LibUSB
                Console.WriteLine("Test programowania przycisku przez LibUSB...");
                var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
                var libUsbProgrammer = new MiraboxButtonProgrammer(libUsbReader);
                
                var imageData2 = MiraboxImageGenerator.GenerateSimpleShape(
                    shapeType: 2,
                    backgroundColor: Color.Green,
                    shapeColor: Color.White
                );
                
                if (libUsbProgrammer.ProgramButton(1, imageData2, packetSize: 512))
                {
                    Console.WriteLine("✓ Przycisk zaprogramowany przez LibUSB!\n");
                }
                else
                {
                    Console.WriteLine("✗ Błąd programowania przycisku przez LibUSB\n");
                }
            }
            else
            {
                Console.WriteLine("✗ Nie można połączyć się przez LibUSB");
                Console.WriteLine("  Możliwe przyczyny:");
                Console.WriteLine("  - Brak sterownika WinUSB (zainstaluj przez Zadig)");
                Console.WriteLine("  - Urządzenie używa innego sterownika\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd podczas próby połączenia przez LibUSB: {ex.Message}\n");
        }
        
        // Podsumowanie
        Console.WriteLine("=== PODSUMOWANIE ===");
        if (hidConnected)
        {
            Console.WriteLine("✓ HID działa - możesz używać bez dodatkowych sterowników!");
        }
        else
        {
            Console.WriteLine("✗ HID nie działa - urządzenie prawdopodobnie używa sterownika WinUSB");
            Console.WriteLine("  Aby używać HID, odinstaluj sterownik WinUSB przez Zadig lub Menedżer urządzeń");
        }
    }
}

