using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
    /// Generuje portret mężczyzny w okularach (czarno-biały, okrągły)
    /// </summary>
    public static byte[] GeneratePortraitIcon(Color backgroundColor, int width = 100, int height = 100)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        // Użyj przekazanego koloru tła jako tła całego obrazu
        graphics.Clear(backgroundColor);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Głowa/twarz - okrągła
        using var faceBrush = new SolidBrush(Color.FromArgb(220, 200, 180)); // Jasny kolor skóry
        int faceSize = 80;
        int faceX = (width - faceSize) / 2;
        int faceY = (height - faceSize) / 2 - 5; // Trochę wyżej
        graphics.FillEllipse(faceBrush, faceX, faceY, faceSize, faceSize);

        // Włosy - ciemne, na górze głowy
        using var hairBrush = new SolidBrush(Color.FromArgb(40, 30, 20));
        int hairY = faceY - 8;
        graphics.FillEllipse(hairBrush, faceX + 5, hairY, faceSize - 10, 25);

        // Okulary - ciemna ramka, lekko przekrzywione
        using var glassesPen = new Pen(Color.FromArgb(30, 20, 10), 4);
        int glassesY = faceY + 25;
        int glassesWidth = 50;
        int glassesHeight = 20;
        int glassesX = (width - glassesWidth) / 2;
        
        // Lewe szkło (lekko niżej)
        graphics.DrawRectangle(glassesPen, glassesX - 2, glassesY + 1, glassesWidth / 2 - 2, glassesHeight);
        // Prawe szkło (lekko wyżej - przekrzywione)
        graphics.DrawRectangle(glassesPen, glassesX + glassesWidth / 2 + 2, glassesY - 1, glassesWidth / 2 - 2, glassesHeight);
        // Mostek okularów
        graphics.DrawLine(glassesPen, glassesX + glassesWidth / 2 - 2, glassesY + glassesHeight / 2, 
                         glassesX + glassesWidth / 2 + 2, glassesY + glassesHeight / 2 - 2);

        // Oczy - ciemne, patrzące w prawo
        using var eyeBrush = new SolidBrush(Color.FromArgb(50, 40, 30));
        int eyeSize = 8;
        int leftEyeX = glassesX + 8;
        int rightEyeX = glassesX + glassesWidth / 2 + 12;
        int eyeY = glassesY + glassesHeight / 2;
        graphics.FillEllipse(eyeBrush, leftEyeX, eyeY - eyeSize / 2, eyeSize, eyeSize);
        graphics.FillEllipse(eyeBrush, rightEyeX, eyeY - eyeSize / 2 - 2, eyeSize, eyeSize);

        // Brwi - lekko zmarszczone
        using var eyebrowPen = new Pen(Color.FromArgb(30, 20, 10), 3);
        graphics.DrawArc(eyebrowPen, leftEyeX - 5, eyeY - eyeSize - 8, 12, 6, 180, 180);
        graphics.DrawArc(eyebrowPen, rightEyeX - 5, eyeY - eyeSize - 10, 12, 6, 180, 180);

        // Nos - górna część
        using var nosePen = new Pen(Color.FromArgb(200, 180, 160), 2);
        int noseX = width / 2;
        int noseY = glassesY + glassesHeight + 8;
        graphics.DrawLine(nosePen, noseX, noseY, noseX, noseY + 8);

        // Broda - ciemna, po bokach twarzy
        using var beardBrush = new SolidBrush(Color.FromArgb(50, 40, 30));
        int beardY = faceY + faceSize - 20;
        graphics.FillEllipse(beardBrush, faceX + 10, beardY, 20, 15);
        graphics.FillEllipse(beardBrush, faceX + faceSize - 30, beardY, 20, 15);

        // Rotacja 90 stopni w lewo (jak emotikona)
        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

        using var ms = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);

        return ms.ToArray();
    }
    
    /// <summary>
    /// Generuje emotikonę (uśmiechniętą lub smutną mordę)
    /// </summary>
    public static byte[] GenerateEmoticon(bool happy, Color backgroundColor, int width = 100, int height = 100)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Tło
        graphics.Clear(backgroundColor);
        
        // Włącz antyaliasing
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        // Kolor twarzy - bardziej żółty (jasny żółty)
        var faceColor = Color.FromArgb(255, 255, 220); // Bardziej żółty
        using var faceBrush = new SolidBrush(faceColor);
        
        // Rysuj twarz (kółko)
        int faceSize = Math.Min(width, height) - 20;
        int faceX = (width - faceSize) / 2;
        int faceY = (height - faceSize) / 2;
        graphics.FillEllipse(faceBrush, faceX, faceY, faceSize, faceSize);
        
        // Kontur twarzy
        using var facePen = new Pen(Color.Black, 2);
        graphics.DrawEllipse(facePen, faceX, faceY, faceSize, faceSize);
        
        // Oczy
        using var eyeBrush = new SolidBrush(Color.Black);
        int eyeSize = faceSize / 8;
        int eyeY = faceY + faceSize / 3;
        
        // Lewe oko
        int leftEyeX = faceX + faceSize / 3;
        graphics.FillEllipse(eyeBrush, leftEyeX, eyeY, eyeSize, eyeSize);
        
        // Prawe oko
        int rightEyeX = faceX + faceSize * 2 / 3 - eyeSize;
        graphics.FillEllipse(eyeBrush, rightEyeX, eyeY, eyeSize, eyeSize);
        
        // Usta
        using var mouthPen = new Pen(Color.Black, 3);
        int mouthY = faceY + faceSize * 2 / 3;
        int mouthWidth = faceSize / 3;
        int mouthX = faceX + (faceSize - mouthWidth) / 2;
        
        if (happy)
        {
            // Uśmiechnięta morda - łuk w górę
            graphics.DrawArc(mouthPen, mouthX, mouthY - mouthWidth / 2, mouthWidth, mouthWidth, 0, 180);
        }
        else
        {
            // Smutna morda - łuk w dół
            graphics.DrawArc(mouthPen, mouthX, mouthY, mouthWidth, mouthWidth, 180, 180);
        }
        
        // Obróć o 90 stopni w lewo (15 minut na zegarze = 90 stopni)
        // 90 stopni w lewo = 270 stopni w prawo
        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
        
        // Konwersja do JPEG quality 100
        using var ms = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);
        
        return ms.ToArray();
    }
    
    /// <summary>
    /// Generuje złą emotikonę (zmarszczone brwi, zły wyraz)
    /// </summary>
    public static byte[] GenerateAngryEmoticon(Color backgroundColor, int width = 100, int height = 100)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Tło
        graphics.Clear(backgroundColor);
        
        // Włącz antyaliasing
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        // Kolor twarzy - bardziej żółty (jasny żółty)
        var faceColor = Color.FromArgb(255, 255, 220);
        using var faceBrush = new SolidBrush(faceColor);
        
        // Rysuj twarz (kółko)
        int faceSize = Math.Min(width, height) - 20;
        int faceX = (width - faceSize) / 2;
        int faceY = (height - faceSize) / 2;
        graphics.FillEllipse(faceBrush, faceX, faceY, faceSize, faceSize);
        
        // Kontur twarzy
        using var facePen = new Pen(Color.Black, 2);
        graphics.DrawEllipse(facePen, faceX, faceY, faceSize, faceSize);
        
        // Zmarszczone brwi (V-shape - charakterystyczne dla złości)
        using var eyebrowPen = new Pen(Color.Black, 4);
        int eyebrowY = faceY + faceSize / 4;
        int eyebrowCenterX = width / 2;
        int eyebrowWidth = faceSize / 3;
        
        // Lewa brew (ukośna linia w dół)
        graphics.DrawLine(eyebrowPen, 
            eyebrowCenterX - eyebrowWidth / 2, eyebrowY,
            eyebrowCenterX - eyebrowWidth / 4, eyebrowY + 8);
        
        // Prawa brew (ukośna linia w dół)
        graphics.DrawLine(eyebrowPen,
            eyebrowCenterX + eyebrowWidth / 4, eyebrowY + 8,
            eyebrowCenterX + eyebrowWidth / 2, eyebrowY);
        
        // Oczy - wąskie, zmrużone (mniejsze niż normalnie)
        using var eyeBrush = new SolidBrush(Color.Black);
        int eyeSize = faceSize / 10; // Mniejsze oczy
        int eyeY = faceY + faceSize / 3 + 2;
        
        // Lewe oko (lekko w dół przez brwi)
        int leftEyeX = faceX + faceSize / 3;
        graphics.FillEllipse(eyeBrush, leftEyeX, eyeY, eyeSize, eyeSize);
        
        // Prawe oko
        int rightEyeX = faceX + faceSize * 2 / 3 - eyeSize;
        graphics.FillEllipse(eyeBrush, rightEyeX, eyeY, eyeSize, eyeSize);
        
        // Usta - złe (odwrócony łuk w dół, jak odwrócone V)
        using var mouthPen = new Pen(Color.Black, 3);
        int mouthY = faceY + faceSize * 2 / 3;
        int mouthWidth = faceSize / 3;
        int mouthX = faceX + (faceSize - mouthWidth) / 2;
        
        // Rysuj odwrócony łuk (zły wyraz) - linia w dół
        var mouthPoints = new Point[]
        {
            new Point(mouthX, mouthY),
            new Point(mouthX + mouthWidth / 2, mouthY + mouthWidth / 3),
            new Point(mouthX + mouthWidth, mouthY)
        };
        graphics.DrawLines(mouthPen, mouthPoints);
        
        // Obróć o 90 stopni w lewo
        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
        
        // Konwersja do JPEG quality 100
        using var ms = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bitmap.Save(ms, jpegEncoder, encoderParameters);
        
        return ms.ToArray();
    }
    
    /// <summary>
    /// Ładuje dowolny obraz (JPG, PNG, BMP, GIF) i konwertuje na ikonę dla przycisku
    /// </summary>
    public static byte[] LoadImageIcon(string imagePath, Color? backgroundColor = null, int width = 100, int height = 100, bool cropToCircle = false)
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"✗ Plik obrazu nie istnieje: {imagePath}");
            return GenerateRandomColor(width, height);
        }
        
        try
        {
            // Wczytaj istniejący obraz (obsługuje JPG, PNG, BMP, GIF itp.)
            using var originalImage = Image.FromFile(imagePath);
            
            // Utwórz nową bitmapę z docelowym rozmiarem
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Włącz antyaliasing dla lepszej jakości
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            
            // Jeśli podano tło, wypełnij nim bitmapę
            if (backgroundColor.HasValue)
            {
                graphics.Clear(backgroundColor.Value);
            }
            else
            {
                // Jeśli nie podano tła, użyj białego
                graphics.Clear(Color.White);
            }
            
            if (cropToCircle)
            {
                // Wytnij obraz w kształt koła
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(0, 0, width, height);
                graphics.SetClip(path);
            }
            
            // Narysuj oryginalny obraz na środku (ze skalowaniem, zachowując proporcje)
            var sourceAspect = (float)originalImage.Width / originalImage.Height;
            var destAspect = (float)width / height;
            
            int drawWidth, drawHeight, drawX, drawY;
            
            if (sourceAspect > destAspect)
            {
                // Obraz jest szerszy - dopasuj do szerokości
                drawWidth = width;
                drawHeight = (int)(width / sourceAspect);
                drawX = 0;
                drawY = (height - drawHeight) / 2;
            }
            else
            {
                // Obraz jest wyższy - dopasuj do wysokości
                drawHeight = height;
                drawWidth = (int)(height * sourceAspect);
                drawX = (width - drawWidth) / 2;
                drawY = 0;
            }
            
            graphics.DrawImage(originalImage, drawX, drawY, drawWidth, drawHeight);
            
            // Jeśli był okrągły crop, usuń clipping
            if (cropToCircle)
            {
                graphics.ResetClip();
            }
            
            // Obróć o 90 stopni w lewo (minus 15 minut na zegarze)
            // 90 stopni w lewo = 270 stopni w prawo
            bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
            
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
                var debugFileName = $"icon_{Path.GetFileNameWithoutExtension(imagePath)}.jpg";
                bitmap.Save(Path.Combine("c:\\temp", debugFileName), jpegEncoder, encoderParameters);
                Console.WriteLine($"  Zapisano debug: c:\\temp\\{debugFileName}");
            }
            catch { }
            
            Console.WriteLine($"✓ Załadowano ikonę: {Path.GetFileName(imagePath)} ({ms.Length} bajtów, {width}x{height}px)");
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd ładowania obrazu {imagePath}: {ex.Message}");
            return GenerateRandomColor(width, height);
        }
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
    /// Ładuje obraz JPG/JPEG i konwertuje na format dla przycisku
    /// </summary>
    public static byte[] LoadJpgIcon(string jpgPath, Color? backgroundColor = null, int width = 100, int height = 100)
    {
        if (!File.Exists(jpgPath))
        {
            Console.WriteLine($"✗ Plik JPG nie istnieje: {jpgPath}");
            return GenerateRandomColor(width, height);
        }
        
        try
        {
            // Wczytaj istniejący obraz JPG
            using var originalImage = Image.FromFile(jpgPath);
            
            // Utwórz nową bitmapę z docelowym rozmiarem
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Włącz antyaliasing dla lepszej jakości
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            
            // Jeśli podano tło, wypełnij nim bitmapę
            if (backgroundColor.HasValue)
            {
                graphics.Clear(backgroundColor.Value);
            }
            
            // Narysuj oryginalny obraz na środku (ze skalowaniem)
            graphics.DrawImage(originalImage, 0, 0, width, height);
            
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
                bitmap.Save($"c:\\temp\\icon_{Path.GetFileNameWithoutExtension(jpgPath)}.jpg", jpegEncoder, encoderParameters);
            }
            catch { }
            
            Console.WriteLine($"✓ Załadowano ikonę JPG: {Path.GetFileName(jpgPath)} ({ms.Length} bajtów)");
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd ładowania JPG {jpgPath}: {ex.Message}");
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
    
    /// <summary>
    /// Ładuje wszystkie ikony JPG z katalogu
    /// </summary>
    public static Dictionary<string, byte[]> LoadAllJpgIcons(string iconsDirectory, Color? backgroundColor = null)
    {
        var icons = new Dictionary<string, byte[]>();
        
        if (!Directory.Exists(iconsDirectory))
        {
            Console.WriteLine($"✗ Katalog nie istnieje: {iconsDirectory}");
            return icons;
        }
        
        var jpgFiles = Directory.GetFiles(iconsDirectory, "*.jpg");
        var jpegFiles = Directory.GetFiles(iconsDirectory, "*.jpeg");
        var allJpgFiles = jpgFiles.Concat(jpegFiles).ToArray();
        
        Console.WriteLine($"\nŁadowanie {allJpgFiles.Length} ikon JPG...");
        
        foreach (var jpgFile in allJpgFiles)
        {
            var iconName = Path.GetFileNameWithoutExtension(jpgFile);
            var imageData = LoadJpgIcon(jpgFile, backgroundColor);
            icons[iconName] = imageData;
        }
        
        return icons;
    }
}

