using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Svg;

namespace mirabox;

public static class MiraboxImageGenerator
{
    public static byte[] GenerateTestImage(int buttonNumber, int width = 320, int height = 240)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Tło
        graphics.Clear(Color.DarkBlue);
        
        // Tekst z numerem przycisku
        using var font = new Font("Arial", 48, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        var text = $"BTN {buttonNumber}";
        var textSize = graphics.MeasureString(text, font);
        var x = (width - textSize.Width) / 2;
        var y = (height - textSize.Height) / 2;
        graphics.DrawString(text, font, brush, x, y);
        
        // Ramka
        using var pen = new Pen(Color.Yellow, 5);
        graphics.DrawRectangle(pen, 5, 5, width - 10, height - 10);
        
        // Konwersja do tablicy bajtów (PNG)
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public static byte[] GenerateColorImage(int buttonNumber, Color backgroundColor, int width = 320, int height = 240)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Tło w wybranym kolorze
        graphics.Clear(backgroundColor);
        
        // Tekst z numerem przycisku
        using var font = new Font("Arial", 48, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        var text = $"BTN {buttonNumber}";
        var textSize = graphics.MeasureString(text, font);
        var x = (width - textSize.Width) / 2;
        var y = (height - textSize.Height) / 2;
        graphics.DrawString(text, font, brush, x, y);
        
        // Konwersja do tablicy bajtów (PNG)
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public static byte[] GenerateRandomColor(int width = 100, int height = 100)
    {
        // Generuj LOSOWY kolor za każdym razem
        var random = new Random();
        var color = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // TYLKO KOLOR TŁA - bez tekstu
        graphics.Clear(color);
        
        // Konwersja do JPEG quality 100
        using var ms = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);
        
        return ms.ToArray();
    }
    
    public static byte[] GenerateSimplePattern(int buttonNumber, int width = 100, int height = 100)
    {
        // Prosty obraz - tylko kolor tła, BEZ rotacji
        // 15 przycisków: 3 rzędy × 5 kolumn
        var colors = new[]
        {
            Color.FromArgb(255, 0, 0),     // 1: Czerwony
            Color.FromArgb(0, 255, 0),     // 2: Zielony
            Color.FromArgb(0, 0, 255),     // 3: Niebieski
            Color.FromArgb(255, 255, 0),   // 4: Żółty
            Color.FromArgb(255, 128, 0),   // 5: Pomarańczowy
            Color.FromArgb(128, 0, 255),   // 6: Fioletowy
            Color.FromArgb(0, 255, 255),   // 7: Cyjan
            Color.FromArgb(255, 0, 255),   // 8: Magenta
            Color.FromArgb(255, 192, 203), // 9: Różowy
            Color.FromArgb(165, 42, 42),   // 10: Brązowy
            Color.FromArgb(128, 128, 128), // 11: Szary
            Color.FromArgb(255, 215, 0),   // 12: Złoty
            Color.FromArgb(0, 128, 0),     // 13: Ciemnozielony
            Color.FromArgb(75, 0, 130),    // 14: Indygo
            Color.FromArgb(240, 128, 128)  // 15: Jasnokoralowy
        };
        
        var color = colors[(buttonNumber - 1) % colors.Length];
        
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Wyłącz antyaliasing - prosty rendering
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        
        // TYLKO KOLOR TŁA - bez tekstu, bez niczego
        graphics.Clear(color);
        
        // BEZ rotacji - prosty test
        // bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
        
        // Konwersja do JPEG quality 100 (dokładnie jak w Node.js)
        using var ms = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);
        
        // Zapisz obraz do pliku dla debugowania
        var debugPath = $"c:\\temp\\button_{buttonNumber}.jpg";
        try
        {
            System.IO.Directory.CreateDirectory("c:\\temp");
            bitmap.Save(debugPath, jpegEncoder, encoderParameters);
            Console.WriteLine($"    Zapisano obraz testowy: {debugPath}");
        }
        catch { }
        
        Console.WriteLine($"    Wygenerowano obraz {buttonNumber}: {ms.Length} bajtów (JPEG quality 100, rotacja 180°)");
        return ms.ToArray();
    }
    
    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null!;
    }
    
    /// <summary>
    /// Generuje prosty kształt geometryczny
    /// </summary>
    public static byte[] GenerateSimpleShape(int shapeType, Color backgroundColor, Color shapeColor, int width = 100, int height = 100)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Tło
        graphics.Clear(backgroundColor);
        
        // Włącz antyaliasing
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        using var brush = new SolidBrush(shapeColor);
        using var pen = new Pen(shapeColor, 3);
        
        int margin = 15;
        int size = Math.Min(width, height) - (margin * 2);
        int x = (width - size) / 2;
        int y = (height - size) / 2;
        
        switch (shapeType)
        {
            case 1: // Kółko wypełnione
                graphics.FillEllipse(brush, x, y, size, size);
                break;
                
            case 2: // Kwadrat wypełniony
                graphics.FillRectangle(brush, x, y, size, size);
                break;
                
            case 3: // Trójkąt wypełniony
                var triangle = new Point[]
                {
                    new Point(width / 2, y),
                    new Point(x, y + size),
                    new Point(x + size, y + size)
                };
                graphics.FillPolygon(brush, triangle);
                break;
                
            case 4: // Romb wypełniony
                var diamond = new Point[]
                {
                    new Point(width / 2, y),
                    new Point(x + size, height / 2),
                    new Point(width / 2, y + size),
                    new Point(x, height / 2)
                };
                graphics.FillPolygon(brush, diamond);
                break;
                
            case 5: // Gwiazda
                var star = CreateStarPoints(width / 2, height / 2, size / 2, size / 4, 5);
                graphics.FillPolygon(brush, star);
                break;
                
            case 6: // Plus
                int thickness = size / 4;
                graphics.FillRectangle(brush, width / 2 - thickness / 2, y, thickness, size);
                graphics.FillRectangle(brush, x, height / 2 - thickness / 2, size, thickness);
                break;
                
            case 7: // Krzyżyk (X)
                using (var thickPen = new Pen(shapeColor, 8))
                {
                    graphics.DrawLine(thickPen, x, y, x + size, y + size);
                    graphics.DrawLine(thickPen, x + size, y, x, y + size);
                }
                break;
                
            case 8: // Serce
                var heart = CreateHeartPoints(width / 2, height / 2, size);
                graphics.FillPolygon(brush, heart);
                break;
                
            case 9: // Strzałka w górę
                var arrowUp = new Point[]
                {
                    new Point(width / 2, y),
                    new Point(x + size, y + size / 2),
                    new Point(x + size * 2 / 3, y + size / 2),
                    new Point(x + size * 2 / 3, y + size),
                    new Point(x + size / 3, y + size),
                    new Point(x + size / 3, y + size / 2),
                    new Point(x, y + size / 2)
                };
                graphics.FillPolygon(brush, arrowUp);
                break;
                
            case 10: // Strzałka w prawo
                var arrowRight = new Point[]
                {
                    new Point(x + size, height / 2),
                    new Point(x + size / 2, y),
                    new Point(x + size / 2, y + size / 3),
                    new Point(x, y + size / 3),
                    new Point(x, y + size * 2 / 3),
                    new Point(x + size / 2, y + size * 2 / 3),
                    new Point(x + size / 2, y + size)
                };
                graphics.FillPolygon(brush, arrowRight);
                break;
                
            default: // Kółko z konturem
                graphics.DrawEllipse(pen, x, y, size, size);
                break;
        }
        
        // Konwersja do JPEG quality 100
        using var ms = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);
        
        return ms.ToArray();
    }
    
    private static Point[] CreateStarPoints(int centerX, int centerY, int outerRadius, int innerRadius, int points)
    {
        var starPoints = new Point[points * 2];
        double angle = -Math.PI / 2;
        double angleStep = Math.PI / points;
        
        for (int i = 0; i < points * 2; i++)
        {
            int radius = (i % 2 == 0) ? outerRadius : innerRadius;
            starPoints[i] = new Point(
                centerX + (int)(radius * Math.Cos(angle)),
                centerY + (int)(radius * Math.Sin(angle))
            );
            angle += angleStep;
        }
        
        return starPoints;
    }
    
    private static Point[] CreateHeartPoints(int centerX, int centerY, int size)
    {
        var points = new List<Point>();
        double scale = size / 100.0;
        
        for (double t = 0; t < 2 * Math.PI; t += 0.1)
        {
            double x = 16 * Math.Pow(Math.Sin(t), 3);
            double y = -(13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t));
            
            points.Add(new Point(
                centerX + (int)(x * scale),
                centerY + (int)(y * scale)
            ));
        }
        
        return points.ToArray();
    }
    
    /// <summary>
    /// Ładuje SVG i konwertuje na JPEG dla przycisku
    /// </summary>
    public static byte[] LoadSvgIcon(string svgPath, Color backgroundColor, int width = 100, int height = 100)
    {
        if (!File.Exists(svgPath))
        {
            Console.WriteLine($"✗ Plik SVG nie istnieje: {svgPath}");
            return GenerateRandomColor(width, height);
        }
        
        try
        {
            // Wczytaj SVG
            var svgDocument = SvgDocument.Open(svgPath);
            
            // Renderuj SVG bezpośrednio do bitmapy 100x100
            using var svgBitmap = svgDocument.Draw(width, height);
            
            // Utwórz nową bitmapę z tłem
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Tło
            graphics.Clear(backgroundColor);
            
            // Włącz antyaliasing dla lepszej jakości
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            
            // Narysuj SVG na środku
            graphics.DrawImage(svgBitmap, 0, 0, width, height);
            
            // Konwersja do JPEG quality 100 (urządzenie nie obsługuje PNG!)
            using var ms = new MemoryStream();
            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
            bitmap.Save(ms, jpegEncoder, encoderParameters);
            
            // Zapisz do pliku dla debugowania
            try
            {
                Directory.CreateDirectory("c:\\temp");
                bitmap.Save($"c:\\temp\\icon_{Path.GetFileNameWithoutExtension(svgPath)}.jpg", jpegEncoder, encoderParameters);
            }
            catch { }
            
            Console.WriteLine($"✓ Załadowano ikonę: {Path.GetFileName(svgPath)} ({ms.Length} bajtów)");
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd ładowania SVG {svgPath}: {ex.Message}");
            return GenerateRandomColor(width, height);
        }
    }
    
    /// <summary>
    /// Ładuje wszystkie ikony SVG z katalogu
    /// </summary>
    public static Dictionary<string, byte[]> LoadAllIcons(string iconsDirectory, Color backgroundColor)
    {
        var icons = new Dictionary<string, byte[]>();
        
        if (!Directory.Exists(iconsDirectory))
        {
            Console.WriteLine($"✗ Katalog nie istnieje: {iconsDirectory}");
            return icons;
        }
        
        var svgFiles = Directory.GetFiles(iconsDirectory, "*.svg");
        Console.WriteLine($"\nŁadowanie {svgFiles.Length} ikon SVG...");
        
        foreach (var svgFile in svgFiles)
        {
            var iconName = Path.GetFileNameWithoutExtension(svgFile);
            var imageData = LoadSvgIcon(svgFile, backgroundColor);
            icons[iconName] = imageData;
        }
        
        return icons;
    }
}

