"""Original synthesized placeholder SFX and boss cue; numpy + Python, no samples.
Run from anywhere: python3 Tools/Audio/generate_audio.py
The three licensed/user-provided stage tracks are imported separately.
"""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio'
SR = 22050
RNG = np.random.default_rng(4701)

def save(path, signal):
    path.parent.mkdir(parents=True, exist_ok=True)
    signal = np.asarray(signal)
    signal = np.tanh(signal * 1.1) * 0.82
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1 if signal.ndim == 1 else signal.shape[1])
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((signal * 32767).astype('<i2').tobytes())

def effect(name, duration, start, end, noise=0, metal=0, pulse=0):
    t = np.arange(int(duration * SR)) / SR
    phase = 2 * np.pi * (start*t + (end-start)*t*t/(2*duration))
    tone = np.sin(phase) + metal*np.sin(phase*2.731)*np.exp(-t*9)
    air = RNG.uniform(-1,1,len(t))
    air = air - np.roll(air,1)*0.65
    env = np.minimum(t/0.008,1) * np.power(np.maximum(0,1-t/duration),1.7)
    signal = ((1-noise)*tone + noise*air) * env
    if pulse:
        signal *= 0.65 + 0.35*np.sin(2*np.pi*pulse*t)
    save(ROOT/'Sfx'/f'{name}.wav', signal)

# Independent timbres for weapon releases, abilities, warning cues, and impacts.
for args in [
 ('Sword',.26,260,65,.78,.3,0), ('Bow',.25,470,130,.35,.7,0),
 ('Magic',.48,420,1050,.10,.4,18), ('WarriorSpecial',1.05,95,38,.50,.4,15),
 ('ArcherSpecial',.75,630,1250,.25,.5,24), ('MageSpecial',1.10,180,680,.15,.8,9),
 ('SupremeWarrior',1.8,65,190,.45,.6,12), ('SupremeArcher',1.8,350,1450,.13,.7,16),
 ('SupremeMage',1.8,720,110,.18,.8,7), ('Claws',.3,200,60,.7,.3,0),
 ('Bone',.32,660,170,.40,1,0), ('Slam',.65,95,28,.48,.4,0),
 ('Charge',.7,60,150,.55,.3,13), ('Spit',.42,190,70,.5,.3,26),
 ('Ember',.4,240,600,.65,.2,0), ('Hit',.14,135,45,.55,.3,0),
 ('Hurt',.35,140,60,.2,.4,0), ('Block',.4,1150,800,.18,1,0),
 ('BossWarning',.75,160,550,.12,.6,9), ('BossBeam',.8,100,48,.50,.7,30),
 ('BossRoar',1.7,58,32,.40,.8,21), ('Defeat',1.7,220,45,.1,.4,0),
 ('Victory',2.2,300,660,.05,.6,5),
]: effect(*args)

# 16 bars, 120 BPM, D minor. Seamless 32-second bed with drones, pulsing bass,
# ostinato strings, distant bells and percussion. Deliberately a placeholder.
DURATION=32
n=int(SR*DURATION)
bed=np.zeros(n)
def add(at, signal, gain=1):
    start=int(at*SR)
    indices=(start+np.arange(len(signal)))%n
    np.add.at(bed, indices, signal*gain)
def note(freq, duration, kind='string'):
    t=np.arange(int(SR*duration))/SR
    if kind=='drum':
        return (np.sin(2*np.pi*(45*t+3*(1-np.exp(-t*25))))+.28*RNG.normal(0,1,len(t))*np.exp(-t*25))*np.exp(-t*9)
    harmonics=range(1,7) if kind=='string' else [1,2,3]
    s=sum(np.sin(2*np.pi*freq*h*t)/(h**1.5) for h in harmonics)
    attack=.035 if kind=='string' else .006
    env=np.minimum(t/attack,1)*np.exp(-t*(3 if kind=='bell' else 1.4))
    env*=np.minimum((duration-t)/.08,1)
    return s*env
midi=lambda m:440*2**((m-69)/12)
roots=[38,38,34,36,38,41,34,33]
for bar in range(16):
    root=roots[bar%8]; start=bar*2
    add(start,note(midi(root-12),2.8),.20)
    for step in range(8):
        pitch=root+[12,19,24,19,15,19,24,22][step]
        add(start+step*.25,note(midi(pitch),.45),.105)
    for beat in range(4):
        add(start+beat*.5,note(midi(root),.4),.17)
        add(start+beat*.5,note(45,.55,'drum'),.43 if beat%2==0 else .22)
    if bar%2==0:
        bell=note(midi(root+36),2.6,'bell')
        add(start,bell,.13)
        add(start+.28,bell,.055)
# Stereo width from a quiet, wrapped room reflection; no discontinuous fade at loop.
stereo=np.column_stack([bed+.13*np.roll(bed,int(.071*SR)),bed+.13*np.roll(bed,int(.113*SR))])
stereo*=.74/max(np.max(np.abs(stereo)),.01)
save(ROOT/'Music/boss_placeholder.wav',stereo)
from character_sfx import generate
generate()
print('Generated material-based character SFX, common cues and the original boss placeholder')
