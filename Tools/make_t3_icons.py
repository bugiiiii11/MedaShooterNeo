"""
T3 weapon inventory icons for MedaShooter (build v24).

The T1/T2 icons live in the full 2048x1024 atlas Resources/NFTIcons/weaponIcons.png (no free
room), so the eight T3 icons are single sprites in Resources/NFTIcons/weaponIconsT3/, one PNG
per weapon, file name = lowercase weapon name (= the sprite name the code looks up).

Source art = the transparent Tier3 renders of the Meda Wars forge
(frontend/public/game/ui-mw/weapons/{gun,sword}/*_Tier3_Image.png), same framing as the old
icons: the business end of the weapon, tilted, on a tier-coloured tile (T1 green, T2 cyan,
T3 purple).

Run:  python Tools/make_t3_icons.py [--sheet out.png]
Rewrites the PNGs; a .meta is written only when missing (keeps the guid stable).
"""
import hashlib
import math
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
ART = os.path.join(os.path.dirname(REPO), "frontend", "public", "game", "ui-mw", "weapons")
OUT = os.path.join(REPO, "Assets", "Resources", "NFTIcons", "weaponIconsT3")
META_TEMPLATE = os.path.join(REPO, "Assets", "Resources", "NFTIcons", "weaponIcons.png.meta")

SIZE = 200          # the atlas icons are 200 x 200
SS = 4              # supersample
ANGLE = 52          # degrees, tip up-right like the T1/T2 icons

# weapon name (lowercase = sprite name), render, length of the weapon in icon widths,
# where the tip lands in the icon (fractions of the width, y down)
ICONS = [
    ("vicious greatsword",         "sword/Sword_Warriors_Tier3_Image.png",    2.3, (0.80, 0.14)),
    ("uchigatana",                 "sword/Sword_Conquerors_Tier3_Image.png",  2.3, (0.82, 0.13)),
    ("knightsfall claymore",       "sword/Sword_Strategist_Tier3_Image.png",  2.3, (0.80, 0.13)),
    ("protector's blade",          "sword/Sword_Guardians_Tier3_Image.png",   2.3, (0.80, 0.13)),
    ("venom",                      "gun/Gun_Assassins_Tier3_Image.png",       1.7, (0.80, 0.14)),
    ("bloodsoaked meda-gun",       "gun/Gun_Wanderers_Tier3_Image.png",       1.9, (0.80, 0.12)),
    ("commander's repeater",       "gun/Gun_Operators_Tier3_Image.png",       1.8, (0.82, 0.12)),
    ("swarmhunter's sniper rifle", "gun/Gun_Nightstalkers_Tier3_Image.png",   1.8, (0.80, 0.14)),
]


def tile(px):
    """Purple radial tile, bright spot low-left like the green / cyan tiles."""
    small = 64
    g = Image.new("RGB", (small, small))
    cx, cy = 0.36 * small, 0.60 * small
    far = math.hypot(small, small) * 0.72
    inner, outer = (176, 62, 236), (24, 14, 46)
    pix = g.load()
    for y in range(small):
        for x in range(small):
            t = min(1.0, math.hypot(x - cx, y - cy) / far) ** 0.85
            pix[x, y] = tuple(int(a + (b - a) * t) for a, b in zip(inner, outer))
    return g.resize((px, px), Image.BICUBIC).convert("RGBA")


def make(render, length, tip):
    px = SIZE * SS
    src = Image.open(os.path.join(ART, render)).convert("RGBA")
    src = src.crop(src.getbbox())
    w, h = src.size
    # canvas with the weapon's tip (right end, mid height) at the centre, then rotate about it
    half = int(math.hypot(w, h)) + 4
    canvas = Image.new("RGBA", (half * 2, half * 2), (0, 0, 0, 0))
    canvas.paste(src, (half - w, half - h // 2), src)
    canvas = canvas.rotate(ANGLE, resample=Image.BICUBIC)
    scale = length * px / w
    canvas = canvas.resize((int(canvas.width * scale), int(canvas.height * scale)), Image.LANCZOS)
    icon = tile(px)
    ox = int(tip[0] * px - canvas.width / 2)
    oy = int(tip[1] * px - canvas.height / 2)
    # soft drop shadow under the weapon
    shadow = Image.new("RGBA", canvas.size, (8, 4, 18, 0))
    shadow.putalpha(canvas.getchannel("A").point(lambda a: a * 110 // 255))
    icon.alpha_composite(shadow, (max(ox + 3 * SS, 0), max(oy + 4 * SS, 0)),
                         (max(-(ox + 3 * SS), 0), max(-(oy + 4 * SS), 0)))
    icon.alpha_composite(canvas, (max(ox, 0), max(oy, 0)), (max(-ox, 0), max(-oy, 0)))
    mask = Image.new("L", (px, px), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, px - 1, px - 1), radius=22 * SS, fill=255)
    icon.putalpha(mask)
    return icon.resize((SIZE, SIZE), Image.LANCZOS)


def meta_text(name):
    with open(META_TEMPLATE, encoding="utf-8") as f:
        lines = f.read().split("\n")
    head = lines[:lines.index("  spriteSheet:")]
    tail = lines[next(i for i, l in enumerate(lines) if l.startswith("  spritePackingTag:")):]
    guid = hashlib.md5(("ms-t3-icon:" + name).encode()).hexdigest()
    out = []
    skipping = False
    for l in head:
        if l.startswith("guid:"):
            l = "guid: " + guid
        elif l.startswith("  internalIDToNameTable:"):
            out.append("  internalIDToNameTable: []")
            skipping = True
            continue
        elif skipping:
            if l.startswith("  externalObjects"):
                skipping = False
            else:
                continue
        elif l.startswith("  spriteMode:"):
            l = "  spriteMode: 1"
        out.append(l)
    sprite_id = hashlib.md5(("ms-t3-sprite:" + name).encode()).hexdigest()
    out += [
        "  spriteSheet:",
        "    serializedVersion: 2",
        "    sprites: []",
        "    outline: []",
        "    physicsShape: []",
        "    bones: []",
        "    spriteID: " + sprite_id,
        "    internalID: 0",
        "    vertices: []",
        "    indices: ",
        "    edges: []",
        "    weights: []",
        "    secondaryTextures: []",
        "    nameFileIdTable: {}",
    ]
    return "\n".join(out + tail)


def main():
    os.makedirs(OUT, exist_ok=True)
    icons = []
    for name, render, length, tip in ICONS:
        icon = make(render, length, tip)
        path = os.path.join(OUT, name + ".png")
        icon.save(path)
        if not os.path.exists(path + ".meta"):
            with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
                f.write(meta_text(name))
        icons.append(icon)
        print("wrote", path)
    if "--sheet" in sys.argv:
        sheet = Image.new("RGBA", (SIZE * 8 + 90, SIZE + 20), (30, 30, 40, 255))
        for i, icon in enumerate(icons):
            sheet.alpha_composite(icon, (10 + i * (SIZE + 10), 10))
        sheet.save(sys.argv[sys.argv.index("--sheet") + 1])


if __name__ == "__main__":
    main()
