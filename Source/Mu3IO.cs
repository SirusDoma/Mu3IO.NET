using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Mu3IO;

public static class Mu3IO
{
    public const string ConfigFileName = ".\\segatools.ini";

    private static NamedPipeServerStream? pipeServer;
    private static StreamReader? pipeServerReader;
    private static NamedPipeClientStream? pipeClient;
    private static StreamWriter? pipeClientWriter;

    private static bool isInitialized = false;

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_api_version")]
    public static ushort GetVersion()
    {
        return 0x0101;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_init")]
    public static int Initialize()
    {
        if (isInitialized)
            return HRESULT.S_OK;

        isInitialized = true;

        Logger.Debug($"IO: [mu3_io_init] => Initializing...");
        Logger.Debug($"IO: [mu3_io_init] => Hook initialized!");

        //Let's get the name of the process that loaded us
        var process = Process.GetCurrentProcess();
        if (process.ProcessName == "amdaemon")
        {
            Logger.Debug($"IO: [mu3_io_init] => Loaded by amdaemon, initializing controller factory...");
            InitControllerFactory();
            InitNamedPipeServer();
        }
        else
        {
            Logger.Debug($"IO: [mu3_io_init] => Loaded by mu3, skipping controller factory initialization");
            InitNamedPipeClient();
        }

        return HRESULT.S_OK;
    }

    public static void InitControllerFactory()
    {
        if (!ControllerFactory.Enumerate().Any())
        {
            // Register known controller (registration order matters, first come first serve (FIFO))
            ControllerFactory.Register<Ontroller>(Ontroller.VendorId, Ontroller.ProductId);
            ControllerFactory.Register<XInput>();
            ControllerFactory.Register<Mouse>();
            ControllerFactory.Register<Keyboard>();
        }
    }

    private static void InitNamedPipeServer()
    {
        Logger.Debug($"IO: [initNamedPipeServer] => Starting named pipe server from amdeamon...");

        pipeServer = new NamedPipeServerStream("Mu3IO.Net_Pipe");
        Logger.Debug("IO: [NamedPipeServerStream] => Waiting for client connection...");

        pipeServerReader = new StreamReader(pipeServer);
        Task.Run(() =>
        {
            pipeServer?.WaitForConnection();
            Logger.Debug("IO: [NamedPipeServerStream] => Client connected!");

            string? readPayload;
            while ((readPayload = pipeServerReader?.ReadLine()) != null)
            {
                if (readPayload == "InitLeds")
                {
                    InternalInitLeds();
                }
                else
                {
                    SetLeds(readPayload);
                }
            }
        });
    }

    private static void InitNamedPipeClient()
    {
        Logger.Debug($"IO: [InitNamedPipeClient] => Starting named pipe client from mu3...");

        pipeClient = new NamedPipeClientStream(".", "Mu3IO.Net_Pipe", PipeDirection.Out);
        pipeClient.Connect();

        pipeClientWriter = new StreamWriter(pipeClient) { AutoFlush = true };
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_poll")]
    public static int Poll() // This is only called from amdaemon
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
    public static unsafe void GetOptionButtons(byte* opbtn)
    {
        if (opbtn == null)
            return;

        var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.OptionButtonsFlag != 0);
        *opbtn = controller?.OptionButtonsFlag ?? 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_gamebtns")]
    public static unsafe void GetGameButtons(byte* left, byte* right)
    {
        if (left != null)
        {
            var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.LeftGameButtonsFlag != 0);
            *left = controller?.LeftGameButtonsFlag ?? 0; ;
        }

        if (right != null)
        {
            var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.RightGameButtonsFlag != 0);
            *right = controller?.RightGameButtonsFlag ?? 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_lever")]
    public static unsafe void GetLeverPosition(short* pos)
    {
        if (pos == null)
            return;

        var controller = ControllerFactory.Enumerate().FirstOrDefault((c) => c.LeverEnabled);
        *pos = controller?.LeverPosition ?? short.MaxValue / 2; // fallback to center
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_led_init")]
    public static unsafe int InitLeds()
    {
        if (!ControllerFactory.Enumerate().Any())
        {
            pipeClientWriter?.WriteLine("InitLeds");
        }
        else
        {
            InternalInitLeds();
        }

        return HRESULT.S_OK;
    }

    private static int InternalInitLeds()
    {
        Logger.Debug($"IO: [InternalInitLeds] => Initializing leds...");

        foreach (var controller in ControllerFactory.Enumerate())
        {
            if (!controller.InitLeds())
                Logger.Debug($"IO: [mu3_io_led_init] => ({controller.GetType().Name}) Failed to init the leds");
        }

        return HRESULT.S_OK;
    }

    [UnmanagedCallersOnly(EntryPoint = "mu3_io_led_set_colors")]
    public static unsafe int SetLeds(byte board, byte* rgb)
    {
        int ledsCount = (board == 0 ? 61 : 6);
        byte[] ledsColors = Enumerable.Range(0, ledsCount * 3)
            .Select(i => *(rgb + i))
            .ToArray();

        if (!ControllerFactory.Enumerate().Any())
        {
            //No controller found to set the leds color so we forward it to the named pipe client...

            string payloadArrayString = $"{board}," + string.Join(",", ledsColors.Select(color => color.ToString()));
            pipeClientWriter?.WriteLine(payloadArrayString);
        }
        else
        {
            foreach (var controller in ControllerFactory.Enumerate())
            {
                if (!controller.SetLeds(board, ledsColors))
                    Logger.Debug($"io: [mu3_io_led_set_colors] => ({controller.GetType().Name}) failed to set the leds color");
            }
        }

        return HRESULT.S_OK;
    }

    public static unsafe int SetLeds(String payloadString)
    {
        //Parse the payload to get the board and the RGB values
        
        string[] splittedPayloadString = payloadString.Split(',');
        byte[] payloadBytes = Array.ConvertAll(splittedPayloadString, byte.Parse);
        int board = payloadBytes[0];
        byte[] ledsColors = payloadBytes.Skip(1).ToArray();

        foreach (var controller in ControllerFactory.Enumerate())
        {
            if (!controller.SetLeds(board, ledsColors))
                Logger.Debug($"io: [mu3_io_led_set_colors] => ({controller.GetType().Name}) failed to set the leds color");
        }

        return HRESULT.S_OK;
    }
}