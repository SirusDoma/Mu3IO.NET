using System.Runtime.InteropServices;

using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using static Windows.Win32.PInvoke;

namespace Mu3IO;

public class Mouse : IController
{
    public Mouse()
    {
        LeverEnabled = GetPrivateProfileInt("io4", "mouse", '1', Mu3IO.ConfigFileName) != 0;
        if (LeverEnabled)
            Logger.Debug($"{GetType().Name}: Configuration loaded!");
    }

    public bool Poll()
    {
        if (!LeverEnabled)
            return true;

        if (!GetCursorPos(out var point))
        {
            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            Logger.Debug($"Mouse: Failed to pool the mouse position ({errorCode})");

            return false;
        }

        int position = point.X;
        int width = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        if (width <= 0)
        {
            var errorCode = (WIN32_ERROR)Marshal.GetLastWin32Error();
            Logger.Debug($"Mouse: Failed to retrieve the primary screen width ({errorCode})");

            return false;
        }

        position         = Math.Clamp(position, 0, width);
        float normalized = (float)position / width;
        LeverPosition    = (short)(normalized * short.MaxValue);

        return true;
    }

    public byte OptionButtonsFlag => 0;
    public byte LeftGameButtonsFlag => 0;
    public byte RightGameButtonsFlag => 0;
    public short LeverPosition { get; private set; }
    public bool LeverEnabled { get; }

    public bool InitLeds()
    {
        // No-Op
        return true;
    }

    public bool SetLeds(int board, byte[] ledsColors)
    {
        // No-Op
        return true;
    }
}