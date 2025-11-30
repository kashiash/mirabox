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

            // Znajdź endpointy - spróbuj różne
            UsbEndpointWriter? outEndpoint = null;
            UsbEndpointReader? inEndpoint = null;
            
            // Spróbuj endpoint 0x01
            outEndpoint = _usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
            inEndpoint = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
            
            if (outEndpoint == null)
            {
                // Spróbuj endpoint 0x02
                Console.WriteLine("Endpoint 0x01 niedostępny, próbuję 0x02...");
                outEndpoint = _usbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
                inEndpoint = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
            }

            if (outEndpoint == null)
            {
                Console.WriteLine("✗ Nie znaleziono OUT endpoint");
                return false;
            }

            _writer = outEndpoint;
            _reader = inEndpoint;

            var outEpId = _writer?.EndpointInfo?.Descriptor.EndpointID ?? 0;
            var inEpId = _reader?.EndpointInfo?.Descriptor.EndpointID ?? 0;
            Console.WriteLine($"✓ Otwarto endpointy (OUT: 0x{outEpId:X2}, IN: 0x{inEpId:X2})");
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
        if (!IsConnected || _reader == null)
        {
            Console.WriteLine("✗ Urządzenie USB nie jest połączone");
            return null;
        }

        try
        {
            byte[] buffer = new byte[bufferSize];
            ErrorCode ec = _reader.Read(buffer, timeout, out int bytesRead);

            if (ec != ErrorCode.None)
            {
                if (ec != ErrorCode.IoTimedOut)
                {
                    Console.WriteLine($"✗ Błąd LibUSB Read: {ec}");
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
            Console.WriteLine($"✗ Błąd podczas odczytu danych LibUSB: {ex.Message}");
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
