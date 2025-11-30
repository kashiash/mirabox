using System;
using Xunit;

namespace mirabox;

public class MiraboxDeviceTest
{
    [Fact]
    public void TestDeviceCommunication()
    {
        Console.WriteLine("\n=== TEST KOMUNIKACJI Z URZĄDZENIEM ===");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Test 1: Wyślij komendę DIS (wake screen)
        Console.WriteLine("\n1. Wysyłanie komendy DIS (wake screen)...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; // C
        disCommand[1] = 0x52; // R
        disCommand[2] = 0x54; // T
        disCommand[3] = 0x00;
        disCommand[4] = 0x00;
        disCommand[5] = 0x44; // D
        disCommand[6] = 0x49; // I
        disCommand[7] = 0x53; // S
        libUsbReader.WriteData(disCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Test 2: Ustaw jasność na maksimum
        Console.WriteLine("\n2. Ustawianie jasności na 100%...");
        var ligCommand = new byte[512];
        ligCommand[0] = 0x43; // C
        ligCommand[1] = 0x52; // R
        ligCommand[2] = 0x54; // T
        ligCommand[3] = 0x00;
        ligCommand[4] = 0x00;
        ligCommand[5] = 0x4C; // L
        ligCommand[6] = 0x49; // I
        ligCommand[7] = 0x47; // G
        ligCommand[8] = 0x00;
        ligCommand[9] = 0x00;
        ligCommand[10] = 0x64; // 100 (0x64)
        libUsbReader.WriteData(ligCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Test 3: Wyczyść ekran
        Console.WriteLine("\n3. Czyszczenie ekranu...");
        var cleCommand = new byte[512];
        cleCommand[0] = 0x43; // C
        cleCommand[1] = 0x52; // R
        cleCommand[2] = 0x54; // T
        cleCommand[3] = 0x00;
        cleCommand[4] = 0x00;
        cleCommand[5] = 0x43; // C
        cleCommand[6] = 0x4C; // L
        cleCommand[7] = 0x45; // E
        cleCommand[8] = 0x00;
        cleCommand[9] = 0x00;
        cleCommand[10] = 0x00;
        cleCommand[11] = 0xFF; // Clear all (0xFF)
        libUsbReader.WriteData(cleCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Test 4: Odśwież
        Console.WriteLine("\n4. Odświeżanie ekranu (STP)...");
        var stpCommand = new byte[512];
        stpCommand[0] = 0x43; // C
        stpCommand[1] = 0x52; // R
        stpCommand[2] = 0x54; // T
        stpCommand[3] = 0x00;
        stpCommand[4] = 0x00;
        stpCommand[5] = 0x53; // S
        stpCommand[6] = 0x54; // T
        stpCommand[7] = 0x50; // P
        libUsbReader.WriteData(stpCommand, false);
        System.Threading.Thread.Sleep(100);
        
        Console.WriteLine("\n✓ Test zakończony - sprawdź czy ekran zareagował");
        Console.WriteLine("Jeśli ekran się wyczyścił, to komunikacja działa!");
    }
}
