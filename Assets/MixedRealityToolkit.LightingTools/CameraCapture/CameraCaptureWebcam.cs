// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// An <see cref="ICameraCapture"/> service that works with a regular webcam.
    /// </summary>
    public class CameraCaptureWebcam : ICameraCapture
    {
        #region Constants
        /// <summary>
        /// The amount of time to wait for a webcam to initialize before frames are served.
        /// </summary>
        /// <remarks>
        /// This delay is necessary because some webcams can take several seconds to fully initialize before sending valid frames.
        /// During this time, many webcams will simply send a black frame. If LightCapture is in Single Frame mode, this will result
        /// in the scene being unlit. Unfortunately there is no know way to query for the webcam to be fully initialized.
        /// </remarks>
        private const float CAMERA_START_DELAY = 2.0f;
        #endregion // Constants

        #region Member Variables
        /// <summary> A transform to reference for the position of the camera. </summary>
        private Transform poseSource = null;
        /// <summary> The WebCamTexture we're using to get a webcam image. </summary>
        private WebCamTexture webcamTex = null;
        /// <summary> Webcam device that we picked to read from. </summary>
        private WebCamDevice device;
        /// <summary> When was this created? WebCamTexture isn't ready right way, so we'll need to wait a bit.</summary>
        private float startTime = 0;
        /// <summary> What field of view should this camera report? </summary>
        private float fieldOfView = 45;
        /// <summary>Preferred resolution for taking pictures, note that resolutions are not guaranteed! Refer to CameraResolution for details.</summary>
        private CameraResolution resolution = null;
        /// <summary>Cache texture for storing the final image.</summary>
        private Texture resizedTexture = null;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="CameraCaptureWebcam"/>.
        /// </summary>
        /// <param name="poseSource">
        /// The transform that serves as the origin and pose of the camera.
        /// </param>
        /// <param name="fieldOfView">
        /// The field of view of the camera.
        /// </param>
        public CameraCaptureWebcam(Transform poseSource, float fieldOfView)
        {
            this.poseSource = poseSource;
            this.fieldOfView = fieldOfView;
        }
        #endregion // Constructors

        #region Public Methods
        /// <inheritdoc/>
        public async Task InitializeAsync(bool preferGPUTexture, CameraResolution preferredResolution)
        {
            // Avoid double initialization
            if (webcamTex != null)
            {
                throw new InvalidOperationException($"{nameof(CameraCaptureWebcam)} {nameof(InitializeAsync)} should only be called once unless it is shut down.");
            }

            // No cameras? Ditch out!
            if (WebCamTexture.devices.Length <= 0)
            {
                Debug.LogWarning($"Could not initialize {nameof(CameraCaptureWebcam)} because there are no webcams attached to the system.");
                return;
            }

            // Store parameters
            resolution = preferredResolution;

            // Find a rear facing camera we can use, or use the first one
            WebCamDevice[] devices = WebCamTexture.devices;
            device = devices[0];
            for (int i = 0; i < devices.Length; i++)
            {
                if (!devices[i].isFrontFacing || devices[i].name.ToLower().Contains("rear"))
                {
                    device = devices[i];
                }
            }

            // Pick a camera resolution
            if (resolution.nativeResolution == NativeResolutionMode.Largest)
            {
                webcamTex = new WebCamTexture(device.name, 10000, 10000, 2);
            }
            else if (resolution.nativeResolution == NativeResolutionMode.Smallest)
            {
                webcamTex = new WebCamTexture(device.name, 1, 1, 2);
            }
            else if (resolution.nativeResolution == NativeResolutionMode.Closest)
            {
                webcamTex = new WebCamTexture(device.name, resolution.size.x, resolution.size.y, 2);
            }
            else
            {
                throw new NotImplementedException(resolution.nativeResolution.ToString());
            }

            // Start the webcam playing
            webcamTex.Play();

            // Bookmark the start time
            startTime = Time.time;

            // Wait for camera to initialize
            await Task.Delay(TimeSpan.FromSeconds(CAMERA_START_DELAY));
        }

        /// <inheritdoc/>
        public Task<ColorResult> RequestColorAsync()
        {
            resolution.ResizeTexture(webcamTex, ref resizedTexture, true);
            Texture2D t2d = resizedTexture as Texture2D;
            if (t2d != null)
            {
                return Task.FromResult(new ColorResult(poseSource == null ? Matrix4x4.identity : poseSource.localToWorldMatrix, t2d));
            }
            throw new NotSupportedException("This device does not support requesting colors.");
        }

        /// <inheritdoc/>
        public Task<TextureResult> RequestTextureAsync()
        {
            resolution.ResizeTexture(webcamTex, ref resizedTexture, false);
            TextureResult result = new TextureResult(poseSource == null ? Matrix4x4.identity : poseSource.localToWorldMatrix, resizedTexture);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (webcamTex != null && webcamTex.isPlaying)
            {
                webcamTex.Stop();
            }
            webcamTex = null;
        }
        #endregion // Public Methods

        #region Public Properties
        /// <inheritdoc/>
        public float FieldOfView
        {
            get
            {
                return fieldOfView;
            }
        }

        /// <inheritdoc/>
        public bool IsReady
        {
            get
            {
                return webcamTex != null && webcamTex.isPlaying && (Time.time - startTime) > CAMERA_START_DELAY;
            }
        }

        /// <inheritdoc/>
        public bool IsRequestingImage
        {
            get;
            private set;
        }
        #endregion // Public Properties
    }
}
