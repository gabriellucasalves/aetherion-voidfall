#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldMapSmoke : MonoBehaviour
{
    const string Output = "/tmp/aetherion-worldmap-qa";
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    readonly List<string> _results = new List<string>();
    int _failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("WorldMapQA.Enabled", false)) return;
        SessionState.SetBool("WorldMapQA.Enabled", false);
        var go = new GameObject("WorldMapSmoke");
        DontDestroyOnLoad(go);
        go.AddComponent<WorldMapSmoke>();
    }

    void Check(bool success, string label)
    {
        _results.Add((success ? "PASS " : "FAIL ") + label);
        if (!success) _failures++;
        Debug.Log("[WorldMap QA] " + _results[_results.Count - 1]);
    }

    void Capture(string name)
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG());
        Destroy(texture);
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(Output);
        const string prefix = "aetherion.worldmap.v1.";
        string[] keys = { "completed", "hero", "node", "game_complete" };
        bool[] existed = new bool[keys.Length];
        string[] values = new string[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            existed[i] = PlayerPrefs.HasKey(prefix + keys[i]);
            values[i] = PlayerPrefs.GetString(prefix + keys[i], "");
            if (keys[i] != "hero" && existed[i]) values[i] = PlayerPrefs.GetInt(prefix + keys[i]).ToString();
        }
        var routine = Run();
        while (true)
        {
            bool next; object current = null;
            try { next = routine.MoveNext(); if (next) current = routine.Current; }
            catch (System.Exception e) { Check(false, e.ToString()); break; }
            if (!next) break;
            yield return current;
        }
        for (int i = 0; i < keys.Length; i++)
        {
            string key = prefix + keys[i];
            if (!existed[i]) PlayerPrefs.DeleteKey(key);
            else if (keys[i] == "hero") PlayerPrefs.SetString(key, values[i]);
            else PlayerPrefs.SetInt(key, int.Parse(values[i]));
        }
        PlayerPrefs.Save();
        File.WriteAllLines(Path.Combine(Output, "results.txt"), _results);
        Debug.Log("[WorldMap QA] FINAL " + (_failures == 0 ? "PASS" : "FAIL") + " cases=" + _results.Count);
        EditorApplication.Exit(_failures == 0 ? 0 : 1);
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(0.8f);
        var stage = FindFirstObjectByType<GroundT1Controller>();
        var spawner = FindFirstObjectByType<SimpleEnemySpawner>();
        var player = FindFirstObjectByType<PlayerController>();
        Check(stage != null && spawner != null && player != null, "Fase 1 inicializou");
        if (stage == null || spawner == null || player == null) yield break;
        player.GetComponent<HealthSystem>().IsInvulnerable = true;
        Check(FindFirstObjectByType<WorldMapController>() == null, "Mapa ausente antes da conclusão");
        yield return new WaitForEndOfFrame(); Capture("01-fase1-antes");

        float deadline = Time.realtimeSinceStartup + 65f;
        while (Time.realtimeSinceStartup < deadline && FindFirstObjectByType<F1ExitPortal>() == null)
        {
            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                Destroy(enemy.gameObject); // Accelerates waves; combat itself is outside this smoke.
            yield return null;
        }
        Check(spawner.Finished && spawner.Wave == 3 && FindFirstObjectByType<F1ExitPortal>() != null,
            "Três ondas obrigatórias abriram o portal");
        var portal = FindFirstObjectByType<F1ExitPortal>();
        if (portal == null) yield break;
        player.GetComponent<Rigidbody2D>().position = portal.transform.position + Vector3.up * 0.1f;
        yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(0.6f);
        Check(SceneManager.GetActiveScene().name == GameScenes.WorldMap, "Portal abriu mapa uma vez");
        Check(WorldMapProgress.IsCompleted(1) && WorldMapProgress.IsUnlocked(2) && !WorldMapProgress.IsUnlocked(3),
            "Fase 1 concluída, 2 liberada, 3 bloqueada");
        var map = FindFirstObjectByType<WorldMapController>();
        if (map == null) yield break;
        yield return new WaitForEndOfFrame(); Capture("02-mapa-apos-portal");

        // Verify route interpolation and map-only actions; browser QA covers real keyboard input.
        var route = typeof(WorldMapController).GetField("_distances", Flags).GetValue(map) as float[];
        typeof(WorldMapController).GetField("_position", Flags).SetValue(map, route[8]);
        typeof(WorldMapController).GetMethod("Refresh", Flags).Invoke(map, null);
        yield return new WaitForEndOfFrame(); Capture("03-caminho-meio");
        typeof(WorldMapController).GetField("_position", Flags).SetValue(map, route[15]);
        typeof(WorldMapController).GetMethod("Refresh", Flags).Invoke(map, null);
        typeof(WorldMapController).GetMethod("Confirm", Flags).Invoke(map, null);
        Check(SceneManager.GetActiveScene().name == GameScenes.WorldMap && WorldMapProgress.CurrentNode == 2,
            "Fase 2 sem cena permanece no mapa e salva nó 2");
        var notice = GameObject.Find("Aviso");
        Check(notice != null && notice.GetComponentInChildren<Text>().text.Contains("EM DESENVOLVIMENTO"),
            "Fase 2 mostra Em desenvolvimento");
        yield return new WaitForEndOfFrame(); Capture("04-fase2-em-desenvolvimento");
        SceneManager.LoadScene(GameScenes.WorldMap);
        yield return null; WorldMapController.EnsureExists(); yield return new WaitForSecondsRealtime(0.4f);
        Check(WorldMapProgress.CurrentNode == 2 && FindFirstObjectByType<WorldMapController>() != null,
            "Progresso e nó sobreviveram recarga da cena");
        yield return new WaitForEndOfFrame(); Capture("05-mapa-recarregado");

        map = FindFirstObjectByType<WorldMapController>();
        route = typeof(WorldMapController).GetField("_distances", Flags).GetValue(map) as float[];
        typeof(WorldMapController).GetField("_position", Flags).SetValue(map, route[24]);
        typeof(WorldMapController).GetMethod("Refresh", Flags).Invoke(map, null);
        typeof(WorldMapController).GetMethod("Confirm", Flags).Invoke(map, null);
        Check(SceneManager.GetActiveScene().name == GameScenes.WorldMap && !WorldMapProgress.IsUnlocked(3),
            "Nó 3 bloqueado não carrega fase");
        yield return new WaitForEndOfFrame(); Capture("06-fase3-bloqueada");

        typeof(WorldMapController).GetField("_position", Flags).SetValue(map, route[0]);
        typeof(WorldMapController).GetMethod("Refresh", Flags).Invoke(map, null);
        typeof(WorldMapController).GetMethod("Confirm", Flags).Invoke(map, null);
        yield return null; GroundT1Controller.EnsureExists(); yield return new WaitForSecondsRealtime(0.5f);
        Check(SceneManager.GetActiveScene().name == GameScenes.GroundT1, "Fase 1 concluída aceita replay");
        yield return new WaitForEndOfFrame(); Capture("07-replay-fase1");
        stage = FindFirstObjectByType<GroundT1Controller>();
        typeof(GroundT1Controller).GetMethod("TogglePause", Flags).Invoke(stage, null);
        Check(Time.timeScale == 0f && GameObject.Find("Pausa") != null, "Pausa funciona no replay");
        yield return new WaitForEndOfFrame(); Capture("08-replay-pausa");
        typeof(GroundT1Controller).GetMethod("TogglePause", Flags).Invoke(stage, null);
        var replayPlayer = FindFirstObjectByType<PlayerController>();
        typeof(GroundT1Controller).GetMethod("OnPlayerDied", Flags).Invoke(stage,
            new object[] { replayPlayer.GetComponent<HealthSystem>() });
        Check(Time.timeScale == 0f && GameObject.Find("Derrota") != null && WorldMapProgress.HighestCompleted == 1,
            "Morte preserva derrota e não desbloqueia Fase 3");
        yield return new WaitForEndOfFrame(); Capture("09-replay-derrota");
        typeof(GroundT1Controller).GetMethod("BackToMap", Flags).Invoke(stage, null);
        yield return null; WorldMapController.EnsureExists(); yield return new WaitForSecondsRealtime(0.3f);
        Check(SceneManager.GetActiveScene().name == GameScenes.WorldMap && Time.timeScale == 1f,
            "Retorno ao mapa após derrota");

        GameManager.Instance.SelectHero(CharacterCatalog.ById("mago"));
        Destroy(GameManager.Instance.gameObject);
        yield return null;
        var restored = GameManager.EnsureExists();
        Check(restored.SelectedHeroId == "mago" && WorldMapProgress.SelectedHeroId == "mago",
            "Herói selecionado persiste após recriar GameManager");
        SceneManager.LoadScene(GameScenes.WorldMap);
        yield return null; WorldMapController.EnsureExists(); yield return new WaitForSecondsRealtime(0.3f);
        var mageAvatar = FindFirstObjectByType<WorldMapHeroAnimator>();
        Check(mageAvatar != null && mageAvatar.GetComponent<Image>().sprite.texture == Resources.Load<Texture2D>("Mago/sheet"),
            "Avatar do mapa usa o sheet atual do Mago salvo");
        yield return new WaitForEndOfFrame(); Capture("10-mago-salvo");
    }
}
#endif
