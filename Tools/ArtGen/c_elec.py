from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("elec")


def bolt(s, x, y, size=1, c="bolt", mirror=False):
    k = size
    s.poly([(x + 2 * k, y), (x, y + 3 * k), (x + 1.5 * k, y + 3 * k), (x + 0.5 * k, y + 6 * k),
            (x + 3 * k, y + 2.5 * k), (x + 1.6 * k, y + 2.5 * k), (x + 2.8 * k, y)], c, mirror)


# ---------- 찌릿새 Zappy: a round spark-chick ----------
def zappy(lv):
    s = Spr()
    if lv == 1:
        bolt(s, 14.5, 10, 0.9)
        s.ellipse(16, 21.5, 6.5, 6, "body")
        s.ellipse(16, 24, 3.5, 2.5, "belly")
        s.poly([(9.5, 20), (7, 23), (10, 24)], "wing", True)
        s.poly([(15, 22), (17, 22), (16, 23.5)], "dark")
        s.rect(14, 28, 14, 28, "dark", True)
        eyes(s, 13, 19, "small"); cheeks(s, 11, 22)
        s.fix(15, 22, (255, 140, 40)); s.fix(16, 22, (255, 140, 40)); s.fix(15, 23, (230, 110, 30)); s.fix(16, 23, (230, 110, 30))
    elif lv == 2:
        bolt(s, 13.5, 2, 1.4)
        bolt(s, 9, 5, 0.8, mirror=True)
        s.ellipse(16, 18.5, 9, 8.5, "body")
        s.ellipse(16, 22, 5, 4, "belly")
        s.poly([(8, 15), (1, 17), (3, 21), (8, 22)], "wing", True)
        s.poly([(2, 18), (4, 20), (6, 19)], "dark", True)
        s.rect(13, 27, 13, 29, "dark", True)
        eyes(s, 12, 15); cheeks(s, 10, 19)
        for (x, y, c) in ((15, 18, (255, 140, 40)), (16, 18, (255, 140, 40)), (15, 19, (230, 110, 30)), (16, 19, (230, 110, 30))):
            s.fix(x, y, c)
    else:
        bolt(s, 12.5, 0, 2)
        s.poly([(9, 13), (0, 3), (2, 9), (0, 13), (3, 16), (0, 20), (8, 21)], "wing", True)
        s.poly([(8, 14), (2, 6), (3, 11), (2, 14), (5, 16), (3, 19), (8, 19)], "dark", True)
        bolt(s, 1, 22, 1, mirror=True)
        s.ellipse(16, 17.5, 8.5, 8.5, "body")
        s.ellipse(16, 21.5, 5, 4, "belly")
        s.rect(12, 26, 13, 30, "dark", True)
        s.rect(11, 30, 13, 30, "dark", True)
        eyes(s, 11, 13, "fierce", (120, 200, 255))
        for (x, y, c) in ((15, 17, (255, 140, 40)), (16, 17, (255, 140, 40)), (15, 18, (230, 110, 30)), (16, 18, (230, 110, 30)), (15, 19, (230, 110, 30))):
            s.fix(x, y, c)
    return s.render(P)


# ---------- 번쩍족제비 FlashWeasel: silver ermine with lightning stripes ----------
def flashweasel(lv):
    s = Spr()
    sc = {1: 0, 2: 1, 3: 2}[lv]
    # long zigzag tail on the right
    if lv == 1:
        s.poly([(20, 26), (27, 21), (25, 24), (29, 22), (24, 28)], "bolt")
        s.ellipse(11.5, 14, 1.8, 1.8, "wing", True)
        s.ellipse(16, 18, 5.5, 4.5, "wing")
        s.ellipse(16, 25, 4, 4, "wing")
        s.ellipse(16, 20, 2.5, 1.5, "belly")
        s.ellipse(16, 25.5, 2, 2.5, "belly")
        s.rect(13, 29, 13, 29, "wing", True)
        eyes(s, 13, 17, "small"); mouth(s, 20)
    elif lv == 2:
        s.poly([(20, 25), (30, 14), (27, 21), (31, 19), (24, 29)], "bolt")
        s.poly([(25, 20), (28, 17), (27, 20)], "body")
        s.ellipse(10.5, 7.5, 2.2, 2.2, "wing", True)
        s.ellipse(16, 12, 6.5, 5, "wing")
        s.ellipse(16, 14.5, 3.5, 2, "belly")
        s.rect(12, 16, 19, 26, "wing")
        s.ellipse(16, 21, 2.5, 4, "belly")
        bolt(s, 14.5, 18, 0.9, "bolt")
        s.ellipse(9.5, 20, 1.6, 2.5, "wing", True)
        s.rect(12, 26, 13, 29, "wing", True)
        eyes(s, 12, 10); mouth(s, 14)
    else:
        s.poly([(19, 26), (31, 8), (27, 18), (31, 15), (23, 30)], "bolt")
        s.poly([(25, 17), (29, 11), (27, 17)], "body")
        bolt(s, 0, 16, 1.2)
        s.ellipse(9.5, 4.5, 2.5, 2.5, "wing", True)
        s.ellipse(16, 10, 7, 5.5, "wing")
        s.ellipse(16, 13, 4, 2, "belly")
        s.poly([(16, 2), (14, 6), (18, 6)], "body")
        s.rect(11, 14, 20, 26, "wing")
        s.ellipse(16, 20, 3, 5, "belly")
        bolt(s, 13.6, 16, 1.4, "bolt")
        s.ellipse(8.5, 19, 2, 3.5, "wing", True)
        s.rect(11, 26, 13, 30, "wing", True)
        s.rect(11, 30, 13, 30, "cloud", True)
        eyes(s, 11, 8, "sharp", (120, 200, 255)); mouth(s, 13, "fang")
    return s.render(P)


def feathers(s, n, top, bottom, reach, c="cloud", tip="dark"):
    """Fan of feathers on the left side (mirrored)."""
    for i in range(n):
        y0 = top + i * (bottom - top) / max(1, n - 1)
        x_tip = reach + i * 0.6
        s.poly([(10, y0 - 1.5), (x_tip, y0 - 1), (x_tip + 1, y0 + 1.2), (10, y0 + 2)], c, True)
        s.poly([(x_tip, y0 - 1), (x_tip + 2.2, y0), (x_tip + 1, y0 + 1.2)], tip, True)


# ---------- 뇌운매 StormHawk ----------
def stormhawk(lv):
    s = Spr()
    beak = "body"
    if lv == 1:
        bolt(s, 14.5, 11, 0.8)
        s.ellipse(16, 20.5, 5.5, 5, "blue")
        s.ellipse(16, 23, 3, 2.5, "belly")
        s.poly([(10, 19), (6, 22), (10, 24)], "cloud", True)
        s.poly([(15, 20), (17, 20), (16, 22)], beak)
        s.rect(13, 26, 14, 27, "body", True)
        eyes(s, 13, 18, "small")
    elif lv == 2:
        bolt(s, 14, 2, 1.2)
        bolt(s, 10.5, 5, 0.7, mirror=True)
        feathers(s, 4, 11, 20, 2)
        s.ellipse(16, 15, 7, 6.5, "blue")
        s.ellipse(16, 19, 4, 3.5, "belly")
        s.poly([(14, 15), (18, 15), (16, 19)], beak)
        s.ellipse(16, 24, 4, 3.5, "blue")
        s.poly([(12, 27), (13, 30), (14, 27)], "body", True)
        s.rect(10, 30, 14, 30, "body", True)
        eyes(s, 12, 12, "sharp")
    else:
        bolt(s, 13, 0, 1.7)
        bolt(s, 9, 2, 1.0, mirror=True)
        feathers(s, 6, 7, 23, 0)
        s.ellipse(16, 14, 8, 7.5, "blue")
        s.ellipse(16, 19, 5, 4.5, "belly")
        for y in (17, 19, 21):
            s.rect(14, y, 17, y, "cloud")
        s.poly([(13.5, 13), (18.5, 13), (16, 18)], beak)
        s.ellipse(16, 24, 5.5, 4, "blue")
        s.poly([(11, 26), (12, 31), (14, 27)], "body", True)
        s.rect(9, 31, 14, 31, "body", True)
        eyes(s, 11, 10, "fierce", (255, 250, 150))
    return s.render(P)


CREATURES = [("Zappy", "찌릿새", zappy), ("FlashWeasel", "번쩍족제비", flashweasel),
             ("StormHawk", "뇌운매", stormhawk)]
