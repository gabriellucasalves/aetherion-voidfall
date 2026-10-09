using UnityEngine;

public class SupremePopup : MonoBehaviour
{
    TextMesh _mesh;
    float _age;
    float _life = 0.55f;
    Color _color;

    public void Kick(Color color)
    {
        _mesh = GetComponent<TextMesh>();
        _color = color;
    }

    void Update()
    {
        _age += Time.deltaTime;
        transform.position += Vector3.up * (1.4f * Time.deltaTime);
        if (_mesh != null)
        {
            var color = _color;
            color.a = 1f - Mathf.Clamp01(_age / _life);
            _mesh.color = color;
        }

        if (_age >= _life)
            Destroy(gameObject);
    }
}
