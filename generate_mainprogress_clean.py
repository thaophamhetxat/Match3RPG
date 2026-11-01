from pathlib import Path
from PIL import Image

src = Path(r'g:\UnityProjects\Match3RPG\Match3RPG\Assets\Art\MainProgress\Animations\TaDiem_Idle_Breathing_Hair_Spaced_1280x768.png')
out = Path(r'g:\UnityProjects\Match3RPG\Match3RPG\Assets\Resources\MainProgress\Animations\TaDiem_Idle_Breathing_Hair_Spaced_1280x768_Clean.png')
out.parent.mkdir(parents=True, exist_ok=True)

img = Image.open(src).convert('RGBA')
width, height = img.size
pixels = img.load()

# Remove only background pixels connected to the image border.
visited = set()
stack = []
for x in range(width):
    for y in (0, height - 1):
        r, g, b, a = pixels[x, y]
        if a > 0 and r >= 240 and g >= 240 and b >= 240:
            stack.append((x, y))
for y in range(height):
    for x in (0, width - 1):
        r, g, b, a = pixels[x, y]
        if a > 0 and r >= 240 and g >= 240 and b >= 240:
            stack.append((x, y))

while stack:
    x, y = stack.pop()
    if x < 0 or y < 0 or x >= width or y >= height:
        continue
    if (x, y) in visited:
        continue
    visited.add((x, y))
    r, g, b, a = pixels[x, y]
    if a == 0 or not (r >= 240 and g >= 240 and b >= 240):
        continue
    pixels[x, y] = (0, 0, 0, 0)
    for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
        if 0 <= nx < width and 0 <= ny < height:
            if (nx, ny) not in visited:
                nr, ng, nb, na = pixels[nx, ny]
                if na > 0 and nr >= 240 and ng >= 240 and nb >= 240:
                    stack.append((nx, ny))

# Light cleanup around the silhouette to avoid harsh white halos while preserving inner highlights.
for y in range(1, height - 1):
    for x in range(1, width - 1):
        r, g, b, a = pixels[x, y]
        if a > 0:
            neighbors = [pixels[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
            if any(n[3] == 0 for n in neighbors):
                if r >= 245 and g >= 245 and b >= 245:
                    pixels[x, y] = (r, g, b, max(0, a - 10))

img.save(out)
print(f'Created cleaned sprite sheet: {out}')
