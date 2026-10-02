from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("fire")


# ---------- 불똥이 Embit: a living spark ----------
def embit(lv):
    s = Spr()
    if lv == 1:
        s.poly([(16, 9), (13.5, 14), (18.5, 14)], "flame")
        s.poly([(13, 11), (12, 15), (15, 15)], "flame")
        s.poly([(19, 11), (20, 15), (17, 15)], "flame")
        s.poly([(16, 12), (15, 14.5), (17, 14.5)], "core")
        s.ellipse(16, 21.5, 7, 6.5, "body")
        s.ellipse(16, 24.5, 4, 3, "belly")
        s.rect(12, 28, 13, 28, "body", True)
        eyes(s, 12, 19); cheeks(s, 11, 22); mouth(s, 23)
    elif lv == 2:
        s.poly([(16, 3), (12, 12), (20, 12)], "flame")
        s.poly([(10, 6), (9, 13), (14, 12)], "flame", True)
        s.poly([(16, 7), (14.5, 12), (17.5, 12)], "core")
        s.ellipse(16, 18.5, 9, 8.5, "body")
        s.ellipse(16, 22, 5, 4, "belly")
        s.poly([(7.5, 18), (3, 21), (4, 23), (8, 22)], "body", True)  # arms
        s.poly([(3, 18), (2, 21), (5, 20)], "flame", True)
        s.rect(11, 26, 13, 28, "body", True)
        eyes(s, 12, 15); cheeks(s, 10, 19); mouth(s, 20, "smile")
    else:
        s.poly([(16, 0), (11, 9), (21, 9)], "flame")
        s.poly([(8, 2), (7, 11), (13, 9)], "flame", True)
        s.poly([(3, 7), (4, 15), (8, 12)], "flame", True)
        s.poly([(16, 4), (13.5, 9), (18.5, 9)], "core")
        s.poly([(8, 6), (8.5, 10), (11, 9)], "core", True)
        s.ellipse(16, 17, 10.5, 9, "body")
        s.ellipse(16, 21, 6, 5, "belly")
        s.ellipse(16, 21, 2.5, 2.5, "ember")
        s.poly([(6, 16), (1, 22), (3, 25), (8, 21)], "body", True)
        s.poly([(2, 19), (0, 25), (4, 25)], "flame", True)
        s.rect(10, 25, 13, 29, "body", True)
        s.rect(10, 29, 13, 29, "dark", True)
        eyes(s, 11, 13, "fierce"); mouth(s, 18, "fang")
    return s.render(P)


# ---------- 화로곰 HearthBear: bear with a furnace belly ----------
def hearthbear(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(10.5, 12.5, 2.5, 2.5, "brown", True)
        s.ellipse(16, 17, 6.5, 5.5, "brown")
        s.ellipse(16, 25, 6, 4.5, "brown")
        s.ellipse(16, 25.5, 3, 2.5, "belly")
        s.ellipse(16, 25.5, 1.2, 1.2, "ember")
        s.ellipse(16, 19.5, 2.5, 1.5, "belly")
        s.rect(11, 29, 13, 29, "brown", True)
        eyes(s, 12, 15, "small"); mouth(s, 19); cheeks(s, 11, 18)
        s.fix(10, 12, (255, 200, 120), True)
    elif lv == 2:
        s.ellipse(9.5, 7.5, 3, 3, "brown", True)
        s.ellipse(9.5, 7.5, 1.5, 1.5, "body", True)
        s.ellipse(16, 12, 7.5, 6.5, "brown")
        s.ellipse(16, 15, 3, 2, "belly")
        s.ellipse(16, 23, 9, 7, "brown")
        s.ellipse(16, 23.5, 5, 4.5, "dark")
        s.ellipse(16, 24, 3.5, 3, "flame")
        s.ellipse(16, 24.5, 1.8, 1.5, "core")
        s.ellipse(7.5, 23.5, 2.2, 3.5, "brown", True)
        s.rect(10, 28, 13, 30, "brown", True)
        eyes(s, 12, 10); mouth(s, 15); cheeks(s, 10, 13)
    else:
        s.poly([(3, 12), (2, 4), (6, 8), (7, 2), (10, 10)], "flame", True)
        s.poly([(4, 10), (4, 6), (7, 8)], "core", True)
        s.ellipse(9.5, 4.5, 3, 3, "brown", True)
        s.ellipse(9.5, 4.5, 1.5, 1.5, "body", True)
        s.ellipse(16, 9.5, 8, 6.5, "brown")
        s.ellipse(16, 12.5, 3.5, 2.2, "belly")
        s.ellipse(16, 22, 10.5, 8.5, "brown")
        s.rect(10, 17, 21, 27, "dark")
        s.rect(11, 18, 20, 26, "dark")
        s.rect(12, 20, 19, 25, "flame")
        s.rect(13, 22, 18, 25, "core")
        s.rect(11, 19, 20, 19, "brown")
        s.ellipse(4.5, 23, 2.8, 4.5, "brown", True)
        s.rect(8, 29, 12, 31, "brown", True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 11, 7, "fierce"); mouth(s, 12, "fang")
        for x in (12, 14, 17, 19):
            s.fix(x, 19, (60, 30, 30))
    return s.render(P)


# ---------- 용암갈기 LavaMane: lion with a lava mane ----------
def lavamane(lv):
    s = Spr()
    if lv == 1:
        spikes(s, 16, 17, 6.5, 9, 9, "flame")
        s.ellipse(16, 17, 6, 5.5, "body")
        s.poly([(10, 11), (11, 15), (13.5, 13)], "body", True)
        s.ellipse(16, 19.5, 2.5, 1.6, "belly")
        s.ellipse(16, 25.5, 5, 3.5, "body")
        s.ellipse(16, 26, 2.5, 2, "belly")
        s.rect(12, 28, 13, 29, "body", True)
        eyes(s, 13, 16, "small"); mouth(s, 20); cheeks(s, 12, 19)
    elif lv == 2:
        s.poly([(22, 22), (29, 12), (27, 19), (31, 15), (27, 27), (21, 27)], "flame")
        s.poly([(23, 23), (27, 17), (26, 23), (28, 21), (25, 26)], "core")
        spikes(s, 16, 12.5, 8, 11.5, 11, "flame")
        spikes(s, 16, 12.5, 7, 9, 11, "core", rot=0.28)
        s.ellipse(16, 12.5, 7, 6, "body")
        s.ellipse(16, 15.5, 3, 2, "belly")
        s.ellipse(16, 23, 6.5, 6, "body")
        s.ellipse(16, 23.5, 3.5, 4, "belly")
        s.rect(10, 27, 12, 29, "body", True)
        eyes(s, 12, 11); mouth(s, 15); cheeks(s, 11, 14)
    else:
        s.poly([(4, 24), (1, 10), (6, 15), (6, 5), (11, 12), (16, 1), (21, 12), (26, 5),
                (26, 15), (31, 10), (28, 24)], "flame")
        s.poly([(8, 22), (7, 13), (11, 16), (16, 6), (21, 16), (25, 13), (24, 22)], "core")
        s.poly([(9, 5), (11, 11), (13.5, 9)], "dark", True)
        s.ellipse(16, 13, 7.5, 6.5, "body")
        s.ellipse(16, 16, 4, 2.5, "belly")
        s.ellipse(16, 23, 9, 6.5, "body")
        s.ellipse(16, 23.5, 5, 4.5, "belly")
        s.rect(8, 27, 11, 30, "body", True)
        s.rect(8, 30, 11, 30, "dark", True)
        eyes(s, 11, 11, "fierce"); mouth(s, 16, "fang")
    return s.render(P)


CREATURES = [("Embit", "불똥이", embit), ("HearthBear", "화로곰", hearthbear),
             ("LavaMane", "용암갈기", lavamane)]
