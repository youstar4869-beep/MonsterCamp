from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W

P = pal("rock")


def crystal(s, x, y, h, mirror=False):
    s.poly([(x, y), (x - 1.5, y + h * 0.4), (x, y + h), (x + 1.5, y + h * 0.4)], "crystal", mirror)


# ---------- 돌멩군 Pebblet: a living pebble ----------
def pebblet(lv):
    s = Spr()
    if lv == 1:
        s.poly([(10, 17), (13, 14), (20, 14), (23, 18), (22, 27), (10, 27)], "stone")
        s.ellipse(13, 16, 1.2, 0.8, "horn")
        s.rect(12, 28, 13, 28, "stone", True)
        eyes(s, 13, 20, "small"); mouth(s, 24)
        s.fix(18, 25, (90, 82, 76)); s.fix(19, 25, (90, 82, 76))
    elif lv == 2:
        s.ellipse(14, 8, 3, 1.5, "moss")
        s.poly([(7, 13), (11, 8), (21, 8), (25, 13), (24, 25), (8, 25)], "stone")
        s.ellipse(11, 11, 2, 1, "horn")
        s.ellipse(14, 8, 3, 1.3, "moss")
        s.poly([(6, 16), (2, 20), (4, 23), (7, 21)], "dark", True)
        s.rect(10, 26, 13, 29, "dark", True)
        eyes(s, 12, 15); mouth(s, 20, "smile")
        for x, y in ((19, 22), (20, 22), (9, 21)):
            s.fix(x, y, (110, 100, 92))
    else:
        crystal(s, 16, 0, 6)
        crystal(s, 10, 2, 5, True)
        s.poly([(5, 11), (10, 5), (22, 5), (27, 11), (26, 25), (6, 25)], "stone")
        s.ellipse(10, 9, 2.5, 1.2, "horn")
        s.ellipse(22, 7, 3, 1.3, "moss")
        s.poly([(5, 13), (0, 19), (2, 24), (7, 22)], "dark", True)
        crystal(s, 2, 15, 4, True)
        s.rect(8, 26, 13, 30, "dark", True)
        s.rect(8, 30, 13, 30, "dark", True)
        eyes(s, 11, 13, "fierce", (180, 160, 255)); mouth(s, 19, "fang")
        for x, y in ((20, 21), (21, 21), (22, 22), (9, 20), (10, 21)):
            s.fix(x, y, (110, 100, 92))
    return s.render(P)


# ---------- 자갈갑옷 GravelShell: an armadillo with a gravel shell ----------
def gravelshell(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(16, 21, 7.5, 6, "stone")
        for x in (11, 14, 17, 20):
            s.fix(x, 17, (150, 140, 128)); s.fix(x + 1, 20, (70, 64, 62))
        s.ellipse(16, 24.5, 4.5, 3.5, "belly")
        s.ellipse(11.5, 21.5, 1.4, 1.4, "belly", True)
        s.rect(12, 28, 13, 28, "belly", True)
        eyes(s, 14, 23, "small"); mouth(s, 26)
    elif lv == 2:
        s.ellipse(9, 9, 1.8, 2.5, "belly", True)
        s.ellipse(16, 18, 11, 9, "stone")
        for y in (12, 15, 18):
            s.rect(7, y, 24, y, "dark")
        for x, y in ((9, 11), (13, 14), (19, 11), (22, 17), (11, 17), (17, 16)):
            s.fix(x, y, (160, 150, 136)); s.fix(x + 1, y, (160, 150, 136))
        s.ellipse(16, 23, 6, 4.5, "belly")
        s.ellipse(16, 21, 3.5, 2.5, "body")
        s.rect(9, 27, 12, 29, "belly", True)
        eyes(s, 13, 21); mouth(s, 25, "smile")
    else:
        spikes(s, 16, 15, 10, 14, 9, "stone", ry_scale=0.75)
        s.ellipse(8, 7, 2, 2.8, "belly", True)
        s.ellipse(16, 17, 13, 10, "stone")
        for y in (10, 13, 16, 19):
            s.rect(5, y, 26, y, "dark")
        for x, y in ((7, 12), (11, 9), (16, 11), (21, 9), (24, 15), (9, 18), (14, 15), (19, 18)):
            s.fix(x, y, (170, 160, 146)); s.fix(x + 1, y, (170, 160, 146))
        crystal(s, 16, 2, 5)
        s.ellipse(16, 23, 7, 5, "belly")
        s.ellipse(16, 21, 4, 2.5, "body")
        s.rect(7, 27, 11, 30, "belly", True)
        s.rect(7, 30, 11, 30, "dark", True)
        eyes(s, 12, 20, "sharp"); mouth(s, 25, "fang")
    return s.render(P)


# ---------- 암반코뿔소 BedrockRhino ----------
def bedrockrhino(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(10.5, 14, 1.6, 1.6, "body", True)
        s.ellipse(16, 19, 6, 5, "body")
        s.ellipse(16, 21.5, 3.5, 2, "belly")
        s.poly([(16, 15), (14.8, 19), (17.2, 19)], "horn")
        s.ellipse(16, 25.5, 5.5, 3.5, "body")
        s.rect(11, 28, 13, 29, "body", True)
        eyes(s, 12, 18, "small"); mouth(s, 22)
    elif lv == 2:
        s.ellipse(9, 7, 2, 2, "body", True)
        s.ellipse(16, 13, 7.5, 6, "body")
        s.ellipse(16, 16, 4.5, 2.5, "belly")
        s.poly([(16, 5), (14, 14), (18, 14)], "horn")
        s.ellipse(16, 23, 9, 6.5, "body")
        s.poly([(7, 18), (10, 20), (7, 22)], "stone", True)
        s.rect(9, 27, 12, 30, "body", True)
        s.rect(9, 30, 12, 30, "dark", True)
        eyes(s, 11, 11); mouth(s, 17)
    else:
        s.poly([(10, 3), (9, 9), (12, 8)], "horn", True)
        crystal(s, 4, 10, 6, True)
        s.poly([(3, 18), (8, 13), (24, 13), (29, 18), (29, 28), (3, 28)], "stone")
        for x in range(5, 28, 4):
            s.rect(x, 20, x + 1, 21, "dark")
        s.ellipse(16, 24, 9, 6, "body")
        s.ellipse(8, 7, 2.2, 2.2, "body", True)
        s.ellipse(16, 12, 8, 6.5, "body")
        s.ellipse(16, 15.5, 5, 2.5, "belly")
        s.poly([(16, 3), (13.5, 14), (18.5, 14)], "horn")
        s.rect(7, 27, 11, 31, "body", True)
        s.rect(7, 31, 11, 31, "dark", True)
        eyes(s, 10, 9, "fierce", (255, 120, 80)); mouth(s, 16, "fang")
    return s.render(P)


CREATURES = [("Pebblet", "돌멩군", pebblet), ("GravelShell", "자갈갑옷", gravelshell),
             ("BedrockRhino", "암반코뿔소", bedrockrhino)]
