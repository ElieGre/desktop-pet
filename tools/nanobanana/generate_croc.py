"""
Iterative pixel-art croc generation against Vertex AI's Gemini 2.5 Flash Image
("Nano Banana"), using the user's own `gcloud` credentials (Vertex AI Studio is
IAM-blocked on this project, but the raw generateContent API isn't).

Each run appends one turn to a persisted conversation (conversation.json) and saves
the returned image to output/croc_NN.png, so you can keep refining the same
character ("make the legs shorter", "flip to face right", ...) across runs instead
of starting from scratch each time.

Usage:
    python generate_croc.py "initial prompt"          # start/continue conversation
    python generate_croc.py "refinement prompt"       # iterate on the last image
    python generate_croc.py "new prompt" --reset      # start a fresh conversation
    python generate_croc.py "prompt" --from 1         # branch off croc_01 specifically,
                                                       # discarding any later turns first
                                                       # (use when a later frame drifted
                                                       # and you want to retry from a
                                                       # known-good earlier one)
"""
import base64
import json
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

PROJECT = "prj-798846855667"
LOCATION = "us-central1"
MODEL = "gemini-2.5-flash-image"

ROOT = Path(__file__).resolve().parent
STATE_FILE = ROOT / "conversation.json"
OUTPUT_DIR = ROOT / "output"


def get_token() -> str:
    # shell=True because `gcloud` is a .cmd shim on Windows; subprocess can't
    # resolve that via PATH without going through the shell.
    result = subprocess.run(
        "gcloud auth print-access-token",
        shell=True, capture_output=True, text=True, check=True,
    )
    return result.stdout.strip()


def load_history() -> list:
    if STATE_FILE.exists():
        return json.loads(STATE_FILE.read_text())
    return []


def save_history(history: list) -> None:
    STATE_FILE.write_text(json.dumps(history))


def next_output_path() -> Path:
    existing = sorted(OUTPUT_DIR.glob("croc_*.png"))
    idx = len(existing) + 1
    return OUTPUT_DIR / f"croc_{idx:02d}.png"


def main() -> None:
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    prompt = sys.argv[1]
    rest = sys.argv[2:]
    reset = "--reset" in rest

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    history = [] if reset else load_history()

    if "--from" in rest:
        frame_no = int(rest[rest.index("--from") + 1])
        history = history[: 2 * frame_no]

    history.append({"role": "user", "parts": [{"text": prompt}]})

    token = get_token()
    url = (
        f"https://{LOCATION}-aiplatform.googleapis.com/v1/projects/{PROJECT}"
        f"/locations/{LOCATION}/publishers/google/models/{MODEL}:generateContent"
    )
    body = json.dumps({"contents": history}).encode()
    req = urllib.request.Request(
        url,
        data=body,
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
        },
    )

    try:
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        print(f"Request failed ({e.code}):", e.read().decode())
        sys.exit(1)

    candidate = data["candidates"][0]["content"]
    history.append(candidate)
    save_history(history)

    image_parts = [p for p in candidate["parts"] if "inlineData" in p]
    if not image_parts:
        text_parts = [p.get("text", "") for p in candidate["parts"]]
        print("No image returned. Model said:", " ".join(text_parts))
        sys.exit(1)

    out_path = next_output_path()
    out_path.write_bytes(base64.b64decode(image_parts[0]["inlineData"]["data"]))
    print(f"Saved {out_path}")


if __name__ == "__main__":
    main()
