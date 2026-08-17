"""
Turns the raw nano-banana output frames (opaque white background, 1024x1024,
inconsistent framing) into the horizontal sprite sheets DesktopPet expects:
transparent background, all frames cropped/aligned/padded to one common size.

Usage:
    python compose_sheet.py
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
OUTPUT_DIR = ROOT / "output"
SPRITES_DIR = ROOT.parent.parent / "src" / "DesktopPet" / "Assets" / "Sprites"

WALK_FRAMES = ["croc_01.png", "croc_02.png", "croc_03.png", "croc_04.png"]
IDLE_FRAMES = ["croc_06.png", "croc_08.png", "croc_07.png", "croc_08.png"]  # closed -> half -> open -> half, loops

WHITE_THRESHOLD = 245  # pixels this close to pure white are treated as background


def remove_white_background(img: Image.Image) -> Image.Image:
    img = img.convert("RGBA")
    pixels = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, _ = pixels[x, y]
            if r >= WHITE_THRESHOLD and g >= WHITE_THRESHOLD and b >= WHITE_THRESHOLD:
                pixels[x, y] = (r, g, b, 0)
    return img


def crop_to_content(img: Image.Image) -> Image.Image:
    bbox = img.getbbox()
    return img.crop(bbox) if bbox else img


def build_sheet(frame_names: list[str], target_height: int = 64, canvas_width: int | None = None) -> Image.Image:
    """
    canvas_width: if given, every frame slot is forced to this width (scaling
    down further if a frame would otherwise be wider) instead of deriving the
    slot width from this sheet's own frames. Pass the walk sheet's frame width
    when building the idle sheet so both animations share one frame size --
    they're swapped into the same fixed-size window, so mismatched native
    sizes would otherwise stretch one of them.
    """
    cropped = [crop_to_content(remove_white_background(Image.open(OUTPUT_DIR / name))) for name in frame_names]

    # Scale every frame to the same height (preserving aspect ratio) before
    # measuring the common canvas, so poses with a taller/shorter bbox don't
    # end up a different visual size from one another.
    scaled = []
    for frame in cropped:
        ratio = target_height / frame.height
        new_size = (max(1, round(frame.width * ratio)), target_height)
        scaled.append(frame.resize(new_size, Image.LANCZOS))

    slot_width = canvas_width if canvas_width is not None else max(f.width for f in scaled)
    canvas_height = target_height

    sheet = Image.new("RGBA", (slot_width * len(scaled), canvas_height), (0, 0, 0, 0))
    for i, frame in enumerate(scaled):
        if frame.width > slot_width:
            ratio = slot_width / frame.width
            frame = frame.resize((slot_width, max(1, round(frame.height * ratio))), Image.LANCZOS)
        x_offset = i * slot_width + (slot_width - frame.width) // 2
        y_offset = canvas_height - frame.height  # bottom-align so feet stay planted
        sheet.paste(frame, (x_offset, y_offset), frame)

    return sheet


def main() -> None:
    SPRITES_DIR.mkdir(parents=True, exist_ok=True)

    walk_sheet = build_sheet(WALK_FRAMES)
    walk_sheet.save(SPRITES_DIR / "croc_walk.png")
    frame_w = walk_sheet.width // len(WALK_FRAMES)
    print(f"Wrote croc_walk.png ({len(WALK_FRAMES)} frames, {walk_sheet.size})")

    # Force the idle sheet to share the walk sheet's frame width so both
    # animations render at the same size in the app's fixed-size window.
    idle_sheet = build_sheet(IDLE_FRAMES, canvas_width=frame_w)
    idle_sheet.save(SPRITES_DIR / "croc_idle.png")
    print(f"Wrote croc_idle.png ({len(IDLE_FRAMES)} frames, {idle_sheet.size})")

    print(f"Single frame size: {frame_w}x{walk_sheet.height} -- update MainWindow.xaml Width/Height to match.")


if __name__ == "__main__":
    main()
