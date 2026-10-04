"""배경 16장 + 구름 1장 (180x320). python bg.py
→ Assets/_Project/Art/Backgrounds/ 에 바로 저장, 미리보기 out/backgrounds_preview.png
외곽선 없이 3단 음영(위·왼쪽 밝게), 채도·대비 낮춤. 크리처 자리(y 120~280)는 단순하게."""
import math
import os
import random
import uuid
from PIL import Image, ImageDraw

W, H = 180, 320
HZ = 107  # 전투 배경 하늘/땅 경계 (위 1/3)
HERE = os.path.dirname(os.path.abspath(__file__))
DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "Backgrounds")


def tri(m):
    """중간색 하나로 (어둡게, 중간, 밝게) 3단."""
    d = tuple(int(c * .72) for c in m)
    l = tuple(min(255, int(c + (255 - c) * .28)) for c in m)
    return d, m, l


class Bg:
    def __init__(self, w=W, h=H, wrap=False):
        self.w, self.h, self.wrap = w, h, wrap
        self.g = [[None] * w for _ in range(h)]
        self.fx = {}

    def put(self, x, y, c):
        x, y = int(x), int(y)
        if self.wrap:
            x %= self.w
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[y][x] = c

    def fix(self, x, y, c):
        x, y = int(x), int(y)
        if self.wrap:
            x %= self.w
        self.fx[(x, y)] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.put(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1:
                    self.put(x, y, c)

    def poly(self, pts, c):
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
            for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
                if _inside(x + .5, y + .5, pts):
                    self.put(x, y, c)

    def ridge(self, y0, f, c, y1=None):
        """x마다 높이 f(x)부터 y1(기본 y0)까지 채움: 산·언덕."""
        for x in range(self.w):
            for y in range(int(f(x)), (y1 or y0) + 1):
                self.put(x, y, c)

    def get(self, x, y):
        if self.wrap:
            x %= self.w
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.g[y][x]
        return "edge"

    def render(self, pal, flat=()):
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        px = img.load()
        for y in range(self.h):
            for x in range(self.w):
                c = self.g[y][x]
                if c is None:
                    continue
                d, m, l = pal[c]
                col = m
                if c not in flat:
                    nb = lambda dx, dy: self.get(x + dx, y + dy) in (c, "edge") or self.get(x + dx, y + dy) in PATCH
                    if not nb(0, 1):
                        col = d
                    elif not nb(0, -1):
                        col = l
                    elif not nb(1, 0):
                        col = d
                    elif not nb(-1, 0):
                        col = l
                px[x, y] = col + (255,)
        for (x, y), c in self.fx.items():
            if 0 <= x < self.w and 0 <= y < self.h:
                px[x, y] = (c if len(c) == 4 else c + (255,))
        return img


def _inside(x, y, pts):
    inside = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            inside = not inside
        j = i
    return inside


def soften(img, sat=.7, con=.82, mid=118):
    """채도·대비 낮추기."""
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = .299 * r + .587 * g + .114 * b
            out = []
            for c in (r, g, b):
                c = lum + (c - lum) * sat
                c = mid + (c - mid) * con
                out.append(max(0, min(255, int(c))))
            px[x, y] = tuple(out) + (a,)
    return img


# ---------- 하늘 ----------
SKY = {
    "day": [(150, 196, 232), (170, 210, 236), (192, 224, 238), (212, 234, 240)],
    "dusk": [(150, 120, 120), (180, 140, 120), (206, 160, 120), (222, 186, 140)],
    "pale": [(170, 200, 220), (186, 212, 226), (202, 222, 230), (218, 232, 236)],
    "storm": [(70, 70, 92), (86, 86, 108), (102, 102, 122), (120, 118, 136)],
    "high": [(110, 160, 230), (140, 186, 238), (176, 210, 244), (210, 230, 248)],
    "night": [(20, 24, 52), (28, 34, 66), (38, 46, 80), (50, 58, 92)],
    "boss": [(70, 18, 26), (104, 30, 34), (140, 48, 40), (176, 76, 52)],
}


def sky(s, kind, y1, pal):
    """y 0~y1 하늘을 4단 띠로, 경계는 체크무늬 디더."""
    cols = SKY[kind]
    for i, c in enumerate(cols):
        pal["sky%d" % i] = (c, c, c)
    for y in range(y1 + 1):
        t = y / max(1, y1) * 3.999
        i = int(t)
        f = t - i
        for x in range(s.w):
            k = i
            if i < 3 and f > .75 and (x + y) % 2 == 0:
                k = i + 1
            s.put(x, y, "sky%d" % k)


# 바닥 얼룩은 음영 없이 납작하게 (구멍처럼 보이지 않게)
PATCH = {"grass2", "moss", "light", "ground2", "snow2", "rock2", "floor2", "glow", "glow2"}

SKY_KEYS = {"sky0", "sky1", "sky2", "sky3", "star", "moon", "sun", "bolt"}


def boss_pal(pal):
    """하늘만 붉게, 나머지는 조금 어둡게."""
    out = {}
    for k, v in pal.items():
        if k.startswith("sky"):
            out[k] = v
        elif k in SKY_KEYS:
            out[k] = v
        else:
            out[k] = tuple(tuple(int(c * .78) for c in t) for t in v)
    return out


def tufts(s, rng, n, y0, y1, key, avoid=(40, 140), avoid_y=(120, 280)):
    """풀포기 같은 작은 점 무늬. 가운데 크리처 자리는 피함."""
    for _ in range(n):
        x, y = rng.randrange(s.w), rng.randrange(y0, y1)
        if avoid_y[0] <= y <= avoid_y[1] and avoid[0] <= x <= avoid[1]:
            continue
        s.put(x, y, key)
        s.put(x + 1, y - 1, key)
        s.put(x + 2, y, key)


# ---------- 전투 배경 ----------

def meadow(boss):
    s, rng = Bg(), random.Random(1)
    pal = {"hill2": tri((120, 160, 120)), "hill": tri((110, 160, 92)), "grass": tri((122, 168, 92)),
           "grass2": tri((108, 152, 84)), "tuft": tri((92, 138, 72)), "flower": tri((230, 220, 150)),
           "flower2": tri((220, 160, 170)), "path": tri((178, 160, 118)), "tree": tri((86, 130, 76)),
           "trunk": tri((120, 92, 66))}
    sky(s, "boss" if boss else "day", HZ, pal)
    s.ridge(HZ, lambda x: 86 + 8 * math.sin(x / 22 + 1) + 4 * math.sin(x / 9), "hill2")
    s.ridge(HZ, lambda x: 96 + 6 * math.sin(x / 15 + 3), "hill")
    for cx in (22, 150):
        s.rect(cx, 84, cx + 1, 96, "trunk")
        s.ellipse(cx + 1, 82, 8, 7, "tree")
    s.rect(0, HZ, W - 1, H - 1, "grass")
    for y in (125, 160, 205, 260):  # 아주 옅은 잔디 띠
        s.ellipse(rng.choice((30, 90, 150)), y, 40, 5, "grass2")
    s.poly([(80, HZ), (100, HZ), (130, H), (50, H)], "path")
    tufts(s, rng, 120, HZ + 2, H - 2, "tuft")
    for _ in range(26):
        x, y = rng.choice((rng.randrange(4, 36), rng.randrange(144, 176))), rng.randrange(HZ + 4, H - 4)
        s.fix(x, y, pal["flower" if rng.random() < .5 else "flower2"][2])
    return s, pal


def forest(boss):
    s, rng = Bg(), random.Random(2)
    pal = {"far": tri((100, 128, 110)), "mid": tri((72, 108, 78)), "ground": tri((88, 116, 72)),
           "moss": tri((78, 104, 66)), "trunk": tri((96, 74, 58)), "leaf": tri((60, 96, 64)),
           "tuft": tri((66, 92, 58)), "mush": tri((200, 120, 110)), "stem": tri((222, 210, 186)),
           "light": tri((140, 156, 110))}
    sky(s, "boss" if boss else "pale", HZ, pal)
    for i, x in enumerate(range(-6, W + 6, 13)):  # 먼 침엽수 줄
        h = 34 + (i * 7) % 12
        s.poly([(x, HZ), (x + 7, HZ - h), (x + 14, HZ)], "far")
    for i, x in enumerate(range(0, W + 10, 19)):
        h = 22 + (i * 5) % 10
        s.poly([(x - 9, HZ + 2), (x, HZ - h), (x + 9, HZ + 2)], "mid")
    s.rect(0, HZ, W - 1, H - 1, "ground")
    s.ellipse(90, 210, 64, 26, "light")  # 가운데 옅은 빛(단순)
    for y in (140, 240, 300):
        s.ellipse(rng.choice((20, 160)), y, 26, 6, "moss")
    # 양옆 큰 나무 기둥(액자)
    for x0, sgn in ((0, 1), (W - 1, -1)):
        s.rect(min(x0, x0 + sgn * 11), 0, max(x0, x0 + sgn * 11), H - 1, "trunk")
        for y in range(0, 100, 26):
            s.ellipse(x0 + sgn * 12, y + 10, 18, 14, "leaf")
        s.ellipse(x0 + sgn * 14, H - 6, 10, 6, "trunk")
    tufts(s, rng, 90, HZ + 2, H - 2, "tuft")
    for x, y in ((18, 292), (160, 270), (24, 150)):
        s.rect(x, y, x + 1, y + 3, "stem")
        s.ellipse(x + 1, y, 4, 2.5, "mush")
    return s, pal


def beach(boss):
    s, rng = Bg(), random.Random(3)
    pal = {"sea": tri((96, 150, 180)), "sea2": tri((84, 136, 170)), "sand": tri((222, 204, 160)),
           "wet": tri((196, 182, 150)), "rock": tri((132, 126, 118)), "trunk": tri((150, 118, 80)),
           "palm": tri((92, 146, 92)), "shell": tri((236, 196, 190)), "isle": tri((120, 150, 130))}
    sky(s, "boss" if boss else "day", HZ, pal)
    s.ellipse(150, HZ, 22, 8, "isle")
    s.rect(0, HZ, W - 1, 150, "sea")
    s.rect(0, HZ, W - 1, 112, "sea2")
    for y in range(116, 148, 7):  # 잔물결
        for x in range((y * 3) % 17, W, 17):
            s.fix(x, y, pal["sea"][2]); s.fix(x + 1, y, pal["sea"][2])
    s.ridge(150, lambda x: 150 + 3 * math.sin(x / 13), "wet", 160)
    s.ridge(H - 1, lambda x: 156 + 3 * math.sin(x / 13 + 1), "sand")
    for x in range(0, W, 2):  # 거품 줄
        s.fix(x, int(150 + 3 * math.sin(x / 13)), (240, 244, 240))
    # 왼쪽 야자수
    for i in range(40):
        s.rect(10 + i * .18, 150 - i * 1.6, 13 + i * .18, 150 - i * 1.6 + 2, "trunk")
    for a in (-2.6, -1.9, -1.2, -.4, .3):
        tx, ty = 19, 86
        s.poly([(tx, ty), (tx + 30 * math.cos(a), ty + 30 * math.sin(a) + 10),
                (tx + 26 * math.cos(a + .2), ty + 26 * math.sin(a + .2) + 16)], "palm")
    for x, y, r in ((160, 296, 12), (172, 280, 7), (8, 306, 9)):
        s.ellipse(x, y, r, r * .7, "rock")
    for x, y in ((30, 200), (150, 230), (40, 300), (124, 306)):
        s.ellipse(x, y, 2, 1.5, "shell")
    return s, pal


def volcano(boss):
    s, rng = Bg(), random.Random(4)
    pal = {"cone": tri((104, 86, 84)), "cone2": tri((88, 74, 74)), "lava": tri((226, 120, 70)),
           "ground": tri((96, 84, 80)), "ground2": tri((86, 76, 74)), "rock": tri((70, 62, 62)),
           "smoke": tri((140, 128, 128)), "crack": tri((210, 110, 66))}
    sky(s, "boss" if boss else "dusk", HZ, pal)
    for cx, cy, r in ((96, 30, 10), (104, 20, 13), (94, 10, 9)):
        s.ellipse(cx, cy, r, r * .7, "smoke")
    s.ridge(HZ, lambda x: max(44 + abs(x - 92) * 1.15, 80 + 6 * math.sin(x / 11)), "cone")
    s.ridge(HZ, lambda x: 94 + 6 * math.sin(x / 9 + 2), "cone2")
    s.poly([(86, 46), (98, 46), (102, 70), (95, 62), (90, 76), (88, 58)], "lava")
    s.rect(0, HZ, W - 1, H - 1, "ground")
    for y in (150, 230, 290):
        s.ellipse(rng.choice((25, 155)), y, 30, 7, "ground2")
    for x, y, r in ((10, 140, 10), (170, 180, 12), (14, 296, 14), (166, 304, 10)):
        s.ellipse(x, y, r, r * .8, "rock")
    for x0, y0 in ((20, 250), (150, 130), (140, 290)):  # 용암 틈(가장자리만)
        x, y = x0, y0
        for _ in range(9):
            s.put(x, y, "crack")
            x += rng.choice((-1, 1)); y += 1
    for _ in range(14):  # 불티
        s.fix(rng.randrange(W), rng.randrange(10, HZ), (240, 170, 110))
    return s, pal


def snow(boss):
    s, rng = Bg(), random.Random(5)
    pal = {"mtn": tri((134, 148, 168)), "cap": tri((228, 234, 240)), "mtn2": tri((150, 166, 184)),
           "snow": tri((226, 232, 238)), "snow2": tri((210, 220, 232)), "ice": tri((170, 206, 224)),
           "pine": tri((80, 112, 104)), "pinecap": tri((232, 238, 242))}
    sky(s, "boss" if boss else "pale", HZ, pal)
    for cx, top, half in ((40, 50, 48), (124, 40, 56)):
        s.poly([(cx - half, HZ), (cx, top), (cx + half, HZ)], "mtn")
        s.poly([(cx - 10, top + 12), (cx, top), (cx + 10, top + 12), (cx + 4, top + 9), (cx - 3, top + 13)], "cap")
    s.ridge(HZ, lambda x: 96 + 5 * math.sin(x / 14), "mtn2")
    s.rect(0, HZ, W - 1, H - 1, "snow")
    for y in (140, 200, 270):
        s.ellipse(rng.choice((30, 90, 150)), y, 46, 5, "snow2")
    for x, y, h in ((4, 200, 30), (170, 150, 24), (162, 300, 34), (12, 312, 26)):  # 얼음 덩이
        s.poly([(x - 8, y), (x - 3, y - h), (x + 4, y - h + 6), (x + 9, y)], "ice")
    for x, y in ((24, 120), (156, 118), (10, 160)):  # 눈 덮인 작은 전나무
        s.poly([(x - 7, y), (x, y - 18), (x + 7, y)], "pine")
        s.poly([(x - 3, y - 10), (x, y - 18), (x + 3, y - 10)], "pinecap")
    for _ in range(40):  # 눈송이
        s.fix(rng.randrange(W), rng.randrange(0, HZ), (244, 246, 250))
    return s, pal


def storm(boss):
    s, rng = Bg(), random.Random(6)
    pal = {"peak": tri((92, 92, 108)), "peak2": tri((110, 108, 122)), "rock": tri((118, 114, 120)),
           "rock2": tri((106, 102, 110)), "boulder": tri((90, 86, 94)), "bolt": tri((240, 236, 180))}
    sky(s, "boss" if boss else "storm", HZ, pal)
    for cx, top, half in ((20, 60, 34), (70, 74, 30), (128, 54, 40), (176, 70, 28)):
        s.poly([(cx - half, HZ), (cx - 4, top + 3), (cx, top), (cx + half, HZ)], "peak")
    s.ridge(HZ, lambda x: 94 + 7 * math.sin(x / 8) + 3 * math.sin(x / 3), "peak2")
    x, y = 100, 0  # 번개
    while y < 40:
        s.fix(x, y, (246, 244, 200)); s.fix(x + 1, y, (246, 244, 200))
        y += 1
        if y % 6 == 0:
            x += rng.choice((-3, 2, 3))
    s.rect(0, HZ, W - 1, H - 1, "rock")
    for y in (150, 220, 290):
        s.ellipse(rng.choice((20, 160)), y, 36, 6, "rock2")
    for x, y, r in ((8, 130, 12), (172, 160, 10), (6, 300, 16), (170, 296, 14)):
        s.poly([(x - r, y + r // 2), (x - r // 2, y - r // 2), (x + r // 2, y - r // 2 - 3), (x + r, y + r // 2)], "boulder")
    for _ in range(6):  # 바람 줄
        x, y = rng.randrange(0, W - 30), rng.randrange(8, HZ - 10)
        for i in range(rng.randrange(12, 24)):
            s.fix(x + i, y, (150, 150, 168))
    return s, pal


def skytemple(boss):
    s, rng = Bg(), random.Random(7)
    pal = {"floor": tri((228, 232, 240)), "floor2": tri((214, 220, 234)), "col": tri((222, 214, 196)),
           "roof": tri((196, 176, 140)), "far": tri((206, 210, 226)), "stone": tri((176, 168, 156)),
           "gold": tri((222, 196, 120)), "tile": tri((206, 200, 186))}
    sky(s, "boss" if boss else "high", HZ, pal)
    # 먼 구름 위 신전
    s.ellipse(90, HZ - 2, 50, 9, "far")
    s.poly([(68, 80), (90, 68), (112, 80)], "roof")
    s.rect(70, 80, 110, 82, "roof")
    for x in range(72, 110, 8):
        s.rect(x, 83, x + 3, 98, "col")
    s.rect(68, 98, 112, 100, "col")
    s.fix(90, 74, pal["gold"][2])
    for x, y, r in ((30, 60, 6), (154, 46, 5)):  # 떠 있는 돌
        s.poly([(x - r, y), (x + r, y), (x, y + r * 1.6)], "stone")
    s.rect(0, HZ, W - 1, H - 1, "floor")
    for y in (130, 190, 250, 300):
        s.ellipse(rng.choice((30, 90, 150)), y, 50, 6, "floor2")
    s.poly([(78, HZ), (102, HZ), (124, H), (56, H)], "tile")  # 돌길
    # 양옆 큰 기둥
    for x0 in (2, W - 16):
        s.rect(x0, 40, x0 + 13, H - 1, "col")
        s.rect(x0 - 2, 34, x0 + 15, 40, "roof")
        s.rect(x0 - 2, 296, x0 + 15, H - 1, "stone")
    return s, pal


BATTLE = [("Meadow", meadow), ("Forest", forest), ("Beach", beach), ("Volcano", volcano),
          ("Snow", snow), ("Storm", storm), ("Sky", skytemple)]


# ---------- 시작·상점 ----------

def title():
    s, rng = Bg(), random.Random(8)
    pal = {"hill": tri((40, 52, 62)), "hill2": tri((34, 46, 52)), "ground": tri((48, 58, 56)),
           "glow": tri((96, 82, 64)), "glow2": tri((72, 66, 58)), "tent": tri((120, 96, 84)),
           "tent2": tri((90, 104, 120)), "door": tri((40, 34, 36)), "log": tri((100, 70, 52)),
           "fire": tri((240, 150, 70)), "core": tri((255, 220, 130)), "sil": tri((26, 28, 36)),
           "moon": tri((236, 232, 200)), "stone": tri((80, 80, 84)), "tree": tri((30, 40, 46))}
    sky(s, "night", 190, pal)
    s.ellipse(140, 46, 11, 11, "moon")
    s.ellipse(145, 42, 9, 9, "sky0")  # 초승달
    for _ in range(70):
        x, y = rng.randrange(W), rng.randrange(0, 170)
        s.fix(x, y, (230, 230, 210) if rng.random() < .3 else (150, 156, 180))
    s.ridge(200, lambda x: 168 + 10 * math.sin(x / 20) + 4 * math.sin(x / 7), "hill")
    for x in (8, 22, 160, 172):
        s.poly([(x - 8, 186), (x, 150), (x + 8, 186)], "tree")
    s.ridge(200, lambda x: 186 + 5 * math.sin(x / 12 + 1), "hill2")
    s.rect(0, 198, W - 1, H - 1, "ground")
    s.ellipse(90, 262, 70, 38, "glow2")
    s.ellipse(90, 264, 44, 24, "glow")
    # 텐트 둘
    s.poly([(4, 240), (34, 196), (64, 240)], "tent")
    s.poly([(28, 240), (34, 214), (40, 240)], "door")
    s.poly([(126, 232), (150, 198), (174, 232)], "tent2")
    s.poly([(145, 232), (150, 214), (155, 232)], "door")
    # 모닥불
    for a in range(8):
        s.ellipse(90 + 11 * math.cos(a * math.pi / 4), 272 + 5 * math.sin(a * math.pi / 4), 2.5, 2, "stone")
    s.poly([(78, 274), (100, 266), (102, 270), (80, 278)], "log")
    s.poly([(80, 266), (102, 274), (100, 278), (78, 270)], "log")
    s.poly([(82, 270), (86, 256), (90, 246), (93, 254), (97, 250), (98, 270)], "fire")
    s.poly([(87, 270), (90, 258), (94, 270)], "core")
    for x, y in ((92, 240), (86, 236), (96, 230)):
        s.fix(x, y, (250, 190, 110))
    # 크리처 실루엣 3마리 (불가 둘레에 앉음)
    s.ellipse(52, 268, 10, 9, "sil"); s.ellipse(52, 256, 7, 6, "sil")  # 왼쪽: 귀 긴 동물
    s.poly([(47, 252), (45, 240), (50, 250)], "sil"); s.poly([(55, 250), (58, 240), (58, 252)], "sil")
    s.ellipse(130, 270, 12, 8, "sil"); s.ellipse(124, 262, 6, 5, "sil")  # 오른쪽: 엎드린 동물
    s.poly([(138, 268), (150, 262), (142, 272)], "sil")
    s.ellipse(110, 296, 8, 6, "sil"); s.poly([(104, 292), (110, 284), (116, 292)], "sil")  # 앞쪽 작은 녀석
    for x, y in ((50, 255), (54, 255), (122, 261)):
        s.fix(x, y, (230, 200, 120))
    return s, pal, {"sky0", "sky1", "sky2", "sky3"} | PATCH


def shop():
    s, rng = Bg(), random.Random(9)
    pal = {"ground": tri((196, 180, 140)), "ground2": tri((184, 168, 130)), "grass": tri((130, 166, 104)),
           "awnA": tri((206, 120, 104)), "awnB": tri((232, 222, 200)), "awnC": tri((110, 150, 190)),
           "awnD": tri((216, 190, 110)), "post": tri((128, 96, 70)), "counter": tri((150, 112, 80)),
           "crate": tri((170, 128, 86)), "crate2": tri((140, 104, 70)), "flagA": tri((206, 110, 100)),
           "flagB": tri((110, 160, 190)), "flagC": tri((222, 196, 110)), "rope": tri((120, 100, 80)),
           "hill": tri((150, 180, 140)), "goods": tri((170, 190, 110)), "jar": tri((140, 160, 190))}
    sky(s, "day", 96, pal)
    s.ridge(100, lambda x: 84 + 6 * math.sin(x / 18), "hill")
    s.rect(0, 96, W - 1, H - 1, "grass")
    s.rect(0, 112, W - 1, H - 1, "ground")
    for y in (150, 210, 270):
        s.ellipse(rng.choice((40, 90, 140)), y, 50, 6, "ground2")
    # 깃발 줄 두 개
    for (x0, y0, x1, y1) in ((0, 22, 90, 32), (90, 32, W, 20)):
        for i in range(0, 91, 1):
            t = i / 90
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t + 6 * math.sin(math.pi * t)
            s.fix(x, y, pal["rope"][1])
            if i % 10 == 5:
                s.poly([(x - 3, y), (x + 3, y), (x, y + 7)], ("flagA", "flagB", "flagC")[(i // 10) % 3])
    # 노점 셋 (뒤 줄)
    for (x0, x1, a, b, top) in ((2, 58, "awnA", "awnB", 66), (62, 118, "awnC", "awnB", 60), (122, 178, "awnD", "awnB", 66)):
        s.rect(x0 + 2, top + 12, x0 + 3, 116, "post")
        s.rect(x1 - 3, top + 12, x1 - 2, 116, "post")
        s.rect(x0 + 2, 104, x1 - 2, 116, "counter")
        for i, x in enumerate(range(x0, x1, 8)):
            s.rect(x, top, min(x + 7, x1), top + 10, a if i % 2 == 0 else b)
            s.poly([(x, top + 11), (min(x + 7, x1), top + 11), (x + 3.5, top + 15)], a if i % 2 == 0 else b)
        s.poly([(x0, top), (x1, top), ((x0 + x1) / 2, top - 12)], a)
        for gx in range(x0 + 8, x1 - 8, 9):
            s.ellipse(gx, 102, 3, 2.5, "goods" if gx % 2 else "jar")
    # 나무 상자 (양옆 아래)
    for x, y, w in ((4, 262, 26), (22, 284, 30), (148, 270, 28), (134, 292, 24), (2, 296, 18)):
        s.rect(x, y, x + w, y + w * .7, "crate")
        s.rect(x, y + w * .35, x + w, y + w * .35, "crate2")
        s.rect(x + w // 2, y, x + w // 2, y + w * .7, "crate2")
    # 깃대
    for x in (6, 172):
        s.rect(x, 130, x + 1, 260, "post")
        s.poly([(x + 2, 130), (x + 2 + (14 if x < 90 else -14), 136), (x + 2, 142)], "flagA" if x < 90 else "flagB")
    return s, pal, {"sky0", "sky1", "sky2", "sky3"} | PATCH


# ---------- 구름 (좌우 이어짐) ----------

def clouds():
    s, rng = Bg(W, 80, wrap=True), random.Random(10)
    pal = {"c": ((206, 214, 226), (234, 238, 244), (252, 252, 252))}
    for cx, cy, w in ((20, 22, 30), (100, 14, 22), (150, 50, 34), (64, 60, 18)):
        s.ellipse(cx, cy + 4, w / 2, 5, "c")
        for i in range(3):
            s.ellipse(cx - w / 4 + i * w / 4, cy - (2 if i == 1 else 0), w / 5 + (2 if i == 1 else 0), 5 + (2 if i == 1 else 0), "c")
    return s.render(pal)


# ---------- 저장 ----------

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: {wrap}
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def save(name, img, wrap=1):
    path = os.path.join(DST, name + ".png")
    img.save(path)
    if not os.path.exists(path + ".meta"):  # 이미 있으면 GUID 유지
        with open(path + ".meta", "w", newline="\n") as f:
            f.write(META.format(guid=uuid.uuid4().hex, wrap=wrap))


def main():
    os.makedirs(DST, exist_ok=True)
    os.makedirs(os.path.join(HERE, "out"), exist_ok=True)
    flat = {"sky0", "sky1", "sky2", "sky3"} | PATCH
    imgs = []
    for name, fn in (("Title", title), ("Shop", shop)):
        s, pal, fl = fn()
        imgs.append((name, soften(s.render(pal, fl))))
    for name, fn in BATTLE:
        s, pal = fn(False)
        imgs.append(("Battle_" + name, soften(s.render(pal, flat))))
    for name, fn in BATTLE:
        s, pal = fn(True)
        imgs.append(("Battle_%s_Boss" % name, soften(s.render(boss_pal(pal), flat))))
    cl = soften(clouds(), sat=.8, con=.9)
    for name, img in imgs:
        save(name, img)
    save("Battle_Clouds", cl, wrap=0)

    # 미리보기 2배: 9장 × 2줄 (마지막 칸에 구름 2번 이어 붙여 이음새 확인)
    sc, pad, lab = 2, 6, 12
    cols = 9
    cw, ch = W * sc + pad, H * sc + pad + lab
    prev = Image.new("RGBA", (cols * cw + pad, 2 * ch + pad), (46, 52, 64, 255))
    d = ImageDraw.Draw(prev)
    cells = imgs + [("Battle_Clouds (x2)", None)]
    for i, (name, img) in enumerate(cells):
        x, y = pad + (i % cols) * cw, pad + (i // cols) * ch
        d.text((x, y), name, fill=(230, 230, 230, 255))
        if img is None:
            sky_img = imgs[4][1].crop((0, 0, W, 80))
            tile = Image.new("RGBA", (W, 160))
            tile.alpha_composite(sky_img, (0, 0)); tile.alpha_composite(cl, (0, 0))
            tile.alpha_composite(sky_img, (0, 80)); tile.alpha_composite(cl, (90, 80))
            tile.alpha_composite(cl, (-90, 80))
            prev.alpha_composite(tile.resize((W * sc, 160 * sc), Image.NEAREST), (x, y + lab))
        else:
            prev.alpha_composite(img.resize((W * sc, H * sc), Image.NEAREST), (x, y + lab))
    prev.save(os.path.join(HERE, "out", "backgrounds_preview.png"))
    print("saved", len(imgs) + 1, "->", os.path.normpath(DST))


if __name__ == "__main__":
    main()
