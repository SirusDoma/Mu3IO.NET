using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Usb;
using Windows.Win32.Foundation;

using static Windows.Win32.PInvoke;

namespace Mu3IO;

public abstract class WinUsb : IDisposable
{
    private readonly WinUsb_FreeSafeHandle _usbHandle;
    private readonly USB_INTERFACE_DESCRIPTOR _usbInterfaceDescriptor;

    public Device Device { get; }

    public SafeHandle UsbHandle => _usbHandle;

    protected WinUsb(Device device, ushort vendorId, ushort productId)
    {
        // Assign and validate the given device
        Device = device;
        if (Device.VendorId != vendorId || Device.ProductId != productId)
        {
            throw new ArgumentOutOfRangeException(nameof(device), "Invalid device identifiers");
        }

        // Open device
        var handle = Device.Open();

        // Validate device handle
        if (handle.IsInvalid)
        {
            handle.Close();

            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            throw new IOException($"Failed to open the device ({errorCode})");
        }

        // Initialize WinUSB
        if (!WinUsb_Initialize(handle, out _usbHandle))
        {
            handle.Close();

            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            throw new IOException($"Failed to initialize WinUSB ({errorCode})");
        }

        // Query USB Descriptor
        if (!WinUsb_QueryInterfaceSettings(_usbHandle, 0, out _usbInterfaceDescriptor))
        {
            handle.Close();
            _usbHandle.Close();

            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            throw new IOException($"Failed to perform QueryInterfaceSettings ({errorCode})");
        }
    }

    public abstract bool ReadInputData(out byte[] buffer, out int transferred);

    public abstract bool WriteOutputData(byte[] buffer, out int transferred);

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        Device.Dispose();
        UsbHandle.Dispose();
    }
}