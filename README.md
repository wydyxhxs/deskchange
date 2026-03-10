# DeskChange

DeskChange is a portable Windows virtual desktop manager with a compact Chinese UI. It provides configurable hotkeys, desktop create/delete actions, startup options, and optional switch animation control.

## Features

- Configurable hotkeys for desktops 1-4
- Create and delete virtual desktops from the main window
- Toggle switch animation on or off
- Optional startup with hidden tray launch
- Portable build and installer build

## Requirements

- Windows 11
- .NET Framework 4.8

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Build outputs are generated under `artifacts/`.

## Third-party component

This project bundles the open-source virtual desktop helper by Markus Scholtes under the MIT License.
See `vendor/LICENSE.MScholtes.txt` for details.
