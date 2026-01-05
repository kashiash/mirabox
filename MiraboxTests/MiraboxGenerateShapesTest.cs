using System;
using System.Drawing;
using System.IO;
using Xunit;

namespace mirabox;

public class MiraboxGenerateShapesTest
{
    [Fact]
    public void GenerateAllShapesToFiles()
    {
        Console.WriteLine("\n=== GENEROWANIE KSZTAŁTÓW DO PLIKÓW JPG ===");
        
        var imagesDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
        Directory.CreateDirectory(imagesDirectory);
        
        // Kolory tła i kształtów
        var backgrounds = new[]
        {
            Color.FromArgb(30, 30, 30),    // Ciemnoszary
            Color.FromArgb(0, 50, 100),    // Ciemnoniebieski
            Color.FromArgb(50, 0, 50),     // Ciemnofioletowy
            Color.FromArgb(0, 50, 0),      // Ciemnozielony
            Color.FromArgb(50, 25, 0)      // Ciemnobrązowy
        };
        
        var shapeColors = new[]
        {
            Color.White,
            Color.Yellow,
            Color.Cyan,
            Color.LimeGreen,
            Color.Orange
        };
        
        var shapeNames = new[]
        {
            "circle", "square", "triangle", "diamond", "star",
            "plus", "cross", "heart", "arrow_up", "arrow_right"
        };
        
        Console.WriteLine($"Katalog docelowy: {imagesDirectory}\n");
        
        // Generuj wszystkie 10 kształtów
        for (int i = 1; i <= 10; i++)
        {
            var bgColor = backgrounds[(i - 1) % backgrounds.Length];
            var shapeColor = shapeColors[(i - 1) % shapeColors.Length];
            var shapeName = shapeNames[i - 1];
            
            var imageData = MiraboxImageGenerator.GenerateSimpleShape(i, bgColor, shapeColor);
            
            var filePath = Path.Combine(imagesDirectory, $"shape_{i:D2}_{shapeName}.jpg");
            File.WriteAllBytes(filePath, imageData);
            
            Console.WriteLine($"✓ Zapisano: {Path.GetFileName(filePath)} ({imageData.Length} bajtów)");
        }
        
        // Generuj dodatkowe warianty z różnymi kolorami
        Console.WriteLine("\nGenerowanie wariantów kolorystycznych...");
        
        var colorVariants = new[]
        {
            (Color.Black, Color.Red, "red"),
            (Color.Black, Color.Blue, "blue"),
            (Color.Black, Color.Green, "green"),
            (Color.White, Color.Black, "black_on_white"),
            (Color.FromArgb(255, 100, 0), Color.White, "orange_bg")
        };
        
        foreach (var (bg, fg, colorName) in colorVariants)
        {
            // Generuj tylko kilka popularnych kształtów dla każdego koloru
            for (int shapeType = 1; shapeType <= 5; shapeType++)
            {
                var imageData = MiraboxImageGenerator.GenerateSimpleShape(shapeType, bg, fg);
                var shapeName = shapeNames[shapeType - 1];
                var filePath = Path.Combine(imagesDirectory, $"shape_{shapeName}_{colorName}.jpg");
                File.WriteAllBytes(filePath, imageData);
                
                Console.WriteLine($"✓ Zapisano: {Path.GetFileName(filePath)}");
            }
        }
        
        Console.WriteLine($"\n✓ Zakończono generowanie kształtów!");
        Console.WriteLine($"Sprawdź katalog: {imagesDirectory}");
    }
}
