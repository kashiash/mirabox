using System;
using System.Drawing;
using Xunit;

namespace mirabox;

public class MiraboxPortraitIconTest
{
    [Fact]
    public void ConvertPortraitToIcon()
    {
        Console.WriteLine("\n=== KONWERSJA PORTRETU NA IKONĘ ===");
        
        // Podaj ścieżkę do pliku z portretem
        // Przykłady:
        // var imagePath = @"c:\Users\Programista\source\repos\mirabox\Images\portrait.jpg";
        // var imagePath = @"c:\temp\portrait.png";
        
        var imagePath = @"c:\temp\portrait.jpg"; // ZMIEŃ NA SWOJĄ ŚCIEŻKĘ
        
        if (!System.IO.File.Exists(imagePath))
        {
            Console.WriteLine($"✗ Plik nie istnieje: {imagePath}");
            Console.WriteLine("Podaj prawidłową ścieżkę do pliku z portretem.");
            return;
        }
        
        // Ciemnoszare tło (jak w innych ikonach)
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Konwertuj portret na okrągłą ikonę 100x100px
        Console.WriteLine($"\nŁadowanie portretu: {imagePath}");
        var iconData = MiraboxImageGenerator.LoadImageIcon(
            imagePath, 
            backgroundColor, 
            width: 100, 
            height: 100, 
            cropToCircle: true  // Okrągły crop dla portretu
        );
        
        Console.WriteLine($"✓ Utworzono ikonę: {iconData.Length} bajtów");
        Console.WriteLine($"  Zapisano wersję debug: c:\\temp\\icon_{System.IO.Path.GetFileNameWithoutExtension(imagePath)}.jpg");
        
        // Opcjonalnie: zaprogramuj przycisk (odkomentuj jeśli chcesz)
        /*
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Programuj przycisk 1 portretem
        Console.WriteLine("\nProgramowanie przycisku 1 portretem...");
        programmer.ProgramButton(1, iconData, 512);
        
        Console.WriteLine("\n✓ Gotowe! Sprawdź przycisk 1 na urządzeniu.");
        */
    }
    
    [Fact]
    public void ConvertPortraitToIconAndProgram()
    {
        Console.WriteLine("\n=== KONWERSJA PORTRETU I PROGRAMOWANIE PRZYCISKU ===");
        
        var imagePath = @"c:\temp\portrait.jpg"; // ZMIEŃ NA SWOJĄ ŚCIEŻKĘ
        
        if (!System.IO.File.Exists(imagePath))
        {
            Console.WriteLine($"✗ Plik nie istnieje: {imagePath}");
            Console.WriteLine("Podaj prawidłową ścieżkę do pliku z portretem.");
            return;
        }
        
        // Ciemnoszare tło
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Konwertuj portret na okrągłą ikonę
        Console.WriteLine($"\nŁadowanie portretu: {imagePath}");
        var iconData = MiraboxImageGenerator.LoadImageIcon(
            imagePath, 
            backgroundColor, 
            width: 100, 
            height: 100, 
            cropToCircle: true
        );
        
        Console.WriteLine($"✓ Utworzono ikonę: {iconData.Length} bajtów");
        
        // Połącz z urządzeniem
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem");
        
        var libUsbReader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(libUsbReader);
        
        // Wyślij inicjalizację
        Console.WriteLine("\nInicjalizacja urządzenia...");
        var disCommand = new byte[512];
        disCommand[0] = 0x43; disCommand[1] = 0x52; disCommand[2] = 0x54;
        disCommand[5] = 0x44; disCommand[6] = 0x49; disCommand[7] = 0x53;
        libUsbReader.WriteData(disCommand, false);
        System.Threading.Thread.Sleep(100);
        
        // Programuj przycisk 1 portretem
        Console.WriteLine("\nProgramowanie przycisku 1 portretem...");
        programmer.ProgramButton(1, iconData, 512);
        
        Console.WriteLine("\n✓ Gotowe! Sprawdź przycisk 1 na urządzeniu - powinien wyświetlać portret w okrągłym kadrze.");
    }
}

