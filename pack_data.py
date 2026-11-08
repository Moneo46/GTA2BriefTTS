"""Build pack_data.dat — voice/brief metadata for user installs (not in config.json)."""
from __future__ import annotations

import json
import struct
from pathlib import Path

MAGIC = b"G2BT\x01"
# Obfuscation only — same bytes in PackDataStore.cs
_KEY = bytes(
    b ^ 0xA7
    for b in "GTA2BriefTTS-pack-v1-2025".encode("ascii")
)

PACK_KEYS = (
    "gang_voices",
    "gang_voices_en",
    "brief_ids",
    "brief_ids_tutorial_full",
    "tts_base_voice",
    "tts_default_voice",
)


def _xor(data: bytes) -> bytes:
    return bytes(b ^ _KEY[i % len(_KEY)] for i, b in enumerate(data))


def pack_payload(cfg: dict) -> dict:
    return {k: cfg[k] for k in PACK_KEYS if k in cfg}


def write_pack_data(dst: Path, cfg: dict) -> None:
    raw = json.dumps(pack_payload(cfg), ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    enc = _xor(raw)
    dst.write_bytes(MAGIC + struct.pack("<I", len(enc)) + enc)


def strip_user_config(cfg: dict) -> None:
    for k in PACK_KEYS:
        cfg.pop(k, None)
