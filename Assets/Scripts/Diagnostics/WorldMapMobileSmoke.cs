#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WorldMapMobileSmoke : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> _results = new List<string>();
    int _failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("WorldMapMobileQA.Enabled", false)) return;
        SessionState.SetBool("WorldMapMobileQA.Enabled", false);
        var root = new GameObject("WorldMapMobileSmoke");
        DontDestroyOnLoad(root);
        root.AddComponent<WorldMapMobileSmoke>();
    }

    void Check(bool yes, string label)
    {
        _results.Add((yes ? "PASS " : "FAIL ") + label);
        if (!yes) _failures++;
        Debug.Log("[Map Mobile QA] " + _results.Last());
    }

    static void Call(object target, string method) { target.GetType().GetMethod(method, Private).Invoke(target, null); }
    static PointerEventData Finger(int id) => new PointerEventData(EventSystem.current) { pointerId = id };
    static WorldMapDirectionButton Direction(string name) => GameObject.Find(name).GetComponent<WorldMapDirectionButton>();

    IEnumerator Start()
    {
        Directory.CreateDirectory("/tmp/aetherion-worldmap-mobile-qa");
        const string prefix = "aetherion.worldmap.v1.";
        var keys = new[] { "completed", "node", "hero", "game_complete" };
        var exists = keys.Select(k => PlayerPrefs.HasKey(prefix + k)).ToArray();
        var values = keys.Select(k => k == "hero" ? PlayerPrefs.GetString(prefix + k) : PlayerPrefs.GetInt(prefix + k).ToString()).ToArray();
        var forced = typeof(MobileControls).GetField("_forcedOn", BindingFlags.Static | BindingFlags.NonPublic);
        bool previousTouch = (bool)forced.GetValue(null);
        string selected = GameManager.EnsureExists().SelectedHeroId;
        int phase = StageData.ActivePhase;
        forced.SetValue(null, true);
        var run = Run();
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (System.Exception e) { Check(false, e.ToString()); break; }
            yield return current;
        }
        forced.SetValue(null, previousTouch);
        StageData.ActivePhase = phase;
        if (!string.IsNullOrEmpty(selected)) GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(selected));
        for (int i = 0; i < keys.Length; i++)
        {
            if (!exists[i]) PlayerPrefs.DeleteKey(prefix + keys[i]);
            else if (keys[i] == "hero") PlayerPrefs.SetString(prefix + keys[i], values[i]);
            else PlayerPrefs.SetInt(prefix + keys[i], int.Parse(values[i]));
        }
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        File.WriteAllLines("/tmp/aetherion-worldmap-mobile-qa/results.txt", _results.Concat(new[] { "FAILURES=" + _failures }));
        Debug.Log("[Map Mobile QA] Finished, failures=" + _failures);
        EditorApplication.isPlaying = false;
    }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(.4f);
        foreach (int completed in new[] { 0, 1, 2, 3 })
        {
            PlayerPrefs.SetInt("aetherion.worldmap.v1.completed", completed);
            PlayerPrefs.SetInt("aetherion.worldmap.v1.node", 3);
            WorldMapProgress.Complete(1);
            Check(WorldMapProgress.CurrentNode == 1, "Concluir Fase 1 volta ao início com progresso " + completed);
            Check(WorldMapProgress.HighestCompleted == Mathf.Max(1, completed), "Replay preserva fases liberadas " + completed);
        }
        PlayerPrefs.SetInt("aetherion.worldmap.v1.completed", 0);
        PlayerPrefs.SetInt("aetherion.worldmap.v1.node", 1);
        GameManager.EnsureExists().SelectHero(CharacterCatalog.ById("guerreiro"));
        StageData.ActivePhase = 1;
        SceneTransitionManager.Instance.Load(GameScenes.GroundT1);
        yield return new WaitForSecondsRealtime(.4f);
        FindFirstObjectByType<GroundT1Controller>().OnDemoComplete();
        yield return new WaitForSecondsRealtime(.4f);
        var map = FindFirstObjectByType<WorldMapController>();
        var touch = FindFirstObjectByType<WorldMapTouchControls>();
        Check(SceneManager.GetActiveScene().name == GameScenes.WorldMap && map.CurrentDestination == 1 && map.RouteDistance == 0f,
            "Conclusão real da Fase 1 chega no início do mapa");
        Check(touch.Visible && WorldMapProgress.IsUnlocked(2), "Mapa mobile mostra controles e libera Fase 2");
        var right = Direction("MapWalkRight");
        var left = Direction("MapWalkLeft");
        right.OnPointerDown(Finger(10));
        yield return new WaitForSecondsRealtime(.3f);
        Check(map.RouteDistance > 20f && touch.Move == 1f, "Segurar seta direita faz caminhar pela trilha");
        Check(HeroPortrait.WalkFrames("guerreiro").Contains(FindFirstObjectByType<WorldMapHeroAnimator>().GetComponent<Image>().sprite), "Movimento por toque anima o personagem");
        right.OnPointerUp(Finger(99));
        Check(touch.Move == 1f, "Outro dedo não cancela a seta pressionada");
        right.OnPointerUp(Finger(10));
        float stopped = map.RouteDistance;
        yield return new WaitForSecondsRealtime(.12f);
        Check(map.RouteDistance == stopped && touch.Move == 0f, "Soltar seta interrompe movimento");
        right.OnPointerDown(Finger(11)); left.OnPointerDown(Finger(12));
        Check(touch.Move == 0f, "Duas direções simultâneas se neutralizam");
        right.OnPointerExit(Finger(11));
        Check(touch.Move == -1f, "Arrastar para fora libera apenas aquela direção");
        yield return new WaitForSecondsRealtime(.6f);
        left.OnPointerUp(Finger(12));
        Check(map.RouteDistance == 0f, "Seta esquerda retorna ao começo sem sair da trilha");
        right.OnPointerDown(Finger(13));
        touch.SetBlocked(true);
        Check(!touch.Visible && touch.Move == 0f, "Bloquear controles limpa dedos pressionados");
        touch.SetBlocked(false);
        Check(touch.Visible && touch.Move == 0f, "Reabrir controles não deixa movimento preso");
        GameObject.Find("MapEnter").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.4f);
        Check(SceneManager.GetActiveScene().name == GameScenes.GroundT1 && FindFirstObjectByType<WorldMapTouchControls>() == null,
            "ENTRAR funciona por toque e remove controles do mapa");
        PlayerPrefs.SetInt("aetherion.worldmap.v1.completed", 2);
        PlayerPrefs.SetInt("aetherion.worldmap.v1.node", 3);
        FindFirstObjectByType<GroundT1Controller>().OnDemoComplete();
        yield return new WaitForSecondsRealtime(.4f);
        map = FindFirstObjectByType<WorldMapController>();
        Check(map.CurrentDestination == 1 && map.RouteDistance == 0f && WorldMapProgress.HighestCompleted == 2,
            "Rejogar Fase 1 retorna ao início mesmo com nó 3 salvo");
        foreach (string id in new[] { "guerreiro", "mago", "arqueiro" })
        {
            GameManager.Instance.SelectHero(CharacterCatalog.ById(id));
            SceneTransitionManager.Instance.Load(GameScenes.WorldMap);
            yield return new WaitForSecondsRealtime(.3f);
            var avatar = FindFirstObjectByType<WorldMapHeroAnimator>();
            Check(avatar.GetComponent<RectTransform>().sizeDelta == new Vector2(192f, 192f), id + " está 50% maior no mapa");
            Direction("MapWalkLeft").OnPointerDown(Finger(15));
            yield return new WaitForSecondsRealtime(.1f);
            Direction("MapWalkLeft").OnPointerUp(Finger(15));
            Check(avatar.transform.localScale.x == -1f && avatar.GetComponent<RectTransform>().sizeDelta.x == 192f,
                id + " preserva tamanho ao virar");
        }
        map = FindFirstObjectByType<WorldMapController>();
        touch = FindFirstObjectByType<WorldMapTouchControls>();
        right = Direction("MapWalkRight");
        right.OnPointerDown(Finger(20));
        float deadline = Time.unscaledTime + 10f;
        while (map.CurrentDestination != 2 && Time.unscaledTime < deadline) yield return null;
        right.OnPointerUp(Finger(20));
        Check(map.CurrentDestination == 2 && GameObject.Find("MapEnter").GetComponent<Button>().interactable,
            "Toque percorre rota inteira até Fase 2 e habilita ENTRAR");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("/tmp/aetherion-worldmap-mobile-qa/map.png");
        GameObject.Find("MapEnter").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.4f);
        Check(SceneManager.GetActiveScene().name == GameScenes.GroundT2, "Botão de toque carrega Fase 2");
        FindFirstObjectByType<GroundT1Controller>().OnDemoComplete();
        yield return new WaitForSecondsRealtime(.4f);
        Check(FindFirstObjectByType<WorldMapController>().CurrentDestination == 2, "Conclusão da Fase 2 preserva retorno ao seu nó");
        GameObject.Find("MapMenu").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.4f);
        Check(SceneManager.GetActiveScene().name == GameScenes.MainMenu, "MENU do mapa funciona por toque");
    }
}
#endif
