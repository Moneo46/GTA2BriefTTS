GTA2 Brief TTS (inject)
=======================

Auto-detects mission briefs via Frida hooks inside GTA2.exe (SetHudBrief / DrawBrief).

Install (dev):
  pip install -r requirements.txt

Windows release (exe + config + optional cache):
  pip install -r requirements-release.txt
  python build_release.py --with-cache --version 1.0.0
  python build_release.py --installer   (needs Inno Setup 6)

End-user installers (RU / EN, cache baked in, no BakeBriefs, no startup bake):
  python bake_briefs.py --lang ru
  python bake_briefs.py --lang en
  python build_release.py --user-pack ru en --version 1.0.0 --installer
  -> release/dist/GTA2BriefTTS-<ver>-ru-setup.exe
     release/dist/GTA2BriefTTS-<ver>-en-setup.exe

Pre-bake all mission briefs (needs internet):
  python bake_briefs.py
  python bake_briefs.py --lang en
  python bake_briefs.py --lang both

Languages (config.json):
  game_language  — HUD subtitles in GTA2 (registry r/e). Applied by inject and by
                   Start GTA2 (RTX Remix).bat after gta2_russian.reg (see apply_hud_language.py).
  tts_language   — brief voice + GXT text for TTS (can differ from HUD)
  gxt_files      — ru: data/r.gxt, en: data/e.gxt
  gang_voices / gang_voices_en — Edge voices per gang
  record_session — append played briefs to brief_tts/recordings/*.wav
  record_language — tts | ru | en (track language; may bake a second lang)

TTS: Microsoft Edge neural voices per gang (edge_voices.py).
Cache layout: tts_cache/<lang>/<voice>__<prosody>/<brief_id>.mp3
Identical brief text (same voice folder) is synthesized once; other ids copy
from txt_<hash>.mp3 (e.g. Alan Jack / dead brief line ~72x en, ~71x ru).
On inject start, portrait briefs are baked for tts_language (and record_language
if different). Playback uses cache only.

Run:
  Start GTA2 (RTX Remix) + Brief TTS.bat

Or:
  Start GTA2 (RTX Remix).bat
  cd brief_tts && python inject_runner.py

Console should show:
  [inject] Agent ready …
  [inject] Brief 1600 …

Manual fallback (F12): python speak_hotkey.py
Quick TTS test: python test_tts.py

TTS providers, commercial use, costs (~229k chars ru+en):
  see VOICE_OPTIONS.md

Brief hooks (SetHudBrief / DrawBrief):
  auto_addresses scans GTA2.exe at inject start (see brief_signatures.json).
  Cache: addresses.cache.json (keyed by exe SHA-256).
  Manual: python brief_scanner.py path\\to\\GTA2.exe
  Fallback: addresses.json if scan finds no match.
