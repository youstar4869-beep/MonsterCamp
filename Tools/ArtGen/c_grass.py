from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("grass")


def leaf(s, x0, y0, x1, y1, c="leaf", w=2.2, mirror=False):
    """Leaf from (x0,y0) base to (x1,y1) tip."""
    import math
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy)
    nx, ny = -dy / L * w, dx / L * w
    mx, my = x0 + dx * 0.45, y0 + dy * 0.45
    s.poly([(x0, y0), (mx + nx, my + ny), (x1, y1), (mx - nx, my - ny)], c, mirror)


# ---------- 새싹토리 Sproutee: an acorn sprout ----------
def sproutee(lv):
    s = Spr()
    if lv == 1:
        s.rect(15, 11, 16, 15, "leaf")
        leaf(s, 15, 12, 10, 9, w=2)
        leaf(s, 16, 12, 21, 9, w=2)
        s.ellipse(16, 18, 6, 3, "wood")
        s.ellipse(16, 23, 6, 6, "orange")
        eyes(s, 13, 21, "small"); mouth(s, 25); cheeks(s, 12, 24)
    elif lv == 2:
        s.rect(15, 5, 16, 9, "leaf")
        leaf(s, 15, 7, 7, 2, w=2.8)
        leaf(s, 16, 7, 24, 2, w=2.8)
        s.ellipse(16, 12, 9.5, 4, "wood")
        for x in range(9, 24, 3):
            s.fix(x, 11, (110, 72, 46))
        s.ellipse(16, 20, 8.5, 8.5, "orange")
        s.ellipse(16, 24, 4.5, 3, "belly")
        leaf(s, 8, 21, 3, 24, w=1.6, mirror=True)
        s.rect(12, 28, 13, 29, "wood", True)
        eyes(s, 12, 17); mouth(s, 21, "smile"); cheeks(s, 11, 20)
    else:
        for (a, b) in ((16, 2),):
            pass
        spikes(s, 16, 4.5, 2, 5, 5, "flower")
        s.ellipse(16, 4.5, 1.6, 1.6, "orange")
        leaf(s, 13, 7, 4, 3, w=3)
        leaf(s, 19, 7, 28, 3, w=3)
        s.ellipse(16, 11, 11, 4.5, "wood")
        for x in range(7, 26, 3):
            s.fix(x, 10, (110, 72, 46))
        s.ellipse(16, 19.5, 10, 9.5, "orange")
        s.ellipse(16, 23.5, 6, 4, "belly")
        leaf(s, 7, 19, 1, 24, w=2, mirror=True)
        s.rect(10, 28, 13, 30, "wood", True)
        eyes(s, 11, 16, "sharp"); mouth(s, 21, "w")
    return s.render(P)


# ---------- 덩굴여우 VineFox ----------
def vinefox(lv):
    s = Spr()
    if lv == 1:
        leaf(s, 21, 26, 28, 18, w=3)
        s.poly([(10, 11), (11, 17), (14.5, 14)], "body", True)
        s.ellipse(16, 18, 6, 5, "body")
        s.ellipse(16, 20.5, 3, 2, "belly")
        s.ellipse(16, 25.5, 4.5, 3.5, "body")
        s.ellipse(16, 26, 2.2, 2.2, "belly")
        s.rect(12, 29, 13, 29, "body", True)
        eyes(s, 13, 17, "small"); mouth(s, 21); cheeks(s, 12, 19)
    elif lv == 2:
        leaf(s, 21, 27, 30, 13, w=4)
        leaf(s, 23, 24, 29, 22, "body", w=1.5)
        s.poly([(7, 3), (9, 11), (13.5, 8)], "body", True)
        s.poly([(8, 5), (9.5, 9), (11.5, 8)], "leaf", True)
        s.ellipse(16, 12, 7, 5.5, "body")
        s.poly([(10, 14), (16, 18), (22, 14)], "belly")
        s.ellipse(16, 22.5, 6, 6, "body")
        s.ellipse(16, 23, 3.2, 4.2, "belly")
        s.rect(11, 27, 12, 29, "body", True)
        leaf(s, 12, 18, 8, 17, w=1.3)
        eyes(s, 12, 10); mouth(s, 16); cheeks(s, 10, 14)
    else:
        leaf(s, 20, 28, 31, 8, w=4.5)
        leaf(s, 22, 28, 31, 20, "leaf", w=3)
        s.poly([(6, 0), (8, 9), (13.5, 6)], "body", True)
        s.poly([(7, 2), (8.5, 7), (11.5, 6)], "leaf", True)
        spikes(s, 16, 15.5, 6, 9, 8, "leaf")
        s.ellipse(16, 10.5, 7.5, 6, "body")
        s.poly([(9, 13), (16, 18), (23, 13)], "belly")
        s.ellipse(16, 23, 7, 6.5, "body")
        s.ellipse(16, 23.5, 3.5, 4.5, "belly")
        s.ellipse(19, 5, 1.5, 1.5, "flower")
        s.rect(10, 28, 13, 30, "body", True)
        s.rect(10, 30, 13, 30, "dark", True)
        eyes(s, 11, 9, "sharp"); mouth(s, 15, "fang")
    return s.render(P)


# ---------- 숲뿔사슴 GroveStag ----------
def antler(s, base, lv):
    bx, by = base
    if lv == 1:
        s.rect(bx, by - 3, bx, by, "wood", True)
        return
    h = 6 if lv == 2 else 8
    s.rect(bx, by - h, bx, by, "wood", True)
    s.rect(bx - 2, by - h + 1, bx, by - h + 1, "wood", True)
    s.rect(bx - 2, by - h - 1, bx - 2, by - h + 1, "wood", True)
    s.rect(bx - 3, by - 3, bx, by - 3, "wood", True)
    if lv == 3:
        s.rect(bx - 4, by - 5, bx - 3, by - 3, "wood", True)
        s.rect(bx + 1, by - h - 1, bx + 1, by - h + 2, "wood", True)


def grovestag(lv):
    s = Spr()
    if lv == 1:
        antler(s, (12, 12), 1)
        s.poly([(8, 13), (12, 15), (10, 17)], "orange", True)
        s.ellipse(16, 16, 5, 4.5, "orange")
        s.ellipse(16, 19, 2.5, 1.6, "belly")
        s.ellipse(16, 24.5, 5, 4, "orange")
        s.fix(14, 23, (255, 240, 220)); s.fix(18, 25, (255, 240, 220))
        s.rect(12, 28, 13, 29, "orange", True)
        eyes(s, 13, 15, "small"); mouth(s, 19); cheeks(s, 12, 18)
    elif lv == 2:
        antler(s, (12, 8), 2)
        leaf(s, 9, 3, 6, 1, w=1.2, mirror=True)
        s.poly([(6, 9), (11, 11), (8, 13)], "orange", True)
        s.ellipse(16, 12, 6, 5, "orange")
        s.ellipse(16, 15, 3, 2, "belly")
        s.ellipse(16, 22.5, 7, 6, "orange")
        s.ellipse(16, 21, 3.5, 3, "belly")
        for x, y in ((12, 24), (19, 23), (15, 26)):
            s.fix(x, y, (255, 240, 220))
        s.rect(10, 27, 11, 30, "orange", True)
        eyes(s, 12, 10); mouth(s, 15); cheeks(s, 11, 13)
    else:
        antler(s, (12, 8), 3)
        for (x, y) in ((7, 2), (9, 0), (8, 5), (11, 0)):
            s.ellipse(x + .5, y + .5, 1.6, 1.4, "leaf", True)
        s.ellipse(9.5, 0.8, 0.8, 0.8, "flower", True)
        spikes(s, 16, 19.5, 5, 8, 8, "leaf")
        s.ellipse(16, 23, 9, 6.5, "orange")
        s.ellipse(16, 21.5, 4, 2.5, "belly")
        s.poly([(5, 9), (11, 11), (8, 14)], "orange", True)
        s.ellipse(16, 12, 6.5, 5.5, "orange")
        s.ellipse(16, 15.5, 3, 2, "belly")
        for x, y in ((11, 24), (20, 23), (15, 26), (18, 27)):
            s.fix(x, y, (255, 240, 220))
        s.rect(9, 27, 11, 31, "orange", True)
        s.rect(9, 31, 11, 31, "wood", True)
        eyes(s, 11, 10, "sharp"); mouth(s, 15)
    return s.render(P)


CREATURES = [("Sproutee", "새싹토리", sproutee), ("VineFox", "덩굴여우", vinefox),
             ("GroveStag", "숲뿔사슴", grovestag)]
