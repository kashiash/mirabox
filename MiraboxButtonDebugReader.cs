using System;
using System.Linq;

namespace mirabox;

/// <summary>
/// Debug reader - wyświetla surowe dane z urządzenia, żeby zobaczyć format
/// </summary>
public class MiraboxButtonDebugReader
{
    private readonly MiraboxLibUsbTransfer _usbTransfer;
    private int _readCount = 0;
    private DateTime _lastDebugTime = DateTime.Now;
    
    public MiraboxButtonDebugReader(MiraboxLibUsbTransfer usbTransfer)
    {
        _usbTransfer = usbTransfer;
    }
    
    public void StartDebugReading()
    {
        Console.WriteLine("=== DEBUG ODCZYTU PRZYCISKÓW ===");
        Console.WriteLine("Wyświetlam wszystkie dane przychodzące z urządzenia...");
        Console.WriteLine("Naciśnij przycisk na urządzeniu, aby zobaczyć format danych.\n");
        
        while (true)
        {
            var data = _usbTransfer.ReadData(bufferSize: 64, timeout: 10);
            _readCount++;
            
            // Wyświetl status co 5 sekund
            var now = DateTime.Now;
            if ((now - _lastDebugTime).TotalSeconds >= 5)
            {
                Console.WriteLine($"[Próba {_readCount} - czekam na dane...]");
                _lastDebugTime = now;
            }
            
            if (data != null && data.Length > 0)
            {
                // Wyświetl wszystkie dane
                Console.WriteLine($"\n✓ ODEBRANO {data.Length} BAJTÓW:");
                Console.WriteLine($"  Hex: {BitConverter.ToString(data)}");
                Console.WriteLine($"  Dec: {string.Join(" ", data.Select(b => b.ToString().PadLeft(3)))}");
                Console.WriteLine($"  Bin: {string.Join(" ", data.Take(16).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}");
                
                // Analiza - szukaj numerów przycisków
                Console.WriteLine("  Analiza:");
                for (int i = 0; i < Math.Min(data.Length, 16); i++)
                {
                    if (data[i] >= 1 && data[i] <= 15)
                    {
                        Console.WriteLine($"    Bajt[{i}] = {data[i]} (może być przycisk {data[i]})");
                    }
                    else if (data[i] != 0)
                    {
                        Console.WriteLine($"    Bajt[{i}] = {data[i]} (0x{data[i]:X2})");
                    }
                }
                Console.WriteLine();
            }
            
            System.Threading.Thread.Sleep(10);
        }
    }
}

