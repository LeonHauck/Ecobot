"""
Monta o trailer do EcoBot a partir da gravação do piloto automático.

Entrada (pasta Gravacao/):  gravacao.mp4 (1080p60)  +  eventos.csv (sons e músicas por quadro)
Saída   (pasta Trailer/):   EcoBot_Trailer_1080p.mp4, EcoBot_Trailer_720p.mp4
                            e Docs/Imagens/trailer.gif (versão curta para o README)

Uso: python Tools/make_trailer.py
Requer FFmpeg (winget install Gyan.FFmpeg).
"""
import array
import csv
import glob
import math
import os
import shutil
import subprocess
import wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REC = os.path.join(ROOT, "Gravacao")
OUT = os.path.join(ROOT, "Trailer")
TMP = os.path.join(OUT, "tmp")
AUDIO = os.path.join(ROOT, "Assets", "Audio")
SR = 44100
FPS = 60

# ------------------------------------------------------------------ cortes
# (início, fim) em segundos da gravação, grupo (define a música) e legenda opcional.
SEGMENTS = [
    (4.5, 9.5, "level1_forest", None),
    (11.0, 15.5, "level1_forest", "Corra, pule e escale"),
    (25.6, 27.6, "level1_forest", None),
    (30.0, 33.0, "level2_cave", None),
    (36.8, 41.6, "level2_cave", "Plataformas que desabam"),
    (43.5, 48.5, "level2_cave", "Cipós sobre o abismo"),
    (49.0, 51.5, "level3_factory", None),
    (52.5, 58.5, "level3_factory", "Lodo tóxico e plataformas móveis"),
    (62.5, 66.0, "level3_factory", None),
    (71.5, 76.0, "level3_factory", "Plataformas verticais"),
    (78.0, 81.0, "level4_tower", None),
    (83.5, 88.0, "level4_tower", "Suba a Chaminé Final"),
    (94.6, 98.6, "level4_tower", None),
    (103.6, 107.0, "level4_tower", None),
    (109.2, 112.5, "victory", None),
]
INTRO = 2.6        # menu congelado no início
OUTRO = 3.6        # cartela final
INTRO_CAPTION = "Um robô reciclador. Uma floresta para salvar."
OUTRO_LINES = ["EcoBot: Raízes do Amanhã", "Jogo de plataforma 2D  •  Unity 6  •  C#", "github.com/LeonHauck/Ecobot"]

# Trechos curtos para o GIF do README (início, fim)
GIF_SEGMENTS = [(11.2, 14.2), (44.0, 47.0), (53.0, 56.0), (95.0, 98.0)]


def find_ffmpeg():
    exe = shutil.which("ffmpeg")
    if exe:
        return exe
    hits = glob.glob(os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft", "WinGet", "Packages", "*FFmpeg*", "**", "ffmpeg.exe"), recursive=True)
    if not hits:
        raise SystemExit("FFmpeg não encontrado. Instale com: winget install Gyan.FFmpeg")
    return hits[0]


FFMPEG = find_ffmpeg()


def run(args):
    print(">", " ".join(a if len(a) < 80 else a[:77] + "..." for a in args[:12]), "...")
    subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y"] + args, check=True, cwd=ROOT)


# ------------------------------------------------------------------ áudio
_cache = {}


def load_clip(name):
    """Carrega um .wav do jogo (22050 Hz mono) como lista de floats."""
    if name in _cache:
        return _cache[name]
    for sub in ("SFX", "Music"):
        path = os.path.join(AUDIO, sub, name + ".wav")
        if os.path.exists(path):
            with wave.open(path) as w:
                rate = w.getframerate()
                data = array.array("h", w.readframes(w.getnframes()))
            _cache[name] = ([s / 32768.0 for s in data], rate)
            return _cache[name]
    raise FileNotFoundError(name)


def mix(buf, start_sample, clip_name, gain, pitch=1.0, loop=False, length=None, fade_in=0.0, fade_out=0.0, offset=0.0):
    data, rate = load_clip(clip_name)
    step = pitch * rate / SR
    n = length if length is not None else int(len(data) / step)
    pos = offset * rate
    fi, fo = int(fade_in * SR), int(fade_out * SR)
    for i in range(n):
        j = start_sample + i
        if j >= len(buf):
            break
        p = pos + i * step
        if loop:
            p %= len(data)
        elif p >= len(data) - 1:
            break
        k = int(p)
        v = data[k] + (data[(k + 1) % len(data)] - data[k]) * (p - k)
        g = gain
        if fi and i < fi:
            g *= i / fi
        if fo and i > n - fo:
            g *= max(0.0, (n - i) / fo)
        buf[j] += v * g


def build_audio(total, timeline):
    buf = [0.0] * int(total * SR)
    events = [r for r in csv.DictReader(open(os.path.join(REC, "eventos.csv"), encoding="utf-8")) if r["tipo"] == "sfx"]

    # música do menu sob a abertura
    mix(buf, 0, "menu_theme", 0.5, loop=True, length=int((INTRO + 0.4) * SR), fade_in=0.3, fade_out=0.5)

    # música contínua por grupo (fase) + efeitos sonoros nos quadros certos
    groups = []
    for (t_out, (a, b, group, _)) in timeline:
        if not groups or groups[-1][0] != group:
            groups.append([group, t_out, 0.0])
        groups[-1][2] += b - a
    for i, (group, start, dur) in enumerate(groups):
        extra = OUTRO if i == len(groups) - 1 else 0.25
        mix(buf, int(start * SR), group, 0.55, loop=True, length=int((dur + extra) * SR),
            fade_in=0.15, fade_out=1.5 if extra == OUTRO else 0.3)

    for (t_out, (a, b, _, _)) in timeline:
        for e in events:
            t = int(e["frame"]) / FPS
            if a <= t < b:
                mix(buf, int((t_out + t - a) * SR), e["valor"], float(e["volume"]) * 1.1, pitch=float(e["pitch"]))

    peak = max(1e-6, max(abs(x) for x in buf))
    g = 0.89 / peak if peak > 0.89 else 1.0
    path = os.path.join(TMP, "audio.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(array.array("h", (int(max(-1.0, min(1.0, x * g)) * 32767) for x in buf)).tobytes())
    return path


# ------------------------------------------------------------------ vídeo
def caption_filter(name, text, duration, y="h-190", size=56):
    path = os.path.join(TMP, name + ".txt")
    with open(path, "w", encoding="utf-8") as f:
        f.write(text)
    fade = 0.35
    return (f"drawtext=fontfile=Trailer/tmp/font.ttf:textfile=Trailer/tmp/{name}.txt:fontsize={size}:"
            f"fontcolor=white:borderw=5:bordercolor=0x12303a:box=1:boxcolor=0x12303a@0.55:boxborderw=22:"
            f"x=90:y={y}:alpha='if(lt(t,{fade}),t/{fade},if(gt(t,{duration - fade}),({duration}-t)/{fade},1))'")


def build_trailer():
    os.makedirs(TMP, exist_ok=True)
    shutil.copy(r"C:\Windows\Fonts\arialbd.ttf", os.path.join(TMP, "font.ttf"))
    eco = os.path.join(ROOT, "Docs", "Imagens", "eco.png")

    chains, labels = [], []
    # abertura: primeiro quadro do menu congelado
    chains.append(f"[0:v]trim=start_frame=2:end_frame=3,loop=loop={int(INTRO * FPS) - 1}:size=1:start=0,"
                  f"setpts=N/{FPS}/TB,fade=in:st=0:d=0.5,{caption_filter('intro', INTRO_CAPTION, INTRO)}[v0]")
    labels.append("[v0]")

    timeline, t = [], INTRO
    prev_group = None
    for i, (a, b, group, caption) in enumerate(SEGMENTS, start=1):
        d = b - a
        f = [f"[0:v]trim=start={a}:end={b},setpts=PTS-STARTPTS"]
        next_group = SEGMENTS[i][2] if i < len(SEGMENTS) else None
        if group != prev_group:
            f.append("fade=in:st=0:d=0.2")
        if group != next_group:
            f.append(f"fade=out:st={d - 0.2:.3f}:d=0.2")
        if caption:
            f.append(caption_filter(f"c{i}", caption, d))
        chains.append(",".join(f) + f"[v{i}]")
        labels.append(f"[v{i}]")
        timeline.append((t, (a, b, group, caption)))
        t += d
        prev_group = group

    # cartela final com o Eco
    n = len(SEGMENTS) + 1
    lines = []
    for k, (txt, size, y) in enumerate(zip(OUTRO_LINES, (84, 40, 44), (610, 720, 800))):
        with open(os.path.join(TMP, f"o{k}.txt"), "w", encoding="utf-8") as fh:
            fh.write(txt)
        color = "0x94e68c" if k == 0 else ("white" if k == 1 else "0xffe16e")
        lines.append(f"drawtext=fontfile=Trailer/tmp/font.ttf:textfile=Trailer/tmp/o{k}.txt:fontsize={size}:"
                     f"fontcolor={color}:borderw=4:bordercolor=black:x=(w-text_w)/2:y={y}")
    chains.append(f"color=c=0x12303a:s=1920x1080:r={FPS}:d={OUTRO}[bg]")
    chains.append("[1:v]scale=320:320:flags=neighbor[eco]")
    chains.append(f"[bg][eco]overlay=(W-w)/2:220:shortest=1,{','.join(lines)},fade=in:st=0:d=0.4,fade=out:st={OUTRO - 0.6}:d=0.6[v{n}]")
    labels.append(f"[v{n}]")
    total = t + OUTRO

    chains.append(f"{''.join(labels)}concat=n={len(labels)}:v=1:a=0,format=yuv420p[vout]")
    graph = os.path.join(TMP, "graph.txt")
    with open(graph, "w", encoding="utf-8") as fh:
        fh.write(";\n".join(chains))

    print(f"Duração do trailer: {total:.1f} s")
    print("Reconstruindo o áudio...")
    audio = build_audio(total, timeline)

    master = os.path.join(OUT, "EcoBot_Trailer_1080p.mp4")
    print("Renderizando 1080p...")
    run(["-i", os.path.join(REC, "gravacao.mp4"), "-loop", "1", "-i", eco, "-i", audio,
         "-/filter_complex", graph, "-map", "[vout]", "-map", "2:a",
         "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-r", str(FPS),
         "-c:a", "aac", "-b:a", "192k", "-t", f"{total:.3f}", "-movflags", "+faststart", master])

    print("Renderizando versão leve (720p)...")
    run(["-i", master, "-vf", "scale=1280:720:flags=lanczos,fps=30", "-c:v", "libx264", "-preset", "slow",
         "-crf", "27", "-c:a", "aac", "-b:a", "96k", "-movflags", "+faststart",
         os.path.join(OUT, "EcoBot_Trailer_720p.mp4")])
    return master


def build_gif():
    print("Renderizando GIF do README...")
    parts = "".join(f"[0:v]trim=start={a}:end={b},setpts=PTS-STARTPTS[g{i}];" for i, (a, b) in enumerate(GIF_SEGMENTS))
    joined = "".join(f"[g{i}]" for i in range(len(GIF_SEGMENTS)))
    graph = (parts + f"{joined}concat=n={len(GIF_SEGMENTS)}:v=1:a=0,fps=15,scale=640:-1:flags=lanczos,split[a][b];"
             "[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle")
    gif = os.path.join(ROOT, "Docs", "Imagens", "trailer.gif")
    run(["-i", os.path.join(REC, "gravacao.mp4"), "-filter_complex", graph, "-loop", "0", gif])
    return gif


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    build_trailer()
    gif = build_gif()
    for f in (os.path.join(OUT, "EcoBot_Trailer_1080p.mp4"), os.path.join(OUT, "EcoBot_Trailer_720p.mp4"), gif):
        print(f"{os.path.relpath(f, ROOT)}: {os.path.getsize(f) / 1e6:.1f} MB")
