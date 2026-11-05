#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// QA local com física e RenderTexture reais. Código excluído do WebGL.
public class AtmosV1Smoke : MonoBehaviour
{
    const string Output = "/tmp/aetherion-atmos-v1-qa";
    readonly List<string> _results = new List<string>();
    int _failed;
    PlayerController _player;
    Rigidbody2D _body;
    BoxCollider2D _box;
    GroundT1Controller _controller;
    StageData _stage;
    Camera _camera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("AtmosQA.Enabled", false)) return;
        SessionState.SetBool("AtmosQA.Enabled", false);
        var go = new GameObject("AtmosV1Smoke");
        DontDestroyOnLoad(go);
        go.AddComponent<AtmosV1Smoke>();
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(Output);
        var routine = Run();
        while (true)
        {
            object current = null; bool next = false;
            try { next = routine.MoveNext(); if (next) current = routine.Current; }
            catch (Exception e) { Check(false, "Exceção: " + e); Finish(); yield break; }
            if (!next) break;
            yield return current;
        }
        Finish();
    }
    void Check(bool pass, string name)
    {
        _results.Add((pass ? "PASS " : "FAIL ") + name);
        if (!pass) _failed++;
        Debug.Log("[Atmos QA] " + _results[_results.Count - 1]);
    }
    void Finish()
    {
        File.WriteAllLines(Output + "/playmode-results.txt", _results);
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, SessionState.GetBool("AtmosQA.OldAtmos", true));
        Debug.Log("[Atmos QA] FINAL " + (_failed == 0 ? "PASS" : "FAIL") + " cases=" + _results.Count);
        EditorApplication.Exit(_failed == 0 ? 0 : 1);
    }
    void Bind()
    {
        _controller = FindFirstObjectByType<GroundT1Controller>();
        _stage = _controller.Stage;
        _player = FindFirstObjectByType<PlayerController>();
        _body = _player.GetComponent<Rigidbody2D>();
        _box = _player.GetComponent<BoxCollider2D>();
        _camera = Camera.main;
        FindFirstObjectByType<SimpleEnemySpawner>().SetLocked(true);
        _player.enabled = false;
        _player.GetComponent<HealthSystem>().IsInvulnerable = true;
    }
    float RootAboveFeet => _box.size.y * 0.5f - _box.offset.y;
    void PlaceFeet(float x, float y)
    {
        _body.position = new Vector2(x, y + RootAboveFeet + 0.015f);
        _body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    void Capture(string name, bool reference = false)
    {
        var old = _camera.transform.position;
        if (reference) _camera.transform.position = new Vector3(-_stage.HalfWidth + 12f, _stage.GroundTop + 5.5f, -10f);
        foreach (var layer in FindObjectsByType<AtmosV1Parallax>(FindObjectsSortMode.None))
            layer.Apply(_camera.transform.position);
        _camera.Render();
        var rt = _camera.targetTexture;
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Output + "/" + name + ".rgba", texture.GetRawTextureData());
        RenderTexture.active = previous;
        Destroy(texture);
        _camera.transform.position = old;
    }
    IEnumerator Run()
    {
        yield return new WaitForSeconds(0.3f);
        Bind();
        var rt = _camera.targetTexture;
        Check(T1AtmosScenery.Built && !T1FrontLevel.Built, "Atmos ativo sem terreno legado");
        Check(rt.width == 768 && rt.height == 432 && rt.filterMode == FilterMode.Point && !rt.useMipMap
            && rt.antiAliasing == 1 && Mathf.Abs(_camera.orthographicSize - 6.75f) < 0.001f, "Apresentação 768x432 Point Ortho 6.75 preservada");
        Check(_stage.HalfWidth == 36f && AtmosFrontLevel.Gaps.Count == 2 && AtmosFrontLevel.Platforms.Count == 3,
            "72 unidades, somente dois gaps e três plataformas");
        foreach (var pair in new[] { new Vector2(-28.5f,-26f),new Vector2(-21.625f,-18.5f) })
        {
            bool hit = EnemyTraversal.SurfaceAt((pair.x + pair.y) * 0.5f, _stage.GroundTop + 0.1f, 4f, out _);
            Check(!hit, "Sem chão invisível no gap " + pair.x);
        }
        Check(EnemyTraversal.SurfaceAt(32f, _stage.GroundTop + 0.1f, 0.3f, out _), "Extensão final sólida preserva acesso ao portal");
        Check(GameObject.Find("Plataformas") == null && GameObject.Find("Chao") == null,
            "Raízes e chão clássico ausentes");
        foreach (var name in new[] { "layer_p1","layer_p2","overlay_p2b_fog","layer_p3","overlay_p3b_veil","layer_p4","layer_p5","ground","plat_s","plat_m","plat_l","sky","far","mid","ruins","playfield_p5","preview","preview_x2" })
        {
            var texture = Resources.Load<Texture2D>(T1AtmosScenery.Folder + name);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            Check(texture != null && importer != null && importer.spritePixelsPerUnit == 32 && importer.filterMode == FilterMode.Point
                && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                "Import nativo: " + name);
        }
        var layers = FindObjectsByType<AtmosV1Parallax>(FindObjectsSortMode.None);
        Check(layers.Length == 6, "P1-P4 e dois overlays independentes");
        bool snapped = true, coverage = true;
        foreach (var layer in layers)
            foreach (float x in new[] { -80f,-24f,0f,24f,80f })
            {
                var camera = new Vector3(x, _stage.GroundTop + 6.25f, -10);
                var pos = layer.PositionFor(camera);
                snapped &= Mathf.Abs(pos.x * 32 - Mathf.Round(pos.x * 32)) < 0.001f && Mathf.Abs(pos.y * 32 - Mathf.Round(pos.y * 32)) < 0.001f;
                coverage &= Mathf.Abs(camera.x - pos.x) <= 12.02f;
            }
        Check(snapped && coverage, "Snap 1/32 e cobertura horizontal das três cópias");
        bool seams = true;
        foreach (var layer in layers)
        {
            foreach (float cameraX in new[] { -24f, 5f, 24f, 80f })
            {
                layer.Apply(new Vector3(cameraX, _stage.GroundTop + 5.5f, -10f));
                var copies = new List<SpriteRenderer>(layer.GetComponentsInChildren<SpriteRenderer>());
                copies.Sort((leftCopy,rightCopy) => leftCopy.transform.localPosition.x.CompareTo(rightCopy.transform.localPosition.x));
                for (int i = 0; i + 1 < copies.Count; i++)
                {
                    var left = copies[i]; var right = copies[i+1];
                    var tex = left.sprite.texture;
                    int lx = left.flipX ? 0 : tex.width-1;
                    int rx = right.flipX ? tex.width-1 : 0;
                    for (int y = 0; y < tex.height; y++) seams &= tex.GetPixel(lx,y) == tex.GetPixel(rx,y);
                }
            }
        }
        Check(seams, "Bordas das cópias coincidem antes e depois da reciclagem");
        PlaceFeet(-31, _stage.GroundTop);
        yield return new WaitForSeconds(0.2f);
        Check(Mathf.Abs(_box.bounds.min.y - _stage.GroundTop) <= 1f / 32f, "Pés alinhados ao chão (até um pixel)");
        var renderers = _player.GetComponentsInChildren<SpriteRenderer>(true);
        var enabledStates = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) { enabledStates[i] = renderers[i].enabled; renderers[i].enabled = false; }
        Capture("referencia_sem_player", true);
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = enabledStates[i];
        Capture("inicio", true);

        // Saltos reais com o mesmo corpo, gravidade e velocidade usados pelo PlayerController.
        foreach (var gap in AtmosFrontLevel.Gaps)
        {
            PlaceFeet(gap.x - 1.25f, _stage.GroundTop);
            yield return new WaitForSeconds(0.15f);
            _body.linearVelocity = new Vector2(_player.MoveSpeed, T1AtmosScenery.PlayerJumpSpeed);
            for (float t = 0; t < 1.03f; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
            Check(_body.position.x > gap.y && _box.bounds.min.y >= _stage.GroundTop - 0.05f, "Salto atravessa gap " + gap.x);
        }
        // A parte do chão; B parte da borda de A; C parte de B, sem mover a geometria.
        var a = AtmosFrontLevel.Platforms[0]; var b = AtmosFrontLevel.Platforms[1]; var c = AtmosFrontLevel.Platforms[2];
        PlaceFeet((a.Left + a.Right) * 0.5f, _stage.GroundTop);
        yield return new WaitForSeconds(0.15f);
        _body.linearVelocity = new Vector2(0, T1AtmosScenery.PlayerJumpSpeed);
        yield return new WaitForSeconds(1.05f);
        Check(Mathf.Abs(_box.bounds.min.y - a.Top) < 0.04f && _player.Grounded, "A alcançável do chão e pouso alinhado");
        Capture("plat_A", true);
        PlaceFeet(a.Right - 0.02f, a.Top);
        yield return new WaitForSeconds(0.1f);
        _body.linearVelocity = new Vector2(_player.MoveSpeed, T1AtmosScenery.PlayerJumpSpeed);
        yield return new WaitForSeconds(0.94f);
        _body.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.2f);
        Check(Mathf.Abs(_box.bounds.min.y - b.Top) < 0.04f, "B alcançável a partir de A");
        Capture("plat_B", true);
        PlaceFeet(b.Right - 0.35f, b.Top);
        yield return new WaitForSeconds(0.1f);
        _body.linearVelocity = new Vector2(3.2f, T1AtmosScenery.PlayerJumpSpeed);
        yield return new WaitForSeconds(1.02f);
        _body.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.15f);
        Check(Mathf.Abs(_box.bounds.min.y - c.Top) < 0.04f, "C alcançável a partir de B");
        Capture("plat_C", true);
        // O contato pela face inferior não é apoio.
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.position = new Vector2((b.Left+b.Right)*0.5f, b.Top - 0.45f + RootAboveFeet);
        _body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
        Check(!_player.Grounded, "Face inferior de one-way não renova apoio");
        _body.bodyType = RigidbodyType2D.Dynamic;

        foreach (var data in new[] { EnemyData.CreateAlienSkeleton(),EnemyData.CreateBeast(),EnemyData.CreateSlateGolem() })
        {
            Check(!EnemyTraversal.TrySpawnPoint(-27.25f, _stage, data.Size, out _), data.DisplayName + " rejeita spawn no gap");
            Check(EnemyTraversal.TrySpawnPoint(20, _stage, data.Size, out _), data.DisplayName + " aceita extensão sólida");
            PlaceFeet(c.Right - 0.7f, c.Top);
            var enemy = SimpleEnemySpawner.SpawnAt(data, _player.transform, _stage, new Vector2(a.Left-2f, _stage.GroundTop+0.02f), _box);
            enemy.Health.Configure(1000);
            bool reached = false;
            for (float t = 0; t < 10f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                if (enemy == null) break;
                if (enemy.transform.position.y > c.Top - 0.1f && enemy.GetComponent<EnemyTraversal>().Grounded) { reached = true; break; }
            }
            Check(reached, data.DisplayName + " alcança C navegando pelas ilhas");
            if (enemy != null) Destroy(enemy.gameObject);
            foreach (var projectile in FindObjectsByType<AetherBone>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
            yield return null;
        }
        // As duas mortes usam a UI real do controlador e ignoram i-frames.
        for (int index = 0; index < 2; index++)
        {
            var gap = AtmosFrontLevel.Gaps[index];
            PlaceFeet((gap.x+gap.y)*0.5f, _stage.GroundTop+1f);
            yield return new WaitForSeconds(0.3f);
            Capture("gap_"+(index+1), true);
            yield return new WaitForSecondsRealtime(1f);
            bool message = false;
            foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
                if (text.text == "VOCÊ CAIU" && text.gameObject.activeInHierarchy) message = true;
            Check(_player.GetComponent<HealthSystem>().IsDead && message && Time.timeScale == 0f,
                "Gap "+(index+1)+" é mortal com painel VOCÊ CAIU");
            if (index == 0)
            {
                Time.timeScale = 1;
                var operation = SceneManager.LoadSceneAsync("Ground_T1");
                while (!operation.isDone) yield return null;
                GroundT1Controller.EnsureExists();
                yield return new WaitForSeconds(0.3f);
                Bind();
            }
        }
    }
}
#endif
