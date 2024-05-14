using Windows.Win32;

using static Windows.Win32.PInvoke;

namespace Mu3IO;

public class Keyboard : IController
{
    private readonly int _test, _coin, _service;
    private readonly int _left1, _left2, _left3, _leftSide;
    private readonly int _right1, _right2, _right3, _rightSide;
    private readonly int _leftMenu, _rightMenu;
    private readonly int _leverLeft, _leverRight;

    private readonly bool _enabled;
    private bool _coinEnabled = true;

    public Keyboard()
    {
        _enabled = LeverEnabled = GetPrivateProfileInt("io4", "keyboard", '1', Mu3IO.ConfigFileName) != 0;
        _test = GetPrivateProfileInt("io4", "test", '1', Mu3IO.ConfigFileName);
        _service = GetPrivateProfileInt("io4", "service", '2', Mu3IO.ConfigFileName);
        _coin = GetPrivateProfileInt("io4", "coin", '3', Mu3IO.ConfigFileName);
        _left1 = GetPrivateProfileInt("io4", "left1", 'A', Mu3IO.ConfigFileName);
        _left2 = GetPrivateProfileInt("io4", "left2", 'S', Mu3IO.ConfigFileName);
        _left3 = GetPrivateProfileInt("io4", "left3", 'D', Mu3IO.ConfigFileName);
        _leftSide = GetPrivateProfileInt("io4", "leftSide", 'Q', Mu3IO.ConfigFileName);
        _rightSide = GetPrivateProfileInt("io4", "rightSide", 'E', Mu3IO.ConfigFileName);
        _right1 = GetPrivateProfileInt("io4", "right1", 'J', Mu3IO.ConfigFileName);
        _right2 = GetPrivateProfileInt("io4", "right2", 'K', Mu3IO.ConfigFileName);
        _right3 = GetPrivateProfileInt("io4", "right3", 'L', Mu3IO.ConfigFileName);
        _leftMenu = GetPrivateProfileInt("io4", "leftMenu", 'U', Mu3IO.ConfigFileName);
        _rightMenu = GetPrivateProfileInt("io4", "rightMenu", 'O', Mu3IO.ConfigFileName);
        _leverLeft = GetPrivateProfileInt("io4", "leverLeft", 0xA4, Mu3IO.ConfigFileName);
        _leverRight = GetPrivateProfileInt("io4", "leverRight", 0xA5, Mu3IO.ConfigFileName);

        LeverPosition = short.MaxValue/ 2;

        if (_enabled)
            Logger.Debug($"{GetType().Name}: Configuration loaded!");
    }

    public bool LeverEnabled { get; private set; }

    public bool Poll()
    {
        if (!_enabled)
            return true;

        OptionButtonsFlag = 0;
        LeftGameButtonsFlag = 0;
        RightGameButtonsFlag = 0;

        if ((GetAsyncKeyState(_test) & 0x8000) > 0)
            OptionButtonsFlag |= 0x01;

        if ((GetAsyncKeyState(_service) & 0x8000) > 0)
            OptionButtonsFlag |= 0x02;

        if ((GetAsyncKeyState(_coin) & 0x8000) > 0)
        {
            _coinEnabled = !_coinEnabled;
            OptionButtonsFlag |= 0x04;
        }

        if ((GetAsyncKeyState(_left1) & 0x8000) > 0)
            LeftGameButtonsFlag |= 0x01;

        if ((GetAsyncKeyState(_left2) & 0x8000) > 0)
            LeftGameButtonsFlag |= 0x02;

        if ((GetAsyncKeyState(_left3) & 0x8000) > 0)
            LeftGameButtonsFlag |= 0x04;

        if ((GetAsyncKeyState(_right1) & 0x8000) > 0)
            RightGameButtonsFlag |= 0x01;

        if ((GetAsyncKeyState(_right2) & 0x8000) > 0)
            RightGameButtonsFlag |= 0x02;

        if ((GetAsyncKeyState(_right3) & 0x8000) > 0)
            RightGameButtonsFlag |= 0x04;

        if ((GetAsyncKeyState(_leftMenu) & 0x8000) > 0)
            LeftGameButtonsFlag |= 0x10;

        if ((GetAsyncKeyState(_rightMenu) & 0x8000) > 0)
            RightGameButtonsFlag |= 0x10;

        if ((GetAsyncKeyState(_leftSide) & 0x8000) > 0)
            LeftGameButtonsFlag |= 0x08;

        if ((GetAsyncKeyState(_rightSide) & 0x8000) > 0)
            RightGameButtonsFlag |= 0x08;

        if ((GetAsyncKeyState(_leverLeft) & 0x8000) > 0)
        {
            if (LeverPosition - 0xFF > 0)
                LeverPosition -= 0x1FF;
        }

        if ((GetAsyncKeyState(_leverRight) & 0x8000) > 0)
        {
            if (LeverPosition + 0xFF < short.MaxValue)
                LeverPosition += 0x1FF;
        }

        return true;
    }

    public byte OptionButtonsFlag { get; private set; }
    public byte LeftGameButtonsFlag { get; private set; }
    public byte RightGameButtonsFlag { get; private set; }
    public short LeverPosition { get; private set; }

    public bool InitLeds()
    {
        // No-Op
        return true;
    }

    public unsafe bool SetLeds(byte board, byte* rgb)
    {
        // No-Op
        return true;
    }
}