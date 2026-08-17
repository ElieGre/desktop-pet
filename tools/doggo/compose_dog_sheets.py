"""
Turns the raw Gemini dog sheet (one PNG holding a 2x4 grid of poses on a
transparent background) into the horizontal sprite sheets DesktopDog expects.

Unlike tools/nanobanana/compose_sheet.py -- which scaled every croc frame to the
same height because all its poses were the same size -- the dog poses differ on
purpose (the howl frames are taller because the head goes up), so scaling them
to a common total height would shrink the howl instead of raising the head.
Frames are normalised on back height instead (see measure_back_height), then
bottom-aligned (feet planted) and torso-aligned horizontally, so the dog's body
holds still while only the head, legs and tail move.

Usage:
    python compose_dog_sheets.py
"""
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "Gemini_Generated_Image_bpn5mobpn5mobpn5.png"
FRAMES_DIR = ROOT / "frames"
SPRITES_DIR = ROOT.parent.parent / "src" / "DesktopDog" / "Assets" / "Sprites"

# Frame numbers refer to the source grid read left-to-right, top-to-bottom:
#   1 stand/mouth closed   2 head up, mouth ajar   3 head up, mouth wide   4 stand/panting
#   5-8 walk cycle (tongue out)
WALK_FRAMES = [5, 6, 7, 8]
IDLE_FRAMES = [1, 4]            # mouth closed -> panting, loops
HOWL_FRAMES = [1, 2, 3, 3, 2, 1]  # wind up, hold the howl, settle back; played once per howl

FRAME_HEIGHT = 64  # px; the tallest pose (full howl) fills this, everything else is shorter

# Horizontal alignment anchor, as a fraction of the standing dog's height measured
# up from its feet: the torso slab, which is the one part of the dog that holds
# still across every pose (legs swing, head lifts, tail moves).
TORSO_LOW = 0.35
TORSO_HIGH = 0.65

# Slice of the sprite (fraction of its width, from the tail end) used to measure
# back height. Every dog faces right, so this lands on the mid-back/shoulder --
# behind the head, in front of the tail.
BACK_LOW = 0.30
BACK_HIGH = 0.52


def extract_poses(source: Path) -> list[Image.Image]:
    """
    Splits the source grid into individual poses. The poses can't be cut on a
    fixed grid -- neighbouring dogs' bounding boxes overlap (one dog's tail
    reaches past the next one's nose) -- so they're separated as connected
    components of the alpha channel instead, at 1/4 resolution for speed.
    """
    img = Image.open(source).convert("RGBA")
    arr = np.array(img)
    alpha = arr[:, :, 3]
    height, width = alpha.shape

    scale = 4
    small = alpha[::scale, ::scale] > 100
    rows, cols = small.shape
    seen = np.zeros_like(small)
    components: list[list[tuple[int, int]]] = []

    for y in range(rows):
        for x in range(cols):
            if not small[y, x] or seen[y, x]:
                continue
            queue = deque([(y, x)])
            seen[y, x] = True
            points = []
            while queue:
                cy, cx = queue.popleft()
                points.append((cy, cx))
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < rows and 0 <= nx < cols and small[ny, nx] and not seen[ny, nx]:
                            seen[ny, nx] = True
                            queue.append((ny, nx))
            if len(points) > 200:  # ignores stray specks in the source art
                components.append(points)

    def bounds(points):
        ys = [p[0] for p in points]
        xs = [p[1] for p in points]
        return min(xs), min(ys)

    # Reading order: top row left-to-right, then bottom row.
    components.sort(key=lambda pts: (bounds(pts)[1] // 50, bounds(pts)[0]))

    poses = []
    for points in components:
        mask = np.zeros((rows, cols), bool)
        for cy, cx in points:
            mask[cy, cx] = True
        big = np.kron(mask, np.ones((scale, scale), bool))[:height, :width]
        # Grow the mask a little so the pose keeps its own anti-aliased edge
        # without dragging in the neighbouring dog's pixels.
        grown = big.copy()
        for step in range(1, scale + 3):
            grown[step:, :] |= big[:-step, :]
            grown[:-step, :] |= big[step:, :]
            grown[:, step:] |= big[:, :-step]
            grown[:, :-step] |= big[:, step:]

        isolated = arr.copy()
        isolated[:, :, 3] = np.where(grown, alpha, 0)
        pose = Image.fromarray(isolated)
        poses.append(pose.crop(pose.getbbox()))

    return poses


def measure_back_height(frame: Image.Image) -> float:
    """
    Distance from the feet to the top of the mid-back. The source art draws the
    walking poses about 10% smaller than the standing ones, which pops when the
    app swaps sheets; back height is the one measurement that is meaningful in
    every pose (total height isn't -- it grows when the head lifts to howl).
    """
    alpha = np.array(frame)[:, :, 3] > 100
    low = int(frame.width * BACK_LOW)
    high = max(low + 1, int(frame.width * BACK_HIGH))
    band = alpha[:, low:high]
    ys = np.nonzero(band.any(axis=1))[0]
    if ys.size == 0:
        return float(frame.height)
    return float(ys.max() - ys.min())


def torso_center_x(frame: Image.Image, body_height: float) -> float:
    """Centroid x of the opaque pixels in the torso slab, measured up from the feet."""
    alpha = np.array(frame)[:, :, 3] > 100
    top = max(0, int(frame.height - body_height * TORSO_HIGH))
    bottom = max(top + 1, int(frame.height - body_height * TORSO_LOW))
    slab = alpha[top:bottom, :]
    xs = np.nonzero(slab.any(axis=0))[0]
    if xs.size == 0:
        return frame.width / 2
    weights = slab.sum(axis=0)[xs]
    return float((xs * weights).sum() / weights.sum())


def build_sheet(frames: list[Image.Image], canvas: tuple[int, int], anchor_x: float,
                body_height: float) -> Image.Image:
    width, height = canvas
    sheet = Image.new("RGBA", (width * len(frames), height), (0, 0, 0, 0))
    for i, frame in enumerate(frames):
        x = i * width + round(anchor_x - torso_center_x(frame, body_height))
        y = height - frame.height  # bottom-align: feet stay planted on the taskbar
        sheet.paste(frame, (x, y), frame)
    return sheet


def main() -> None:
    poses = extract_poses(SOURCE)
    if len(poses) != 8:
        raise SystemExit(f"Expected 8 poses in the source grid, found {len(poses)}")

    FRAMES_DIR.mkdir(exist_ok=True)
    for i, pose in enumerate(poses, start=1):
        pose.save(FRAMES_DIR / f"dog_{i:02d}.png")

    used = sorted(set(WALK_FRAMES + IDLE_FRAMES + HOWL_FRAMES))
    backs = {n: measure_back_height(poses[n - 1]) for n in used}

    # Equalise back height across poses first, then pick the one global factor
    # that makes the tallest resulting pose (the full howl) fill FRAME_HEIGHT.
    reference_back = backs[IDLE_FRAMES[0]]
    normalised = {n: poses[n - 1].height * reference_back / backs[n] for n in used}
    global_scale = FRAME_HEIGHT / max(normalised.values())

    scaled = {}
    for n in used:
        pose = poses[n - 1]
        scale = global_scale * reference_back / backs[n]
        size = (max(1, round(pose.width * scale)), max(1, round(pose.height * scale)))
        scaled[n] = pose.resize(size, Image.LANCZOS)

    # The standing pose defines where the torso sits; every other pose is aligned
    # against that same slab so the body stays put as the head/legs move.
    body_height = scaled[IDLE_FRAMES[0]].height
    centers = {n: torso_center_x(f, body_height) for n, f in scaled.items()}

    # A canvas wide enough that the widest pose still fits once shifted onto the
    # shared anchor (the panting pose reaches further forward than the others).
    anchor = max(centers.values())
    canvas_width = max(round(anchor + (f.width - centers[n])) for n, f in scaled.items()) + 1
    canvas = (canvas_width, FRAME_HEIGHT)

    SPRITES_DIR.mkdir(parents=True, exist_ok=True)
    for name, numbers in (("walk", WALK_FRAMES), ("idle", IDLE_FRAMES), ("howl", HOWL_FRAMES)):
        sheet = build_sheet([scaled[n] for n in numbers], canvas, anchor, body_height)
        sheet.save(SPRITES_DIR / f"dog_{name}.png")
        print(f"Wrote dog_{name}.png ({len(numbers)} frames, {sheet.size})")

    print(f"Single frame size: {canvas[0]}x{canvas[1]} -- "
          f"PetProfile.Dog FrameSize / PetWindow must match.")


if __name__ == "__main__":
    main()
