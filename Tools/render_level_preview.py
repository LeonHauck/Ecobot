"""
Renderiza uma imagem de "mapa" de cada fase a partir dos arquivos ASCII
em Assets/LevelData, usando os sprites do jogo. Util para documentacao
(README / relatorio) e para revisar o design das fases fora da Unity.

Uso:  python Tools/render_level_preview.py [pasta_saida] [pixels_por_bloco]
      (padrao: Docs/Mapas, 8 px por bloco)
"""
import os
import struct
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPR = os.path.join(ROOT, "Assets", "Art", "Sprites")
LEVELS = os.path.join(ROOT, "Assets", "LevelData")

THEMES = {
    "Level1": dict(top="forest_top", fill="forest_fill_0", oneway="oneway_wood", climb="vine", bg=(150, 205, 230)),
    "Level2": dict(top="cave_top", fill="cave_fill_0", oneway="oneway_wood", climb="vine", bg=(34, 28, 50)),
    "Level3": dict(top="factory_top", fill="factory_fill_0", oneway="oneway_metal", climb="ladder_metal", bg=(170, 145, 130)),
    "Level4": dict(top="tower_top", fill="tower_fill_0", oneway="oneway_metal", climb="ladder_metal", bg=(120, 80, 120)),
}


def read_png(path):
    data = open(path, "rb").read()
    pos, w, h, idat = 8, 0, 0, b""
    while pos < len(data):
        ln = struct.unpack(">I", data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + ln]
        if tag == b"IHDR":
            w, h = struct.unpack(">II", body[:8])
        elif tag == b"IDAT":
            idat += body
        pos += 12 + ln
    raw = zlib.decompress(idat)
    px, stride = [], w * 4 + 1
    for y in range(h):
        row = raw[y * stride + 1:(y + 1) * stride]   # gerado com filtro 0
        px.append([tuple(row[x * 4:x * 4 + 4]) for x in range(w)])
    return w, h, px


def write_png(path, w, h, px):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        for x in range(w):
            raw.extend(px[y][x])

    def chunk(tag, d):
        return struct.pack(">I", len(d)) + tag + d + struct.pack(">I", zlib.crc32(tag + d) & 0xFFFFFFFF)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))


_cache = {}


def sprite(name):
    if name not in _cache:
        for folder in os.listdir(SPR):
            p = os.path.join(SPR, folder, name + ".png")
            if os.path.exists(p):
                _cache[name] = read_png(p)
                break
        else:
            raise FileNotFoundError(name)
    return _cache[name]


def render(level, out_dir, ppb):
    lines = open(os.path.join(LEVELS, level + ".txt")).read().splitlines()
    H, W = len(lines), max(len(l) for l in lines)
    grid = [l.ljust(W, ".") for l in lines]
    th = THEMES[level]
    scale = 16 // ppb
    img = [[th["bg"] + (255,) for _ in range(W * ppb)] for _ in range(H * ppb)]

    def at(x, y):
        return grid[y][x] if 0 <= x < W and 0 <= y < H else "."

    def blit(name, cx, cy, dx=0, dy=0):
        w, h, px = sprite(name)
        ox = cx * ppb + dx * ppb // 16
        oy = (cy + 1) * ppb - h // scale + dy * ppb // 16
        for y in range(0, h, scale):
            for x in range(0, w, scale):
                p = px[y][x]
                X, Y = ox + x // scale, oy + y // scale
                if p[3] > 0 and 0 <= X < W * ppb and 0 <= Y < H * ppb:
                    a = p[3] / 255.0
                    b = img[Y][X]
                    img[Y][X] = tuple(int(b[i] * (1 - a) + p[i] * a) for i in range(3)) + (255,)

    simple = {"o": "seed_0", "b": "battery_0", "L": "leaf_0", "^": "spikes", "s": "slime_walk_0",
              "h": "hedgehog_walk_0", "d": "drone_fly_0", "P": "eco_idle_0", "F": "falling_0"}
    for y in range(H):
        for x in range(W):
            c = at(x, y)
            if c == "#":
                blit(th["top"] if at(x, y - 1) != "#" else th["fill"], x, y)
            elif c == "~":
                blit("toxic_top" if at(x, y - 1) != "~" else "toxic_fill", x, y)
            elif c == "=":
                if at(x, y + 1) == "H":
                    blit(th["climb"], x, y)
                blit(th["oneway"], x, y)
            elif c == "H":
                blit(th["climb"], x, y)
            elif c in simple:
                blit(simple[c], x, y, dy=-8 if c == "F" else 0)
            elif c.isdigit():
                blit("sign", x, y)
            elif c == "C":
                blit("checkpoint_off", x, y)
            elif c == "G":
                blit("goal_0", x, y, dx=-8)
            elif c in "MV":
                blit("moving_platform", x, y, dy=-8)
    # marca destinos de plataformas moveis / drones
    for y in range(H):
        for x in range(W):
            if at(x, y) == ":":
                for k in range(ppb // 2):
                    img[y * ppb + ppb // 2][x * ppb + k + ppb // 4] = (255, 80, 200, 255)
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, f"{level}_mapa.png")
    write_png(path, W * ppb, H * ppb, img)
    print("mapa:", path)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Docs", "Mapas")
    ppb = int(sys.argv[2]) if len(sys.argv) > 2 else 8
    for lv in ("Level1", "Level2", "Level3", "Level4"):
        render(lv, out, ppb)
