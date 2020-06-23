// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// The result of a camera capture as colors.
    /// </summary>
    public class ColorResult : TextureResult
    {
        /// <summary>
        /// Initialize a new <see cref="ColorResult"/> from a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="matrix">
        /// The camera matrix.
        /// </param>
        /// <param name="texture">
        /// The texture.
        /// </param>
        public ColorResult(Matrix4x4 matrix, Texture2D texture) : base(matrix, texture)
        {
            Colors = texture.GetRawTextureData<Color24>();
        }

        /// <summary>
        /// Initialize a new <see cref="ColorResult"/> from a <see cref="Texture"/> and a color array.
        /// </summary>
        /// <param name="matrix">
        /// The camera matrix.
        /// </param>
        /// <param name="texture">
        /// The texture.
        /// </param>
        /// <param name="colors">
        /// The manually supplied color array.
        /// </param>
        public ColorResult(Matrix4x4 matrix, Texture texture, NativeArray<Color24> colors) : base(matrix, texture)
        {
            this.Colors = colors;
        }

        /// <summary>
        /// Gets the colors for the result.
        /// </summary>
        public NativeArray<Color24> Colors { get; }
    }

    /// <summary>
    /// The result of a camera capture as a texture.
    /// </summary>
    public class TextureResult
    {
        /// <summary>
        /// Initialize a new <see cref="TextureResult"/>.
        /// </summary>
        /// <param name="matrix">
        /// The camera matrix.
        /// </param>
        /// <param name="texture">
        /// The texture.
        /// </param>
        public TextureResult(Matrix4x4 matrix, Texture texture)
        {
            Matrix = matrix;
            Texture = texture;
        }

        /// <summary>
        /// Gets the camera matrix for the result.
        /// </summary>
        public Matrix4x4 Matrix { get; }

        /// <summary>
        /// Gets the texture for the result.
        /// </summary>
        public Texture Texture { get; }
    }

    /// <summary>
    /// The interface for a camera capture service.
    /// </summary>
    public interface ICameraCapture
    {
        #region Public Methods
        /// <summary>
        /// Initializes a device's camera and finds appropriate picture settings based on the provided resolution.
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
        Task InitializeAsync(bool preferGPUTexture, CameraResolution preferredResolution);

        /// <summary>
        /// Requests an image from the camera and provide it as a Texture on the GPU and array of Colors on the CPU.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        Task<ColorResult> RequestColorAsync();

        /// <summary>
        /// Requests an image from the camera and provides it as a Texture on the GPU.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that yields the <see cref="TextureResult"/>.
        /// </returns>
        Task<TextureResult> RequestTextureAsync();

        /// <summary>
        /// Done with the camera, free up resources!
        /// </summary>
        void Shutdown();
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Is the camera completely initialized and ready to begin taking pictures?
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Is the camera currently already busy with taking a picture?
        /// </summary>
        bool IsRequestingImage { get; }

        /// <summary>
        /// Field of View of the camera in degrees. This value is never ready until after
        /// initialization, and in many cases, isn't accurate until after a picture has
        /// been taken. It's best to check this after each picture if you need it.
        /// </summary>
        float FieldOfView { get; }
        #endregion // Public Properties
    }
}