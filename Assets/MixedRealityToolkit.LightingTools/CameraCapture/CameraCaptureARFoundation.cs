// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if USE_ARFOUNDATION

using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// An <see cref="ICameraCapture"/> service that works with an AR Foundation camera.
    /// </summary>
    public class CameraCaptureARFoundation : ICameraCapture
    {
        #region Member Variables
        /// <summary>Preferred resolution for taking pictures, note that resolutions are not guaranteed! Refer to CameraResolution for details.</summary>
        private CameraResolution resolution;
        /// <summary>Texture cache for storing captured images.</summary>
        private Texture2D captureTex;
        /// <summary>Is this ICameraCapture ready for capturing pictures?</summary>
        private bool ready = false;

        private ARCameraManager cameraManager;
        private Matrix4x4 lastProjMatrix;
        private Matrix4x4 lastDisplayMatrix;
        #endregion // Member Variables

        #region Overrides / Event Handlers
        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            if (args.projectionMatrix.HasValue)
                lastProjMatrix = args.projectionMatrix.Value;
            if (args.displayMatrix.HasValue)
                lastDisplayMatrix = args.displayMatrix.Value;
        }
        #endregion // Overrides / Event Handlers

        #region Public Methods
        /// <inheritdoc/>
        public Task InitializeAsync(bool preferGPUTexture, CameraResolution preferredResolution)
        {
            // Store values
            resolution = preferredResolution;
            lastProjMatrix = Matrix4x4.identity;
            lastDisplayMatrix = Matrix4x4.identity;

            // Before moving on, make sure we have an AR camera manager
            cameraManager = GameObject.FindObjectOfType<ARCameraManager>();
            if (cameraManager == null)
            {
                throw new InvalidOperationException("CameraCapture needs an ARCameraManager to be present in the scene!");
            }

            // Subscribe to camera frame changes
            cameraManager.frameReceived += OnFrameReceived;

            // Create a completion source to handle AR Foundation initializing asynchronously
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            // Create a handler for the AR Session to start
            Action<ARSessionStateChangedEventArgs> handler = null;
            handler = (aState) =>
            {
                // Camera and orientation data aren't ready until ARFoundation is actually tracking!
                if (aState.state == ARSessionState.SessionTracking)
                {
                    // Now that we're tracking, the handler is no longer needed
                    ARSession.stateChanged -= handler;

                    // And we're ready
                    ready = true;

                    // Signal complete
                    tcs.SetResult(true);
                }
            };

            // Subscribe to session state changes
            ARSession.stateChanged += handler;

            // Return the completion source task to allow someone to await initialization
            return tcs.Task;
        }

        /// <summary>
        /// Gets the image data from ARFoundation, preps it, and drops it into captureTex.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that yields <c>true</c> if the capture was successful; otherwise <c>false</c>.
        /// </returns>
        private Task<bool> GrabScreenAsync()
        {
            // Grab the latest image from ARFoundation
            XRCameraImage image;
            if (!cameraManager.TryGetLatestImage(out image))
            {
                Debug.LogError("[CameraCaptureARFoundation] Could not get latest image!");
                Task.FromResult<bool>(false);
            }

            // Set up resizing parameters
            Vector2Int size = resolution.AdjustSize(new Vector2Int(image.width, image.height));
            var conversionParams = new XRCameraImageConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(size.x, size.y),
                outputFormat = TextureFormat.RGB24,
                transformation = CameraImageTransformation.MirrorY,
            };

            // make sure we have a texture to store the resized image
            if (captureTex == null || captureTex.width != size.x || captureTex.height != size.y)
            {
                if (captureTex != null)
                {
                    GameObject.Destroy(captureTex);
                }
                captureTex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            }

            // Create a completion source to wait for the async operation
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            // And do the resize!
            image.ConvertAsync(conversionParams, (status, p, data) =>
            {
                if (status == AsyncCameraImageConversionStatus.Ready)
                {
                    captureTex.LoadRawTextureData(data);
                    captureTex.Apply();
                }
                if (status == AsyncCameraImageConversionStatus.Ready || status == AsyncCameraImageConversionStatus.Failed)
                {
                    image.Dispose();

                    // TODO: Should we log the failure or fail the task? Previously this completed no matter what.
                    tcs.SetResult(status == AsyncCameraImageConversionStatus.Ready);
                }
            });

            // Return the completion source task so callers can await
            return tcs.Task;
        }

        /// <summary>
        /// Gets the camera's current transform in Unity coordinates, including any magic or trickery for offsets from ARFoundation.
        /// </summary>
        /// <returns>Camera's current transform in Unity coordinates.</returns>
        private Matrix4x4 GetCamTransform()
        {
            Matrix4x4 matrix = lastDisplayMatrix;

            // This matrix transforms a 2D UV coordinate based on the device's orientation.
            // It will rotate, flip, but maintain values in the 0-1 range. This is technically
            // just a 3x3 matrix stored in a 4x4

            // These are the matrices provided in specific phone orientations:

            #if UNITY_ANDROID

            // 1 0 0 Landscape Left (upside down)
            // 0 1 0
            // 0 0 0
            if (Mathf.RoundToInt(matrix[0,0]) == 1 && Mathf.RoundToInt(matrix[1,1]) == 1)
            {
                matrix = Matrix4x4.Rotate( Quaternion.Euler(0,0,180) );
            }

            //-1 0 1 Landscape Right
            // 0-1 1
            // 0 0 0
            else if (Mathf.RoundToInt(matrix[0,0]) == -1 && Mathf.RoundToInt(matrix[1,1]) == -1)
            {
                matrix = Matrix4x4.identity;
            }

            // 0 1 0 Portrait
            //-1 0 1
            // 0 0 0
            else if (Mathf.RoundToInt(matrix[0,1]) == 1 && Mathf.RoundToInt(matrix[1,0]) == -1)
            {
                matrix = Matrix4x4.Rotate( Quaternion.Euler(0,0,90) );
            }

            // 0-1 1 Portrait (upside down)
            // 1 0 0
            // 0 0 0
            else if (Mathf.RoundToInt(matrix[0,1]) == -1 && Mathf.RoundToInt(matrix[1,0]) == 1)
            {
                matrix = Matrix4x4.Rotate( Quaternion.Euler(0,0,-90) );
            }

            #elif UNITY_IOS

            // 0-.6 0 Portrait
            //-1  1 0 The source image is upside down as well, so this is identity
            // 1 .8 1
            if (Mathf.RoundToInt(matrix[0,0]) == 0)
            {
                matrix = Matrix4x4.Rotate( Quaternion.Euler(0,0,90) );
            }

            //-1  0 0 Landscape Right
            // 0 .6 0
            // 1 .2 1
            else if (Mathf.RoundToInt(matrix[0,0]) == -1)
            {
                matrix = Matrix4x4.identity;
            }

            // 1  0 0 Landscape Left
            // 0-.6 0
            // 0 .8 1
            else if (Mathf.RoundToInt(matrix[0,0]) == 1)
            {
                matrix = Matrix4x4.Rotate( Quaternion.Euler(0,0,180) );
            }

            // iOS has no upside down?
            #else

            if (true)
            {
            }
            #endif

            else
            {
#pragma warning disable 0162
                Debug.LogWarningFormat("Unexpected Matrix provided from ARFoundation!\n{0}", matrix.ToString());
#pragma warning restore 0162
            }

            return Camera.main.transform.localToWorldMatrix * matrix;
        }

        /// <inheritdoc/>
        public async Task<ColorResult> RequestColorAsync()
        {
            Matrix4x4 transform = GetCamTransform();
            await GrabScreenAsync();
            return new ColorResult(transform, captureTex);
        }

        /// <inheritdoc/>
        public async Task<TextureResult> RequestTextureAsync()
        {
            Matrix4x4 transform = GetCamTransform();
            await GrabScreenAsync();
            return new TextureResult(transform, captureTex);
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (captureTex != null)
            {
                GameObject.Destroy(captureTex);
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <inheritdoc/>
        public float FieldOfView
        {
            get
            {
                float fov = Mathf.Atan(1.0f / lastProjMatrix[1, 1]) * 2.0f * Mathf.Rad2Deg;
                return fov;
            }
        }

        /// <inheritdoc/>
        public bool IsReady
        {
            get
            {
                return ready;
            }
        }

        /// <inheritdoc/>
        public bool IsRequestingImage
        {
            get
            {
                return false;
            }
        }
        #endregion // Public Properties
    }
}
#endif