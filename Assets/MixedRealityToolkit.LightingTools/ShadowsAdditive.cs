using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShadowsAdditive: MonoBehaviour {
	[SerializeField, Range(0, 1)] float _brightness = 0.1f;

    MeshFilter   _filter;
	MeshRenderer _renderer;
    int          _brightnessId;

    public float Brightness {
        get { return _brightness; }
        set {
            _brightness = value;
            if (_renderer != null)
                _renderer.sharedMaterial.SetFloat(_brightnessId, _brightness);
        }
    }

    void Start ()
    {
		_filter       = GetComponent<MeshFilter>();
		_renderer     = GetComponent<MeshRenderer>();
        _brightnessId = Shader.PropertyToID("_Brightness");

        if (_filter.sharedMesh == null)
		    _filter.sharedMesh = new Mesh();
        Mesh mesh = _filter.sharedMesh;
        CreatePlane(ref mesh);
        
        _renderer.sharedMaterial = new Material(Shader.Find("Hidden/Shadow Screen Vignette"));
        Brightness = _brightness;
    }
    
	Mesh CreatePlane(ref Mesh m)
    {
		m.vertices  = new Vector3[] { new Vector3(-1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0), new Vector3(-1, -1, 0) };
		m.triangles = new int[] { 0,1,2,  0,2,3 };
		m.bounds    = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
		return m;
	}

	private void OnValidate()
	{
		Brightness = _brightness;
    }
}
