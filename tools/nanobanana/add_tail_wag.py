"""
Adds a subtle tail-wag to the already-composited croc_walk.png by cropping the
tail region (leftmost slice of each frame, where the croc's tail lives) and
rotating it a few degrees around its base, alternating per frame. Nano banana
resisted real leg-pose edits, so this sidesteps another round of prompting for
what's really just a rigid-body rotation.

Usage:
    python add_tail_wag.py
"""
from pathlib import Path

from PIL import Image, ImageFilter

SPRITES_DIR = Path(__file__).resolve().parent.parent.parent / "src" / "DesktopPet" / "Assets" / "Sprites"
WALK_SHEET = SPRITES_DIR / "croc_walk.png"

TAIL_REGION_FRACTION = 0.34  # leftmost slice of each frame treated as "tail"
PIVOT_Y_FRACTION = 0.42      # approx height of the tail/body joint
WAG_ANGLES_DEG = [-2, 0, 2, 0]  # one per walk frame, in frame order


def wag_frame(frame: Image.Image, angle: float) -> Image.Image:
    w, h = frame.size
    tail_w = round(w * TAIL_REGION_FRACTION)
    pivot = (tail_w, round(h * PIVOT_Y_FRACTION))

    tail_tile = frame.crop((0, 0, tail_w, h))
    # NEAREST, not BICUBIC: pixel art wants hard edges, and bicubic overshoot
    # at a black-outline-to-transparent boundary produces a bright fringe.
    rotated_tail = tail_tile.rotate(
        angle, resample=Image.NEAREST, center=pivot, expand=False, fillcolor=(0, 0, 0, 0)
    )

    result = frame.copy()
    result.paste((0, 0, 0, 0), (0, 0, tail_w, h))  # clear old tail pixels
    result.paste(rotated_tail, (0, 0), rotated_tail)

    # Rotating a hard-edged crop against unrotated neighboring pixels leaves
    # 1px seam notches where a pattern line no longer lines up across the cut.
    # Patch just a narrow strip around the seam with a morphological closing
    # (dilate then erode) -- applying it to the whole frame blurs/grays
    # unrelated details elsewhere (e.g. the white teeth against transparent
    # background), so keep the fix local to where the seam actually is.
    seam_margin = 6
    strip_box = (max(0, tail_w - seam_margin), 0, min(w, tail_w + seam_margin), h)
    strip = result.crop(strip_box)
    closed_strip = strip.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
    result.paste(closed_strip, strip_box[:2])
    return result


def main() -> None:
    sheet = Image.open(WALK_SHEET).convert("RGBA")
    frame_count = len(WAG_ANGLES_DEG)
    frame_w = sheet.width // frame_count
    frame_h = sheet.height

    out_sheet = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
    for i, angle in enumerate(WAG_ANGLES_DEG):
        box = (i * frame_w, 0, (i + 1) * frame_w, frame_h)
        frame = sheet.crop(box)
        wagged = wag_frame(frame, angle)
        out_sheet.paste(wagged, (i * frame_w, 0), wagged)

    out_sheet.save(WALK_SHEET)
    print(f"Applied tail wag ({WAG_ANGLES_DEG}) to {WALK_SHEET}")


if __name__ == "__main__":
    main()
