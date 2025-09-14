# NetKit

A tiny Windows tray app for quick network checks. It runs in the system tray and pops open with a global hotkey.

## Features
- HTTP GET/POST with headers and body
- SSL/TLS certificate details and expiry
- Redirect chain viewer
- DNS lookups (A, AAAA, CNAME, MX, TXT, NS, PTR)

## Download & Run
- Windows 10/11.
- Download the latest Release ZIP, extract, and run `NetKit.exe`.
- A tray icon appears. Press `Win + ' (apostrophe)` to show/hide the window or double click the tray icon. 
- To stop the app just right-click the tray icon and click on Close.

## Notes
- Settings are saved to `%AppData%\\NetKit\\settings.json`.

## Build From Source
- Prerequisite: .NET 8 SDK.
- Restore/build: `dotnet restore` then `dotnet build -c Release`.

## Optional single-file build (can be larger and slower to start for WPF):
  - `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`

  ##