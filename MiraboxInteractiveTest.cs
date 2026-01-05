using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace mirabox;

/// <summary>
/// Interaktywny test - programuje wszystkie przyciski i odczytuje naciśnięcia
/// </summary>
public class MiraboxInteractiveTest
{
    // Mapowanie numeru przycisku na nazwę figury
    private static readonly string[] ShapeNames = new[]
    {
        "", // 0 - nieużywane
        "Kółko",      // 1
        "Kwadrat",    // 2
        "Trójkąt",    // 3
        "Romb",       // 4
        "Gwiazda",    // 5
        "Plus",       // 6
        "Krzyżyk",   // 7
        "Serce",      // 8
        "Strzałka w górę",    // 9
        "Strzałka w prawo",   // 10
        "Kółko (zielone)",    // 11 - powtórzenie z kolorami
        "Kwadrat (niebieski)", // 12
        "Trójkąt (czerwony)",  // 13
        "Romb (żółty)",        // 14
        "Gwiazda (fioletowa)"  // 15
    };
    
    // Kolory dla przycisków 11-15
    private static readonly Color[] ShapeColors = new[]
    {
        Color.White,  // 0 - nieużywane
        Color.White,  // 1
        Color.White,  // 2
        Color.White,  // 3
        Color.White,  // 4
        Color.White,  // 5
        Color.White,  // 6
        Color.White,  // 7
        Color.White,  // 8
        Color.White,  // 9
        Color.White,  // 10
        Color.Green,  // 11
        Color.Blue,   // 12
        Color.Red,    // 13
        Color.Yellow, // 14
        Color.Purple  // 15
    };
    
    /// <summary>
    /// Programuje jeden przycisk portretem (losowym z plików lub generowanym)
    /// </summary>
    private static void ProgramSingleButtonWithPortrait(MiraboxButtonProgrammer programmer, int buttonNumber, List<byte[]> portraits, Dictionary<int, bool> buttonBackgrounds)
    {
        var random = new Random();
        
        byte[] portraitImageData;
        
        // Jeśli mamy załadowane portrety z plików, użyj losowego portretu
        if (portraits.Count > 0)
        {
            int portraitIndex = random.Next(portraits.Count);
            var selectedPortrait = portraits[portraitIndex];
            portraitImageData = selectedPortrait;
            Console.Write($"Przycisk {buttonNumber:D2}: Portret {portraitIndex + 1}/{portraits.Count} z pliku... ");
        }
        else
        {
            // Jeśli nie ma portretów z plików, użyj generowanego portretu
            // Przełącz tło przycisku (białe ↔ czerwone)
            if (!buttonBackgrounds.ContainsKey(buttonNumber))
            {
                buttonBackgrounds[buttonNumber] = true; // Domyślnie białe
            }
            
            // Przełącz tło
            buttonBackgrounds[buttonNumber] = !buttonBackgrounds[buttonNumber];
            var currentBg = buttonBackgrounds[buttonNumber] ? Color.White : Color.Red;
            var bgName = buttonBackgrounds[buttonNumber] ? "białe" : "czerwone";
            
            // Wygeneruj ikonę portretu z aktualnym tłem
            portraitImageData = MiraboxImageGenerator.GeneratePortraitIcon(
                backgroundColor: currentBg
            );
            Console.Write($"Przycisk {buttonNumber:D2}: Generowany portret (tło: {bgName})... ");
        }
        
        // Zaprogramuj przycisk z ikoną portretu
        if (programmer.ProgramButton(buttonNumber, portraitImageData, packetSize: 512))
        {
            Console.WriteLine("✓");
        }
        else
        {
            Console.WriteLine("✗");
        }
    }
    
    /// <summary>
    /// Programuje wszystkie 15 przycisków portretami (losowymi z plików lub generowanymi)
    /// Każdy przycisk dostaje inny portret (jeśli jest wystarczająco dużo portretów)
    /// </summary>
    private static void ProgramAllButtonsWithPortraits(MiraboxButtonProgrammer programmer, List<byte[]> portraits, Dictionary<int, bool> buttonBackgrounds)
    {
        var random = new Random();
        
        Console.WriteLine("\n=== PROGRAMOWANIE WSZYSTKICH PRZYCISKÓW PORTRETAMI ===");
        
        // Jeśli mamy portrety z plików, przygotuj listę indeksów do losowego wyboru
        // Używamy losowego mieszania, aby każdy przycisk dostał inny portret (jeśli to możliwe)
        List<int>? availablePortraitIndices = null;
        if (portraits.Count > 0)
        {
            // Utwórz listę indeksów i wymieszaj ją losowo
            availablePortraitIndices = new List<int>();
            for (int idx = 0; idx < portraits.Count; idx++)
            {
                availablePortraitIndices.Add(idx);
            }
            // Wymieszaj listę (Fisher-Yates shuffle)
            for (int i = availablePortraitIndices.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = availablePortraitIndices[i];
                availablePortraitIndices[i] = availablePortraitIndices[j];
                availablePortraitIndices[j] = temp;
            }
        }
        
        for (int i = 1; i <= 15; i++)
        {
            byte[] portraitImageData;
            
            // Jeśli mamy załadowane portrety z plików, użyj losowego portretu (każdy inny)
            if (portraits.Count > 0 && availablePortraitIndices != null)
            {
                // Użyj indeksu z wymieszanej listy (modulo, jeśli portretów jest mniej niż 15)
                int portraitIndex = availablePortraitIndices[(i - 1) % availablePortraitIndices.Count];
                var selectedPortrait = portraits[portraitIndex];
                portraitImageData = selectedPortrait;
                Console.Write($"Przycisk {i:D2}: Portret {portraitIndex + 1}/{portraits.Count} z pliku... ");
            }
            else
            {
                // Jeśli nie ma portretów z plików, użyj generowanego portretu
                // Przełącz tło przycisku (białe ↔ czerwone)
                if (!buttonBackgrounds.ContainsKey(i))
                {
                    buttonBackgrounds[i] = true; // Domyślnie białe
                }
                
                // Przełącz tło
                buttonBackgrounds[i] = !buttonBackgrounds[i];
                var currentBg = buttonBackgrounds[i] ? Color.White : Color.Red;
                var bgName = buttonBackgrounds[i] ? "białe" : "czerwone";
                
                // Wygeneruj ikonę portretu z aktualnym tłem
                portraitImageData = MiraboxImageGenerator.GeneratePortraitIcon(
                    backgroundColor: currentBg
                );
                Console.Write($"Przycisk {i:D2}: Generowany portret (tło: {bgName})... ");
            }
            
            // Zaprogramuj przycisk z ikoną portretu
            if (programmer.ProgramButton(i, portraitImageData, packetSize: 512))
            {
                Console.WriteLine("✓");
            }
            else
            {
                Console.WriteLine("✗");
            }
            
            Thread.Sleep(50); // Krótka przerwa między przyciskami
        }
        
        Console.WriteLine("\n✓ Wszystkie przyciski zaprogramowane portretami!\n");
    }
    
    /// <summary>
    /// Programuje wszystkie 15 przycisków losowymi figurkami (kształtami)
    /// </summary>
    private static void ProgramAllButtonsWithRandomShapes(MiraboxButtonProgrammer programmer)
    {
        var random = new Random();
        
        // Dostępne kształty (1-10)
        var availableShapes = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        
        // Dostępne kolory kształtów
        var shapeColors = new[]
        {
            Color.White, Color.Yellow, Color.Cyan, Color.LimeGreen, Color.Orange,
            Color.Magenta, Color.Red, Color.Blue, Color.Green, Color.Purple,
            Color.Pink, Color.Gold, Color.Silver, Color.Turquoise, Color.Violet
        };
        
        // Dostępne kolory tła
        var backgrounds = new[]
        {
            Color.FromArgb(30, 30, 30), Color.FromArgb(0, 50, 100), Color.FromArgb(50, 0, 50),
            Color.FromArgb(0, 50, 0), Color.FromArgb(50, 25, 0), Color.FromArgb(20, 20, 40),
            Color.FromArgb(40, 20, 0), Color.FromArgb(0, 30, 30), Color.FromArgb(30, 0, 30),
            Color.FromArgb(10, 10, 10)
        };
        
        Console.WriteLine("\n=== PROGRAMOWANIE WSZYSTKICH PRZYCISKÓW LOSOWYMI FIGURKAMI ===");
        
        for (int i = 1; i <= 15; i++)
        {
            // Losowy kształt
            var shapeType = availableShapes[random.Next(availableShapes.Length)];
            
            // Losowy kolor kształtu
            var shapeColor = shapeColors[random.Next(shapeColors.Length)];
            
            // Losowe tło
            var bgColor = backgrounds[random.Next(backgrounds.Length)];
            
            Console.Write($"Przycisk {i:D2}: Kształt {shapeType}, kolor {shapeColor.Name}, tło RGB({bgColor.R},{bgColor.G},{bgColor.B})... ");
            
            var shapeIcon = MiraboxImageGenerator.GenerateSimpleShape(shapeType, bgColor, shapeColor);
            
            if (programmer.ProgramButton(i, shapeIcon, packetSize: 512))
            {
                Console.WriteLine("✓");
            }
            else
            {
                Console.WriteLine("✗");
            }
            
            Thread.Sleep(50); // Krótka przerwa między przyciskami
        }
        
        Console.WriteLine("\n✓ Wszystkie przyciski zaprogramowane losowymi figurkami!\n");
    }
    
    public static void Main()
    {
        Console.WriteLine("=== INTERAKTYWNY TEST MIRABOX ===\n");
        Console.WriteLine("Programowanie wszystkich 15 przycisków z figurkami...\n");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        // Połącz z urządzeniem
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            Console.WriteLine("Sprawdź, czy urządzenie jest podłączone i czy ma sterownik WinUSB");
            return;
        }
        
        Console.WriteLine("✓ Połączono z urządzeniem!\n");
        
        var reader = new MiraboxLibUsbButtonReader(libUsbTransfer);
        var programmer = new MiraboxButtonProgrammer(reader);
        var backgroundSetter = new MiraboxBackgroundSetter(reader);
        
        // Załaduj portrety z plików
        Console.WriteLine("=== ŁADOWANIE PORTRETÓW Z PLIKÓW ===");
        var portraitsDirectory = @"c:\temp";
        var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
        var portraitFiles = imageExtensions
            .SelectMany(ext => Directory.Exists(portraitsDirectory) 
                ? Directory.GetFiles(portraitsDirectory, ext, SearchOption.TopDirectoryOnly)
                : Array.Empty<string>())
            .Where(f => Path.GetFileName(f).StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .Take(15)
            .ToArray();
        
        var portraits = new List<byte[]>();
        var backgroundColor = Color.FromArgb(40, 40, 40);
        
        if (portraitFiles.Length > 0)
        {
            Console.WriteLine($"✓ Znaleziono {portraitFiles.Length} portretów w: {portraitsDirectory}");
            foreach (var imagePath in portraitFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(imagePath)}");
                var iconData = MiraboxImageGenerator.LoadImageIcon(
                    imagePath,
                    backgroundColor,
                    width: 100,
                    height: 100,
                    cropToCircle: true
                );
                portraits.Add(iconData);
            }
        }
        else
        {
            Console.WriteLine($"⚠ Nie znaleziono portretów w: {portraitsDirectory}");
            Console.WriteLine("  (Szukam plików zaczynających się od 'gemini')");
            Console.WriteLine("  Program będzie używał generowanych portretów.\n");
        }
        
        Console.WriteLine();
        
        // Ustaw tło ekranu (eksperymentalne - może nie działać)
        Console.WriteLine("=== USTAWIENIE TŁA EKRANU ===");
        Console.WriteLine("Próba ustawienia tła ekranu na czarne...");
        var backgroundImage = MiraboxBackgroundSetter.GenerateBackgroundImage(
            backgroundColor: Color.Black,
            width: 320,
            height: 240
        );
        backgroundSetter.SetBackground(backgroundImage, packetSize: 512);
        Console.WriteLine();
        
        // Programuj wszystkie 15 przycisków - wszystkie z figurkami (kształtami)
        Console.WriteLine("Programowanie przycisków z figurkami...\n");
        
        ProgramAllButtonsWithRandomShapes(programmer);
        
        Console.WriteLine("\n✓ Wszystkie przyciski zaprogramowane!\n");
        
        // Timer na 2 minuty - zakończy program
        var startTime = DateTime.Now;
        var endTime = startTime.AddMinutes(2);
        Console.WriteLine($"⏰ Program zakończy działanie za 2 minuty (o {endTime:HH:mm:ss})\n");
        
        // Słownik do przechowywania stanu tła dla każdego przycisku
        // true = białe tło, false = czerwone tło
        var buttonBackgrounds = new Dictionary<int, bool>();
        
        // Inicjalizuj wszystkie przyciski z białym tłem
        for (int i = 1; i <= 15; i++)
        {
            buttonBackgrounds[i] = true; // Białe tło na start
        }
        
        // Odczytywanie naciśnięć przycisków
        Console.WriteLine("=== ODCZYTYWANIE NACISNIĘĆ ===");
        Console.WriteLine("Naciśnij przycisk 1 - wszystkie przyciski zostaną zaprogramowane losowymi figurkami.");
        Console.WriteLine("Naciśnij inny przycisk (2-15) - wszystkie przyciski zostaną zaprogramowane portretami.");
        Console.WriteLine("Program zakończy się automatycznie po 2 minutach.\n");
        Console.WriteLine("UWAGA: Mirabox zawsze wysyła ten sam format danych dla każdego przycisku:");
        Console.WriteLine("  - Format: ACK + OK + numer przycisku (1-15) + stan (0=released, 1=pressed)");
        Console.WriteLine("  - Nie można skonfigurować, jakie dane wysyła każdy przycisk - to jest stałe w urządzeniu.\n");
        
        var lastButton = -1;
        var lastState = "";
        var readCount = 0;
        var lastDebugTime = DateTime.Now;
        var lastDataTime = DateTime.MinValue;
        var lastTimeCheck = DateTime.Now;
        
        Console.WriteLine("Czekam na naciśnięcia przycisków...\n");
        
        while (true)
        {
            var buttonPress = reader.ReadButtonPress();
            readCount++;
            
            // Sprawdź czy minęły 2 minuty
            var now2 = DateTime.Now;
            if (now2 >= endTime)
            {
                var elapsed = now2 - startTime;
                Console.WriteLine($"\n⏰ Minęły 2 minuty ({elapsed.TotalMinutes:F1} min). Kończę działanie programu...");
                Console.WriteLine("✓ Program zakończony.");
                return;
            }
            
            // Wyświetl status co 5 sekund (żeby pokazać, że działa)
            if ((now2 - lastDebugTime).TotalSeconds >= 5)
            {
                var remaining = endTime - now2;
                Console.WriteLine($"[Czekam... próba {readCount}, pozostało: {remaining.TotalSeconds:F0}s]");
                lastDebugTime = now2;
            }
            
            if (buttonPress != null)
            {
                var btnNum = buttonPress.ButtonNumber;
                
                // Reaguj na naciśnięcia (pressed) LUB zwolnienia (released) - przełącz tło i ustaw ikonę portretu
                // Ponieważ urządzenie może wysyłać dane tylko przy zwolnieniu, reaguj na oba stany
                if (btnNum >= 1 && btnNum <= 15)
                {
                    // Sprawdź czy to nowe zdarzenie (nie powtarzaj dla tego samego przycisku w tym samym stanie)
                    bool isNewEvent = (btnNum != lastButton) || (buttonPress.State != lastState);
                    
                    // Reaguj na oba stany - urządzenie może wysyłać dane tylko przy zwolnieniu
                    if (isNewEvent)
                    {
                        // Specjalna obsługa przycisku 1 - programuj wszystkie przyciski losowymi figurkami
                        // Reaguj na "released" bo urządzenie wysyła dane głównie przy zwolnieniu
                        if (btnNum == 1 && (buttonPress.State == "pressed" || buttonPress.State == "released"))
                        {
                            Console.WriteLine($"\n🎯 PRZYCISK 01 - {buttonPress.State.ToUpper()} - Programuję wszystkie przyciski losowymi figurkami...");
                            ProgramAllButtonsWithRandomShapes(programmer);
                            lastButton = btnNum;
                            lastState = buttonPress.State;
                            continue; // Przejdź do następnej iteracji
                        }
                        
                        // Po naciśnięciu/zwolnieniu innego przycisku (nie 1) - ustaw portret tylko na naciśniętym przycisku
                        // Reaguj na "released" bo urządzenie wysyła dane głównie przy zwolnieniu
                        if (btnNum != 1 && (buttonPress.State == "pressed" || buttonPress.State == "released"))
                        {
                            Console.WriteLine($"\n🎯 PRZYCISK {btnNum:D2} - {buttonPress.State.ToUpper()} - Ustawiam portret na tym przycisku...");
                            ProgramSingleButtonWithPortrait(programmer, btnNum, portraits, buttonBackgrounds);
                            lastButton = btnNum;
                            lastState = buttonPress.State;
                            continue; // Przejdź do następnej iteracji
                        }
                    }
                }
                else if (btnNum > 0)
                {
                    Console.WriteLine($"\n🎯 PRZYCISK {btnNum} - {buttonPress.State}");
                }
            }
            
            Thread.Sleep(10); // Krótka przerwa, żeby nie obciążać CPU
        }
    }
}

