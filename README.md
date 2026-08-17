# Desktop Pet Crocodile

A pixel-art crocodile that idles and waddles back and forth along your Windows taskbar
(Shimeji-style desktop pet). Transparent, click-through, always-on-top WPF window.

## Enable / disable

Enable (start it):

```
dotnet run --project src/DesktopPet
```

or double-click the built `src/DesktopPet/bin/Debug/net8.0-windows/DesktopPet.exe`.

Disable (stop it): right-click the tray icon (bottom-right of the taskbar) and choose
**Exit** — the pet window has no title bar or taskbar entry of its own.

## Boot on startup

Right-click the tray icon and toggle **Start with Windows**. This writes/removes a
per-user entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` — no admin
rights needed, and unchecking it removes the entry cleanly.

This only works when running the built `DesktopPet.exe` directly (not via
`dotnet run`, which would register `dotnet.exe` with no arguments instead). For a
stable path across rebuilds, publish a Release build once:

```
dotnet publish src/DesktopPet -c Release -r win-x64 --self-contained false
```

then run `DesktopPet.exe` from `src/DesktopPet/bin/Release/net8.0-windows/win-x64/publish/`
going forward and enable the toggle from there.

## Project layout

- `src/DesktopPet` — the WPF app.
  - `Native/Win32.cs` — makes the window click-through (`WS_EX_LAYERED | WS_EX_TRANSPARENT`).
  - `Pet/TaskbarTracker.cs` — resolves the taskbar's on-screen bounds.
  - `Pet/PetStateMachine.cs` — Idle/Walk loop, direction, edge bouncing.
  - `Pet/SpriteAnimator.cs` — slices a sprite sheet into frames and cycles them.
  - `Assets/Sprites/croc_idle.png`, `croc_walk.png` — sprite sheets (see below).
- `tools/GenerateSprites` — console app that procedurally drew the original placeholder
  sprites (superseded by the Nano Banana art below, kept for reference).
- `tools/nanobanana` — pipeline used to generate the current croc art via Vertex AI's
  Gemini 2.5 Flash Image ("Nano Banana"):
  - `generate_croc.py "prompt"` — sends a prompt (appending to a persisted
    conversation in `conversation.json`, so you can keep iterating on the same
    character across runs) and saves the result to `output/croc_NN.png`. Pass
    `--reset` to start a fresh character, or `--from N` to branch off frame N
    and discard any later turns (useful when a later iteration drifted).
    Auth is via your own `gcloud` credentials — no service account/key needed.
  - `compose_sheet.py` — takes the chosen `output/croc_NN.png` frames (currently
    `croc_01`–`croc_04` for the walk cycle; `croc_06`/`croc_08`/`croc_07`/`croc_08`
    — mouth closed/half/open/half — for the idle yawn loop), removes the white
    background, crops/aligns/pads them to one common frame size shared by both
    sheets, and writes `croc_walk.png`/`croc_idle.png` straight into
    `src/DesktopPet/Assets/Sprites`. Edit the `WALK_FRAMES`/`IDLE_FRAMES` lists
    at the top of the script to change which output frames get used.
  - `add_tail_wag.py` — run **after** `compose_sheet.py` (which would otherwise
    overwrite it). Nano banana wouldn't reliably vary leg poses on edit
    requests, so the walk cycle reuses frames 1–4 as generated; this script
    fakes tail motion by rotating just the tail region a couple degrees on
    alternating frames, then patches the small seam a hard rotation leaves
    with a locally-scoped morphological closing (see comments in the file for
    why it's scoped that way — applying it whole-frame blurred unrelated
    details like the teeth).

## Swapping in different art

Replace `Assets/Sprites/croc_idle.png` and `croc_walk.png` with your own horizontal
sprite sheets (either hand-made, or by re-running the nano-banana pipeline above with
new frames/prompts) — same frame count, and all frames in a sheet the same width (the
app reads frame width as `sheet width / frame count`). Update the window's
`Width`/`Height` in `MainWindow.xaml` to match your frame size, and the `frameCount`
args in `MainWindow.xaml.cs` if you change how many frames are in either sheet.

## Known limitations (v1)

- Assumes a single taskbar on the primary monitor (no multi-monitor taskbar support).
- No support for an auto-hidden taskbar.
- Walks the taskbar only — doesn't yet climb onto other open windows (planned v2).
