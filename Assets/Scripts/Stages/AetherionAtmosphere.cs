using UnityEngine;

/// <summary>
/// Adiciona vida atmosférica 16-bit ao cenário:
/// - Partículas de pólen dourado e brisa celeste flutuando suavemente pela fase.
/// - Shimmer nos raios de sol e reflexos de luz.
/// Totalmente 16-bit pixel art, sem artefatos ou pós-processamento pesado.
/// </summary>
public class AetherionAtmosphere : MonoBehaviour
{
    const int MoteCount = 38;
    Transform[] _motes;
    Vector3[] _speeds;
    float[] _phases;
    Camera _cam;
    StageData _stage;

    public static void Attach(StageData stage)
    {
        var go = new GameObject("AetherionAtmosphere");
        var atmos = go.AddComponent<AetherionAtmosphere>();
        atmos._stage = stage;
    }

    void Start()
    {
        _cam = Camera.main;
        _motes = new Transform[MoteCount];
        _speeds = new Vector3[MoteCount];
        _phases = new float[MoteCount];

        // Cria sprite 1x1 / 2x2 para o pólen
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var goldColor = new Color(1f, 0.92f, 0.5f, 0.75f);
        tex.SetPixel(0, 0, goldColor);
        tex.SetPixel(0, 1, goldColor);
        tex.SetPixel(1, 0, goldColor);
        tex.SetPixel(1, 1, new Color(1f, 1f, 0.8f, 0.95f));
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);

        for (int i = 0; i < MoteCount; i++)
        {
            var m = new GameObject("Mote_" + i);
            m.transform.SetParent(transform, false);
            var sr = m.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 18; // na frente do cenário, ao redor do herói

            float startX = Random.Range(-35f, 35f);
            float startY = Random.Range(-2f, 5f);
            m.transform.position = new Vector3(startX, startY, 0f);

            _motes[i] = m.transform;
            _speeds[i] = new Vector3(
                Random.Range(0.25f, 0.65f), // deriva lenta para a direita
                Random.Range(-0.15f, 0.15f),
                0f
            );
            _phases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        float camX = _cam != null ? _cam.transform.position.x : 0f;
        float viewLeft = camX - 16f;
        float viewRight = camX + 16f;

        for (int i = 0; i < MoteCount; i++)
        {
            if (_motes[i] == null) continue;

            _phases[i] += Time.deltaTime * 1.5f;
            var pos = _motes[i].position;

            // Movimento suave ondulante (vento)
            pos.x += _speeds[i].x * Time.deltaTime;
            pos.y += Mathf.Sin(_phases[i]) * 0.35f * Time.deltaTime + _speeds[i].y * Time.deltaTime;

            // Loop ao redor da visão da câmera
            if (pos.x > viewRight)
                pos.x = viewLeft;
            else if (pos.x < viewLeft)
                pos.x = viewRight;

            if (pos.y > 6.0f)
                pos.y = -2.2f;
            else if (pos.y < -2.3f)
                pos.y = 5.8f;

            _motes[i].position = pos;
        }
    }
}
