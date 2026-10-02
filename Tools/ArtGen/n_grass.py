from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W
from c_grass import leaf

P = pal("grass")


def spiral(s, cx, cy, r):
    """Shell with a darker spiral line."""
    s.ellipse(cx, cy, r, r, "moss")
    s.ellipse(cx, cy, r * 0.62, r * 0.62, "wood")
    s.ellipse(cx, cy, r * 0.3, r * 0.3, "moss")


# ---------- 이끼달팽이 MossSnail (1골드) ----------
def mosssnail(lv):
    s = Spr()
    if lv == 1:
        spiral(s, 19, 21, 5)
        s.ellipse(19, 16, 2, 1, "leaf")
        s.rect(10, 15, 10, 18, "belly"); s.rect(13, 15, 13, 18, "belly")
        s.ellipse(11.5, 21, 3.5, 3.5, "belly")
        s.poly([(8, 24), (26, 24), (26, 27), (8, 27)], "belly")
        eyes(s, 10, 20, "small"); s.fix(13, 20, K); s.fix(13, 21, K)
        s.fix(11, 23, K); s.fix(12, 23, K)
    elif lv == 2:
        spiral(s, 19, 16, 8)
        leaf(s, 18, 9, 14, 3, w=2)
        leaf(s, 20, 9, 25, 4, w=2)
        s.ellipse(22, 10, 1.5, 1.5, "flower")
        s.rect(7, 7, 7, 12, "belly"); s.rect(11, 7, 11, 12, "belly")
        s.ellipse(7, 7, 1.2, 1.2, "belly"); s.ellipse(11, 7, 1.2, 1.2, "belly")
        s.ellipse(9, 16, 5, 5, "belly")
        s.poly([(4, 21), (29, 22), (29, 27), (4, 27)], "belly")
        eyes(s, 6, 14, "cute"); s.fix(10, 14, K); s.fix(11, 14, K); s.fix(10, 15, K); s.fix(11, 15, K); s.fix(10, 16, K); s.fix(11, 16, K); s.fix(10, 14, W)
        s.fix(8, 19, K); s.fix(9, 19, K); s.fix(5, 18, (255, 128, 140))
    else:
        leaf(s, 18, 6, 12, 0, w=2.4)
        leaf(s, 20, 6, 27, 0, w=2.4)
        leaf(s, 19, 6, 19, 0, w=1.6)
        spiral(s, 19, 14, 11)
        for x, y in ((12, 7), (26, 8), (28, 16)):
            s.ellipse(x, y, 1.6, 1.6, "flower")
        s.rect(5, 4, 5, 10, "belly"); s.rect(10, 4, 10, 10, "belly")
        s.ellipse(5, 4, 1.3, 1.3, "flower"); s.ellipse(10, 4, 1.3, 1.3, "flower")
        s.ellipse(7.5, 15, 6, 6, "belly")
        s.poly([(1, 21), (31, 22), (31, 29), (1, 29)], "belly")
        s.rect(1, 29, 31, 29, "moss")
        for x in (4, 9):
            s.fix(x, 12, K); s.fix(x + 1, 13, K); s.fix(x + 2, 13, K); s.fix(x + 1, 14, K); s.fix(x + 2, 14, K)
            s.fix(x + 2, 13, (120, 220, 90))
        s.fix(5, 18, K); s.fix(6, 18, K); s.fix(7, 18, K); s.fix(8, 18, K); s.fix(5, 19, W); s.fix(8, 19, W)
    return s.render(P)


# ---------- 꽃잎나방 PetalMoth (2골드) ----------
def petalmoth(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(10, 19, 4, 3.5, "flower", True)
        s.ellipse(11, 24, 2.5, 2, "flower", True)
        s.ellipse(16, 21, 3.5, 5, "belly")
        s.rect(14, 14, 14, 16, "dark", True)
        eyes(s, 14, 19, "small"); mouth(s, 23)
    elif lv == 2:
        s.poly([(14, 8), (11, 3), (10, 4), (13, 9)], "dark", True)
        s.ellipse(7, 13, 6, 5.5, "flower", True)
        s.ellipse(8, 22, 4.5, 4, "flower", True)
        s.ellipse(6, 13, 2, 2, "orange", True)
        s.ellipse(16, 17, 4.5, 8, "belly")
        for y in (20, 22, 24):
            s.rect(13, y, 18, y, "wood")
        eyes(s, 13, 13, "cute"); mouth(s, 17); cheeks(s, 12, 16)
    else:
        s.poly([(14, 6), (9, 0), (8, 1), (13, 7)], "dark", True)
        s.ellipse(9, 1, 1.2, 1.2, "leaf", True)
        s.poly([(12, 8), (0, 1), (0, 16), (11, 17)], "flower", True)
        s.poly([(12, 18), (1, 20), (3, 29), (12, 24)], "flower", True)
        s.ellipse(5, 8, 2.5, 2.5, "leaf", True)
        s.ellipse(5, 8, 1.2, 1.2, "orange", True)
        s.ellipse(6, 23, 1.5, 1.5, "orange", True)
        s.ellipse(16, 16, 5, 10, "belly")
        s.ellipse(16, 10, 5, 3, "flower")
        for y in (19, 21, 23, 25):
            s.rect(13, y, 18, y, "wood")
        eyes(s, 12, 12, "fierce", (255, 150, 200)); mouth(s, 16, "fang")
    return s.render(P)


# ---------- 덩굴파리지옥 Flytrap (4골드) ----------
def flytrap(lv):
    s = Spr()
    if lv == 1:
        leaf(s, 15, 26, 9, 23, w=1.8)
        leaf(s, 16, 26, 22, 23, w=1.8)
        s.rect(15, 21, 16, 27, "leaf")
        s.ellipse(16, 17, 6, 5, "body")
        s.poly([(10, 17), (22, 17), (21, 19), (11, 19)], "red")
        for x in (11, 13, 18, 20):
            s.fix(x, 17, W)
        s.ellipse(16, 28, 4, 1.5, "wood")
        eyes(s, 13, 13, "small")
    elif lv == 2:
        leaf(s, 14, 26, 4, 21, w=2.6)
        leaf(s, 17, 26, 27, 21, w=2.6)
        s.rect(14, 18, 17, 28, "leaf")
        s.ellipse(16, 11, 9, 7, "body")
        s.poly([(7, 11), (25, 11), (23, 15), (9, 15)], "red")
        s.ellipse(16, 14, 4, 1.5, "flower")
        for x in range(8, 25, 2):
            s.fix(x, 11, W)
        for x in range(10, 23, 3):
            s.fix(x, 15, W)
        s.ellipse(16, 29, 6, 2, "wood")
        eyes(s, 12, 6, "cute")
    else:
        leaf(s, 13, 25, 1, 17, w=3)
        leaf(s, 18, 25, 30, 17, w=3)
        leaf(s, 14, 27, 4, 28, w=2)
        leaf(s, 17, 27, 27, 28, w=2)
        s.poly([(13, 17), (11, 22), (14, 30), (18, 30), (20, 22), (18, 17)], "leaf")
        spikes(s, 16, 10, 10, 13, 12, "dark", ry_scale=0.75)
        s.ellipse(16, 10, 11, 8.5, "body")
        s.poly([(5, 10), (27, 10), (24, 16), (8, 16)], "red")
        s.ellipse(16, 14.5, 5, 1.8, "flower")
        for x in range(6, 27, 2):
            s.fix(x, 10, W); s.fix(x, 11, W) if x % 4 == 0 else None
        for x in range(9, 24, 3):
            s.fix(x, 16, W)
        s.ellipse(16, 30.5, 7, 1.5, "wood")
        eyes(s, 11, 5, "fierce", (255, 220, 80))
    return s.render(P)


# ---------- 고목거인 ElderTreant (5골드) ----------
def eldertreant(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(16, 13, 6, 4, "leaf")
        s.ellipse(13, 12, 1.2, 1.2, "flower")
        s.rect(11, 16, 20, 27, "wood")
        s.rect(10, 27, 12, 28, "wood", True)
        s.rect(14, 24, 17, 24, "dark")
        eyes(s, 13, 19, "small"); mouth(s, 23); cheeks(s, 12, 21)
    elif lv == 2:
        s.ellipse(16, 8, 11, 6.5, "leaf")
        s.ellipse(9, 6, 1.5, 1.5, "flower"); s.ellipse(22, 5, 1.5, 1.5, "flower")
        s.poly([(10, 14), (3, 12), (2, 16), (10, 19)], "wood", True)
        leaf(s, 3, 13, 0, 10, w=1.5, mirror=True)
        s.rect(9, 12, 22, 27, "wood")
        s.rect(13, 21, 13, 25, "dark"); s.rect(18, 20, 18, 24, "dark")
        s.poly([(8, 27), (14, 27), (13, 30), (7, 30)], "wood", True)
        s.ellipse(16, 12, 4, 1.5, "moss")
        eyes(s, 12, 15, "cute"); mouth(s, 20, "smile")
    else:
        s.ellipse(16, 7, 16, 7.5, "leaf")
        s.ellipse(9, 3, 4, 3, "leaf"); s.ellipse(23, 3, 4, 3, "leaf")
        for x, y in ((5, 6), (12, 2), (20, 3), (27, 7), (16, 9)):
            s.ellipse(x, y, 1.5, 1.5, "flower")
        s.poly([(8, 15), (0, 11), (0, 16), (3, 18), (8, 21)], "wood", True)
        s.rect(7, 11, 24, 28, "wood")
        s.poly([(7, 14), (24, 14), (22, 16), (9, 16)], "moss")
        for x, y0, y1 in ((8, 18, 27), (23, 18, 27), (15, 24, 27)):
            s.rect(x, y0, x, y1, "dark")
        s.poly([(5, 27), (13, 27), (13, 31), (3, 31)], "wood", True)
        s.rect(3, 31, 13, 31, "dark", True)
        s.ellipse(16, 30.5, 2.5, 1.5, "moss")
        eyes(s, 10, 17, "fierce", (180, 255, 120)); mouth(s, 22, "fang")
    return s.render(P)


CREATURES = [("MossSnail", "이끼달팽이", mosssnail), ("PetalMoth", "꽃잎나방", petalmoth),
             ("Flytrap", "덩굴파리지옥", flytrap), ("ElderTreant", "고목거인", eldertreant)]
