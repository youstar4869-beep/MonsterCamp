"""Item icons (16x16): python items.py -> Assets/_Project/Art/Items/{key}.png + out/items_preview.png"""
import os
from px import Spr, sheet

N = 16
DST = os.path.join("..", "..", "Assets", "_Project", "Art", "Items")

CLAW = ((176, 176, 196), (230, 230, 240), (255, 255, 255))
PAD = ((84, 52, 40), (128, 84, 60), (168, 118, 84))
SHELL = ((120, 72, 40), (170, 112, 64), (214, 160, 100))
FEATHER = ((40, 84, 176), (70, 136, 230), (140, 196, 255))
BOLT = ((220, 170, 30), (255, 220, 60), (255, 250, 170))
BERRY = ((150, 30, 44), (214, 54, 64), (250, 110, 110))
LEAF = ((36, 104, 52), (66, 156, 72), (120, 206, 100))
STEEL = ((92, 96, 112), (140, 146, 160), (196, 202, 214))
SPIKE = ((150, 150, 164), (210, 210, 220), (250, 250, 255))
GOLD = ((190, 130, 30), (240, 190, 50), (255, 236, 140))
CLOVER = ((30, 110, 50), (60, 170, 70), (130, 220, 110))

W = (255, 255, 255)


def p(outline, **cols):
    d = dict(cols)
    d["outline"] = (None, outline)
    return d


def sharp_claw():
    s = Spr(N)
    # 발톱 3개 (가운데가 가장 길다), 아래 짙은 발바닥
    for ox, top in ((1, 4), (6, 1), (11, 4)):
        s.poly([(ox, 12), (ox + 0.6, top + 5), (ox + 1.6, top + 2), (ox + 3.6, top),
                (ox + 3, top + 4), (ox + 3, 12)], "claw")
    s.ellipse(8, 13, 7, 2.2, "pad")
    return s.render(p((40, 30, 34), claw=CLAW, pad=PAD))


def hard_shell():
    s = Spr(N)
    # 부채꼴 조개 + 아래 경첩
    s.ellipse(8, 9, 6.6, 6.2, "shell")
    s.rect(1, 10, 14, 15, None)
    s.poly([(1.6, 9), (14.4, 9), (11, 12.6), (5, 12.6)], "shell")
    s.rect(6, 12, 9, 13, "hinge")
    img = s.render(p((48, 28, 20), shell=SHELL, hinge=PAD))
    px = img.load()
    # 방사형 골
    for x, y in ((4, 5), (4, 6), (5, 8), (5, 9), (6, 10),
                 (8, 4), (8, 5), (8, 6), (8, 7), (8, 8), (8, 9), (8, 10),
                 (11, 5), (11, 6), (10, 8), (10, 9), (9, 10)):
        px[x, y] = SHELL[0] + (255,)
    return img


def thunder_feather():
    s = Spr(N)
    # 왼쪽 아래 → 오른쪽 위로 기운 깃털
    s.poly([(2, 14), (3.5, 9), (6.5, 4.5), (11, 1.5), (14.2, 1.5), (13, 5),
            (10, 9.5), (6, 12.5)], "feather")
    s.rect(1, 14, 1, 14, "feather")
    img = s.render(p((16, 26, 64), feather=FEATHER))
    px = img.load()
    # 깃대
    for x, y in ((2, 13), (3, 12), (4, 11), (5, 10), (6, 9), (7, 8), (8, 7), (9, 6), (10, 5), (11, 4), (12, 3)):
        px[x, y] = FEATHER[2] + (255,)
    # 노란 번개 줄무늬 (지그재그)
    for (x, y), c in (((5, 6), 2), ((6, 6), 1), ((7, 6), 1), ((6, 7), 1), ((7, 7), 0), ((8, 8), 1),
                      ((9, 8), 1), ((8, 9), 1), ((9, 9), 0), ((10, 9), 0),
                      ((9, 2), 2), ((10, 3), 1), ((11, 3), 1), ((12, 4), 1), ((12, 5), 0)):
        px[x, y] = BOLT[c] + (255,)
    return img


def healing_berry():
    s = Spr(N)
    s.ellipse(5.2, 10, 4.1, 4.1, "berry")
    s.ellipse(11, 10.6, 3.7, 3.7, "berry2")
    s.poly([(7, 5), (8, 2.5), (11, 1), (14, 1.5), (12.5, 4), (9.5, 5.5)], "leaf")
    s.rect(7, 5, 7, 6, "stem")
    img = s.render(p((56, 16, 24), berry=BERRY, berry2=BERRY, leaf=LEAF, stem=LEAF))
    px = img.load()
    # 두 열매 사이 경계선
    for y in range(N):
        for x in range(N - 1):
            if s.g[y][x] == "berry" and s.g[y][x + 1] == "berry2":
                px[x, y] = (56, 16, 24, 255)
    px[3, 8] = W + (255,)
    px[4, 8] = W + (255,)
    px[3, 9] = W + (255,)
    px[10, 9] = W + (255,)
    return img


def thorn_armor():
    s = Spr(N)
    # 가슴판: 어깨가 넓고 허리가 좁다
    s.poly([(1.5, 7), (5.5, 6), (8, 7.5), (10.5, 6), (14.5, 7), (13, 10.5), (11.5, 15), (4.5, 15), (3, 10.5)], "steel")
    # 어깨 가시 (위로), 옆구리 가시 (바깥으로), 가슴 가운데 가시
    for x0, tip in ((1.5, 2), (4, 2.5), (10, 2.5), (12.5, 2)):
        s.poly([(x0, 7), (x0 + 1.25, tip - 1), (x0 + 2.5, 6.5)], "spike")
    s.poly([(3.2, 12.5), (0.2, 10.5), (2.6, 9.5)], "spike")
    s.poly([(12.8, 12.5), (15.8, 10.5), (13.4, 9.5)], "spike")
    s.poly([(6.5, 11), (8, 7.8), (9.5, 11)], "spike")
    img = s.render(p((30, 30, 40), steel=STEEL, spike=SPIKE))
    px = img.load()
    # 허리 띠
    for x in range(5, 11):
        px[x, 13] = STEEL[0] + (255,)
    return img


def lucky_coin():
    s = Spr(N)
    s.ellipse(8, 8, 7, 7, "gold")
    img = s.render(p((72, 44, 12), gold=GOLD))
    px = img.load()
    # 안쪽 테두리
    for y in range(N):
        for x in range(N):
            d = ((x + .5 - 8) ** 2 + (y + .5 - 8) ** 2) ** .5
            if 4.9 <= d < 5.8:
                px[x, y] = GOLD[0] + (255,)
    # 네잎클로버 (잎 4장 3x3, 가운데 어두운 줄, 바깥 모서리 깎음)
    L, M, D = CLOVER[2], CLOVER[1], CLOVER[0]
    rows = [
        ".LM..LM.",
        "LMMDLMMD",
        "MMDDMMDD",
        "LMDDLMMD",
        "LMMDLMMD",
        ".MD..MD.",
        "....S...",
        ".....S..",
    ]
    for dy, row in enumerate(rows):
        for dx, ch in enumerate(row):
            if ch != ".":
                px[4 + dx, 3 + dy] = {"L": L, "M": M, "D": D, "S": D}[ch] + (255,)
    return img


ITEMS = [
    ("SharpClaw", sharp_claw),
    ("HardShell", hard_shell),
    ("ThunderFeather", thunder_feather),
    ("HealingBerry", healing_berry),
    ("ThornArmor", thorn_armor),
    ("LuckyCoin", lucky_coin),
]

if __name__ == "__main__":
    os.makedirs(DST, exist_ok=True)
    os.makedirs("out", exist_ok=True)
    imgs = []
    for key, fn in ITEMS:
        im = fn()
        im.save(os.path.join(DST, key + ".png"))
        imgs.append(im)
    sheet(imgs, scale=8).save("out/items_preview.png")
    print("ok", len(imgs))
