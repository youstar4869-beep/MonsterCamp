from px import Spr
from lib import pal, eyes, cheeks, mouth, spikes, K, W
from n_fire import flame
from n_water import bubble
from c_grass import leaf
from c_elec import bolt
from c_rock import crystal

# 조합 크리처 하급 5종: 재료 두 마리를 섞고, 같은 레벨 재료보다 장식(작은 왕관·불꽃)을 하나 더


# ---------- 불씨도깨비 EmberImp: 불똥이 + 성냥쥐 (불꽃 뿔 도깨비, 성냥 방망이) ----------
def emberimp(lv):
    s = Spr()
    if lv == 1:
        s.poly([(20, 27), (25, 25), (26, 22), (27, 23), (26, 26), (21, 28)], "brown")
        flame(s, 26.5, 18, 4, 1.8)
        s.ellipse(10, 16.5, 2.5, 2.5, "body", True)
        s.ellipse(10, 16.5, 1.2, 1.2, "pink", True)
        s.poly([(13, 13), (12.5, 17), (14.5, 16.5)], "core", True)  # horns
        flame(s, 16, 12, 4, 1.5)  # little flame crown
        s.ellipse(16, 22, 6, 5.5, "body")
        s.ellipse(16, 25, 3.5, 2.2, "belly")
        s.rect(12, 28, 13, 28, "body", True)
        eyes(s, 13, 20, "small"); cheeks(s, 11, 22); mouth(s, 23, "smile")
    elif lv == 2:
        s.ellipse(8.5, 11, 3, 3, "body", True)
        s.ellipse(8.5, 11, 1.6, 1.6, "pink", True)
        s.poly([(12, 5), (11, 11), (14, 10)], "core", True)
        flame(s, 16, 3, 6, 2.2)
        s.ellipse(16, 18, 8, 8.5, "body")
        s.ellipse(16, 21.5, 4.5, 4.5, "belly")
        s.poly([(8.5, 18), (5, 22), (7, 24), (9.5, 21)], "body")  # left arm
        s.rect(11, 26, 13, 29, "body", True)
        s.rect(26, 12, 27, 26, "brown")  # match club in right hand
        flame(s, 26.5, 4, 8, 2.5)
        s.ellipse(26.5, 12, 2, 1.5, "dark")
        s.ellipse(25, 21, 2, 2, "body")
        eyes(s, 12, 15); cheeks(s, 10, 18); mouth(s, 19, "smile")
    else:
        flame(s, 3.5, 3, 6, 2, True)
        s.ellipse(6.5, 10, 3.8, 3.8, "body", True)
        s.ellipse(6.5, 10, 2, 2, "pink", True)
        s.poly([(10.5, 1), (10, 9), (13.5, 8)], "core", True)
        flame(s, 16, 0, 8, 3)
        s.ellipse(16, 17, 10, 9.5, "body")
        s.ellipse(16, 21, 6, 5.5, "belly")
        s.ellipse(16, 21.5, 2.3, 2.3, "ember")
        s.poly([(6.5, 16), (2, 22), (4, 25), (8, 21)], "body")
        s.rect(9, 25, 13, 30, "body", True)
        s.rect(9, 30, 13, 30, "dark", True)
        s.rect(26, 10, 28, 26, "brown")
        flame(s, 27, 0, 10, 3.5)
        s.ellipse(27, 10, 2.5, 1.5, "dark")
        s.ellipse(25.5, 20, 2.5, 2.5, "body")
        eyes(s, 10, 12, "fierce"); mouth(s, 17, "fang")
    return s.render(pal("fire"))


# ---------- 물방울집게 BubbleClaw: 퐁당이 + 거품게 (물방울 몸, 집게, 거품 왕관) ----------
def bubbleclaw(lv):
    s = Spr()
    if lv == 1:
        bubble(s, 13, 11, 1); bubble(s, 16, 10, 1.2); bubble(s, 19, 11, 1)
        s.poly([(16, 12), (11, 20), (21, 20)], "body")
        s.ellipse(16, 22, 6, 6, "body")
        s.ellipse(16, 24.5, 3.5, 2.5, "belly")
        s.ellipse(8, 20, 2.4, 2.2, "shell", True)
        s.poly([(6, 18), (7, 20), (5, 20)], "body", True)
        for x in (12, 14):
            s.rect(x, 27, x, 28, "shell", True)
        eyes(s, 13, 20, "small"); mouth(s, 23); cheeks(s, 12, 22)
    elif lv == 2:
        bubble(s, 12, 4, 1.3); bubble(s, 16, 2.5, 1.6); bubble(s, 20, 4, 1.3)
        s.poly([(16, 4), (8, 16), (24, 16)], "body")
        s.ellipse(16, 18.5, 8.5, 8, "body")
        s.ellipse(16, 22, 5, 4, "belly")
        s.poly([(8, 18), (4, 15), (3, 19), (6, 21)], "shell", True)
        s.ellipse(4, 13, 3, 3, "shell", True)
        s.poly([(1, 9), (4, 13), (2, 14)], "shell", True)
        for x in (10, 12):
            s.poly([(x, 24), (x - 2, 29), (x - 1, 29), (x + 1, 25)], "shell", True)
        eyes(s, 12, 15); cheeks(s, 10, 19); mouth(s, 20, "smile")
    else:
        bubble(s, 10, 2, 1.6); bubble(s, 16, 1.5, 1.6); bubble(s, 22, 2, 1.6)
        s.poly([(16, 1), (6, 15), (26, 15)], "body")
        s.ellipse(16, 17.5, 10, 9.5, "body")
        s.ellipse(16, 21.5, 6, 5, "belly")
        s.ellipse(9, 7, 1.5, 1.5, "foam", True)
        s.poly([(7, 16), (3, 12), (1, 17), (5, 20)], "shell", True)
        s.ellipse(4, 10, 4, 4, "shell", True)
        s.poly([(0, 4), (3, 9), (1, 11)], "shell", True)
        s.poly([(3, 4), (5, 7), (7, 5)], "shell", True)
        for x in (9, 11, 13):
            s.poly([(x, 24), (x - 2, 30), (x - 1, 30), (x + 1, 25)], "shell", True)
        eyes(s, 11, 13, "fierce", (120, 220, 255)); mouth(s, 19, "fang")
    return s.render(pal("water"))


# ---------- 꽃껍질달팽이 BloomSnail: 새싹토리 + 이끼달팽이 (도토리 껍질 집, 꽃 왕관) ----------
def bloomsnail(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(16, 20, 7, 5.5, "wood")
        s.ellipse(16, 20, 4.5, 3.5, "moss")
        leaf(s, 16, 15, 13, 12.5, w=1.1); leaf(s, 16, 15, 19, 12.5, w=1.1)
        s.ellipse(16, 14, 1.2, 1.2, "flower")
        s.poly([(7, 26), (25, 26), (25, 28), (7, 28)], "belly")
        s.ellipse(16, 23.5, 4.5, 3.8, "orange")
        s.ellipse(16, 25.5, 2.5, 1.3, "belly")
        s.rect(12, 16, 13, 20, "belly", True)
        eyes(s, 13, 22, "small"); cheeks(s, 12, 24)
    elif lv == 2:
        s.ellipse(16, 16, 10.5, 8, "wood")
        s.ellipse(16, 16, 7.5, 5.5, "moss")
        s.ellipse(16, 16, 4, 3, "leaf")
        leaf(s, 15, 8, 11, 4, w=1.6); leaf(s, 17, 8, 21, 4, w=1.6)
        s.ellipse(16, 7, 1.8, 1.8, "flower")
        s.poly([(3, 24), (29, 24), (29, 29), (3, 29)], "belly")
        s.ellipse(16, 20.5, 6.5, 5, "orange")
        s.ellipse(16, 23, 3.5, 1.8, "belly")
        s.rect(9, 7, 10, 15, "belly", True)
        s.ellipse(9.5, 5.5, 1.5, 1.5, "flower", True)
        eyes(s, 12, 18); cheeks(s, 11, 21); mouth(s, 22, "smile")
    else:
        s.ellipse(16, 14, 14, 10, "wood")
        s.ellipse(16, 14, 11, 7.5, "moss")
        s.ellipse(16, 14, 7, 4.5, "leaf")
        s.ellipse(16, 14, 3, 2, "moss")
        for x, y in ((3.5, 14), (5.5, 20), (6, 8)):
            s.ellipse(x, y, 1.6, 1.6, "flower", True)
        spikes(s, 16, 4, 1.5, 3.2, 5, "flower")
        s.ellipse(16, 4, 1.2, 1.2, "orange")
        s.poly([(1, 24), (30, 24), (30, 30), (1, 30)], "belly")
        s.rect(1, 30, 30, 30, "moss")
        s.ellipse(16, 19, 8, 6, "orange")
        s.ellipse(16, 22.5, 4.5, 2.2, "belly")
        s.rect(8, 3, 9, 14, "belly", True)
        s.ellipse(8.5, 2, 1.8, 1.8, "flower", True)
        eyes(s, 11, 16, "fierce"); mouth(s, 21, "fang")
    return s.render(pal("grass"))


# ---------- 번개날다람쥐 VoltGlider: 찌릿새 + 찌릿다람쥐 (파란 날다람쥐, 비막 날개, 번개 볏) ----------
def voltglider(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(23.5, 19, 4.5, 6.5, "wool")
        s.poly([(22.5, 16), (25, 19), (23.5, 19), (25, 23), (21.5, 19), (23, 19)], "bolt")
        bolt(s, 14.5, 9, 0.8)
        s.ellipse(11.5, 15, 1.6, 1.6, "blue", True)
        s.poly([(10, 19), (7, 25), (11, 25)], "wing", True)
        s.ellipse(16, 21, 5.5, 5.5, "blue")
        s.ellipse(16, 23.5, 3, 2.5, "belly")
        s.rect(13, 27, 14, 28, "blue", True)
        eyes(s, 13, 19, "small"); cheeks(s, 12, 21)
        s.fix(15, 22, (255, 140, 40)); s.fix(16, 22, (255, 140, 40))
    elif lv == 2:
        s.ellipse(26, 12, 5.5, 9, "wool")
        s.poly([(26, 6), (29, 12), (26, 12), (29, 19), (23, 11), (26, 11)], "bolt")
        bolt(s, 13.5, 1, 1.2)
        bolt(s, 9, 4, 0.7, mirror=True)
        s.ellipse(10, 8, 2, 2, "blue", True)
        s.poly([(10, 13), (2, 18), (3, 26), (11, 24)], "wing", True)  # glide membrane
        s.poly([(4, 19), (4, 24), (7, 22)], "dark", True)
        s.ellipse(16, 13, 6.5, 5.5, "blue")
        s.ellipse(16, 22, 6, 6, "blue")
        s.ellipse(16, 23, 3.5, 4, "belly")
        s.rect(11, 27, 13, 29, "blue", True)
        eyes(s, 12, 11); cheeks(s, 11, 14)
        for (x, y, c) in ((15, 14, (255, 140, 40)), (16, 14, (255, 140, 40)), (15, 15, (230, 110, 30)), (16, 15, (230, 110, 30))):
            s.fix(x, y, c)
    else:
        s.ellipse(27, 10, 5, 10, "wool")
        s.poly([(27, 2), (31, 10), (27, 10), (31, 19), (23, 9), (27, 9)], "bolt")
        bolt(s, 12.5, 0, 1.7)
        bolt(s, 7, 1, 1, mirror=True)
        s.ellipse(9, 7, 2.5, 2.5, "blue", True)
        s.ellipse(9, 7, 1.2, 1.2, "bolt", True)
        s.poly([(10, 11), (0, 14), (0, 22), (2, 28), (11, 24)], "wing", True)
        s.poly([(2, 16), (2, 25), (6, 21)], "dark", True)
        bolt(s, 2, 16, 0.8, mirror=True)
        s.ellipse(16, 11.5, 7.5, 6, "blue")
        s.ellipse(16, 21.5, 7.5, 7, "blue")
        s.ellipse(16, 22.5, 4.5, 5, "belly")
        s.rect(10, 26, 13, 30, "blue", True)
        s.rect(10, 30, 13, 30, "dark", True)
        eyes(s, 11, 9, "fierce", (255, 250, 150))
        for (x, y, c) in ((15, 13, (255, 140, 40)), (16, 13, (255, 140, 40)), (15, 14, (230, 110, 30)), (16, 14, (230, 110, 30))):
            s.fix(x, y, c)
        s.fix(14, 15, W); s.fix(17, 15, W)
    return s.render(pal("elec"))


# ---------- 바위두더지왕 BoulderMoleKing: 돌멩군 + 모래두더지 (바위 투구 두더지, 돌 왕관) ----------
def bouldermoleking(lv):
    s = Spr()
    if lv == 1:
        s.ellipse(16, 27.5, 9, 2.5, "belly")
        s.ellipse(16, 21.5, 6, 5.5, "body")
        s.ellipse(16, 17, 5.5, 2.3, "stone")
        for x in (13, 16, 19):
            s.poly([(x - 1, 15), (x, 12), (x + 1, 15)], "glow")
        s.ellipse(16, 23.5, 2.5, 1.8, "pink")
        s.ellipse(10, 24, 1.8, 1.4, "pink", True)
        eyes(s, 13, 19, "closed"); s.fix(15, 22, K); s.fix(16, 22, K)
    elif lv == 2:
        s.ellipse(16, 27, 13, 3.5, "belly")
        for x in (5, 26):
            s.ellipse(x, 25, 1.5, 1.2, "stone")
        s.ellipse(16, 18, 8.5, 8, "body")
        s.ellipse(16, 13, 8.5, 5, "stone")
        s.poly([(10, 9), (10, 4), (12.5, 7), (16, 3), (19.5, 7), (22, 4), (22, 9)], "glow")
        s.ellipse(16, 7, 1, 1, "crystal")
        s.ellipse(16, 22, 4, 3, "belly")
        s.ellipse(16, 20, 2, 1.4, "pink")
        s.poly([(8, 20), (3, 18), (2, 22), (4, 24), (8, 23)], "pink", True)
        for x in (2, 4):
            s.fix(x, 24, W, True)
        eyes(s, 12, 16, "closed"); mouth(s, 22, "w")
    else:
        s.ellipse(16, 28, 15, 3.5, "belly")
        for x, y in ((3, 26), (28, 26), (8, 29), (24, 29)):
            s.ellipse(x, y, 1.6, 1.3, "stone")
        s.ellipse(16, 17, 10, 9.5, "body")
        s.ellipse(16, 11, 10.5, 6, "stone")
        s.ellipse(11, 9, 2, 1.2, "dark", True)
        s.poly([(8, 7), (7, 0), (11, 4), (16, 0), (21, 4), (25, 0), (24, 7)], "glow")
        crystal(s, 16, 1, 5)
        s.ellipse(16, 21.5, 5, 4, "belly")
        s.ellipse(16, 18.5, 2.5, 1.6, "pink")
        s.poly([(7, 18), (1, 15), (0, 20), (2, 24), (7, 23)], "pink", True)
        for x in (0, 2, 4):
            s.fix(x, 24, W, True)
        eyes(s, 11, 14, "fierce", (255, 180, 60)); mouth(s, 21, "fang")
    return s.render(pal("rock"))


CREATURES = [("EmberImp", "불씨도깨비", emberimp), ("BubbleClaw", "물방울집게", bubbleclaw),
             ("BloomSnail", "꽃껍질달팽이", bloomsnail), ("VoltGlider", "번개날다람쥐", voltglider),
             ("BoulderMoleKing", "바위두더지왕", bouldermoleking)]
