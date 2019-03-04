using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VignetteQuad: MonoBehaviour {
	[SerializeField, Range(0,1)] float _brightness;
    [SerializeField, Range(0, 1)] float _fade = 0.2f;

    MeshFilter _filter;
	MeshRenderer _renderer;

    void Start () {
		_filter   = GetComponent<MeshFilter>();
		_renderer = GetComponent<MeshRenderer>();

        if (_filter.sharedMesh == null)
		    _filter.sharedMesh = new Mesh();
        _filter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
        Mesh mesh = _filter.sharedMesh;
        CreatePlane(ref mesh, _fade);
		
		_renderer.sharedMaterial = new Material(Shader.Find("Unlit/Vignette"));
		_renderer.sharedMaterial.color = new Color(_brightness, _brightness, _brightness, 1);
	}
    
	Mesh CreatePlane(ref Mesh m, float aBlend = 0.2f) {
		m.vertices = new Vector3[] { 
			new Vector3(-1,1,0), new Vector3(1,1,0), new Vector3(1,-1,0), new Vector3(-1,-1,0),
			new Vector3(-1 + aBlend, 1-aBlend*2, 0), new Vector3(1-aBlend, 1-aBlend*2, 0), new Vector3(1-aBlend, -1+aBlend*2, 0), new Vector3(-1+aBlend, -1+aBlend*2, 0) };
		m.colors = new Color[] { new Color(1,1,1,0), new Color(1,1,1,0), new Color(1,1,1,0), new Color(1,1,1,0),
			Color.white, Color.white, Color.white, Color.white };
		m.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
			new Vector2(aBlend/2, aBlend), new Vector2(1-aBlend/2, aBlend), new Vector2(1-aBlend/2, 1-aBlend), new Vector2(aBlend/2, 1-aBlend)};
		m.triangles = new int[] {
			0,1,5,  0,5,4,
			5,1,2,  5,2,6,
			6,2,7,  7,2,3,
			7,3,0,  7,0,4,
			4,5,6,  4,6,7};
		m.bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));

		return m;
	}

	private void OnValidate()
	{
		if (_renderer != null)
			_renderer.sharedMaterial.color = new Color(_brightness, _brightness, _brightness, 1);

        if (_filter != null) { 
            if (_filter.sharedMesh == null)
                _filter.sharedMesh = new Mesh();
            Mesh mesh = _filter.sharedMesh;
            CreatePlane(ref mesh, _fade);
        }
    }
}
