"""
Gerador procedural de sprites pixel-art do EcoBot.

Todos os sprites do jogo sao desenhados por este script (arte 100% original).
Uso:  python Tools/generate_sprites.py [pasta_preview]
Saida: Assets/Art/Sprites/<categoria>/<nome>.png  (16 px = 1 unidade na Unity)

Nao depende de bibliotecas externas (PNG gravado com zlib + struct).
"""
import math
import os
import random
import struct
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Sprites")


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(c1, c2, t):
    return tuple(int(round(c1[i] + (c2[i] - c1[i]) * t)) for i in range(4))


# ---------------------------------------------------------------- paleta
OUTLINE = rgb("1b2633")
G1 = rgb("2e8c57")   # verde escuro (corpo)
G2 = rgb("4cbb6c")   # verde medio
G3 = rgb("94e68c")   # brilho
SCREEN = rgb("12303a")
EYE = rgb("78f5ff")
EYE_DIM = rgb("3c8c96")
EYE_HURT = rgb("ff5a5a")
SCREEN_HURT = rgb("4a1820")
MET1 = rgb("5b6670")
MET2 = rgb("9aa7b0")
MET3 = rgb("d0d8de")
LEAF = rgb("6ed246")
LEAF2 = rgb("3c9632")
WOOD1 = rgb("6e4628")
WOOD2 = rgb("aa7846")
WOOD3 = rgb("d2a064")
GOLD1 = rgb("b4822a")
GOLD2 = rgb("e6b43c")
GOLD3 = rgb("ffe16e")
GOLD4 = rgb("fff8c8")
RED = rgb("d23c3c")
WHITE = rgb("ffffff")
BLACK = rgb("101018")


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [[None] * w for _ in range(h)]

    def set(self, x, y, c):
        if c is not None and 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return None

    def clear(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = None

    def rect(self, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.set(xx, yy, c)

    def points(self, pts, c, ox=0, oy=0):
        for (x, y) in pts:
            self.set(x + ox, y + oy, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                dx = (x + 0.5 - cx) / max(rx, 0.01)
                dy = (y + 0.5 - cy) / max(ry, 0.01)
                if dx * dx + dy * dy <= 1.0:
                    self.set(x, y, c)

    def outline(self, c=OUTLINE):
        add = []
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is None:
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        n = self.get(x + dx, y + dy)
                        if n is not None and n != c:
                            add.append((x, y))
                            break
        for x, y in add:
            self.px[y][x] = c
        return self

    def flip_h(self):
        n = Canvas(self.w, self.h)
        for y in range(self.h):
            n.px[y] = list(reversed(self.px[y]))
        return n

    def paste(self, other, ox, oy):
        for y in range(other.h):
            for x in range(other.w):
                if other.px[y][x] is not None:
                    self.set(x + ox, y + oy, other.px[y][x])

    def save(self, rel):
        path = os.path.join(OUT, rel + ".png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        write_png(path, self)
        SAVED.append((rel, self))


SAVED = []


def write_png(path, canvas):
    raw = bytearray()
    for y in range(canvas.h):
        raw.append(0)
        for x in range(canvas.w):
            c = canvas.px[y][x]
            raw.extend(c if c is not None else (0, 0, 0, 0))

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", canvas.w, canvas.h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


# ================================================================ PERSONAGEM
def robot(bob=0, lean=0, back_leg=(0, 0), front_leg=(0, 0), arms="down",
          eyes="normal", back_view=False, climb_phase=0):
    """Eco, o robo reciclador. Desenhado virado para a direita."""
    c = Canvas(16, 16)
    b = bob

    def leg(x, dx, lift, col):
        h = 2 - lift
        c.rect(x + dx, 13 - lift, 2, max(h, 1), col)

    def arm(pts, col):
        c.points(pts, col, 0, b)

    # pernas
    if back_view:
        leg(5, 0, 1 if climb_phase == 0 else 0, MET1)
        leg(9, 0, 0 if climb_phase == 0 else 1, MET1)
    else:
        leg(5, back_leg[0], back_leg[1], MET1)

    # braco de tras
    arm_sets = {
        "down": ([(4, 9), (4, 10), (4, 11)], [(11, 9), (11, 10), (11, 11)]),
        "swingA": ([(4, 9), (3, 10), (3, 11)], [(11, 9), (12, 10), (12, 11)]),
        "swingB": ([(4, 9), (4, 10), (5, 11)], [(11, 9), (11, 10), (10, 11)]),
        "pumpA": ([(4, 9), (3, 10), (2, 10)], [(11, 9), (12, 9), (13, 8)]),
        "pumpB": ([(4, 9), (4, 10), (4, 11)], [(11, 10), (12, 11), (11, 11)]),
        "up": ([(4, 9), (3, 8), (3, 7)], [(11, 9), (12, 8), (12, 7)]),
        "out": ([(4, 9), (3, 9), (2, 8)], [(11, 9), (12, 9), (13, 8)]),
        "fling": ([(4, 9), (3, 8), (2, 7)], [(11, 9), (12, 8), (13, 7)]),
        "cheer": ([(4, 9), (3, 8), (3, 7), (3, 6)], [(11, 9), (12, 8), (12, 7), (12, 6)]),
    }
    if back_view:
        up_l = [(4, 9), (3, 8), (3, 7), (3, 6)]
        dn_l = [(4, 9), (4, 10), (4, 11)]
        up_r = [(11, 9), (12, 8), (12, 7), (12, 6)]
        dn_r = [(11, 9), (11, 10), (11, 11)]
        arm(up_l if climb_phase == 0 else dn_l, MET2)
        arm(dn_r if climb_phase == 0 else up_r, MET2)
    else:
        arm(arm_sets[arms][0], MET1)

    # corpo
    c.rect(5, 9 + b, 6, 4, G1)
    c.rect(6, 10 + b, 4, 2, G2)
    if back_view:
        c.rect(7, 10 + b, 2, 2, MET1)          # bateria nas costas
    else:
        c.set(8, 10 + b, LEAF)                 # simbolo de folha no peito
        c.set(9, 11 + b, LEAF)
        c.set(8, 11 + b, LEAF2)

    # cabeca
    hx = lean
    c.rect(4 + hx, 3 + b, 8, 6, G2)
    for (x, y) in ((4, 3), (11, 3), (4, 8), (11, 8)):
        c.clear(x + hx, y + b)
    c.rect(5 + hx, 3 + b, 6, 1, G3)
    c.rect(4 + hx, 8 + b - 1, 1, 1, G1)
    c.rect(5 + hx, 8 + b, 6, 1, G1)

    # antena + folhinha
    ax = 7 + hx if not back_view else 8
    c.set(ax, 2 + b, MET2)
    c.set(ax, 1 + b, LEAF)
    c.set(ax + 1, 1 + b, LEAF)
    c.set(ax + 1, 0 + b, LEAF2) if b > 0 else None

    if back_view:
        c.rect(6, 5 + b, 4, 3, MET1)
        c.set(6, 5 + b, MET2)
        c.set(9, 7 + b, MET2)
    else:
        scr = SCREEN_HURT if eyes == "hurt" else SCREEN
        c.rect(6 + hx, 5 + b, 5, 3, scr)
        ey = {"normal": 6, "up": 5, "down": 7, "blink": 6, "hurt": 6, "happy": 5}[eyes]
        col = {"blink": EYE_DIM, "hurt": EYE_HURT}.get(eyes, EYE)
        c.set(8 + hx, ey + b, col)
        c.set(10 + hx, ey + b, col)
        if eyes == "hurt":
            c.set(9 + hx, 7 + b, EYE_HURT)
        if eyes == "happy":
            c.set(9 + hx, 7 + b, EYE)

    # perna da frente e braco da frente
    if not back_view:
        leg(9, front_leg[0], front_leg[1], MET2)
        arm(arm_sets[arms][1], MET2)

    return c.outline()


def make_player():
    p = "Player/"
    robot().save(p + "eco_idle_0")
    robot(bob=1).save(p + "eco_idle_1")
    robot(eyes="blink").save(p + "eco_idle_2")

    walk = [
        dict(back_leg=(-1, 0), front_leg=(1, 0), arms="swingA"),
        dict(bob=1, back_leg=(0, 1), front_leg=(0, 0), arms="down"),
        dict(back_leg=(1, 0), front_leg=(-1, 0), arms="swingB"),
        dict(bob=1, back_leg=(0, 0), front_leg=(0, 1), arms="down"),
    ]
    for i, w in enumerate(walk):
        robot(**w).save(p + f"eco_walk_{i}")

    run = [
        dict(lean=1, back_leg=(-2, 0), front_leg=(2, 1), arms="pumpA"),
        dict(lean=1, bob=1, back_leg=(-1, 1), front_leg=(0, 0), arms="down"),
        dict(lean=1, back_leg=(2, 1), front_leg=(-2, 0), arms="pumpB"),
        dict(lean=1, bob=1, back_leg=(0, 0), front_leg=(1, 1), arms="down"),
    ]
    for i, r in enumerate(run):
        robot(**r).save(p + f"eco_run_{i}")

    robot(back_leg=(-1, 1), front_leg=(1, 1), arms="up", eyes="up").save(p + "eco_jump")
    robot(back_leg=(-1, 0), front_leg=(1, 0), arms="out", eyes="down").save(p + "eco_fall")
    robot(back_view=True, climb_phase=0).save(p + "eco_climb_0")
    robot(back_view=True, climb_phase=1).save(p + "eco_climb_1")
    robot(lean=-1, back_leg=(-1, 0), front_leg=(1, 0), arms="fling", eyes="hurt").save(p + "eco_hurt")
    robot(arms="cheer", eyes="happy").save(p + "eco_win_0")
    robot(bob=1, arms="up", eyes="happy").save(p + "eco_win_1")


# ================================================================ INIMIGOS
SL1 = rgb("5a4668")
SL2 = rgb("8a6a9a")
SL3 = rgb("b896c4")


def slime(w, h, dead=False):
    c = Canvas(16, 16)
    cx = 8
    bottom = 15
    c.ellipse(cx, bottom - h / 2.0, w / 2.0, h / 2.0, SL2)
    c.rect(int(cx - w / 2) + 1, bottom - 2, w - 2, 2, SL2)
    c.ellipse(cx, bottom - h / 2.0 + 1, w / 2.0 - 1.5, h / 2.0 - 1.5, SL2)
    # sombra e brilho
    for x in range(16):
        for y in range(16):
            if c.get(x, y) == SL2 and (c.get(x, y + 1) is None or y >= bottom - 1):
                c.set(x, y, SL1)
    top = bottom - h
    c.set(int(cx - w / 4), top + 2, SL3)
    c.set(int(cx - w / 4) + 1, top + 1, SL3)
    if dead:
        c.rect(3, 12, 4, 2, MET2)                      # lata tombada
        c.set(4, 12, RED)
        c.set(9, bottom - 2, BLACK)
        c.set(11, bottom - 2, BLACK)
    else:
        # lata de lixo presa na cabeca
        lx = cx - 1
        c.rect(lx, top - 3, 4, 4, MET2)
        c.rect(lx, top - 2, 4, 1, RED)
        c.set(lx + 3, top - 3, MET3)
        # olhos bravos
        ey = top + max(2, h // 2 - 1)
        for ex in (cx - 3, cx + 2):
            c.rect(ex, ey, 2, 2, WHITE)
            c.set(ex + 1, ey + 1, BLACK)
        c.set(cx - 3, ey - 1, BLACK)
        c.set(cx + 3, ey - 1, BLACK)
        c.set(cx - 1, ey + 3, BLACK) if h >= 7 else None
    return c.outline()


def make_slime():
    for i, (w, h) in enumerate([(12, 8), (13, 7), (12, 8), (11, 9)]):
        slime(w, h).save(f"Enemies/slime_walk_{i}")
    slime(14, 4, dead=True).save("Enemies/slime_dead")


def drone(frame, dead=False):
    c = Canvas(16, 16)
    # helice
    if not dead:
        blades = [(3, 13), (5, 11), (7, 9)][frame]
        c.rect(blades[0], 2, blades[1] - blades[0], 1, MET3)
        c.set(8, 3, MET1)
        c.set(7, 3, MET1)
    # corpo
    off = 1 if dead else 0
    c.ellipse(7.5 + off, 7.5 + off, 5, 3.6, MET1)
    c.ellipse(7.5 + off, 7.0 + off, 4, 2.6, MET2)
    c.rect(4 + off, 6 + off, 3, 1, MET3)
    # olho
    eye = MET1 if dead else EYE_HURT
    c.rect(7 + off, 7 + off, 2, 2, eye)
    if not dead:
        c.set(7, 7, rgb("ffd2c8"))
    # bracinhos
    c.set(3 + off, 10 + off, MET1)
    c.set(12 + off, 10 + off, MET1)
    c.outline()
    # fumaca saindo
    smoke = rgb("8c8c96", 200) if not dead else rgb("3c3c46", 220)
    puffs = [[(6, 13), (9, 14)], [(7, 14), (10, 13)], [(5, 14), (8, 13)]][frame if not dead else 0]
    for (x, y) in puffs:
        c.set(x, y, smoke)
        c.set(x + 1, y, smoke)
    return c


def make_drone():
    for i in range(3):
        drone(i).save(f"Enemies/drone_fly_{i}")
    drone(0, dead=True).save("Enemies/drone_dead")


RUST1 = rgb("8a4628")
RUST2 = rgb("c46e3c")
RUST3 = rgb("e69a5a")


def hedgehog(frame):
    c = Canvas(16, 16)
    legs = [((4, 0), (10, 1)), ((5, 1), (9, 0)), ((4, 1), (10, 0)), ((5, 0), (9, 1))][frame]
    for (x, lift) in legs:
        c.rect(x, 13 - lift, 2, 2 - lift if lift else 2, RUST1)
    c.ellipse(7.5, 10.5, 6, 3.6, RUST2)
    c.rect(3, 9, 9, 2, RUST2)
    c.rect(4, 12, 8, 1, RUST1)
    # espinhos de metal
    spikes = [(3, 7), (5, 5), (7, 4), (9, 5), (11, 6), (4, 6), (8, 5), (10, 5), (6, 5)]
    for (x, y) in spikes:
        c.set(x, y, MET3)
        c.set(x, y + 1, MET2)
    c.set(2, 9, MET2)
    c.set(1, 8, MET3)
    # focinho e olho (virado para a direita)
    c.rect(12, 10, 2, 2, RUST3)
    c.set(14, 11, BLACK)
    c.set(11, 9, WHITE)
    c.set(12, 9, BLACK)
    return c.outline()


def make_hedgehog():
    for i in range(4):
        hedgehog(i).save(f"Enemies/hedgehog_walk_{i}")


# ================================================================ COLETAVEIS
def seed(frame):
    c = Canvas(16, 16)
    widths = [5, 4, 2, 1, 2, 4]
    w = widths[frame]
    cx, cy = 8, 9
    c.ellipse(cx, cy, w / 2.0 + 0.5, 3.6, GOLD2)
    if w >= 2:
        c.ellipse(cx - 0.5, cy - 0.5, max(w / 2.0 - 0.8, 0.3), 2.2, GOLD3)
    c.set(cx - 1 if w > 2 else cx, cy + 2, GOLD1)
    # broto
    c.set(cx, cy - 4, LEAF2)
    c.set(cx, cy - 5, LEAF)
    c.set(cx + 1, cy - 6, LEAF)
    if w > 2:
        c.set(cx - 1, cy - 6, LEAF)
    c.outline(GOLD1 if False else OUTLINE)
    # brilho girando
    sparkle = [(3, 4), (12, 5), (13, 12), (2, 12), (4, 3), (12, 4)][frame]
    c.set(sparkle[0], sparkle[1], GOLD4)
    return c


def make_items():
    for i in range(6):
        seed(i).save(f"Items/seed_{i}")

    for i in range(2):
        c = Canvas(16, 16)
        c.rect(6, 3, 4, 2, MET2)
        c.rect(4, 5, 8, 9, rgb("2f7d4a"))
        c.rect(5, 6, 6, 7, rgb("58c878") if i == 0 else rgb("8cf0a0"))
        bolt = [(8, 7), (7, 8), (6, 9), (7, 9), (8, 9), (9, 9), (8, 10), (7, 11)]
        c.points(bolt, GOLD3 if i == 0 else WHITE)
        c.outline()
        if i == 1:
            for (x, y) in ((2, 4), (13, 6), (3, 13)):
                c.set(x, y, GOLD4)
        c.save(f"Items/battery_{i}")

    for i in range(4):
        c = Canvas(16, 16)
        leaf_pts = []
        for y in range(3, 14):
            t = (y - 3) / 10.0
            half = int(round(math.sin(t * math.pi) * 4.2))
            for x in range(8 - half, 8 + half + 1):
                leaf_pts.append((x, y))
        c.points(leaf_pts, GOLD2)
        for y in range(4, 13):
            c.set(8, y, GOLD1)
        for y in range(5, 10):
            c.set(6, y, GOLD3)
        c.set(8, 14, GOLD1)
        c.outline()
        shine = [(5, 4), (11, 6), (11, 11), (4, 10)][i]
        c.set(shine[0], shine[1], GOLD4)
        c.set(shine[0] + 1, shine[1], GOLD4)
        c.save(f"Items/leaf_{i}")


# ================================================================ CHECKPOINT / OBJETIVO
def make_checkpoint():
    for name, light, glow in (("off", MET1, None), ("on_0", LEAF, rgb("b4ff8c", 160)),
                              ("on_1", G3, rgb("b4ff8c", 90))):
        c = Canvas(16, 32)
        c.rect(5, 28, 6, 3, MET1)
        c.rect(7, 12, 2, 16, MET2)
        c.rect(7, 12, 1, 16, MET3)
        c.rect(4, 6, 8, 7, MET1)
        c.rect(5, 7, 6, 5, light)
        c.set(6, 8, WHITE if name != "off" else MET2)
        # folha no topo
        c.rect(7, 4, 2, 2, MET2)
        c.outline()
        if glow:
            for (x, y) in ((3, 5), (12, 5), (2, 9), (13, 9), (3, 13), (12, 13), (8, 2)):
                c.set(x, y, glow)
        c.save(f"Level/checkpoint_{name}")


def make_goal():
    for f in range(4):
        c = Canvas(32, 32)
        # vaso de pedra
        c.rect(9, 25, 14, 6, rgb("7a6a5a"))
        c.rect(8, 24, 16, 2, rgb("9a8a78"))
        c.rect(10, 27, 12, 1, rgb("5e5044"))
        # tronco
        c.rect(15, 14, 2, 10, WOOD2)
        c.rect(15, 14, 1, 10, WOOD3)
        # copa
        c.ellipse(16, 10, 7, 5, LEAF2)
        c.ellipse(15, 9, 5.5, 3.8, LEAF)
        c.ellipse(13, 8, 2, 1.5, G3)
        c.outline()
        # brilho pulsante
        r = [9, 10, 11, 10][f]
        glow = rgb("fff3a0", [140, 180, 220, 180][f])
        for k in range(10):
            ang = k * math.pi * 2 / 10 + f * 0.3
            x = int(round(16 + math.cos(ang) * r))
            y = int(round(11 + math.sin(ang) * (r - 2)))
            if c.get(x, y) is None:
                c.set(x, y, glow)
        c.save(f"Level/goal_{f}")


def make_level_objects():
    # espinhos
    c = Canvas(16, 16)
    for sx in (0, 5, 10):
        for row in range(7):
            half = row // 2
            y = 8 + row
            for x in range(sx + 2 - half, sx + 3 + half):
                if 0 <= x < 16:
                    c.set(x, y, MET2 if x > sx + 2 - half else MET3)
    c.rect(0, 14, 16, 1, MET1)
    c.outline()
    c.save("Level/spikes")

    # plataforma movel (48x8)
    c = Canvas(48, 8)
    c.rect(0, 0, 48, 8, MET1)
    c.rect(1, 1, 46, 5, MET2)
    c.rect(1, 1, 46, 1, MET3)
    for x in range(3, 46, 6):
        c.set(x, 4, MET1)
    c.rect(21, 3, 6, 2, LEAF)
    c.rect(0, 7, 48, 1, OUTLINE)
    c.save("Level/moving_platform")

    # plataforma que cai (16x8): normal e rachada
    for name in ("falling_0", "falling_1"):
        c = Canvas(16, 8)
        c.rect(0, 0, 16, 8, rgb("6a5a4a"))
        c.rect(1, 1, 14, 5, rgb("a08a70"))
        c.rect(1, 1, 14, 1, rgb("c8b090"))
        c.rect(0, 7, 16, 1, OUTLINE)
        if name == "falling_1":
            for (x, y) in ((5, 1), (6, 2), (6, 3), (7, 4), (10, 2), (11, 3), (10, 5)):
                c.set(x, y, OUTLINE)
        c.save(f"Level/{name}")

    # placa de dica
    c = Canvas(16, 16)
    c.rect(7, 9, 2, 7, WOOD1)
    c.rect(2, 2, 12, 8, WOOD2)
    c.rect(2, 2, 12, 1, WOOD3)
    c.rect(4, 4, 8, 1, WOOD1)
    c.rect(4, 6, 6, 1, WOOD1)
    c.outline()
    c.save("Level/sign")


# ================================================================ TILES
def noise_tile(base, dark, light, seed_, density=0.12):
    r = random.Random(seed_)
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, base)
    for _ in range(int(256 * density)):
        x, y = r.randrange(16), r.randrange(16)
        c.set(x, y, dark if r.random() < 0.6 else light)
    return c


def make_tiles():
    # ---- floresta
    dirt, dirt_d, dirt_l = rgb("7a5032"), rgb("5c3a24"), rgb("9a6c48")
    grass, grass_d, grass_l = rgb("4caa46"), rgb("2e7a34"), rgb("8cdc5a")
    for i, sd in enumerate((11, 12)):
        noise_tile(dirt, dirt_d, dirt_l, sd).save(f"Tiles/forest_fill_{i}")
    c = noise_tile(dirt, dirt_d, dirt_l, 13)
    for x in range(16):
        h = 4 + (1 if x % 5 in (1, 2) else 0) + (1 if x % 7 == 3 else 0)
        c.rect(x, 0, 1, h, grass)
        c.set(x, h - 1, grass_d)
        if x % 3 == 0:
            c.set(x, 0, grass_l)
    for x in (2, 7, 13):
        c.set(x, 1, grass_l)
    c.save("Tiles/forest_top")

    # ---- caverna
    rock, rock_d, rock_l = rgb("464058"), rgb("2e2a3c"), rgb("625a78")
    for i, sd in enumerate((21, 22)):
        c = noise_tile(rock, rock_d, rock_l, sd, 0.1)
        c.rect(0, 7, 9, 1, rock_d)
        c.rect(8, 0, 1, 7, rock_d)
        c.rect(12, 8, 1, 8, rock_d)
        c.save(f"Tiles/cave_fill_{i}")
    c = noise_tile(rock, rock_d, rock_l, 23, 0.1)
    moss, moss_d = rgb("5a9a64"), rgb("3a6e48")
    for x in range(16):
        h = 2 + (1 if x % 4 in (0, 1) else 0)
        c.rect(x, 0, 1, h, moss)
        c.set(x, h, moss_d)
    c.save("Tiles/cave_top")

    # ---- fabrica
    m, md, ml = rgb("6c7480"), rgb("4a505a"), rgb("8e98a4")
    for i in range(2):
        c = Canvas(16, 16)
        c.rect(0, 0, 16, 16, m)
        c.rect(0, 0, 16, 1, ml)
        c.rect(0, 0, 1, 16, ml)
        c.rect(0, 15, 16, 1, md)
        c.rect(15, 0, 1, 16, md)
        for (x, y) in ((2, 2), (13, 2), (2, 13), (13, 13)):
            c.set(x, y, md)
            c.set(x - 1, y - 1, ml)
        if i == 1:
            c.rect(4, 6, 8, 4, md)
            c.rect(5, 7, 6, 2, rgb("3a3e46"))
        c.save(f"Tiles/factory_fill_{i}")
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, m)
    c.rect(0, 15, 16, 1, md)
    for x in range(16):
        for y in range(0, 4):
            c.set(x, y, rgb("e6be28") if ((x + y) // 3) % 2 == 0 else rgb("23232a"))
    c.rect(0, 4, 16, 1, md)
    for (x, y) in ((3, 9), (12, 9)):
        c.set(x, y, md)
    c.save("Tiles/factory_top")

    # ---- torre (tijolos)
    br, brd, mort = rgb("8c4638"), rgb("6a3228"), rgb("4a3634")

    def bricks(c, y0):
        for y in range(y0, 16):
            row = (y - y0) // 4
            for x in range(16):
                edge = (y - y0) % 4 == 3 or (x + (row % 2) * 4) % 8 == 7
                c.set(x, y, mort if edge else (brd if (x + y) % 5 == 0 else br))

    for i in range(2):
        c = Canvas(16, 16)
        bricks(c, 0)
        if i == 1:
            c.set(5, 5, rgb("5a8a4a"))
            c.set(6, 5, rgb("5a8a4a"))
        c.save(f"Tiles/tower_fill_{i}")
    c = Canvas(16, 16)
    bricks(c, 4)
    c.rect(0, 0, 16, 4, rgb("a29a90"))
    c.rect(0, 0, 16, 1, rgb("c8c0b4"))
    c.rect(0, 3, 16, 1, rgb("6e665e"))
    c.save("Tiles/tower_top")

    # ---- plataformas de mao unica
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 5, WOOD2)
    c.rect(0, 0, 16, 1, WOOD3)
    c.rect(0, 4, 16, 1, WOOD1)
    c.set(2, 2, WOOD1)
    c.set(13, 2, WOOD1)
    c.rect(2, 5, 2, 2, WOOD1)
    c.rect(12, 5, 2, 2, WOOD1)
    c.save("Tiles/oneway_wood")
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 4, MET1)
    c.rect(0, 0, 16, 1, MET3)
    for x in range(1, 16, 3):
        c.rect(x, 1, 2, 2, MET2)
    c.rect(0, 4, 16, 1, OUTLINE)
    c.save("Tiles/oneway_metal")

    # ---- escalaveis
    c = Canvas(16, 16)
    for y in range(16):
        x = 7 + int(round(math.sin(y / 16.0 * math.pi * 2) * 1.2))
        c.set(x, y, LEAF2)
        c.set(x + 1, y, rgb("4a9a3a"))
    for (lx, ly, d) in ((5, 2, -1), (10, 7, 1), (5, 11, -1), (10, 15, 1)):
        c.set(lx, ly, LEAF)
        c.set(lx + d, ly, LEAF)
        c.set(lx, ly - 1, LEAF)
        c.set(lx + d * 2, ly - 1, LEAF2)
    c.save("Tiles/vine")
    for name, rail, rung in (("ladder_wood", WOOD2, WOOD3), ("ladder_metal", MET1, MET2)):
        c = Canvas(16, 16)
        c.rect(2, 0, 2, 16, rail)
        c.rect(12, 0, 2, 16, rail)
        for y in (2, 6, 10, 14):
            c.rect(4, y, 8, 1, rung)
        c.rect(2, 0, 1, 16, OUTLINE)
        c.rect(13, 0, 1, 16, OUTLINE)
        c.save(f"Tiles/{name}")

    # ---- lodo toxico
    tox, tox_d, tox_l = rgb("5ac83c"), rgb("3a9628"), rgb("aaff5a")
    c = Canvas(16, 16)
    c.rect(0, 3, 16, 13, tox)
    for x in range(16):
        top = 2 + (1 if x % 6 in (0, 1, 2) else 0)
        c.rect(x, top, 1, 1, tox_l)
    for (x, y) in ((3, 7), (10, 10), (6, 13), (13, 6)):
        c.set(x, y, tox_l)
        c.set(x + 1, y, tox_d)
    c.set(7, 0, tox_l)
    c.set(12, 1, tox_l)
    c.save("Tiles/toxic_top")
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, tox_d)
    for (x, y) in ((3, 4), (10, 9), (6, 13), (13, 2)):
        c.set(x, y, tox)
    c.save("Tiles/toxic_fill")


# ================================================================ DECORACAO
def make_decor():
    d = "Decor/"
    c = Canvas(16, 16)
    for x, h in ((3, 3), (5, 5), (7, 4), (9, 6), (11, 3), (12, 4)):
        c.rect(x, 16 - h, 1, h, rgb("5cbc4c") if x % 2 else rgb("3c8c3c"))
    c.save(d + "grass_tuft")
    for name, petal in (("flower_red", rgb("e64650")), ("flower_yellow", rgb("f0d23c"))):
        c = Canvas(16, 16)
        c.rect(7, 10, 1, 6, LEAF2)
        c.set(8, 13, LEAF)
        c.points([(6, 8), (8, 8), (7, 7), (7, 9)], petal)
        c.set(7, 8, GOLD3 if petal[0] > 200 and petal[1] < 100 else WOOD2)
        c.outline()
        c.save(d + name)
    c = Canvas(16, 16)
    c.rect(7, 12, 2, 4, rgb("e8dcc8"))
    c.ellipse(8, 11, 4, 2.4, rgb("d2463c"))
    c.points([(6, 10), (9, 10)], WHITE)
    c.outline()
    c.save(d + "mushroom")
    c = Canvas(16, 16)
    c.ellipse(8, 12, 6, 3.6, rgb("3c8c3c"))
    c.ellipse(6, 11, 3, 2.4, rgb("5cbc4c"))
    c.outline()
    c.save(d + "bush")
    c = Canvas(16, 16)
    c.ellipse(8, 13, 5, 2.6, rgb("8a8a96"))
    c.ellipse(7, 12.5, 3, 1.5, rgb("aaaab4"))
    c.outline()
    c.save(d + "rock")
    c = Canvas(16, 16)            # lixo: saco preto
    c.ellipse(8, 12, 4.5, 3.6, rgb("2c2c34"))
    c.rect(7, 7, 2, 2, rgb("2c2c34"))
    c.set(6, 10, rgb("50505c"))
    c.outline()
    c.save(d + "trash_bag")
    c = Canvas(16, 16)            # lata amassada
    c.rect(5, 12, 6, 3, MET2)
    c.rect(5, 13, 6, 1, RED)
    c.outline()
    c.save(d + "trash_can")
    c = Canvas(16, 16)            # cristal (caverna)
    c.points([(7, 8), (7, 9), (8, 9), (6, 10), (7, 10), (8, 10), (6, 11), (7, 11), (8, 11),
              (6, 12), (7, 12), (8, 12), (6, 13), (7, 13), (8, 13), (6, 14), (7, 14), (8, 14),
              (9, 12), (10, 11), (10, 12), (9, 13), (10, 13), (9, 14), (10, 14)], rgb("64b4f0"))
    c.points([(7, 9), (7, 10), (7, 11)], rgb("c8f0ff"))
    c.outline()
    c.save(d + "crystal")
    c = Canvas(16, 16)            # cogumelo brilhante
    c.rect(7, 12, 2, 4, rgb("c8dcd2"))
    c.ellipse(8, 11, 4, 2.2, rgb("3cc8b4"))
    c.points([(6, 10), (10, 11)], rgb("b4fff0"))
    c.outline()
    c.save(d + "glow_mushroom")
    c = Canvas(16, 16)            # estalagmite
    for row in range(9):
        half = (row + 1) // 3
        c.rect(8 - half, 7 + row, half * 2 + 1, 1, rgb("5a5070"))
    c.outline()
    c.save(d + "stalagmite")
    c = Canvas(16, 16)            # barril toxico
    c.rect(4, 6, 8, 10, rgb("c8a028"))
    c.rect(4, 8, 8, 1, rgb("8c6e14"))
    c.rect(4, 13, 8, 1, rgb("8c6e14"))
    c.points([(7, 10), (8, 10), (7, 11), (8, 11)], BLACK)
    c.rect(5, 5, 3, 1, rgb("aaff5a"))
    c.outline()
    c.save(d + "barrel")
    c = Canvas(16, 16)            # engrenagem
    c.ellipse(8, 11, 4, 4, MET1)
    for (x, y) in ((8, 6), (8, 15), (3, 11), (12, 11), (5, 8), (11, 8), (5, 14), (11, 14)):
        c.set(x, y, MET1)
    c.ellipse(8, 11, 1.5, 1.5, MET3)
    c.outline()
    c.save(d + "gear")
    c = Canvas(16, 16)            # cano com valvula
    c.rect(0, 11, 16, 3, MET1)
    c.rect(0, 11, 16, 1, MET2)
    c.rect(7, 8, 2, 3, MET2)
    c.rect(5, 7, 6, 1, RED)
    c.outline()
    c.save(d + "pipe")
    c = Canvas(16, 16)            # vaso com plantinha (esperanca)
    c.rect(5, 12, 6, 4, rgb("b45a3c"))
    c.rect(4, 11, 8, 1, rgb("d2785a"))
    c.rect(7, 7, 1, 4, LEAF2)
    c.points([(6, 7), (5, 6), (8, 8), (9, 7)], LEAF)
    c.outline()
    c.save(d + "plant_pot")


# ================================================================ UI / FX
def make_ui():
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, rgb("1b2633", 235))
    c.rect(1, 1, 14, 14, rgb("24384a", 235))
    c.rect(1, 1, 14, 1, rgb("4cbb6c"))
    c.rect(0, 0, 16, 1, OUTLINE)
    c.save("UI/ui_panel")
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, OUTLINE)
    c.rect(1, 1, 14, 14, rgb("dfe9e2"))
    c.rect(1, 12, 14, 3, rgb("aab8b0"))
    c.rect(1, 1, 14, 1, WHITE)
    c.save("UI/ui_button")
    # icone de vida = cabeca do robo
    head = robot()
    c = Canvas(16, 16)
    for y in range(0, 10):
        for x in range(16):
            if head.px[y][x] is not None:
                c.set(x, y + 4, head.px[y][x])
    c.save("UI/icon_life")
    for name, fill in (("icon_energy_full", rgb("58c878")), ("icon_energy_empty", rgb("2a3a34"))):
        c = Canvas(16, 16)
        c.rect(6, 1, 4, 2, MET2)
        c.rect(3, 3, 10, 12, rgb("2f7d4a") if fill[0] > 50 else rgb("3a4a44"))
        c.rect(4, 4, 8, 10, fill)
        if fill[0] > 50:
            c.points([(8, 6), (7, 7), (6, 8), (7, 8), (8, 8), (9, 8), (8, 9), (7, 10)], GOLD3)
        c.outline()
        c.save(f"UI/{name}")
    c = seed(0)
    c.save("UI/icon_seed")

    # particulas
    c = Canvas(8, 8)
    c.points([(3, 0), (4, 0), (3, 7), (4, 7), (0, 3), (0, 4), (7, 3), (7, 4)], rgb("ffffff", 160))
    c.rect(3, 1, 2, 6, WHITE)
    c.rect(1, 3, 6, 2, WHITE)
    c.save("FX/fx_sparkle")
    c = Canvas(8, 8)
    c.ellipse(4, 4, 3.5, 3.5, rgb("ffffff", 200))
    c.ellipse(3.5, 3.5, 2, 2, WHITE)
    c.save("FX/fx_dust")
    c = Canvas(4, 4)
    c.rect(0, 0, 4, 4, WHITE)
    c.save("FX/fx_pixel")


# ================================================================ FUNDOS (parallax)
BW, BH = 160, 120


def wave(x, terms):
    return sum(a * math.sin(2 * math.pi * k * x / BW + p) for (a, k, p) in terms)


def gradient(c, top, bottom, y0=0, y1=BH):
    for y in range(y0, y1):
        t = (y - y0) / max(1, (y1 - y0 - 1))
        col = mix(top, bottom, t)
        for x in range(BW):
            c.set(x, y, col)


def wrap_ellipse(c, cx, cy, rx, ry, col):
    for off in (-BW, 0, BW):
        c.ellipse(cx + off, cy, rx, ry, col)


def ridge(c, base, terms, col, y_end=BH):
    for x in range(BW):
        top = int(base + wave(x, terms))
        for y in range(max(0, top), y_end):
            c.set(x, y, col)


def make_backgrounds():
    r = random.Random(7)
    # ---- floresta
    c = Canvas(BW, BH)
    gradient(c, rgb("6eb4e6"), rgb("cdeef0"))
    for (cx, cy, rx) in ((20, 22, 12), (75, 14, 16), (130, 28, 11), (150, 18, 9)):
        wrap_ellipse(c, cx, cy, rx, rx * 0.35, rgb("ffffff", 230))
        wrap_ellipse(c, cx + 6, cy - 3, rx * 0.6, rx * 0.35, rgb("ffffff", 230))
    ridge(c, 74, [(6, 1, 0.3), (3, 3, 1.2)], rgb("96c8b4"))
    ridge(c, 88, [(5, 2, 2.0), (2, 5, 0.4)], rgb("6eaa8c"))
    c.save("Backgrounds/forest_far")
    c = Canvas(BW, BH)
    for i in range(8):
        tx = i * 20 + r.randrange(-4, 5)
        th = r.randrange(26, 42)
        base = 108
        for off in (-BW, 0, BW):
            c.rect(tx + off - 1, base - th // 2, 3, th // 2 + 2, rgb("3c5a3c"))
            c.ellipse(tx + off + 0.5, base - th // 2 - 4, 8, th / 2.2, rgb("2e6e46"))
            c.ellipse(tx + off - 1.5, base - th // 2 - 7, 4, th / 4.0, rgb("3c8250"))
    ridge(c, 104, [(2, 3, 0.0), (1, 7, 1.0)], rgb("2a5a3a"))
    c.save("Backgrounds/forest_mid")

    # ---- caverna
    c = Canvas(BW, BH)
    gradient(c, rgb("16121e"), rgb("2e2840"))
    for _ in range(90):
        x, y = r.randrange(BW), r.randrange(BH)
        c.rect(x, y, r.randrange(2, 6), r.randrange(1, 3), rgb("3a3250"))
    for _ in range(14):
        x, y = r.randrange(BW), r.randrange(20, BH)
        c.set(x, y, rgb("78c8ff"))
        c.set(x + 1, y, rgb("78c8ff", 120))
    c.save("Backgrounds/cave_far")
    c = Canvas(BW, BH)
    for i in range(10):
        x = i * 16 + r.randrange(0, 8)
        h = r.randrange(10, 34)
        for row in range(h):
            half = max(0, int((h - row) / h * 5))
            c.rect(x - half, row, half * 2 + 1, 1, rgb("2c2442"))
        h2 = r.randrange(8, 26)
        for row in range(h2):
            half = max(0, int((h2 - row) / h2 * 6))
            c.rect(x + 8 - half, BH - 1 - row, half * 2 + 1, 1, rgb("2c2442"))
    c.save("Backgrounds/cave_mid")

    # ---- fabrica
    c = Canvas(BW, BH)
    gradient(c, rgb("786e78"), rgb("d2a882"))
    c.ellipse(118, 40, 9, 9, rgb("f0d2a0", 200))
    for (cx, cy, rx) in ((30, 20, 18), (95, 14, 22), (150, 26, 14)):
        wrap_ellipse(c, cx, cy, rx, rx * 0.3, rgb("8c8288", 150))
    for i in range(6):
        x = i * 27 + 5
        h = 28 + (i * 13) % 20
        c.rect(x, BH - h, 20, h, rgb("6e6672"))
        c.rect(x + 14, BH - h - 22, 4, 22, rgb("6e6672"))
        for k in range(4):
            wrap_ellipse(c, x + 16 + k * 3, BH - h - 26 - k * 6, 3 + k, 2 + k * 0.6, rgb("968c90", 150))
    c.save("Backgrounds/factory_far")
    c = Canvas(BW, BH)
    c.rect(0, 70, BW, 4, rgb("4a4652"))
    c.rect(0, 70, BW, 1, rgb("6a6472"))
    for x in range(0, BW, 32):
        c.rect(x + 4, 74, 3, BH - 74, rgb("3e3a46"))
        for y in range(76, BH, 8):
            c.rect(x + 7, y, 10, 1, rgb("3e3a46"))
        c.rect(x + 17, 74, 3, BH - 74, rgb("3e3a46"))
    c.rect(0, 108, BW, 12, rgb("34303c"))
    c.save("Backgrounds/factory_mid")

    # ---- torre / por do sol
    c = Canvas(BW, BH)
    gradient(c, rgb("3a2860"), rgb("f08c6e"))
    for _ in range(30):
        x, y = r.randrange(BW), r.randrange(0, 40)
        c.set(x, y, rgb("fff0dc", r.randrange(120, 255)))
    for (cx, cy, rx) in ((40, 56, 20), (110, 70, 26), (150, 50, 12)):
        wrap_ellipse(c, cx, cy, rx, rx * 0.22, rgb("ffb4a0", 170))
    c.save("Backgrounds/tower_far")
    c = Canvas(BW, BH)
    x = 0
    while x < BW:
        w = r.randrange(10, 20)
        h = r.randrange(20, 55)
        if x + w > BW:
            w = BW - x
        c.rect(x, BH - h, w, h, rgb("2e2044"))
        for wy in range(BH - h + 3, BH - 2, 5):
            for wx in range(x + 2, x + w - 2, 4):
                if r.random() < 0.35:
                    c.rect(wx, wy, 2, 2, rgb("ffd278", 220))
        x += w + r.randrange(0, 4)
    c.save("Backgrounds/tower_mid")


# ================================================================ PREVIEW
def contact_sheet(path, scale=4, cols=10):
    items = [(n, cv) for (n, cv) in SAVED if not n.startswith("Backgrounds/")]
    cell = 34
    rows = (len(items) + cols - 1) // cols
    sheet = Canvas(cols * cell, rows * cell)
    for i, (_, cv) in enumerate(items):
        ox, oy = (i % cols) * cell + 1, (i // cols) * cell + 1
        for y in range(cell - 2):
            for x in range(cell - 2):
                sheet.set(ox + x, oy + y, rgb("c8c8d2") if ((x // 4 + y // 4) % 2) else rgb("e6e6ee"))
        sx = max(1, min(2, 32 // max(cv.w, cv.h)))
        for y in range(cv.h):
            for x in range(cv.w):
                p = cv.px[y][x]
                if p is not None and p[3] > 0:
                    for yy in range(sx):
                        for xx in range(sx):
                            sheet.set(ox + x * sx + xx, oy + y * sx + yy, p[:3] + (255,))
    big = Canvas(sheet.w * scale, sheet.h * scale)
    for y in range(big.h):
        for x in range(big.w):
            big.px[y][x] = sheet.px[y // scale][x // scale]
    write_png(path, big)


def main():
    make_player()
    make_slime()
    make_drone()
    make_hedgehog()
    make_items()
    make_checkpoint()
    make_goal()
    make_level_objects()
    make_tiles()
    make_decor()
    make_ui()
    make_backgrounds()
    print(f"{len(SAVED)} sprites gerados em {OUT}")
    if len(sys.argv) > 1:
        os.makedirs(sys.argv[1], exist_ok=True)
        contact_sheet(os.path.join(sys.argv[1], "sprites_preview.png"))
        print("preview salvo em", sys.argv[1])


if __name__ == "__main__":
    main()
