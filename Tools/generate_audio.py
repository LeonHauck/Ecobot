"""
Gerador procedural de audio do EcoBot (efeitos sonoros + trilhas chiptune).

Todo o audio do jogo e sintetizado por este script (100% original).
Uso:  python Tools/generate_audio.py
Saida: Assets/Audio/SFX/*.wav e Assets/Audio/Music/*.wav  (22050 Hz, mono, 16 bits)

Sem dependencias externas: usa apenas math, random, struct e wave.
"""
import math
import os
import random
import struct
import wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Audio")
SR = 22050
rng = random.Random(42)

NOTE_INDEX = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5,
              "F#": 6, "Gb": 6, "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def midi(name):
    """'C5' -> 72"""
    pitch, octave = name[:-1], int(name[-1])
    return 12 * (octave + 1) + NOTE_INDEX[pitch]


def freq(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


# ------------------------------------------------------------------ osciladores
def osc(kind, phase, duty=0.5):
    p = phase % 1.0
    if kind == "square":
        return 1.0 if p < duty else -1.0
    if kind == "triangle":
        return 4.0 * abs(p - 0.5) - 1.0
    if kind == "saw":
        return 2.0 * p - 1.0
    if kind == "sine":
        return math.sin(2 * math.pi * p)
    if kind == "noise":
        return rng.uniform(-1, 1)
    raise ValueError(kind)


def render_tone(buf, start, dur, f0, f1=None, kind="square", vol=0.3, duty=0.5,
                attack=0.005, release=0.04, vibrato=0.0, decay=None):
    """Escreve uma nota em buf (lista de floats) com envelope simples."""
    n = int(dur * SR)
    s0 = int(start * SR)
    f1 = f0 if f1 is None else f1
    phase = 0.0
    a_n = max(1, int(attack * SR))
    r_n = max(1, int(release * SR))
    noise_hold, noise_val = 0, 0.0
    for i in range(n):
        idx = s0 + i
        if idx >= len(buf):
            break
        t = i / max(1, n - 1)
        f = f0 + (f1 - f0) * t
        if vibrato:
            f *= 1.0 + vibrato * math.sin(2 * math.pi * 5.5 * i / SR)
        if kind == "noise":
            # ruido "chiptune": segura o valor por alguns samples (mais grave)
            if noise_hold <= 0:
                noise_val = rng.uniform(-1, 1)
                noise_hold = max(1, int(SR / max(f, 1)))
            noise_hold -= 1
            v = noise_val
        else:
            phase += f / SR
            v = osc(kind, phase, duty)
        env = 1.0
        if i < a_n:
            env = i / a_n
        elif i > n - r_n:
            env = max(0.0, (n - i) / r_n)
        if decay:
            env *= math.exp(-decay * i / SR)
        buf[idx] += v * vol * env


def save_wav(rel, buf, peak=0.85):
    path = os.path.join(OUT, rel + ".wav")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    m = max(1e-6, max(abs(x) for x in buf))
    g = peak / m if m > peak else 1.0
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x * g)) * 32767)) for x in buf))
    print("  ", rel, f"{len(buf) / SR:.1f}s")


def silence(sec):
    return [0.0] * int(sec * SR)


# ================================================================== EFEITOS
def make_sfx():
    s = "SFX/"
    b = silence(0.22)
    render_tone(b, 0, 0.2, 280, 720, "square", 0.35, duty=0.25, release=0.08)
    save_wav(s + "jump", b)

    b = silence(0.1)
    render_tone(b, 0, 0.08, 900, 200, "noise", 0.35, release=0.05)
    save_wav(s + "land", b)

    b = silence(0.25)
    render_tone(b, 0, 0.06, freq(83), None, "square", 0.3, duty=0.5, release=0.01)
    render_tone(b, 0.06, 0.17, freq(88), None, "square", 0.3, duty=0.5, release=0.1)
    save_wav(s + "collect", b)

    b = silence(0.6)
    for i, n in enumerate(["C5", "E5", "G5", "C6", "E6", "G6"]):
        render_tone(b, i * 0.07, 0.12 if i < 5 else 0.22, freq(midi(n)), None, "square", 0.25,
                    duty=0.25, release=0.06)
    save_wav(s + "extra_life", b)

    b = silence(0.35)
    for i, n in enumerate(["A5", "C#6", "E6"]):
        render_tone(b, i * 0.06, 0.12, freq(midi(n)), None, "triangle", 0.45, release=0.05)
    render_tone(b, 0.18, 0.15, freq(midi("A6")), None, "square", 0.15, duty=0.125, release=0.1)
    save_wav(s + "energy", b)

    b = silence(0.4)
    render_tone(b, 0, 0.35, 620, 140, "square", 0.3, duty=0.5, release=0.12)
    render_tone(b, 0, 0.18, 2000, 400, "noise", 0.2, release=0.1)
    save_wav(s + "hurt", b)

    b = silence(0.2)
    render_tone(b, 0, 0.16, 420, 70, "triangle", 0.6, release=0.06)
    render_tone(b, 0, 0.06, 3000, 800, "noise", 0.25, release=0.04)
    save_wav(s + "stomp", b)

    b = silence(0.6)
    for i, n in enumerate(["G5", "C6", "E6", "G6"]):
        render_tone(b, i * 0.08, 0.3 if i == 3 else 0.1, freq(midi(n)), None, "triangle", 0.5,
                    release=0.08, vibrato=0.01 if i == 3 else 0)
    save_wav(s + "checkpoint", b)

    b = silence(0.5)
    for i in range(10):
        render_tone(b, i * 0.035, 0.05, 1500 - i * 80, 500, "noise", 0.25 - i * 0.015, release=0.03)
    save_wav(s + "crumble", b)

    b = silence(1.6)
    seq = [("C5", 0.1), ("E5", 0.1), ("G5", 0.1), ("C6", 0.25), ("G5", 0.1), ("C6", 0.6)]
    t = 0.0
    for n, d in seq:
        render_tone(b, t, d, freq(midi(n)), None, "square", 0.25, duty=0.25, release=0.05,
                    vibrato=0.008 if d > 0.5 else 0)
        render_tone(b, t, d, freq(midi(n) - 12), None, "triangle", 0.3, release=0.05)
        t += d
    save_wav(s + "level_complete", b)

    b = silence(1.8)
    t = 0.0
    for n, d in (("G4", 0.25), ("E4", 0.25), ("C4", 0.25), ("C3", 0.9)):
        render_tone(b, t, d, freq(midi(n)), None, "square", 0.3, duty=0.5, release=0.1,
                    vibrato=0.02 if d > 0.5 else 0)
        t += d
    save_wav(s + "game_over", b)

    b = silence(0.06)
    render_tone(b, 0, 0.05, 880, None, "square", 0.25, duty=0.25, release=0.02)
    save_wav(s + "ui_move", b)

    b = silence(0.15)
    render_tone(b, 0, 0.05, 660, None, "square", 0.25, duty=0.25, release=0.01)
    render_tone(b, 0.05, 0.09, 990, None, "square", 0.25, duty=0.25, release=0.05)
    save_wav(s + "ui_select", b)

    b = silence(0.25)
    render_tone(b, 0, 0.08, 520, None, "triangle", 0.5, release=0.02)
    render_tone(b, 0.12, 0.1, 390, None, "triangle", 0.5, release=0.05)
    save_wav(s + "pause", b)

    b = silence(0.35)
    render_tone(b, 0, 0.3, 300, 900, "sine", 0.4, release=0.1, vibrato=0.05)
    render_tone(b, 0, 0.3, 4000, 2000, "noise", 0.08, release=0.1)
    save_wav(s + "enemy_hit", b)


# ================================================================== MUSICA
CHORD_QUALITY = {"": (0, 4, 7), "m": (0, 3, 7)}


def parse_chord(ch):
    if len(ch) > 1 and ch[1] in "#b":
        root, q = ch[:2], ch[2:]
    else:
        root, q = ch[:1], ch[1:]
    base = 48 + NOTE_INDEX[root]          # C3
    return base, CHORD_QUALITY[q]


def song(name, bpm, chords, melody, bass_pattern, drum_pattern, lead="square",
         lead_duty=0.25, arp=True, swing_bass="triangle", passes=2):
    eighth = 60.0 / bpm / 2
    bar = eighth * 8
    total = bar * len(chords) * passes
    buf = silence(total + 0.01)

    for p in range(passes):
        off = p * bar * len(chords)
        for bi, ch in enumerate(chords):
            t0 = off + bi * bar
            root, intervals = parse_chord(ch)
            # ---- melodia
            tokens = melody[bi].split()
            i = 0
            while i < len(tokens):
                tok = tokens[i]
                if tok not in ("-", "."):
                    n = 1
                    while i + n < len(tokens) and tokens[i + n] == "-":
                        n += 1
                    render_tone(buf, t0 + i * eighth, n * eighth * 0.95, freq(midi(tok)), None,
                                lead, 0.16, duty=lead_duty, release=0.05,
                                vibrato=0.006 if n >= 3 else 0)
                    i += n
                else:
                    i += 1
            # ---- baixo
            for k, tok in enumerate(bass_pattern):
                if tok in ".-":
                    continue
                n = 1
                while k + n < len(bass_pattern) and bass_pattern[k + n] == "-":
                    n += 1
                m = root - 12 if tok == "R" else root - 12 + intervals[2] if tok == "F" else root
                render_tone(buf, t0 + k * eighth, n * eighth * 0.9, freq(m), None, swing_bass,
                            0.3, duty=0.5, release=0.03)
            # ---- arpejo (so na segunda passada, para dar variacao)
            if arp and p % 2 == 1:
                tones = [root + 12 + iv for iv in intervals] + [root + 24]
                for k in range(16):
                    render_tone(buf, t0 + k * eighth / 2, eighth / 2 * 0.8,
                                freq(tones[k % 4]), None, "square", 0.05, duty=0.125, release=0.02)
            # ---- bateria
            step = eighth / 2
            for k, tok in enumerate(drum_pattern):
                ts = t0 + k * step
                if tok == "k":
                    render_tone(buf, ts, 0.12, 160, 45, "sine", 0.5, release=0.05)
                elif tok == "s":
                    render_tone(buf, ts, 0.1, 6000, 3000, "noise", 0.16, release=0.06)
                    render_tone(buf, ts, 0.06, 240, 180, "triangle", 0.18, release=0.03)
                elif tok == "h":
                    render_tone(buf, ts, 0.03, 9000, None, "noise", 0.05, release=0.02)
    save_wav("Music/" + name, buf, peak=0.7)


def make_music():
    song("menu_theme", 116,
         ["C", "Am", "F", "G", "C", "Am", "Dm", "G"],
         ["E5 - G5 - C6 - G5 -",
          "A5 - G5 E5 - - C5 -",
          "F5 - A5 - C6 - A5 G5",
          "G5 - - - D5 - . .",
          "E5 G5 C6 - B5 - G5 -",
          "A5 - E5 - A5 - C6 -",
          "D6 - C6 A5 F5 - D5 -",
          "G5 - B5 - D6 - . ."],
         "R . F . R . F .", "k...h...s...h...")

    song("level1_forest", 132,
         ["G", "Em", "C", "D", "G", "Em", "C", "D"],
         ["G5 . B5 . D6 - B5 .",
          "E5 - G5 - B5 - G5 E5",
          "C6 - B5 - A5 - G5 -",
          "A5 - . F#5 A5 - D6 -",
          "B5 . D6 . G6 - D6 .",
          "E6 - D6 B5 G5 - E5 -",
          "C6 . E6 . D6 C6 B5 A5",
          "D6 - - - . . A5 ."],
         "R . F R . R F .", "k.h.s.h.k.k.s.h.")

    song("level2_cave", 100,
         ["Am", "F", "G", "Em", "Am", "F", "Dm", "E"],
         ["A4 - - - C5 - E5 -",
          "F5 - - - E5 - C5 -",
          "D5 - - - B4 - G4 -",
          "E5 - - - - - . .",
          "A5 - G5 - E5 - C5 -",
          "F5 - E5 - C5 - A4 -",
          "D5 - F5 - A5 - F5 -",
          "E5 - - - G#4 - B4 -"],
         "R - - - F - - -", "k.......s.....h.", lead="triangle")

    song("level3_factory", 140,
         ["Em", "Em", "C", "D", "Em", "Em", "C", "B"],
         ["E5 . E5 G5 . E5 B5 .",
          "A5 . G5 . F#5 E5 D5 .",
          "E5 . E5 G5 . E5 C6 .",
          "B5 . A5 . F#5 . D5 .",
          "G5 G5 . B5 . G5 E6 .",
          "D6 . B5 . A5 G5 F#5 .",
          "G5 . A5 . B5 . C6 .",
          "B5 - - - D#6 - F#6 -"],
         "R R O R R R O R", "k.h.s.hkk.h.s.hh", lead_duty=0.5, swing_bass="square")

    song("level4_tower", 150,
         ["Dm", "Bb", "C", "A", "Dm", "Bb", "Gm", "A"],
         ["D5 F5 A5 D6 A5 F5 D5 F5",
          "Bb5 - A5 - F5 - D5 -",
          "C5 E5 G5 C6 G5 E5 C5 E5",
          "C#6 - A5 - E5 - C#5 -",
          "D6 - - A5 F5 - D5 -",
          "F5 - Bb5 - D6 - F6 -",
          "G5 - Bb5 - D6 - G6 -",
          "A5 - C#6 - E6 - A6 -"],
         "R O R O R O R O", "kkh.s.h.kkh.s.ss")

    song("victory", 112,
         ["C", "F", "G", "C", "Am", "F", "G", "C"],
         ["C5 E5 G5 C6 - - G5 -",
          "A5 - C6 - F6 - - -",
          "G5 B5 D6 G6 - - F6 -",
          "E6 - - - C6 - . .",
          "E6 - C6 - A5 - C6 -",
          "F6 - E6 - C6 - A5 -",
          "D6 - B5 - G5 - B5 -",
          "C6 - - - - - . ."],
         "R . F . R . F .", "k...h...s...h.h.")


if __name__ == "__main__":
    print("Gerando efeitos sonoros...")
    make_sfx()
    print("Gerando musicas...")
    make_music()
    print("Pronto:", OUT)
