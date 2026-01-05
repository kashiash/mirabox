using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Xunit;

namespace mirabox;

public class MiraboxPortraitsToButtonsTest
{
    [Fact]
    public void LoadPortraitsAndProgramButtons()
    {
        Console.WriteLine("\n=== KONWERSJA ZDJĘĆ PORTRETÓW NA IKONY I PROGRAMOWANIE PRZYCISKÓW ===");
        
        // Katalog ze zdjęciami portretów
        var portraitsDirectory = @"c:\temp";
        
        if (!Directory.Exists(portraitsDirectory))
        {
            Console.WriteLine($"✗ Katalog nie istnieje: {portraitsDirectory}");
            Console.WriteLine("\nUtwórz katalog i umieść w nim zdjęcia portretów (JPG, PNG, BMP).");
            Console.WriteLine("Przykład: c:\\temp\\portraits\\portrait1.jpg");
            return;
        }
        
        // Znajdź wszystkie pliki obrazów zaczynające się od "gemini"
        var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
        var imageFiles = imageExtensions
            .SelectMany(ext => Directory.GetFiles(portraitsDirectory, ext, SearchOption.TopDirectoryOnly))
            .Where(f => Path.GetFileName(f).StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .Take(15) // Maksymalnie 15 przycisków
            .ToArray();
        
        if (imageFiles.Length == 0)
        {
            Console.WriteLine($"✗ Nie znaleziono żadnych zdjęć w katalogu: {portraitsDirectory}");
            Console.WriteLine("Dodaj pliki JPG, PNG lub BMP do tego katalogu.");
            return;
        }
        
        Console.WriteLine($"\n✓ Znaleziono {imageFiles.Length} zdjęć:");
        foreach (var file in imageFiles)
        {
            Console.WriteLine($"  - {Path.GetFileName(file)}");
        }
        
        // Ciemnoszare tło (jak w innych ikonach)
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Konwertuj wszystkie zdjęcia na ikony
        Console.WriteLine("\n=== KONWERSJA ZDJĘĆ NA IKONY ===");
        var icons = new System.Collections.Generic.Dictionary<int, byte[]>();
        
        for (int i = 0; i < imageFiles.Length; i++)
        {
            var imagePath = imageFiles[i];
            var buttonNumber = i + 1;
            
            Console.WriteLine($"\n[{buttonNumber}/{imageFiles.Length}] Konwertowanie: {Path.GetFileName(imagePath)}");
            
            var iconData = MiraboxImageGenerator.LoadImageIcon(
                imagePath,
                backgroundColor,
                width: 100,
                height: 100,
                cropToCircle: true  // Okrągły crop dla portretów
            );
            
            icons[buttonNumber] = iconData;
            Console.WriteLine($"  ✓ Utworzono ikonę: {iconData.Length} bajtów");
        }
        
        // Połącz z urządzeniem
        Console.WriteLine("\n=== POŁĄCZENIE Z URZĄDZENIEM ===");
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            Console.WriteLine("Upewnij się, że urządzenie Mirabox jest podłączone.");
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
        
        // Programuj wszystkie przyciski
        Console.WriteLine("\n=== PROGRAMOWANIE PRZYCISKÓW ===");
        foreach (var kvp in icons)
        {
            var buttonNumber = kvp.Key;
            var iconData = kvp.Value;
            var fileName = Path.GetFileName(imageFiles[buttonNumber - 1]);
            
            Console.WriteLine($"\nProgramowanie przycisku {buttonNumber} ({fileName})...");
            programmer.ProgramButton(buttonNumber, iconData, 512);
            
            // Krótka przerwa między przyciskami
            System.Threading.Thread.Sleep(50);
        }
        
        Console.WriteLine($"\n✓ Gotowe! Zaprogramowano {icons.Count} przycisków.");
        Console.WriteLine("Sprawdź urządzenie - każdy przycisk powinien wyświetlać portret w okrągłym kadrze.");
    }
    
    [Fact]
    public void LoadSinglePortraitAndProgram()
    {
        Console.WriteLine("\n=== KONWERSJA POJEDYNCZEGO PORTRETU NA IKONĘ ===");
        
        // Podaj ścieżkę do pojedynczego zdjęcia
        var imagePath = @"c:\temp\portrait.jpg"; // ZMIEŃ NA SWOJĄ ŚCIEŻKĘ
        var buttonNumber = 1; // Na który przycisk zaprogramować
        
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"✗ Plik nie istnieje: {imagePath}");
            Console.WriteLine("Podaj prawidłową ścieżkę do pliku z portretem.");
            return;
        }
        
        // Ciemnoszare tło
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Konwertuj portret na okrągłą ikonę
        Console.WriteLine($"\nŁadowanie portretu: {Path.GetFileName(imagePath)}");
        var iconData = MiraboxImageGenerator.LoadImageIcon(
            imagePath,
            backgroundColor,
            width: 100,
            height: 100,
            cropToCircle: true
        );
        
        Console.WriteLine($"✓ Utworzono ikonę: {iconData.Length} bajtów");
        Console.WriteLine($"  Zapisano wersję debug: c:\\temp\\icon_{Path.GetFileNameWithoutExtension(imagePath)}.jpg");
        
        // Połącz z urządzeniem
        Console.WriteLine("\n=== POŁĄCZENIE Z URZĄDZENIEM ===");
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
        
        // Programuj przycisk
        Console.WriteLine($"\nProgramowanie przycisku {buttonNumber}...");
        programmer.ProgramButton(buttonNumber, iconData, 512);
        
        Console.WriteLine($"\n✓ Gotowe! Sprawdź przycisk {buttonNumber} na urządzeniu.");
    }
    
    [Fact]
    public void ConvertPortraitsToIconsOnly()
    {
        Console.WriteLine("\n=== KONWERSJA ZDJĘĆ PORTRETÓW NA IKONY (BEZ PROGRAMOWANIA) ===");
        
        var portraitsDirectory = @"c:\temp";
        var outputDirectory = @"c:\temp\icons"; // Katalog docelowy dla ikon
        
        if (!Directory.Exists(portraitsDirectory))
        {
            Console.WriteLine($"✗ Katalog nie istnieje: {portraitsDirectory}");
            return;
        }
        
        Directory.CreateDirectory(outputDirectory);
        
        // Znajdź wszystkie pliki obrazów zaczynające się od "gemini"
        var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
        var imageFiles = imageExtensions
            .SelectMany(ext => Directory.GetFiles(portraitsDirectory, ext, SearchOption.TopDirectoryOnly))
            .Where(f => Path.GetFileName(f).StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToArray();
        
        if (imageFiles.Length == 0)
        {
            Console.WriteLine($"✗ Nie znaleziono żadnych zdjęć w katalogu: {portraitsDirectory}");
            return;
        }
        
        Console.WriteLine($"\n✓ Znaleziono {imageFiles.Length} zdjęć");
        
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Konwertuj wszystkie zdjęcia na ikony
        Console.WriteLine("\n=== KONWERSJA ===");
        for (int i = 0; i < imageFiles.Length; i++)
        {
            var imagePath = imageFiles[i];
            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var outputPath = Path.Combine(outputDirectory, $"icon_{fileName}.jpg");
            
            Console.WriteLine($"\n[{i + 1}/{imageFiles.Length}] {Path.GetFileName(imagePath)}");
            
            var iconData = MiraboxImageGenerator.LoadImageIcon(
                imagePath,
                backgroundColor,
                width: 100,
                height: 100,
                cropToCircle: true
            );
            
            File.WriteAllBytes(outputPath, iconData);
            Console.WriteLine($"  ✓ Zapisano: {Path.GetFileName(outputPath)} ({iconData.Length} bajtów)");
        }
        
        Console.WriteLine($"\n✓ Gotowe! Wszystkie ikony zapisane w: {outputDirectory}");
    }
}

