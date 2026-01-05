# Analiza kodu komunikacji z Mirabox

## Podsumowanie

Twój kod jest **poprawny** i umożliwia komunikację z urządzeniem Mirabox **bez instalowania dodatkowych sterowników**. Kod używa standardowego sterownika HID systemu Windows.

## Architektura rozwiązania

### 1. Komunikacja przez HID (bez dodatkowych sterowników)

Kod używa **HID (Human Interface Device)** - standardowego protokołu Windows, który nie wymaga instalacji dodatkowych sterowników:

- **`MiraboxHidTransfer`** - główna klasa komunikacji przez HID
- Używa biblioteki **HidSharp** do komunikacji
- Ma fallback do bezpośredniego Windows API (`MiraboxButtonReader`) jeśli HidSharp nie działa

### 2. Programowanie przycisków

- **`MiraboxButtonProgrammer`** - programuje przyciski z obrazami JPEG
- Wysyła komendy w protokole CRT (Command Response Transfer)
- Obsługuje obrazy 100×100 pikseli w formacie JPEG

### 3. Odczytywanie przycisków

- **`MiraboxHidButtonReader`** - odczytuje naciśnięcia przycisków
- Zwraca numer przycisku i stan (pressed/released)

## Weryfikacja poprawności kodu

### ✅ Co działa poprawnie:

1. **Połączenie HID** - kod próbuje połączyć się przez HID, a jeśli nie działa, używa fallback do Windows API
2. **Format pakietów** - pakiety mają 512 bajtów + Report ID (0x00) na początku
3. **Protokół CRT** - komendy są wysyłane w poprawnym formacie:
   - DIS (wake screen)
   - BAT (button image)
   - STP (refresh)
4. **Dane obrazu** - wysyłane w chunkach po 512 bajtów bez prefiksu CRT
5. **Report ID** - poprawnie dodawany dla komunikacji HID

### ⚠️ Potencjalne problemy:

1. **Rozmiar pakietu w SendImageData**:
   - Tworzy pakiet 512 bajtów, potem dodaje Report ID (0x00) na początku
   - W rezultacie pakiet ma 513 bajtów
   - To może działać, ale niektóre urządzenia HID wymagają dokładnie 512 bajtów (z Report ID wliczonym)

2. **Fallback do Windows API**:
   - Jeśli HidSharp nie działa, kod próbuje użyć bezpośredniego Windows API
   - To wymaga znajomości ścieżki HID urządzenia, która może się różnić

## Jak używać kodu

### Podstawowe użycie (bez dodatkowych sterowników):

```csharp
using var hidTransfer = new MiraboxHidTransfer();

// Połącz z urządzeniem
if (!hidTransfer.Connect(0x5548, 0x6670))
{
    Console.WriteLine("Nie można połączyć się z urządzeniem");
    return;
}

var reader = new MiraboxHidButtonReader(hidTransfer);
var programmer = new MiraboxButtonProgrammer(reader);

// Wygeneruj obraz
var imageData = MiraboxImageGenerator.GenerateSimpleShape(
    shapeType: 1,  // Kółko
    backgroundColor: Color.Black,
    shapeColor: Color.White
);

// Zaprogramuj przycisk
programmer.ProgramButton(buttonNumber: 1, imageData, packetSize: 512);

// Odczytywanie naciśnięć
while (true)
{
    var buttonPress = reader.ReadButtonPress();
    if (buttonPress != null)
    {
        Console.WriteLine($"Przycisk {buttonPress.ButtonNumber}: {buttonPress.State}");
    }
    Thread.Sleep(10);
}
```

### Sprawdzenie czy urządzenie jest widoczne jako HID:

```bash
dotnet run --project . CheckHidDevices
```

## Wymagania

### ✅ Nie wymaga:
- WinUSB (sterownik instalowany przez Zadig)
- libusb-win32
- Jakichkolwiek dodatkowych sterowników

### ✅ Wymaga:
- Windows (HID jest standardowym protokołem Windows)
- .NET 10.0
- Biblioteka HidSharp (już w projekcie)

## Rozwiązywanie problemów

### Problem: Nie można połączyć się przez HID

**Możliwe przyczyny:**

1. **Urządzenie używa sterownika WinUSB** (zainstalowanego przez Zadig)
   - Rozwiązanie: Odinstaluj sterownik WinUSB przez Zadig lub Menedżer urządzeń
   - System Windows automatycznie zainstaluje sterownik HID

2. **Urządzenie nie jest widoczne jako HID**
   - Sprawdź w Menedżerze urządzeń, czy urządzenie jest w kategorii "Human Interface Devices"
   - Jeśli nie, spróbuj zaktualizować sterownik

3. **Inna aplikacja używa urządzenia**
   - Zamknij inne aplikacje, które mogą używać urządzenia

### Problem: Obrazy nie wyświetlają się poprawnie

**Możliwe przyczyny:**

1. **Nieprawidłowy format obrazu**
   - Obrazy muszą być JPEG 100×100 pikseli
   - Jakość JPEG: 100 (maksymalna)

2. **Nieprawidłowy rozmiar pakietu**
   - Sprawdź, czy używasz `packetSize: 512`

3. **Brak komendy STP (refresh)**
   - Kod automatycznie wysyła STP po programowaniu przycisku

## Zalecenia

1. **Użyj prostego przykładu** (`MiraboxSimpleExample.cs`) jako punktu startowego
2. **Sprawdź urządzenia HID** przed użyciem (`CheckHidDevices.cs`)
3. **Używaj HID zamiast WinUSB** - nie wymaga dodatkowych sterowników
4. **Testuj na małej liczbie przycisków** przed programowaniem wszystkich 15

## Podsumowanie

Twój kod jest **poprawny** i **gotowy do użycia**. Umożliwia:
- ✅ Wyświetlanie ikon na przyciskach
- ✅ Odczytywanie naciśnięć przycisków
- ✅ Działanie bez dodatkowych sterowników (używa HID)

Jedynym wymaganiem jest, aby urządzenie było widoczne jako HID w systemie Windows (co jest standardem dla większości urządzeń USB HID).


