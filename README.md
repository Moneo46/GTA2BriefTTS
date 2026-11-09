# GTA2 Brief TTS

**GTA2 Brief TTS does not modify any game files.** It only **reads** your existing GTA 2 installation (for example GXT text and paths you point it at). Settings are stored in this tool’s own folder (`config.json`); nothing is written into the game directory.

Windows companion for **GTA 2** that plays pre-recorded mission brief voices, sets HUD subtitle language, and adjusts in-game sound levels while you play.

This repository contains the **end-user client** source (C#). Pre-built ZIPs and installers (with voices and packaging assets) are on Google Drive below.

## Download (pre-built releases)

| Package | Description |
|--------|-------------|
| [GTA2BriefTTS-1.0.0-en-setup.exe](https://drive.google.com/file/d/16qs5MmMdBWo7-xEwH7HGRUUN8F-Ef05n/view?usp=drive_link) | English voices — Windows installer |
| [GTA2BriefTTS-1.0.0-en-win64.zip](https://drive.google.com/file/d/1L3FhP2vA2Jj2Hb2jOpQVZV9ydUlATODw/view?usp=drive_link) | English voices — portable ZIP |
| [GTA2BriefTTS-1.0.0-ru-setup.exe](https://drive.google.com/file/d/1c326U2B1L_eq-hQQADB1FA41Yi9-br_L/view?usp=drive_link) | Russian voices — Windows installer |
| [GTA2BriefTTS-1.0.0-ru-win64.zip](https://drive.google.com/file/d/1piEXQ66I9oy0oV3QH27TQ2hSvsvGtP2B/view?usp=drive_link) | Russian voices — portable ZIP |

> Google Drive may ask you to sign in. If the browser warns about scan limits on large files, choose **Download anyway**.

## Installation (end users)

### Option A — installer (English pack)

1. Download [GTA2BriefTTS-1.0.0-en-setup.exe](https://drive.google.com/file/d/16qs5MmMdBWo7-xEwH7HGRUUN8F-Ef05n/view?usp=drive_link).
2. Run the installer and choose the folder that contains `GTA2.exe` (you can change it later in the app).
3. Start **GTA2 Brief TTS** from the Start menu shortcut.

### Option B — portable ZIP (Russian or English)

1. Download the ZIP for your language (table above).
2. Extract to any folder (for example `C:\Games\GTA2BriefTTS`).
3. Run `GTA2BriefTTS.exe`.
4. Set the game directory to the folder that contains `GTA2.exe` (**Browse…**), then **Save**.

### Requirements

- **Windows 10** or newer (64-bit)
- A **GTA 2** install with `GTA2.exe`
- No Internet required for brief playback (voices are in the package)

### How to use

1. Open **GTA2 Brief TTS**.
2. Confirm the game folder path.
3. Set HUD subtitle language, brief voice language, and volumes if needed.
4. Click **Save**, then **Start** — the app starts GTA 2 when needed and plays mission brief audio.
5. Click **Stop** or close the window to end the session.

You can set the `GTA2_ROOT` environment variable instead of choosing the folder in the app.

## Repository layout

| Path | Purpose |
|------|---------|
| `user_client/GTA2BriefTTS.User/` | C# WinForms source |

Runtime files (`config.json`, `*.js`, Inno/`release/` output, `tts_cache/`, installers) are **not** in Git — use the Google Drive builds above.

## Build from source (maintainers)

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download) on Windows.

```powershell
dotnet publish user_client\GTA2BriefTTS.User\GTA2BriefTTS.User.csproj -c Release -r win-x64 --self-contained true
```

Publish output is under `user_client\GTA2BriefTTS.User\bin\Release\net9.0-windows\win-x64\publish\`. A playable install still needs the assets from a Google Drive package above.

## Support the project

[![Support on Boosty](https://img.shields.io/badge/Boosty-Support%20on%20Boosty-EE8208?style=flat-square)](https://boosty.to/mon_studio)

Releases stay free on GitHub and Google Drive. If you want to help fund better voices, tooling, and the next fan projects, subscribe on **[Mon studio @ Boosty](https://boosty.to/mon_studio)** — development news, early access, and votes on what we tackle next.

**Поддержка:** [boosty.to/mon_studio](https://boosty.to/mon_studio) — новости разработки, ранний доступ и голосования по проектам.

## License

Private repository — rights reserved unless stated otherwise by the owner.
