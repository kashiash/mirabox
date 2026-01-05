using System;
using System.Linq;
using HidSharp;

namespace mirabox;

/// <summary>
/// Szczegółowa diagnostyka urządzenia Mirabox - szuka we wszystkich możliwych miejscach
/// </summary>
public class MiraboxDeviceDiagnostics
{
    public static void Main()
    {
        Console.WriteLine("=== SZCZEGÓŁOWA DIAGNOSTYKA URZĄDZENIA MIRABOX ===\n");
        
        const int VID = 0x5548;
        const int PID = 0x6670;
        
        Console.WriteLine($"Szukam urządzenia: VID: 0x{VID:X4} (5548), PID: 0x{PID:X4} (6670)\n");
        
        // 1. Sprawdź przez WMI (Win32_PnPEntity)
        Console.WriteLine("1. SPRAWDZANIE PRZEZ WMI (Win32_PnPEntity)...");
        var allDevices = MiraboxDeviceFinder.GetAllUsbDevices();
        var miraboxDevices = allDevices
            .Where(d => d.VendorId == "5548" && d.ProductId?.StartsWith("6670") == true)
            .ToList();
        
        if (miraboxDevices.Count > 0)
        {
            Console.WriteLine($"   ✓ Znaleziono {miraboxDevices.Count} urządzeń przez WMI:\n");
            foreach (var device in miraboxDevices)
            {
                Console.WriteLine($"   Nazwa: {device.Name}");
                Console.WriteLine($"   DeviceID: {device.DeviceId}");
                Console.WriteLine($"   Status: {device.Status}");
                Console.WriteLine($"   Opis: {device.Description}");
                
                // Sprawdź czy to HID czy USB
                if (device.DeviceId?.Contains("HID\\") == true || device.DeviceId?.Contains("HID#") == true)
                {
                    Console.WriteLine($"   Typ: HID");
                }
                else if (device.DeviceId?.Contains("USB\\") == true || device.DeviceId?.Contains("USB#") == true)
                {
                    Console.WriteLine($"   Typ: USB (może wymagać WinUSB)");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("   ✗ Nie znaleziono przez WMI\n");
        }
        
        // 2. Sprawdź przez HidSharp (HID devices)
        Console.WriteLine("2. SPRAWDZANIE PRZEZ HidSharp (HID devices)...");
        try
        {
            var hidDevices = DeviceList.Local.GetHidDevices(VID, PID).ToArray();
            
            if (hidDevices.Length > 0)
            {
                Console.WriteLine($"   ✓ Znaleziono {hidDevices.Length} urządzeń HID przez HidSharp:\n");
                foreach (var device in hidDevices)
                {
                    Console.WriteLine($"   Nazwa: {device.GetProductName()}");
                    Console.WriteLine($"   Producent: {device.GetManufacturer()}");
                    Console.WriteLine($"   VID: 0x{device.VendorID:X4}");
                    Console.WriteLine($"   PID: 0x{device.ProductID:X4}");
                    Console.WriteLine($"   Max Input Report: {device.GetMaxInputReportLength()}");
                    Console.WriteLine($"   Max Output Report: {device.GetMaxOutputReportLength()}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("   ✗ Nie znaleziono przez HidSharp.GetHidDevices()");
                
                // Spróbuj we wszystkich urządzeniach HID
                Console.WriteLine("   Sprawdzam wszystkie urządzenia HID...");
                var allHidDevices = DeviceList.Local.GetAllDevices().OfType<HidDevice>();
                var matchingHid = allHidDevices
                    .Where(d => d.VendorID == VID && d.ProductID == PID)
                    .ToArray();
                
                if (matchingHid.Length > 0)
                {
                    Console.WriteLine($"   ✓ Znaleziono {matchingHid.Length} urządzeń HID we wszystkich urządzeniach:\n");
                    foreach (var device in matchingHid)
                    {
                        Console.WriteLine($"   Nazwa: {device.GetProductName()}");
                        Console.WriteLine($"   Producent: {device.GetManufacturer()}");
                        Console.WriteLine($"   Max Input Report: {device.GetMaxInputReportLength()}");
                        Console.WriteLine($"   Max Output Report: {device.GetMaxOutputReportLength()}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("   ✗ Nie znaleziono wśród wszystkich urządzeń HID\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Błąd podczas sprawdzania HidSharp: {ex.Message}\n");
        }
        
        // 3. Sprawdź przez LibUSB
        Console.WriteLine("3. SPRAWDZANIE PRZEZ LibUSB...");
        bool foundInLibUsbCheck = false;
        try
        {
            using var libUsbTransfer = new MiraboxLibUsbTransfer();
            
            // Sprawdź wszystkie urządzenia USB widoczne przez LibUSB
            Console.WriteLine("   Dostępne urządzenia USB przez LibUSB:");
            
            foreach (LibUsbDotNet.Main.UsbRegistry usbRegistry in LibUsbDotNet.UsbDevice.AllDevices)
            {
                if (usbRegistry.Vid == VID && usbRegistry.Pid == PID)
                {
                    foundInLibUsbCheck = true;
                    Console.WriteLine($"   ✓ Znaleziono: VID: 0x{usbRegistry.Vid:X4}, PID: 0x{usbRegistry.Pid:X4}, Name: {usbRegistry.Name}");
                }
            }
            
            if (!foundInLibUsbCheck)
            {
                Console.WriteLine("   ✗ Nie znaleziono w urządzeniach LibUSB");
            }
            
            // Spróbuj połączyć
            Console.WriteLine("   Próba połączenia przez LibUSB...");
            if (libUsbTransfer.Connect(VID, PID))
            {
                Console.WriteLine("   ✓ Połączono przez LibUSB!");
            }
            else
            {
                Console.WriteLine("   ✗ Nie można połączyć się przez LibUSB (wymaga sterownika WinUSB)");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Błąd podczas sprawdzania LibUSB: {ex.Message}\n");
        }
        
        // 4. Sprawdź możliwe ścieżki HID
        Console.WriteLine("4. SPRAWDZANIE MOŻLIWYCH ŚCIEŻEK HID...");
        if (miraboxDevices.Count > 0)
        {
            var device = miraboxDevices[0];
            
            // Spróbuj zbudować różne ścieżki HID
            var hidPath1 = MiraboxHidPathBuilder.BuildHidPath(device);
            if (!string.IsNullOrEmpty(hidPath1))
            {
                Console.WriteLine($"   Ścieżka HID (BuildHidPath): {hidPath1}");
                
                // Spróbuj połączyć
                using var reader = new MiraboxButtonReader();
                if (reader.Connect(hidPath1))
                {
                    Console.WriteLine("   ✓ Połączono przez tę ścieżkę!");
                    reader.Dispose();
                }
                else
                {
                    Console.WriteLine("   ✗ Nie można połączyć się przez tę ścieżkę");
                }
            }
            
            // Spróbuj bezpośrednio z DeviceID
            if (device.DeviceId != null)
            {
                var directPath = $@"\\?\{device.DeviceId}";
                Console.WriteLine($"   Ścieżka bezpośrednia (DeviceID): {directPath}");
                
                using var reader2 = new MiraboxButtonReader();
                if (reader2.Connect(directPath))
                {
                    Console.WriteLine("   ✓ Połączono przez bezpośrednią ścieżkę!");
                    reader2.Dispose();
                }
                else
                {
                    Console.WriteLine("   ✗ Nie można połączyć się przez bezpośrednią ścieżkę");
                }
            }
        }
        Console.WriteLine();
        
        // 5. Podsumowanie i rekomendacje
        Console.WriteLine("=== PODSUMOWANIE I REKOMENDACJE ===");
        
        bool foundInWmi = miraboxDevices.Count > 0;
        bool foundInHidSharp = false;
        
        try
        {
            foundInHidSharp = DeviceList.Local.GetHidDevices(VID, PID).Any() ||
                             DeviceList.Local.GetAllDevices().OfType<HidDevice>()
                                 .Any(d => d.VendorID == VID && d.ProductID == PID);
        }
        catch { }
        
        Console.WriteLine($"WMI (Win32_PnPEntity): {(foundInWmi ? "✓" : "✗")}");
        Console.WriteLine($"HidSharp (HID): {(foundInHidSharp ? "✓" : "✗")}");
        Console.WriteLine($"LibUSB: {(foundInLibUsbCheck ? "✓" : "✗")}");
        Console.WriteLine();
        
        if (foundInWmi && !foundInHidSharp && !foundInLibUsbCheck)
        {
            Console.WriteLine("REKOMENDACJA:");
            Console.WriteLine("Urządzenie jest widoczne w systemie, ale nie jako HID ani przez LibUSB.");
            Console.WriteLine("Prawdopodobnie używa domyślnego sterownika Windows USB.");
            Console.WriteLine();
            Console.WriteLine("Aby używać HID (bez dodatkowych sterowników):");
            Console.WriteLine("1. Otwórz Menedżer urządzeń");
            Console.WriteLine("2. Znajdź urządzenie 'Urządzenie wejściowe USB'");
            Console.WriteLine("3. Kliknij prawym → 'Update driver'");
            Console.WriteLine("4. Wybierz 'Browse my computer' → 'Let me pick'");
            Console.WriteLine("5. Wybierz 'Human Interface Devices' → 'HID-compliant device'");
            Console.WriteLine();
            Console.WriteLine("LUB użyj LibUSB z sterownikiem WinUSB (wymaga Zadig):");
            Console.WriteLine("1. Pobierz Zadig: https://zadig.akeo.ie/");
            Console.WriteLine("2. Uruchom jako Administrator");
            Console.WriteLine("3. Options → List All Devices");
            Console.WriteLine("4. Znajdź urządzenie Mirabox");
            Console.WriteLine("5. Wybierz 'WinUSB' → 'Replace Driver'");
        }
        else if (foundInHidSharp)
        {
            Console.WriteLine("REKOMENDACJA:");
            Console.WriteLine("✓ Urządzenie jest widoczne jako HID - możesz używać bez dodatkowych sterowników!");
            Console.WriteLine("Użyj: MiraboxHidTransfer + MiraboxHidButtonReader");
        }
        else if (foundInLibUsbCheck)
        {
            Console.WriteLine("REKOMENDACJA:");
            Console.WriteLine("✓ Urządzenie jest widoczne przez LibUSB - użyj sterownika WinUSB");
            Console.WriteLine("Użyj: MiraboxLibUsbTransfer + MiraboxLibUsbButtonReader");
        }
    }
}

