using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.XboxController;
using static Windows.Win32.PInvoke;

namespace Mu3IO;

public class XInput : IController
{
    private bool _enabled, _xinputAnalogLever;

    public XInput()
    {
        _enabled = GetPrivateProfileInt("io4", "xinput", '1', Mu3IO.ConfigFileName) != 0;
        _xinputAnalogLever = GetPrivateProfileInt("io4", "xinputAnalogLever", 0, Mu3IO.ConfigFileName) != 0;
        if (_enabled)
        {
            var errorCode = (WIN32_ERROR)XInputGetState(0, out _);
            if (errorCode == WIN32_ERROR.ERROR_SUCCESS)
                Logger.Debug($"{GetType().Name}: Connected!");
            else if (errorCode == WIN32_ERROR.ERROR_DEVICE_NOT_CONNECTED)
                Logger.Debug($"{GetType().Name}: Device is not connected");
            else
                Logger.Debug($"{GetType().Name}: Failed to connect with the gamepad ({errorCode})");
        }
    }

    private void DigitalLever(XINPUT_STATE state)
    {
        if (Math.Abs(state.Gamepad.sThumbLX) > (short)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE)
        {
            LeverPosition += (short)(state.Gamepad.sThumbLX / 24);
        }

        if (Math.Abs(state.Gamepad.sThumbRX) > (short)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE)
        {
            LeverPosition += (short)(state.Gamepad.sThumbRX / 24);
        }

        LeverPosition -= (short)(state.Gamepad.bLeftTrigger * 64 + state.Gamepad.bRightTrigger * 64);
    }

    private void AnalogLever(XINPUT_STATE state)
    {
        LeverPosition = state.Gamepad.sThumbLX;
    }

    public bool Poll()
    {
        if (!_enabled)
            return true;

        // Get input state
        var errorCode = (WIN32_ERROR)XInputGetState(0, out var state);

        // Disable lever if device is not present
        LeverEnabled = errorCode == WIN32_ERROR.ERROR_SUCCESS;

        // Check for errors
        if (errorCode != WIN32_ERROR.ERROR_SUCCESS)
        {
            if (errorCode != WIN32_ERROR.ERROR_DEVICE_NOT_CONNECTED)
                Logger.Debug($"XInput: Failed to pool the input data ({errorCode})");

            return errorCode == WIN32_ERROR.ERROR_DEVICE_NOT_CONNECTED;
        }

        // Get the state of buttons in flag
        ushort buttonsFlag = (ushort)state.Gamepad.wButtons;

        // Clear states
        OptionButtonsFlag = 0;
        LeftGameButtonsFlag = 0;
        RightGameButtonsFlag = 0;

        // Map button flag and lever into IO
        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_DPAD_LEFT) > 0)
            LeftGameButtonsFlag |= 0x01;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_DPAD_UP) > 0)
            LeftGameButtonsFlag |= 0x02;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_DPAD_RIGHT) > 0)
            LeftGameButtonsFlag |= 0x04;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_X) > 0)
            RightGameButtonsFlag |= 0x01;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_Y) > 0)
            RightGameButtonsFlag |= 0x02;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_B) > 0)
            RightGameButtonsFlag |= 0x04;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_BACK) > 0)
            LeftGameButtonsFlag |= 0x10;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_START) > 0)
            RightGameButtonsFlag |= 0x10;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_LEFT_SHOULDER) > 0)
            LeftGameButtonsFlag |= 0x08;

        if ((buttonsFlag & (ushort)XINPUT_GAMEPAD_BUTTON_FLAGS.XINPUT_GAMEPAD_RIGHT_SHOULDER) > 0)
            RightGameButtonsFlag |= 0x08;


        if (!_xinputAnalogLever)
            DigitalLever(state);
        else
            AnalogLever(state);

        return true;
    }

    public byte OptionButtonsFlag { get; private set; }
    public byte LeftGameButtonsFlag { get; private set; }
    public byte RightGameButtonsFlag { get; private set; }
    public short LeverPosition { get; private set; }
    public bool LeverEnabled { get; private set; }

    public unsafe bool SetLeds(byte* payload)
    {
        // No-Op
        return true;
    }
}