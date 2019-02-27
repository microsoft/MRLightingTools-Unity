using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raycaster
{
    [SerializeField] int _rayLayer;

    public Raycaster(int aRayLayer)
    {
        _rayLayer = aRayLayer;
    }

    public Vector3? Intersect(Vector3 aStart, Vector3 aDirection)
    {
        Vector3? result = null;
        RaycastHit hit;
        if (Physics.Raycast(new Ray(aStart, aDirection), out hit, float.MaxValue, _rayLayer)) {
            result = hit.point;
        }
        return result;
    }
}
