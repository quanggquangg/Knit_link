"""Knit Link – renders every game sound + the background music into WAV files (27/10).

Output: Assets/KnitLink/Resources/Sounds/<name>.wav  (the game plays a file there instead of its code-made sound, see Game/Sfx.cs)
Run:    python render_sounds.py <output folder>        (needs numpy + scipy)

Sound palette ("cozy knitting"): a soft marimba / wooden pluck for notes, felt drum for hits, filtered paper noise for
whooshes, a small room reverb on everything, all in C major pentatonic so sounds never clash with the music.
Every sound is mixed to the same loudness family:
  small taps/landings  peak -12 dB      matches / boosters  peak -8 dB      win / warnings  peak -5 dB      music  peak -9 dB
To change a sound: edit its function below and run the script again; to go back to the code-made sound, delete its file.
"""
import sys, os
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

SR = 44100
rng = np.random.default_rng(7)


# ---------------------------------------------------------------- building blocks
def t_axis(d): return np.arange(int(d * SR)) / SR

def env(d, a=.004, decay=6.0):
    """fast attack, exponential decay (decay = how fast it dies, per second)"""
    t = t_axis(d)
    e = np.exp(-t * decay)
    na = max(1, int(a * SR)); e[:na] *= np.linspace(0, 1, na)
    return e

def marimba(f, d=.6, vol=1.0, decay=7.0, bright=1.0):
    """wooden bar: fundamental + the marimba's tuned overtones (4x, ~10x) that die faster"""
    t = t_axis(d)
    s = np.sin(2 * np.pi * f * t) * env(d, .003, decay)
    s += .35 * bright * np.sin(2 * np.pi * f * 3.99 * t) * env(d, .002, decay * 3.2)
    s += .12 * bright * np.sin(2 * np.pi * f * 10.7 * t) * env(d, .001, decay * 7)
    return vol * s

def pluck(f, d=.5, vol=1.0, damp=.996):
    """Karplus-Strong string: a soft yarn/wool 'pluck'"""
    n = int(d * SR); p = max(2, int(SR / f))
    buf = rng.uniform(-1, 1, p); buf = sosfilt(butter(1, 3000, fs=SR, output='sos'), buf)
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % p]
        buf[i % p] = damp * .5 * (buf[i % p] + buf[(i + 1) % p])
    return vol * out * env(d, .002, 2.5)

def bell(f, d=1.0, vol=1.0, decay=3.5):
    """small glassy bell for sparkles"""
    t = t_axis(d)
    s = np.sin(2 * np.pi * f * t) + .5 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 4) + .25 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t * 8)
    return vol * s * env(d, .002, decay)

def noise(d, lo=200, hi=4000, vol=1.0, a=.01, decay=8.0):
    s = rng.uniform(-1, 1, int(d * SR))
    s = sosfilt(butter(2, [lo, hi], btype='band', fs=SR, output='sos'), s)
    return vol * s * env(d, a, decay)

def whoosh(d=.35, lo=300, hi=3500, vol=1.0, up=True):
    """filtered noise that swells and moves in pitch (cloth / paper moving)"""
    n = int(d * SR); s = rng.uniform(-1, 1, n)
    out = np.zeros(n); seg = 512
    for i in range(0, n, seg):
        k = i / n; c = (lo + (hi - lo) * (k if up else 1 - k))
        sos = butter(2, [max(60, c * .6), min(SR / 2 - 100, c * 1.4)], btype='band', fs=SR, output='sos')
        out[i:i + seg] = sosfilt(sos, s[i:i + seg])
    shape = np.sin(np.pi * np.linspace(0, 1, n)) ** 1.5
    return vol * out * shape

def felt_drum(f=90, d=.4, vol=1.0, drop=.5):
    """soft low drum: sine with a quick pitch drop + a little felt noise"""
    t = t_axis(d); fr = f * (1 + drop * np.exp(-t * 30))
    s = np.sin(2 * np.pi * np.cumsum(fr) / SR) * env(d, .002, 9)
    return vol * (s + .15 * noise(d, 100, 1200, 1, .001, 40))

def glide(f0, f1, d, vol=1.0, wave='sine'):
    t = t_axis(d); fr = f0 * (f1 / f0) ** (t / d); ph = 2 * np.pi * np.cumsum(fr) / SR
    s = np.sin(ph) if wave == 'sine' else 2 / np.pi * np.arcsin(np.sin(ph))
    return vol * s * env(d, .01, 3 / d)

def place(dst, src, at):
    i = int(at * SR); end = min(len(dst), i + len(src)); dst[i:end] += src[:end - i]; return dst

def track(d): return np.zeros(int(d * SR))

def reverb(x, wet=.18, size=.9, damp=5000):
    """small warm room: convolution with decaying filtered noise"""
    n = int(size * SR); ir = rng.uniform(-1, 1, n) * np.exp(-np.arange(n) / SR * (6.9 / size))
    ir = sosfilt(butter(1, damp, fs=SR, output='sos'), ir); ir /= np.sqrt(np.sum(ir ** 2))
    tail = np.concatenate([fftconvolve(x, ir), [0.0]])[:len(x) + n]
    out = np.concatenate([x, np.zeros(n)]) * (1 - wet) + tail * wet
    return out

def finish(x, peak_db, rev=.18, tail_trim=True):
    x = reverb(x, rev) if rev > 0 else x
    x = sosfilt(butter(1, 40, btype='high', fs=SR, output='sos'), x)        # no rumble
    if tail_trim:
        thr = np.max(np.abs(x)) * 10 ** (-60 / 20); idx = np.nonzero(np.abs(x) > thr)[0]
        x = x[:idx[-1] + 1] if len(idx) else x
        f = min(len(x), int(.03 * SR)); x[-f:] *= np.linspace(1, 0, f)
    return x / np.max(np.abs(x)) * 10 ** (peak_db / 20)

# notes (C major pentatonic + a few)
N = {n: 440 * 2 ** ((m - 69) / 12) for n, m in
     dict(C3=48, D3=50, E3=52, G3=55, A3=57, C4=60, D4=62, E4=64, F4=65, G4=67, A4=69, B4=71, C5=72, D5=74, E5=76,
          F5=77, G5=79, A5=81, B5=83, C6=84, D6=86, E6=88, G6=91, A6=93, C7=96, Bb4=70, Eb4=63, Ab4=68, Eb5=75, Bb3=58, B3=59).items()}


# ---------------------------------------------------------------- the sounds
def s_tap():        # pick a yarn ball: one short high wooden tick
    x = track(.25); place(x, marimba(N['G5'], .25, 1, 22), 0); return finish(x, -12, .10)

def s_link():       # a pair matched: two rising notes, a little "yes"
    x = track(.6); place(x, marimba(N['E5'], .4, 1, 9), 0); place(x, marimba(N['A5'], .5, 1, 8), .07)
    place(x, whoosh(.18, 1500, 5000, .12), 0); return finish(x, -8, .18)

def s_clink():      # ball lands in the basket: soft wool "thup" + small wooden knock
    x = track(.3); place(x, felt_drum(180, .2, .7, .3), 0); place(x, marimba(N['C5'], .25, .55, 16), .005)
    return finish(x, -12, .12)

def s_shelf():      # ball lands on the wooden shelf: lower knock
    x = track(.3); place(x, marimba(N['G4'], .3, 1, 14, .7), 0); place(x, noise(.06, 300, 2500, .25, .001, 50), 0)
    return finish(x, -12, .12)

def s_err():        # blocked: two low muted "bonk"s going down
    x = track(.45); place(x, marimba(N['E4'], .2, 1, 18, .4), 0); place(x, marimba(N['C4'], .25, 1, 16, .4), .1)
    return finish(x, -10, .08)

def s_pour():       # a full basket starts knitting: yarn swish
    x = track(.6); place(x, whoosh(.5, 400, 3000, 1), 0)
    for i in range(4): place(x, noise(.05, 2000, 7000, .25, .001, 60), .08 + i * .09)
    return finish(x, -11, .2)

def s_done():       # basket finished knitting: warm three-note chord rising
    x = track(1.0)
    for i, n in enumerate(['C5', 'E5', 'G5']): place(x, marimba(N[n], .8, 1, 5), i * .07)
    place(x, bell(N['C6'], .8, .25), .21); return finish(x, -8, .25)

def s_boost():      # booster used: quick sparkle run up
    x = track(.8)
    for i, n in enumerate(['G4', 'C5', 'E5', 'G5', 'C6']): place(x, marimba(N[n], .3, .9, 10), i * .045)
    place(x, whoosh(.3, 800, 6000, .25), 0); place(x, bell(N['G6'], .5, .2), .22); return finish(x, -8, .25)

def s_unlock():     # something opens (booster, tangle, box): two bright bells
    x = track(1.0); place(x, bell(N['E6'], .7, 1), 0); place(x, bell(N['A6'], .8, 1), .09); return finish(x, -9, .3)

def s_win():        # level won: happy marimba arpeggio + bell + low C under it
    x = track(1.8)
    for i, n in enumerate(['C5', 'E5', 'G5', 'C6', 'E6']): place(x, marimba(N[n], .9, 1, 4.5), i * .085)
    place(x, marimba(N['C3'], 1.2, .8, 3, .3), 0); place(x, bell(N['C7'], 1.0, .25), .42); place(x, bell(N['G6'], 1.0, .2), .5)
    return finish(x, -5, .3)

def s_fail():       # level lost: slow sad notes down (minor)
    x = track(2.0)
    for i, n in enumerate(['G4', 'Eb4', 'C4']): place(x, marimba(N[n], 1.0, 1, 3.2, .6), i * .22)
    place(x, marimba(N['C3'], 1.4, .5, 2.5, .2), .44); return finish(x, -7, .3)

def s_bagopen():    # yarn bag pops up: soft rising pop + cloth
    x = track(.5); place(x, glide(220, 520, .22, 1), 0); place(x, whoosh(.3, 400, 2500, .35), 0)
    place(x, marimba(N['C5'], .3, .4, 10), .12); return finish(x, -10, .2)

def s_baglift():    # a ball lifts off the shelf towards the bag: tiny high pluck
    x = track(.2); place(x, marimba(N['D6'], .18, 1, 26, .5), 0); return finish(x, -14, .1)

def s_hard():       # HARD warning: whoosh in, two felt drum hits, falling "dun-dun"
    x = track(1.4); place(x, whoosh(.32, 300, 3000, .6), 0)
    place(x, felt_drum(85, .5, 1), .14); place(x, felt_drum(85, .5, .9), .42)
    place(x, marimba(N['G4'], .5, .8, 5, .6), .14); place(x, marimba(N['D4'], .7, .9, 4, .6), .42)
    return finish(x, -5, .22)

def s_superhard():  # SUPER HARD warning: stronger - three drum hits, three falling minor notes, a short alarm glide
    x = track(1.8); place(x, whoosh(.35, 300, 3500, .7), 0)
    for i in range(3): place(x, felt_drum(80, .5, 1), .14 + i * .2)
    for i, n in enumerate(['Bb4', 'G4', 'Eb4']): place(x, marimba(N[n], .6, .85, 4.5, .7), .14 + i * .2)
    place(x, glide(660, 990, .25, .12, 'tri'), .2); place(x, glide(660, 990, .25, .12, 'tri'), .6)
    return finish(x, -5, .22)

SOUNDS = dict(tap=s_tap, link=s_link, clink=s_clink, shelf=s_shelf, err=s_err, pour=s_pour, done=s_done, boost=s_boost,
              unlock=s_unlock, win=s_win, fail=s_fail, bagopen=s_bagopen, baglift=s_baglift, hard=s_hard, superhard=s_superhard)


# ---------------------------------------------------------------- background music
def s_music():
    """'Cozy Stitches' – 16 bars at 84 BPM in C major (I–vi–IV–V twice, the second time with a melody), loops seamlessly.
    Layers: soft pad, marimba arpeggio, round bass, a quiet shaker; melody on a wool pluck in the second half."""
    bpm = 84; beat = 60 / bpm; bars = 16; L = bars * 4 * beat
    left = track(L); right = track(L)
    chords = [('C3', ['C4', 'E4', 'G4']), ('A3', ['A3', 'C4', 'E4']), ('F4', ['F4', 'A4', 'C5']), ('G3', ['G3', 'B3', 'D4'])]
    def put(sig, at, pan=0.0, vol=1.0):
        place(left, sig * vol * (1 - max(0, pan)), at); place(right, sig * vol * (1 + min(0, pan)), at)
    for bar in range(bars):
        root, notes = chords[bar % 4]; t0 = bar * 4 * beat
        # pad: sustained chord, slow swell (sines + a soft fifth)
        d = 4 * beat; t = t_axis(d); sw = np.sin(np.pi * np.linspace(0, 1, len(t))) ** .8
        pad = sum(np.sin(2 * np.pi * N[n] * t) + .3 * np.sin(2 * np.pi * N[n] * 2.003 * t) for n in notes) * sw
        put(pad, t0, 0, .05)
        # bass: root on beat 1 and 3
        bn = N[root] / 2 if N[root] > 150 else N[root]
        for b in (0, 2): put(marimba(bn, 1.2, 1, 3, .15), t0 + b * beat, 0, .32)
        # arpeggio: eighth notes up and down
        fs = [N[n] for n in notes] + [N[notes[0]] * 2]          # chord tones + the root an octave up
        for k, si in enumerate([0, 1, 2, 3, 2, 1, 2, 1]):
            f = fs[si] * 2 if fs[si] < 400 else fs[si]
            put(marimba(f, .5, 1, 7, .5), t0 + k * beat / 2, .35 if k % 2 else -.35, .11)
        # shaker on the off-beats (very quiet)
        for k in range(4): put(noise(.08, 4000, 10000, 1, .002, 50), t0 + k * beat + beat / 2, .5, .03)
    # melody in bars 9–16 (pentatonic, simple and singable)
    mel = [('E5', 0, 1), ('G5', 1, 1), ('A5', 2, 1.5), ('G5', 3.5, .5), ('E5', 4, 2), ('D5', 6, 2),
           ('C5', 8, 1), ('D5', 9, 1), ('E5', 10, 1.5), ('G5', 11.5, .5), ('D5', 12, 3), ('E5', 16, 1), ('G5', 17, 1),
           ('A5', 18, 1.5), ('C6', 19.5, .5), ('A5', 20, 2), ('G5', 22, 2), ('E5', 24, 1), ('D5', 25, 1), ('E5', 26, 1),
           ('D5', 27, 1), ('C5', 28, 4)]
    for n, b, dur in mel:
        put(pluck(N[n], max(.4, dur * beat * 1.2), 1), 8 * 4 * beat + b * beat, -.1, .16)
    # reverb + seamless loop: the tail that rings past the end is folded back onto the start
    outs = []
    for ch in (left, right):
        r = reverb(ch, .28, 1.6, 4500); tail = r[len(ch):]; r = r[:len(ch)].copy(); r[:len(tail)] += tail
        r = sosfilt(butter(1, 40, btype='high', fs=SR, output='sos'), r); outs.append(r)
    st = np.stack(outs, 1); return st / np.max(np.abs(st)) * 10 ** (-9 / 20)


def write_wav(path, x):
    from scipy.io import wavfile
    x = np.clip(x, -1, 1); wavfile.write(path, SR, (x * 32767).astype(np.int16))


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else 'Sounds'
    os.makedirs(out, exist_ok=True)
    for name, fn in SOUNDS.items():
        x = fn(); write_wav(os.path.join(out, name + '.wav'), x); print(f'{name:10s} {len(x) / SR:.2f}s')
    m = s_music(); write_wav(os.path.join(out, 'music.wav'), m); print(f'music      {len(m) / SR:.1f}s (loop)')
