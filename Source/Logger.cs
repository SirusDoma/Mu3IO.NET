using static Windows.Win32.PInvoke;

namespace Mu3IO;

public static class Logger
{
    public static void Debug(string message)
    {
        message = $"[mu3io.net] {message}\n";
        OutputDebugString(message);
        Console.Write(message);
    }
}