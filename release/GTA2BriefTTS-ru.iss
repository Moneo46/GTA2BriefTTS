
#define MyAppName "GTA2 Brief TTS (Russian)"
#define MyAppVersion "1.0.0"
#define StageDir "H:/Games/gta2/brief_tts/release/dist/GTA2BriefTTS-1.0.0-ru"
#define OutDir "H:/Games/gta2/brief_tts/release/dist"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\GTA2 Brief TTS (RU)
DefaultGroupName={#MyAppName}
OutputBaseFilename=GTA2BriefTTS-1.0.0-ru-setup
OutputDir={#OutDir}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
SetupIconFile="H:/Games/gta2/brief_tts/release/app.ico"
UninstallDisplayIcon={app}\GTA2BriefTTS.exe

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\GTA2 Brief TTS"; Filename: "{app}\GTA2BriefTTS.exe"
Name: "{autodesktop}\GTA2 Brief TTS"; Filename: "{app}\GTA2BriefTTS.exe"

[Run]
Filename: "{app}\README_RELEASE.txt"; Description: "Read me"; Flags: postinstall shellexec skipifsilent
#include "inno_game_folder_code.iss"
