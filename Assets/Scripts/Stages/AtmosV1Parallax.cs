using UnityEngine;

// Factor é a velocidade aparente relativa ao mundo: P5=1 permanece fixo no mundo.
// Logo a compensação da câmera é (1-factor). Tudo é amostrado na grade de 1/32 u.
[DefaultExecutionOrder(100)]
public class AtmosV1Parallax : MonoBehaviour
{
    public const float TileWidth = 24f;
    float _factor;
    Vector2 _reference;
    bool _sky;
    SpriteRenderer[] _copies;
    public float Factor => _factor;

    public void Setup(float factor, Vector2 reference, bool sky)
    {
        _factor = factor;
        _reference = reference;
        _sky = sky;
        _copies = GetComponentsInChildren<SpriteRenderer>();
        Follow();
    }
    public static float Snap(float value) => Mathf.Round(value * 32f) / 32f;
    public static Vector3 Snap(Vector3 value) => new Vector3(Snap(value.x), Snap(value.y), value.z);

    public Vector3 PositionFor(Vector3 cameraPosition)
    {
        float x = _reference.x + (cameraPosition.x - _reference.x) * (1f - _factor);
        // Mantém as três cópias centradas perto da câmera; o salto ocorre fora do viewport.
        x += Mathf.Round((cameraPosition.x - x) / TileWidth) * TileWidth;
        // O céu opaco mantém cobertura vertical completa. Outros planos têm paralaxe vertical.
        float y = _sky ? cameraPosition.y : _reference.y + (cameraPosition.y - _reference.y) * (1f - _factor);
        return Snap(new Vector3(x, y, 0f));
    }
    public void Apply(Vector3 cameraPosition)
    {
        transform.position = PositionFor(cameraPosition);
        float baseX = _reference.x + (cameraPosition.x - _reference.x) * (1f - _factor);
        int index = Mathf.RoundToInt((cameraPosition.x - baseX) / TileWidth);
        // Alterna cópias, mantendo o índice global na reciclagem. A borda toca sua própria
        // borda espelhada, evitando costuras sem pintar ou filtrar o PNG aprovado.
        foreach (var renderer in _copies)
        {
            int copy = Mathf.RoundToInt(renderer.transform.localPosition.x / TileWidth);
            renderer.flipX = ((index + copy) & 1) != 0;
        }
    }
    void LateUpdate() => Follow();
    void Follow()
    {
        if (Camera.main != null) Apply(Camera.main.transform.position);
    }
}
