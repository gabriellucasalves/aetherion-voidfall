"""Deep arcane casts, three-stage supreme design, and six prologue soundscapes.
Original synthesis; no borrowed game audio. Run to update just this sound pass.
"""
from pathlib import Path
import wave
import numpy as np
from character_sfx import SR, clock, env, mix, metal, save

rng = np.random.default_rng(12782)
ROOT=Path(__file__).resolve().parents[2]/'Assets/Resources/Audio'

def wind(d,low=60,high=1700):
    x=rng.normal(0,1,round(d*SR));f=np.fft.rfftfreq(len(x),1/SR)
    spectrum=np.fft.rfft(x)*(1-np.exp(-(f/low)**4))*np.exp(-(f/high)**4)
    y=np.fft.irfft(spectrum,len(x));return y/max(np.std(y),.001)

def breath(d):
    t=clock(d)
    return wind(d,120,1400)*(.65+.35*np.sin(2*np.pi*5*t))

def choir(d,root=110):
    t=clock(d);x=np.zeros(len(t))
    for detune in [.994,1,1.007]:
        for k in range(1,15):
            f=root*k
            formant=.14+np.exp(-((f-520)/240)**2)+.5*np.exp(-((f-1040)/300)**2)
            x+=np.sin(2*np.pi*f*detune*t+.04*np.sin(2*np.pi*.8*t))*formant/(k**1.35)
    return x/3

def impact(d,weight=1):
    t=clock(d)
    phase=2*np.pi*(42*weight*t+38*weight*(1-np.exp(-t*16))/16)
    body=(np.sin(phase)+.3*np.sin(phase*2))*env(t,.003,4.5)
    grit=wind(d,160,1900)*env(t,.002,15)
    return body*.75+grit*.23

def surge(d,root=110):
    t=clock(d)
    return (choir(d,root)*.65+breath(d)*.23)*np.sin(np.pi*t/d)**.65

def character_overrides():
    # A cast is a breath/gesture, a low resonant pulse and dissipating arcane air.
    # No high bell cluster, rising sine "bubble", or piercing crystal transient.
    t=clock(.68)
    cast=(choir(.68,146.83)*.45+wind(.68,170,1450)*.24)*env(t,.035,7)
    cast+=mix(.68,(.07,impact(.55,1.5),.25))
    field=surge(1.65,110)*.6+mix(1.65,(.15,impact(1.45,1.1),.4))
    charge={}
    d=2.1;t=clock(d);ramp=np.minimum(t/.20,1)*(.25+.75*(t/d)**.8)*np.minimum((d-t)/.12,1)
    charge['SupremeWarrior']=mix(d,(0,impact(.6,.9),.45),(.1,wind(2,100,2200)*ramp[:len(clock(2))],.45)) + choir(d,73.42)*ramp*.5
    charge['SupremeMage']=mix(d,(0,impact(.65,1.15),.38))+(choir(d,98)*.7+breath(d)*.2)*ramp
    charge['SupremeArcher']=mix(d,(0,impact(.6,1.4),.38))+(choir(d,146.83)*.46+wind(d,250,2500)*.3)*ramp
    out={'Magic':cast,'MageSpecial':field,**charge}
    out['MageGuard']=impact(.42,1.4)*.35+choir(.42,146.83)*env(clock(.42),.005,12)*.5
    out['ArcherGuard']=metal(.32,840,25)*.4+wind(.32,650,2200)*env(clock(.32),.005,18)*.12
    out['WarriorRelease']=mix(1.6,(0,impact(1.5,.85),.75),(0,metal(1.2,195,4),.3),(.03,wind(1.5,180,2800)*env(clock(1.5),.015,3),.4))
    out['MageRelease']=mix(1.7,(0,impact(1.5,1.1),.7),(.02,surge(1.6,98),.65))
    out['ArcherRelease']=mix(1.4,(0,impact(1.3,1.5),.65),(.01,wind(1.3,600,3500)*env(clock(1.3),.008,4),.5),(.04,choir(1.2,196)*env(clock(1.2),.02,4),.3))
    out['WarriorCrescent']=mix(.6,(0,wind(.55,160,2800)*np.sin(np.pi*clock(.55)/.55)**2,.5),(.03,impact(.5,1.4),.5))
    out['WarriorFinal']=mix(1.8,(0,impact(1.7,.75),.8),(.02,metal(1.6,180,3.5),.26),(.08,wind(1.6,140,2400)*env(clock(1.6),.01,2.4),.35))
    out['MageTimeBreak']=surge(2.65,82.41)*.65+mix(2.65,(0,impact(.8,1.1),.5))
    out['MageNova']=mix(2.,(0,impact(1.9,.95),.85),(.06,choir(1.9,110)*env(clock(1.9),.03,1.8),.55),(.01,wind(1.6,140,1800)*env(clock(1.6),.01,2.8),.32))
    out['ArcherNova']=mix(1.5,(0,impact(1.4,1.4),.7),(.04,wind(1.3,300,2800)*env(clock(1.3),.02,3.5),.45))
    out['MageHurt']=(choir(.3,164.81)*.45+wind(.3,130,1100)*.18)*env(clock(.3),.005,14)
    return out

def prologue_cues():
    # A page change cancels the previous accent. Completing a line never retriggers it.
    return {
        'ProloguePeace':surge(1.6,220)*.18,
        'PrologueOmen':surge(2.7,55)*.5,
        'PrologueRupture':mix(4.,(0,impact(3.,.7),.8),(.25,impact(2.6,1.1),.45),(.65,wind(3.2,130,3200)*env(clock(3.2),.03,1.8),.4)),
        'PrologueVoid':surge(3.4,41.2)*.5+mix(3.4,(0,impact(1.5,.75),.4)),
        'PrologueMemory':surge(2.2,196)*.25,
        'PrologueResolve':surge(2.5,146.83)*.5+mix(2.5,(0,impact(1.2,1.5),.3)),
    }

def write_bed(name,x):
    x=np.tanh(x)
    # Fourier-created wind and rounded oscillator frequencies are periodic.
    # Add a wrapped early reflection for width without hard panning.
    stereo=np.column_stack([x+.13*np.roll(x,round(.061*SR)),x+.13*np.roll(x,round(.097*SR))])
    stereo*=.46/max(np.max(np.abs(stereo)),.001)
    p=ROOT/'Ambience'/f'{name}.wav';p.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(p),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((stereo*32767).astype('<i2').tobytes())

def generate():
    out={**character_overrides(),**prologue_cues()}
    # Sustained power under the full Japanese chant, without repeating the flash.
    dloop=2.;tl=clock(dloop)
    for hero,root in [('Warrior',73),('Mage',98),('Archer',147)]:
        tone=sum(np.sin(2*np.pi*(round(root*k*dloop)/dloop)*tl)/(k**1.6) for k in range(1,7))
        out[hero+'ChannelLoop']=tone*(.8+.2*np.sin(2*np.pi*2.5*tl))*.3+wind(dloop,90,1000)*.035
    for name,x in out.items():save(name,x,.84 if 'Release' in name or 'Nova' in name or 'Supreme' in name else .76)
    d=12;t=clock(d)
    def tone(f): return np.sin(2*np.pi*(round(f*d)/d)*t)
    breeze=wind(d,150,2400)*(.75+.25*np.sin(2*np.pi*t/d))
    water=wind(d,350,3500)*(.6+.4*np.sin(2*np.pi*t*3/d))
    calm=breeze*.10+water*.045+(tone(146.83)+.55*tone(220)+.2*tone(293.66))*.055
    rumble=wind(d,25,480)*(.8+.2*np.sin(2*np.pi*2*t/d))
    omen=rumble*.18+breeze*.035+(tone(49)+.35*tone(73.42))*.12
    destruction=rumble*.2+wind(d,150,3000)*(.35+.25*np.sin(2*np.pi*13*t/d)**2)*.21
    destruction+=(tone(36.71)+.35*tone(110))*.07
    void=rumble*.12+wind(d,120,1200)*(.5+.5*np.sin(2*np.pi*2*t/d))*.045+(tone(41.2)+tone(43.65)*.4)*.13
    memory=calm*.8+rumble*.055+tone(55)*.025
    resolve=breeze*.06+rumble*.09+(tone(73.42)+.45*tone(110)+.28*tone(146.83))*.12
    for name,x in zip(['prologue_calm','prologue_omen','prologue_destruction','prologue_void','prologue_memory','prologue_resolve'],[calm,omen,destruction,void,memory,resolve]):write_bed(name,x)
    print(f'Generated {len(out)} cinematic cues and six seamless prologue soundscapes')

if __name__=='__main__':generate()
