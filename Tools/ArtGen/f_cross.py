from px import Spr
from lib import PALS, pal, eyes, cheeks, mouth, spikes, K, W, YEL
from n_fire import flame
from c_elec import bolt

# 조합 크리처 교차 2종: 몸·외곽선은 앞 타입 색, 장식·무늬는 뒤 타입 색(x 접두사)


def mix(a, b):
    p = pal(a)
    p.update({"x" + k: v for k, v in PALS[b].items()})
    return p


def xflame(s, x, y, h, w=2.5, mirror=False):
    s.poly([(x, y), (x - w, y + h), (x + w, y + h)], "xflame", mirror)
    s.poly([(x, y + h * 0.4), (x - w * 0.5, y + h), (x + w * 0.5, y + h)], "xcore", mirror)


# ---------- 화산코뿔소 VolcanoRhino: 용암갈기 + 암반코뿔소 (바위 코뿔소, 분화구 뿔, 용암 갈기, 등의 분화구) ----------
def volcanorhino(lv):
    s = Spr()
    if lv == 1:
        spikes(s, 16, 18.5, 6, 8.5, 10, "xflame")
        spikes(s, 16, 18.5, 5.5, 7, 10, "xcore", rot=0.3)
        s.ellipse(16, 18.5, 6, 5, "body")
        s.ellipse(16, 21, 3.5, 2, "belly")
        s.poly([(15, 13), (17, 13), (18, 19), (14, 19)], "stone")  # crater horn
        s.rect(15, 13, 16, 14, "xflame")
        s.ellipse(16, 25.5, 5.5, 3.5, "body")
        s.rect(11, 28, 13, 29, "body", True)
        s.fix(12, 25, (255, 150, 30), True); s.fix(13, 26, (255, 150, 30), True)  # lava cracks
        eyes(s, 12, 18, "small"); mouth(s, 22)
    elif lv == 2:
        spikes(s, 16, 13.5, 7, 9.5, 11, "xflame")
        spikes(s, 16, 13.5, 6.5, 8, 11, "xcore", rot=0.28)
        s.ellipse(16, 23, 9, 6.5, "body")
        s.poly([(5, 18), (10, 17), (9, 22), (6, 22)], "stone", True)  # rock pauldrons
        xflame(s, 7.5, 13, 5, 1.5, True)
        s.ellipse(16, 13.5, 7, 6, "body")
        s.ellipse(16, 16.5, 4.5, 2.5, "belly")
        s.poly([(14.5, 6), (17.5, 6), (19, 14), (13, 14)], "stone")
        s.rect(15, 6, 16, 7, "xflame")
        s.poly([(15, 8), (16, 8), (16.5, 11), (14.5, 11)], "xflame")
        for x, y in ((11, 22), (12, 23), (12, 24)):
            s.fix(x, y, (255, 150, 30), True)
        s.rect(9, 27, 12, 30, "body", True)
        s.rect(9, 30, 12, 30, "dark", True)
        eyes(s, 11, 12); mouth(s, 18)
    else:
        s.poly([(1, 19), (6, 12), (26, 12), (31, 19), (30, 28), (2, 28)], "stone")
        for cx in (5, 27):  # shoulder volcano vents erupting
            s.poly([(cx - 2, 8), (cx + 2, 8), (cx + 4, 15), (cx - 4, 15)], "dark")
        xflame(s, 5, 1, 7, 2.2, True)
        s.rect(3, 8, 6, 8, "xflame", True)
        spikes(s, 16, 13, 8, 12, 12, "xflame", ry_scale=0.85)
        spikes(s, 16, 13, 7, 9.5, 12, "xcore", rot=0.26, ry_scale=0.85)
        s.ellipse(16, 23.5, 10, 6.5, "body")
        for x, y in ((8, 20), (9, 21), (9, 22), (10, 23), (12, 25), (13, 26)):
            s.fix(x, y, (255, 150, 30), True)
        s.ellipse(16, 13, 8, 6.5, "body")
        s.ellipse(16, 16.5, 5, 2.5, "belly")
        s.poly([(14, 2), (18, 2), (20, 13), (12, 13)], "stone")
        xflame(s, 16, 0, 4, 2)
        s.poly([(15, 4), (17, 4), (17.5, 9), (14.5, 9)], "xflame")
        s.rect(15, 9, 16, 10, "xcore")
        s.rect(7, 27, 11, 31, "body", True)
        s.rect(7, 31, 11, 31, "dark", True)
        eyes(s, 10, 11, "fierce", (255, 150, 30)); mouth(s, 17, "fang")
    return s.render(mix("rock", "fire"))


# ---------- 폭풍고래 TempestWhale: 해일고래 + 뇌운매 (먹구름 고래, 물지느러미 날개, 물기둥 분수 + 번개) ----------
def tempestwhale(lv):
    s = Spr()
    if lv == 1:
        s.poly([(16, 12), (13.5, 17), (18.5, 17)], "xfoam")
        bolt(s, 14.5, 11, 0.6, "bolt")
        s.poly([(10, 21), (4, 16), (5, 20), (3, 23), (9, 25)], "xfin", True)
        s.poly([(5, 17), (4, 19), (7, 20)], "xdeep", True)
        s.ellipse(16, 23, 7.5, 6.5, "cloud")
        s.ellipse(16, 26, 5, 3, "belly")
        for x in (13, 15, 17):
            s.rect(x, 25, x, 28, "xfoam")
        eyes(s, 12, 21, "small"); mouth(s, 24, "smile"); cheeks(s, 10, 24)
    elif lv == 2:
        s.poly([(16, 6), (12.5, 12), (19.5, 12)], "xfoam")
        s.ellipse(12, 7, 1.4, 1.4, "xfoam", True)
        bolt(s, 14, 3, 1.0)
        s.poly([(7, 15), (0, 7), (1, 12), (0, 15), (2, 18), (7, 21)], "xfin", True)
        s.poly([(1, 9), (0, 12), (3, 12)], "xdeep", True)
        s.poly([(0, 15), (2, 18), (4, 16)], "xdeep", True)
        s.ellipse(16, 20, 11.5, 9, "cloud")
        s.rect(12, 11, 19, 12, "cloud")
        for x in (10, 22):
            s.rect(x, 13, x + 1, 13, "dark")
        s.ellipse(16, 24, 8.5, 4.5, "belly")
        for x in range(11, 22, 2):
            s.rect(x, 23, x, 27, "xfoam")
        s.poly([(5, 24), (1, 29), (6, 28)], "xfin", True)
        eyes(s, 10, 17, "sharp", YEL); mouth(s, 22, "smile")
    else:
        s.poly([(16, 4), (10, 10), (22, 10)], "xfoam")
        s.ellipse(10, 4, 2, 2, "xfoam", True)
        s.ellipse(5, 7, 1.3, 1.3, "xfoam", True)
        bolt(s, 13.5, 0, 1.4)
        s.poly([(7, 13), (0, 0), (0, 7), (1, 10), (0, 13), (2, 17), (6, 19)], "xfin", True)
        s.poly([(0, 2), (0, 6), (3, 6)], "xdeep", True)
        s.poly([(0, 9), (1, 12), (3, 11)], "xdeep", True)
        s.ellipse(16, 19.5, 14.5, 10.5, "cloud")
        for x, y in ((8, 11), (24, 11), (16, 9)):
            s.ellipse(x, y, 3.5, 2.5, "cloud")
        s.ellipse(16, 24, 11, 5.5, "belly")
        for x in range(8, 25, 2):
            s.rect(x, 22, x, 28, "xfoam")
        s.poly([(3, 24), (0, 31), (6, 29)], "xfin", True)
        for x in (6, 26):
            s.fix(x, 15, (255, 250, 150)); s.fix(x, 16, (255, 250, 150))
        eyes(s, 9, 15, "fierce", (255, 250, 150)); mouth(s, 21, "fang")
    return s.render(mix("elec", "water"))


CREATURES = [("VolcanoRhino", "화산코뿔소", volcanorhino), ("TempestWhale", "폭풍고래", tempestwhale)]
