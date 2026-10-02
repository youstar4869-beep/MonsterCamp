from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("water")


def bubble(s, x, y, r=1.2):
    s.ellipse(x, y, r, r, "foam")


# ---------- 거품게 BubbleCrab (1골드) ----------
def bubblecrab(lv):
    s = Spr()
    if lv == 1:
        bubble(s, 24, 13, 1.2)
        s.rect(12, 16, 12, 18, "red", True)
        s.ellipse(8, 20, 2.5, 2.2, "red", True)
        s.ellipse(16, 22, 6.5, 4.5, "red")
        s.ellipse(16, 24.5, 3.5, 1.6, "belly")
        for x in (11, 13):
            s.rect(x, 26, x, 28, "red", True)
        eyes(s, 13, 20, "small"); mouth(s, 23); cheeks(s, 12, 22)
    elif lv == 2:
        bubble(s, 26, 6, 1.6); bubble(s, 23, 10, 1)
        s.rect(12, 10, 12, 13, "red", True)
        s.ellipse(12.5, 10, 1.3, 1.3, "red", True)
        s.poly([(8, 18), (3, 14), (2, 18), (5, 20)], "red", True)
        s.ellipse(4, 14, 3, 3, "red", True)
        s.poly([(1, 11), (4, 14), (2, 15)], "red", True)
        s.ellipse(16, 19, 9, 6.5, "red")
        s.ellipse(16, 22, 5, 2.5, "belly")
        for x in (8, 10, 12):
            s.poly([(x, 23), (x - 2, 28), (x - 1, 28), (x + 1, 24)], "red", True)
        eyes(s, 12, 16, "cute")
        mouth(s, 20, "smile"); cheeks(s, 10, 19)
    else:
        bubble(s, 27, 2, 1.8); bubble(s, 24, 6, 1.1); bubble(s, 4, 3, 1.3)
        s.rect(12, 6, 12, 10, "red", True)
        s.ellipse(12.5, 6, 1.4, 1.4, "red", True)
        s.poly([(8, 16), (3, 12), (1, 17), (5, 19)], "red", True)
        s.ellipse(4, 11, 4, 4, "red", True)
        s.poly([(0, 6), (4, 11), (1, 11)], "shell", True)
        s.poly([(5, 7), (6, 11), (8, 9)], "red", True)
        s.ellipse(16, 17, 11, 7.5, "red")
        for x, y in ((8, 14), (16, 11), (24, 14)):
            s.ellipse(x, y, 1.6, 1.2, "shell")
        s.ellipse(16, 21, 6, 3, "belly")
        for x in (7, 9.5, 12):
            s.poly([(x + 1, 21), (x - 2.5, 30), (x - 1, 30), (x + 2.5, 22)], "red", True)
        s.rect(5, 30, 7, 30, "dark", True)
        eyes(s, 11, 15, "fierce", (120, 230, 255))
        mouth(s, 19, "fang")
    return s.render(P)


# ---------- 산호해마 CoralSeahorse (2골드) ----------
def coralseahorse(lv):
    s = Spr()
    if lv == 1:
        s.poly([(17, 13), (14, 11), (12, 13), (15, 15)], "shell")
        s.ellipse(17, 17, 4, 3.5, "body")
        s.poly([(19, 16), (24, 16.5), (24, 18.5), (19, 19)], "body")
        s.ellipse(17, 23, 4, 4, "body")
        s.ellipse(16, 23.5, 2, 2.5, "belly")
        s.poly([(19, 25), (21, 29), (17, 30), (16, 28), (18, 27)], "body")
        s.poly([(13, 21), (11, 19), (11, 24)], "fin")
        s.fix(18, 16, K); s.fix(18, 17, K); s.fix(16, 19, (255, 128, 140))
    elif lv == 2:
        for x, y, h in ((12, 3, 5), (16, 1, 6), (19, 4, 4)):
            s.rect(x, y, x + 1, y + h, "shell")
        s.ellipse(16, 11, 5.5, 4.5, "body")
        s.poly([(19, 10), (27, 10.5), (27, 13), (19, 14)], "body")
        s.ellipse(15, 19, 5.5, 5, "body")
        s.ellipse(13.5, 19.5, 3, 3.5, "belly")
        for y in (17, 19, 21):
            s.rect(12, y, 15, y, "foam")
        s.poly([(18, 22), (22, 27), (20, 30), (15, 30), (14, 27), (17, 28), (18, 26)], "body")
        s.poly([(10, 16), (5, 13), (6, 18), (5, 22), (10, 21)], "fin")
        s.ellipse(9, 28, 1.3, 1.3, "foam")
        eyes(s, 17, 9, "cute")
        s.fix(15, 13, (255, 128, 140))
    else:
        for x, y, h in ((9, 2, 6), (14, 0, 7), (19, 1, 6), (23, 4, 4)):
            s.rect(x, y, x + 1, y + h, "shell")
            s.rect(x - 1, y + 1, x - 1, y + 2, "shell")
        s.ellipse(16, 10, 6.5, 5, "body")
        s.poly([(20, 8), (30, 9), (30, 12.5), (20, 13)], "body")
        s.ellipse(15, 19, 7, 6.5, "body")
        s.ellipse(13, 19.5, 3.5, 4.5, "belly")
        for y in (16, 18, 20, 22):
            s.rect(11, y, 15, y, "foam")
        s.poly([(19, 22), (25, 27), (22, 31), (14, 31), (13, 27), (19, 28), (19, 25)], "body")
        s.poly([(9, 14), (1, 9), (3, 15), (0, 19), (3, 23), (9, 22)], "fin")
        s.poly([(8, 15), (3, 12), (4, 15), (2, 19), (8, 20)], "foam")
        s.ellipse(27, 22, 1.5, 1.5, "foam"); s.ellipse(25, 18, 1, 1, "foam")
        for x in (21, 22, 23):
            s.fix(x, 6 + (x - 21), (255, 180, 200))
        s.fix(16, 8, K); s.fix(17, 9, K); s.fix(18, 9, K); s.fix(17, 10, K); s.fix(18, 10, K)
        s.fix(18, 9, (255, 220, 120))
    return s.render(P)


# ---------- 소용돌이문어 WhirlOctopus (4골드) ----------
def tentacle(s, x0, y0, x1, y1, w, c="purple", mirror=False):
    s.poly([(x0 - w, y0), (x0 + w, y0), (x1 + 0.6, y1), (x1 - 0.6, y1)], c, mirror)


def whirloctopus(lv):
    s = Spr()
    if lv == 1:
        for x0, x1 in ((12, 9), (14.5, 13), (17.5, 19), (20, 23)):
            tentacle(s, x0, 22, x1, 28, 1.2)
        s.ellipse(16, 18, 6.5, 6.5, "purple")
        s.ellipse(14, 15, 1.5, 1.5, "shell")
        eyes(s, 13, 18, "small"); mouth(s, 22, "w"); cheeks(s, 11, 21)
    elif lv == 2:
        s.ellipse(25, 6, 2.5, 2.5, "foam"); s.ellipse(25, 6, 1.2, 1.2, "fin")
        for x0, x1, y1 in ((11, 3, 27), (13, 8, 30), (15, 13, 30)):
            tentacle(s, x0, 19, x1, y1, 1.6, mirror=True)
        s.poly([(3, 27), (1, 24), (2, 22), (4, 25)], "purple", True)
        s.ellipse(16, 13, 9, 9, "purple")
        for x, y in ((11, 8), (20, 7), (22, 12)):
            s.ellipse(x, y, 1.2, 1.2, "shell")
        eyes(s, 12, 13, "cute"); mouth(s, 18, "w"); cheeks(s, 10, 16)
    else:
        # whirlpool behind
        for r, c in ((15, "fin"), (12, "body"), (9, "fin")):
            s.ellipse(16, 22, r, r * 0.45, c)
        for x0, x1, y1 in ((10, 0, 22), (12, 3, 31), (14, 9, 31), (15.5, 14, 31)):
            tentacle(s, x0, 19, x1, y1, 1.9, mirror=True)
        s.poly([(0, 22), (1, 17), (3, 17), (2, 20)], "purple", True)
        s.ellipse(16, 11, 10, 10, "purple")
        s.ellipse(16, 4, 5, 3, "purple")
        for x, y in ((9, 6), (22, 5), (24, 11), (8, 13)):
            s.ellipse(x, y, 1.4, 1.4, "shell")
        eyes(s, 11, 11, "fierce", (120, 230, 255)); mouth(s, 17, "fang")
    return s.render(P)


# ---------- 심해룡 AbyssDragon (5골드) ----------
def abyssdragon(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 26), (26, 24), (28, 20), (29, 25), (22, 29)], "deep")
        s.poly([(27, 21), (30, 19), (29, 23)], "fin")
        s.poly([(10, 13), (12, 9), (13, 14)], "fin", True)
        s.ellipse(16, 18, 6, 5, "deep")
        s.ellipse(16, 20.5, 3, 1.6, "belly")
        s.ellipse(16, 25.5, 4.5, 3.5, "deep")
        s.ellipse(16, 26, 2.5, 2.5, "belly")
        s.rect(12, 28, 13, 29, "deep", True)
        eyes(s, 13, 17, "small", None); mouth(s, 21)
        s.fix(13, 17, (120, 230, 255), True); cheeks(s, 11, 20)
    elif lv == 2:
        s.poly([(20, 25), (29, 22), (31, 16), (31, 24), (22, 29)], "deep")
        s.poly([(29, 17), (31, 13), (31, 19)], "fin")
        s.poly([(9, 8), (10, 2), (13, 7)], "fin", True)
        s.poly([(8, 16), (1, 12), (2, 17), (0, 21), (8, 22)], "fin", True)
        s.ellipse(16, 11, 7.5, 6, "deep")
        s.ellipse(16, 14, 4, 2.5, "belly")
        s.ellipse(16, 22.5, 7, 6.5, "deep")
        s.ellipse(16, 23, 3.5, 5, "belly")
        for y in (20, 23, 26):
            s.rect(14, y, 17, y, "foam")
        s.rect(10, 27, 12, 30, "deep", True)
        eyes(s, 11, 9, "sharp", (120, 230, 255)); mouth(s, 15)
        s.ellipse(16, 4, 1.2, 1.2, "foam")
        s.fix(16, 4, (180, 255, 255))
    else:
        s.poly([(20, 24), (30, 18), (31, 10), (31, 22), (23, 30)], "deep")
        s.poly([(30, 11), (31, 5), (31, 13)], "fin")
        s.poly([(9, 13), (0, 2), (0, 9), (1, 12), (0, 16), (2, 20), (9, 21)], "fin", True)
        s.poly([(8, 14), (2, 6), (2, 11), (3, 14), (2, 17), (8, 19)], "deep", True)
        s.poly([(10, 6), (8, 0), (13, 4)], "fin", True)
        s.ellipse(16, 10, 8, 6.5, "deep")
        s.poly([(11, 11), (16, 18), (21, 11)], "belly")
        s.ellipse(16, 22.5, 8.5, 7, "deep")
        s.ellipse(16, 23, 4, 5.5, "belly")
        for y in (19, 22, 25):
            s.rect(13, y, 18, y, "foam")
        s.rect(8, 27, 12, 31, "deep", True)
        s.rect(8, 31, 12, 31, "dark", True)
        # glowing lure
        s.rect(16, 0, 16, 3, "dark")
        s.fix(16, 0, (180, 255, 255)); s.fix(15, 0, (120, 230, 255)); s.fix(17, 0, (120, 230, 255))
        eyes(s, 10, 8, "fierce", (120, 230, 255)); mouth(s, 14, "fang")
    return s.render(P)


CREATURES = [("BubbleCrab", "거품게", bubblecrab), ("CoralSeahorse", "산호해마", coralseahorse),
             ("WhirlOctopus", "소용돌이문어", whirloctopus), ("AbyssDragon", "심해룡", abyssdragon)]
