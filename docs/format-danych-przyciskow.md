# Format danych przycisków Mirabox

## Struktura pakietu (64 bajty)

Mirabox wysyła dane o naciśnięciu przycisku w pakietach o stałej długości 64 bajtów.

### Przykład danych:
```
41-43-4B-00-00-4F-4B-00-00-0D-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
```

### Szczegółowa struktura:

| Pozycja | Bajty | Wartość (hex) | Wartość (ASCII/dec) | Znaczenie |
|---------|-------|---------------|---------------------|-----------|
| 0-2     | 3     | 41-43-4B      | "ACK"               | Potwierdzenie odbioru (Acknowledgment) |
| 3-4     | 2     | 00-00         | -                   | Padding/Rezerwa |
| 5-6     | 2     | 4F-4B         | "OK"                | Status OK - operacja zakończona pomyślnie |
| 7-8     | 2     | 00-00         | -                   | Padding/Rezerwa |
| 9       | 1     | 0D            | 13 (decimal)        | **Numer przycisku (1-15)** |
| 10      | 1     | 00            | 0 (released)        | **Stan przycisku (0=released, 1=pressed)** |
| 11-63   | 53    | 00...         | -                   | Padding - reszta pakietu wypełniona zerami |

## Interpretacja

### Bajty 0-2: "ACK"
- **Znaczenie**: Potwierdzenie odbioru poprzedniej komendy
- **Wartość**: Zawsze `41-43-4B` (ASCII "ACK")
- **Cel**: Urządzenie potwierdza, że otrzymało i przetworzyło poprzednią komendę

### Bajty 3-4: Padding
- **Znaczenie**: Rezerwa/padding
- **Wartość**: Zawsze `00-00`
- **Cel**: Wyrównanie struktury danych

### Bajty 5-6: "OK"
- **Znaczenie**: Status operacji
- **Wartość**: Zawsze `4F-4B` (ASCII "OK")
- **Cel**: Informacja, że operacja zakończyła się pomyślnie

### Bajty 7-8: Padding
- **Znaczenie**: Rezerwa/padding
- **Wartość**: Zawsze `00-00`
- **Cel**: Wyrównanie struktury danych

### Bajt 9: Numer przycisku
- **Znaczenie**: Który przycisk został naciśnięty
- **Zakres**: 1-15 (0x01-0x0F)
- **Przykład**: `0D` = 13 (dziesiętnie) = przycisk 13

### Bajt 10: Stan przycisku
- **Znaczenie**: Czy przycisk został naciśnięty czy zwolniony
- **Wartości**:
  - `00` = released (zwolniony)
  - `01` = pressed (naciśnięty)
- **Uwaga**: Niektóre urządzenia mogą wysyłać `0xFF` dla pressed

### Bajty 11-63: Padding
- **Znaczenie**: Reszta pakietu wypełniona zerami
- **Cel**: Stała długość pakietu (64 bajty)

## Przykłady

### Przycisk 1 naciśnięty:
```
41-43-4B-00-00-4F-4B-00-00-01-01-00-00-00-00...
```
- Bajt 9: `01` = przycisk 1
- Bajt 10: `01` = pressed

### Przycisk 15 zwolniony:
```
41-43-4B-00-00-4F-4B-00-00-0F-00-00-00-00-00...
```
- Bajt 9: `0F` = 15 (przycisk 15)
- Bajt 10: `00` = released

## Uwagi

1. **Stały format**: Mirabox zawsze wysyła dane w tym samym formacie - nie można go skonfigurować
2. **Numer przycisku**: Jest stały dla każdego fizycznego przycisku (1-15)
3. **Długość pakietu**: Zawsze 64 bajty, niezależnie od zawartości
4. **Padding**: Wszystkie nieużywane bajty są wypełnione zerami

## Implementacja w kodzie

Parser w `MiraboxLibUsbButtonReader.cs` odczytuje:
- Bajt 9 → numer przycisku
- Bajt 10 → stan przycisku

Bajty 0-8 (ACK, OK, padding) są używane tylko do weryfikacji poprawności pakietu.

