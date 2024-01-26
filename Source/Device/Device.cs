using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

using Microsoft.Win32.SafeHandles;

using static Windows.Win32.PInvoke;

namespace Mu3IO;

public class Device(string devicePath, string name, string description, string manufacturer, Guid? classGuid, ushort vendorId, ushort productId) : IDisposable
{
    public Device()
        : this (string.Empty, string.Empty, string.Empty, string.Empty, null, 0, 0)
    {
    }

    public string DevicePath { get; } = devicePath;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Manufacturer { get; } = manufacturer;
    public Guid? ClassGuid { get; } = classGuid;
    public ushort VendorId { get; } = vendorId;
    public ushort ProductId { get; } = productId;

    private SafeFileHandle? _handle;

    public SafeFileHandle Open()
    {
        if (_handle is { IsInvalid: false })
        {
            Logger.Debug("Device: already open");
            return _handle;
        }

        // Open device
        return _handle = CreateFile(
            DevicePath,
            (uint)(GENERIC_ACCESS_RIGHTS.GENERIC_READ | GENERIC_ACCESS_RIGHTS.GENERIC_WRITE),
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_ALWAYS,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED,
            null
        );
    }

    public void Close()
    {
        if (_handle is { IsInvalid: false })
            _handle.Close();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_handle is { IsInvalid: false })
            _handle.Dispose();
    }
}