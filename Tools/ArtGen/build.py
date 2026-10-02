import sys, importlib
from PIL import Image, ImageDraw
from px import sheet

mods = sys.argv[1:] or ["c_fire", "c_water", "c_grass", "c_elec", "c_rock"]
rows = []
for m in mods:
    mod = importlib.import_module(m)
    for key, name, fn in mod.CREATURES:
        imgs = [fn(lv) for lv in (1, 2, 3)]
        for lv, im in enumerate(imgs, 1):
            im.save(f"out/{key}_Lv{lv}.png")
        rows.append(sheet(imgs, scale=6))
w = max(r.width for r in rows)
h = sum(r.height for r in rows)
out = Image.new("RGBA", (w, h), (46, 52, 64, 255))
y = 0
for r in rows:
    out.alpha_composite(r, (0, y)); y += r.height
out.save("out/sheet_" + "_".join(mods) + ".png")
