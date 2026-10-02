from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("water")


# ---------- 퐁당이 Plopper: a water droplet ----------
def plopper(lv):
    s = Spr()
    if lv == 1:
        s.poly([(16, 12), (10, 21), (22, 21)], "body")
        s.ellipse(16, 23, 6.5, 6, "body")
        s.ellipse(14, 19, 1.2, 2, "foam")
        eyes(s, 13, 22, "small"); mouth(s, 25); cheeks(s, 12, 24)
    elif lv == 2:
        s.poly([(16, 5), (8, 17), (24, 17)], "body")
        s.ellipse(16, 20.5, 9, 8.5, "body")
        s.poly([(7, 21), (3, 18), (4, 24)], "fin", True)
        s.ellipse(16, 24, 5, 3.5, "belly")
        s.ellipse(13, 12, 1.4, 2.5, "foam")
        s.ellipse(25.5, 9, 1.5, 1.5, "foam")
        eyes(s, 12, 17); cheeks(s, 11, 21); mouth(s, 21, "smile")
    else:
        spikes(s, 16, 7, 3, 6, 5, "foam", ry_scale=0.7)
        s.poly([(16, 2), (6, 15), (26, 15)], "body")
        s.ellipse(16, 19, 11, 10, "body")
        s.poly([(5, 20), (0, 14), (1, 25)], "fin", True)
        s.ellipse(16, 23.5, 6.5, 4.5, "belly")
        s.ellipse(12, 11, 1.5, 3, "foam")
        s.ellipse(28, 4, 1.8, 1.8, "foam")
        s.ellipse(3, 6, 1.2, 1.2, "foam")
        s.rect(11, 28, 13, 29, "body", True)
        eyes(s, 11, 15, "sharp"); mouth(s, 20, "w")
    return s.render(P)


# ---------- 물보라수달 SprayOtter ----------
def sprayotter(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(11, 14, 1.8, 1.8, "brown", True)
        s.ellipse(16, 18, 6, 5, "brown")
        s.ellipse(16, 20, 3.5, 2.2, "belly")
        s.ellipse(16, 25.5, 5, 4, "brown")
        s.ellipse(16, 26, 2.8, 2.8, "belly")
        s.rect(12, 29, 13, 29, "brown", True)
        eyes(s, 13, 17, "small"); mouth(s, 20); cheeks(s, 12, 19)
        s.fix(11, 20, K, True)
    elif lv == 2:
        s.poly([(16, 0), (13, 6), (19, 6)], "foam")
        s.ellipse(12, 4, 1.5, 1.5, "foam", True)
        s.poly([(22, 26), (29, 22), (30, 25), (24, 29)], "brown")
        s.ellipse(10.5, 8, 2, 2, "brown", True)
        s.ellipse(16, 12, 7, 5.5, "brown")
        s.ellipse(16, 14.5, 4, 2.5, "belly")
        s.ellipse(16, 22, 7, 7, "brown")
        s.ellipse(16, 22.5, 4, 5, "belly")
        s.ellipse(9, 20, 1.8, 2.8, "brown", True)
        s.rect(11, 28, 13, 29, "brown", True)
        eyes(s, 12, 10); mouth(s, 14); cheeks(s, 11, 13)
        for x, y in ((9, 14), (9, 15)):
            s.fix(x, y, K, True)
    else:
        spikes(s, 16, 5, 3, 6, 6, "foam", ry_scale=0.8)
        s.poly([(16, 0), (13, 5), (19, 5)], "body")
        s.poly([(21, 27), (30, 18), (31, 24), (25, 30)], "brown")
        s.poly([(27, 18), (31, 13), (31, 21)], "fin")
        s.ellipse(10, 7.5, 2.2, 2.2, "brown", True)
        s.ellipse(16, 11.5, 7.5, 6, "brown")
        s.ellipse(16, 14.5, 4.5, 2.5, "belly")
        s.ellipse(16, 22, 9, 8, "brown")
        s.ellipse(16, 22.5, 5, 5.5, "belly")
        s.ellipse(16, 22, 3, 3, "shell")
        s.ellipse(7, 20, 2.2, 3.5, "brown", True)
        s.ellipse(4.5, 23, 1.6, 1.6, "foam", True)
        s.rect(10, 28, 13, 30, "brown", True)
        eyes(s, 11, 9, "sharp"); mouth(s, 14, "fang")
    return s.render(P)


# ---------- 해일고래 TideWhale ----------
def tidewhale(lv):
    s = Spr()
    if lv == 1:
        s.poly([(16, 11), (14, 15), (18, 15)], "foam")
        s.ellipse(14, 12, 1, 1, "foam", True)
        s.ellipse(16, 22, 8, 6.5, "body")
        s.ellipse(16, 25, 5.5, 3, "belly")
        s.poly([(8, 22), (4, 25), (8, 26)], "fin", True)
        eyes(s, 11, 20, "small"); mouth(s, 23, "smile"); cheeks(s, 10, 23)
    elif lv == 2:
        s.poly([(16, 4), (13, 10), (19, 10)], "foam")
        s.ellipse(12.5, 5, 1.5, 1.5, "foam", True)
        s.ellipse(16, 19, 12, 9, "body")
        s.ellipse(16, 23, 8.5, 4.5, "belly")
        for x in range(11, 22, 2):
            s.rect(x, 22, x, 26, "foam")
        s.poly([(4, 19), (0, 25), (5, 25)], "fin", True)
        s.rect(13, 10, 18, 11, "body")
        eyes(s, 9, 16); mouth(s, 21, "smile"); cheeks(s, 8, 20)
    else:
        s.poly([(16, 0), (11, 7), (21, 7)], "foam")
        s.ellipse(10, 2, 2, 2, "foam", True)
        s.ellipse(5, 6, 1.4, 1.4, "foam", True)
        s.ellipse(16, 18.5, 15, 10.5, "body")
        s.poly([(3, 10), (6, 4), (9, 10)], "fin", True)
        s.ellipse(16, 23, 11, 5.5, "belly")
        for x in range(8, 25, 2):
            s.rect(x, 21, x, 27, "foam")
        s.poly([(1, 22), (0, 30), (6, 27)], "fin", True)
        eyes(s, 8, 14, "fierce", (120, 220, 255)); mouth(s, 20, "fang")
        s.fix(15, 8, (255, 255, 255)); s.fix(16, 8, (255, 255, 255))
    return s.render(P)


CREATURES = [("Plopper", "퐁당이", plopper), ("SprayOtter", "물보라수달", sprayotter),
             ("TideWhale", "해일고래", tidewhale)]
