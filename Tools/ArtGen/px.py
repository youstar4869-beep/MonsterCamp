"""Tiny pixel-art engine: draw shapes on a grid, then auto-shade and outline."""
from PIL import Image
import math

N = 32


class Spr:
    def __init__(self, n=N):
        self.n = n
        self.g = [[None] * n for _ in range(n)]  # (base_rgb, shade_flag) or None
        self.fx = {}  # fixed pixels drawn after shading (eyes etc)

    def put(self, x, y, c, mirror=False):
        x, y = int(round(x)), int(round(y))
        for xx in ([x, self.n - 1 - x] if mirror else [x]):
            if 0 <= xx < self.n and 0 <= y < self.n:
                self.g[y][xx] = c

    def fix(self, x, y, c, mirror=False):
        for xx in ([x, self.n - 1 - x] if mirror else [x]):
            self.fx[(xx, y)] = c

    def ellipse(self, cx, cy, rx, ry, c, mirror=False):
        for y in range(self.n):
            for x in range(self.n):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1:
                    self.put(x, y, c, mirror)

    def poly(self, pts, c, mirror=False):
        for y in range(self.n):
            for x in range(self.n):
                if _inside(x + .5, y + .5, pts):
                    self.put(x, y, c, mirror)

    def rect(self, x0, y0, x1, y1, c, mirror=False):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, c, mirror)

    def render(self, pal, light=(-1, -1)):
        """pal: dict name -> (dark, mid, light). Outline drawn with outline colour."""
        n = self.n
        img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
        px = img.load()
        # bounding info per colour region for shading: use local neighbour test
        for y in range(n):
            for x in range(n):
                c = self.g[y][x]
                if c is None:
                    continue
                d, m, l = pal[c]
                col = m
                # shade: pixel whose lower/right neighbour is empty or different -> dark
                below = self.g[y + 1][x] if y + 1 < n else None
                right = self.g[y][x + 1] if x + 1 < n else None
                above = self.g[y - 1][x] if y > 0 else None
                left = self.g[y][x - 1] if x > 0 else None
                if below != c or (right != c and below != c):
                    col = d
                elif right != c and x > n // 2:
                    col = d
                elif above != c or (left != c and x < n // 2):
                    col = l
                px[x, y] = col + (255,)
        # outline
        out = pal["outline"][1]
        for y in range(n):
            for x in range(n):
                if self.g[y][x] is not None:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < n and 0 <= yy < n and self.g[yy][xx] is not None:
                        px[x, y] = out + (255,)
                        break
        for (x, y), c in self.fx.items():
            if 0 <= x < n and 0 <= y < n:
                px[x, y] = c + (255,)
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


def eye(s, x, y, big=False, mirror=True):
    W, K = (255, 255, 255), (24, 20, 37)
    if big:
        for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2)):
            s.fix(x + dx, y + dy, K, mirror)
        s.fix(x, y, W, mirror)
    else:
        s.fix(x, y, K, mirror)
        s.fix(x, y + 1, K, mirror)
        s.fix(x, y, W, mirror) if False else None


def sheet(imgs, scale=8, pad=4, bg=(46, 52, 64)):
    w = sum(i.width * scale + pad for i in imgs) + pad
    h = max(i.height for i in imgs) * scale + pad * 2
    out = Image.new("RGBA", (w, h), bg + (255,))
    x = pad
    for i in imgs:
        big = i.resize((i.width * scale, i.height * scale), Image.NEAREST)
        out.alpha_composite(big, (x, pad + (h - pad * 2 - big.height)))
        x += big.width + pad
    return out
