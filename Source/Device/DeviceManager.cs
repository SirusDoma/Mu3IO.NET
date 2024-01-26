using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using static Windows.Win32.PInvoke;

namespace Mu3IO;

public static class DeviceManager
{
    private static Dictionary<string, bool> _watchStates = new();

    public static unsafe IEnumerable<Device> Enumerate()
    {
        List<Device> devices = [];

        // Get Device Info
        var hDevInfo = SetupDiGetClassDevs(GUID_DEVINTERFACE_USB_DEVICE, null, HWND.Null, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);
        if (hDevInfo.IsInvalid)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Logger.Debug($"DeviceEnumerator: [SetupDiGetClassDevs] => INVALID_HANDLE_VALUE ({errorCode})");
            return devices;
        }

        // Enumerate connected devices in the device info
        uint deviceIndex = 0;
        var deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
        while (SetupDiEnumDeviceInterfaces(hDevInfo, null, GUID_DEVINTERFACE_USB_DEVICE, deviceIndex, ref deviceInterfaceData))
        {
            // Get the length of SP_DEVICE_INTERFACE_DETAIL_DATA structure.
            // Skip ERROR_INSUFFICIENT_BUFFER since it is expected
            uint requiredSize = 0;
            if (!SetupDiGetDeviceInterfaceDetail(hDevInfo, deviceInterfaceData, null, 0, &requiredSize, null))
            {
                var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
                if (errorCode != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                {
                    Logger.Debug($"DeviceEnumerator: [SetupDiGetDeviceInterfaceDetail] => Failed to get the required size ({errorCode})");
                    return devices;
                }
            }

            // Allocate memory for the SP_DEVICE_INTERFACE_DETAIL_DATA structure using the returned buffer size.
            // Can't use SP_DEVICE_INTERFACE_DETAIL_DATA directly since we need to initialize the struct with required size.
            IntPtr buffer = Marshal.AllocHGlobal((int)requiredSize);

            // Store cbSize in the first bytes of the array.
            Marshal.WriteInt32(buffer, 8); // For x64 bit system (NativeAOT doesn't support x86 anyway)

            // Additionally, retrieve the dev info data
            var devInfoData = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

            // Retrieve the actual SP_DEVICE_INTERFACE_DETAIL_DATA
            if (!SetupDiGetDeviceInterfaceDetail(hDevInfo, deviceInterfaceData, (SP_DEVICE_INTERFACE_DETAIL_DATA_W*)buffer, requiredSize, &requiredSize, &devInfoData))
            {
                var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
                Logger.Debug($"DeviceEnumerator: [SetupDiGetDeviceInterfaceDetail] => Failed to get the SP_DEVICE_INTERFACE_DETAIL_DATA ({errorCode})");
                return devices.ToArray();
            }

            // Skip cbSize (4 bytes) and jump directly into SP_DEVICE_INTERFACE_DETAIL_DATA_W.DevicePath
            // This because SP_DEVICE_INTERFACE_DETAIL_DATA_W use null-terminated and DevicePath might in wide string.
            string? devicePath = Marshal.PtrToStringUni(new IntPtr(buffer.ToInt64() + 4));
            if (string.IsNullOrEmpty(devicePath))
                continue;

            // Retrieve device information
            var device = GetDevice(devicePath, hDevInfo, devInfoData);
            if (device != null)
                devices.Add(device);
            else
                Logger.Debug($"DeviceEnumerator: [GetDevice] => Skipping {devicePath}..");

            deviceIndex++;
        }

        return devices;
    }

    public static Device? FindDevice(ushort vendorId, ushort productId)
    {
        return Enumerate().FirstOrDefault(device => device.VendorId == vendorId && device.ProductId == productId);
    }

    public static void Watch(ushort vendorId, ushort productId, Action<Device> connected, Action disconnected)
    {
        _watchStates[$"VID_{vendorId:X}/PID_{productId:X}"] = true;
        Task.Run(async () =>
        {
            var last = FindDevice(vendorId, productId);
            while (_watchStates[$"VID_{vendorId:X}/PID_{productId:X}"])
            {
                var current = FindDevice(vendorId, productId);
                if (last == null && current != null)
                    connected(current!);
                else if (last != null && current == null)
                    disconnected();

                last = current;
                await Task.Delay(500);
            }
        });
    }

    public static void Watch(Device device, Action<Device> connected, Action disconnected)
    {
        Watch(device.VendorId, device.ProductId, connected, disconnected);
    }

    public static void StopWatch(ushort vendorId, ushort productId)
    {
        _watchStates[$"VID_{vendorId:X}/PID_{productId:X}"] = false;
    }

    public static void StopWatch(Device device)
    {
        StopWatch(device.VendorId, device.ProductId);
    }

    private static Device? GetDevice(string devicePath, SetupDiDestroyDeviceInfoListSafeHandle hDevInfo, SP_DEVINFO_DATA devInfoData)
    {
        string description   = GetDeviceProperty(hDevInfo, devInfoData, SPDRP_DEVICEDESC)   ?? "Unknown";
        string name          = GetDeviceProperty(hDevInfo, devInfoData, SPDRP_FRIENDLYNAME) ?? description;
        string manufacturer  = GetDeviceProperty(hDevInfo, devInfoData, SPDRP_MFG)          ?? "Unknown";
        string classGuid     = GetDeviceProperty(hDevInfo, devInfoData, SPDRP_CLASSGUID)    ?? string.Empty;
        string[] hardwareIds = GetDeviceProperty(hDevInfo, devInfoData, SPDRP_HARDWAREID)   ?? Array.Empty<string>();

        ushort vid = 0, pid = 0;
        var regex = new Regex("^USB\\\\VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
        foreach (string hardwareId in hardwareIds)
        {
            var match = regex.Match(hardwareId);
            if (!match.Success)
                continue;

            vid = ushort.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.AllowHexSpecifier);
            pid = ushort.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.AllowHexSpecifier);
            break;
        }

        if (vid == 0 || pid == 0)
        {
            Logger.Debug($"DeviceEnumerator: [GetDevice] => Vendor ID or Product ID cannot be found for hardware {devicePath}");
            return null;
        }

        return new Device(devicePath, name, description, manufacturer, string.IsNullOrEmpty(classGuid) ? null : new Guid(classGuid), vid, pid);
    }

    private static unsafe DevicePropertyData? GetDeviceProperty(SetupDiDestroyDeviceInfoListSafeHandle hDevInfo, SP_DEVINFO_DATA devInfoData, uint property)
    {
        // Get the required size of the property
        uint requiredSize;
        if (!SetupDiGetDeviceRegistryProperty(hDevInfo, devInfoData, property, null, null, &requiredSize))
        {
            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            if (errorCode != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            {
                // Ignore ERROR_INVALID_DATA on initial call because the device might not have the given property
                if (errorCode != WIN32_ERROR.ERROR_INVALID_DATA)
                    Logger.Debug($"DeviceEnumerator: [SetupDiGetDeviceRegistryProperty] => Failed to allocate the device property (property: {property} | error: {errorCode} | code: {(int)errorCode})");

                return null;
            }
        }

        // Get the actual property
        byte[] propertyBuffer = new byte[requiredSize];
        REG_VALUE_TYPE regType;
        if (!SetupDiGetDeviceRegistryProperty(hDevInfo, devInfoData, property, (uint*)&regType, propertyBuffer, &requiredSize))
        {
            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            Logger.Debug($"DeviceEnumerator: [SetupDiGetDeviceRegistryProperty] => Failed to retrieve device property (property: {property} | error: {errorCode} | code: {{(int)errorCode}})");

            return null;
        }

        return new DevicePropertyData(regType, propertyBuffer);
    }

    private class DevicePropertyData(REG_VALUE_TYPE regValueType, byte[] propertyBuffer)
    {
        public byte[] PropertyBuffer { get; } = propertyBuffer;
        public REG_VALUE_TYPE RegValueType { get; } = regValueType;

        public override string ToString()
        {
            if (RegValueType != REG_VALUE_TYPE.REG_SZ)
            {
                Logger.Debug($"DevicePropertyData: [ToString] => Property value type is mismatch ({RegValueType})");
                return string.Empty;
            }

            return System.Text.Encoding.Unicode.GetString(PropertyBuffer).Trim(' ', '\0');
        }

        public string[] ToMultiString()
        {
            if (RegValueType != REG_VALUE_TYPE.REG_MULTI_SZ)
            {
                Logger.Debug($"DevicePropertyData: [ToString] => Property value type is mismatch ({RegValueType})");
                return [];
            }

            return System.Text.Encoding.Unicode.GetString(PropertyBuffer).Split('\0', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        public static implicit operator string(DevicePropertyData property)
        {
            return property.ToString();
        }

        public static implicit operator string[](DevicePropertyData property)
        {
            return property.ToMultiString();
        }
    }
}