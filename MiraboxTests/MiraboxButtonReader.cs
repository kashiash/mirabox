using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Text;

namespace mirabox;

public class MiraboxButtonReader : IDisposable, IMiraboxReader
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
    private static extern bool ReadFile(
        SafeFileHandle hFile,
        [Out] byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetFeature(
        SafeFileHandle hFile,
        byte[] lpReportBuffer,
        uint reportBufferLength);

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    private SafeFileHandle? _deviceHandle;
    private bool _disposed = false;

    public bool IsConnected => _deviceHandle != null && !_deviceHandle.IsInvalid;

    public bool Connect(string devicePath)
    {
        try
        {
            _deviceHandle = CreateFile(
                devicePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            return IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd połączenia z urządzeniem: {ex.Message}");
            return false;
        }
    }

    public byte[]? ReadData(int bufferSize = 64)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Urządzenie nie jest połączone");
            return null;
        }

        try
        {
            var buffer = new byte[bufferSize];
            if (ReadFile(_deviceHandle!, buffer, (uint)bufferSize, out uint bytesRead, IntPtr.Zero))
            {
                if (bytesRead > 0)
                {
                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    return data;
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"Błąd odczytu: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas odczytu danych: {ex.Message}");
        }

        return null;
    }

    public string FormatButtonData(byte[] data)
    {
        if (data == null || data.Length == 0)
            return "Brak danych";

        var sb = new StringBuilder();
        sb.AppendLine($"Odebrano {data.Length} bajtów:");
        sb.AppendLine($"Hex: {BitConverter.ToString(data)}");
        sb.AppendLine($"Bin: {string.Join(" ", data.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}");

        // Próba interpretacji jako przyciski (pierwszy bajt często zawiera stan przycisków)
        if (data.Length >= 1)
        {
            sb.AppendLine($"\nAnaliza przycisków:");
            for (int i = 0; i < Math.Min(8, data.Length); i++)
            {
                var byteValue = data[i];
                if (byteValue != 0)
                {
                    sb.AppendLine($"  Bajt {i}: {byteValue} (0x{byteValue:X2})");
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if ((byteValue & (1 << bit)) != 0)
                        {
                            sb.AppendLine($"    Przycisk {i * 8 + bit + 1} jest naciśnięty");
                        }
                    }
                }
            }
        }

        return sb.ToString();
    }

    public virtual bool WriteData(byte[] data, bool useFeatureReport = true)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Urządzenie nie jest połączone");
            return false;
        }

        if (data == null || data.Length == 0)
        {
            Console.WriteLine("Brak danych do wysłania");
            return false;
        }

        try
        {
            if (useFeatureReport)
            {
                // Użyj Feature Report (HidD_SetFeature) - standardowe dla urządzeń HID
                // Feature Reports wymagają, aby pierwszy bajt był Report ID
                // Dla Mirabox/StreamDock, Report ID może być 0x00 lub może być wbudowany w komendę
                byte[] reportData;
                
                // Sprawdź czy pierwszy bajt to już Report ID (0x00-0x7F)
                // Jeśli data[0] to 0x02 (komenda), to prawdopodobnie nie ma Report ID
                if (data.Length > 0 && data[0] == 0x02)
                {
                    // Dodaj Report ID (0x00) na początku
                    reportData = new byte[data.Length + 1];
                    reportData[0] = 0x00; // Report ID dla Feature Report
                    Array.Copy(data, 0, reportData, 1, data.Length);
                }
                else if (data.Length > 0 && data[0] >= 0x00 && data[0] < 0x80)
                {
                    // Prawdopodobnie już jest Report ID, użyj danych jak są
                    reportData = data;
                }
                else
                {
                    // Dodaj Report ID (0x00 dla Feature Report)
                    reportData = new byte[data.Length + 1];
                    reportData[0] = 0x00; // Report ID
                    Array.Copy(data, 0, reportData, 1, data.Length);
                }

                if (HidD_SetFeature(_deviceHandle!, reportData, (uint)reportData.Length))
                {
                    Console.WriteLine($"✓ Wysłano Feature Report ({reportData.Length} bajtów, Report ID: 0x{reportData[0]:X2})");
                    return true;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    var errorMessage = error switch
                    {
                        1 => "ERROR_INVALID_FUNCTION - urządzenie nie obsługuje Feature Reports",
                        87 => "ERROR_INVALID_PARAMETER - nieprawidłowy format danych lub Report ID",
                        5 => "ERROR_ACCESS_DENIED - brak uprawnień",
                        2 => "ERROR_FILE_NOT_FOUND - urządzenie nie znalezione",
                        _ => $"Błąd systemowy: {error}"
                    };
                    Console.WriteLine($"✗ Błąd Feature Report ({error}): {errorMessage}");
                    
                    // Spróbuj użyć WriteFile jako fallback (Output Report)
                    Console.WriteLine("  Próba użycia Output Report (WriteFile) jako alternatywy...");
                    return WriteData(data, false);
                }
            }
            else
            {
                // Użyj Output Report (WriteFile)
                // Dla HID Output Reports, pierwszy bajt powinien być Report ID
                // Jeśli data[0] to już 0x00 (Report ID), użyj danych jak są
                // Jeśli data[0] to 0x02 (komenda), dodaj Report ID 0x00 na początku
                byte[] outputData;
                if (data.Length > 0 && data[0] == 0x00)
                {
                    // Już ma Report ID
                    outputData = data;
                }
                else if (data.Length > 0 && data[0] == 0x02)
                {
                    // Dodaj Report ID 0x00 na początku
                    outputData = new byte[data.Length + 1];
                    outputData[0] = 0x00; // Report ID
                    Array.Copy(data, 0, outputData, 1, data.Length);
                }
                else
                {
                    // Użyj danych jak są
                    outputData = data;
                }
                
                if (WriteFile(_deviceHandle!, outputData, (uint)outputData.Length, out uint bytesWritten, IntPtr.Zero))
                {
                    if (bytesWritten == outputData.Length)
                    {
                        Console.WriteLine($"✓ Wysłano Output Report ({bytesWritten} bajtów, Report ID: 0x{outputData[0]:X2})");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Wysłano tylko {bytesWritten} z {outputData.Length} bajtów");
                        return false;
                    }
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    var errorMessage = error switch
                    {
                        87 => "ERROR_INVALID_PARAMETER - nieprawidłowy format danych lub rozmiar pakietu",
                        5 => "ERROR_ACCESS_DENIED - brak uprawnień",
                        2 => "ERROR_FILE_NOT_FOUND - urządzenie nie znalezione",
                        _ => $"Błąd systemowy: {error}"
                    };
                    Console.WriteLine($"✗ Błąd Output Report ({error}): {errorMessage}");
                    Console.WriteLine($"  Próbowano wysłać {outputData.Length} bajtów, pierwsze bajty: {BitConverter.ToString(outputData.Take(Math.Min(16, outputData.Length)).ToArray())}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wysyłania danych: {ex.Message}");
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

