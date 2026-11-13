#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NazorAtaquesSmoke : MonoBehaviour
{
    public const string Output = "/tmp/aetherion-combate-hud-local";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    readonly List<string> _results = new List<string>(), _errors = new List<string>(), _prints = new List<string>();
    readonly List<(string id, bool active, float time)> _marks = new List<(string,bool,float)>();
    readonly List<(string id, float seconds, float magnitude)> _juice = new List<(string,float,float)>();
    readonly List<(float raw, float actual, int stop)> _damage = new List<(float,float,int)>();
    VoidDevourerBoss _boss;
    PlayerController _real, _mock;
    HealthSystem _mockHp;
    StageData _stage;
    float _lastHp;
    int _failures;
    bool _mirror;
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Flags).Invoke(owner,args);
    static void Set(object owner, string field, object value) => owner.GetType().GetField(field,Flags).SetValue(owner,value);
    void Check(bool ok,string message)
    {
        _results.Add((ok ? "PASS " : "FAIL ") + message);
        if(!ok)_failures++;
        Debug.Log("[Nazor QA] " + _results.Last());
    }
    void Error(string message,string trace,LogType type)
    {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)_errors.Add(message);}
    void Mark(string id,bool active,float time){_marks.Add((id,active,time));}
    void Juice(string id,float seconds,float magnitude){_juice.Add((id,seconds,magnitude));}
    void Damage(float raw,int stop)
    {
        if(_mockHp==null)return;
        _damage.Add((raw,_lastHp-_mockHp.Current,stop));_lastHp=_mockHp.Current;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if(!SessionState.GetBool("NazorAtaquesQA.Enabled",false))return;
        SessionState.SetBool("NazorAtaquesQA.Enabled",false);
        var root=new GameObject("NazorAtaquesQA");DontDestroyOnLoad(root);root.AddComponent<NazorAtaquesSmoke>();
    }
    void LateUpdate(){if(_mirror && _real!=null && _mock!=null)_real.transform.position=_mock.transform.position;}
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        if(image!=null){File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());Destroy(image);_prints.Add(name+".png");}
        else Check(false,"Captura indisponível: "+name);
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(Output);
        Application.logMessageReceived+=Error;
        NazorVfx.AttackMarked+=Mark;NazorVfx.ShakePlayed+=Juice;NazorVfx.PlayerHit+=Damage;
        var keys=new[]{"aetherion.worldmap.v1.completed","aetherion.worldmap.v1.game_complete","aetherion.worldmap.v1.node"};
        var saved=keys.Select(k=>PlayerPrefs.GetInt(k,-1)).ToArray();
        string heroKey="aetherion.worldmap.v1.hero",oldHero=PlayerPrefs.GetString(heroKey,"");
        int oldPhase=StageData.ActivePhase;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object current=null;bool more=false;
            try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
            catch(Exception e){Check(false,e.ToString());stack.Clear();break;}
            if(!more){stack.Pop();continue;}
            if(current is IEnumerator nested)stack.Push(nested);else yield return current;
        }
        if(_boss!=null)_boss.CancelActiveAttacks();
        _mirror=false;if(_mock!=null)Destroy(_mock.gameObject);
        HitStop.ForceRestore();CinematicTime.End();Time.timeScale=1f;
        NazorVfx.AttackMarked-=Mark;NazorVfx.ShakePlayed-=Juice;NazorVfx.PlayerHit-=Damage;
        Application.logMessageReceived-=Error;
        Check(_errors.Count==0,"Nenhum erro/exception de runtime: "+string.Join("; ",_errors));
        Check(Time.timeScale==1f,"Time.timeScale restaurado em 1 ao terminar");
        for(int i=0;i<keys.Length;i++)if(saved[i]<0)PlayerPrefs.DeleteKey(keys[i]);else PlayerPrefs.SetInt(keys[i],saved[i]);
        if(oldHero.Length==0)PlayerPrefs.DeleteKey(heroKey);else PlayerPrefs.SetString(heroKey,oldHero);
        StageData.ActivePhase=oldPhase;PlayerPrefs.Save();
        _results.Add("PASS_COUNT="+_results.Count(r=>r.StartsWith("PASS ")));_results.Add("FAILURES="+_failures);
        File.WriteAllLines(Path.Combine(Output,"nazor_ataques_resultado.txt"),_results);
        File.WriteAllLines(Path.Combine(Output,"nazor_prints.txt"),_prints);
        EditorApplication.isPlaying=false;
    }
    void Reset(int form, Vector3? position=null)
    {
        HitStop.ForceRestore();Time.timeScale=1f;
        _boss.CancelActiveAttacks();_boss.ForceStateFlying();
        var oldAura=(GameObject)_boss.GetType().GetField("_enrageAura",Flags).GetValue(_boss);
        if(oldAura!=null)Destroy(oldAura);Set(_boss,"_enrageAura",null);Set(_boss,"_enrageSecondFlash",false);
        Set(_boss,"_phase",form);Set(_boss,"_isEnraged",false);Set(_boss,"_enrageCadence",1f);
        _boss.Health.Configure(BossData.FormHealth(form));
        _boss.GetComponent<BossFormVisual>().SetForm(form);
        _boss.transform.localScale=Vector3.one;
        _boss.transform.position=new Vector3(52f,_stage.GroundTop+4.8f,0);
        Set(_boss,"_stateTimer",999f);
        Set(_boss,"_supremeDamageAccumulator",0f);Set(_boss,"_supremeCapTimer",0f);
        _mock.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        _mock.transform.position=position ?? new Vector3(43f,_stage.GroundTop+.6f,0);
        _mockHp.Configure(10000);_mockHp.IsInvulnerable=false;_lastHp=_mockHp.Current;
        Physics2D.SyncTransforms();_marks.Clear();_juice.Clear();_damage.Clear();
    }
    Coroutine Begin(string method,params object[] args)
    {
        Set(_boss,"_state",VoidDevourerBoss.State.Attacking);
        return _boss.StartCoroutine((IEnumerator)Call(_boss,method,args));
    }
    IEnumerator UntilFinished(float limit=10f)
    {
        float start=Time.realtimeSinceStartup;
        while(_boss!=null && _boss.CurrentState!=VoidDevourerBoss.State.Flying && Time.realtimeSinceStartup-start<limit)yield return null;
        Check(_boss!=null && _boss.CurrentState==VoidDevourerBoss.State.Flying,"Ataque terminou e devolveu controle ao ciclo");
        if(_boss!=null)Set(_boss,"_stateTimer",999f);
    }
    void Timings(string label)
    {
        var pending=new Dictionary<string,Queue<float>>();
        foreach(var mark in _marks)
        {
            if(!pending.ContainsKey(mark.id))pending[mark.id]=new Queue<float>();
            if(!mark.active)pending[mark.id].Enqueue(mark.time);
            else if(pending[mark.id].Count>0)
            {
                float elapsed=mark.time-pending[mark.id].Dequeue();
                float expected=mark.id=="olhar" ? (label=="olhar_f3" ? .70f : .75f)
                    :mark.id=="mergulho" ? .85f:mark.id=="varredura" ? .85f:mark.id=="tentaculo" ? (label=="erupcao_f3" ? .85f : label=="erupcao_f2" ? .95f:1f)
                    :mark.id=="garras" ? .9f:mark.id=="fenda" ? 1.6f:mark.id=="eclipse" ? 1.05f:mark.id=="caco" ? .8f:-1f;
                Check(elapsed>=.59f,label+" / "+mark.id+" aviso real >=0,6 s: "+elapsed.ToString("F3"));
                if(expected>0)Check(Mathf.Abs(elapsed-expected)<=.08f,label+" / "+mark.id+" tempo da tabela ±0,08 s");
            }
        }
    }
    IEnumerator Attack(string label,int form,string method,string key,float actionTime,float expectedBase,params object[] args)
    {
        Vector3? pos=key=="desabamento" ? new Vector3(VoidDevourerBoss.CollapseLaneX(0),_stage.GroundTop+.6f,0):(Vector3?)null;
        Reset(form,pos);
        int fire=_boss.FiredVolleys,dive=_boss.DiveLaunches,sonic=_boss.SonicVolleys,eclipse=_boss.EclipseVolleys;
        Begin(method,args);
        yield return new WaitForSeconds(.15f);yield return Capture("nazor_"+label+"_aviso");
        float before=_mockHp.Current;
        Check(before==10000,label+" não causa dano nos primeiros 0,15 s do aviso");
        yield return new WaitForSeconds(Mathf.Max(.01f,actionTime-.15f));
        yield return Capture("nazor_"+label+"_acao");
        yield return UntilFinished();
        Timings(label);
        Check(_marks.Any(m=>m.active && m.id==key),label+" abriu janela ativa real");
        if(expectedBase>0)
        {
            float expected=expectedBase*BossData.FormDamage(form);
            Check(_damage.Any(d=>Mathf.Abs(d.raw-expected)<.01f),label+" atingiu fixture com dano base×forma = "+expected.ToString("F2")+"; observado: "+string.Join(",",_damage.Select(d=>d.raw.ToString("F2"))));
        }
        Check(_damage.All(d=>Mathf.Abs(d.raw-d.actual)<.02f),label+" dano entregue à vida é exato no fixture sem armadura");
        if(key=="olhar" || label=="combo")Check(_boss.FiredVolleys>fire,label+" preserva FiredVolleys");
        if(key=="mergulho" || label=="combo")Check(_boss.DiveLaunches>dive,label+" preserva DiveLaunches");
        if(key=="sonica")Check(_boss.SonicVolleys==sonic+3,"SonicVolleys registra três salvas");
        if(key=="eclipse")Check(_boss.EclipseVolleys==eclipse+3,"EclipseVolleys registra três ondas normais");
        if(key=="desabamento")Check(_boss.CollapseWaves>=3,"CollapseWaves registra três ondas");
        if(key=="garras")Check(_boss.ClawStrikes>=4,"ClawStrikes registra quatro golpes");
        if(label=="combo")Check(_boss.ComboLaunches>0,"ComboLaunches incrementou");
        string juiceId=label=="combo" ? "combo":key;
        Vector2 shake=key=="varredura" ? new Vector2(1.6f,.035f):key=="tentaculo" ? new Vector2(.14f,.06f)
            :key=="mergulho" ? new Vector2(.22f,.12f):key=="nucleo" ? new Vector2(.12f,.06f)
            :key=="sonica" ? new Vector2(.09f,.06f):key=="eclipse" ? new Vector2(.12f,.08f)
            :key=="desabamento" ? new Vector2(.2f,.1f):key=="garras" ? new Vector2(.16f,.09f)
            :key=="fenda" ? new Vector2(.45f,.07f):new Vector2(.1f,.04f);
        Check(_juice.Any(j=>j.id==juiceId && Mathf.Abs(j.seconds-shake.x)<.001f && Mathf.Abs(j.magnitude-shake.y)<.001f),label+" chama tremor com duração/magnitude exatas da tabela");
        int stop=label=="combo" ? 50:key=="olhar" || key=="sonica" ? 45:key=="varredura" ? 50:key=="tentaculo" ? 40
            :key=="mergulho" || key=="garras" ? 60:key=="fenda" ? 70:55;
        if(expectedBase>0)Check(_damage.Any(d=>Mathf.Abs(d.raw-expectedBase*BossData.FormDamage(form))<.01f && d.stop==stop),label+" associa hitstop exato ao dano real ("+stop+" ms)");
        _boss.CancelActiveAttacks();yield return null;
        Check(!FindObjectsByType<NazorFallingDebris>(FindObjectsSortMode.None).Any() && !FindObjectsByType<NazorClawStrike>(FindObjectsSortMode.None).Any(),label+" limpa perigos ao cancelar");
    }
    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(.25f);
        // Validate all imported sheets against the provided manifest and form files.
        NazorSheets.EnsureManifest();
        Check(NazorSheets.Entries.Count==45,"Manifesto fornece as 45 folhas VFX");
        foreach(var pair in NazorSheets.Entries)
        {
            var entry=pair.Value;var frames=NazorSheets.Get(pair.Key);var texture=frames[0].texture;
            Check(frames.Length==entry.Quadros && texture.width==entry.CelulaW*entry.Quadros && texture.height==entry.CelulaH,"VFX dimensões/quadros: "+pair.Key);
            Check(frames.All(f=>f.pixelsPerUnit==32f),"VFX PPU32: "+pair.Key);
            Check(texture.filterMode==FilterMode.Point && texture.mipmapCount==1,"VFX Point sem mipmap: "+pair.Key);
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Check(importer.textureCompression==TextureImporterCompression.Uncompressed && importer.wrapMode==TextureWrapMode.Clamp && importer.alphaIsTransparency,"VFX sem compressão, Clamp e alpha: "+pair.Key);
        }
        for(int form=1;form<=3;form++)
        {
            var spec=NazorSheets.Form(form);
            foreach(string animation in new[]{"idle_8f","dano_2f","ataque_4f","nucleo_brilho_6f"})
            {
                var frames=NazorSheets.Get("forma"+form+"_"+animation);
                int count=animation=="idle_8f" ? 8:animation=="dano_2f" ? 2:animation=="ataque_4f" ? 4:6;
                Check(frames.Length==count && frames.All(f=>f.pixelsPerUnit==32f),"Forma "+form+" carrega "+animation+" em PPU32");
                var tex=frames[0].texture;var imp=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
                Check(tex.width==count*frames[0].rect.width && tex.filterMode==FilterMode.Point && tex.mipmapCount==1 && imp.textureCompression==TextureImporterCompression.Uncompressed,"Forma "+form+" / "+animation+" importação íntegra");
                if(animation!="nucleo_brilho_6f")Check(frames[0].rect.size==spec.Cell,"Célula segue JSON da forma "+form);
            }
        }
        foreach(string sheet in new[]{"forma1_transformacao_f2_8f","forma2_transformacao_f3_8f","forma3_morte_8f"})
            Check(NazorSheets.Get(sheet).Length==8,"Oito quadros carregados: "+sheet);
        Check(NazorSheets.Pivot("eclipse_coluna_aviso_40x144_6f").y<.01f,"Pivô de chão convertido de Y da imagem para Y do Unity");
        GameManager.EnsureExists().SelectHero(CharacterCatalog.ById("guerreiro"));StageData.ActivePhase=3;
        SceneTransitionManager.Instance.Load(GameScenes.GroundT3);yield return new WaitForSecondsRealtime(.5f);
        var controller=FindFirstObjectByType<GroundT1Controller>();_stage=controller.Stage;
        _real=FindFirstObjectByType<PlayerController>();
        Call(controller,"EnterBossArena");yield return null;
        _boss=FindFirstObjectByType<VoidDevourerBoss>();_boss.ForceStateFlying();
        _real.enabled=false;_real.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
        _real.GetComponent<Collider2D>().enabled=false;_real.GetComponent<HealthSystem>().IsInvulnerable=true;
        var fixture=new GameObject("NazorQAAlvoSemArmadura");fixture.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
        fixture.AddComponent<BoxCollider2D>().size=new Vector2(.55f,1.1f);
        _mockHp=fixture.AddComponent<HealthSystem>();_mock=fixture.AddComponent<PlayerController>();
        Set(_boss,"_target",_mock.transform);_mirror=true;
        for(int form=1;form<=3;form++)
        {
            Reset(form);_boss.SetAnimationPose(NazorPose.Hover);yield return null;
            var spec=NazorSheets.Form(form);var body=_boss.GetComponent<SpriteRenderer>();
            Check(Mathf.Abs(_boss.transform.localScale.x)==1f && _boss.transform.localScale.y==1f,"Forma "+form+" escala unitária");
            Check(_boss.BodyHurtbox.size==spec.BodySize && _boss.BodyHurtbox.offset==spec.BodyCenter,"Forma "+form+" hurtbox do JSON");
            Check(Vector2.Distance(_boss.Nucleus.transform.localPosition,spec.Nucleus)<.001f,"Forma "+form+" núcleo no ponto do JSON");
            _boss.Nucleus.SetActive(true);Physics2D.SyncTransforms();
            Vector2 size=_boss.Nucleus.Hurtbox.bounds.size;
            _boss.Nucleus.SetActive(false);
            Check(Vector2.Distance(size,spec.NucleusSize)<.001f,"Forma "+form+" elipse do núcleo nas dimensões do JSON");
            Check(_boss.GetComponent<NazorArticulation>().VertexCount==0 && !_boss.GetComponent<SpriteRenderer>().forceRenderingOff,"Forma "+form+" sem deformação ou malha");
            yield return Capture("nazor_forma"+form+"_idle");
            _boss.EnemyCtrl.ReceiveDamage(2f);yield return new WaitForEndOfFrame();
            Check(body.sprite.texture==NazorSheets.GetFrame("forma"+form+"_dano_2f",0).texture,"Forma "+form+" usa dano_2f ao receber golpe");
            yield return new WaitForSeconds(.09f);yield return new WaitForEndOfFrame();
            Check(body.sprite.name.EndsWith("_1"),"Forma "+form+" exibe segundo quadro de dano");
            yield return Capture("nazor_forma"+form+"_dano");
            yield return new WaitForSeconds(.12f);yield return new WaitForEndOfFrame();
            Check(body.sprite.texture==NazorSheets.GetFrame("forma"+form+"_idle_8f",0).texture,"Forma "+form+" retorna ao idle após reação");
            _boss.Nucleus.SetActive(true);
            float beforeCore=_boss.Health.Current;_boss.OnNucleusDamaged(10f);
            Check(Mathf.Abs(beforeCore-_boss.Health.Current-15f)<.01f,"Núcleo da forma "+form+" preserva multiplicador 1,5");
            yield return Capture("nazor_forma"+form+"_nucleo");_boss.Nucleus.SetActive(false);
            if(form<3)
            {
                _boss.Health.TakeDamage(100000f);
                for(int frameIndex=0;frameIndex<3;frameIndex++)
                {
                    int targetFrame=new[]{0,4,7}[frameIndex];
                    Set(_boss,"_poseStarted",Time.time-(targetFrame+.2f)/8f*BossData.TransitionDuration);
                    yield return new WaitForEndOfFrame();
                    Check(body.sprite.texture==NazorSheets.GetFrame("forma"+form+"_transformacao_f"+(form+1)+"_8f",0).texture,"Transição "+form+" usa folha de origem, quadro "+targetFrame);
                    yield return Capture("nazor_transformacao_f"+form+"_f"+(form+1)+"_quadro"+targetFrame);
                }
                _boss.ForceStateFlying();
            }
        }
        Safety();
        yield return Attack("olhar_f1",1,"AttackDoubleGlare","olhar",.86f,16f,2,.75f);
        yield return Attack("varredura",1,"AttackRedSweep","varredura",1.04f,22f,true,.85f,22f);
        yield return Attack("erupcao_f1",1,"AttackTentacleEruption","tentaculo",1.12f,18f,3,1f,0f);
        yield return Attack("mergulho_f1",1,"AttackDive","mergulho",1.04f,20f);
        yield return Attack("nucleo_f1",1,"AttackDescentExposed","nucleo",.72f,0f,2.6f);
        yield return Attack("sonica",2,"AttackSonicBurst","sonica",1.12f,20f);
        yield return Attack("desabamento",2,"AttackPalaceCollapse","desabamento",1.48f,22f);
        yield return Attack("erupcao_f2",2,"AttackTentacleEruption","tentaculo",1.08f,18f,4,.95f,0f);
        yield return Attack("combo",2,"AttackGlareDiveCombo","olhar",.86f,16f);
        yield return Attack("eclipse",3,"AttackEclipseRain","eclipse",1.38f,24f);
        yield return Attack("olhar_f3",3,"AttackDoubleGlare","olhar",.81f,16f,3,.70f);
        yield return Attack("garras",3,"AttackVoidClaws","garras",1.0f,28f);
        yield return Attack("erupcao_f3",3,"AttackTentacleEruption","tentaculo",.98f,18f,5,.85f,.35f);
        yield return Attack("mergulho_f3",3,"AttackDive","mergulho",1.04f,20f);
        yield return Attack("fenda",3,"AttackStellarRift","fenda",1.98f,30f);
        yield return Attack("nucleo_f3",3,"AttackDescentExposed","nucleo",.72f,0f,2.2f);
        yield return RiftSafety();
        yield return ComboTarget();
        yield return Enrage();
        yield return HitStopChecks();
        Reset(3);var supreme=_real.GetComponent<SpecialController>();
        supreme.StartCoroutine((IEnumerator)Call(supreme,"Run"));float beforeCap=_boss.Health.Current;
        _boss.EnemyCtrl.ReceiveDamage(10000f);_boss.EnemyCtrl.ReceiveDamage(10000f);
        Check(Mathf.Abs(beforeCap-_boss.Health.Current-BossData.SupremeDamageCap)<.01f,"SupremeDamageCap de 260 preservado durante supremo real");SpecialController.AbortActive();CinematicTime.End();
        Reset(3);_boss.SetAnimationPose(NazorPose.Dying);yield return Capture("nazor_forma3_morte");
        yield return new WaitForSeconds(.55f);yield return Capture("nazor_forma3_morte_quadro4");
        _boss.CancelActiveAttacks();Set(_boss,"_stateTimer",999f);
        Check(Time.timeScale==1f,"Regressão: escala de tempo 1 após todos os ataques");
    }
    void Safety()
    {
        for(int wave=0;wave<3;wave++)
        {
            var intervals=new List<Vector2>();
            for(int lane=0;lane<10;lane++)if(VoidDevourerBoss.CollapseMasks[wave][lane]!=0)
                intervals.Add(new Vector2(VoidDevourerBoss.CollapseLaneX(lane)-.875f,VoidDevourerBoss.CollapseLaneX(lane)+.875f));
            foreach(int lane in VoidDevourerBoss.CollapseShardLanes[wave])
            {
                Check(VoidDevourerBoss.CollapseMasks[wave][lane]==0,"Onda "+(wave+1)+": caco ocupa faixa de vão, não coluna");
                intervals.Add(new Vector2(VoidDevourerBoss.CollapseLaneX(lane)-.525f,VoidDevourerBoss.CollapseLaneX(lane)+.525f));
            }
            intervals=intervals.OrderBy(i=>i.x).ToList();
            var gaps=new List<Vector2>();float edge=37.5f;
            foreach(var interval in intervals){if(interval.x>edge)gaps.Add(new Vector2(edge,interval.x));edge=Mathf.Max(edge,interval.y);}
            if(edge<58.5f)gaps.Add(new Vector2(edge,58.5f));
            Check(gaps.Any(g=>g.y-g.x>=4.2f),"Desabamento onda "+(wave+1)+": vão contínuo >=4,2 u incluindo cacos e largura do herói");
            var usable=gaps.Where(g=>g.y-g.x>=.55f).ToArray();float worst=0;
            for(float x=37.5f;x<=58.5f;x+=.05f)worst=Mathf.Max(worst,usable.Min(g=>Mathf.Abs(x-Mathf.Clamp(x,g.x,g.y))));
            Check(worst<=4.2f,"Desabamento onda "+(wave+1)+": distância máxima até vão <=4,2 u ("+worst.ToString("F2")+")");
        }
        bool claws=true,rifts=true;
        for(float x=37.5f;x<=58.5f;x+=.1f)
        {
            float[] c=Enumerable.Range(0,3).Select(i=>VoidDevourerBoss.ClawPosition(i,x,x)).OrderBy(v=>v).ToArray();
            claws &= c.All(v=>v>=37.5f && v<=58.5f) && c[1]-c[0]-1.6f>=2.19f && c[2]-c[1]-1.6f>=2.19f;
            var r=VoidDevourerBoss.PlanRift(x,false);rifts &= Mathf.Abs(r[0]-x)<=6f;
            if(r.Length>1)rifts &= Mathf.Abs(r[1]-x)>=7.99f && Mathf.Abs(r[1]-r[0])>=5.99f;
            var e=VoidDevourerBoss.PlanRift(x,true);rifts &= e.Length==1 && Mathf.Abs(e[0]-x)<=5f;
        }
        Check(claws,"Garras: vão >=2,2 u e limites válidos em toda a arena (G4 remira em outro instante)");
        Check(rifts,"Fenda: A alcançável, B distante e separado; enrage com um ponto <=5 u");
    }
    IEnumerator RiftSafety()
    {
        Reset(3);Begin("AttackStellarRift");yield return new WaitForSeconds(.3f);
        Check(_boss.RiftSafeCenters.Count>=1 && _boss.RiftSafeWidth==3f,"Fenda abriu ponto seguro de 3 u");
        float x=_boss.RiftSafeCenters[0];_mock.transform.position=new Vector3(x,_stage.GroundTop+.6f,0);Physics2D.SyncTransforms();
        yield return Capture("nazor_fenda_ponto_seguro");
        float before=_mockHp.Current;yield return UntilFinished();
        Check(_mockHp.Current==before,"Herói parado no ponto seguro não toma dano nas duas varreduras");
        Reset(3);Begin("AttackStellarRift");yield return UntilFinished();
        var hits=_damage.Where(d=>Mathf.Abs(d.raw-39f)<.01f).ToArray();
        Check(hits.Length==2 && hits.All(d=>Mathf.Abs(d.actual-39f)<.02f),"Fora dos pontos: exatamente 39 de dano, uma vez por varredura");
        _boss.CancelActiveAttacks();yield return null;
        Check(!GameObject.Find("TintaSupremo") && !_boss.RiftActive,"Fenda remove tinta e parede no cancelamento");
    }
    IEnumerator ComboTarget()
    {
        Reset(2);Begin("AttackGlareDiveCombo");
        yield return new WaitForSeconds(1.4f);_mock.transform.position=new Vector3(49f,_stage.GroundTop+.6f,0);Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.4f);
        Check(Mathf.Abs(_boss.LastDiveTarget.x-49f)<.05f,"Combo amostra alvo do mergulho após a esquiva do olhar");
        yield return Capture("nazor_combo_mergulho_aviso");yield return UntilFinished();
    }
    IEnumerator Enrage()
    {
        Reset(3);int entries=_boss.EnrageEntries;Begin("AttackDoubleGlare",1,.7f);_boss.Health.TakeDamage(_boss.Health.Max*.71f);
        yield return new WaitForSeconds(.1f);Check(!_boss.IsEnraged,"Fúria não interrompe ataque em andamento");
        float until=Time.realtimeSinceStartup+4f;
        while(!_boss.IsEnraged && Time.realtimeSinceStartup<until)yield return null;
        Check(_boss.IsEnraged && _boss.EnrageEntries==entries+1,"Fúria entra uma vez a 29% de vida");
        yield return new WaitForSecondsRealtime(1.2f);
        Check(_boss.EnrageEntries==entries+1 && Mathf.Abs(_boss.EnrageCadence-1.25f)<.001f && Mathf.Abs(_boss.CadenceMultiplier-1.65f)<.001f,"Fúria mantém multiplicador 1,25 sem repetir entrada");
        Check(_boss.transform.Find("Vfx_aura_enrage_192x224_8f")!=null,"Aura permanente presente");
        yield return Capture("nazor_forma3_enrage");
        _boss.CancelActiveAttacks();Set(_boss,"_stateTimer",999f);
        Check(_boss.Telegraph(.85f)>=.6f && _boss.Telegraph(.7f)==.7f && _boss.Telegraph(1.6f)>=.6f,"Fúria preserva mínimo de aviso e não encurta base <=0,75 s");
        int waves=_boss.EclipseVolleys;_marks.Clear();Begin("AttackEclipseRain");
        yield return new WaitForSeconds(.15f);var hazards=FindObjectsByType<NazorHazard>(FindObjectsSortMode.None).OrderBy(h=>h.WarningPosition.x).ToArray();
        Check(hazards.Length==5 && Mathf.Abs(hazards[1].WarningPosition.x-hazards[0].WarningPosition.x-2.1f)<.01f,"Eclipse enfurecido usa faixas de 2,1 u");
        yield return Capture("nazor_eclipse_enrage_aviso");yield return UntilFinished();
        Check(_boss.EclipseVolleys==waves+4,"Eclipse enfurecido dispara quatro ondas");
        Check(_marks.Where(m=>m.active).All(m=>m.time-_marks.First(w=>!w.active && w.id==m.id).time>=.6f),"Nenhum disparo do Eclipse enfurecido ocorre antes de 0,6 s");
        Begin("AttackStellarRift");yield return new WaitForSeconds(.3f);
        Check(_boss.RiftSafeCenters.Count==1 && _boss.RiftSafeWidth==3.4f,"Fenda enfurecida tem um ponto seguro de 3,4 u");
        yield return Capture("nazor_fenda_enrage_aviso");yield return UntilFinished();
        _marks.Clear();Begin("AttackVoidClaws");yield return UntilFinished();
        var warnings=_marks.Where(m=>!m.active && m.id=="garras").ToArray();
        Check(warnings.Length==4 && Mathf.Abs(warnings[1].time-warnings[0].time-.44f)<.08f,"Garras enfurecidas espaçam marcadores em 0,44 s");
        Timings("garras_enrage");
        _boss.CancelActiveAttacks();yield return null;
    }
    IEnumerator HitStopChecks()
    {
        Reset(1);_mockHp.IsInvulnerable=true;
        Check(!NazorVfx.DamagePlayer(_mock.transform,Vector3.zero,16,45) && !HitStop.IsActive,"Alvo invulnerável não dispara hitstop");
        _mockHp.IsInvulnerable=false;NazorVfx.DamagePlayer(_mock.transform,Vector3.zero,16,45);
        Check(HitStop.IsActive && Time.timeScale==0f,"Acerto real dispara hitstop");
        HitStop.Do(60);yield return new WaitForSecondsRealtime(.11f);
        Check(!HitStop.IsActive && Time.timeScale==1f,"Hitstop usa maior duração e restaura escala 1");
        HitStop.Do(120);_mockHp.Kill();yield return new WaitForSecondsRealtime(.18f);
        Check(Time.timeScale==1f,"Hitstop restaura escala mesmo com morte do fixture no meio");
        Time.timeScale=0f;HitStop.Do(60);Check(!HitStop.IsActive && Time.timeScale==0f,"Pausa externa não é desfeita pelo hitstop");Time.timeScale=1f;
        CinematicTime.Begin(.1f);HitStop.Do(60);Check(!HitStop.IsActive && Mathf.Abs(Time.timeScale-.1f)<.001f,"CinematicTime tem prioridade sobre hitstop");CinematicTime.End();
        SupremeFx.PlayFlash(new Color32(130,225,255,255),.3f,.12f);yield return null;
        Check(GameObject.Find("FlashSupremo")!=null,"Flash cria overlay temporário");
        yield return new WaitForSecondsRealtime(.2f);Check(GameObject.Find("FlashSupremo")==null,"Flash destrói overlay ao concluir");
        var crater=NazorVfx.Play("eclipse_cratera_80x64_7f",new Vector3(48,_stage.GroundTop,0),duration:.6f);
        crater.GetComponent<NazorVfxInstance>().FadeSeconds=.2f;
        yield return new WaitForSeconds(.53f);
        Check(crater!=null && crater.GetComponent<SpriteRenderer>().color.a<1f,"Cratera segura último quadro durante a dissolução explícita");
        yield return new WaitForSeconds(.12f);
        Check(crater==null,"Cratera desaparece ao concluir a duração, sem VFX órfão");
        foreach(string cue in new[]{"BossWarning","BossBeam","BossRoar","BossSonicCharge","BossSonicWave","BossVoidMark","BossVoidImpact","Tentacle","Slam","KnightSlam","Claws","BossHit"})
            Check(Enum.IsDefined(typeof(GameAudio.Cue),cue),"Cue existente preservado: "+cue);
    }
}
#endif
