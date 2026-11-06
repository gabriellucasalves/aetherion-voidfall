using System.Collections.Generic;
using UnityEngine;

public class AtmosV1Root : MonoBehaviour
{
    readonly List<Material> _materials = new List<Material>();
    public void Own(Material material) => _materials.Add(material);
    void OnDestroy()
    {
        T1AtmosScenery.ClearIfOwner(gameObject);
        foreach (var material in _materials) if (material != null) Destroy(material);
    }
}
