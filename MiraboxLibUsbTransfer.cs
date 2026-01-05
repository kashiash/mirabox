using System;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace mirabox;

/// <summary>
/// Komunikacja z urządzeniem Mirabox przez LibUSB (interrupt transfer)
/// Zgodnie z protokołem z rigor789/mirabox-streamdock-node
/// </summary>
public class MiraboxLibUsbTransfer : IDisposable
{
    private UsbDevice? _usbDevice;
    private UsbEndpointWriter? _writer;
    private UsbEndpointReader? _reader;
    private bool _disposed = false;

    // VID i PID dla Mirabox (wartości hex)
    private const int VID = 0x5548;  // 21832 w decimal
    private const int PID = 0x6670;  // 26224 w decimal

    public bool IsConnected => _usbDevice != null && _usbDevice.IsOpen;

    /// <summary>
    /// Łączy się z urządzeniem Mirabox przez LibUSB
    /// </summary>
    public bool Connect(int vid = VID, int pid = PID)
    {
        try
        {
            Console.WriteLine($"Szukanie urządzenia USB VID: 0x{vid:X4}, PID: 0x{pid:X4}...");
            
            // Wylistuj wszystkie urządzenia USB
            Console.WriteLine("Dostępne urządzenia USB:");
            foreach (UsbRegistry usbRegistry in UsbDevice.AllDevices)
            {
                Console.WriteLine($"  - VID: 0x{usbRegistry.Vid:X4}, PID: 0x{usbRegistry.Pid:X4}, Name: {usbRegistry.Name}");
            }

            // Znajdź urządzenie
            var usbDeviceFinder = new UsbDeviceFinder(vid, pid);
            _usbDevice = UsbDevice.OpenUsbDevice(usbDeviceFinder);

            if (_usbDevice == null)
            {
                Console.WriteLine($"✗ Nie znaleziono urządzenia Mirabox (VID: 0x{vid:X4}, PID: 0x{pid:X4})");
                Console.WriteLine("UWAGA: LibUSB wymaga sterownika WinUSB lub libusb-win32.");
                Console.WriteLine("Użyj narzędzia Zadig (https://zadig.akeo.ie/) aby zainstalować sterownik WinUSB dla urządzenia.");
                return false;
            }

            Console.WriteLine($"✓ Znaleziono urządzenie: {_usbDevice.Info.ProductString}");
            Console.WriteLine($"   Manufacturer: {_usbDevice.Info.ManufacturerString}");
            Console.WriteLine($"   Serial: {_usbDevice.Info.SerialString}");
            
            // Wyświetl informacje o konfiguracji
            Console.WriteLine($"\nInformacje o urządzeniu:");
            Console.WriteLine($"  Liczba konfiguracji: {_usbDevice.Configs.Count}");
            
            foreach (var config in _usbDevice.Configs)
            {
                Console.WriteLine($"  Konfiguracja {config.Descriptor.ConfigID}:");
                Console.WriteLine($"    Liczba interfejsów: {config.InterfaceInfoList.Count}");
                
                foreach (var iface in config.InterfaceInfoList)
                {
                    Console.WriteLine($"    Interface {iface.Descriptor.InterfaceID}:");
                    Console.WriteLine($"      Liczba endpointów: {iface.EndpointInfoList.Count}");
                    
                    foreach (var ep in iface.EndpointInfoList)
                    {
                        Console.WriteLine($"      Endpoint 0x{ep.Descriptor.EndpointID:X2}: {ep.Descriptor.EndpointID} (Max packet: {ep.Descriptor.MaxPacketSize})");
                    }
                }
            }

            // Dla urządzeń "whole device" (nie composite), ustaw konfigurację
            if (_usbDevice is IUsbDevice wholeUsbDevice)
            {
                // Wybierz konfigurację 1
                wholeUsbDevice.SetConfiguration(1);
                
                // Claim interface 0
                wholeUsbDevice.ClaimInterface(0);
                
                Console.WriteLine("\n✓ Skonfigurowano urządzenie (config 1, interface 0)");
            }

            // Znajdź endpointy - użyj informacji z diagnostyki
            UsbEndpointWriter? outEndpoint = null;
            UsbEndpointReader? inEndpoint = null;
            
            // Znajdź endpointy z listy dostępnych
            foreach (var config in _usbDevice.Configs)
            {
                foreach (var iface in config.InterfaceInfoList)
                {
                    foreach (var ep in iface.EndpointInfoList)
                    {
                        var epId = ep.Descriptor.EndpointID;
                        var isIn = (epId & 0x80) != 0; // Bit 7 = 1 oznacza IN
                        var epNum = epId & 0x0F; // Dolne 4 bity to numer endpointu
                        
                        Console.WriteLine($"  Sprawdzam endpoint 0x{epId:X2} (IN={isIn}, num={epNum})...");
                        
                        if (isIn && epId == 0x82)
                        {
                            // Endpoint 0x82 (IN) - to jest endpoint do odczytu
                            try
                            {
                                // W LibUsbDotNet, endpoint 0x82 = ReadEndpointID.Ep02
                                // (bo 0x82 = 130, a Ep02 = 2, ale bit 7 jest ustawiony dla IN)
                                inEndpoint = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
                                Console.WriteLine($"    ✓ Otwarto IN endpoint 0x{epId:X2} (ReadEndpointID.Ep02)");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ✗ Błąd otwierania IN endpoint 0x{epId:X2}: {ex.Message}");
                            }
                        }
                        else if (!isIn && epId == 0x01)
                        {
                            // Endpoint 0x01 (OUT) - to jest endpoint do zapisu
                            try
                            {
                                outEndpoint = _usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
                                Console.WriteLine($"    ✓ Otwarto OUT endpoint 0x{epId:X2} (WriteEndpointID.Ep01)");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ✗ Błąd otwierania OUT endpoint 0x{epId:X2}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            
            // Jeśli nie znaleziono przez pętlę, spróbuj standardowe metody
            if (outEndpoint == null)
            {
                try
                {
                    outEndpoint = _usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
                    Console.WriteLine("✓ Otwarto OUT endpoint przez WriteEndpointID.Ep01");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Nie można otworzyć OUT endpoint: {ex.Message}");
                }
            }
            
            if (inEndpoint == null)
            {
                try
                {
                    inEndpoint = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
                    Console.WriteLine("✓ Otwarto IN endpoint przez ReadEndpointID.Ep02");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Nie można otworzyć IN endpoint przez Ep02: {ex.Message}");
                    try
                    {
                        inEndpoint = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                        Console.WriteLine("✓ Otwarto IN endpoint przez ReadEndpointID.Ep01");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"✗ Nie można otworzyć IN endpoint: {ex2.Message}");
                    }
                }
            }

            if (outEndpoint == null)
            {
                Console.WriteLine("✗ Nie znaleziono OUT endpoint - programowanie przycisków nie będzie działać");
                return false;
            }
            
            if (inEndpoint == null)
            {
                Console.WriteLine("⚠ Nie znaleziono IN endpoint - odczyt przycisków nie będzie działał");
                Console.WriteLine("   Programowanie przycisków będzie działać, ale odczyt nie");
            }

            _writer = outEndpoint;
            _reader = inEndpoint;

            var outEpId = _writer?.EndpointInfo?.Descriptor.EndpointID ?? 0;
            var inEpId = _reader?.EndpointInfo?.Descriptor.EndpointID ?? 0;
            Console.WriteLine($"\n✓ Otwarto endpointy (OUT: 0x{outEpId:X2}, IN: 0x{inEpId:X2})");
            
            if (inEpId == 0)
            {
                Console.WriteLine("⚠ UWAGA: Endpoint IN (odczyt) nie został otwarty - odczyt przycisków nie będzie działał");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd połączenia LibUSB: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Wysyła dane przez USB interrupt transfer
    /// </summary>
    public bool WriteData(byte[] data)
    {
        if (!IsConnected || _writer == null)
        {
            Console.WriteLine("✗ Urządzenie USB nie jest połączone");
            return false;
        }

        if (data == null || data.Length == 0)
        {
            Console.WriteLine("✗ Brak danych do wysłania");
            return false;
        }

        try
        {
            // Wyślij dane przez interrupt transfer
            ErrorCode ec = _writer.Write(data, 5000, out int bytesWritten);

            if (ec != ErrorCode.None)
            {
                Console.WriteLine($"✗ Błąd LibUSB Write: {ec} ({UsbDevice.LastErrorString})");
                return false;
            }

            if (bytesWritten != data.Length)
            {
                Console.WriteLine($"⚠ Wysłano tylko {bytesWritten} z {data.Length} bajtów");
                return false;
            }

            Console.WriteLine($"✓ Wysłano {bytesWritten} bajtów przez USB interrupt transfer");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd podczas wysyłania danych LibUSB: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Odczytuje dane z urządzenia
    /// </summary>
    public byte[]? ReadData(int bufferSize = 512, int timeout = 1000)
    {
        if (!IsConnected)
        {
            return null;
        }
        
        if (_reader == null)
        {
            // Endpoint IN nie został otwarty - nie można odczytać
            return null;
        }

        try
        {
            byte[] buffer = new byte[bufferSize];
            ErrorCode ec = _reader.Read(buffer, timeout, out int bytesRead);

            if (ec != ErrorCode.None)
            {
                // Nie wyświetlaj błędów timeout i Win32Error - to normalne gdy nie ma danych
                if (ec != ErrorCode.IoTimedOut && ec != ErrorCode.Win32Error)
                {
                    // Wyświetlaj tylko inne błędy (tylko raz na 1000 prób, żeby nie spamować)
                    // Console.WriteLine($"✗ Błąd LibUSB Read: {ec}");
                }
                return null;
            }

            if (bytesRead > 0)
            {
                byte[] data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                return data;
            }

            return null;
        }
        catch (Exception ex)
        {
            // Nie wyświetlaj błędów timeout
            if (!ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                // Console.WriteLine($"✗ Błąd podczas odczytu danych LibUSB: {ex.Message}");
            }
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_usbDevice != null)
            {
                if (_usbDevice.IsOpen)
                {
                    // Zwolnij interface
                    if (_usbDevice is IUsbDevice wholeUsbDevice)
                    {
                        wholeUsbDevice.ReleaseInterface(0);
                    }

                    _usbDevice.Close();
                }
            }

            _usbDevice = null;
            _writer = null;
            _reader = null;
            _disposed = true;

            // Zwolnij zasoby LibUSB
            UsbDevice.Exit();
        }
    }
}
