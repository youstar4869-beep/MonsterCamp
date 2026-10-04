from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W, YEL
from n_fire import flame
from n_water import bubble
from c_grass import leaf
from c_elec import bolt
from c_rock import crystal

# 조합 크리처 중급 5종: 재료 두 마리를 섞고, 같은 레벨 재료보다 장식(갑옷·큰 꼬리)을 하나 더

# ---------- 화산곰 VolcanoBear: 화로곰 + 숯불도마뱀 (잿빛 곰, 분화구 배, 바위 어깨갑옷, 도마뱀 꼬리) ----------
def volcanobear(lv):
    s = Spr()
    sc = "gray"
    if lv == 1:
        s.poly([(20, 27), (26, 26), (27, 23), (28, 27), (22, 29)], sc)
        flame(s, 27.5, 18, 4.5, 1.6)
        s.ellipse(10.5, 13.5, 2.3, 2.3, sc, True)
        s.ellipse(10.5, 13.5, 1.1, 1.1, "flame", True)
        s.ellipse(16, 17.5, 6, 5, sc)
        s.ellipse(16, 19.5, 2.5, 1.5, "belly")
        s.ellipse(16, 25, 6, 4.5, sc)
        s.ellipse(16, 25.5, 3, 2.5, "dark")
        s.ellipse(16, 25.5, 1.8, 1.5, "flame")
        s.ellipse(10, 22, 2, 1.5, "dark", True)  # shoulder armor
        s.rect(11, 29, 13, 29, sc, True)
        eyes(s, 13, 16, "small"); mouth(s, 19); cheeks(s, 11, 18)
    elif lv == 2:
        for x in (13, 16, 19):
            s.poly([(x, 2), (x - 1.5, 6), (x + 1.5, 6)], "flame")
        s.poly([(21, 25), (29, 23), (31, 18), (31, 25), (23, 29)], sc)
        flame(s, 31, 12, 6, 1.6)
        s.ellipse(9.5, 6.5, 3, 3, sc, True)
        s.ellipse(9.5, 6.5, 1.5, 1.5, "flame", True)
        s.ellipse(16, 11, 7.5, 6, sc)
        s.ellipse(16, 14, 3, 2, "belly")
        s.ellipse(16, 22.5, 9, 7, sc)
        s.ellipse(16, 23, 5, 4.5, "dark")
        s.ellipse(16, 23.5, 3.5, 3, "flame")
        s.ellipse(16, 24, 1.8, 1.5, "core")
        s.ellipse(7.5, 18, 3.5, 2.5, "dark", True)  # rock pauldrons
        s.poly([(6, 16), (7, 13), (8.5, 16)], "flame", True)
        s.ellipse(7, 22.5, 2.2, 3.5, sc, True)
        s.rect(10, 28, 13, 30, sc, True)
        eyes(s, 12, 9); mouth(s, 14); cheeks(s, 10, 12)
    else:
        spikes(s, 16, 8, 8, 12, 9, "flame", ry_scale=0.75)
        spikes(s, 16, 8, 7, 9.5, 9, "core", rot=0.35, ry_scale=0.75)
        s.poly([(21, 25), (30, 21), (31, 13), (31, 26), (23, 30)], sc)
        flame(s, 30.5, 5, 8, 1.5)
        s.ellipse(9, 4, 3, 3, sc, True)
        s.ellipse(9, 4, 1.5, 1.5, "flame", True)
        s.ellipse(16, 9, 8, 6.5, sc)
        s.ellipse(16, 12, 3.5, 2.2, "belly")
        s.ellipse(16, 22, 10.5, 8.5, sc)
        s.ellipse(16, 22.5, 6, 5.5, "dark")
        s.ellipse(16, 23, 4.5, 4, "flame")
        s.ellipse(16, 23.5, 2.5, 2, "core")
        s.ellipse(5.5, 16, 4.5, 3, "dark", True)
        s.poly([(3, 15), (4, 10), (6, 15)], "flame", True)
        s.poly([(6, 14), (7.5, 11), (8.5, 14)], "flame", True)
        s.ellipse(4.5, 22.5, 2.6, 4, sc, True)
        s.rect(8, 29, 12, 31, sc, True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 11, 7, "fierce"); mouth(s, 12, "fang")
    return s.render(pal("fire"))


# ---------- 파도수달 WaveOtter: 물보라수달 + 산호해마 (파란 수달, 산호 볏, 큰 파도 꼬리) ----------
def waveotter(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 28), (26, 26), (28, 21), (26, 18), (30, 20), (30, 26), (23, 30)], "fin")  # wave tail
        s.ellipse(27, 19.5, 1.2, 1.2, "foam")
        for x in (14, 16, 18):
            s.poly([(x - 0.8, 13), (x, 10), (x + 0.8, 13)], "shell")
        s.ellipse(11, 14.5, 1.8, 1.8, "body", True)
        s.ellipse(16, 17.5, 6, 5, "body")
        s.ellipse(16, 19.5, 3.5, 2.2, "belly")
        s.ellipse(16, 25.5, 5, 4, "body")
        s.ellipse(16, 26, 2.8, 2.8, "belly")
        s.rect(12, 29, 13, 29, "body", True)
        eyes(s, 13, 16, "small"); mouth(s, 19); cheeks(s, 12, 18)
        s.fix(11, 19, K, True)
    elif lv == 2:
        s.poly([(21, 27), (27, 24), (29, 17), (26, 12), (31, 13), (31, 24), (25, 30)], "fin")
        s.poly([(26, 12), (29, 10), (31, 13)], "foam")
        bubble(s, 27, 7, 1.2)
        for x, h in ((12, 4), (14.5, 2), (17.5, 2), (20, 4)):
            s.poly([(x - 1, 7), (x, h), (x + 1, 7)], "shell")
        s.ellipse(10, 8, 2, 2, "body", True)
        s.ellipse(16, 11.5, 7, 5.5, "body")
        s.ellipse(16, 14, 4, 2.5, "belly")
        s.ellipse(16, 22, 7, 7, "body")
        s.ellipse(16, 22.5, 4, 5, "belly")
        s.ellipse(9, 20, 1.8, 2.8, "body", True)
        s.rect(11, 28, 13, 29, "body", True)
        eyes(s, 12, 10); mouth(s, 14); cheeks(s, 11, 13)
        for x, y in ((9, 14), (9, 15)):
            s.fix(x, y, K, True)
    else:
        s.poly([(21, 27), (28, 23), (30, 14), (26, 7), (31, 6), (31, 25), (25, 31)], "fin")
        s.poly([(26, 7), (29, 3), (31, 6)], "foam")
        s.poly([(28, 12), (30, 10), (30, 20)], "foam")
        bubble(s, 25, 2, 1.4)
        for x, h in ((10, 3), (13, 0), (16, 1), (19, 0), (22, 3)):
            s.poly([(x - 1.2, 6), (x, h), (x + 1.2, 6)], "shell")
        s.ellipse(9.5, 7.5, 2.2, 2.2, "body", True)
        s.ellipse(16, 11.5, 7.5, 6, "body")
        s.ellipse(16, 14.5, 4.5, 2.5, "belly")
        s.ellipse(16, 22, 9, 8, "body")
        s.ellipse(16, 22.5, 5, 5.5, "belly")
        s.ellipse(16, 22, 2.5, 2.5, "shell")
        s.ellipse(7, 20, 2.2, 3.5, "body", True)
        s.poly([(3, 18), (5, 16), (5, 22)], "fin", True)
        s.rect(10, 28, 13, 30, "body", True)
        s.rect(10, 30, 13, 30, "deep", True)
        eyes(s, 11, 9, "sharp"); mouth(s, 14, "fang")
    return s.render(pal("water"))


# ---------- 꽃덩굴여우 BloomVineFox: 덩굴여우 + 꽃잎나방 (꽃잎 날개 여우, 더듬이, 큰 꽃 꼬리) ----------
def bloomvinefox(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 26), (24, 18), (28, 16), (28, 22), (23, 28)], "leaf")  # flower tail
        s.ellipse(27, 16.5, 1.6, 1.6, "flower")
        s.poly([(13, 20), (7, 17), (6, 23), (13, 25)], "flower", True)  # petal wings
        s.poly([(10, 11), (11, 16), (13.5, 14)], "body", True)
        s.ellipse(16, 18, 5.5, 4.5, "body")
        s.ellipse(16, 20, 2.5, 1.8, "belly")
        s.ellipse(16, 25.5, 4, 3.5, "body")
        s.ellipse(16, 26, 2, 2, "belly")
        s.rect(12, 28, 13, 29, "body", True)
        s.fix(14, 12, (30, 96, 50), True); s.fix(13, 11, (244, 120, 160), True)  # antennae
        eyes(s, 13, 17, "small"); mouth(s, 20); cheeks(s, 12, 19)
    elif lv == 2:
        s.poly([(21, 26), (24, 15), (30, 9), (31, 18), (26, 28)], "leaf")
        s.poly([(25, 18), (29, 12), (29, 19)], "body")
        spikes(s, 29, 10, 1.3, 3, 5, "flower")
        s.ellipse(29, 10, 1, 1, "orange")
        s.poly([(10, 17), (2, 12), (1, 19), (4, 24), (10, 23)], "flower", True)
        s.ellipse(4, 17, 1.3, 1.3, "orange", True)
        s.poly([(8, 3), (9, 10), (12.5, 7)], "body", True)
        s.poly([(9, 5), (10, 8), (11.5, 7)], "flower", True)
        s.rect(13, 3, 13, 6, "dark", True)
        s.ellipse(12.5, 2.5, 1.2, 1.2, "flower", True)
        s.ellipse(16, 11, 7, 5.5, "body")
        s.poly([(12, 12), (16, 17), (20, 12)], "belly")
        s.ellipse(16, 22, 6, 6, "body")
        s.ellipse(16, 22.5, 3, 4, "belly")
        s.rect(11, 27, 13, 29, "body", True)
        eyes(s, 12, 9); mouth(s, 14); cheeks(s, 10, 13)
    else:
        s.poly([(21, 26), (23, 12), (31, 3), (31, 18), (26, 29)], "leaf")
        s.poly([(25, 20), (29, 9), (29, 21)], "body")
        spikes(s, 29, 5, 1.6, 3.5, 5, "flower")
        s.ellipse(29, 5, 1.2, 1.2, "orange")
        s.ellipse(25, 16, 1.4, 1.4, "flower")
        s.poly([(10, 14), (0, 6), (0, 16), (2, 19), (0, 24), (5, 27), (10, 22)], "flower", True)
        s.ellipse(3.5, 12, 1.5, 1.5, "orange", True)
        s.ellipse(3, 23, 1.2, 1.2, "leaf", True)
        s.poly([(7, 0), (8.5, 9), (12.5, 6)], "body", True)
        s.poly([(8, 2), (9.5, 7), (11.5, 6)], "flower", True)
        s.rect(13, 1, 13, 5, "dark", True)
        s.ellipse(12.5, 1, 1.3, 1.3, "flower", True)
        s.ellipse(16, 10.5, 8, 6, "body")
        s.poly([(11, 11), (16, 18), (21, 11)], "belly")
        s.ellipse(16, 22.5, 8, 6.5, "body")
        s.ellipse(16, 23, 4, 4.5, "belly")
        s.rect(8, 27, 12, 31, "body", True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 10, 8, "fierce"); mouth(s, 15, "fang")
    return s.render(pal("grass"))


# ---------- 뇌전족제비 StormWeasel: 번쩍족제비 + 전기뱀장어 (파란 뱀장어 족제비, 지느러미 볏, 큰 번개 꼬리) ----------
def stormweasel(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 26), (27, 20), (25, 24), (30, 21), (24, 28)], "bolt")
        s.poly([(15, 14), (16, 10), (17, 14)], "wing")  # fin crest
        s.ellipse(16, 18, 5.5, 4.5, "blue")
        s.poly([(12, 17), (8.5, 14), (9, 18.5)], "wing", True)  # ear fins
        s.ellipse(16, 20, 2.5, 1.5, "belly")
        s.ellipse(16, 25, 4, 4, "blue")
        s.ellipse(16, 25.5, 2, 2.5, "belly")
        s.poly([(12, 23), (9, 22), (10, 26)], "wing", True)  # side fins
        s.rect(13, 29, 13, 29, "blue", True)
        eyes(s, 13, 17, "small"); mouth(s, 20)
        s.fix(12, 19, YEL, True)
    elif lv == 2:
        s.poly([(20, 25), (31, 12), (27, 20), (31, 19), (24, 29)], "bolt")
        s.poly([(25, 21), (29, 16), (28, 20)], "wing")
        s.poly([(14, 8), (16, 1), (18, 8)], "wing")
        s.poly([(15, 8), (16, 4), (17, 8)], "bolt")
        s.ellipse(16, 11.5, 6.5, 5, "blue")
        s.poly([(11, 10), (5.5, 5), (6, 11), (11, 13)], "wing", True)
        s.ellipse(16, 14, 3.5, 2, "belly")
        s.rect(12, 16, 19, 26, "blue")
        s.ellipse(16, 21, 2.5, 4, "belly")
        for y in (18, 21, 24):
            s.fix(12, y, YEL, True)
        s.poly([(12, 18), (7, 16), (8, 22)], "wing", True)
        s.rect(12, 26, 13, 29, "blue", True)
        eyes(s, 12, 9); mouth(s, 14)
    else:
        s.poly([(19, 24), (31, 5), (28, 15), (31, 15), (26, 24), (29, 24), (23, 30)], "bolt")
        s.poly([(22, 22), (29, 11), (27, 19), (25, 24)], "wing")
        s.poly([(13.5, 7), (16, 0), (18.5, 7)], "wing")
        s.poly([(15, 7), (16, 3), (17, 7)], "bolt")
        s.ellipse(16, 11, 7.5, 5.5, "blue")
        s.poly([(10, 9), (3, 3), (3.5, 10), (10, 13)], "wing", True)
        s.poly([(8, 9), (5, 6), (5, 10)], "bolt", True)
        s.ellipse(16, 14, 4, 2, "belly")
        s.rect(10, 16, 21, 27, "blue")
        s.ellipse(16, 21.5, 3.5, 5, "belly")
        bolt(s, 14.5, 18, 0.9)
        for y in (17, 20, 23, 26):
            s.fix(10, y, YEL, True)
        s.poly([(10, 17), (3, 13), (2, 19), (5, 23), (10, 22)], "wing", True)
        s.poly([(5, 16), (4, 20), (8, 19)], "dark", True)
        s.rect(10, 27, 13, 31, "blue", True)
        s.rect(10, 31, 13, 31, "dark", True)
        eyes(s, 10, 9, "fierce", YEL); mouth(s, 14, "fang")
    return s.render(pal("elec"))


# ---------- 수정갑주 CrystalArmor: 자갈갑옷 + 수정고슴도치 (줄무늬 돌 투구, 수정 볏, 수정 가슴갑옷) ----------
def crystalarmor(lv):
    s = Spr()
    band = (70, 64, 62)
    if lv == 1:
        for x in (13, 16, 19):
            s.poly([(x - 0.8, 15), (x, 11), (x + 0.8, 15)], "crystal")
        s.ellipse(16, 20, 7.5, 5.5, "stone")
        s.rect(10, 18, 21, 18, "dark")
        s.ellipse(16, 23.5, 4.5, 3.5, "belly")
        s.ellipse(16, 26.5, 2, 1.2, "crystal")
        s.ellipse(11, 24, 1.4, 1.4, "stone", True)
        s.rect(12, 28, 13, 28, "belly", True)
        eyes(s, 13, 22, "small"); mouth(s, 25)
    elif lv == 2:
        for x, h in ((10, 6), (13, 3), (16, 2), (19, 3), (22, 6)):
            crystal(s, x, h, 7)
        s.ellipse(9, 13, 1.8, 2.5, "belly", True)
        s.ellipse(16, 15, 10, 7.5, "stone")
        for y in (11, 14):
            s.rect(7, y, 24, y, "dark")
        s.ellipse(16, 20, 6, 4.5, "belly")
        s.ellipse(16, 25, 5, 3, "stone")
        s.poly([(14, 23), (16, 22), (18, 23), (16, 27)], "crystal")  # chest plate gem
        s.ellipse(8.5, 21.5, 2.5, 2.5, "stone", True)
        s.rect(10, 27, 13, 29, "belly", True)
        eyes(s, 12, 18); mouth(s, 21, "smile")
    else:
        for x, h in ((5, 6), (9, 2), (13, 0), (16, 0), (19, 0), (23, 2), (27, 6)):
            crystal(s, x, h, 8)
        s.ellipse(16, 13, 13, 8.5, "stone")
        for y in (9, 12, 15):
            s.rect(4, y, 27, y, "dark")
        for x, y in ((7, 11), (12, 8), (20, 8), (24, 13), (10, 14)):
            s.fix(x, y, (160, 150, 136)); s.fix(x + 1, y, (160, 150, 136))
        s.ellipse(16, 19, 7, 4.5, "belly")
        s.ellipse(16, 25, 7.5, 4, "stone")
        s.poly([(12, 23), (16, 21), (20, 23), (16, 29)], "crystal")
        s.ellipse(5.5, 21, 3.5, 3.5, "stone", True)
        crystal(s, 5.5, 16, 5, True)
        s.rect(8, 28, 12, 31, "belly", True)
        s.rect(8, 31, 12, 31, "dark", True)
        eyes(s, 11, 17, "fierce", (200, 180, 255)); mouth(s, 21, "fang")
    return s.render(pal("rock"))


CREATURES = [("VolcanoBear", "화산곰", volcanobear), ("WaveOtter", "파도수달", waveotter),
             ("BloomVineFox", "꽃덩굴여우", bloomvinefox), ("StormWeasel", "뇌전족제비", stormweasel),
             ("CrystalArmor", "수정갑주", crystalarmor)]
