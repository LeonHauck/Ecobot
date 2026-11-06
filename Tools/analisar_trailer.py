"""
Resume o registro do piloto automático (Trailer/posicoes.csv):
para cada comando de cada cena mostra posição inicial/final, duração,
e alerta quedas de energia, mortes e comandos que estouraram o tempo.

Uso: python Tools/analisar_trailer.py [Trailer/posicoes.csv]
"""
import csv
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Trailer", "posicoes.csv")

rows = list(csv.DictReader(open(path, encoding="utf-8")))
groups = []
for r in rows:
    key = (r["cena"], r["comando"])
    if not groups or groups[-1][0] != key:
        groups.append((key, []))
    groups[-1][1].append(r)

last_scene = None
for (scene, cmd), rs in groups:
    if scene != last_scene:
        print(f"\n== {scene}")
        last_scene = scene
    a, b = rs[0], rs[-1]
    secs = len(rs) / 60.0
    if not a["x"]:
        print(f"  {cmd:<14} {secs:5.2f}s")
        continue
    energy = [int(r["energia"]) for r in rs]
    dead = any(r["morto"] == "1" for r in rs)
    miny = min(float(r["y"]) for r in rs)
    flags = []
    if min(energy) < energy[0]:
        flags.append(f"DANO {energy[0]}->{min(energy)}")
    if dead:
        flags.append("MORREU")
    if secs >= 14.9:
        flags.append("ESTOUROU O TEMPO")
    print(f"  {cmd:<14} {secs:5.2f}s  ({float(a['x']):6.2f},{float(a['y']):6.2f}) -> "
          f"({float(b['x']):6.2f},{float(b['y']):6.2f})  ymin={miny:6.2f} vidas={b['vidas']} "
          + " ".join(flags))
