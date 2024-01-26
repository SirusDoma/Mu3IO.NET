using System.Diagnostics.CodeAnalysis;

namespace Mu3IO;

#pragma warning disable IL2072

public static class ControllerFactory
{
    private class ControllerMetadata
    {
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
    }

    private static readonly Dictionary<Type, ControllerMetadata> Metadata = new();
    private static readonly Dictionary<Type, IController?> Controllers = new();

    public static void Register<T>()
        where T : IController, new()
    {
        Logger.Debug($"{typeof(T).Name}: Connecting..");

        Metadata[typeof(T)] = new ControllerMetadata();
        Controllers[typeof(T)] = new T();
    }

    public static void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ushort vendorId, ushort productId)
        where T : IController
    {
        try
        {
            Logger.Debug($"{typeof(T).Name}: Connecting..");
            var device = DeviceManager.FindDevice(vendorId, productId);
            if (device == null)
                Logger.Debug($"{typeof(T).Name}: Device is not connected");

            DeviceManager.Watch(vendorId, productId,
                dev => Controllers[typeof(T)] = (T)Activator.CreateInstance(typeof(T), dev)!,
                () => Controllers[typeof(T)] = null
            );

            Metadata[typeof(T)] = new ControllerMetadata { VendorId = vendorId, ProductId = productId };
            Controllers[typeof(T)] = device != null ? (T)Activator.CreateInstance(typeof(T), device)! : null;
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to register the controller: ({ex.Message})\n{ex.StackTrace}");
        }
    }

    public static void Refresh()
    {
        foreach (var meta in Metadata)
        {
            if (meta.Value.VendorId == 0 || meta.Value.ProductId == 0)
                continue;

            var device = DeviceManager.FindDevice(meta.Value.VendorId, meta.Value.ProductId);
            Controllers[meta.Key] = device != null ? (IController?)Activator.CreateInstance(meta.Key, device)! : null;
        }
    }

    public static IEnumerable<IController> Enumerate()
    {
        return Controllers.Values.Where((c) => c != null)!;
    }
}

#pragma warning restore IL2072