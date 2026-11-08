# GTA2 Brief TTS

**GTA2 Brief TTS does not modify any game files.** It only **reads** your existing GTA 2 installation (for example GXT text and paths you point it at). Settings are stored in this tool’s own folder (`config.json`); nothing is written into the game directory.

Windows companion for **GTA 2** that plays pre-recorded mission brief voices, sets HUD subtitle language, and adjusts in-game sound levels while you play.

This repository contains the **end-user client**: the C# app, runtime data files shipped beside the executable, and scripts to build language packs. Pre-built ZIPs and installers (with `tts_cache` and `pack_data.dat`) are on Google Drive below.

## Download (pre-built releases)

| Package | Description |
|--------|-------------|
| [GTA2BriefTTS-1.0.0-ru-win64.zip](https://drive.google.com/file/d/1piEXQ66I9oy0oV3QH27TQ2hSvsvGtP2B/view?usp=drive_link) | Russian brief voices — portable ZIP |
| [GTA2BriefTTS-1.0.0-en-win64.zip](https://drive.google.com/file/d/1L3FhP2vA2Jj2Hb2jOpQVZV9ydUlATODw/view?usp=drive_link) | English brief voices — portable ZIP |
| [GTA2BriefTTS-1.0.0-en-setup.exe](https://drive.google.com/file/d/16qs5MmMdBWo7-xEwH7HGRUUN8F-Ef05n/view?usp=drive_link) | English voices — Windows installer |

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
| `config.json` | Default/template settings (user installs get a trimmed copy) |
| `addresses.json`, `brief_signatures.json`, `frida_agent.js` | Supporting files shipped beside the published `.exe` (read-only at runtime) |
| `pack_data.py`, `build_release.py` | Build `pack_data.dat` and user ZIP/installer |
| `release/` | Inno Setup scripts and user-facing `README_USER_*.txt` |
| `README.txt` | Short notes copied into release folders |

Large assets (`tts_cache/`, built `.exe`, installers) are **not** in Git — use the Google Drive builds above.

## Build from source (maintainers)

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download), Python 3.11+ for packaging.

1. Publish the client:

```powershell
dotnet publish user_client\GTA2BriefTTS.User\GTA2BriefTTS.User.csproj -c Release -r win-x64 --self-contained true -o release\build\csharp-user-publish
```

2. Build a language pack (needs `tts_cache\<lang>\` from a local bake — not included in this repo):

```powershell
python build_release.py --user-pack ru en --version 1.0.0 --installer --skip-exe
```

Output: `release/dist/GTA2BriefTTS-<version>-<lang>/` and optional setup executables when Inno Setup 6 is installed.

## License

Private repository — rights reserved unless stated otherwise by the owner.
