"""Original Japanese channeling and impact calls using installed macOS synthesis voices.
No voice cloning or borrowed character recordings. Requires say and ffmpeg.
"""
from pathlib import Path
import subprocess
import tempfile
import wave
import numpy as np
from character_sfx import SR

ROOT=Path(__file__).resolve().parents[2]/'Assets/Resources/Audio/Sfx'
# Display text comes directly from the same constants as the game.
import re
SOURCE=(Path(__file__).resolve().parents[2]/'Assets/Scripts/Abilities/SpecialJapanese.cs').read_text()
LINES={key:value.replace('\\n',' ') for key,value in re.findall(r'public const string (\w+) = "([^"]+)";',SOURCE)}
CALLS=[]
for hero,voice,rate,pitch in [('Warrior','Rocko (Japonês (Japão))',240,.94),
                             ('Mage','Reed (Japonês (Japão))',245,.96),
                             ('Archer','Eddy (Japonês (Japão))',250,1.03)]:
    CALLS.append((hero+'Callout',voice,rate,LINES[hero+'Channel'],pitch))
    # Explicit kana disambiguates the author's requested impact readings.
    impact={'Warrior':'ぐれんのつき！！！','Mage':'スーパーノヴァ！！！','Archer':'しちせいがり！！！'}[hero]
    CALLS.append((hero+'ImpactCallout',voice,rate,impact,pitch))

def generate():
    with tempfile.TemporaryDirectory(prefix='aetherion-callouts-') as folder:
        for name,voice,rate,phrase,pitch in CALLS:
            source=Path(folder)/(name+'.aiff')
            subprocess.run(['say','-v',voice,'-r',str(rate),'-o',str(source),phrase],check=True)
            filt=f'aresample=22050,asetrate=22050*{pitch},aresample=22050,highpass=f=95,lowpass=f=4300,acompressor=threshold=0.13:ratio=3:attack=4:release=70'
            raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-ar',str(SR),'-ac','1','-af',filt,'-f','f32le','-'])
            x=np.frombuffer(raw,dtype='<f4').astype(float)
            # Remove synthesizer lead/trailing silence so the cry lands on the bust reveal.
            audible=np.flatnonzero(abs(x)>.005)
            if len(audible):x=x[max(0,audible[0]-round(.015*SR)):min(len(x),audible[-1]+round(.07*SR))]
            # Restrained slapback and room, preserving consonants and intelligibility.
            tail=round(.20*SR);out=np.zeros(len(x)+tail)
            out[:len(x)]+=x
            for delay,gain in [(.045,.12),(.085,.07),(.145,.04)]:
                start=round(delay*SR);out[start:start+len(x)]+=x*gain
            out=np.tanh(out*1.3);out*=.84/max(np.max(np.abs(out)),.001)
            with wave.open(str(ROOT/(name+'.wav')),'wb') as w:
                w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes((out*32767).astype('<i2').tobytes())
            print(name,phrase,round(len(out)/SR,2),'seconds')

if __name__=='__main__':generate()
