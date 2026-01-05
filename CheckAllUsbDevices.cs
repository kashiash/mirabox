using System;
using System.Linq;

namespace mirabox;

/// <summary>
/// Sprawdza wszystkie urządzenia USB/HID w systemie
/// </summary>
public class CheckAllUsbDevices
{
    public static void Main()
    {
        Console.WriteLine("\n=== SPRAWDZANIE WSZYSTKICH URZĄDZEŃ USB/HID ===");
        Console.WriteLine("Szukam urządzeń z VID: 0x5548, PID: 0x6670\n");
        
        var allDevices = MiraboxDeviceFinder.GetAllUsbDevices();
        Console.WriteLine($"Znaleziono {allDevices.Count} urządzeń USB/HID w systemie\n");
        
        // Szukaj urządzeń z VID 5548
        var vid5548Devices = allDevices
            .Where(d => d.VendorId == "5548" || d.DeviceId?.Contains("VID_5548") == true)
            .ToList();
        
        if (vid5548Devices.Count > 0)
        {
            Console.WriteLine($"✓ Znaleziono {vid5548Devices.Count} urządzeń z VID 5548:\n");
            foreach (var device in vid5548Devices)
            {
                Console.WriteLine($"  Nazwa: {device.Name}");
                Console.WriteLine($"  DeviceID: {device.DeviceId}");
                Console.WriteLine($"  Status: {device.Status}");
                Console.WriteLine($"  VID: {device.VendorId ?? "brak"}");
                Console.WriteLine($"  PID: {device.ProductId ?? "brak"}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("✗ Nie znaleziono urządzeń z VID 5548");
        }
        
        // Szukaj urządzeń z PID 6670
        var pid6670Devices = allDevices
            .Where(d => d.ProductId == "6670" || d.DeviceId?.Contains("PID_6670") == true)
            .ToList();
        
        if (pid6670Devices.Count > 0)
        {
            Console.WriteLine($"\n✓ Znaleziono {pid6670Devices.Count} urządzeń z PID 6670:\n");
            foreach (var device in pid6670Devices)
            {
                Console.WriteLine($"  Nazwa: {device.Name}");
                Console.WriteLine($"  DeviceID: {device.DeviceId}");
                Console.WriteLine($"  Status: {device.Status}");
                Console.WriteLine($"  VID: {device.VendorId ?? "brak"}");
                Console.WriteLine($"  PID: {device.ProductId ?? "brak"}");
                Console.WriteLine();
            }
        }
        
        // Szukaj urządzeń Mirabox po nazwie
        var miraboxDevices = MiraboxDeviceFinder.FindMiraboxDevices();
        if (miraboxDevices.Count > 0)
        {
            Console.WriteLine($"\n✓ Znaleziono {miraboxDevices.Count} urządzeń Mirabox:\n");
            foreach (var device in miraboxDevices)
            {
                Console.WriteLine($"  Nazwa: {device.Name}");
                Console.WriteLine($"  DeviceID: {device.DeviceId}");
                Console.WriteLine($"  Status: {device.Status}");
                Console.WriteLine($"  VID: {device.VendorId ?? "brak"}");
                Console.WriteLine($"  PID: {device.ProductId ?? "brak"}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("\n✗ Nie znaleziono urządzeń Mirabox");
            Console.WriteLine("\nMożliwe przyczyny:");
            Console.WriteLine("1. Urządzenie nie jest podłączone");
            Console.WriteLine("2. Urządzenie używa sterownika WinUSB (nie HID)");
            Console.WriteLine("3. Urządzenie nie jest widoczne w systemie");
        }
    }
}

