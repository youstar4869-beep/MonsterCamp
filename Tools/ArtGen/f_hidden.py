from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W, YEL
from n_fire import flame
from n_water import tentacle
from c_grass import leaf
from c_elec import bolt
from c_rock import crystal
from f_cross import mix

# 히든 조합 5종: 상급보다 위(전설급). 상급보다 큰 장식 + 5종 공통 금색 왕관·테두리, Lv1부터 상급 Lv3 못지않게

GOLD = ((170, 110, 26), (236, 184, 52), (255, 238, 150))
CYAN = (120, 230, 255)
LAVA = (255, 150, 30)


def hpal(a, b=None):
    p = mix(a, b) if b else pal(a)
    p["gold"] = GOLD
    return p


def crown(s, y, half, h, gem, back=None):
    """금 왕관: 위 끝 y, 반폭 half, 높이 h(띠 2줄 포함), 가운데 보석. back: 뒤 장식과 섞이지 않게 테두리 색."""
    if back:
        s.rect(15 - half, y + h - 3, 16 + half, y + h, back)
        s.poly([(15 - half, y + h - 2), (15 - half, y - 1), (19.5 - half, y + h - 2)], back, True)
        s.poly([(13, y + h - 2), (16, y - 2.5), (19, y + h - 2)], back)
    s.rect(16 - half, y + h - 2, 15 + half, y + h - 1, "gold")
    s.poly([(16 - half, y + h - 2), (16 - half, y), (18.5 - half, y + h - 2)], "gold", True)
    s.poly([(14.2, y + h - 2), (16, y - 1), (17.8, y + h - 2)], "gold")
    s.fix(15, y + h - 2, gem); s.fix(16, y + h - 2, gem)


# ---------- 홍염제왕 CrimsonEmperor: 일륜늑대 + 화산코뿔소 (불꽃 갈기 늑대, 코뿔소 뿔, 용암 갑옷, 불꽃 왕관) ----------
def crimsonemperor(lv):
    s = Spr()
    if lv == 1:
        spikes(s, 16, 17, 7.5, 10.5, 12, "ember", ry_scale=0.85)
        spikes(s, 16, 17, 6.5, 8.5, 12, "flame", rot=0.26, ry_scale=0.85)
        s.poly([(6, 22), (8, 20), (12, 21), (11, 25), (7, 25)], "xstone", True)  # lava pauldrons
        s.rect(8, 21, 9, 21, "xglow", True)
        s.poly([(10, 12), (11, 17), (13.5, 15)], "body", True)
        s.ellipse(16, 24.5, 6, 4.5, "body")
        s.poly([(12, 21), (20, 21), (19, 26), (13, 26)], "xstone")
        s.rect(12, 21, 19, 21, "gold")
        s.ellipse(16, 23.5, 1.4, 1.4, "xglow")
        s.ellipse(16, 17.5, 6, 4.5, "body")
        s.ellipse(16, 19.5, 3, 1.6, "belly")
        s.poly([(15, 19.5), (17, 19.5), (16, 16.5)], "xhorn")
        crown(s, 11, 3, 4, (255, 60, 40), "dark")
        s.rect(11, 28, 13, 29, "body", True)
        s.rect(11, 29, 13, 29, "xstone", True)
        eyes(s, 12, 16, "sharp", YEL); mouth(s, 20)
    elif lv == 2:
        spikes(s, 16, 13, 9, 13, 14, "ember", ry_scale=0.85)
        spikes(s, 16, 13, 7.5, 10.5, 14, "flame", rot=0.22, ry_scale=0.85)
        s.poly([(3, 20), (5, 17), (10, 18), (10, 24), (4, 24)], "xstone", True)
        flame(s, 5, 12, 5, 1.6, True)
        s.rect(4, 17, 6, 17, "xglow", True)
        s.poly([(7, 4), (10, 11), (13.5, 8)], "body", True)
        s.poly([(8, 6), (10, 9), (12, 8)], "dark", True)
        s.ellipse(16, 23, 8, 6, "body")
        s.poly([(10, 19), (22, 19), (20, 27), (12, 27)], "xstone")
        s.rect(10, 19, 21, 19, "gold")
        s.ellipse(16, 23, 2, 2, "xglow")
        for x, y in ((12, 21), (13, 22), (13, 25)):
            s.fix(x, y, LAVA, True)
        s.ellipse(16, 12.5, 7, 5.5, "body")
        s.poly([(12, 13), (16, 18), (20, 13)], "belly")
        s.poly([(14.5, 16), (17.5, 16), (16, 11)], "xhorn")
        crown(s, 4, 4, 5, (255, 60, 40), "dark")
        s.rect(9, 27, 12, 30, "body", True)
        s.rect(9, 30, 12, 30, "xstone", True)
        eyes(s, 11, 10, "sharp", YEL); mouth(s, 16, "fang")
    else:
        spikes(s, 16, 10.5, 11.5, 17, 16, "ember", ry_scale=0.85)
        spikes(s, 16, 10.5, 9.5, 13, 16, "flame", rot=0.2, ry_scale=0.85)
        s.poly([(0, 19), (3, 15), (10, 16), (10, 24), (1, 24)], "xstone", True)
        flame(s, 4, 8, 7, 2.2, True)
        s.rect(2, 15, 5, 15, "xglow", True)
        s.rect(0, 23, 9, 23, "gold", True)
        s.poly([(6, 2), (9, 10), (13.5, 6)], "body", True)
        s.poly([(7, 4), (9, 8), (12, 6)], "dark", True)
        s.ellipse(16, 23, 9.5, 6.5, "body")
        s.poly([(9, 18), (23, 18), (21, 28), (11, 28)], "xstone")
        s.rect(9, 18, 22, 18, "gold")
        s.ellipse(16, 23, 2.6, 2.6, "xglow")
        s.ellipse(16, 23, 1, 1, "core")
        for x, y in ((11, 20), (12, 21), (12, 22), (13, 25), (14, 26)):
            s.fix(x, y, LAVA, True)
        s.ellipse(16, 11, 8, 6, "body")
        s.poly([(11, 12), (16, 18), (21, 12)], "belly")
        s.poly([(14.5, 15), (17.5, 15), (16, 9)], "xhorn")
        crown(s, 1, 5, 6, (255, 60, 40), "dark")
        s.rect(7, 27, 12, 31, "body", True)
        s.rect(7, 31, 12, 31, "xstone", True)
        s.rect(7, 27, 12, 27, "gold", True)
        eyes(s, 10, 8, "fierce", YEL); mouth(s, 16, "fang")
    return s.render(hpal("fire", "rock"))


# ---------- 창해신룡 OceanDragon: 심연크라켄 + 폭풍고래 (바다 용머리, 크라켄 촉수 꼬리, 고래 배, 번개 지느러미) ----------
def oceandragon(lv):
    s = Spr()
    if lv == 1:
        for x0, x1 in ((13, 9), (15, 13.5)):
            tentacle(s, x0, 24, x1, 29, 1.3, "deep", True)
        s.poly([(11, 19), (3, 12), (4, 18), (3, 22), (10, 24)], "fin", True)
        bolt(s, 4, 14, 0.7, "xbolt", True)
        s.poly([(11, 14), (7, 10), (8, 16)], "fin", True)
        s.ellipse(16, 23.5, 5.5, 4, "body")
        s.ellipse(16, 24.5, 3.5, 2.5, "belly")
        s.ellipse(16, 17, 6, 4.5, "body")
        s.ellipse(16, 19.5, 3.5, 1.8, "belly")
        s.poly([(12, 19), (13, 19), (10, 23), (9, 23)], "xbolt", True)  # whiskers
        s.poly([(11, 14), (9, 9), (12.5, 13)], "xbolt", True)
        crown(s, 10, 3, 4, CYAN)
        eyes(s, 12, 16, "sharp", CYAN); mouth(s, 20, "w")
    elif lv == 2:
        for x0, x1, y1 in ((11, 4, 29), (13, 9, 30), (15, 14, 30)):
            tentacle(s, x0, 22, x1, y1, 1.5, "deep", True)
        s.poly([(9, 17), (0, 6), (1, 12), (0, 15), (1, 19), (0, 22), (8, 23)], "fin", True)
        bolt(s, 1, 9, 0.9, "xbolt", True)
        s.poly([(10, 10), (4, 5), (5, 11), (10, 14)], "fin", True)
        s.ellipse(16, 22, 7, 5.5, "body")
        s.ellipse(16, 23, 4.5, 3.5, "belly")
        for y in (21, 23, 25):
            s.rect(13, y, 18, y, "foam")
        s.ellipse(16, 12, 7, 5.5, "body")
        s.ellipse(16, 15.5, 4, 2, "belly")
        s.poly([(11, 15), (12, 15), (7, 22), (6, 22)], "xbolt", True)
        s.poly([(10.5, 9), (7, 2), (9, 3), (12.5, 8)], "xbolt", True)
        crown(s, 3, 4, 5, CYAN)
        eyes(s, 11, 10, "sharp", CYAN); mouth(s, 16, "fang")
    else:
        for r, c in ((15.5, "fin"), (12, "body")):
            s.ellipse(16, 27, r, r * 0.3, c)
        for x0, x1, y1 in ((10, 1, 28), (12, 4, 31), (14, 9, 31), (15.5, 14, 31)):
            tentacle(s, x0, 22, x1, y1, 1.8, "deep", True)
        s.poly([(8, 15), (0, 1), (1, 8), (0, 11), (1, 15), (0, 19), (7, 21)], "fin", True)
        s.poly([(7, 15), (2, 6), (2, 13), (7, 18)], "deep", True)
        bolt(s, 0, 8, 1.1, "xbolt", True)
        s.poly([(10, 8), (3, 2), (4, 9), (10, 12)], "fin", True)
        bolt(s, 4, 3, 0.6, "xbolt", True)
        s.ellipse(16, 21.5, 9, 6.5, "body")
        s.ellipse(16, 22.5, 6, 4.5, "belly")
        for y in (20, 22, 24, 26):
            s.rect(12, y, 19, y, "foam")
        s.rect(7, 18, 24, 18, "gold")
        s.ellipse(16, 10.5, 8, 6, "body")
        s.ellipse(16, 14.5, 4.5, 2.2, "belly")
        for x, y in ((10, 8), (22, 7)):
            s.ellipse(x, y, 1, 1, "foam")
        s.poly([(10, 14), (11.5, 14), (5, 23), (4, 23)], "xbolt", True)
        s.poly([(10, 7), (6, 0), (8, 0.5), (12, 6)], "xbolt", True)
        crown(s, 1, 5, 6, CYAN)
        eyes(s, 10, 9, "fierce", CYAN); mouth(s, 15, "fang")
    return s.render(hpal("water", "elec"))


# ---------- 녹림여제 ForestEmpress: 세계수 + 꽃덩굴여우 (꽃 수관 후광, 여우 얼굴, 잎 망토, 꽃 티아라) ----------
def wreath(s, cy, rx, ry, n, flowers):
    spikes(s, 16, cy, rx - 2, rx + 1, n, "dark", ry_scale=ry / rx)
    s.ellipse(16, cy, rx, ry, "leaf")
    s.ellipse(16, cy, rx * 0.62, ry * 0.55, None)
    for x, y in flowers:
        s.ellipse(x, y, 1.4, 1.4, "flower", True)


def forestempress(lv):
    s = Spr()
    if lv == 1:
        wreath(s, 17, 10, 4, 14, ((7, 17), (9.5, 14)))
        leaf(s, 12, 19, 4, 27, "flower", w=2.4, mirror=True)  # petal-leaf cape
        leaf(s, 13, 20, 9, 29, "leaf", w=1.8, mirror=True)
        s.poly([(9.5, 10), (11, 17), (13.5, 15)], "body", True)
        s.poly([(10.5, 12), (11.5, 15.5), (12.5, 15)], "flower", True)
        s.ellipse(16, 24.5, 5, 4.5, "body")
        s.ellipse(16, 25, 2.5, 3, "belly")
        s.ellipse(16, 18, 5.5, 4.5, "body")
        s.poly([(13, 19), (16, 22), (19, 19)], "belly")
        crown(s, 10, 3, 4, (255, 120, 170))
        s.rect(12, 28, 13, 29, "body", True)
        eyes(s, 12, 17, "sharp", (180, 255, 120)); cheeks(s, 11, 20); s.fix(15, 21, K); s.fix(16, 21, K)
    elif lv == 2:
        wreath(s, 12.5, 13, 5, 18, ((4, 12.5), (6, 9), (10.5, 8), (6, 16)))
        leaf(s, 11, 16, 1, 25, "flower", w=3, mirror=True)
        leaf(s, 12, 18, 5, 30, "leaf", w=2.4, mirror=True)
        s.ellipse(4, 20, 1, 1, "orange", True)
        s.poly([(7, 2), (9.5, 12), (13, 8.5)], "body", True)
        s.poly([(8, 4), (9.5, 9), (11.5, 8.5)], "flower", True)
        s.ellipse(16, 22.5, 6.5, 6, "body")
        s.ellipse(16, 23, 3.2, 4, "belly")
        s.rect(10, 18, 21, 18, "gold")
        s.ellipse(16, 12.5, 6.5, 5.5, "body")
        s.poly([(12, 13), (16, 18), (20, 13)], "belly")
        crown(s, 3, 4, 5, (255, 120, 170))
        s.rect(10, 27, 13, 30, "body", True)
        s.rect(10, 30, 13, 30, "wood", True)
        eyes(s, 11, 11, "sharp", (180, 255, 120)); cheeks(s, 10, 15); mouth(s, 16)
    else:
        wreath(s, 11.5, 15.5, 6.5, 22, ((1.5, 11.5), (3, 7.5), (7.5, 5.5), (3, 15.5)))
        for x, y in ((5, 9.5), (10, 6)):
            s.ellipse(x, y, 0.9, 0.9, "orange", True)
        leaf(s, 10, 15, 0, 26, "flower", w=3.5, mirror=True)
        leaf(s, 11, 17, 3, 31, "leaf", w=3, mirror=True)
        s.ellipse(3.5, 21, 1.2, 1.2, "orange", True)
        s.poly([(6, 0), (8.5, 11), (13, 7)], "body", True)
        s.poly([(7, 2), (9, 8), (11.5, 7)], "flower", True)
        s.ellipse(16, 22.5, 8, 6.5, "body")
        s.ellipse(16, 23, 4, 4.5, "belly")
        s.rect(8, 17, 23, 17, "gold")
        s.ellipse(11, 18, 1.4, 1.4, "flower", True)
        s.ellipse(16, 11, 7.5, 6, "body")
        s.poly([(11, 12), (16, 18), (21, 12)], "belly")
        crown(s, 1, 5, 6, (255, 120, 170))
        s.rect(8, 27, 12, 31, "body", True)
        s.rect(8, 31, 12, 31, "wood", True)
        eyes(s, 10, 9, "fierce", (180, 255, 120)); cheeks(s, 9, 13); mouth(s, 15, "smile")
    return s.render(hpal("grass"))


# ---------- 뇌제 ThunderEmperor: 천뢰기린 + 뇌전족제비 (파란 날렵한 몸, 금 후광, 번개 뿔, 먹구름 갈기, 지느러미 날개) ----------
def halo(s, cy, rx, ry, t=1.5):
    s.ellipse(16, cy, rx, ry, "gold")
    s.ellipse(16, cy, rx - t, ry - t, None)


def thunderemperor(lv):
    s = Spr()
    if lv == 1:
        halo(s, 16, 8.5, 6.5)
        s.poly([(11, 22), (5, 19), (4, 24), (11, 26)], "purple", True)
        bolt(s, 5, 20, 0.6, mirror=True)
        for x, y in ((11, 21), (21, 21)):
            s.ellipse(x, y, 2.2, 2.2, "cloud")
        bolt(s, 9, 9, 1.0, mirror=True)  # bolt horns
        s.rect(12, 21, 19, 27, "blue")
        s.ellipse(16, 24, 2, 3, "belly")
        s.ellipse(16, 17, 5, 4, "blue")
        s.ellipse(16, 19.5, 3, 1.6, "belly")
        crown(s, 10, 3, 4, (180, 150, 255))
        s.rect(12, 28, 13, 29, "blue", True)
        s.rect(12, 29, 13, 29, "gold", True)
        eyes(s, 13, 16, "sharp", (180, 150, 255)); mouth(s, 20)
    elif lv == 2:
        spikes(s, 16, 12, 9, 12, 10, "bolt", ry_scale=0.85)
        halo(s, 12, 11, 8.5)
        s.poly([(11, 18), (2, 13), (1, 20), (4, 25), (11, 24)], "purple", True)
        s.poly([(4, 16), (3, 21), (7, 20)], "dark", True)
        for x, y in ((9, 15), (23, 15), (11, 18), (21, 18)):
            s.ellipse(x, y, 2.6, 2.6, "cloud")
        bolt(s, 7, 2, 1.1, mirror=True)
        s.rect(11, 17, 20, 27, "blue")
        s.ellipse(16, 22, 2.5, 4, "belly")
        bolt(s, 14.8, 19, 0.6)
        s.ellipse(16, 11, 6, 5, "blue")
        s.ellipse(16, 14, 3.5, 2, "belly")
        crown(s, 3, 4, 5, (180, 150, 255))
        s.rect(11, 27, 13, 30, "blue", True)
        s.rect(11, 30, 13, 30, "gold", True)
        eyes(s, 11, 9, "sharp", (180, 150, 255)); mouth(s, 14)
    else:
        spikes(s, 16, 11, 12, 16, 12, "bolt", ry_scale=0.8)
        halo(s, 11, 13.5, 10.5, 1.7)
        s.poly([(10, 17), (1, 10), (0, 19), (2, 24), (0, 28), (10, 24)], "purple", True)
        s.poly([(4, 14), (3, 20), (8, 19)], "dark", True)
        bolt(s, 1, 20, 0.8, mirror=True)
        for x, y in ((8, 14), (24, 14), (10, 18), (22, 18), (12, 15), (20, 15)):
            s.ellipse(x, y, 3, 3, "cloud")
        bolt(s, 6, -1, 1.5, mirror=True)
        s.rect(10, 17, 21, 27, "blue")
        s.ellipse(16, 22, 3.5, 5, "belly")
        bolt(s, 14.5, 18.5, 0.9)
        for y in (19, 22, 25):
            s.fix(10, y, (255, 250, 150), True)
        s.ellipse(16, 10, 7, 5.5, "blue")
        s.ellipse(16, 13.5, 4, 2.2, "belly")
        crown(s, 1, 4, 5, (180, 150, 255))
        s.rect(10, 27, 13, 31, "blue", True)
        s.rect(10, 31, 13, 31, "gold", True)
        eyes(s, 10, 8, "fierce", (180, 150, 255)); mouth(s, 14, "fang")
    return s.render(hpal("elec"))


# ---------- 수정거신왕 CrystalTitanKing: 대지거신 + 수정갑주 (거신 몸, 수정 어깨, 금테 수정 방패 가슴, 수정 왕관) ----------
def kite(s, x0, x1, y0, y1, y2, c):
    s.poly([(x0, y0), (x1, y0), (x1, y1), (16, y2), (x0, y1)], c)


def crystaltitanking(lv):
    s = Spr()
    band = (70, 64, 62)
    if lv == 1:
        s.poly([(7, 9), (3.5, 16), (7, 24), (10.5, 16)], "crystal", True)  # crystal pauldrons
        crystal(s, 4, 13, 5, True)
        s.rect(10, 13, 21, 18, "stone")
        s.rect(10, 15, 21, 15, "dark")
        s.rect(9, 18, 22, 27, "stone")
        kite(s, 10, 21, 18, 23, 29, "gold")
        kite(s, 11, 20, 19, 23, 27.5, "crystal")
        s.rect(6, 19, 8, 24, "stone", True)
        crown(s, 9, 4, 4, (200, 180, 255))
        s.rect(10, 28, 12, 29, "stone", True)
        s.rect(10, 29, 12, 29, "dark", True)
        eyes(s, 12, 16, "sharp", (200, 180, 255))
    elif lv == 2:
        s.poly([(5, 6), (1, 13), (5, 20), (9, 13)], "crystal", True)
        crystal(s, 2, 10, 5, True)
        s.rect(10, 7, 21, 13, "stone")
        for y in (9, 11):
            s.rect(10, y, 21, y, "dark")
        s.rect(5, 13, 26, 25, "stone")
        s.rect(5, 13, 26, 13, "gold")
        kite(s, 9, 22, 15, 21, 29, "gold")
        kite(s, 10, 21, 16, 21, 27.5, "crystal")
        s.ellipse(16, 20, 1.5, 1.5, "glow")
        s.rect(2, 14, 4, 23, "stone", True)
        s.rect(2, 23, 4, 25, "dark", True)
        crown(s, 2, 5, 5, (200, 180, 255))
        s.rect(7, 26, 11, 30, "stone", True)
        s.rect(7, 30, 11, 30, "dark", True)
        eyes(s, 12, 9, "sharp", (200, 180, 255))
    else:
        s.poly([(4.5, 1), (0, 9), (4.5, 17), (9, 9)], "crystal", True)
        crystal(s, 1, 6, 6, True)
        s.poly([(8, 3), (6.5, 7), (8, 12), (9.5, 7)], "crystal", True)
        s.rect(10, 5, 21, 12, "stone")
        for y in (7, 10):
            s.rect(10, y, 21, y, "dark")
        s.rect(3, 12, 28, 25, "stone")
        s.rect(3, 12, 28, 12, "gold")
        for x, y in ((5, 15), (25, 17), (6, 21)):
            s.fix(x, y, band); s.fix(x + 1, y, band)
        kite(s, 8, 23, 14, 21, 30, "gold")
        kite(s, 9, 22, 15, 21, 28.5, "crystal")
        s.poly([(14, 17), (16, 15.5), (18, 17), (16, 24)], "horn")
        s.rect(0, 13, 2, 23, "stone", True)
        s.rect(0, 23, 3, 26, "dark", True)
        crystal(s, 1, 17, 5, True)
        crown(s, 1, 5, 5, (200, 180, 255))
        s.rect(5, 26, 10, 31, "stone", True)
        s.rect(5, 31, 10, 31, "dark", True)
        eyes(s, 11, 8, "fierce", (200, 180, 255))
    return s.render(hpal("rock"))


CREATURES = [("CrimsonEmperor", "홍염제왕", crimsonemperor), ("OceanDragon", "창해신룡", oceandragon),
             ("ForestEmpress", "녹림여제", forestempress), ("ThunderEmperor", "뇌제", thunderemperor),
             ("CrystalTitanKing", "수정거신왕", crystaltitanking)]


if __name__ == "__main__":
    # 미리보기: 히든 Lv1~3 | 재료 Lv3 두 장, 4배 (python f_hidden.py → out/fusion_hidden_preview.png)
    from PIL import Image
    from px import sheet
    import f_top, f_cross, f_mid
    src = {k: fn for m in (f_top, f_cross, f_mid) for k, _, fn in m.CREATURES}
    pairs = {"CrimsonEmperor": ("SolarWolf", "VolcanoRhino"), "OceanDragon": ("AbyssKraken", "TempestWhale"),
             "ForestEmpress": ("WorldTree", "BloomVineFox"), "ThunderEmperor": ("HeavenQilin", "StormWeasel"),
             "CrystalTitanKing": ("EarthTitan", "CrystalArmor")}
    blank = Image.new("RGBA", (8, 32), (0, 0, 0, 0))
    rows = [sheet([fn(1), fn(2), fn(3), blank] + [src[k](3) for k in pairs[key]], scale=4)
            for key, _, fn in CREATURES]
    out = Image.new("RGBA", (max(r.width for r in rows), sum(r.height for r in rows)), (46, 52, 64, 255))
    y = 0
    for r in rows:
        out.alpha_composite(r, (0, y)); y += r.height
    out.save("out/fusion_hidden_preview.png")
