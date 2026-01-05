using System;
using System.Linq;
using HidSharp;

namespace mirabox;

/// <summary>
/// Komunikacja z urządzeniem Mirabox przez HID (bez WinUSB!)
/// Używa bezpośredniego Windows API jako fallback jeśli HidSharp nie działa
/// </summary>
public class MiraboxHidTransfer : IDisposable
{
    private HidDevice? _device;
    private HidStream? _stream;
    private MiraboxButtonReader? _fallbackReader; // Fallback do Windows API
    
    public bool Connect(int vendorId, int productId)
    {
        try
        {
            Console.WriteLine($"\n=== PRÓBA POŁĄCZENIA PRZEZ HID ===");
            Console.WriteLine($"Szukam urządzenia VID: 0x{vendorId:X4}, PID: 0x{productId:X4}");
            
            // Najpierw spróbuj standardową metodą
            var devices = DeviceList.Local.GetHidDevices(vendorId, productId).ToArray();
            
            // Jeśli nie znaleziono, szukaj we wszystkich urządzeniach HID (również z statusem Unknown)
            if (devices.Length == 0)
            {
                Console.WriteLine("Nie znaleziono przez GetHidDevices(), szukam we wszystkich urządzeniach HID...");
                var allHidDevices = DeviceList.Local.GetAllDevices().OfType<HidDevice>();
                devices = allHidDevices
                    .Where(d => d.VendorID == vendorId && d.ProductID == productId)
                    .ToArray();
            }
            
            if (devices.Length == 0)
            {
                Console.WriteLine("✗ Nie znaleziono urządzenia HID przez HidSharp");
                Console.WriteLine("Próba użycia bezpośredniego Windows API...");
                
                // Spróbuj użyć bezpośredniego Windows API (MiraboxButtonReader)
                // Znajdź urządzenie HID w systemie i zbuduj ścieżkę
                // Użyj bezpośrednio InstanceId z PowerShell: HID\VID_5548&PID_6670\6&4006A10&0&0000
                // Spróbuj różne warianty ścieżki HID
                var hidPaths = new[]
                {
                    $@"\\?\HID\VID_5548&PID_6670\6&4006A10&0&0000",
                    $@"\\?\HID#VID_5548&PID_6670#6&4006A10&0&0000#{{4d1e55b2-f16f-11cf-88cb-001111000030}}",
                    $@"\\?\HID#VID_5548&PID_6670#355499441494#{{4d1e55b2-f16f-11cf-88cb-001111000030}}"
                };
                
                _fallbackReader = new MiraboxButtonReader();
                foreach (var hidPath in hidPaths)
                {
                    Console.WriteLine($"Próba ścieżki HID: {hidPath}");
                    if (_fallbackReader.Connect(hidPath))
                    {
                        Console.WriteLine("✓ Połączono przez Windows API!");
                        return true;
                    }
                }
                
                // Jeśli bezpośrednie ścieżki nie działają, spróbuj znaleźć przez DeviceFinder
                var allDevices = MiraboxDeviceFinder.GetAllUsbDevices();
                var hidDevice = allDevices.FirstOrDefault(d => 
                    d.DeviceId != null && 
                    d.DeviceId.Contains("HID") && 
                    d.DeviceId.Contains("VID_5548") && 
                    d.DeviceId.Contains("PID_6670"));
                
                if (hidDevice != null)
                {
                    // Użyj bezpośrednio InstanceId jako ścieżki HID
                    // Format: \\?\HID\VID_xxxx&PID_xxxx\...
                    var hidPath = $@"\\?\{hidDevice.DeviceId}";
                    Console.WriteLine($"Znaleziono urządzenie HID w systemie, próba połączenia przez Windows API...");
                    Console.WriteLine($"Ścieżka HID: {hidPath}");
                    
                    _fallbackReader = new MiraboxButtonReader();
                    if (_fallbackReader.Connect(hidPath))
                    {
                        Console.WriteLine("✓ Połączono przez Windows API!");
                        return true;
                    }
                    
                    // Spróbuj zbudować standardową ścieżkę HID
                    var standardPath = MiraboxHidPathBuilder.BuildHidPath(hidDevice);
                    if (!string.IsNullOrEmpty(standardPath) && standardPath != hidPath)
                    {
                        Console.WriteLine($"Próba alternatywnej ścieżki: {standardPath}");
                        if (_fallbackReader.Connect(standardPath))
                        {
                            Console.WriteLine("✓ Połączono przez Windows API (alternatywna ścieżka)!");
                            return true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Nie znaleziono urządzenia HID w systemie");
                }
                
                Console.WriteLine("✗ Nie można połączyć się przez Windows API");
                return false;
            }
            
            Console.WriteLine($"✓ Znaleziono {devices.Length} urządzeń HID");
            
            // Wybierz pierwsze urządzenie
            _device = devices[0];
            
            Console.WriteLine($"Urządzenie: {_device.GetProductName()}");
            Console.WriteLine($"Producent: {_device.GetManufacturer()}");
            Console.WriteLine($"Max Input Report: {_device.GetMaxInputReportLength()}");
            Console.WriteLine($"Max Output Report: {_device.GetMaxOutputReportLength()}");
            
            // Otwórz stream
            if (!_device.TryOpen(out _stream))
            {
                Console.WriteLine("✗ Nie można otworzyć strumienia HID");
                return false;
            }
            
            Console.WriteLine("✓ Połączono przez HID!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd połączenia HID: {ex.Message}");
            return false;
        }
    }
    
    public bool WriteData(byte[] data, bool removeReportId = true)
    {
        // Jeśli używamy fallback (Windows API)
        if (_fallbackReader != null && _fallbackReader.IsConnected)
        {
            return _fallbackReader.WriteData(data, useFeatureReport: false);
        }
        
        if (_stream == null)
        {
            Console.WriteLine("✗ Brak połączenia HID");
            return false;
        }
        
        try
        {
            // HID wymaga Report ID na początku
            // Jeśli dane już mają Report ID (0x00), użyj ich bezpośrednio
            // Jeśli nie, dodaj Report ID
            
            byte[] dataToSend;
            
            if (removeReportId && data.Length > 0 && data[0] == 0x00)
            {
                // Dane już mają Report ID - użyj bezpośrednio
                dataToSend = data;
            }
            else if (!removeReportId)
            {
                // Dane już mają Report ID
                dataToSend = data;
            }
            else
            {
                // Dodaj Report ID 0x00
                dataToSend = new byte[data.Length + 1];
                dataToSend[0] = 0x00;
                Array.Copy(data, 0, dataToSend, 1, data.Length);
            }
            
            // Wyświetl pierwsze bajty dla debugowania
            var preview = string.Join("-", dataToSend.Take(16).Select(b => $"{b:X2}"));
            Console.WriteLine($"  Wysyłanie {dataToSend.Length} bajtów przez HID (pierwsze bajty: {preview})");
            
            // Wyślij dane
            _stream.Write(dataToSend);
            _stream.Flush();
            
            Console.WriteLine($"  ✓ Wysłano {dataToSend.Length} bajtów przez HID");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Błąd wysyłania HID: {ex.Message}");
            return false;
        }
    }
    
    public byte[]? ReadData(int timeout = 1000)
    {
        if (_stream == null)
        {
            return null;
        }
        
        try
        {
            _stream.ReadTimeout = timeout;
            var buffer = new byte[_device!.GetMaxInputReportLength()];
            var bytesRead = _stream.Read(buffer, 0, buffer.Length);
            
            if (bytesRead > 0)
            {
                var result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
            
            return null;
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd odczytu HID: {ex.Message}");
            return null;
        }
    }
    
    public void Dispose()
    {
        _stream?.Close();
        _stream?.Dispose();
        _fallbackReader?.Dispose();
        Console.WriteLine("Zamknięto połączenie HID");
    }
}
