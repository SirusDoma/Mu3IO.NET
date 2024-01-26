namespace Mu3IO;

public interface IController
{
    bool LeverEnabled => true;

    bool Poll();

    byte OptionButtonsFlag { get; }

    byte LeftGameButtonsFlag { get; }

    byte RightGameButtonsFlag { get; }

    short LeverPosition { get; }

    unsafe bool SetLeds(byte* payload); // Keep pointer to avoid copy
}