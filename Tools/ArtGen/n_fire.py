from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("fire")


def flame(s, x, y, h, w=2.5, mirror=False):
    s.poly([(x, y), (x - w, y + h), (x + w, y + h)], "flame", mirror)
    s.poly([(x, y + h * 0.4), (x - w * 0.5, y + h), (x + w * 0.5, y + h)], "core", mirror)


# ---------- 성냥쥐 MatchMouse (1골드) ----------
def matchmouse(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 27), (25, 24), (26, 21), (27, 22), (26, 25), (21, 28)], "brown")
        flame(s, 26.5, 17, 4, 1.8)
        s.ellipse(11, 16, 3, 3, "body", True)
        s.ellipse(11, 16, 1.6, 1.6, "pink", True)
        s.ellipse(16, 21, 5.5, 4.5, "body")
        s.ellipse(16, 23, 2.5, 1.5, "belly")
        s.ellipse(16, 26.5, 4, 2.5, "body")
        s.rect(13, 28, 13, 28, "body", True)
        eyes(s, 13, 20, "small"); s.fix(15, 23, K); s.fix(16, 23, K); cheeks(s, 12, 22)
    elif lv == 2:
        flame(s, 16, 1, 6, 2.5)
        s.ellipse(16, 8, 2, 1.5, "dark")
        s.poly([(20, 27), (28, 22), (29, 18), (30, 19), (29, 24), (21, 29)], "brown")
        flame(s, 29.5, 12, 6, 2)
        s.ellipse(9.5, 10, 3.5, 3.5, "body", True)
        s.ellipse(9.5, 10, 2, 2, "pink", True)
        s.ellipse(16, 14, 7, 6, "body")
        s.ellipse(16, 17, 3, 2, "belly")
        s.ellipse(16, 23.5, 6, 5.5, "body")
        s.ellipse(16, 24, 3, 3.5, "belly")
        s.rect(11, 28, 13, 29, "body", True)
        eyes(s, 12, 12); s.fix(15, 16, K); s.fix(16, 16, K); cheeks(s, 10, 15)
    else:
        flame(s, 16, 0, 8, 3.5)
        flame(s, 6, 1, 6, 2.5, True)
        s.poly([(20, 26), (30, 20), (31, 15), (31, 24), (22, 29)], "brown")
        flame(s, 29, 7, 8, 2.6)
        s.ellipse(8, 9, 4, 4, "body", True)
        s.ellipse(8, 9, 2.2, 2.2, "flame", True)
        s.ellipse(16, 13.5, 8, 6.5, "body")
        s.ellipse(16, 17, 3.5, 2, "belly")
        s.ellipse(16, 23.5, 8, 6.5, "body")
        s.ellipse(16, 24, 4, 4.5, "belly")
        s.ellipse(8, 22, 2, 3, "body", True)
        s.rect(10, 28, 13, 30, "body", True)
        s.rect(10, 30, 13, 30, "dark", True)
        eyes(s, 11, 11, "fierce"); mouth(s, 16, "fang")
    return s.render(P)


# ---------- 숯불도마뱀 CinderLizard (2골드) ----------
def cinderlizard(lv):
    s = Spr()
    sc = "gray"
    if lv == 1:
        s.poly([(20, 27), (27, 26), (28, 23), (29, 27), (22, 29)], sc)
        s.ellipse(16, 20, 6.5, 4.5, sc)
        s.ellipse(16, 26, 5, 3.5, sc)
        s.ellipse(16, 26.5, 3, 2, "ember")
        s.rect(10, 27, 11, 28, sc, True)
        eyes(s, 12, 18, "small"); mouth(s, 22, "smile")
        s.fix(13, 16, (255, 160, 60), True)
    elif lv == 2:
        for x in (12, 16, 20):
            s.poly([(x, 6), (x - 1.5, 9), (x + 1.5, 9)], "flame")
        s.poly([(20, 25), (29, 23), (31, 18), (31, 25), (22, 29)], sc)
        flame(s, 31, 13, 5, 1.5)
        s.ellipse(16, 13, 8.5, 5.5, sc)
        s.ellipse(16, 22.5, 7, 6, sc)
        s.ellipse(16, 23, 4, 4.5, "ember")
        s.ellipse(14, 22, 1, 1, "core"); s.ellipse(18, 25, 1, 1, "core")
        s.poly([(9, 20), (4, 24), (6, 26), (10, 24)], sc, True)
        s.rect(9, 27, 12, 29, sc, True)
        eyes(s, 10, 11, "cute"); mouth(s, 16, "smile")
    else:
        spikes(s, 16, 12, 9, 13, 9, "flame", ry_scale=0.8)
        spikes(s, 16, 12, 8, 10, 9, "core", rot=0.35, ry_scale=0.8)
        s.poly([(20, 25), (30, 20), (31, 13), (31, 25), (22, 30)], sc)
        s.ellipse(16, 12.5, 9.5, 6, sc)
        s.ellipse(16, 22.5, 9, 7, sc)
        s.ellipse(16, 23, 5, 5, "ember")
        s.ellipse(16, 23, 2.5, 2.5, "core")
        s.poly([(8, 18), (2, 24), (4, 27), (9, 23)], sc, True)
        s.rect(8, 27, 12, 30, sc, True)
        s.rect(8, 30, 12, 30, "dark", True)
        eyes(s, 9, 10, "fierce"); mouth(s, 15, "fang")
    return s.render(P)


# ---------- 불꽃늑대 BlazeWolf (4골드) ----------
def blazewolf(lv):
    s = Spr()
    if lv == 1:
        s.poly([(9, 11), (11, 17), (14, 14)], "body", True)
        s.ellipse(16, 18, 6, 4.5, "body")
        s.ellipse(16, 20.5, 2.5, 1.8, "belly")
        flame(s, 16, 20, 4, 4)
        s.ellipse(16, 25.5, 4.5, 3.5, "body")
        s.rect(12, 28, 13, 29, "body", True)
        eyes(s, 12, 17, "small"); s.fix(15, 20, K); s.fix(16, 20, K)
    elif lv == 2:
        s.poly([(21, 25), (29, 16), (28, 23), (31, 21), (25, 29)], "flame")
        s.poly([(7, 3), (10, 11), (13.5, 7)], "body", True)
        s.poly([(8, 5), (10, 9), (12, 7)], "dark", True)
        spikes(s, 16, 17, 6, 9, 10, "flame", ry_scale=0.7)
        s.ellipse(16, 11, 7, 5.5, "body")
        s.poly([(12, 12), (16, 18), (20, 12)], "belly")
        s.ellipse(16, 23, 6, 6, "body")
        s.ellipse(16, 23, 3, 4, "belly")
        s.rect(10, 27, 12, 30, "body", True)
        eyes(s, 11, 9, "sharp", (255, 220, 80)); s.fix(15, 16, K); s.fix(16, 16, K)
    else:
        s.poly([(20, 24), (31, 10), (29, 19), (31, 18), (26, 29)], "flame")
        s.poly([(22, 24), (29, 15), (27, 22), (25, 27)], "core")
        s.poly([(5, 0), (9, 10), (13.5, 6)], "body", True)
        s.poly([(6, 2), (9, 8), (12, 6)], "dark", True)
        spikes(s, 16, 16, 8, 13, 11, "flame", ry_scale=0.75)
        spikes(s, 16, 16, 7, 10, 11, "core", rot=0.3, ry_scale=0.75)
        s.ellipse(16, 10, 8, 6, "body")
        s.poly([(11, 11), (16, 18), (21, 11)], "belly")
        s.ellipse(16, 23, 8, 6.5, "body")
        s.ellipse(16, 23, 4, 4.5, "belly")
        s.rect(8, 27, 12, 31, "body", True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 10, 8, "fierce"); mouth(s, 15, "fang")
    return s.render(P)


# ---------- 태양불사조 SunPhoenix (5골드) ----------
def sunphoenix(lv):
    s = Spr()
    if lv == 1:
        flame(s, 16, 11, 5, 2)
        s.ellipse(16, 21, 6, 5.5, "body")
        s.ellipse(16, 23.5, 3.5, 2.5, "belly")
        s.poly([(10, 20), (6, 18), (7, 23), (10, 24)], "flame", True)
        s.poly([(15, 21), (17, 21), (16, 23)], "core")
        s.rect(14, 26, 14, 28, "ember", True)
        eyes(s, 13, 19, "small"); cheeks(s, 11, 22)
    elif lv == 2:
        flame(s, 16, 1, 8, 2.5)
        flame(s, 12, 4, 6, 1.8, True)
        s.poly([(10, 14), (1, 8), (2, 13), (0, 17), (4, 19), (2, 22), (10, 22)], "flame", True)
        s.poly([(10, 16), (4, 13), (5, 17), (4, 20), (10, 20)], "core", True)
        s.ellipse(16, 16, 7, 6.5, "body")
        s.ellipse(16, 20, 4, 3.5, "belly")
        s.poly([(14, 16), (18, 16), (16, 19)], "core")
        s.poly([(13, 22), (16, 31), (19, 22)], "flame")
        s.rect(13, 23, 13, 27, "ember", True)
        eyes(s, 12, 13); cheeks(s, 11, 17)
    else:
        spikes(s, 16, 14, 9, 15, 10, "ember", ry_scale=0.9)
        flame(s, 16, 0, 9, 3)
        flame(s, 11, 2, 7, 2, True)
        s.poly([(10, 12), (0, 2), (1, 9), (0, 14), (2, 18), (0, 22), (10, 22)], "flame", True)
        s.poly([(10, 14), (3, 7), (4, 12), (3, 16), (5, 19), (10, 19)], "body", True)
        s.ellipse(16, 15, 7.5, 7, "body")
        s.ellipse(16, 19.5, 4.5, 4, "belly")
        s.poly([(13.5, 14), (18.5, 14), (16, 18)], "core")
        s.poly([(11, 21), (16, 31), (21, 21)], "flame")
        s.poly([(13, 22), (16, 29), (19, 22)], "core")
        s.rect(12, 22, 13, 27, "ember", True)
        eyes(s, 11, 11, "fierce")
    return s.render(P)


CREATURES = [("MatchMouse", "성냥쥐", matchmouse), ("CinderLizard", "숯불도마뱀", cinderlizard),
             ("BlazeWolf", "불꽃늑대", blazewolf), ("SunPhoenix", "태양불사조", sunphoenix)]
