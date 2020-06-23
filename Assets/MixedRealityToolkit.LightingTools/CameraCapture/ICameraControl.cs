// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// The interface for advanced camera control services.
    /// </summary>
    public interface ICameraControl
    {
        /// <summary>
        /// Manually override the camera's exposure.
        /// </summary>
        /// <param name="exposure">
        /// This value can range from 0.0 to 1.0 inclusive. It will be scaled to the devices max exposure range.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        Task SetExposureAsync(double exposure);

        /// <summary>
        /// Manually override the camera's white balance.
        /// </summary>
        /// <param name="temperature">
        /// White balance temperature in kelvin.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        /// <remarks>
        /// Though specified in kelvin, not all camera controllers follow the spec exactly.
        /// </remarks>
        Task SetWhiteBalanceAsync(uint temperature);

        /// <summary>
        /// Manually override the camera's ISO.
        /// </summary>
        /// <param name="iso">
        /// Camera's sensitivity to light.
        /// </param>
        /// <remarks>
        /// ISO is similar to gain.
        /// </remarks>
        Task SetISOAsync(uint iso);
    }
}