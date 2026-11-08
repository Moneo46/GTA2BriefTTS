#!/usr/bin/env python3
"""
Build Windows release folder (PyInstaller onedir) + optional ZIP and Inno Setup installer.

Usage (from brief_tts, in a **clean** venv — only requirements-release.txt):
  python -m venv .venv-release
  .venv-release\\Scripts\\pip install -r requirements-release.txt
  .venv-release\\Scripts\\python build_release.py

Do not build from a global Python with torch/librosa installed; PyInstaller may bundle them.
  .venv-release\\Scripts\\python build_release.py --with-cache --version 1.0.0
  .venv-release\\Scripts\\python build_release.py --installer

User installers (C# cache-only client, pre-baked tts_cache, ru/en separate):
  python bake_briefs.py --lang ru
  python bake_briefs.py --lang en
  python build_release.py --user-pack ru en --version 1.0.0 --installer

Dev full release (Python: inject, bake, record_session) — omit --user-pack:
  python build_release.py --version 1.0.0

Output:
  release/dist/GTA2BriefTTS-<version>/
  release/dist/GTA2BriefTTS-<version>-win64.zip
  release/dist/GTA2BriefTTS-<version>-setup.exe  (if Inno Setup is installed)
  release/dist/GTA2BriefTTS-<version>-ru/  (+ zip + setup with --user-pack)
  release/dist/GTA2BriefTTS-<version>-en/
"""
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

from pack_data import strip_user_config, write_pack_data

DIR = Path(__file__).resolve().parent
RELEASE_DIR = DIR / "release"
BUILD_WORK = RELEASE_DIR / "build"
DIST_ROOT = RELEASE_DIR / "dist"

DATA_FILES = (
    "config.json",
    "frida_agent.js",
    "addresses.json",
    "brief_signatures.json",
    "README.txt",
)

OPTIONAL_DATA = ("addresses.cache.json", "VOICE_OPTIONS.md")

EXES = (
    ("inject_runner.py", "GTA2BriefTTS"),
    ("bake_briefs.py", "BakeBriefs"),
    ("speak_hotkey.py", "SpeakHotkeyF12"),
)

USER_CLIENT_PROJ = DIR / "user_client" / "GTA2BriefTTS.User" / "GTA2BriefTTS.User.csproj"
USER_CSHARP_PUBLISH = BUILD_WORK / "csharp-user-publish"

USER_LANG_TITLES = {
    "ru": "GTA2 Brief TTS (Russian)",
    "en": "GTA2 Brief TTS (English)",
}

# PyInstaller must not bundle optional ML stacks (librosa lazy_loader used to pull ~500MB torch).
PYINSTALLER_EXCLUDES = [
    "librosa",
    "torch",
    "torchvision",
    "torchaudio",
    "tensorflow",
    "sklearn",
    "scipy",
    "pandas",
    "matplotlib",
    "pyarrow",
    "onnxruntime",
    "boto3",
    "botocore",
    "numba",
    "llvmlite",
    "transformers",
    "IPython",
    "notebook",
    "jupyter",
]


def run(cmd: list[str], cwd: Path | None = None) -> None:
    print("+", " ".join(cmd))
    subprocess.run(cmd, cwd=cwd or DIR, check=True)


def ensure_pyinstaller() -> None:
    try:
        import PyInstaller  # noqa: F401
    except ImportError:
        run([sys.executable, "-m", "pip", "install", "pyinstaller>=6.0"])


def write_spec(entry: str, name: str, spec_path: Path) -> None:
    hidden = [
        "frida",
        "frida._frida",
        "edge_tts",
        "pygame",
        "asyncio",
        "certifi",
    ]
    hidden_repr = repr(hidden)
    excludes_repr = repr(PYINSTALLER_EXCLUDES)
    collect_frida = entry == "inject_runner.py"
    if collect_frida:
        frida_block = """
_frida_datas, _frida_binaries, _frida_hidden = collect_all("frida")
_hiddenimports = {hidden} + list(_frida_hidden)
_binaries = _frida_binaries
_datas = _frida_datas
""".format(hidden=hidden_repr)
    else:
        frida_block = f"""
_hiddenimports = {hidden_repr}
_binaries = []
_datas = []
"""
    spec = f'''# -*- mode: python ; coding: utf-8 -*-
from PyInstaller.utils.hooks import collect_all

block_cipher = None
{frida_block}
a = Analysis(
    [r"{(DIR / entry).as_posix()}"],
    pathex=[r"{DIR.as_posix()}"],
    binaries=_binaries,
    datas=_datas,
    hiddenimports=_hiddenimports,
    hookspath=[],
    hooksconfig={{}},
    runtime_hooks=[],
    excludes={excludes_repr},
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)
pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)
exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name="{name}",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=False,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=False,
    upx_exclude=[],
    name="{name}",
)
'''
    spec_path.write_text(spec, encoding="utf-8")


def build_exe(entry: str, name: str) -> Path:
    spec_path = BUILD_WORK / f"{name}.spec"
    write_spec(entry, name, spec_path)
    run(
        [
            sys.executable,
            "-m",
            "PyInstaller",
            "--noconfirm",
            "--clean",
            "--distpath",
            str(BUILD_WORK / "dist"),
            "--workpath",
            str(BUILD_WORK / "work" / name),
            str(spec_path),
        ]
    )
    return BUILD_WORK / "dist" / name


def merge_onedir(primary: Path, extra: Path, dest_exe_name: str) -> None:
    """Copy extra exe into primary onedir folder."""
    src_exe = extra / f"{extra.name}.exe"
    if src_exe.is_file():
        shutil.copy2(src_exe, primary / f"{dest_exe_name}.exe")


def copy_data_files(target: Path, with_cache: bool) -> None:
    for name in DATA_FILES:
        src = DIR / name
        if src.is_file():
            shutil.copy2(src, target / name)
    for name in OPTIONAL_DATA:
        src = DIR / name
        if src.is_file():
            shutil.copy2(src, target / name)
    if with_cache:
        cache = DIR / "tts_cache"
        if cache.is_dir():
            shutil.copytree(cache, target / "tts_cache", dirs_exist_ok=True)
        else:
            print("Warning: --with-cache but tts_cache/ is missing", file=sys.stderr)


def copy_cache_lang(target: Path, lang: str) -> None:
    src = DIR / "tts_cache" / lang
    if not src.is_dir():
        raise SystemExit(
            f"Missing tts_cache/{lang}/ — run: python bake_briefs.py --lang {lang}"
        )
    mp3_count = sum(1 for _ in src.rglob("*.mp3"))
    if mp3_count < 10:
        raise SystemExit(
            f"tts_cache/{lang}/ has only {mp3_count} mp3 files — bake the full portrait set first."
        )
    dst_root = target / "tts_cache"
    dst_root.mkdir(parents=True, exist_ok=True)
    shutil.copytree(src, dst_root / lang, dirs_exist_ok=True)
    print(f"Cache: tts_cache/{lang}/ ({mp3_count} mp3)")


def build_user_csharp_client() -> Path:
    """Compact user GTA2BriefTTS.exe (Frida + cache mp3 playback, no Python)."""
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise SystemExit(
            "dotnet SDK not found — install .NET 9 SDK to build the C# user client "
            "(see brief_tts/user_client/README.md)."
        )
    if not USER_CLIENT_PROJ.is_file():
        raise SystemExit(f"Missing {USER_CLIENT_PROJ}")
    if USER_CSHARP_PUBLISH.exists():
        shutil.rmtree(USER_CSHARP_PUBLISH)
    USER_CSHARP_PUBLISH.mkdir(parents=True)
    run(
        [
            dotnet,
            "publish",
            str(USER_CLIENT_PROJ),
            "-c",
            "Release",
            "-r",
            "win-x64",
            "--self-contained",
            "true",
            "-p:DebugType=none",
            "-p:DebugSymbols=false",
            "-o",
            str(USER_CSHARP_PUBLISH),
        ]
    )
    print(f"C# user client: {USER_CSHARP_PUBLISH}")
    return USER_CSHARP_PUBLISH


def build_runtime_onedir(exes: tuple[tuple[str, str], ...]) -> Path:
    """PyInstaller onedir with given entry points; returns folder path."""
    ensure_pyinstaller()
    BUILD_WORK.mkdir(parents=True, exist_ok=True)
    if not exes:
        raise ValueError("exes must not be empty")
    primary_name = exes[0][1]
    primary = build_exe(exes[0][0], primary_name)
    for entry, name in exes[1:]:
        extra = build_exe(entry, name)
        merge_onedir(primary, extra, name)
    return primary


def write_launcher_bats(target: Path, *, user_pack: bool = False) -> None:
    if user_pack:
        for name in ("Start Brief TTS (inject).bat", "Bake brief cache.bat"):
            (target / name).unlink(missing_ok=True)
        return
    (target / "Start Brief TTS (inject).bat").write_text(
        "@echo off\r\n"
        'set "HERE=%~dp0"\r\n'
        'cd /d "%HERE%"\r\n'
        'if not defined GTA2_ROOT set "GTA2_ROOT=%HERE%.."\r\n'
        'start "GTA2 Brief TTS" /D "%HERE%" GTA2BriefTTS.exe\r\n',
        encoding="utf-8",
    )
    (target / "Bake brief cache.bat").write_text(
        "@echo off\r\n"
        'cd /d "%~dp0"\r\n'
        'if not defined GTA2_ROOT set "GTA2_ROOT=%~dp0.."\r\n'
        "BakeBriefs.exe --lang both\r\n"
        "pause\r\n",
        encoding="utf-8",
    )


def write_release_readme(target: Path, version: str, with_cache: bool) -> None:
    text = f"""GTA2 Brief TTS release {version}
================================

Install:
  1. Copy this folder next to your GTA2 install, e.g.:
       D:\\Games\\gta2\\GTA2BriefTTS\\
     (game_root in config.json defaults to "..", i.e. parent folder)

  2. Or set environment variable GTA2_ROOT to the folder containing GTA2.exe

  3. Run "Start Brief TTS (inject).bat" before or with the game.

Files:
  GTA2BriefTTS.exe   — Frida inject + TTS (main)
  BakeBriefs.exe     — pre-bake tts_cache (needs internet)
  SpeakHotkeyF12.exe — manual F12 test without inject
  config.json        — languages, voices, game_root
  tts_cache/         — {"included" if with_cache else "run Bake brief cache.bat or first inject bake"}

Edit config.json if GTA2.exe is not in the parent directory.

Requires: Windows 10+, same bitness as GTA2 (32-bit game — use 32-bit Python build if inject fails).
"""
    (target / "README_RELEASE.txt").write_text(text, encoding="utf-8")


USER_README_FILES = {
    "ru": RELEASE_DIR / "README_USER_ru.txt",
    "en": RELEASE_DIR / "README_USER_en.txt",
}


def write_user_readme(target: Path, version: str, lang: str) -> None:
    src = USER_README_FILES.get(lang)
    if src and src.is_file():
        text = src.read_text(encoding="utf-8").replace("{version}", version).replace("{lang}", lang)
    else:
        title = USER_LANG_TITLES.get(lang, f"GTA2 Brief TTS ({lang})")
        text = f"{title} — release {version}\n"
    (target / "README_RELEASE.txt").write_text(text, encoding="utf-8")
    if src and src.is_file():
        shutil.copy2(src, target / "README.txt")


def make_zip(folder: Path, zip_path: Path) -> None:
    if zip_path.is_file():
        zip_path.unlink()
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        prefix = folder.name
        for path in folder.rglob("*"):
            if path.is_file():
                arc = f"{prefix}/{path.relative_to(folder).as_posix()}"
                zf.write(path, arc)
    print(f"ZIP: {zip_path}")


def write_inno_iss(
    version: str,
    stage_dir: Path,
    out_setup: Path,
    *,
    lang: str | None = None,
) -> Path:
    if lang:
        app_name = USER_LANG_TITLES.get(lang, f"GTA2 Brief TTS ({lang})")
        iss = RELEASE_DIR / f"GTA2BriefTTS-{lang}.iss"
        out_base = f"GTA2BriefTTS-{version}-{lang}-setup"
        default_dir = f"{{autopf}}\\GTA2 Brief TTS ({lang.upper()})"
        start_icon = (
            'Name: "{group}\\GTA2 Brief TTS"; Filename: "{app}\\GTA2BriefTTS.exe"\r\n'
            'Name: "{autodesktop}\\GTA2 Brief TTS"; Filename: "{app}\\GTA2BriefTTS.exe"\r\n'
        )
        bake_icon = ""
    else:
        app_name = "GTA2 Brief TTS"
        iss = RELEASE_DIR / "GTA2BriefTTS.iss"
        out_base = f"GTA2BriefTTS-{version}-setup"
        default_dir = "{autopf}\\GTA2 Brief TTS"
        start_icon = (
            'Name: "{group}\\Start Brief TTS"; Filename: "{app}\\Start Brief TTS (inject).bat"\r\n'
        )
        bake_icon = (
            'Name: "{group}\\Bake cache"; Filename: "{app}\\Bake brief cache.bat"\r\n'
        )

    game_folder_include = ""
    if lang:
        game_folder_include = '#include "inno_game_folder_code.iss"\r\n'

    iss.write_text(
        f"""
#define MyAppName "{app_name}"
#define MyAppVersion "{version}"
#define StageDir "{stage_dir.as_posix()}"
#define OutDir "{out_setup.as_posix()}"

[Setup]
AppName={{#MyAppName}}
AppVersion={{#MyAppVersion}}
DefaultDirName={default_dir}
DefaultGroupName={{#MyAppName}}
OutputBaseFilename={out_base}
OutputDir={{#OutDir}}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
SetupIconFile="{RELEASE_DIR.as_posix()}/app.ico"
UninstallDisplayIcon={{app}}\\GTA2BriefTTS.exe

[Files]
Source: "{{#StageDir}}\\*"; DestDir: "{{app}}"; Flags: ignoreversion recursesubdirs

[Icons]
{start_icon}{bake_icon}
[Run]
Filename: "{{app}}\\README_RELEASE.txt"; Description: "Read me"; Flags: postinstall shellexec skipifsilent
{game_folder_include}""",
        encoding="utf-8",
    )
    return iss


def run_inno(iss: Path) -> bool:
    for iscc in (
        r"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        r"C:\Program Files\Inno Setup 6\ISCC.exe",
    ):
        if Path(iscc).is_file():
            run([iscc, str(iss)])
            return True
    return False


def patch_config_release(target: Path) -> None:
    cfg_path = target / "config.json"
    if not cfg_path.is_file():
        return
    with cfg_path.open(encoding="utf-8") as f:
        cfg = json.load(f)
    cfg.setdefault("game_root", "..")
    if cfg.get("tts_bake_on_start") is True:
        cache = target / "tts_cache"
        if cache.is_dir() and any(cache.rglob("*.mp3")):
            cfg["tts_bake_on_start"] = False
    with cfg_path.open("w", encoding="utf-8") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)
        f.write("\n")


def patch_config_user_pack(target: Path, lang: str) -> None:
    src_cfg_path = DIR / "config.json"
    with src_cfg_path.open(encoding="utf-8") as f:
        src_cfg = json.load(f)

    write_pack_data(target / "pack_data.dat", src_cfg)

    cfg_path = target / "config.json"
    if cfg_path.is_file():
        with cfg_path.open(encoding="utf-8") as f:
            cfg = json.load(f)
    else:
        cfg = json.loads(json.dumps(src_cfg))
    cfg["game_root"] = cfg.get("game_root") or ".."
    cfg["tts_language"] = lang
    cfg["game_language"] = lang
    cfg["tts_bake_on_start"] = False
    cfg["record_session"] = False
    cfg["auto_addresses"] = False
    strip_user_config(cfg)
    if str(cfg.get("record_language", "tts")).lower() in ("ru", "en"):
        cfg["record_language"] = lang
    with cfg_path.open("w", encoding="utf-8") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)
        f.write("\n")


def stage_user_pack(
    version: str,
    lang: str,
    runtime: Path,
    *,
    make_installer: bool,
) -> Path:
    tag = f"GTA2BriefTTS-{version}-{lang}"
    stage = DIST_ROOT / tag
    if stage.exists():
        shutil.rmtree(stage)
    shutil.copytree(runtime, stage, dirs_exist_ok=True)
    copy_data_files(stage, with_cache=False)
    for name in ("README.txt",):
        p = stage / name
        if p.is_file():
            p.unlink()
    copy_cache_lang(stage, lang)
    patch_config_user_pack(stage, lang)
    write_launcher_bats(stage, user_pack=True)
    for stray in ("SpeakHotkeyF12.exe", "VOICE_OPTIONS.md"):
        (stage / stray).unlink(missing_ok=True)
    internal = stage / "_internal"
    if internal.is_dir():
        shutil.rmtree(internal)
    for pdb in stage.glob("*.pdb"):
        pdb.unlink(missing_ok=True)
    write_user_readme(stage, version, lang)
    zip_path = DIST_ROOT / f"{tag}-win64.zip"
    make_zip(stage, zip_path)
    if make_installer:
        iss = write_inno_iss(version, stage, DIST_ROOT, lang=lang)
        if run_inno(iss):
            print(f"Installer ({lang}): {DIST_ROOT / f'GTA2BriefTTS-{version}-{lang}-setup.exe'}")
        else:
            print(f"Inno Setup missing; compile {iss}", file=sys.stderr)
    print(f"User pack ({lang}): {stage}")
    return stage


def main() -> int:
    parser = argparse.ArgumentParser(description="Build GTA2 Brief TTS Windows release")
    parser.add_argument("--version", default="1.0.0")
    parser.add_argument(
        "--with-cache",
        action="store_true",
        help="Copy tts_cache/ into release (large; skips bake on first run)",
    )
    parser.add_argument(
        "--installer",
        action="store_true",
        help="Run Inno Setup ISCC.exe if installed",
    )
    parser.add_argument("--skip-exe", action="store_true", help="Only repack data/zip")
    parser.add_argument(
        "--user-pack",
        nargs="+",
        metavar="LANG",
        choices=("ru", "en"),
        help="Build end-user installers per language (needs tts_cache/LANG from bake)",
    )
    args = parser.parse_args()

    if args.user_pack:
        langs: list[str] = []
        for code in args.user_pack:
            if code not in langs:
                langs.append(code)
        if not args.skip_exe:
            runtime = build_user_csharp_client()
        elif not USER_CSHARP_PUBLISH.is_dir():
            raise SystemExit(f"--skip-exe but missing {USER_CSHARP_PUBLISH} (build C# client first)")
        else:
            runtime = USER_CSHARP_PUBLISH
        for lang in langs:
            stage_user_pack(
                args.version,
                lang,
                runtime,
                make_installer=args.installer,
            )
        return 0

    tag = f"GTA2BriefTTS-{args.version}"
    stage = DIST_ROOT / tag
    if stage.exists():
        shutil.rmtree(stage)
    stage.mkdir(parents=True)

    if not args.skip_exe:
        built = build_runtime_onedir(EXES)
        shutil.copytree(built, stage, dirs_exist_ok=True)

    copy_data_files(stage, args.with_cache)
    patch_config_release(stage)
    write_launcher_bats(stage, user_pack=False)
    write_release_readme(stage, args.version, args.with_cache)

    zip_path = DIST_ROOT / f"{tag}-win64.zip"
    make_zip(stage, zip_path)

    if args.installer:
        iss = write_inno_iss(args.version, stage, DIST_ROOT)
        if run_inno(iss):
            print(f"Installer build requested; check {DIST_ROOT}")
        else:
            print(
                "Inno Setup not found. Install from https://jrsoftware.org/isinfo.php "
                f"or compile: {iss}",
                file=sys.stderr,
            )

    print(f"\nRelease folder: {stage}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
