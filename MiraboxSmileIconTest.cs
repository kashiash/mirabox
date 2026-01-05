using System;
using System.Drawing;
using Xunit;

namespace mirabox;

public class MiraboxSmileIconTest
{
    [Fact]
    public void GenerateSmileIcon()
    {
        Console.WriteLine("\n=== GENEROWANIE IKONY UŚMIECHU ===");
        
        // Ciemnoszare tło (jak w innych ikonach)
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Generuj uśmiechniętą emotikonę
        Console.WriteLine("Generowanie uśmiechniętej emotikony...");
        var iconData = MiraboxImageGenerator.GenerateEmoticon(
            happy: true,  // true = uśmiechnięta, false = smutna
            backgroundColor,
            width: 100,
            height: 100
        );
        
        Console.WriteLine($"✓ Wygenerowano ikonę uśmiechu: {iconData.Length} bajtów");
        
        // Zapisz do pliku dla podglądu
        try
        {
            System.IO.Directory.CreateDirectory("c:\\temp");
            System.IO.File.WriteAllBytes("c:\\temp\\smile_icon.jpg", iconData);
            Console.WriteLine("  Zapisano: c:\\temp\\smile_icon.jpg");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Nie udało się zapisać pliku: {ex.Message}");
        }
    }
    
    [Fact]
    public void GenerateSmileIconAndProgram()
    {
        Console.WriteLine("\n=== GENEROWANIE IKONY UŚMIECHU I PROGRAMOWANIE PRZYCISKU ===");
        
        // Ciemnoszare tło
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Generuj uśmiechniętą emotikonę
        Console.WriteLine("Generowanie uśmiechniętej emotikony...");
        var iconData = MiraboxImageGenerator.GenerateEmoticon(
            happy: true,
            backgroundColor,
            width: 100,
            height: 100
        );
        
        Console.WriteLine($"✓ Wygenerowano ikonę uśmiechu: {iconData.Length} bajtów");
        
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
        
        // Programuj przycisk 1 ikoną uśmiechu
        Console.WriteLine("\nProgramowanie przycisku 1 ikoną uśmiechu...");
        programmer.ProgramButton(1, iconData, 512);
        
        Console.WriteLine("\n✓ Gotowe! Sprawdź przycisk 1 na urządzeniu - powinien wyświetlać uśmiechniętą emotikonę.");
    }
    
    [Fact]
    public void GenerateAngryIcon()
    {
        Console.WriteLine("\n=== GENEROWANIE IKONY ZŁEJ ===");
        
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Generuj złą emotikonę
        Console.WriteLine("Generowanie złej emotikony...");
        var iconData = MiraboxImageGenerator.GenerateAngryEmoticon(backgroundColor);
        
        Console.WriteLine($"✓ Wygenerowano ikonę złości: {iconData.Length} bajtów");
        
        // Zapisz do pliku
        try
        {
            System.IO.Directory.CreateDirectory("c:\\temp");
            System.IO.File.WriteAllBytes("c:\\temp\\angry_icon.jpg", iconData);
            Console.WriteLine("  Zapisano: c:\\temp\\angry_icon.jpg");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Nie udało się zapisać pliku: {ex.Message}");
        }
    }
    
    [Fact]
    public void GenerateAngryIconAndProgram()
    {
        Console.WriteLine("\n=== GENEROWANIE IKONY ZŁEJ I PROGRAMOWANIE PRZYCISKU ===");
        
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        // Generuj złą emotikonę
        Console.WriteLine("Generowanie złej emotikony...");
        var iconData = MiraboxImageGenerator.GenerateAngryEmoticon(backgroundColor);
        
        Console.WriteLine($"✓ Wygenerowano ikonę złości: {iconData.Length} bajtów");
        
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
        
        // Programuj przycisk 1 ikoną złości
        Console.WriteLine("\nProgramowanie przycisku 1 ikoną złości...");
        programmer.ProgramButton(1, iconData, 512);
        
        Console.WriteLine("\n✓ Gotowe! Sprawdź przycisk 1 na urządzeniu - powinien wyświetlać złą emotikonę.");
    }
    
    [Fact]
    public void GenerateAllEmoticons()
    {
        Console.WriteLine("\n=== GENEROWANIE WSZYSTKICH EMOTIKON ===");
        
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        System.IO.Directory.CreateDirectory("c:\\temp");
        
        // Generuj uśmiechniętą
        Console.WriteLine("\nGenerowanie uśmiechniętej emotikony...");
        var smileIcon = MiraboxImageGenerator.GenerateEmoticon(true, backgroundColor);
        System.IO.File.WriteAllBytes("c:\\temp\\smile_happy.jpg", smileIcon);
        Console.WriteLine($"✓ Uśmiechnięta: {smileIcon.Length} bajtów -> c:\\temp\\smile_happy.jpg");
        
        // Generuj smutną
        Console.WriteLine("\nGenerowanie smutnej emotikony...");
        var sadIcon = MiraboxImageGenerator.GenerateEmoticon(false, backgroundColor);
        System.IO.File.WriteAllBytes("c:\\temp\\smile_sad.jpg", sadIcon);
        Console.WriteLine($"✓ Smutna: {sadIcon.Length} bajtów -> c:\\temp\\smile_sad.jpg");
        
        // Generuj złą
        Console.WriteLine("\nGenerowanie złej emotikony...");
        var angryIcon = MiraboxImageGenerator.GenerateAngryEmoticon(backgroundColor);
        System.IO.File.WriteAllBytes("c:\\temp\\smile_angry.jpg", angryIcon);
        Console.WriteLine($"✓ Zła: {angryIcon.Length} bajtów -> c:\\temp\\smile_angry.jpg");
        
        Console.WriteLine("\n✓ Gotowe! Wszystkie pliki zapisane w c:\\temp\\");
    }
}

