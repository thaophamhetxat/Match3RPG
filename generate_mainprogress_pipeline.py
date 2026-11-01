from pathlib import Path
import re

try:
    from PIL import Image
except ImportError as exc:
    raise SystemExit(f"Pillow is required to generate the cleaned meditation frames. {exc}")

project_root = Path(r"g:\UnityProjects\Match3RPG\Match3RPG")
source_png = project_root / "Assets" / "Art" / "MainProgress" / "Animations" / "TaDiem_Idle_Breathing_Hair_Spaced_1280x768.png"
source_meta = source_png.with_suffix(".png.meta")

if not source_png.exists():
    raise SystemExit(f"Source sprite sheet not found: {source_png}")
if not source_meta.exists():
    raise SystemExit(f"Source metadata not found: {source_meta}")

output_dir = project_root / "Assets" / "Generated" / "MainProgress" / "Meditation"
resource_dir = project_root / "Assets" / "Resources" / "MainProgress" / "Animations"
output_dir.mkdir(parents=True, exist_ok=True)
resource_dir.mkdir(parents=True, exist_ok=True)

# Keep the source art untouched; only generated copies are normalized.
for stale in list(output_dir.glob("MainProgress_Meditation_*.png")):
    stale.unlink()
for stale in list(resource_dir.glob("MainProgress_Meditation_*.png")):
    stale.unlink()

meta_text = source_meta.read_text(encoding="utf-8")
pattern = re.compile(
    r"name:\s*(?:.+?)\n\s*rect:\s*\n\s*serializedVersion:\s*2\n\s*x:\s*(\d+)\n\s*y:\s*(\d+)\n\s*width:\s*(\d+)\n\s*height:\s*(\d+)",
    re.MULTILINE,
)
entries = []
for match in pattern.finditer(meta_text):
    x, y, width, height = [int(v) for v in match.groups()]
    entries.append((x, y, width, height))

if not entries:
    raise SystemExit(f"Could not parse sprite rects from {source_meta}")

sheet = Image.open(source_png).convert("RGBA")
frame_records = []
canvas_size = (512, 512)
for index, (x, y, width, height) in enumerate(entries):
    crop = sheet.crop((x, y, x + width, y + height)).convert("RGBA")
    alpha = crop.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        continue

    # Harvest only actual painted pixels and remove the white background from the generated copy.
    trimmed = crop.crop(bbox)
    trimmed = trimmed.convert("RGBA")
    pixels = trimmed.load()
    for py in range(trimmed.height):
        for px in range(trimmed.width):
            r, g, b, a = pixels[px, py]
            if a <= 0:
                pixels[px, py] = (0, 0, 0, 0)
            elif r > 245 and g > 245 and b > 245:
                pixels[px, py] = (r, g, b, 0)

    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    x_offset = max(0, (canvas_size[0] - trimmed.width) // 2)
    y_offset = max(0, canvas_size[1] - trimmed.height - 42)
    canvas.paste(trimmed, (x_offset, y_offset), trimmed)

    frame_name = f"MainProgress_Meditation_{index:02d}.png"
    generated_path = output_dir / frame_name
    resource_path = resource_dir / frame_name
    canvas.save(generated_path)
    canvas.save(resource_path)
    frame_records.append((frame_name, str(generated_path), str(resource_path)))

if not frame_records:
    raise SystemExit("No valid meditation frames were generated.")

summary = "Generated cleaned meditation frames\n" + "\n".join(f"{name}: {generated}" for name, generated, _ in frame_records) + "\n"
(output_dir / "MAINPROGRESS_MEDITATION_AUDIT.txt").write_text(summary, encoding="utf-8")
print(f"Generated {len(frame_records)} frames in {output_dir} and {resource_dir}")
