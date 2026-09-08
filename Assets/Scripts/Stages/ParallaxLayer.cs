using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    float _factor;
    Vector3 _origin;

    public void Setup(float factor)
    {
        _factor = factor;
        _origin = transform.position;
    }

    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        var cam = camera.transform.position;
        transform.position = new Vector3(
            _origin.x + cam.x * _factor,
            _origin.y + cam.y * _factor * 0.18f,
            _origin.z);
    }
}
