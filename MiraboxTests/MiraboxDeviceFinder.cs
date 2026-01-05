using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace mirabox;

public class UsbDeviceInfo
{
    public string? Name { get; set; }
    public string? DeviceId { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
}

public static class MiraboxDeviceFinder
{
    public static List<UsbDeviceInfo> GetAllUsbDevices()
    {
        var devices = new List<UsbDeviceInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%USB%' OR DeviceID LIKE '%HID%'");
            foreach (ManagementObject device in searcher.Get())
            {
                var deviceId = device["DeviceID"]?.ToString();
                if (!string.IsNullOrEmpty(deviceId))
                {
                    var name = device["Name"]?.ToString() ?? "Nieznane";
                    var deviceInfo = new UsbDeviceInfo
                    {
                        Name = name,
                        DeviceId = deviceId,
                        Description = device["Description"]?.ToString() ?? "Brak opisu",
                        Status = device["Status"]?.ToString() ?? "Nieznany"
                    };

                    // Wyciągnij VID i PID z DeviceID
                    if (deviceId.Contains("VID_") && deviceId.Contains("PID_"))
                    {
                        var vidIndex = deviceId.IndexOf("VID_");
                        var pidIndex = deviceId.IndexOf("PID_");
                        
                        if (vidIndex >= 0)
                        {
                            var vidEnd = deviceId.IndexOf("&", vidIndex);
                            if (vidEnd < 0) vidEnd = deviceId.IndexOf("#", vidIndex);
                            if (vidEnd < 0) vidEnd = deviceId.Length;
                            deviceInfo.VendorId = deviceId.Substring(vidIndex + 4, vidEnd - vidIndex - 4);
                        }
                        
                        if (pidIndex >= 0)
                        {
                            var pidEnd = deviceId.IndexOf("&", pidIndex);
                            if (pidEnd < 0) pidEnd = deviceId.IndexOf("#", pidIndex);
                            if (pidEnd < 0) pidEnd = deviceId.Length;
                            deviceInfo.ProductId = deviceId.Substring(pidIndex + 4, pidEnd - pidIndex - 4);
                        }
                    }

                    devices.Add(deviceInfo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania urządzeń: {ex.Message}");
        }

        return devices;
    }

    public static List<UsbDeviceInfo> FindMiraboxDevices()
    {
        var allDevices = GetAllUsbDevices();
        
        // Szukaj po nazwie
        var byName = allDevices.Where(d => 
            d.Name != null && (
                d.Name.Contains("mirabox", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("Mirabox", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("MiraBox", StringComparison.OrdinalIgnoreCase)
            )
        ).ToList();
        
        if (byName.Count > 0)
            return byName;
        
        // Jeśli nie znaleziono po nazwie, szukaj po znanych VID/PID Mirabox
        // VID_5548 i PID_6670 to możliwe identyfikatory Mirabox
        var byVidPid = allDevices.Where(d =>
            (d.VendorId == "5548" && d.ProductId == "6670") ||
            (d.VendorId == "5548" && d.ProductId?.StartsWith("6670") == true)
        ).ToList();
        
        return byVidPid;
    }
    
    public static List<UsbDeviceInfo> FindDevicesByVidPid(string vendorId, string productId)
    {
        var allDevices = GetAllUsbDevices();
        return allDevices.Where(d =>
            d.VendorId == vendorId && 
            (d.ProductId == productId || d.ProductId?.StartsWith(productId) == true)
        ).ToList();
    }
}

