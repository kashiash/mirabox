using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace mirabox;

/// <summary>
/// Klasa do bezpośredniego transferu USB (bulk transfer) zamiast HID
/// Na podstawie analizy protokołu Mirabox/StreamDock
/// </summary>
public class MiraboxUsbTransfer : IDisposable
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    private SafeFileHandle? _deviceHandle;
    private bool _disposed = false;

    public bool IsConnected => _deviceHandle != null && !_deviceHandle.IsInvalid;

    /// <summary>
    /// Łączy się z urządzeniem USB używając ścieżki USB zamiast HID
    /// </summary>
    public bool Connect(string usbPath)
    {
        try
        {
            _deviceHandle = CreateFile(
                usbPath,
                GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            return IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd połączenia USB: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Wysyła dane przez USB bulk transfer
    /// </summary>
    public bool WriteData(byte[] data)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Urządzenie USB nie jest połączone");
            return false;
        }

        if (data == null || data.Length == 0)
        {
            Console.WriteLine("Brak danych do wysłania");
            return false;
        }

        try
        {
            if (WriteFile(_deviceHandle!, data, (uint)data.Length, out uint bytesWritten, IntPtr.Zero))
            {
                if (bytesWritten == data.Length)
                {
                    Console.WriteLine($"✓ Wysłano {bytesWritten} bajtów przez USB bulk transfer");
                    return true;
                }
                else
                {
                    Console.WriteLine($"⚠ Wysłano tylko {bytesWritten} z {data.Length} bajtów");
                    return false;
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                var errorMessage = error switch
                {
                    87 => "ERROR_INVALID_PARAMETER - nieprawidłowy format danych",
                    5 => "ERROR_ACCESS_DENIED - brak uprawnień",
                    2 => "ERROR_FILE_NOT_FOUND - urządzenie nie znalezione",
                    _ => $"Błąd systemowy: {error}"
                };
                Console.WriteLine($"✗ Błąd USB transfer ({error}): {errorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wysyłania danych USB: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _deviceHandle?.Dispose();
            _disposed = true;
        }
    }
}

