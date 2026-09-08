using UnityEngine;

public class RainField : MonoBehaviour
{
    Transform _camera;
    Transform[] _drops;
    Vector3[] _vel;
    float _halfW = 14f;
    float _halfH = 8f;

    public void Setup(Transform camera)
    {
        _camera = camera;
        int count = 55;
        _drops = new Transform[count];
        _vel = new Vector3[count];
        var sprite = DropSprite();
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Gota");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.62f, 0.78f, 0.86f, Random.Range(0.1f, 0.26f));
            renderer.sortingOrder = 40;
            _drops[i] = go.transform;
            _vel[i] = new Vector3(-2.8f, -11f - Random.value * 5f, 0f);
            Scatter(i, true);
        }
    }

    void LateUpdate()
    {
        if (_camera == null || _drops == null)
            return;

        var center = _camera.position;
        for (int i = 0; i < _drops.Length; i++)
        {
            _drops[i].position += _vel[i] * Time.deltaTime;
            var p = _drops[i].position;
            if (p.y < center.y - _halfH || p.x < center.x - _halfW)
                Scatter(i, false);
        }
    }

    void Scatter(int i, bool anywhere)
    {
        var center = _camera != null ? _camera.position : Vector3.zero;
        float x = center.x + Random.Range(-_halfW, _halfW);
        float y = anywhere ? center.y + Random.Range(-_halfH, _halfH) : center.y + _halfH;
        _drops[i].position = new Vector3(x, y, 0f);
    }

    static Sprite DropSprite()
    {
        int w = 2;
        int h = 8;
        var pixels = PixelArt.Clear(w, h);
        for (int y = 0; y < h; y++)
        {
            pixels[y * w] = new Color(0.7f, 0.85f, 0.92f, 0.55f);
            pixels[y * w + 1] = new Color(0.55f, 0.7f, 0.8f, 0.2f);
        }

        return PixelArt.Make(w, h, pixels);
    }
}
