"""Original low sonic/void effects for Nazor's second and third forms.
No supplied recordings are changed. Regenerate with python3 Tools/Audio/nazor_sfx.py.
"""
from pathlib import Path
import wave
import numpy as np
ROOT = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Sfx'
SR = 22050
rng = np.random.default_rng(147)
def write(name, duration, start, end, noise, charge=False):
    t = np.arange(round(duration * SR)) / SR
    frequency = start + (end - start) * t / duration
    phase = 2 * np.pi * np.cumsum(frequency) / SR
    x = np.sin(phase) * .55 + np.sin(phase * .503) * .24 + np.sin(phase * 2.01) * .08
    air = np.convolve(rng.normal(0, 1, len(t)), np.ones(28) / 28, mode='same')
    env = (1-np.exp(-t*35)) * (np.exp(-t * 4 / duration) if not charge else .2+.8*t/duration)
    env *= np.minimum(1, (duration-t)/.055)
    x = np.tanh((x + air * noise) * 1.3) * env
    x *= .84 / max(np.max(np.abs(x)), .001)
    with wave.open(str(ROOT / (name+'.wav')), 'wb') as out:
        out.setnchannels(1); out.setsampwidth(2); out.setframerate(SR)
        out.writeframes((x*32767).astype('<i2').tobytes())
write('BossSonicCharge', .95, 48, 110, .60, True)
write('BossSonicWave', .8, 125, 40, 1.20)
write('BossVoidMark', 1.05, 170, 96, .42, True)
write('BossVoidImpact', .65, 92, 28, 2.0)
print('4 Nazor cues generated')
