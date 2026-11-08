'use strict';

// Injected by inject_runner.py as __ADDR_JSON__
const ADDR = JSON.parse('__ADDR_JSON__');

function rvaPtr(rvaHex) {
  const mod = Process.getModuleByName('GTA2.exe');
  return mod.base.add(parseInt(rvaHex, 16));
}

function scanPatternInModule(patternStr) {
  const mod = Process.getModuleByName('GTA2.exe');
  const hits = [];
  const ranges = mod.enumerateRanges('r-x');
  for (let i = 0; i < ranges.length; i++) {
    const range = ranges[i];
    try {
      Memory.scanSync(range.base, range.size, patternStr).forEach(function (m) {
        hits.push(m.address);
      });
    } catch (e) {
      /* skip unreadable range */
    }
  }
  if (hits.length === 0) {
    throw new Error('signature not found in module: ' + patternStr.slice(0, 24) + '…');
  }
  if (hits.length > 1) {
    throw new Error('ambiguous signature (' + hits.length + ' hits)');
  }
  return hits[0];
}

function extractCurrBriefOffset(matchAddr) {
  const b = matchAddr.add(4).readByteArray(6);
  const u8 = new Uint8Array(b);
  if (u8[0] !== 0x8b || u8[1] !== 0x86) {
    throw new Error('draw_brief: expected mov eax,[esi+disp]');
  }
  return u8[2] | (u8[3] << 8) | (u8[4] << 16) | (u8[5] << 24);
}

function resolveBriefPtr(name, rvaHex, sigEntry) {
  let ptr = rvaPtr(rvaHex);
  const pattern = sigEntry && sigEntry.pattern;
  if (!ADDR.auto_scan_in_process || !pattern) {
    return ptr;
  }
  try {
    const scanned = scanPatternInModule(pattern);
    if (scanned.compare(ptr) !== 0) {
      send({
        type: 'debug',
        msg: name + ' RVA adjusted ' + ptr + ' -> ' + scanned,
      });
    }
    return scanned;
  } catch (e) {
    send({ type: 'debug', msg: name + ' in-process scan: ' + e + '; using RVA ' + ptr });
    return ptr;
  }
}

const sigs = ADDR.signatures || {};
const setSig = sigs.set_hud_brief;
const drawSig = sigs.draw_brief;

const setHudBrief = resolveBriefPtr('SetHudBrief', ADDR.set_hud_brief_rva, setSig);
const drawBrief = resolveBriefPtr('DrawBrief', ADDR.draw_brief_rva, drawSig);

let OFF_CURR_BRIEF = parseInt(ADDR.brief_curr_briefs_offset, 16);
if (drawSig && ADDR.auto_scan_in_process) {
  try {
    OFF_CURR_BRIEF = extractCurrBriefOffset(drawBrief);
  } catch (e) {
    send({ type: 'debug', msg: 'curr_brief offset: ' + e });
  }
}
const OFF_ENTRY_ID = parseInt(ADDR.brief_entry_id_offset || '0x0', 16);
const briefTrigger = (ADDR.brief_trigger || 'set').toLowerCase();

let lastEmittedId = null;
let hookDrawOk = false;
let hookSetOk = false;
let debug = ADDR.debug === true;

function logDbg(msg) {
  if (debug) {
    send({ type: 'debug', msg: msg });
  }
}

function emitBrief(id) {
  if (!id || id === lastEmittedId) {
    return;
  }
  lastEmittedId = id;
  send({ type: 'brief', id: id });
}

function installHooks() {
  if (briefTrigger === 'set' || briefTrigger === 'both') {
    try {
      Interceptor.attach(setHudBrief, {
        onEnter(args) {
          const idPtr = args[1];
          if (idPtr.isNull()) {
            return;
          }
          const id = idPtr.readCString();
          logDbg('SetHudBrief id=' + id);
          emitBrief(id);
        },
      });
      hookSetOk = true;
    } catch (e) {
      send({ type: 'error', msg: 'SetHudBrief hook: ' + e });
    }
  }

  if (briefTrigger === 'draw' || briefTrigger === 'both') {
    try {
      Interceptor.attach(drawBrief, {
        onEnter() {
          const self = ptr(this.context.ecx);
          const entry = self.add(OFF_CURR_BRIEF).readPointer();
          if (entry.isNull()) {
            return;
          }
          const id = entry.add(OFF_ENTRY_ID).readCString();
          logDbg('DrawBrief id=' + id);
          emitBrief(id);
        },
      });
      hookDrawOk = true;
    } catch (e) {
      send({ type: 'error', msg: 'DrawBrief hook: ' + e });
    }
  }
}

installHooks();

send({
  type: 'ready',
  hookDraw: hookDrawOk,
  hookSet: hookSetOk,
  briefTrigger: briefTrigger,
  drawAt: drawBrief.toString(),
  setAt: setHudBrief.toString(),
  currBriefOffset: '0x' + OFF_CURR_BRIEF.toString(16),
  moduleBase: Process.getModuleByName('GTA2.exe').base.toString(),
  scanMethod: ADDR.scan_method || 'static',
});
