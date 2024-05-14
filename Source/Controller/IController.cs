namespace Mu3IO;

public interface IController
{
    bool LeverEnabled => true;

    bool Poll();

    byte OptionButtonsFlag { get; }

    byte LeftGameButtonsFlag { get; }

    byte RightGameButtonsFlag { get; }

    short LeverPosition { get; }

    bool InitLeds();

    unsafe bool SetLeds(byte board, byte* rgb); // Keep pointer to avoid copy
}