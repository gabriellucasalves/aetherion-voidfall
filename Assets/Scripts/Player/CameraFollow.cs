using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Transform _target;
    StageData _stage;
    Camera _camera;
    float _lookX;
    const float Smooth = 7.5f;
    const float LookAhead = 2.4f;

    public void Configure(Transform target, StageData stage)
    {
        _target = target;
        _stage = stage;
        _camera = GetComponent<Camera>();
        if (_target == null)
            return;

        transform.position = Clamped(TargetPoint());
    }

    public void SetTarget(Transform target)
    {
        Configure(target, _stage);
    }

    void LateUpdate()
    {
        if (_target == null)
            return;

        var next = Vector3.Lerp(transform.position, Clamped(TargetPoint()), Smooth * Time.deltaTime);
        transform.position = next;
    }

    Vector3 TargetPoint()
    {
        var player = _target.GetComponent<PlayerController>();
        float desiredLook = player != null ? player.Facing.x * LookAhead : 0f;
        _lookX = Mathf.Lerp(_lookX, desiredLook, 3.2f * Time.deltaTime);
        float y = _target.position.y + 1.15f;
        return new Vector3(_target.position.x + _lookX, y, -10f);
    }

    Vector3 Clamped(Vector3 point)
    {
        if (_stage == null || _camera == null)
            return point;

        float viewHalfH = _camera.orthographicSize;
        float viewHalfW = viewHalfH * _camera.aspect;
        float minX = -_stage.HalfWidth + viewHalfW;
        float maxX = _stage.HalfWidth - viewHalfW;
        float minY = _stage.GroundTop + viewHalfH * 0.55f;
        float maxY = _stage.GroundTop + 8.5f;
        if (minX > maxX)
        {
            minX = 0f;
            maxX = 0f;
        }

        point.x = Mathf.Clamp(point.x, minX, maxX);
        point.y = Mathf.Clamp(point.y, minY, maxY);
        point.z = -10f;
        return point;
    }
}
