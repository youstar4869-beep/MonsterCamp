from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W
from c_elec import bolt

P = pal("elec")


# ---------- 찌릿다람쥐 SparkSquirrel (1골드): blue squirrel, huge fluffy tail ----------
def sparksquirrel(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(23, 19, 5, 7, "wool")
        s.poly([(22, 16), (25, 19), (23, 19), (25, 23), (21, 19), (23, 19)], "bolt")
        s.poly([(10, 15), (11, 11), (13, 14)], "blue", True)
        s.ellipse(16, 19, 5, 4.5, "blue")
        s.ellipse(16, 21, 2.5, 1.5, "belly")
        s.ellipse(15, 25.5, 4, 3.5, "blue")
        s.ellipse(15, 26, 2, 2.5, "belly")
        s.rect(12, 28, 13, 29, "blue", True)
        eyes(s, 13, 18, "small"); mouth(s, 22); cheeks(s, 11, 20)
    elif lv == 2:
        s.ellipse(25, 14, 6.5, 10, "wool")
        s.ellipse(26, 5, 4, 3, "wool")
        s.poly([(25, 8), (28, 14), (25, 14), (28, 21), (22, 13), (25, 13)], "bolt")
        s.poly([(8, 8), (9, 2), (12, 6)], "blue", True)
        s.rect(9, 1, 9, 2, "bolt", True)
        s.ellipse(15, 12, 7, 5.5, "blue")
        s.ellipse(15, 14.5, 3.5, 2, "belly")
        s.ellipse(15, 22.5, 6, 6, "blue")
        s.ellipse(15, 23, 3, 4, "belly")
        s.ellipse(9, 20, 1.6, 2.5, "blue")
        s.ellipse(21, 20, 1.6, 2.5, "blue")
        s.rect(10, 27, 12, 30, "blue"); s.rect(18, 27, 20, 30, "blue")
        eyes(s, 11, 10); s.fix(18, 10, K); s.fix(19, 10, K); s.fix(18, 11, K); s.fix(19, 11, K); s.fix(18, 12, K); s.fix(19, 12, K); s.fix(18, 10, W)
        mouth(s, 15, "w", cx=15); s.fix(9, 14, (255, 128, 140)); s.fix(21, 14, (255, 128, 140))
    else:
        s.ellipse(25, 13, 7, 12, "wool")
        s.ellipse(25, 3, 5.5, 3.5, "wool")
        s.poly([(25, 4), (29, 12), (25, 12), (29, 22), (21, 11), (25, 11)], "bolt")
        bolt(s, 0, 18, 1.1)
        s.poly([(6, 7), (7, 0), (11.5, 5)], "blue", True)
        s.rect(7, 0, 7, 1, "bolt", True)
        s.ellipse(14, 10, 8, 6, "blue")
        s.ellipse(14, 13, 4, 2, "belly")
        s.ellipse(14, 22.5, 7.5, 7, "blue")
        s.ellipse(14, 23, 4, 5, "belly")
        bolt(s, 11.5, 18, 1.0)
        s.ellipse(6, 19, 2, 3.5, "blue")
        s.ellipse(21, 19, 2, 3.5, "blue")
        s.rect(8, 27, 11, 31, "blue"); s.rect(17, 27, 20, 31, "blue")
        s.rect(8, 31, 11, 31, "dark"); s.rect(17, 31, 20, 31, "dark")
        for x, m in ((8, 1), (18, -1)):
            s.fix(x, 8, K); s.fix(x + m, 9, K); s.fix(x + 2 * m, 9, K); s.fix(x + m, 10, K); s.fix(x + 2 * m, 10, K)
            s.fix(x + 2 * m, 9, (255, 250, 150))
        mouth(s, 13, "fang", cx=15)
    return s.render(P)


# ---------- 전기뱀장어 VoltEel (2골드) ----------
def volteel(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(14, 26, 7, 2.8, "blue")
        s.ellipse(22, 24, 3, 2, "blue")
        s.ellipse(16, 19, 4.5, 4.5, "blue")
        s.ellipse(16, 21.5, 2.5, 1.5, "belly")
        for x in (11, 16, 21):
            s.rect(x, 26, x, 26, "bolt")
        s.poly([(16, 14), (14, 11), (18, 11)], "bolt")
        eyes(s, 14, 18, "small"); mouth(s, 21); cheeks(s, 12, 20)
    elif lv == 2:
        s.ellipse(16, 27, 12, 3.2, "blue")
        s.ellipse(26, 23, 4, 3, "blue")
        s.poly([(28, 21), (31, 16), (30, 23)], "bolt")
        s.rect(13, 14, 19, 26, "blue")
        s.ellipse(16, 11, 6.5, 5.5, "blue")
        s.ellipse(16, 15, 3.5, 2, "belly")
        s.rect(15, 17, 16, 26, "belly")
        for y in (5, 8):
            s.poly([(16, y - 3), (14, y), (18, y)], "bolt")
        for x in (7, 11, 21, 25):
            s.rect(x, 27, x + 1, 27, "bolt")
        eyes(s, 12, 9, "cute"); mouth(s, 14, "smile")
    else:
        s.ellipse(16, 28, 15, 3.2, "blue")
        s.ellipse(28, 23, 3.5, 4, "blue")
        s.poly([(29, 20), (31, 13), (31, 22)], "bolt")
        s.poly([(3, 26), (0, 18), (2, 19), (5, 25)], "bolt")
        s.rect(12, 13, 20, 27, "blue")
        s.ellipse(16, 10, 8, 7, "blue")
        s.poly([(10, 12), (16, 18), (22, 12)], "belly")
        s.rect(15, 18, 16, 27, "belly")
        for x, y in ((16, 0), (11, 2), (21, 2)):
            s.poly([(x, y), (x - 2, y + 4), (x + 2, y + 4)], "bolt")
        for x in (4, 8, 23, 27):
            s.rect(x, 28, x + 1, 28, "bolt")
        bolt(s, 19.5, 18, 0.9, "bolt")
        eyes(s, 10, 8, "fierce", (255, 250, 150)); mouth(s, 14, "fang")
    return s.render(P)


# ---------- 천둥양 ThunderRam (4골드) ----------
def horn(s, cx, cy, r, mirror=True):
    s.ellipse(cx, cy, r, r, "wing", mirror)
    s.ellipse(cx + 0.3, cy + 0.3, r * 0.45, r * 0.45, "dark", mirror)


def thunderram(lv):
    s = Spr()
    if lv == 1:
        for x, y in ((11, 20), (21, 20), (13, 17), (19, 17), (16, 16), (11, 24), (21, 24)):
            s.ellipse(x, y, 3, 3, "wool")
        s.ellipse(16, 22, 6, 5, "wool")
        s.ellipse(16, 21, 3.5, 3, "body")
        s.rect(12, 26, 13, 28, "dark", True)
        s.fix(15, 20, W); s.fix(14, 20, K); s.fix(17, 20, K); s.fix(16, 20, W)
        s.fix(15, 22, (255, 128, 140)); s.fix(16, 22, (255, 128, 140))
        bolt(s, 14.5, 9, 0.9)
    elif lv == 2:
        bolt(s, 13.5, 0, 1.2)
        for x, y in ((7, 14), (25, 14), (9, 10), (23, 10), (13, 8), (19, 8), (6, 19), (26, 19), (9, 23), (23, 23), (16, 24)):
            s.ellipse(x, y, 3.5, 3.5, "wool")
        s.ellipse(16, 16, 10, 8, "wool")
        horn(s, 9, 13, 3)
        s.ellipse(16, 15, 4.5, 4.5, "body")
        s.rect(10, 27, 12, 30, "dark", True)
        eyes(s, 13, 13, "small"); s.fix(14, 13, W, True)
        s.fix(15, 17, (255, 128, 140)); s.fix(16, 17, (255, 128, 140))
    else:
        bolt(s, 12.5, 0, 1.6)
        bolt(s, 2, 2, 1.0, mirror=True)
        for x, y in ((5, 15), (27, 15), (7, 10), (25, 10), (12, 7), (20, 7), (4, 21), (28, 21), (7, 25), (25, 25), (16, 26), (11, 27), (21, 27)):
            s.ellipse(x, y, 4, 4, "cloud")
        s.ellipse(16, 17, 12, 9.5, "cloud")
        for x, y in ((9, 14), (23, 14), (16, 10), (12, 22), (20, 22)):
            s.ellipse(x, y, 2.5, 2, "wool")
        horn(s, 7, 12, 4.5)
        s.ellipse(16, 16, 5, 5.5, "body")
        s.rect(9, 28, 12, 31, "dark", True)
        s.rect(9, 31, 12, 31, "bolt", True)
        eyes(s, 12, 13, "fierce", (255, 250, 150)); mouth(s, 19, "fang")
    return s.render(P)


# ---------- 뇌광기린 VoltQilin (5골드): white qilin, purple mane, gold bolt horn ----------
def voltqilin(lv):
    s = Spr()
    if lv == 1:
        s.poly([(16, 9), (15, 14), (17, 14)], "bolt")
        s.poly([(12, 13), (11, 15), (14, 15)], "purple", True)
        s.ellipse(16, 18, 5, 4, "wool")
        s.ellipse(16, 21, 3, 2, "belly")
        s.ellipse(16, 25.5, 4.5, 3, "wool")
        s.poly([(20, 22), (24, 20), (23, 25)], "purple")
        for x in (12, 14):
            s.rect(x, 27, x, 29, "wool", True)
        s.rect(12, 29, 14, 29, "body", True)
        eyes(s, 13, 17, "small"); s.fix(15, 21, K); s.fix(16, 21, K); cheeks(s, 11, 19)
    elif lv == 2:
        s.poly([(16, 0), (14.5, 8), (17.5, 8)], "bolt")
        s.poly([(10, 7), (7, 4), (12, 5)], "wool", True)
        for y in (6, 9, 12, 15, 18):
            s.ellipse(22, y, 3, 2.5, "purple")
        s.ellipse(16, 11, 5.5, 4.5, "wool")
        s.ellipse(16, 15, 3.5, 2.5, "belly")
        s.ellipse(16, 23, 6.5, 4.5, "wool")
        s.poly([(22, 22), (28, 18), (27, 25)], "purple")
        for x, y in ((12, 22), (20, 23)):
            s.fix(x, y, (255, 230, 120))
        for x in (10, 13):
            s.rect(x, 26, x + 1, 30, "wool", True)
        s.rect(10, 30, 14, 30, "body", True)
        eyes(s, 12, 10); s.fix(15, 16, K); s.fix(16, 16, K); cheeks(s, 11, 13)
    else:
        bolt(s, 14, 0, 1.4)
        s.poly([(10, 7), (4, 3), (11, 5)], "wool", True)
        bolt(s, 0, 11, 1.0, mirror=True)
        for y in (3, 6, 9, 12, 15, 18):
            s.ellipse(22 + (y % 2), y, 4, 3, "purple")
        s.ellipse(16, 10, 6.5, 5, "wool")
        s.ellipse(16, 14.5, 4, 2.5, "belly")
        s.ellipse(16, 22, 8, 5.5, "wool")
        s.poly([(22, 20), (31, 14), (30, 21), (31, 26), (23, 25)], "purple")
        for x, y in ((10, 20), (14, 19), (18, 20), (22, 22), (11, 24)):
            s.fix(x, y, (255, 230, 120))
        for x in (8, 12):
            s.rect(x, 26, x + 1, 31, "wool", True)
        s.rect(8, 31, 13, 31, "body", True)
        eyes(s, 11, 8, "fierce", (180, 150, 255)); s.fix(15, 16, K); s.fix(16, 16, K)
    return s.render(P)


CREATURES = [("SparkSquirrel", "찌릿다람쥐", sparksquirrel), ("VoltEel", "전기뱀장어", volteel),
             ("ThunderRam", "천둥양", thunderram), ("VoltQilin", "뇌광기린", voltqilin)]
