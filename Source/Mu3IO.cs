using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Mu3IO;

public static class Mu3IO
{
    public const string ConfigFileName = ".\\segatools.ini";

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_api_version")]
    public static ushort GetVersion()
    {
        return 0x0110;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_init")]
    public static int Initialize()
    {
        Logger.Debug($"IO: [mu3_io_init] => Initializing..");

        // Register known controller (registration order matters, first come first serve (FIFO))
        ControllerFactory.Register<Ontroller>(Ontroller.VendorId, Ontroller.ProductId);
        ControllerFactory.Register<XInput>();
        ControllerFactory.Register<Mouse>();
        ControllerFactory.Register<Keyboard>();

        Logger.Debug($"IO: [mu3_io_init] => Hook initialized!");
        return HRESULT.S_OK;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_poll")]
    public static int Poll()
    {
        foreach (var controller in ControllerFactory.Enumerate())
        {
            try
            {
                if (!controller.Poll())
                    Logger.Debug($"IO: [mu3_io_poll] => ({controller.GetType().Name}) Failed to poll the input data");
            }
            catch (Exception ex)
            {
                Logger.Debug($"IO: [mu3_io_poll] => ({controller.GetType().Name}) Exception when polling the input data ({ex.Message})\n{ex.StackTrace}");
            }
        }

        return HRESULT.S_OK;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_opbtns")]
    public static unsafe void GetOptionButtons(byte *opbtn)
    {
        if (opbtn == null)
            return;

        var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.OptionButtonsFlag != 0);
        *opbtn = controller?.OptionButtonsFlag ?? 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_gamebtns")]
    public static unsafe void GetGameButtons(byte *left, byte *right)
    {
        if (left != null)
        {
            var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.LeftGameButtonsFlag != 0);
            *left = controller?.LeftGameButtonsFlag ?? 0;;
        }

        if (right != null)
        {
            var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.RightGameButtonsFlag != 0);
            *right = controller?.RightGameButtonsFlag ?? 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_lever")]
    public static unsafe void GetLeverPosition(short *pos)
    {
        if (pos == null)
            return;

        var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.LeverEnabled);
        *pos = controller?.LeverPosition ?? short.MaxValue / 2; // fallback to center
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_set_leds")]
    public static unsafe int SetLeds(byte *payload)
    {
        foreach (var controller in ControllerFactory.Enumerate())
        {
            if (!controller.SetLeds(payload))
                Logger.Debug($"IO: [mu3_io_poll] => ({controller.GetType().Name}) Failed to write the output data");
        }

        return HRESULT.S_OK;
    }
}