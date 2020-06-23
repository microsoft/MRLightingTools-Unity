// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    public class LightCapture : MonoBehaviour, ICameraControl
    {
        #region Member Variables
        /// <summary> Interface to the camera we're using. This is different on different platforms. </summary>
        private ICameraCapture cameraCapture;

        /// <summary> Interface to control advanced camera settings, if supported. </summary>
        private ICameraControl cameraControl;

        /// <summary> The Cubemap tool we're using to generate the light information for Unity's probes. </summary>
        private CubeMapper map = null;
        /// <summary> Histogram of the Cubemap's colors. Used for primary light source color calculations. </summary>
        private Histogram histogram = new Histogram();
        /// <summary> Used to track the original skybox material, in case we want to disable light capture and restore state. </summary>
        private Material startSky;
        private Quaternion startLightRot;
        private Color startLightColor;
        private float startLightBrightness;
        /// <summary> The number of stamps currently in our cubemap.  Used for singleStampOnly option. </summary>
        private int stampCount;

        // For easing the light directions
        private Quaternion lightTargetDir;
        private Quaternion lightStartDir;
        private float lightStartTime = -1;
        private float lightTargetDuration;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Settings")]
        [Tooltip("Resolution (pixels) per-face of the generated lighting Cubemap.")]
        [SerializeField] private int mapResolution = 128;

        [Header("Stamp Optimizations")]
        [Tooltip("Should the component only do the initial wraparound stamp? If true, only one picture will be taken, at the very beginning.")]
        [SerializeField] private bool singleStampOnly = false;
        [Tooltip("When stamping a camera picture onto the Cubemap, scale it up by this so it covers a little more space. This can mean fewer total stamps needed to complete the Cubemap, at the expense of a less perfect reflection.")]
        [SerializeField, Range(1, 2)] private float stampFovMultiplier = 1f;
        [Tooltip("This is the distance (meters) the camera must travel for a stamp to expire. When a stamp expires, the Camera will take another picture in that direction when given the opportunity. Zero means no expiration.")]
        [SerializeField] private float stampExpireDistance = 0;

        [Header("Directional Lighting")]
        [Tooltip("Should the system calculate information for a directional light? This will scrape the lower mips of the Cubemap to find the direction and color of the brightest values, and apply it to the scene's light.")]
        [SerializeField] private bool useDirectionalLight = true;
        [Tooltip("When finding the primary light color, it will average the brightest 20% of the pixels, and use that color for the light. This sets the cap for the saturation of that color.")]
        [SerializeField, Range(0, 1)] private float maxLightColorSaturation = 0.3f;
        [Tooltip("The light eases into its new location when the information is updated. This is the speed at which it eases to its new destination, measured in degrees per second.")]
        [SerializeField] private float lightAngleAdjustPerSecond = 45f;

        [Header("Optional Overrides")]
        [Tooltip("Defaults to Camera.main. Which object should we be looking at for our orientation and position?")]
        [SerializeField] private Transform cameraOrientation;
        [Tooltip("Default will pick from the scene, or create one automatically. If you have settings you'd like to configure on your probe, hook it in here.")]
        [SerializeField] private ReflectionProbe probe;
        [Tooltip("Default will pick the first directional light in the scene. If no directional light is found, one will be created!")]
        [SerializeField] private Light directionalLight;
        #endregion // Unity Inspector Variables

        #region Private Methods
        private async Task TakeStampAsync()
        {
            var result = await cameraCapture.RequestTextureAsync();
            var texture = result.Texture;
            var matrix = result.Matrix;

            map.Stamp(texture, matrix.GetColumn(3), matrix.rotation, matrix.MultiplyVector(Vector3.forward));
            stampCount += 1;

            DynamicGI.UpdateEnvironment();

            if (useDirectionalLight)
            {
                UpdateDirectionalLight();
            }
        }

        private void UpdateDirectionalLight()
        {
            if (directionalLight == null || map == null)
            {
                return;
            }

            // Calculate the light direction
            Vector3 dir = map.GetWeightedDirection(ref histogram);
            dir.y = Mathf.Abs(dir.y); // Don't allow upward facing lights! In many cases, 'light' from below is just a large surface that reflects from an overhead source

            // Prevent zero vectors, Unity doesn't like them.
            if (dir.sqrMagnitude < 0.000001f)
                dir = Vector3.up;

            if (lightStartTime < 0 || lightAngleAdjustPerSecond == 0)
            {
                directionalLight.transform.forward = -dir;
                lightStartTime = 0;
            }
            else
            {
                lightTargetDir = Quaternion.LookRotation(-dir);
                lightStartDir = directionalLight.transform.localRotation;
                lightStartTime = Time.time;
                lightTargetDuration = Quaternion.Angle(lightTargetDir, lightStartDir) / lightAngleAdjustPerSecond;

                if (lightTargetDuration <= 0)
                {
                    directionalLight.transform.forward = -dir;
                    lightStartTime = 0;
                }
            }

            // Calculate a color and intensity from the cubemap's histogram

            // grab the color for the brightest 20% of the image
            float bright = histogram.FindPercentage(.8f);
            Color color = histogram.GetColor(bright, 1);

            // For the final color, we use a 'value' of 1, since we use the intensity for brightness of the light source.
            // Also, light sources are rarely very saturated, so we cap that as well
            float hue, sat, val;
            Color.RGBToHSV(color, out hue, out sat, out val);
            directionalLight.color = Color.HSVToRGB(hue, Mathf.Min(sat, maxLightColorSaturation), 1);
            directionalLight.intensity = bright;
        }

        /// <summary>
        /// Warns if the the current camera does not support advanced control.
        /// </summary>
        /// <returns>
        /// <c>true</c> if statement if a warning was generated; otherwise <c>false</c>.
        /// </returns>
        private bool WarnIfNoControl()
        {
            if (cameraControl == null)
            {
                Debug.LogWarning($"{nameof(LightCapture)} {name} - The current system does not support advanced camera control.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Warns if the system is not ready to perform an action.
        /// </summary>
        /// <returns>
        /// <c>true</c> if statement if a warning was generated; otherwise <c>false</c>.
        /// </returns>
        private bool WarnIfNotReady()
        {
            if (cameraCapture == null)
            {
                Debug.LogWarning($"{nameof(LightCapture)} {name} is not enabled or has not been initialized.");
                return true;
            }
            return false;
        }
        #endregion

        #region Unity Overrides
        protected virtual void Awake ()
        {
            // Save initial settings in case we wish to restore them
            startSky = RenderSettings.skybox;

            // Pick camera based on platform
            #if WINDOWS_UWP && !UNITY_EDITOR
            cameraCapture = new CameraCaptureUWP();
            #elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            captureCamera = new CameraCaptureARFoundation();
            #else
            // On a desktop computer, or certain laptops, we may not have access to any cameras.
            if (WebCamTexture.devices.Length <= 0)
            {
                // Alternatively, you can simulate a camera by taking screenshots instead.
                // captureCamera = new CameraCaptureScreen(Camera.main);

                // When disabling and returning immediately, OnDisable will be called without
                // OnEnable ever getting called.
                enabled = false;
                return;
            }
            else
            {
                cameraCapture = new CameraCaptureWebcam(Camera.main.transform, Camera.main.fieldOfView);
            }
            #endif

            // Try to get camera control as well
            cameraControl = cameraCapture as ICameraControl;

            // Make sure we have access to a probe in the scene
            if (probe == null)
            {
                probe = FindObjectOfType<ReflectionProbe>();
            }
            if (probe == null)
            {
                GameObject probeObj = new GameObject("_LightCaptureProbe", typeof(ReflectionProbe));
                probeObj.transform.SetParent(transform, false);
                probe = probeObj.GetComponent<ReflectionProbe>();

                probe.size          = Vector3.one * 10000;
                probe.boxProjection = false;
            }

            // Same with a camera object
            if (cameraOrientation == null)
            {
                cameraOrientation = Camera.main.transform;
            }

            // And check for a directional light in the scene
            if (useDirectionalLight && directionalLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        directionalLight = lights[i];
                        break;
                    }
                }
                if (directionalLight == null)
                {
                    GameObject lightObj = new GameObject("_DirectionalLight", typeof(Light));
                    lightObj.transform.SetParent(transform, false);
                    directionalLight = lightObj.GetComponent<Light>();
                    directionalLight.type = LightType.Directional;
                }
            }

            // Save initial light settings
            if (directionalLight != null)
            {
                startLightColor      = directionalLight.color;
                startLightRot        = directionalLight.transform.rotation;
                startLightBrightness = directionalLight.intensity;
            }
        }

        protected virtual void OnDisable()
        {
            // Restore and render a default probe
            if (probe != null && probe.isActiveAndEnabled)
            {
                probe.mode        = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
                probe.RenderProbe();
            }

            RenderSettings.skybox = startSky;

            if (directionalLight != null)
            {
                directionalLight.color              = startLightColor;
                directionalLight.transform.rotation = startLightRot;
                directionalLight.intensity          = startLightBrightness;
            }

            if (cameraCapture != null)
                cameraCapture.Shutdown();

            DynamicGI.UpdateEnvironment();
        }

        protected virtual async void OnEnable()
        {
            // Save initial settings in case we wish to restore them
            startSky = RenderSettings.skybox;
            // Save initial light settings
            if (directionalLight != null)
            {
                startLightColor      = directionalLight.color;
                startLightRot        = directionalLight.transform.rotation;
                startLightBrightness = directionalLight.intensity;
                UpdateDirectionalLight();
            }

            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;

            CameraResolution resolution = new CameraResolution();
            resolution.nativeResolution = NativeResolutionMode.Smallest;
            resolution.resize           = ResizeWhen.Never;

            await cameraCapture.InitializeAsync(true, resolution);

            if (map == null)
            {
                map = new CubeMapper();
                map.Create(cameraCapture.FieldOfView * stampFovMultiplier, mapResolution);
                map.StampExpireDistance = stampExpireDistance;
            }
            probe.customBakedTexture = map.Map;
            RenderSettings.skybox = map.SkyMaterial;
            RenderSettings.ambientMode = AmbientMode.Skybox;


            DynamicGI.UpdateEnvironment();
        }

        protected virtual void OnValidate()
        {
            if (map != null)
            {
                map.StampExpireDistance = stampExpireDistance;
            }
        }

        protected virtual void Update ()
        {
            // ditch out if we already have our first stamp
            if ((stampCount > 0 && singleStampOnly) )
            {
                return;
            }

            // check the cache to see if our current orientation would benefit from a new stamp
            if (map != null && cameraCapture.IsReady && !cameraCapture.IsRequestingImage)
            {
                if (!map.IsCached(cameraOrientation.position, cameraOrientation.forward))
                {
                    // Take stamp, but do not await. Let it run blindly so we don't block the
                    // rest of our Update loop
                    var t = TakeStampAsync();
                }
            }

            // If we have a target light rotation to get to, start lerping!
            if (lightStartTime > 0)
            {
                float t =  Mathf.Clamp01( (Time.time - lightStartTime) / lightTargetDuration );
                if (t >= 1)
                {
                    lightStartTime = 0;
                }

                // This is a cheap cubic in/out easing function, so we aren't doing this linear (ew)
                t = t<.5f ? 4*t*t*t : (t-1)*(2*t-2)*(2*t-2)+1;
                directionalLight.transform.localRotation = Quaternion.Lerp(lightStartDir, lightTargetDir, t);
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <inheritdoc/>
        public Task SetExposureAsync(double exposure)
        {
            if (WarnIfNotReady()) { return Task.CompletedTask; }
            if (WarnIfNoControl()) { return Task.CompletedTask; }
            return cameraControl.SetExposureAsync(exposure);
        }

        /// <inheritdoc/>
        public Task SetWhiteBalanceAsync(uint temperature)
        {
            if (WarnIfNotReady()) { return Task.CompletedTask; }
            if (WarnIfNoControl()) { return Task.CompletedTask; }
            return cameraControl.SetWhiteBalanceAsync(temperature);
        }

        /// <inheritdoc/>
        public Task SetISOAsync(uint iso)
        {
            if (WarnIfNotReady()) { return Task.CompletedTask; }
            if (WarnIfNoControl()) { return Task.CompletedTask; }
            return cameraControl.SetISOAsync(iso);
        }

        /// <summary> Clear the internal representation of light, and start over again from scratch. </summary>
        public void Clear()
        {
            if (map != null) { map.Clear(); }
            stampCount = 0;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Direct access to the CubeMapper class we're using!
        /// </summary>
        public CubeMapper CubeMapper
        {
            get
            {
                return map;
            }
        }
        #endregion // Public Properties
    }
}