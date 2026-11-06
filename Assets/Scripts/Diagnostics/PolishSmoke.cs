#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PolishSmoke : MonoBehaviour
{
    const string Output="/tmp/astra-polish-qa";
    readonly List<string> results=new List<string>(); int failures;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if(!SessionState.GetBool("PolishQA.Enabled",false))return;
        SessionState.SetBool("PolishQA.Enabled",false);
        var go=new GameObject("PolishSmoke");DontDestroyOnLoad(go);go.AddComponent<PolishSmoke>();
    }
    void Check(bool ok,string msg){results.Add((ok?"PASS ":"FAIL ")+msg);if(!ok)failures++;Debug.Log("[Polish QA] "+results.Last());}
    IEnumerator Start()
    {
        Directory.CreateDirectory(Output);
        var run=Run();
        while(true)
        {
            bool next;object current=null;
            try{next=run.MoveNext();if(next)current=run.Current;}
            catch(System.Exception e){Check(false,e.ToString());break;}
            if(!next)break;yield return current;
        }
        File.WriteAllLines(Output+"/results.txt",results);
        Debug.Log("[Polish QA] FINAL "+(failures==0?"PASS":"FAIL")+" cases="+results.Count);
        EditorApplication.Exit(failures==0?0:1);
    }
    void Capture(string name)
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());Destroy(image);
    }
    IEnumerator Run()
    {
        foreach(string hero in new[]{"guerreiro","mago","arqueiro"})
        {
            yield return new WaitForSeconds(.3f);
            GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(hero));
            SceneManager.LoadScene("Ground_T1");yield return null;
            GroundT1Controller.EnsureExists();yield return new WaitForSeconds(.3f);
            var player=FindFirstObjectByType<PlayerController>();
            FindFirstObjectByType<SimpleEnemySpawner>().SetLocked(true);
            player.GetComponent<HealthSystem>().IsInvulnerable=true;
            foreach(var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            var card=FindFirstObjectByType<PhaseTitleCardFader>();if(card!=null)Destroy(card.gameObject);
            yield return new WaitForSeconds(.2f);
            var camera=Camera.main;var rt=camera.targetTexture;
            Check(rt.width==1536&&rt.height==864&&rt.filterMode==FilterMode.Point&&!rt.useMipMap&&rt.antiAliasing==1,"RT 1536x864 Point "+hero);
            Check(Mathf.Abs(camera.orthographicSize-6.75f)<.001f&&Mathf.Abs(camera.aspect-16f/9)<.001f,"Framing 24x13.5 u "+hero);
            var hud=GameObject.Find("HudCentral");Check(hud!=null,"HUD central "+hero);
            var images=hud.GetComponentsInChildren<Image>(true);
            var portrait=images.First(i=>i.name=="Retrato");
            Check(portrait.sprite!=null&&portrait.sprite.name=="Retrato_"+hero,"Retrato correto "+hero);
            var defense=images.First(i=>i.name=="BarraEscudo");
            Check(defense.gameObject.activeSelf==(hero!="arqueiro"),"Segunda barra visível correta "+hero);
            if(hero!="arqueiro")Check(defense.GetComponentsInChildren<Text>().Any(t=>t.text==(hero=="mago"?"CAMPO":"ESCUDO")),"Rótulo defesa "+hero);
            foreach(var bar in images.Where(i=>i.name=="BarraVida"||i.name=="BarraEscudo"||i.name=="BarraEspecial"))Check(bar.rectTransform.rect.height==40&&bar.color.a>=.95f,"Barra grossa opaca "+bar.name+" "+hero);
            Check(!T1AtmosScenery.AllowLegacyFallback&&T1AtmosScenery.Built,"Atmos ativo fallback off "+hero);
            Check(FindObjectsByType<AtmosV1Parallax>(FindObjectsSortMode.None).Length==6,"Planos e overlays ativos "+hero);
            if(hero=="guerreiro")
            {
                player.GetComponent<ResonanceMeter>().Add(200);yield return null;
                Check(hud.GetComponentsInChildren<Text>().Any(t=>t.text=="PRONTO (L/Q/E)"),"Especial cheio legível");
            }
            yield return new WaitForEndOfFrame();Capture(hero+"-1920");
            var previousRT=RenderTexture.active;RenderTexture.active=rt;
            var sample=new Texture2D(1,1,TextureFormat.RGBA32,false);
            sample.ReadPixels(new Rect(30,rt.height-30,1,1),0,0);sample.Apply();
            Check(sample.GetPixel(0,0).maxColorComponent>.1f,"Céu renderizado após recarga "+hero);
            Destroy(sample);RenderTexture.active=previousRT;
            if(hero=="guerreiro")
            {
                Check(Screen.width==1920&&Screen.height==1080,"Game View Full HD real");
                var p=PixelPresentation.FitRect(1920,1080);Check(p.width==1536&&p.height==864&&p.x==192&&p.y==108,"Letterbox inteiro Full HD");
                var pg=PixelPresentation.FitRect(2560,1440);Check(pg.width==1536&&pg.height==864&&pg.x==512&&pg.y==288,"Letterbox inteiro QHD");
                var small=PixelPresentation.FitRect(1280,720);Check(small.x>=0&&small.y>=0&&small.width<=1280,"Viewport menor não recorta o mundo");
                // Reflete a chamada somente no Editor, sem dependência do assembly de runtime.
                var editorType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("PolishValidation")).First(t=>t!=null);
                editorType.GetMethod("Size").Invoke(null,new object[]{2560,1440});
                yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();Capture("guerreiro-2560");
                Check(Screen.width==2560&&Screen.height==1440,"Game View QHD real");
                editorType.GetMethod("Size").Invoke(null,new object[]{1920,1080});yield return new WaitForSeconds(.3f);
                var hudCanvas=GameObject.Find("HudT1").GetComponent<Canvas>();hudCanvas.enabled=false;
                var renderers=player.GetComponentsInChildren<SpriteRenderer>();var states=renderers.Select(r=>r.enabled).ToArray();
                foreach(var r in renderers)r.enabled=false;
                yield return new WaitForEndOfFrame();Capture("inicio-sem-hud");
                hudCanvas.enabled=true;for(int i=0;i<renderers.Length;i++)renderers[i].enabled=states[i];
                var stage=FindFirstObjectByType<GroundT1Controller>().Stage;var gap=AtmosFrontLevel.Gaps[0];
                player.enabled=false;player.GetComponent<Rigidbody2D>().position=new Vector2((gap.x+gap.y)*.5f,stage.GroundTop+.8f);
                player.GetComponent<HealthSystem>().IsInvulnerable=false;
                yield return new WaitForSeconds(.22f);yield return new WaitForEndOfFrame();Capture("gap");
                yield return new WaitForSecondsRealtime(1f);
                Check(player.GetComponent<HealthSystem>().IsDead&&GameObject.Find("Derrota")!=null,"Gap real mantém VOCÊ CAIU");
                yield return new WaitForEndOfFrame();Capture("voce-caiu");Time.timeScale=1;
            }
        }
    }
}
#endif
