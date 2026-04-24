# DimmerBeyond (RGsScreenDimmer)

DimmerBeyond is a Windows Forms screen dimmer for Windows that overlays black, click-through forms on each connected display and lets you control per-display dimming from the system tray.

## Features

- Per-display opacity control from tray menu
- Per-display enable and disable toggle
- Multi-monitor support with live display change detection
- Persisted settings per physical display device name
- Single-instance app behavior (mutex)

## Tech Stack

- .NET 8
- Windows Forms
- C#

## How It Works

1. On startup, the app loads cached screen settings.
2. It discovers currently connected screens.
3. It creates or syncs one overlay form per active screen.
4. It creates tray controls for each active display.
5. On slider or checkbox changes, in-memory settings update and are saved when tray closes.
6. On display topology changes, overlays and tray controls are refreshed.

## Settings Persistence

Settings are stored at:

`C:\ProgramData\RGAppData\DimmerBeyond\DimmerCache.json`

Cached values are keyed by Windows screen `DeviceName`, and each entry stores:

- `OpacityPercent` (0 to 80)
- `Enabled` (true or false)

Notes:

- If a display is disconnected, its cached settings are retained.
- If a new display is detected, it starts with fallback opacity from `Constants.DefaultOpacity` and `Enabled = true`.

## Build and Run

From repository root:

```bash
dotnet build RGsScreenDimmer.sln
```

From project folder:

```bash
cd DimmerBeyond
dotnet run
```

## Publish (Example)

```bash
cd DimmerBeyond
dotnet publish -c Release -r win-x86 --self-contained false
```

## Tray Usage

- Left-click tray icon to open controls
- For each display:
  - Use checkbox to enable or disable dimming
  - Use slider to set opacity
- Select Exit to close the app

## Constraints and Behavior

- App is single-instance only.
- Opacity slider range is intentionally capped at 80.
- Overlay forms are top-most, borderless, and click-through.

## Troubleshooting

- If settings do not seem updated, close the tray menu once after changes to force persistence.
- If monitor arrangement changes, wait briefly for Windows to raise display update events and for UI refresh to complete.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
