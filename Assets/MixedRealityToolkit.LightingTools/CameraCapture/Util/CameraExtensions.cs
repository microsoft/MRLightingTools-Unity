using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Windows.WebCam;
using static UnityEngine.Windows.WebCam.PhotoCapture;

namespace Microsoft.MixedReality.Toolkit.LightingTools
{
    /// <summary>
    /// Return data for the
    /// <see cref="CameraExtensions.TakePhotoAsync(PhotoCapture, CameraParameters)"/>
    /// extension method.
    /// </summary>
    public class TakePhotoResult
    {
        public TakePhotoResult(PhotoCaptureResult captureResult, PhotoCaptureFrame frame)
        {
            CaptureResult = captureResult;
            Frame = frame;
        }

        /// <summary>
        /// Gets the camera matrix for the result.
        /// </summary>
        public PhotoCaptureResult CaptureResult { get; }

        /// <summary>
        /// Gets the texture for the result.
        /// </summary>
        public PhotoCaptureFrame Frame { get; }
    }

    static public class CameraExtensions
    {
        /// <summary>
        /// <see cref="PhotoCapture.CreateAsync(bool, OnCaptureResourceCreatedCallback)"/> as a task.
        /// </summary>
        /// <param name="camera">
        /// The <see cref="PhotoCapture"/> camera.
        /// </param>
        /// <param name="showHolograms">
        /// Whether or not to show holograms during capture.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that yields the created camera.
        /// </returns>
        static public Task<PhotoCapture> CreateAsync(bool showHolograms)
        {
            // Create a completion source
            var tcs = new TaskCompletionSource<PhotoCapture>();

            // Start the callback version
            PhotoCapture.CreateAsync(false, captureObject =>
            {
                tcs.SetResult(captureObject);
            });

            // Return the running task from the completion source
            return tcs.Task;
        }

        /// <summary>
        /// <see cref="PhotoCapture.StartPhotoModeAsync(CameraParameters, OnPhotoModeStartedCallback)"/> as a task.
        /// </summary>
        /// <param name="camera">
        /// The <see cref="PhotoCapture"/> camera.
        /// </param>
        /// <param name="setupParams">
        /// The setup parameters.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        static public Task<PhotoCaptureResult> StartPhotoModeAsync(this PhotoCapture camera, CameraParameters setupParams)
        {
            // Validate
            if (camera == null) throw new ArgumentNullException(nameof(camera));

            // Create a completion source
            var tcs = new TaskCompletionSource<PhotoCaptureResult>();

            // Start the callback version
            camera.StartPhotoModeAsync(setupParams, startResult =>
            {
                if (startResult.success)
                {
                    tcs.SetResult(startResult);
                }
                else
                {
                    tcs.SetException(Marshal.GetExceptionForHR((int)startResult.hResult));
                }
            });

            // Return the running task from the completion source
            return tcs.Task;
        }

        /// <summary>
        /// <see cref="PhotoCapture.StopPhotoModeAsync(OnPhotoModeStoppedCallback)"/> as a task.
        /// </summary>
        /// <param name="camera">
        /// The <see cref="PhotoCapture"/> camera.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        static public Task<PhotoCaptureResult> StopPhotoModeAsync(this PhotoCapture camera)
        {
            // Validate
            if (camera == null) throw new ArgumentNullException(nameof(camera));

            // Create a completion source
            var tcs = new TaskCompletionSource<PhotoCaptureResult>();

            // Start the callback version
            camera.StopPhotoModeAsync(stopResult =>
            {
                if (stopResult.success)
                {
                    tcs.SetResult(stopResult);
                }
                else
                {
                    tcs.SetException(Marshal.GetExceptionForHR((int)stopResult.hResult));
                }
            });

            // Return the running task from the completion source
            return tcs.Task;
        }

        /// <summary>
        /// <see cref="PhotoCapture.TakePhotoAsync(string, PhotoCaptureFileOutputFormat, OnCapturedToDiskCallback)"/> as a task.
        /// </summary>
        /// The <see cref="PhotoCapture"/> camera.
        /// The PhotoCapture camera.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        static public Task<TakePhotoResult> TakePhotoAsync(this PhotoCapture camera)
        {
            // Validate
            if (camera == null) throw new ArgumentNullException(nameof(camera));

            // Create a completion source
            var tcs = new TaskCompletionSource<TakePhotoResult>();

            // Start the callback version
            camera.TakePhotoAsync((captureResult, frame) =>
            {
                if (captureResult.success)
                {
                    tcs.SetResult(new TakePhotoResult(captureResult, frame));
                }
                else
                {
                    tcs.SetException(Marshal.GetExceptionForHR((int)captureResult.hResult));
                }
            });

            // Return the running task from the completion source
            return tcs.Task;
        }
    }
}
