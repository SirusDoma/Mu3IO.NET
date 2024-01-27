# Mu3IO.NET

[Segatools](https://github.com/djhackersdev/segatools) Io4 Module enhancement for Mu3 (SDDT).  
Written entirely in C# compiled with [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows) and heavily relied on [CsWin32](https://github.com/microsoft/CsWin32) to make OS-level API calls.

## List of supported controllers

This list is ordered based on the input polling priority:
1. [Ontroller](https://www.dj-dao.com/en/ontroller)
2. XInput Controller
3. Keyboard & Mouse (Lever can be configured separately)

## Installation

1. Get and set up the game data.
2. Download and configure the [Segatools](https://github.com/djhackersdev/segatools).
    * Compatible with `mu3hook.dll` that comes from vanilla [segatools](https://github.com/djhackersdev/segatools) and other common forks. However, the leds won't work unless write GPIO is implemented and forwarded to `mu3_io_set_leds(uint8_t *rgb)` with correct led mapping values.
    * Incompatible with `mu3hook.dll` from [ontroller-hook](https://gitea.tendokyu.moe/phantomlan/ontroller-hook) or other IO/hook that modifies [mu3_io4_poll](https://github.com/djhackersdev/segatools/blob/ca9c72db968c81fdf88ba01f9b4a474bf818e401/mu3hook/io4.c#L34) button flags and lever behavior at the hook level.
3. [Download](https://github.com/SirusDoma/Mu3IO.NET/releases/latest) or Build this project and copy it to the game data directory.
    * Copying `mu3hook.dll` to the game directory is optional. See the notes above.
4. Configure `segatools.ini` to include the following config (For more information, see [Configuration](#configuration)).
    ```
    [mu3io]
    path=mu3io.dll
    ```
5. Run the game with `mu3hook.dll` injected into the `amdaemon.exe` or other loaders that the game uses.
6. Once the game booted, enter operator/test menu and then recalibrate your lever (Normally, the range should be between `0000H` and `8000H`)

## Configuration

Ensure you include `path=mu3io.dll` under the `[mu3io]` section in your `segatools.ini`. 
In addition, new input modes and configurations are available under the `[io4]` section.  

Use the following template to configure the Mu3IO.NET:  
**(Warning: Do not replace the entire of your segatools.ini content with this template)**

```
; IMPORTANT: Required to load Mu3IO.NET
[mu3io]
path=mu3io.dll

[io4]
; Input modes
; Now all the generic controllers can be activated and deactivated individually with the following priority:
; Ontroller -> XInput -> Mouse (lever only) -> Keyboard.
; All input are enabled by default, set 0 to disable the input.
xinput=1    ; Enable buttons and lever emulation with XInput if connected. Otherwise fallback to Mouse for Lever and Keyboard for Buttons.
mouse=1     ; Enable lever emulation with mouse. Disable to move the lever with keyboard.
keyboard=1  ; Enable buttons and lever emulation with keyboard. If mouse emulation is enabled, lever from keyboard will be ignored.

; Keyboard & Mouse bindings
; See the following link for complete list of virtual keycodes:
; https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
test=0x31	; 1
service=0x32	; 2
coin=0x33	; 3
left1=0x4	; A
left2=0x53	; S
left3=0x44	; D
leftSide=0x01	; Mouse Left
rightSide=0x02	; Mouse Right
right1=0x4A	; J
right1=0x4B	; K
right3=0x4C	; L
leftMenu=0x55	; U
rightMenu=0x4F	; O

; New input exposed to move the lever via keyboard
leverLeft=0xA4	; Left ALT
leverRight=0xA5	; Right ALT
```

## Build instructions
This project is compatible with both Visual Studio and Rider. `publish` the project to generate native `mu3io.dll`. 
**DO NOT** use or distribute the managed dll produced by building the project/solution. It won't work!

The `mu3hook.dll` is part of the segatools fork that can be found in here:  
https://github.com/SirusDoma/segatools/tree/mu3-gpio.

## Supporting other dedicated controllers

You must implement `IController` interface and register your controller class into `ControllerFactory` inside `Mu3IO.Initialize()` method.
For dedicated controllers, the IO Module will automatically monitor USB insertion and disconnection once registered into `ControllerFactory`, allowing the users to reconnect their controller at any point of time.  

Additionally, you can implement the `WinUsb`, which provides out-of-the-box `WinUsb` initialization and simple IO read and write functions to implement.

> [!important]
> This project and all dependent NuGet / 3rd party libraries are subject to [Native AOT limitation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#limitations-of-native-aot-deployment).

## Credits

Special thanks to [DJZMO](https://github.com/djzmo) for extensive testing and debugging assistance.

## License

[No License / Public Domain](https://github.com/SirusDoma/Mu3IO.NET/blob/master/LICENSE)
