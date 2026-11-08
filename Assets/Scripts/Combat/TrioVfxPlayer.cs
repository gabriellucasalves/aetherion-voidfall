using UnityEngine;

public class TrioVfxPlayer : MonoBehaviour
{
    Sprite[] _frames;
    SpriteRenderer _renderer;
    float _fps;
    float _age;

    public static void Spawn(string name, Vector3 position, int width, int height,
        int count, Vector2 pivot, float fps, float scale = 1f, float facing = 1f)
    {
        var frames = TrioAnimationFrames.Vfx(name, width, height, count, pivot);
        if (frames == null) return;
        var go = new GameObject("VFX_" + name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(Mathf.Sign(facing) * scale, scale, 1f);
        var player = go.AddComponent<TrioVfxPlayer>();
        player._frames = frames;
        player._fps = fps;
        player._renderer = go.AddComponent<SpriteRenderer>();
        player._renderer.sprite = frames[0];
        player._renderer.sortingOrder = 23;
    }

    void Update()
    {
        _age += Time.deltaTime;
        int frame = Mathf.FloorToInt(_age * _fps);
        if (frame >= _frames.Length) { Destroy(gameObject); return; }
        _renderer.sprite = _frames[frame];
    }
}
