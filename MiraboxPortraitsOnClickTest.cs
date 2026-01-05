using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace mirabox;

public class MiraboxPortraitsOnClickTest
{
    [Fact]
    public void SetupShapesAndShowPortraitsOnClick()
    {
        Console.WriteLine("\n=== USTAWIANIE FIGUREK I WYŚWIETLANIE PORTRETÓW PO NACIŚNIĘCIU ===");
        
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
        Thread.Sleep(100);
        
        // KROK 1: Załaduj portrety z plików "gemini*"
        var portraitsDirectory = @"c:\temp";
        var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
        var portraitFiles = imageExtensions
            .SelectMany(ext => Directory.GetFiles(portraitsDirectory, ext, SearchOption.TopDirectoryOnly))
            .Where(f => Path.GetFileName(f).StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .Take(15)
            .ToArray();
        
        if (portraitFiles.Length == 0)
        {
            Console.WriteLine($"✗ Nie znaleziono plików portretów w: {portraitsDirectory}");
            return;
        }
        
        Console.WriteLine($"\n✓ Znaleziono {portraitFiles.Length} portretów");
        
        var backgroundColor = Color.FromArgb(40, 40, 40);
        var portraits = new Dictionary<int, byte[]>();
        
        // Przygotuj portrety (ale jeszcze nie programuj)
        for (int i = 0; i < portraitFiles.Length; i++)
        {
            var buttonNumber = i + 1;
            var imagePath = portraitFiles[i];
            
            Console.WriteLine($"  Przygotowanie portretu dla przycisku {buttonNumber}: {Path.GetFileName(imagePath)}");
            var iconData = MiraboxImageGenerator.LoadImageIcon(
                imagePath,
                backgroundColor,
                width: 100,
                height: 100,
                cropToCircle: true
            );
            portraits[buttonNumber] = iconData;
        }
        
        // KROK 2: Programuj wszystkie przyciski figurkami (kształtami)
        Console.WriteLine("\n=== PROGRAMOWANIE PRZYCISKÓW FIGURKAMI ===");
        
        var shapeNames = new[]
        {
            "circle", "square", "triangle", "diamond", "star",
            "plus", "cross", "heart", "arrow_up", "arrow_right",
            "circle", "square", "triangle", "diamond", "star"
        };
        
        var shapeColors = new[]
        {
            Color.White, Color.Yellow, Color.Cyan, Color.LimeGreen, Color.Orange,
            Color.Magenta, Color.Red, Color.Blue, Color.Green, Color.Purple,
            Color.White, Color.Yellow, Color.Cyan, Color.LimeGreen, Color.Orange
        };
        
        var backgrounds = new[]
        {
            Color.FromArgb(30, 30, 30), Color.FromArgb(0, 50, 100), Color.FromArgb(50, 0, 50),
            Color.FromArgb(0, 50, 0), Color.FromArgb(50, 25, 0)
        };
        
        for (int i = 1; i <= 15; i++)
        {
            var shapeType = ((i - 1) % 10) + 1; // 1-10 kształtów, powtarzane
            var bgColor = backgrounds[(i - 1) % backgrounds.Length];
            var shapeColor = shapeColors[i - 1];
            
            Console.WriteLine($"\nProgramowanie przycisku {i} figurką: {shapeNames[shapeType - 1]}");
            var shapeIcon = MiraboxImageGenerator.GenerateSimpleShape(shapeType, bgColor, shapeColor);
            programmer.ProgramButton(i, shapeIcon, 512);
            Thread.Sleep(50);
        }
        
        Console.WriteLine("\n✓ Wszystkie przyciski zaprogramowane figurkami");
        Console.WriteLine("Naciśnij przycisk, aby zobaczyć portret (ESC aby zakończyć)");
        
        // KROK 3: Nasłuchuj naciśnięć i zmieniaj ikony na portrety
        Console.WriteLine("\n=== NASŁUCHIWANIE NACIŚNIĘĆ ===");
        
        int lastButton = 0;
        string lastState = "";
        var buttonsWithPortraits = new HashSet<int>();
        
        while (true)
        {
            var buttonPress = libUsbReader.ReadButtonPress();
            
            if (buttonPress != null)
            {
                var btnNum = buttonPress.ButtonNumber;
                
                // Reaguj tylko na naciśnięcia (pressed) i tylko jeśli przycisk ma portret
                if (btnNum >= 1 && btnNum <= 15 && portraits.ContainsKey(btnNum))
                {
                    bool isNewEvent = (btnNum != lastButton) || (buttonPress.State != lastState);
                    
                    if (isNewEvent && buttonPress.State == "pressed")
                    {
                        Console.WriteLine($"\n🎯 PRZYCISK {btnNum:D2} NACIŚNIĘTY - ustawiam portret...");
                        
                        // Zaprogramuj przycisk portretem
                        if (programmer.ProgramButton(btnNum, portraits[btnNum], 512))
                        {
                            Console.WriteLine($"   ✓ Portret ustawiony na przycisku {btnNum:D2}");
                            buttonsWithPortraits.Add(btnNum);
                        }
                        else
                        {
                            Console.WriteLine($"   ✗ Błąd ustawiania portretu");
                        }
                        
                        lastButton = btnNum;
                        lastState = buttonPress.State;
                    }
                    else if (isNewEvent && buttonPress.State == "released")
                    {
                        // Opcjonalnie: po zwolnieniu można wrócić do figurki
                        // Na razie zostawiamy portret
                        lastButton = btnNum;
                        lastState = buttonPress.State;
                    }
                }
            }
            
            // Sprawdź czy użytkownik chce zakończyć (ESC)
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("\n\nZakończono nasłuchiwanie.");
                    break;
                }
            }
            
            Thread.Sleep(10); // Krótka przerwa
        }
        
        Console.WriteLine($"\n✓ Zakończono. Portrety ustawione na {buttonsWithPortraits.Count} przyciskach.");
    }
}

