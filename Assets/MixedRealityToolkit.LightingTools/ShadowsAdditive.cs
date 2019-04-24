using UnityEngine;

#if USE_UNITY_WMR
using UnityEngine.XR.WSA;
#endif

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ShadowsAdditive: MonoBehaviour {
        [Range(0, 1)]
	    [SerializeField] float _brightness = 0.1f;
        [SerializeField] bool  _captureAndEnableShadowSurfaces = true;

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

        private void Awake()
        {
            if (_captureAndEnableShadowSurfaces)
            {
                #if USE_UNITY_WMR
                SpatialMappingRenderer[] renderers = GameObject.FindObjectsOfType<SpatialMappingRenderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].surfaceParent = gameObject;
                }
                #endif
            }
        }
        private void OnTransformChildrenChanged()
        {
            if (_captureAndEnableShadowSurfaces) { 
                for (int i = 0; i < transform.childCount; i++)
                {
                    MeshRenderer renderer = transform.GetChild(i).GetComponent<MeshRenderer>();
                    if (renderer != null)
                        renderer.receiveShadows = true;
                }
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
}
