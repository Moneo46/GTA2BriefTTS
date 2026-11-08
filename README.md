# GTA2 Brief TTS — user client (C#)

Windows GUI companion for **GTA 2** that plays pre-recorded mission brief voices, adjusts HUD subtitle language, and tweaks in-game sound levels while the game is running.

This repository contains the **C# WinForms client** only. Release packages (voice packs, Frida agent, baked cache, installers) are distributed separately.

## Download (pre-built releases)

| Package | Description |
|--------|-------------|
| [GTA2BriefTTS-1.0.0-ru-win64.zip](https://drive.google.com/file/d/1piEXQ66I9oy0oV3QH27TQ2hSvsvGtP2B/view?usp=drive_link) | Russian brief voices — portable ZIP |
| [GTA2BriefTTS-1.0.0-en-win64.zip](https://drive.google.com/file/d/1L3FhP2vA2Jj2Hb2jOpQVZV9ydUlATODw/view?usp=drive_link) | English brief voices — portable ZIP |
| [GTA2BriefTTS-1.0.0-en-setup.exe](https://drive.google.com/file/d/16qs5MmMdBWo7-xEwH7HGRUUN8F-Ef05n/view?usp=drive_link) | English voices — Windows installer |

> **Note:** Google Drive may ask you to sign in. Use *Download anyway* if your browser warns about virus scan limits on large files.

## Installation (end users)

### Option A — installer (English pack)

1. Download [GTA2BriefTTS-1.0.0-en-setup.exe](https://drive.google.com/file/d/16qs5MmMdBWo7-xEwH7HGRUUN8F-Ef05n/view?usp=drive_link).
2. Run the installer and choose the folder that contains `GTA2.exe` (you can change it later in the app).
3. Start **GTA2 Brief TTS** from the Start menu shortcut.

### Option B — portable ZIP (Russian or English)

1. Download the ZIP for your language (links in the table above).
2. Extract to any folder (e.g. `C:\Games\GTA2BriefTTS`).
3. Launch `GTA2BriefTTS.exe`.
4. Set the game directory to the folder that contains `GTA2.exe` (use **Browse…**), then click **Save**.

### Requirements

- **Windows 10** or newer (64-bit)
- A valid **GTA 2** installation with `GTA2.exe`
- No Internet connection required for brief playback (voices are included in the package)

### How to use

1. Open **GTA2 Brief TTS**.
2. Confirm the game folder path.
3. Set HUD subtitle language, brief voice language, and volumes if needed.
4. Click **Save**, then **Start** — the app launches GTA 2 when needed and plays mission brief audio.
5. Click **Stop** or close the window to end the session.

You can set the `GTA2_ROOT` environment variable instead of choosing the folder in the app.

### What ships in a release package (not in this repo)

| File / folder | Role |
|---------------|------|
| `GTA2BriefTTS.exe` | This client (built from `GTA2BriefTTS.User/`) |
| `config.json` | Game path, languages, volumes |
| `pack_data.dat` | Voice/brief tables |
| `frida_agent.js`, `addresses.json`, `brief_signatures.json` | Game hooks |
| `tts_cache/<lang>/` | Pre-baked MP3 brief cache |

## Build from source (developers)

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download) on Windows.

```powershell
cd GTA2BriefTTS.User
dotnet publish GTA2BriefTTS.User.csproj -c Release -r win-x64 --self-contained false
```

Output is under `bin\Release\net9.0-windows\win-x64\publish\`. A full user install still needs the release assets (`pack_data.dat`, `tts_cache`, Frida files) from a voice pack ZIP above.

## License

Private repository — rights reserved unless stated otherwise by the owner.
