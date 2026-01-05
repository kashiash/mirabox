# Instrukcja usunięcia sterownika libusb-win32 dla Mirabox

## Problem
Sterownik **libusb-win32** blokuje dostęp do urządzenia Mirabox przez HID. Urządzenie HID jest widoczne w systemie, ale ma status "Unknown" i nie można się z nim połączyć.

## Rozwiązanie: Usunięcie sterownika libusb-win32

### Metoda 1: Przez Menedżer urządzeń (Zalecane)

1. **Otwórz Menedżer urządzeń:**
   - Naciśnij `Win + X` i wybierz "Device Manager" (Menedżer urządzeń)
   - Lub: `Win + R`, wpisz `devmgmt.msc` i naciśnij Enter

2. **Znajdź urządzenie Mirabox:**
   - Rozwiń kategorię **"libusb-win32 devices"**
   - Znajdź urządzenie **"Urządzenie wejściowe USB"** (VID: 5548, PID: 6670)

3. **Odinstaluj sterownik:**
   - Kliknij prawym przyciskiem na urządzenie
   - Wybierz **"Uninstall device"** (Odinstaluj urządzenie)
   - **WAŻNE:** Zaznacz opcję **"Attempt to remove the driver for this device"** (Spróbuj usunąć sterownik dla tego urządzenia)
   - Kliknij **"Uninstall"**

4. **Odłącz i podłącz urządzenie:**
   - Odłącz urządzenie Mirabox z portu USB
   - Poczekaj 5 sekund
   - Podłącz urządzenie ponownie

5. **Sprawdź instalację:**
   - System Windows powinien automatycznie zainstalować domyślny sterownik HID
   - Sprawdź w Menedżerze urządzeń, czy urządzenie pojawiło się w kategorii **"Human Interface Devices"** (Urządzenia interfejsu użytkownika)
   - Status powinien być **"OK"** zamiast "Unknown"

### Metoda 2: Przez PowerShell (Dla zaawansowanych)

**UWAGA:** Wymaga uruchomienia PowerShell jako Administrator!

```powershell
# 1. Znajdź urządzenie z libusb-win32
$device = Get-PnpDevice | Where-Object { 
    $_.InstanceId -like "*VID_5548*PID_6670*" -and 
    $_.Class -eq "libusb-win32 devices" 
}

# 2. Sprawdź szczegóły
Write-Host "Znalezione urządzenie:"
Write-Host "  Nazwa: $($device.FriendlyName)"
Write-Host "  InstanceId: $($device.InstanceId)"
Write-Host "  Status: $($device.Status)"

# 3. Odinstaluj urządzenie (usuwa również sterownik)
Remove-PnpDevice -InstanceId $device.InstanceId -Confirm:$false

# 4. Odłącz i podłącz urządzenie fizycznie
Write-Host "`nOdłącz urządzenie Mirabox z portu USB, poczekaj 5 sekund i podłącz ponownie."
```

### Metoda 3: Przez Zadig (Zmiana sterownika)

Jeśli chcesz zachować możliwość użycia LibUSB, możesz przełączyć sterownik na WinUSB:

1. **Pobierz Zadig:** https://zadig.akeo.ie/
2. **Uruchom Zadig jako Administrator**
3. **Options → List All Devices**
4. **Znajdź urządzenie Mirabox** (VID: 5548, PID: 6670)
5. **Wybierz sterownik:** 
   - Dla HID: wybierz **"HIDUSB"** lub **"WinUSB"** (jeśli dostępne)
   - Dla LibUSB: wybierz **"WinUSB"**
6. **Kliknij "Replace Driver"** lub **"Install Driver"**

**UWAGA:** WinUSB i HID się wykluczają - nie możesz używać obu jednocześnie!

## Weryfikacja po usunięciu

Po usunięciu libusb-win32, uruchom test:

```powershell
# Sprawdź status urządzenia HID
Get-PnpDevice | Where-Object { 
    $_.InstanceId -like "*VID_5548*PID_6670*" 
} | Format-List FriendlyName, InstanceId, Status, Class
```

Urządzenie HID powinno mieć:
- **Status:** "OK" (zamiast "Unknown")
- **Class:** "HIDClass"
- **Problem:** brak (zamiast "CM_PROB_PHANTOM")

## Test połączenia HID

Po usunięciu libusb-win32, uruchom test:

```bash
dotnet test --filter "FullyQualifiedName~TestHidConnection" --logger "console;verbosity=detailed"
```

Test powinien pokazać:
```
✓ Znaleziono urządzenie HID
✓ Połączono przez HID!
```

## Rozwiązywanie problemów

### Problem: Urządzenie nie pojawia się po podłączeniu
- Sprawdź, czy urządzenie jest podłączone
- Sprawdź w Menedżerze urządzeń, czy nie ma żółtego trójkąta z wykrzyknikiem
- Spróbuj innego portu USB

### Problem: System nie instaluje sterownika HID automatycznie
- Otwórz Menedżer urządzeń
- Kliknij prawym na urządzenie → "Update driver" (Zaktualizuj sterownik)
- Wybierz "Browse my computer" (Przeglądaj mój komputer)
- Wybierz "Let me pick from a list" (Pozwól mi wybrać z listy)
- Wybierz "Human Interface Devices" → "HID-compliant device"

### Problem: Nadal nie można się połączyć przez HID
- Sprawdź, czy inne aplikacje nie używają urządzenia
- Uruchom aplikację jako Administrator
- Sprawdź uprawnienia użytkownika do urządzeń HID

## Powrót do libusb-win32 (jeśli potrzebne)

Jeśli chcesz wrócić do libusb-win32:

1. Pobierz libusb-win32: http://libusb-win32.sourceforge.net/
2. Uruchom `inf-wizard.exe`
3. Wybierz urządzenie Mirabox
4. Wygeneruj i zainstaluj sterownik

**UWAGA:** Po instalacji libusb-win32, HID przestanie działać!

