from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W, YEL
from n_fire import flame
from n_water import bubble, tentacle
from c_grass import leaf
from c_elec import bolt
from c_rock import crystal
from n_elec import horn
from n_rock import mountain, core

# 조합 크리처 상급 5종: 가장 강한 크리처라 후광·날개·뿔·왕관급 큰 장식, Lv1부터 재료보다 강해 보이게

CYAN = (120, 230, 255)


# ---------- 일륜늑대 SolarWolf: 불꽃늑대 + 태양불사조 (태양 후광, 불사조 날개, 늑대) ----------
def solarwolf(lv):
    s = Spr()
    if lv == 1:
        spikes(s, 16, 18, 5.5, 7.5, 10, "ember")
        s.poly([(11, 22), (5, 18), (6, 22), (5, 25), (11, 26)], "flame", True)  # phoenix wings
        s.poly([(11, 23), (7, 21), (7, 24), (11, 25)], "core", True)
        s.poly([(10, 12), (11, 17), (14, 15)], "body", True)
        s.ellipse(16, 18, 6, 4.5, "body")
        s.poly([(13, 19), (16, 22), (19, 19)], "belly")
        s.ellipse(16, 25.5, 4.5, 3.5, "body")
        s.ellipse(16, 25.5, 1.5, 1.5, "core")
        s.rect(12, 28, 13, 29, "body", True)
        eyes(s, 12, 17, "small"); s.fix(15, 20, K); s.fix(16, 20, K)
    elif lv == 2:
        s.poly([(21, 25), (29, 17), (28, 23), (31, 21), (25, 29)], "flame")
        spikes(s, 16, 12, 7.5, 10.5, 12, "ember", ry_scale=0.75)
        spikes(s, 16, 12, 6.5, 8.5, 12, "core", rot=0.26, ry_scale=0.75)
        s.poly([(10, 17), (2, 11), (3, 15), (1, 19), (4, 22), (10, 24)], "flame", True)
        s.poly([(10, 18), (5, 15), (5, 19), (10, 22)], "core", True)
        s.poly([(7, 4), (10, 11), (13.5, 8)], "body", True)
        s.poly([(8, 6), (10, 9), (12, 8)], "dark", True)
        s.ellipse(16, 12, 7, 5.5, "body")
        s.poly([(12, 13), (16, 18), (20, 13)], "belly")
        s.ellipse(16, 23, 6, 6, "body")
        s.ellipse(16, 23.5, 3, 4, "belly")
        flame(s, 16, 20, 5, 1.6)
        s.rect(10, 27, 12, 30, "body", True)
        s.rect(10, 30, 12, 30, "dark", True)
        eyes(s, 11, 10, "sharp", YEL); s.fix(15, 16, K); s.fix(16, 16, K)
    else:
        s.poly([(20, 24), (31, 12), (29, 20), (31, 19), (26, 29)], "flame")
        spikes(s, 16, 11, 10, 15, 14, "ember", ry_scale=0.8)
        spikes(s, 16, 11, 9, 12, 14, "core", rot=0.22, ry_scale=0.8)
        s.poly([(10, 15), (0, 4), (1, 10), (0, 14), (2, 18), (0, 22), (10, 24)], "flame", True)
        s.poly([(10, 17), (3, 9), (4, 14), (3, 18), (10, 21)], "core", True)
        s.poly([(5, 1), (9, 10), (13.5, 6)], "body", True)
        s.poly([(6, 3), (9, 8), (12, 6)], "dark", True)
        s.ellipse(16, 10.5, 8, 6, "body")
        s.poly([(11, 11), (16, 18), (21, 11)], "belly")
        s.ellipse(16, 23, 8, 6.5, "body")
        s.ellipse(16, 23.5, 4, 4.5, "belly")
        flame(s, 16, 19, 7, 2.2)
        s.rect(8, 27, 12, 31, "body", True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 10, 8, "fierce"); mouth(s, 15, "fang")
    return s.render(pal("fire"))


# ---------- 심연크라켄 AbyssKraken: 소용돌이문어 + 심해룡 (심해 용머리 문어, 지느러미 날개·뿔, 발광 미끼) ----------
def abysskraken(lv):
    s = Spr()
    if lv == 1:
        for x0, x1 in ((12, 9), (14.5, 13), (17.5, 19), (20, 23)):
            tentacle(s, x0, 23, x1, 29, 1.2)
        s.poly([(11, 17), (6, 12), (7, 19)], "fin", True)
        s.ellipse(16, 19, 6.5, 6, "deep")
        s.poly([(14.5, 14), (16, 11), (17.5, 14)], "fin")
        s.ellipse(16, 23, 3.5, 1.5, "belly")
        eyes(s, 13, 18, "small"); s.fix(13, 18, CYAN, True); mouth(s, 22, "w")
        s.fix(16, 11, (180, 255, 255))
    elif lv == 2:
        for x0, x1, y1 in ((11, 4, 28), (13, 9, 30), (15, 14, 30)):
            tentacle(s, x0, 19, x1, y1, 1.6, mirror=True)
        s.poly([(4, 28), (2, 25), (3, 23), (5, 26)], "purple", True)
        s.poly([(9, 14), (1, 8), (2, 13), (0, 17), (8, 20)], "fin", True)
        s.poly([(8, 15), (3, 12), (3, 16), (8, 18)], "deep", True)
        s.poly([(10, 9), (8, 3), (12.5, 6)], "fin", True)
        s.ellipse(16, 13, 8.5, 8, "deep")
        for x, y in ((11, 9), (21, 8), (22, 14)):
            s.ellipse(x, y, 1, 1, "foam")
        s.ellipse(16, 18, 4, 1.5, "belly")
        s.rect(16, 4, 16, 6, "dark")
        s.fix(16, 4, (180, 255, 255)); s.fix(15, 4, CYAN); s.fix(17, 4, CYAN)
        eyes(s, 12, 12, "sharp", CYAN); mouth(s, 17, "w")
    else:
        for r, c in ((15, "fin"), (12, "body"), (9, "fin")):
            s.ellipse(16, 24, r, r * 0.4, c)
        for x0, x1, y1 in ((10, 0, 24), (12, 3, 31), (14, 9, 31), (15.5, 14, 31)):
            tentacle(s, x0, 19, x1, y1, 1.9, mirror=True)
        s.poly([(0, 24), (1, 19), (3, 19), (2, 22)], "purple", True)
        s.poly([(8, 12), (0, 0), (0, 7), (1, 10), (0, 14), (2, 18), (8, 19)], "fin", True)
        s.poly([(7, 13), (2, 5), (2, 9), (3, 12), (2, 15), (7, 17)], "deep", True)
        s.poly([(10, 6), (7, 0), (13, 4)], "fin", True)
        s.poly([(12.5, 4), (13.5, 0), (15, 4)], "fin", True)
        s.ellipse(16, 11, 9.5, 9, "deep")
        for x, y in ((9, 7), (23, 6), (24, 12), (8, 13)):
            s.ellipse(x, y, 1.3, 1.3, "foam")
        s.ellipse(16, 17, 5, 2, "belly")
        s.rect(16, 1, 16, 3, "dark")
        s.fix(16, 0, (180, 255, 255)); s.fix(15, 0, CYAN); s.fix(17, 0, CYAN); s.fix(16, 1, CYAN)
        eyes(s, 11, 10, "fierce", CYAN); mouth(s, 16, "fang")
    return s.render(pal("water"))


# ---------- 세계수 WorldTree: 덩굴파리지옥 + 고목거인 (꽃 핀 큰 수관, 줄기 몸통에 파리지옥 입, 파리지옥 손) ----------
def teeth(s, x0, x1, y, step=2):
    for x in range(x0, x1 + 1, step):
        s.fix(x, y, W)


def worldtree(lv):
    s = Spr()
    if lv == 1:
        leaf(s, 11, 21, 5, 17, w=1.4, mirror=True)
        s.ellipse(16, 15, 7.5, 4, "leaf")
        for x, y in ((11, 13), (16, 12), (21, 13)):
            s.ellipse(x, y, 1.2, 1.2, "flower")
        s.ellipse(14, 16, 0.9, 0.9, "orange")
        s.rect(11, 18, 20, 27, "wood")
        s.poly([(11, 22), (20, 22), (19, 25), (12, 25)], "red")
        teeth(s, 12, 19, 22); teeth(s, 13, 18, 25)
        s.rect(10, 28, 12, 29, "wood", True)
        eyes(s, 13, 19, "small")
    elif lv == 2:
        s.poly([(10, 16), (3, 13), (2, 17), (10, 20)], "wood", True)
        s.ellipse(3, 20, 2.5, 2, "body", True)  # flytrap hands
        s.rect(1, 20, 5, 20, "red", True)
        leaf(s, 4, 13, 0, 9, w=1.5, mirror=True)
        spikes(s, 16, 10, 9.5, 12, 14, "dark", ry_scale=0.45)
        s.ellipse(16, 10, 11.5, 5, "leaf")
        for x, y in ((8, 8), (13, 6), (19, 6), (24, 9), (16, 11)):
            s.ellipse(x, y, 1.3, 1.3, "flower")
        for x, y in ((11, 10), (21, 10)):
            s.ellipse(x, y, 1, 1, "orange")
        s.rect(9, 14, 22, 27, "wood")
        s.ellipse(16, 14, 5, 1.5, "moss")
        s.poly([(10, 21), (21, 21), (20, 25), (11, 25)], "red")
        teeth(s, 10, 21, 21); teeth(s, 12, 19, 25, 3)
        s.poly([(8, 27), (14, 27), (13, 30), (7, 30)], "wood", True)
        eyes(s, 12, 16, "cute")
    else:
        s.poly([(8, 16), (1, 14), (0, 18), (8, 21)], "wood", True)
        s.ellipse(3, 22.5, 3, 2.5, "body", True)
        s.rect(0, 22, 5, 22, "red", True)
        for x in (1, 3, 5):
            s.fix(x, 21, W, True)
        spikes(s, 16, 8, 14, 16.5, 18, "dark", ry_scale=0.5)
        s.ellipse(16, 8, 15.5, 6.5, "leaf")
        s.ellipse(9, 3, 4.5, 3, "leaf"); s.ellipse(23, 3, 4.5, 3, "leaf"); s.ellipse(16, 2, 4, 2.5, "leaf")
        for x, y in ((3, 8), (9, 3), (16, 1), (23, 3), (29, 8), (12, 8), (20, 8)):
            s.ellipse(x, y, 1.5, 1.5, "flower")
        for x, y in ((6, 6), (26, 6), (16, 6)):
            s.ellipse(x, y, 1.1, 1.1, "orange")
        s.rect(7, 13, 24, 28, "wood")
        s.poly([(7, 14), (24, 14), (22, 16), (9, 16)], "moss")
        s.poly([(8, 21), (23, 21), (21, 26), (10, 26)], "red")
        s.ellipse(16, 25, 4, 1, "flower")
        teeth(s, 8, 23, 21); teeth(s, 10, 22, 26, 3)
        for x, y0, y1 in ((8, 23, 27), (23, 23, 27)):
            s.rect(x, y0, x, y1, "dark")
        s.poly([(5, 27), (13, 27), (13, 31), (3, 31)], "wood", True)
        s.rect(3, 31, 13, 31, "dark", True)
        eyes(s, 10, 17, "fierce", (180, 255, 120))
    return s.render(pal("grass"))


# ---------- 천뢰기린 HeavenQilin: 천둥양 + 뇌광기린 (흰 기린, 먹구름 갈기, 말린 뿔 + 번개 뿔, 구름 후광) ----------
def heavenqilin(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 23), (25, 20), (24, 26)], "purple")
        for x, y in ((11, 17), (21, 17), (12, 14), (20, 14)):
            s.ellipse(x, y, 2.5, 2.5, "cloud")
        s.poly([(16, 11), (15, 15), (17, 15)], "bolt")
        horn(s, 11.5, 14.5, 1.8)
        s.ellipse(16, 18, 5, 4, "wool")
        s.ellipse(16, 21, 3, 2, "belly")
        s.ellipse(16, 25.5, 4.5, 3, "wool")
        for x in (12, 14):
            s.rect(x, 27, x, 29, "wool", True)
        s.rect(12, 29, 14, 29, "body", True)
        eyes(s, 13, 17, "small"); s.fix(15, 21, K); s.fix(16, 21, K); cheeks(s, 11, 19)
    elif lv == 2:
        s.poly([(21, 21), (29, 15), (28, 22), (30, 26), (22, 25)], "purple")
        bolt(s, 26, 9, 0.8)
        for x, y in ((9, 11), (23, 11), (9, 16), (23, 16), (12, 7), (20, 7), (22, 20)):
            s.ellipse(x, y, 3, 3, "cloud")
        s.poly([(16, 3), (14.5, 9), (17.5, 9)], "bolt")
        horn(s, 9, 8, 2.6)
        s.ellipse(16, 12, 5.5, 4.5, "wool")
        s.ellipse(16, 15.5, 3.5, 2, "belly")
        s.ellipse(16, 23, 6.5, 4.5, "wool")
        for x, y in ((12, 22), (20, 23), (16, 21)):
            s.fix(x, y, (255, 230, 120))
        for x in (10, 13):
            s.rect(x, 26, x + 1, 30, "wool", True)
        s.rect(10, 30, 14, 30, "body", True)
        eyes(s, 12, 11); s.fix(15, 16, K); s.fix(16, 16, K)
    else:
        for x, y in ((4, 10), (28, 10), (6, 4), (26, 4), (11, 1), (21, 1), (3, 16), (29, 16)):
            s.ellipse(x, y, 3, 2.5, "cloud")
        bolt(s, 0, 17, 1.0, mirror=True)
        s.poly([(22, 19), (31, 13), (30, 21), (31, 26), (23, 25)], "purple")
        for x, y in ((9, 12), (23, 12), (9, 17), (23, 17), (12, 7), (20, 7)):
            s.ellipse(x, y, 3.2, 3.2, "cloud")
        bolt(s, 14, 0, 1.3)
        horn(s, 8, 8, 3.8)
        s.ellipse(16, 11, 6, 5, "wool")
        s.ellipse(16, 15, 4, 2.2, "belly")
        s.ellipse(16, 22, 8.5, 5.5, "wool")
        for x, y in ((10, 20), (14, 19), (18, 20), (22, 22), (11, 24)):
            s.fix(x, y, (255, 230, 120))
        for x in (8, 12):
            s.rect(x, 26, x + 1, 31, "wool", True)
        s.rect(8, 31, 13, 31, "bolt", True)
        eyes(s, 11, 9, "fierce", (180, 150, 255)); mouth(s, 16, "fang")
    return s.render(pal("elec"))


# ---------- 대지거신 EarthTitan: 바위골렘 + 산맥거북 (어깨에 설산, 수정 왕관, 용암 핵, 이끼 몸) ----------
def earthtitan(lv):
    s = Spr()
    if lv == 1:
        mountain(s, 9.5, 18, 3, 6)
        mountain(s, 22.5, 18, 3, 6)
        crystal(s, 16, 11, 5)
        s.rect(7, 18, 9, 24, "stone", True)
        s.rect(10, 15, 21, 27, "stone")
        s.ellipse(16, 15.5, 4, 1, "moss")
        core(s, 16, 23, 1.8)
        s.rect(11, 28, 13, 29, "dark", True)
        eyes(s, 13, 18, "small"); s.fix(14, 18, (255, 200, 80), True)
    elif lv == 2:
        mountain(s, 5, 15, 4.5, 9)
        mountain(s, 27, 15, 4.5, 9)
        for x, h in ((13, 4), (16, 2), (19, 4)):
            crystal(s, x, h, 6)
        s.rect(10, 6, 21, 12, "stone")
        s.rect(7, 13, 24, 25, "stone")
        s.ellipse(16, 13, 6, 1.5, "moss")
        s.rect(2, 15, 6, 23, "stone", True)
        s.rect(2, 23, 6, 25, "dark", True)
        core(s, 16, 19, 2.8)
        s.ellipse(16, 19, 1, 1, "horn")
        s.rect(9, 26, 13, 30, "stone", True)
        s.rect(9, 30, 13, 30, "dark", True)
        for x, y in ((9, 15), (22, 22), (11, 23)):
            s.fix(x, y, (70, 64, 62))
        eyes(s, 12, 8, "sharp", (255, 200, 80))
        s.rect(14, 11, 17, 11, "dark")
    else:
        mountain(s, 4.5, 13, 4.5, 13)
        mountain(s, 27.5, 13, 4.5, 13)
        for x, h in ((11, 1), (13.5, 0), (16, 0), (18.5, 0), (21, 1)):
            crystal(s, x, h, 6)
        s.rect(10, 4, 21, 11, "stone")
        s.rect(3, 11, 28, 24, "stone")
        s.ellipse(16, 11, 9, 2, "moss")
        s.ellipse(6, 12, 3, 1.5, "moss", True)
        s.rect(0, 13, 3, 23, "stone", True)
        s.rect(0, 23, 4, 26, "dark", True)
        crystal(s, 1.5, 17, 4, True)
        core(s, 16, 18, 4)
        s.ellipse(16, 18, 1.8, 1.8, "horn")
        s.rect(6, 25, 13, 31, "stone", True)
        s.rect(6, 31, 13, 31, "dark", True)
        for x, y in ((7, 20), (24, 21), (10, 15), (21, 14)):
            s.fix(x, y, (70, 64, 62)); s.fix(x + 1, y + 1, (70, 64, 62))
        eyes(s, 12, 6, "fierce", (255, 200, 80))
        s.rect(13, 10, 18, 10, "glow")
    return s.render(pal("rock"))


CREATURES = [("SolarWolf", "일륜늑대", solarwolf), ("AbyssKraken", "심연크라켄", abysskraken),
             ("WorldTree", "세계수", worldtree), ("HeavenQilin", "천뢰기린", heavenqilin),
             ("EarthTitan", "대지거신", earthtitan)]
