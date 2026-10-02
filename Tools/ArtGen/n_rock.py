from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W
from c_rock import crystal

P = pal("rock")
SNOW = "horn"


# ---------- 모래두더지 SandMole (1골드) ----------
def sandmole(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(16, 27, 9, 2.5, "belly")
        s.ellipse(16, 21, 5.5, 5.5, "body")
        s.ellipse(16, 23, 2.5, 1.8, "pink")
        s.ellipse(10.5, 24, 1.8, 1.4, "pink", True)
        eyes(s, 13, 19, "closed"); s.fix(15, 22, K); s.fix(16, 22, K)
    elif lv == 2:
        s.ellipse(16, 27, 13, 3.5, "belly")
        for x in (6, 26):
            s.ellipse(x, 25, 1.5, 1.2, "stone")
        s.ellipse(16, 17, 8, 8.5, "body")
        s.ellipse(16, 21, 4, 3, "belly")
        s.ellipse(16, 19.5, 2, 1.4, "pink")
        s.poly([(8, 20), (3, 18), (2, 22), (4, 24), (8, 23)], "pink", True)
        for x in (2, 4):
            s.fix(x, 24, W, True)
        eyes(s, 12, 14, "closed"); mouth(s, 22, "w")
    else:
        s.ellipse(16, 28, 15, 3.5, "belly")
        for x, y in ((3, 26), (28, 26), (8, 29), (24, 29)):
            s.ellipse(x, y, 1.6, 1.3, "stone")
        s.ellipse(16, 15, 10, 10.5, "body")
        s.ellipse(16, 6, 3, 1.5, "stone")
        s.ellipse(16, 20, 5, 4, "belly")
        s.ellipse(16, 18, 2.5, 1.6, "pink")
        s.poly([(7, 17), (0, 13), (0, 20), (2, 23), (7, 22)], "pink", True)
        for x in (0, 2, 4):
            s.fix(x, 23, W, True); s.fix(x, 24, W, True)
        crystal(s, 23, 2, 5)
        eyes(s, 10, 12, "fierce", (255, 180, 60)); mouth(s, 21, "fang")
    return s.render(P)


# ---------- 수정고슴도치 CrystalHog (2골드) ----------
def crystalhog(lv):
    s = Spr()
    if lv == 1:
        for x in (11, 16, 21):
            crystal(s, x, 13, 6)
        s.ellipse(16, 22, 7, 5.5, "stone")
        s.ellipse(16, 24, 4.5, 3.5, "belly")
        s.rect(12, 27, 13, 28, "belly", True)
        eyes(s, 13, 22, "small"); s.fix(15, 25, K); s.fix(16, 25, K); cheeks(s, 12, 24)
    elif lv == 2:
        for x, y in ((6, 10), (11, 6), (16, 4), (21, 6), (26, 10)):
            crystal(s, x, y, 8)
        s.ellipse(16, 18, 11, 9, "stone")
        for x, y in ((8, 14), (24, 14), (16, 11)):
            s.fix(x, y, (150, 140, 128))
        s.ellipse(16, 21, 6.5, 6, "belly")
        s.ellipse(16, 23, 2, 1.4, "dark")
        s.rect(9, 27, 12, 29, "belly", True)
        eyes(s, 12, 19); cheeks(s, 11, 22)
    else:
        for x, y, h in ((2, 9, 10), (6, 4, 11), (11, 1, 11), (16, 0, 11), (21, 1, 11), (26, 4, 11), (30, 9, 10)):
            crystal(s, x, y, h)
        s.ellipse(16, 18, 13, 10, "stone")
        s.ellipse(16, 21, 7.5, 7, "belly")
        s.ellipse(16, 23, 2.2, 1.5, "dark")
        crystal(s, 2, 20, 6, True)
        s.rect(8, 27, 12, 31, "belly", True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 11, 18, "fierce", (180, 160, 255)); mouth(s, 25, "fang")
    return s.render(P)


# ---------- 바위골렘 StoneGolem (4골드) ----------
def core(s, cx, cy, r):
    s.ellipse(cx, cy, r, r, "glow")


def stonegolem(lv):
    s = Spr()
    if lv == 1:
        s.rect(11, 15, 20, 27, "stone")
        s.rect(10, 16, 21, 26, "stone")
        core(s, 16, 23, 1.6)
        s.rect(8, 19, 9, 23, "stone", True)
        s.rect(11, 28, 13, 28, "dark", True)
        s.ellipse(13, 15, 1.5, 0.8, "moss")
        eyes(s, 13, 18, "small"); s.fix(14, 18, (255, 200, 80), True)
    elif lv == 2:
        s.rect(10, 4, 21, 12, "stone")
        s.rect(7, 13, 24, 25, "stone")
        s.ellipse(12, 4, 2.5, 1, "moss")
        s.rect(2, 13, 6, 22, "stone", True)
        s.rect(2, 22, 6, 24, "dark", True)
        core(s, 16, 18, 2.6)
        s.rect(9, 26, 13, 30, "stone", True)
        s.rect(9, 30, 13, 30, "dark", True)
        for x, y in ((9, 15), (22, 22), (11, 23)):
            s.fix(x, y, (70, 64, 62))
        eyes(s, 13, 7, "sharp", (255, 200, 80))
        s.rect(14, 10, 17, 10, "dark")
    else:
        crystal(s, 10, 0, 5, True)
        s.rect(10, 3, 21, 11, "stone")
        s.rect(4, 11, 27, 24, "stone")
        s.ellipse(9, 11, 5, 3, "moss", True)
        s.rect(0, 12, 4, 23, "stone", True)
        s.rect(0, 23, 5, 26, "dark", True)
        crystal(s, 1, 7, 5, True)
        core(s, 16, 17, 3.6)
        s.ellipse(16, 17, 1.6, 1.6, "horn")
        s.rect(7, 25, 13, 31, "stone", True)
        s.rect(7, 31, 13, 31, "dark", True)
        for x, y in ((8, 19), (23, 21), (11, 14), (20, 13)):
            s.fix(x, y, (70, 64, 62)); s.fix(x + 1, y + 1, (70, 64, 62))
        eyes(s, 12, 5, "fierce", (255, 200, 80))
        s.rect(13, 9, 18, 9, "glow")
    return s.render(P)


# ---------- 산맥거북 MountainTortoise (5골드) ----------
def mountain(s, cx, base, w, h, snow=True):
    s.poly([(cx, base - h), (cx - w, base), (cx + w, base)], "stone")
    if snow:
        s.poly([(cx, base - h), (cx - w * 0.35, base - h * 0.65), (cx + w * 0.35, base - h * 0.65)], SNOW)


def mountaintortoise(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(16, 21, 7, 4.5, "moss")
        mountain(s, 16, 20, 4, 5)
        s.ellipse(16, 25, 6, 2.5, "body")
        s.rect(10, 27, 11, 28, "body", True)
        s.ellipse(16, 25, 2.5, 2.5, "body")
        eyes(s, 14, 24, "small"); cheeks(s, 13, 26)
    elif lv == 2:
        s.ellipse(16, 18, 12, 7, "moss")
        mountain(s, 11, 17, 5, 8)
        mountain(s, 20, 17, 6, 11)
        s.ellipse(16, 22, 12, 4, "body")
        s.ellipse(16, 22, 4.5, 4, "body")
        s.ellipse(16, 24, 2.5, 1.4, "belly")
        s.rect(5, 25, 8, 29, "body", True)
        s.rect(5, 29, 8, 29, "dark", True)
        eyes(s, 13, 20); cheeks(s, 12, 23)
    else:
        s.ellipse(16, 17, 15.5, 9, "moss")
        mountain(s, 6, 16, 5, 8)
        mountain(s, 25, 16, 6, 10)
        mountain(s, 15, 16, 8, 16)
        crystal(s, 21, 10, 5)
        s.ellipse(16, 22, 15, 4, "body")
        s.ellipse(16, 22.5, 5.5, 5, "body")
        s.ellipse(16, 25, 3, 1.6, "belly")
        s.rect(2, 25, 7, 31, "body", True)
        s.rect(2, 31, 7, 31, "dark", True)
        for x in (3, 5, 7):
            s.fix(x, 31, (250, 246, 232), True)
        eyes(s, 12, 20, "fierce", (130, 200, 255)); mouth(s, 26, "fang")
    return s.render(P)


CREATURES = [("SandMole", "모래두더지", sandmole), ("CrystalHog", "수정고슴도치", crystalhog),
             ("StoneGolem", "바위골렘", stonegolem), ("MountainTortoise", "산맥거북", mountaintortoise)]
