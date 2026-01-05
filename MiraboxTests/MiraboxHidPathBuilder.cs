using System;
using System.Linq;

namespace mirabox;

public static class MiraboxHidPathBuilder
{
    public static string? BuildHidPath(UsbDeviceInfo device)
    {
        if (device.DeviceId == null)
            return null;

        // Szukaj urządzenia HID z tym samym VID/PID
        if (!device.DeviceId.Contains("VID_") || !device.DeviceId.Contains("PID_"))
            return null;

        var vidIndex = device.DeviceId.IndexOf("VID_");
        var pidIndex = device.DeviceId.IndexOf("PID_");

        if (vidIndex < 0 || pidIndex < 0)
            return null;

        // Wyciągnij VID
        var vidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, vidIndex + 4);
        if (vidEnd < 0) vidEnd = device.DeviceId.Length;
        var vid = device.DeviceId.Substring(vidIndex + 4, vidEnd - vidIndex - 4);

        // Wyciągnij PID
        var pidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, pidIndex + 4);
        if (pidEnd < 0) pidEnd = device.DeviceId.Length;
        var pid = device.DeviceId.Substring(pidIndex + 4, pidEnd - pidIndex - 4);

        // Wyciągnij numer seryjny - może być po # lub po \
        string? serial = null;
        
        // Spróbuj znaleźć po #
        var hashIndex = device.DeviceId.IndexOf("#", pidIndex);
        if (hashIndex >= 0)
        {
            var hashEnd = device.DeviceId.IndexOfAny(new[] { '#', '\\' }, hashIndex + 1);
            if (hashEnd < 0) hashEnd = device.DeviceId.Length;
            if (hashEnd > hashIndex + 1)
            {
                serial = device.DeviceId.Substring(hashIndex + 1, hashEnd - hashIndex - 1);
            }
        }
        
        // Jeśli nie znaleziono po #, spróbuj po \
        if (string.IsNullOrEmpty(serial))
        {
            var backslashIndex = device.DeviceId.IndexOf("\\", pidIndex);
            if (backslashIndex >= 0)
            {
                var backslashEnd = device.DeviceId.IndexOfAny(new[] { '\\', '#' }, backslashIndex + 1);
                if (backslashEnd < 0) backslashEnd = device.DeviceId.Length;
                if (backslashEnd > backslashIndex + 1)
                {
                    serial = device.DeviceId.Substring(backslashIndex + 1, backslashEnd - backslashIndex - 1);
                }
            }
        }

        // Jeśli nadal nie ma numeru seryjnego, użyj domyślnego
        if (string.IsNullOrEmpty(serial))
            serial = "00000001";

        // Buduj ścieżkę HID
        return $@"\\?\HID#VID_{vid}&PID_{pid}#{serial}#{{4d1e55b2-f16f-11cf-88cb-001111000030}}";
    }

    public static UsbDeviceInfo? FindHidDevice(UsbDeviceInfo usbDevice)
    {
        if (usbDevice.DeviceId == null || !usbDevice.DeviceId.Contains("VID_") || !usbDevice.DeviceId.Contains("PID_"))
            return null;

        var vidIndex = usbDevice.DeviceId.IndexOf("VID_");
        var pidIndex = usbDevice.DeviceId.IndexOf("PID_");

        if (vidIndex < 0 || pidIndex < 0)
            return null;

        var vidEnd = usbDevice.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, vidIndex + 4);
        if (vidEnd < 0) vidEnd = usbDevice.DeviceId.Length;
        var vid = usbDevice.DeviceId.Substring(vidIndex + 4, vidEnd - vidIndex - 4);

        var pidEnd = usbDevice.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, pidIndex + 4);
        if (pidEnd < 0) pidEnd = usbDevice.DeviceId.Length;
        var pid = usbDevice.DeviceId.Substring(pidIndex + 4, pidEnd - pidIndex - 4);

        // Znajdź odpowiadające urządzenie HID
        var allDevices = MiraboxDeviceFinder.GetAllUsbDevices();
        var hidDevice = allDevices.FirstOrDefault(d =>
            d.DeviceId != null &&
            d.DeviceId.Contains("HID\\") &&
            d.VendorId == vid &&
            (d.ProductId == pid || d.ProductId?.StartsWith(pid) == true)
        );

        return hidDevice;
    }

    public static string? BuildUsbPath(UsbDeviceInfo device)
    {
        if (device.DeviceId == null)
            return null;

        // Buduj ścieżkę USB zamiast HID
        // Format: \\?\USB#VID_xxxx&PID_xxxx#serial#{guid}
        if (!device.DeviceId.Contains("VID_") || !device.DeviceId.Contains("PID_"))
            return null;

        var vidIndex = device.DeviceId.IndexOf("VID_");
        var pidIndex = device.DeviceId.IndexOf("PID_");

        if (vidIndex < 0 || pidIndex < 0)
            return null;

        // Wyciągnij VID
        var vidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, vidIndex + 4);
        if (vidEnd < 0) vidEnd = device.DeviceId.Length;
        var vid = device.DeviceId.Substring(vidIndex + 4, vidEnd - vidIndex - 4);

        // Wyciągnij PID
        var pidEnd = device.DeviceId.IndexOfAny(new[] { '&', '#', '\\' }, pidIndex + 4);
        if (pidEnd < 0) pidEnd = device.DeviceId.Length;
        var pid = device.DeviceId.Substring(pidIndex + 4, pidEnd - pidIndex - 4);

        // Wyciągnij numer seryjny
        string? serial = null;
        var backslashIndex = device.DeviceId.IndexOf("\\", pidIndex);
        if (backslashIndex >= 0)
        {
            var backslashEnd = device.DeviceId.IndexOfAny(new[] { '\\', '#' }, backslashIndex + 1);
            if (backslashEnd < 0) backslashEnd = device.DeviceId.Length;
            if (backslashEnd > backslashIndex + 1)
            {
                serial = device.DeviceId.Substring(backslashIndex + 1, backslashEnd - backslashIndex - 1);
            }
        }

        if (string.IsNullOrEmpty(serial))
            serial = "00000001";

        // GUID dla USB devices
        return $@"\\?\USB#VID_{vid}&PID_{pid}#{serial}#{{28d78fad-5a12-11d1-ae5b-0000f803a8c2}}";
    }
}

