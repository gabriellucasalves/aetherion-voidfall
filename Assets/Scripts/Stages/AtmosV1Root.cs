using UnityEngine;

public class AtmosV1Root : MonoBehaviour
{
    void OnDestroy() => T1AtmosScenery.ClearIfOwner(gameObject);
}
