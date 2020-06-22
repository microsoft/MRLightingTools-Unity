// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#if WINDOWS_UWP
using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Windows.WebCam;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// An <see cref="ICameraCapture"/> service that works with UWP Media Capture.
    /// </summary>
    public class CameraCaptureUWP : ICameraCapture, ICameraControl
    {
        #region Member Variables
        private Texture2D cacheTex = null;
        private PhotoCapture camera = null;
        private CameraParameters cameraParams;
        private double exposure = 0.2f;
        private float fieldOfView = 45;
        private bool hasWarnedCameraMatrix = false;
        private Task initializeTask = null;
        private uint iso = 800;
        private Task<TextureResult> requestImageTask;
        private CameraResolution resolution = null;
        private Texture2D resizedTex = null;
        private uint temperature = 5000;
        private VideoDeviceControllerWrapperUWP wrapper;
        #endregion // Member Variables

        #region Internal Methods
        /// <summary>
        /// Captures an image from the camera.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that yields a <see cref="TextureResult"/>.
        /// </returns>
        private async Task<TextureResult> CaptureImageAsync()
        {
            // Make sure we're initialized before proceeding
            EnsureInitialized();

            // Wait for the camera to start photo mode
            await camera.StartPhotoModeAsync(cameraParams);

            // Update camera values (which can only be done while streaming)
            await wrapper.SetExposureAsync(exposure);
            await wrapper.SetWhiteBalanceAsync(temperature);
            await wrapper.SetISOAsync(iso);

            // Take a picture and get the result
            var takeResult = await camera.TakePhotoAsync();

            // Camera matrix is broken in some unity builds
            // See: https://forum.unity.com/threads/locatable-camera-not-working-on-hololens-2.831514/

            // Shortcut to frame
            var frame = takeResult.Frame;

            // Grab the camera matrix
            Matrix4x4 transform;
            if ((frame.hasLocationData) && (frame.TryGetCameraToWorldMatrix(out transform)))
            {
                transform[0, 2] = -transform[0, 2];
                transform[1, 2] = -transform[1, 2];
                transform[2, 2] = -transform[2, 2];
            }
            else
            {
                if (!hasWarnedCameraMatrix)
                {
                    hasWarnedCameraMatrix = true;
                    Debug.LogWarning($"{nameof(CameraCaptureUWP)} can't get camera matrix. Falling back to main camera.");
                }
                transform = Camera.main.transform.localToWorldMatrix;
            }

            Matrix4x4 proj;
            if (!frame.TryGetProjectionMatrix(out proj))
            {
                fieldOfView = Mathf.Atan(1.0f / proj[0, 0]) * 2.0f * Mathf.Rad2Deg;
            }

            frame.UploadImageDataToTexture(cacheTex);
            Texture tex = resizedTex;
            resolution.ResizeTexture(cacheTex, ref tex, true);
            resizedTex = (Texture2D)tex;

            // Wait for camera to stop
            await camera.StopPhotoModeAsync();

            // Pass on results
            return new TextureResult(transform, resizedTex);
        }

        /// <summary>
        /// Ensures that the camera system has been fully initialized, otherwise throws an exception.
        /// </summary>
        private void EnsureInitialized()
        {
            // Ensure initialized
            if (!IsReady) throw new InvalidOperationException($"{nameof(InitializeAsync)} must be completed first.");
        }

        /// <summary>
        /// Internal version of initialize. Should not be called more than once unless Shutdown has been called.
        /// </summary>
        /// <param name="preferGPUTexture">
        /// Whether GPU textures are preferred over NativeArray of colors. Certain optimizations may be present to take advantage of this preference.
        /// </param>
        /// <param name="preferredResolution">
        /// Preferred resolution for taking pictures. Note that resolutions are not guaranteed! Refer to CameraResolution for details.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        private async Task InnerInitializeAsync(bool preferGPUTexture, CameraResolution preferredResolution)
        {
            // Store preferred resolution
            resolution = preferredResolution;

            // Find the nearest supported camera resolution to the preferred one
            Resolution cameraResolution = resolution.nativeResolution == NativeResolutionMode.Smallest ?
                                          PhotoCapture.SupportedResolutions.OrderBy((res) => res.width * res.height).First() :
                                          PhotoCapture.SupportedResolutions.OrderBy((res) => -res.width * res.height).First();

            // Create the texture cache
            cacheTex = new Texture2D(cameraResolution.width, cameraResolution.height);

            // Setup parameters for the camera
            cameraParams = new CameraParameters();
            cameraParams.hologramOpacity = 0.0f;
            cameraParams.cameraResolutionWidth = cameraResolution.width;
            cameraParams.cameraResolutionHeight = cameraResolution.height;
            cameraParams.pixelFormat = CapturePixelFormat.BGRA32;

            // Create the PhotoCapture camera
            camera = await CameraExtensions.CreateAsync(false);

            // Create the wrapper
            IntPtr unknown = camera.GetUnsafePointerToVideoDeviceController();
            wrapper = new VideoDeviceControllerWrapperUWP(unknown);
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <inheritdoc/>
        public Task InitializeAsync(bool preferGPUTexture, CameraResolution preferredResolution)
        {
            // Make sure not initialized or initializing
            if (initializeTask != null) throw new InvalidOperationException("Already initializing.");

            // Now initializing
            initializeTask = InnerInitializeAsync(preferGPUTexture, preferredResolution);

            // Return task in process
            return initializeTask;
        }

        /// <inheritdoc/>
        public async Task<ColorResult> RequestColorAsync()
        {
            // Call texture overload
            var textureResult = await RequestTextureAsync();

            // Return color result
            Texture2D t2d = textureResult.Texture as Texture2D;
            if (t2d != null)
            {
                return new ColorResult(textureResult.Matrix, t2d);
            }
            throw new NotSupportedException("This device does not support requesting colors.");
        }

        /// <inheritdoc/>
        public Task<TextureResult> RequestTextureAsync()
        {
            // Make sure we're initialized before proceeding
            EnsureInitialized();

            // If already requesting, just wait for it to complete
            if (requestImageTask != null && !requestImageTask.IsCompleted)
            {
                return requestImageTask;
            }
            else
            {
                requestImageTask = CaptureImageAsync();
                return requestImageTask;
            }
        }

        /// <inheritdoc/>
        public async Task SetExposureAsync(double exposure)
        {
            // Validate
            if ((exposure < 0.0) || (exposure > 1.0)) { throw new ArgumentOutOfRangeException(nameof(exposure)); }

            // Save value
            this.exposure = exposure;

            // If we're capturing, update right away
            if (IsRequestingImage)
            {
                // Pass to wrapper
                await wrapper.SetExposureAsync(exposure);
            }
        }

        /// <inheritdoc/>
        public async Task SetWhiteBalanceAsync(uint temperature)
        {
            // Save value
            this.temperature = temperature;

            // If we're capturing, update right away
            if (IsRequestingImage)
            {
                // Pass to wrapper
                await wrapper.SetWhiteBalanceAsync(temperature);
            }
        }

        /// <inheritdoc/>
        public async Task SetISOAsync(uint iso)
        {
            // Save value
            this.iso = iso;

            // If we're capturing, update right away
            if (IsRequestingImage)
            {
                // Pass to wrapper
                await wrapper.SetISOAsync(iso);
            }
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (wrapper != null)
            {
                wrapper.Dispose();
                camera = null;
            }

            if (cacheTex   != null)
            {
                GameObject.Destroy(cacheTex);
                cacheTex = null;
            }

            if (resizedTex != null)
            {
                GameObject.Destroy(resizedTex);
                resizedTex = null;
            }

            requestImageTask = null;
            initializeTask = null;
        }
        #endregion // Public Methods

        #region Public Properties
        /// <inheritdoc/>
        public float FieldOfView => fieldOfView;

        /// <inheritdoc/>
        public bool IsReady => (initializeTask != null && initializeTask.IsCompleted && !initializeTask.IsFaulted);

        /// <inheritdoc/>
        public bool IsRequestingImage => (requestImageTask != null && !requestImageTask.IsCompleted);
        #endregion // Public Properties
    }
}
#endif // WINDOWS_UWP