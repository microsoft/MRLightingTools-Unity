// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// An <see cref="ICameraCapture"/> service that works with Screen Capture.
    /// </summary>
    public class CameraCaptureScreen : ICameraCapture
    {
        #region Member Variables
        /// <summary>Which screen are we rendering?</summary>
        private Camera sourceCamera;
        /// <summary>Preferred resolution for taking pictures, note that resolutions are not guaranteed! Refer to CameraResolution for details.</summary>
        private CameraResolution resolution;
        /// <summary>Cache tex for storing the screen.</summary>
        private Texture2D captureTex = null;
        /// <summary>Is this ICameraCapture ready for capturing pictures?</summary>
        private bool ready = false;
        /// <summary>For controlling which render layers get rendered for this capture.</summary>
        private int renderMask = ~(1 << 31);
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="CameraCaptureScreen"/>.
        /// </summary>
        /// <param name="sourceCamera">
        /// Which screen are we rendering?
        /// </param>
        /// <param name="renderMask">
        /// For controlling which render layers get rendered for this capture.
        /// </param>
        public CameraCaptureScreen(Camera sourceCamera, int renderMask = ~(1 << 31))
        {
            this.sourceCamera = sourceCamera;
            this.renderMask = renderMask;
        }
        #endregion // Constructors

        #region Internal Methods
        /// <summary>
        /// Render the current scene to a texture.
        /// </summary>
        /// <param name="aSize">Desired size to render.</param>
        private void GrabScreen(Vector2Int aSize)
        {
            if (captureTex == null || captureTex.width != aSize.x || captureTex.height != aSize.y)
            {
                if (captureTex != null)
                {
                    GameObject.Destroy(captureTex);
                }
                captureTex = new Texture2D(aSize.x, aSize.y, TextureFormat.RGB24, false);
            }
            RenderTexture rt = RenderTexture.GetTemporary(aSize.x, aSize.y, 24);
            int oldMask = sourceCamera.cullingMask;
            sourceCamera.targetTexture = rt;
            sourceCamera.cullingMask = renderMask;
            sourceCamera.Render();

            RenderTexture.active = rt;
            captureTex.ReadPixels(sourceCamera.pixelRect, 0, 0, false);
            captureTex.Apply();
            sourceCamera.targetTexture = null;
            sourceCamera.cullingMask = oldMask;
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(rt);
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <inheritdoc/>
        public Task InitializeAsync(bool preferGPUTexture, CameraResolution preferredResolution)
        {
            // Store
            resolution = preferredResolution;
            ready = true;

            // Already complete
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<ColorResult> RequestColorAsync()
        {
            Vector2Int size = resolution.AdjustSize(new Vector2Int(sourceCamera.pixelWidth, sourceCamera.pixelHeight));
            GrabScreen(size);
            return Task.FromResult(new ColorResult(sourceCamera.transform.localToWorldMatrix, captureTex));
        }

        /// <inheritdoc/>
        public Task<TextureResult> RequestTextureAsync()
        {
            Vector2Int size = resolution.AdjustSize(new Vector2Int(sourceCamera.pixelWidth, sourceCamera.pixelHeight));
            GrabScreen(size);
            return Task.FromResult(new TextureResult(sourceCamera.transform.localToWorldMatrix, captureTex));
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
                return Camera.main.fieldOfView;
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