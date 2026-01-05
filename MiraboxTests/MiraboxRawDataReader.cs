using System;
using System.Linq;
using System.Threading;

namespace mirabox;

/// <summary>
/// Odczytuje surowe dane z urządzenia - do debugowania formatu
/// </summary>
public class MiraboxRawDataReader
{
    public static void Main()
    {
        Console.WriteLine("=== ODCZYT SUROWYCH DANYCH Z MIRABOX ===\n");
        Console.WriteLine("Wyświetlam WSZYSTKIE dane przychodzące z urządzenia.");
        Console.WriteLine("Naciśnij przycisk na urządzeniu, aby zobaczyć format danych.\n");
        Console.WriteLine("Naciśnij Ctrl+C, aby zakończyć.\n");
        
        using var libUsbTransfer = new MiraboxLibUsbTransfer();
        
        if (!libUsbTransfer.Connect(0x5548, 0x6670))
        {
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem");
            return;
        }
        
        Console.WriteLine("✓ Połączono! Czekam na dane...\n");
        
        var readCount = 0;
        var lastDataTime = DateTime.MinValue;
        
        while (true)
        {
            var data = libUsbTransfer.ReadData(bufferSize: 64, timeout: 50);
            readCount++;
            
            // Wyświetl status co 10 sekund
            var now = DateTime.Now;
            if ((now - lastDataTime).TotalSeconds >= 10)
            {
                Console.WriteLine($"[Próba {readCount} - czekam na dane...]");
                lastDataTime = now;
            }
            
            if (data != null && data.Length > 0)
            {
                Console.WriteLine($"\n{new string('=', 60)}");
                Console.WriteLine($"✓ ODEBRANO {data.Length} BAJTÓW (próba {readCount}):");
                Console.WriteLine($"  Hex: {BitConverter.ToString(data)}");
                Console.WriteLine($"  Dec: {string.Join(" ", data.Select(b => b.ToString().PadLeft(3)))}");
                
                // Analiza - szukaj numerów przycisków
                Console.WriteLine($"  Analiza:");
                bool foundButton = false;
                
                for (int i = 0; i < data.Length; i++)
                {
                    var byteValue = data[i];
                    
                    if (byteValue >= 1 && byteValue <= 15)
                    {
                        Console.WriteLine($"    Bajt[{i}] = {byteValue} (0x{byteValue:X2}) ← MOŻE BYĆ PRZYCISK {byteValue}!");
                        foundButton = true;
                    }
                    else if (byteValue != 0)
                    {
                        Console.WriteLine($"    Bajt[{i}] = {byteValue} (0x{byteValue:X2})");
                    }
                }
                
                // Sprawdź bity
                Console.WriteLine($"  Analiza bitowa:");
                for (int i = 0; i < Math.Min(data.Length, 8); i++)
                {
                    byte byteValue = data[i];
                    if (byteValue != 0)
                    {
                        Console.WriteLine($"    Bajt[{i}] = 0x{byteValue:X2} (bin: {Convert.ToString(byteValue, 2).PadLeft(8, '0')})");
                        for (int bit = 0; bit < 8; bit++)
                        {
                            if ((byteValue & (1 << bit)) != 0)
                            {
                                int possibleButton = i * 8 + bit + 1;
                                if (possibleButton <= 15)
                                {
                                    Console.WriteLine($"      Bit {bit} = 1 → MOŻE BYĆ PRZYCISK {possibleButton}!");
                                    foundButton = true;
                                }
                            }
                        }
                    }
                }
                
                if (!foundButton)
                {
                    Console.WriteLine($"    Nie znaleziono oczywistego numeru przycisku w danych");
                }
                
                Console.WriteLine($"{new string('=', 60)}\n");
                lastDataTime = DateTime.Now;
            }
            
            Thread.Sleep(10);
        }
    }
}

