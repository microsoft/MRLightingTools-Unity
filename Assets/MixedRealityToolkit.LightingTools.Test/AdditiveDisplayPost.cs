using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode(), ]
public class AdditiveDisplayPost : MonoBehaviour
{
    [SerializeField] LayerMask _environment = 0;
    [SerializeField] LayerMask _holograms = 0;

    Material hologramMat;
    Camera cam;
    Camera copy;
    void OnEnable()
    {
        GameObject tmp = new GameObject();
        tmp.hideFlags = HideFlags.HideAndDontSave;
        copy = tmp.AddComponent<Camera>();
        tmp.SetActive(false);

        cam = GetComponent<Camera>();
        hologramMat = new Material(Shader.Find("Hidden/HologramPost"));
    }
    private void OnDisable()
    {
        if (Application.isPlaying) { 
        Destroy(copy.gameObject);
        Destroy(hologramMat);
        } else
        {
            DestroyImmediate(copy.gameObject);
            DestroyImmediate(hologramMat);
        }
    }

    // Update is called once per frame
    void Update()
    {
        cam.cullingMask = _environment;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture tex = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 24, RenderTextureFormat.ARGB32);
        copy.CopyFrom(cam);
        copy.cullingMask = _holograms;
        copy.targetTexture = tex;
        copy.clearFlags = CameraClearFlags.SolidColor;
        copy.backgroundColor = Color.black;
        copy.Render();
        copy.targetTexture = null;
        hologramMat.SetTexture("_HologramTex", tex);
        Graphics.Blit(source, destination, hologramMat);
        RenderTexture.ReleaseTemporary(tex);
    }
}
