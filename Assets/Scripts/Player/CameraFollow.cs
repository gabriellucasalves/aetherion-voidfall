using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Transform _target;
    StageData _stage;
    Camera _camera;
    float _lookX;
    const float Smooth = 7.5f;
    const float LookAhead = 2.4f;

    bool _lockedX;
    float _lockMinX;
    float _lockMaxX;
    float _lockedY;
    bool _hasLockedY;

    public void Configure(Transform target, StageData stage)
    {
        _target = target;
        _stage = stage;
        _camera = GetComponent<Camera>();
        if (_target == null)
            return;

        var position = Clamped(TargetPoint());
        transform.position = (T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built) ? AtmosV1Parallax.Snap(position) : position;
    }

    public void SetTarget(Transform target)
    {
        Configure(target, _stage);
    }

    public void LockX(float minX, float maxX, float? fixedY = null)
    {
        _lockedX = true;
        _lockMinX = minX;
        _lockMaxX = maxX;
        if (fixedY.HasValue)
        {
            _hasLockedY = true;
            _lockedY = fixedY.Value;
        }
        else
        {
            _hasLockedY = false;
        }
    }

    public void UnlockX()
    {
        _lockedX = false;
        _hasLockedY = false;
    }

    void LateUpdate()
    {
        if (_target == null)
            return;

        var next = Vector3.Lerp(transform.position, Clamped(TargetPoint()), Smooth * Time.deltaTime);
        transform.position = (T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built) ? AtmosV1Parallax.Snap(next) : next;
    }

    Vector3 TargetPoint()
    {
        var player = _target.GetComponent<PlayerController>();
        float desiredLook = player != null ? player.Facing.x * LookAhead : 0f;
        _lookX = Mathf.Lerp(_lookX, desiredLook, 3.2f * Time.deltaTime);
        float y = _hasLockedY ? _lockedY : (_target.position.y + 1.15f);
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

        if (_lockedX)
        {
            float lMin = _lockMinX + viewHalfW;
            float lMax = _lockMaxX - viewHalfW;
            if (lMin > lMax)
            {
                minX = maxX = (_lockMinX + _lockMaxX) * 0.5f;
            }
            else
            {
                minX = lMin;
                maxX = lMax;
            }
        }
        else if (minX > maxX)
        {
            minX = 0f;
            maxX = 0f;
        }

        float minY = _stage.GroundTop + ((T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built) ? T1AtmosScenery.GroundBelowCenter : viewHalfH * 0.55f);
        float maxY = _stage.GroundTop + 8.5f;

        point.x = Mathf.Clamp(point.x, minX, maxX);
        point.y = _hasLockedY ? _lockedY : Mathf.Clamp(point.y, minY, maxY);
        point.z = -10f;
        return point;
    }
}
