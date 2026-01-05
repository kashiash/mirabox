using System;
using System.Linq;
using HidSharp;

namespace mirabox;

public class CheckHidDevices
{
    public static void Main()
    {
        Console.WriteLine("\n=== SPRAWDZANIE URZĄDZEŃ HID ===");
        Console.WriteLine("Szukam urządzeń HID z VID: 0x5548, PID: 0x6670\n");
        
        try
        {
            // Pobierz wszystkie urządzenia HID
            var allHidDevices = DeviceList.Local.GetAllDevices().OfType<HidDevice>().ToList();
            Console.WriteLine($"Znaleziono {allHidDevices.Count} urządzeń HID w systemie\n");
            
            // Szukaj urządzeń z konkretnym VID/PID
            var miraboxDevices = DeviceList.Local.GetHidDevices(0x5548, 0x6670).ToArray();
            
            if (miraboxDevices.Length == 0)
            {
                Console.WriteLine("✗ Nie znaleziono urządzenia HID z VID: 0x5548, PID: 0x6670");
                Console.WriteLine("\nSprawdzam wszystkie urządzenia HID z VID 5548...\n");
                
                // Szukaj wszystkich urządzeń z tym VID
                var allVid5548 = allHidDevices
                    .Where(d => d.VendorID == 0x5548)
                    .ToList();
                
                if (allVid5548.Count > 0)
                {
                    Console.WriteLine($"Znaleziono {allVid5548.Count} urządzeń HID z VID 0x5548:");
                    foreach (var device in allVid5548)
                    {
                        Console.WriteLine($"  - PID: 0x{device.ProductID:X4}, Name: {device.GetProductName()}");
                        Console.WriteLine($"    Manufacturer: {device.GetManufacturer()}");
                        Console.WriteLine($"    Max Input Report: {device.GetMaxInputReportLength()}");
                        Console.WriteLine($"    Max Output Report: {device.GetMaxOutputReportLength()}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("✗ Nie znaleziono żadnych urządzeń HID z VID 0x5548");
                    Console.WriteLine("\nMożliwe przyczyny:");
                    Console.WriteLine("1. Urządzenie używa sterownika WinUSB (nie HID)");
                    Console.WriteLine("2. Urządzenie nie jest widoczne jako HID w systemie");
                    Console.WriteLine("3. Urządzenie wymaga innego sterownika");
                }
            }
            else
            {
                Console.WriteLine($"✓ Znaleziono {miraboxDevices.Length} urządzeń HID Mirabox:\n");
                foreach (var device in miraboxDevices)
                {
                    Console.WriteLine($"  Urządzenie: {device.GetProductName()}");
                    Console.WriteLine($"  Producent: {device.GetManufacturer()}");
                    Console.WriteLine($"  VID: 0x{device.VendorID:X4}");
                    Console.WriteLine($"  PID: 0x{device.ProductID:X4}");
                    Console.WriteLine($"  Max Input Report: {device.GetMaxInputReportLength()} bajtów");
                    Console.WriteLine($"  Max Output Report: {device.GetMaxOutputReportLength()} bajtów");
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd podczas sprawdzania urządzeń HID: {ex.Message}");
            Console.WriteLine($"   Szczegóły: {ex}");
        }
    }
}

