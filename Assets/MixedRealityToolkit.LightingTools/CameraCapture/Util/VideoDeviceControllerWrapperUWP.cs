// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#if WINDOWS_UWP
using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;

using System.Runtime.InteropServices;

using Windows.Media.Devices;
using System.Threading.Tasks;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// A wrapper for setting camera exposure settings in UWP.
    /// </summary>
    sealed class VideoDeviceControllerWrapperUWP : IDisposable
    {
        /// <summary>PhotoCapture native object.</summary>
        private VideoDeviceController controller = null;
        /// <summary>IDisposable pattern</summary>
        private bool                  disposed   = false;

        /// <param name="unknown">Pointer to a PhotoCapture camera.</param>
        public VideoDeviceControllerWrapperUWP(IntPtr unknown)
        {
            controller = (VideoDeviceController)Marshal.GetObjectForIUnknown(unknown);
        }

        ~VideoDeviceControllerWrapperUWP()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public async Task SetExposureAsync(double exposure)
        {
            // Validate the range
            if ((exposure < 0.0) || (exposure > 1.0)) { throw new ArgumentOutOfRangeException(nameof(exposure)); }

            // Shortcuts
            var exp = controller.Exposure;
            var expc = controller.ExposureControl;

            // Try to use exposure control
            if (expc.Supported)
            {
                // Debug.Log("Setting exposure using ExposureControl.");

                // If not in manual mode, attempt to put it in manual mode
                if (expc.Auto)
                {
                    try
                    {
                        await expc.SetAutoAsync(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set exposure mode to manual using ExposureControl. {ex}");
                    }
                }

                // Wrap in case of bad controller implementation
                try
                {
                    // Get total possible exposure range as a TimeSpan
                    TimeSpan range = (expc.Max - expc.Min);

                    // Convert exposure to part of total time
                    double exppart = range.TotalMilliseconds * exposure;

                    // Convert exposure to TimeSpan (starting with Minimum and adding to it)
                    TimeSpan exptarget = expc.Min + TimeSpan.FromMilliseconds(exppart);

                    // Make sure we don't go outside the range
                    if (exptarget < expc.Min) { exptarget = expc.Min; }
                    if (exptarget > expc.Max) { exptarget = expc.Max; }

                    // Update the controller
                    // Debug.Log($"Exposure Values - Min: {expc.Min}, Max: {expc.Max}, Input: {exposure}, Target: {exptarget}");
                    await expc.SetValueAsync(exptarget);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set exposure to {exposure}. {ex}");
                }
            }

            // Fall back to Exposure
            else if (exp.Capabilities.Supported)
            {
                // Debug.Log("Setting exposure using regular Exposure.");

                // Try to turn off auto exposure
                if (!exp.TrySetAuto(false))
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to turn auto exposure off.");
                }

                // Get total possible exposure range
                double range = (exp.Capabilities.Max - exp.Capabilities.Min);

                // Convert exposure to part of total range
                double exppart = range * exposure;

                // Convert exposure part to a value in exposure range (starting with Minimum and adding to it)
                double exptarget = exp.Capabilities.Min + exppart;

                // Make sure we don't go outside the range
                if (exptarget < exp.Capabilities.Min) { exptarget = exp.Capabilities.Min; }
                if (exptarget > exp.Capabilities.Max) { exptarget = exp.Capabilities.Max; }

                // Update controller
                // Debug.Log($"Exposure Values - Min: {exp.Capabilities.Min}, Max: {exp.Capabilities.Max}, Input: {exposure}, Target: {exptarget}");
                if (!exp.TrySetValue(exptarget))
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set exposure to {exposure}.");
                }
            }

            // No support
            else
            {
                Debug.LogWarning($"{nameof(VideoDeviceControllerWrapperUWP)} current device does not support setting exposure.");
            }
        }

        /// <inheritdoc/>
        public async Task SetWhiteBalanceAsync(uint temperature)
        {
            // Get shortcuts
            var wb = controller.WhiteBalance;
            var wbc = controller.WhiteBalanceControl;

            // Try WhiteBalanceControl first
            if (wbc.Supported)
            {
                // Debug.Log("Setting white balance using WhiteBalanceControl.");

                // If not in manual mode, attempt to put it in manual mode
                if (wbc.Preset != ColorTemperaturePreset.Manual)
                {
                    try
                    {
                        await wbc.SetPresetAsync(ColorTemperaturePreset.Manual);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set white balance mode to manual. {ex}");
                    }
                }

                // Attempt to set the temperature
                try
                {
                    // Make sure the temperature stays within supported range of the controller
                    uint wbtarget = Math.Max(wbc.Min, temperature);
                    wbtarget = Math.Min(wbc.Max, wbtarget);

                    // Update controller
                    // Debug.Log($"White Balance Values - Min: {wbc.Min}, Max: {wbc.Max}, Input: {temperature}, Target: {wbtarget}");
                    await wbc.SetValueAsync(wbtarget);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set white balance to {temperature}. {ex}");
                }
            }

            // Fall back to WhiteBalance
            else if (wb.Capabilities.Supported)
            {
                // Debug.Log("Setting white balance using regular WhiteBalance.");

                // Attempt to put it in manual mode
                if (!wb.TrySetAuto(false))
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to turn auto white balance off.");
                }

                // Make sure the temperature stays within supported range of the controller
                double wbtarget = Math.Max(wb.Capabilities.Min, temperature);
                wbtarget = Math.Min(wb.Capabilities.Max, wbtarget);

                // Update controller
                // Debug.Log($"White Balance Values - Min: {wb.Capabilities.Min}, Max: {wb.Capabilities.Max}, Input: {temperature}, Target: {wbtarget}");
                if (!wb.TrySetValue(wbtarget))
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set white balance to {temperature}.");
                }
            }

            // No support
            else
            {
                Debug.LogWarning($"{nameof(VideoDeviceControllerWrapperUWP)} current device does not support setting white balance.");
            }
        }

        /// <inheritdoc/>
        public async Task SetISOAsync(uint iso)
        {
            // Shortcuts
            var isoc = controller.IsoSpeedControl;

            // Only option is IsoSpeedControl
            if (isoc.Supported)
            {
                // Debug.Log("Setting ISO using IsoSpeedControl.");

                // Wrap in case of bad controller implementation
                try
                {
                    // Make sure the temperature stays within supported range of the controller
                    uint isotarget = Math.Max(isoc.Min, iso);
                    isotarget = Math.Min(isoc.Max, isotarget);

                    // Update the controller
                    // Debug.Log($"ISO Values - Min: {isoc.Min}, Max: {isoc.Max}, Input: {iso}, Target: {isotarget}");
                    await isoc.SetValueAsync(isotarget);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{nameof(VideoDeviceControllerWrapperUWP)} failed to set ISO to {iso}. {ex}");
                }
            }

            // No support
            else
            {
                Debug.LogWarning($"{nameof(VideoDeviceControllerWrapperUWP)} current device does not support ISO speed control.");
            }
        }

        /// <summary>
        /// Dispose of resources!
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// If that's confusing to you too, maybe read this: https://docs.microsoft.com/en-us/dotnet/api/system.idisposable.dispose
        /// </summary>
        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (controller != null)
                {
                    Marshal.ReleaseComObject(controller);
                    controller = null;
                }
                disposed = true;
            }
        }
    }
}
#endif