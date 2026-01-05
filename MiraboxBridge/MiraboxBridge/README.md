# MiraboxBridge - WebSocket Bridge Service

WebSocket bridge service łączący aplikację XAF Blazor z urządzeniem MiraBox.

## Funkcjonalność

- ✅ Połączenie z urządzeniem MiraBox (LibUSB lub HID)
- ✅ WebSocket server na porcie 8081
- ✅ Odbieranie komunikatów JSON z aplikacji Blazor
- ✅ Programowanie przycisków MiraBox na podstawie odebranych ikon
- ✅ Nasłuchiwanie naciśnięć przycisków i wysyłanie do Blazor

## Uruchomienie

```bash
cd MiraboxBridge
dotnet run
```

Serwis uruchomi się na:
- **WebSocket**: `ws://localhost:8081/mirabox`
- **Status**: `http://localhost:8081/status`

## Format komunikatów

### Blazor → Bridge: Ustawienie akcji

```json
{
  "type": "setActions",
  "viewId": "Pacjenci_ListView",
  "viewType": "ListView",
  "actions": [
    {
      "id": "Save",
      "caption": "Zapisz",
      "icon": "save.svg",
      "buttonNumber": 1
    }
  ]
}
```

### Bridge → Blazor: Naciśnięcie przycisku

```json
{
  "type": "buttonPress",
  "buttonNumber": 1,
  "state": "pressed",
  "actionId": "Save",
  "viewId": "Pacjenci_ListView"
}
```

## Katalog ikon

Ikony powinny być umieszczone w katalogu `Images/`:
- Format: SVG lub JPG
- Rozmiar: automatycznie konwertowane do 100×100px JPEG
- Jeśli ikona nie istnieje, używana jest domyślna ikona (kółko)

## Wymagania

- Windows (dla komunikacji USB/HID)
- .NET 10.0
- Urządzenie MiraBox (VID: 0x5548, PID: 0x6670)

