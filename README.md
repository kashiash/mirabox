# Mirabox - Biblioteka C# do programowania urządzenia Mirabox/StreamDock

Biblioteka C# do komunikacji z urządzeniem Mirabox (StreamDock) przez LibUSB. Umożliwia programowanie 15 przycisków z obrazami JPEG oraz odczyt naciśnięć przycisków.

## 🎯 Funkcjonalność

- ✅ Komunikacja przez LibUSB (WinUSB driver)
- ✅ Programowanie 15 przycisków (3 rzędy × 5 kolumn)
- ✅ Obrazy JPEG 100×100 pikseli
- ✅ Odczyt naciśnięć przycisków
- ✅ Generowanie prostych ikon geometrycznych
- ✅ Ładowanie ikon z plików JPG
- ✅ Animacje i dynamiczne zmiany

## 📋 Wymagania

### Hardware
- Urządzenie Mirabox/StreamDock (VID: 0x5548, PID: 0x6670)
- Sterownik WinUSB zainstalowany przez Zadig

### Software
- .NET 10.0 (Windows)
- LibUsbDotNet 3.0.102-alpha
- System.Drawing.Common 9.0.0
- Svg 3.4.7 (do konwersji SVG)

## 🚀 Instalacja sterownika

1. Pobierz **Zadig** z https://zadig.akeo.ie/
2. Uruchom Zadig jako Administrator
3. Wybierz **Options → List All Devices**
4. Znajdź urządzenie Mirabox
5. Wybierz sterownik **WinUSB**
6. Kliknij **Replace Driver** lub **Install Driver**

## 💻 Użycie

### Podstawowe programowanie przycisków

```csharp
using var libUsbTransfer = new MiraboxLibUsbTransfer();

// Połącz z urządzeniem
if (!libUsbTransfer.Connect(0x5548, 0x6670))
{
    Console.WriteLine("Nie można połączyć się z urządzeniem");
    return;
}

var reader = new MiraboxLibUsbButtonReader(libUsbTransfer);
var programmer = new MiraboxButtonProgrammer(reader);

// Wygeneruj obraz
var imageData = MiraboxImageGenerator.GenerateSimpleShape(
    shapeType: 1,  // Kółko
    backgroundColor: Color.Black,
    shapeColor: Color.White
);

// Zaprogramuj przycisk
programmer.ProgramButton(buttonNumber: 1, imageData, packetSize: 512);
```

### Ładowanie ikon z plików

```csharp
var imagesDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
var imageFiles = Directory.GetFiles(imagesDirectory, "*.jpg");

foreach (var imageFile in imageFiles.Take(15))
{
    var imageData = File.ReadAllBytes(imageFile);
    programmer.ProgramButton(buttonNumber, imageData, 512);
    buttonNumber++;
}
```

### Odczyt naciśnięć przycisków

```csharp
while (true)
{
    var buttonPress = reader.ReadButtonPress();
    if (buttonPress != null)
    {
        Console.WriteLine($"Przycisk {buttonPress.ButtonNumber}: {buttonPress.State}");
    }
}
```

## 🎨 Generowanie ikon

### Dostępne kształty geometryczne

Biblioteka zawiera generator prostych kształtów:

1. **Kółko** - wypełnione
2. **Kwadrat** - wypełniony
3. **Trójkąt** - wypełniony
4. **Romb** - wypełniony
5. **Gwiazda** - 5-ramienna
6. **Plus** - znak +
7. **Krzyżyk** - znak ×
8. **Serce** - kształt serca
9. **Strzałka w górę** - ↑
10. **Strzałka w prawo** - →

```csharp
// Generuj kształt
var imageData = MiraboxImageGenerator.GenerateSimpleShape(
    shapeType: 5,  // Gwiazda
    backgroundColor: Color.FromArgb(30, 30, 30),
    shapeColor: Color.Yellow
);
```

### Generowanie ikon do plików

```csharp
// Wygeneruj wszystkie kształty jako pliki JPG
var imagesDirectory = @"c:\Users\Programista\source\repos\mirabox\Images";
Directory.CreateDirectory(imagesDirectory);

for (int i = 1; i <= 10; i++)
{
    var imageData = MiraboxImageGenerator.GenerateSimpleShape(
        i, 
        Color.Black, 
        Color.White
    );
    
    File.WriteAllBytes(
        Path.Combine(imagesDirectory, $"shape_{i:D2}.jpg"), 
        imageData
    );
}
```

## 🧪 Testy

Projekt zawiera testy xUnit demonstrujące różne funkcjonalności:

### `ProgramButtonsFromImageFiles`
Ładuje ikony z katalogu `Images` i programuje przyciski.

```bash
dotnet test --filter "FullyQualifiedName~ProgramButtonsFromImageFiles"
```

### `ProgramButtonsWithSimpleShapes`
Generuje proste kształty geometryczne i programuje wszystkie 15 przycisków.

```bash
dotnet test --filter "FullyQualifiedName~ProgramButtonsWithSimpleShapes"
```

### `AnimateRandomIconsForOneMinute`
Losowa animacja - zmienia ikony na losowych przyciskach przez 1 minutę.

```bash
dotnet test --filter "FullyQualifiedName~AnimateRandomIconsForOneMinute"
```

### `GenerateAllShapesToFiles`
Generuje 35 ikon (10 kształtów × różne kolory) do katalogu `Images`.

```bash
dotnet test --filter "FullyQualifiedName~GenerateAllShapesToFiles"
```

## 📁 Struktura projektu

```
mirabox/
├── MiraboxLibUsbTransfer.cs          # Komunikacja LibUSB
├── MiraboxLibUsbButtonReader.cs      # Odczyt przycisków
├── MiraboxButtonProgrammer.cs        # Programowanie przycisków
├── MiraboxImageGenerator.cs          # Generator obrazów
├── MiraboxTests.cs                   # Testy podstawowe
├── MiraboxShapesTest.cs              # Test kształtów
├── MiraboxLoadImagesTest.cs          # Test ładowania z plików
├── MiraboxRandomIconsAnimationTest.cs # Test animacji
├── MiraboxGenerateShapesTest.cs      # Generator ikon do plików
└── Images/                           # Katalog z ikonami JPG
    ├── shape_01_circle.jpg
    ├── shape_02_square.jpg
    └── ...
```

## 🔧 Protokół komunikacji Mirabox

Urządzenie używa protokołu CRT (Command Response Transfer):

### Komendy

- **DIS** - Wake screen (budzenie ekranu)
- **BAT** - Button image (programowanie przycisku)
- **STP** - Refresh (odświeżenie ekranu)
- **CLE** - Clear (czyszczenie ekranu)
- **LIG** - Brightness (jasność)

### Format pakietów

Każdy pakiet ma **512 bajtów**:

**Komenda BAT:**
```
[CRT prefix: 0x43,0x52,0x54,0x00,0x00]
[BAT: 0x42,0x41,0x54]
[Size: 4 bajty, big-endian]
[Button number: 1 bajt]
[Padding: zera do 512 bajtów]
```

**Dane obrazu:**
```
[Chunki JPEG po 512 bajtów]
[Ostatni chunk dopełniony zerami do 512 bajtów]
```

**Komenda STP:**
```
[CRT prefix: 0x43,0x52,0x54,0x00,0x00]
[STP: 0x53,0x54,0x50]
[Padding: zera do 512 bajtów]
```

## 📝 Format obrazów

- **Rozmiar:** 100×100 pikseli
- **Format:** JPEG
- **Jakość:** 100 (maksymalna)
- **Rotacja:** 180° (urządzenie wymaga odwróconego obrazu)

## ⚠️ Ważne uwagi

1. **Rozmiar pakietu:** Zawsze 512 bajtów (nie 511!)
2. **Report ID:** LibUSB wymaga dodania bajtu 0x00 na początku każdego pakietu
3. **Dane obrazu:** Wysyłane BEZ prefiksu CRT (tylko czyste chunki JPEG)
4. **Sterownik:** Musi być WinUSB (nie HID!)

## 🐛 Rozwiązywanie problemów

### Urządzenie nie zostaje znalezione
- Sprawdź czy sterownik WinUSB jest zainstalowany (Zadig)
- Sprawdź VID:PID (0x5548:0x6670)
- Uruchom aplikację jako Administrator

### Obrazy są zniekształcone
- Upewnij się że obrazy są JPEG 100×100
- Sprawdź czy dane są wysyłane w chunkach po 512 bajtów
- Sprawdź rotację obrazu (180°)

### Błąd "ERROR_INVALID_PARAMETER"
- Zmień sterownik na WinUSB przez Zadig
- Nie używaj sterownika HID

## 📚 Referencje

- [Node.js implementation](https://github.com/rigor789/mirabox-streamdock-node) - Oryginalna implementacja w Node.js
- [LibUsbDotNet](https://github.com/LibUsbDotNet/LibUsbDotNet) - Biblioteka LibUSB dla .NET
- [Zadig](https://zadig.akeo.ie/) - Narzędzie do instalacji sterowników USB

## 📄 Licencja

MIT License

## 👤 Autor

Projekt stworzony do obsługi urządzenia Mirabox/StreamDock w C#/.NET.