using Xunit;
using System;
using System.Linq;

namespace mirabox;

public class MiraboxTests
{
    [Fact]
    public void FindAllUsbDevices_ShouldReturnDevices()
    {
        // Act
        var devices = MiraboxDeviceFinder.GetAllUsbDevices();

        // Assert
        Assert.NotNull(devices);
        Assert.True(devices.Count > 0, "Nie znaleziono żadnych urządzeń USB/HID");

        Console.WriteLine($"\n=== ZNALEZIONE URZĄDZENIA USB/HID ===");
        Console.WriteLine($"Liczba urządzeń: {devices.Count}");
        Console.WriteLine("----------------------------------------");

        foreach (var device in devices)
        {
            Console.WriteLine($"Nazwa: {device.Name}");
            Console.WriteLine($"ID: {device.DeviceId}");
            Console.WriteLine($"Opis: {device.Description}");
            Console.WriteLine($"Status: {device.Status}");
            if (!string.IsNullOrEmpty(device.VendorId))
                Console.WriteLine($"VID: {device.VendorId}");
            if (!string.IsNullOrEmpty(device.ProductId))
                Console.WriteLine($"PID: {device.ProductId}");
            Console.WriteLine("----------------------------------------");
        }
    }

    [Fact]
    public void FindMiraboxDevices_ShouldFindMirabox()
    {
        // Act
        var miraboxDevices = MiraboxDeviceFinder.FindMiraboxDevices();

        // Assert
        Assert.NotNull(miraboxDevices);

        Console.WriteLine($"\n=== URZĄDZENIA MIRABOX ===");
        Console.WriteLine($"Znaleziono: {miraboxDevices.Count} urządzeń");

        if (miraboxDevices.Count == 0)
        {
            Console.WriteLine("UWAGA: Nie znaleziono urządzenia Mirabox!");
            Console.WriteLine("Sprawdź czy urządzenie jest podłączone i czy nazwa zawiera 'mirabox'");
            
            // Pokaż wszystkie urządzenia HID jako pomoc
            var allDevices = MiraboxDeviceFinder.GetAllUsbDevices();
            var hidDevices = allDevices.Where(d => d.DeviceId?.Contains("HID") == true).ToList();
            
            Console.WriteLine($"\nZnalezione urządzenia HID ({hidDevices.Count}):");
            foreach (var device in hidDevices.Take(10))
            {
                Console.WriteLine($"  - {device.Name} ({device.DeviceId})");
            }
        }
        else
        {
            foreach (var device in miraboxDevices)
            {
                Console.WriteLine($"\nNazwa: {device.Name}");
                Console.WriteLine($"ID: {device.DeviceId}");
                Console.WriteLine($"VID: {device.VendorId ?? "N/A"}");
                Console.WriteLine($"PID: {device.ProductId ?? "N/A"}");
                Console.WriteLine($"Status: {device.Status}");
            }
        }
    }

    [Fact]
    public void ReadMiraboxButtons_ShouldDisplayButtonPresses()
    {
        // Arrange
        var miraboxDevices = MiraboxDeviceFinder.FindMiraboxDevices();
        
        if (miraboxDevices.Count == 0)
        {
            Console.WriteLine("Pomijam test - urządzenie Mirabox nie jest podłączone");
            return;
        }

        var device = miraboxDevices.First();
        Console.WriteLine($"\n=== TEST ODCZYTU PRZYCISKÓW MIRABOX ===");
        Console.WriteLine($"Urządzenie: {device.Name}");
        Console.WriteLine($"DeviceID: {device.DeviceId}");

        // Próba znalezienia ścieżki HID
        string? hidPath = null;
        if (device.DeviceId != null && device.DeviceId.Contains("VID_") && device.DeviceId.Contains("PID_"))
        {
            // Buduj ścieżkę HID
            var vidIndex = device.DeviceId.IndexOf("VID_");
            var pidIndex = device.DeviceId.IndexOf("PID_");
            
            if (vidIndex >= 0 && pidIndex >= 0)
            {
                var vidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, vidIndex + 4);
                var pidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, pidIndex + 4);
                
                if (vidEnd < 0) vidEnd = device.DeviceId.Length;
                if (pidEnd < 0) pidEnd = device.DeviceId.Length;
                
                var vid = device.DeviceId.Substring(vidIndex + 4, vidEnd - vidIndex - 4);
                var pid = device.DeviceId.Substring(pidIndex + 4, pidEnd - pidIndex - 4);
                
                // Szukaj numeru seryjnego
                var serialIndex = device.DeviceId.IndexOf("#", pidIndex);
                var serial = "00000001"; // Domyślny
                if (serialIndex >= 0)
                {
                    var serialEnd = device.DeviceId.IndexOfAny(new[] { '#', '\\' }, serialIndex + 1);
                    if (serialEnd < 0) serialEnd = device.DeviceId.Length;
                    if (serialEnd > serialIndex + 1)
                    {
                        serial = device.DeviceId.Substring(serialIndex + 1, serialEnd - serialIndex - 1);
                    }
                }
                
                hidPath = $@"\\?\HID#VID_{vid}&PID_{pid}#{serial}#{{4d1e55b2-f16f-11cf-88cb-001111000030}}";
                Console.WriteLine($"Ścieżka HID: {hidPath}");
            }
        }

        if (string.IsNullOrEmpty(hidPath))
        {
            Console.WriteLine("Nie można zbudować ścieżki HID - sprawdź DeviceID");
            return;
        }

        // Act - próba połączenia i odczytu
        using var reader = new MiraboxButtonReader();
        if (reader.Connect(hidPath))
        {
            Console.WriteLine("Połączono z urządzeniem!");
            Console.WriteLine("\nCzytanie danych (naciśnij przyciski na urządzeniu)...");
            Console.WriteLine("Naciśnij Ctrl+C aby przerwać\n");

            for (int i = 0; i < 10; i++) // Próba odczytu 10 razy
            {
                var data = reader.ReadData();
                if (data != null && data.Length > 0)
                {
                    Console.WriteLine($"\n=== ODCZYT #{i + 1} ===");
                    Console.WriteLine(reader.FormatButtonData(data));
                }
                else
                {
                    Console.WriteLine($"Odczyt #{i + 1}: Brak danych");
                }
                
                System.Threading.Thread.Sleep(500); // Czekaj 500ms między odczytami
            }
        }
        else
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Console.WriteLine($"Nie można połączyć się z urządzeniem. Błąd: {error}");
            Console.WriteLine("Możliwe przyczyny:");
            Console.WriteLine("  - Urządzenie nie jest urządzeniem HID");
            Console.WriteLine("  - Brak uprawnień do urządzenia");
            Console.WriteLine("  - Urządzenie jest używane przez inny proces");
        }
    }

    [Fact]
    public void ProgramAllButtons_ShouldSendImagesToEachButton()
    {
        // Arrange
        var miraboxDevices = MiraboxDeviceFinder.FindMiraboxDevices();
        
        // Jeśli nie znaleziono po nazwie, spróbuj znaleźć po VID/PID (5548/6670)
        if (miraboxDevices.Count == 0)
        {
            Console.WriteLine("Nie znaleziono urządzenia po nazwie, próbuję po VID/PID 5548/6670...");
            miraboxDevices = MiraboxDeviceFinder.FindDevicesByVidPid("5548", "6670");
        }
        
        if (miraboxDevices.Count == 0)
        {
            Console.WriteLine("Pomijam test - urządzenie Mirabox nie jest podłączone");
            Console.WriteLine("Szukane urządzenia:");
            Console.WriteLine("  - Nazwa zawierająca 'mirabox'");
            Console.WriteLine("  - VID: 5548, PID: 6670");
            return;
        }

        var device = miraboxDevices.First();
        Console.WriteLine($"\n=== PROGRAMOWANIE PRZYCISKÓW MIRABOX ===");
        Console.WriteLine($"Urządzenie USB: {device.Name}");
        Console.WriteLine($"DeviceID: {device.DeviceId}");

        // Znajdź odpowiadające urządzenie HID
        var hidDevice = MiraboxHidPathBuilder.FindHidDevice(device);
        if (hidDevice == null)
        {
            Console.WriteLine("Nie znaleziono odpowiadającego urządzenia HID");
            Console.WriteLine("Próbuję zbudować ścieżkę bezpośrednio z urządzenia USB...");
        }
        else
        {
            Console.WriteLine($"Znaleziono urządzenie HID: {hidDevice.Name}");
            Console.WriteLine($"HID DeviceID: {hidDevice.DeviceId}");
            device = hidDevice; // Użyj urządzenia HID
        }

        // Buduj ścieżkę HID
        var hidPath = MiraboxHidPathBuilder.BuildHidPath(device);
        if (string.IsNullOrEmpty(hidPath))
        {
            Console.WriteLine("Nie można zbudować ścieżki HID - sprawdź DeviceID");
            return;
        }
        
        Console.WriteLine($"Ścieżka HID: {hidPath}");

        // Act - połączenie i programowanie
        // Spróbuj najpierw LibUSB (interrupt transfer), potem fallback do HID
        Console.WriteLine("\n=== PRÓBA 1: LibUSB (interrupt transfer) ===");
        Console.WriteLine("Próba połączenia przez LibUSB...");
        
        try
        {
            using var libUsbTransfer = new MiraboxLibUsbTransfer();
            
            // VID: 0x5548, PID: 0x6670
            if (libUsbTransfer.Connect(0x5548, 0x6670))
            {
                Console.WriteLine("✓ Połączono przez LibUSB!");
                
                // Utwórz adapter, który używa LibUSB
                var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
                var programmer = new MiraboxButtonProgrammer(libUsbReader);
                
                // Programuj wszystkie przyciski
                Console.WriteLine("\nGenerowanie obrazów i programowanie przycisków...");
                Console.WriteLine("Używam protokołu Mirabox CRT z pakietami 512 bajtów");
                Console.WriteLine("(Na podstawie: https://github.com/4ndv/mirajazz i https://github.com/rigor789/mirabox-streamdock-node)");
                
                var packetSizes = new[] { 512, 1024 };
                bool success = false;
                
                foreach (var packetSize in packetSizes)
                {
                    Console.WriteLine($"\n=== Próba z rozmiarem pakietu: {packetSize} bajtów ===");
                    
                    try
                    {
                        programmer.ProgramAllButtons(buttonNumber => 
                        {
                            var imageData = MiraboxImageGenerator.GenerateSimplePattern(buttonNumber);
                            Console.WriteLine($"  Wygenerowano obraz dla przycisku {buttonNumber}: {imageData.Length} bajtów");
                            return imageData;
                        }, packetSize);
                        
                        success = true;
                        Console.WriteLine($"\n✓ Sukces z rozmiarem pakietu {packetSize} bajtów");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Błąd z rozmiarem pakietu {packetSize}: {ex.Message}");
                    }
                }
                
                Console.WriteLine("\n=== PROGRAMOWANIE ZAKOŃCZONE ===");
                Console.WriteLine("Sprawdź urządzenie - każdy przycisk powinien wyświetlać swój obraz");
                return;
            }
            else
            {
                Console.WriteLine("✗ Nie można połączyć się przez LibUSB");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd LibUSB: {ex.Message}");
        }
        
        Console.WriteLine("\n=== PRÓBA 2: Fallback do HID ===");
        
        // Fallback do HID
        using var reader = new MiraboxButtonReader();
        if (reader.Connect(hidPath))
        {
            Console.WriteLine("✓ Połączono z urządzeniem przez HID!");
            
            var programmer = new MiraboxButtonProgrammer(reader);
            
            // Programuj wszystkie przyciski z różnymi obrazami
            Console.WriteLine("\nGenerowanie obrazów i programowanie przycisków...");
            Console.WriteLine("Używam protokołu Mirabox z pakietami 512 bajtów");
            Console.WriteLine("(Na podstawie: https://github.com/4ndv/mirajazz i https://github.com/rigor789/mirabox-streamdock-node)");
            
            // Spróbuj różne rozmiary pakietów
            // Mirabox używa pakietów 512 lub 1024 bajtów (nie 64!)
            var packetSizes = new[] { 512, 1024, 64 };
            bool success = false;
            
            foreach (var packetSize in packetSizes)
            {
                Console.WriteLine($"\n=== Próba z rozmiarem pakietu: {packetSize} bajtów ===");
                
                try
                {
                    programmer.ProgramAllButtons(buttonNumber => 
                    {
                        // Generuj obraz dla każdego przycisku
                        var imageData = MiraboxImageGenerator.GenerateSimplePattern(buttonNumber);
                        Console.WriteLine($"  Wygenerowano obraz dla przycisku {buttonNumber}: {imageData.Length} bajtów");
                        return imageData;
                    }, packetSize);
                    
                    success = true;
                    Console.WriteLine($"\n✓ Sukces z rozmiarem pakietu {packetSize} bajtów");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Błąd z rozmiarem pakietu {packetSize}: {ex.Message}");
                    if (packetSize == packetSizes.Last())
                    {
                        Console.WriteLine("Wszystkie rozmiary pakietów nie powiodły się");
                    }
                }
            }
            
            Console.WriteLine("\n=== PROGRAMOWANIE ZAKOŃCZONE ===");
            Console.WriteLine("Sprawdź urządzenie - każdy przycisk powinien wyświetlać swój obraz");
        }
        else
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Console.WriteLine($"✗ Nie można połączyć się z urządzeniem. Błąd: {error}");
            Console.WriteLine("Możliwe przyczyny:");
            Console.WriteLine("  - Urządzenie nie jest urządzeniem HID");
            Console.WriteLine("  - Brak uprawnień do urządzenia");
            Console.WriteLine("  - Urządzenie jest używane przez inny proces");
        }
    }

    [Fact]
    public void ProgramSingleButton_ShouldSendImageToSpecificButton()
    {
        // Arrange
        var miraboxDevices = MiraboxDeviceFinder.FindMiraboxDevices();
        
        if (miraboxDevices.Count == 0)
        {
            Console.WriteLine("Pomijam test - urządzenie Mirabox nie jest podłączone");
            return;
        }

        var device = miraboxDevices.First();
        var buttonNumber = 1; // Programuj przycisk 1
        
        Console.WriteLine($"\n=== PROGRAMOWANIE POJEDYNCZEGO PRZYCISKU ===");
        Console.WriteLine($"Przycisk: {buttonNumber}");
        Console.WriteLine($"Urządzenie: {device.Name}");

        // Buduj ścieżkę HID (użyj tego samego kodu co wcześniej)
        string? hidPath = null;
        if (device.DeviceId != null && device.DeviceId.Contains("VID_") && device.DeviceId.Contains("PID_"))
        {
            var vidIndex = device.DeviceId.IndexOf("VID_");
            var pidIndex = device.DeviceId.IndexOf("PID_");
            
            if (vidIndex >= 0 && pidIndex >= 0)
            {
                var vidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, vidIndex + 4);
                var pidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, pidIndex + 4);
                
                if (vidEnd < 0) vidEnd = device.DeviceId.Length;
                if (pidEnd < 0) pidEnd = device.DeviceId.Length;
                
                var vid = device.DeviceId.Substring(vidIndex + 4, vidEnd - vidIndex - 4);
                var pid = device.DeviceId.Substring(pidIndex + 4, pidEnd - pidIndex - 4);
                
                var serialIndex = device.DeviceId.IndexOf("#", pidIndex);
                var serial = "00000001";
                if (serialIndex >= 0)
                {
                    var serialEnd = device.DeviceId.IndexOfAny(new[] { '#', '\\' }, serialIndex + 1);
                    if (serialEnd < 0) serialEnd = device.DeviceId.Length;
                    if (serialEnd > serialIndex + 1)
                    {
                        serial = device.DeviceId.Substring(serialIndex + 1, serialEnd - serialIndex - 1);
                    }
                }
                
                hidPath = $@"\\?\HID#VID_{vid}&PID_{pid}#{serial}#{{4d1e55b2-f16f-11cf-88cb-001111000030}}";
            }
        }

        if (string.IsNullOrEmpty(hidPath))
        {
            Console.WriteLine("Nie można zbudować ścieżki HID");
            return;
        }

        // Act
        using var reader = new MiraboxButtonReader();
        if (reader.Connect(hidPath))
        {
            var programmer = new MiraboxButtonProgrammer(reader);
            var imageData = MiraboxImageGenerator.GenerateTestImage(buttonNumber);
            
            Console.WriteLine($"Generowanie obrazu dla przycisku {buttonNumber}...");
            Console.WriteLine($"Rozmiar obrazu: {imageData.Length} bajtów");
            
            var result = programmer.ProgramButton(buttonNumber, imageData);
            
            if (result)
            {
                Console.WriteLine($"\n✓ Przycisk {buttonNumber} został zaprogramowany pomyślnie!");
            }
            else
            {
                Console.WriteLine($"\n✗ Nie udało się zaprogramować przycisku {buttonNumber}");
            }
        }
        else
        {
            Console.WriteLine("Nie można połączyć się z urządzeniem");
        }
    }
}

