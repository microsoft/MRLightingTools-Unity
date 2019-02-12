# Light Estimation
## Basic Usage

From the dropdown menu:
>Mixed Reality Toolkit / Light Estimation / Create Estimation Object

That's it! This will create a `GameObject` in your scene with the `Lightcapture` component on it, this should work on the HoloLens automatically. If you want this example to work in-editor on a laptop that has a camera and a gyroscope, add the `FollowGyroscope` component to your scene's `Main Camera`.

You can check out the 'Minimum' scene for an example of the bare minimum require for light estimation to work.

## Project Configuration

Please use Unity 2018.2 or greater, and ensure your application has permission to access the WebCam.

Light Estimation has no hard dependencies on MRTK, so if you wish to use it separately from MRTK, just copy these folders into your own project:
>MixedRealityToolkit-Preview / LightEstimation<br/>
>MixedRealityToolkit-Preview / CameraCapture

## Configuration for Example Scene Functionality

For the 'Demo' scene running on HoloLens, permissions for Microphone (voice commands) and PicturesLibrary (saving active Cubemap to user's photo library) should be enabled.

The Demo scene uses the Spatial Mapping Collider for placing objects on surfaces in HoloLens. For this to work you need the WMR package:
>Package Manager->Show Preview Packages->Windows Mixed Reality->Install

For ARFoundation functionality, please make sure you add the ARFoundation package, along with the associated platform specific AR packge:
>Package Manager->Show Preview Packages->AR Foundation->Install<br/>
>Package Manager->Show Preview Packages->ARCore XR Plugin->Install

**NOTE:** This project is capable of using a laptop's gyroscope in the Unity Editor through [UnityEditorGyro](https://github.com/maluoi/UnityEditorGyro) to emulate the head rotation of a tracked device. The pre-compiled DLLs will not be included until they are properly signed, but you can add them yourself in the meantime! This can be quite helpful for debugging or development. Check the comments at the top of [EditorGyro.cs](/Assets/MixedRealityToolkit-Preview/LightEstimation-Examples/EditorGyro/EditorGyro.cs) in the project for details!

## How Does it Work?

![Building up a Cubemap, live!](/External/ReadMeImages/LightEstimationHow.gif)

As the user interacts with the environment, this tool creates a Cubemap using the device's camera. This Cubemap is then assigned to one of Unity's Reflection Probes for use by shaders for ambient light and reflection calculations!

When the component loads, it takes a single picture from the camera, and wraps it around the entire Cubemap! This provides an initial, immediate estimate of the lighting in the room that can be improved upon over time.

As the user rotates, the component will 'stamp' the current camera image onto the Cubemap, and save that rotation to a cache. As the user continues to rotate, the component will check the cache to see if there's already a stamp there, before adding another stamp. Settings can be configured to make stamps expire as the user moves from room to room.

**NOTE:** On HoloLens, the camera is locked to a specific exposure to accurately reflect lighting changes in each direction! Other devices **do not** do this due to API availability, which leads to a more even, muddy Cubemap. So, if your lighting captures don't look great on your non-HoloLens device, that would be why.

**NOTE:** Light Estimation uses the camera for core functionality, and may interrupt, or be interrupted by other camera activities, such as marker tracking like Vuforia, or MRC (Mixed Reality Capture) camera streaming.

## LightCapture Settings

- **Map Resolution**
Resolution (pixels) per-face of the generated lighting Cubemap.
- **Single Stamp Only**
Should the component only do the initial wraparound stamp? If true, only one picture will be taken, at the very beginning.
- **Stamp FOV Multiplier**
When stamping a camera picture onto the Cubemap, scale it up by this so it covers a little more space. This can mean fewer total stamps needed to complete the Cubemap, at the expense of a less perfect reflection.
- **Stamp Expire Distance**
This is the distance (meters) the camera must travel for a stamp to expire. When a stamp expires, the Camera will take another picture in that direction when given the opportunity. Zero means no expiration.

- **Use Directional Lighting**
Should the system calculate information for a directional light? This will scrape the lower mips of the Cubemap to find the direction and color of the brightest values, and apply it to the scene's light.
- **Max Light Color Saturation**
When finding the primary light color, it will average the brightest 20% of the pixels, and use that color for the light. This sets the cap for the saturation of that color.
- **Light Angle Adjust Per Second**
The light eases into its new location when the information is updated. This is the speed at which it eases to its new destination, measured in degrees per second.

## Shaders

The ambient lighting information works out of the box with Unity's Standard, Legacy, and Mobile shaders, as well as the MRTK Standard shaders! Reflections only work with the `Standard` and `MRTK Standard` shaders. Also included is a lightweight `LightEstimation IBL` shader that does normal + diffuse textures with ambient lighting (no reflections).

While the demo uses Unity's `Standard` shaders for project portability, we recommend using the `MRTK Standard` shaders, as they'll be significantly faster! The `LightEstimation IBL` shader is a great reference of what features need to be present for Light Estimation to work. Since it has more limited functionality, it may be even a little faster than `MRTK Standard`.

## Included Tools
### Camera Cubemap Creator

>Mixed Reality Toolkit / Light Estimation / Camera Cubemap Creator

This is an editor window for putting together Cubemaps outside of runtime for debugging and fixed locations. It's a little buggy, but can be used to make some good stuff! It saves a Cubemap format .png to the Asset folder.

### Save Cubemap From Probe

>Mixed Reality Toolkit / Light Estimation / Save Cubemap from Probe

If you're in the editor during runtime, and want to save the current Reflection Probe, use this menu item! Also saves a Cubemap format .png to the Asset folder.

### Save Cubemap on HoloLens

The 'Demo' scene can save a Cubemap .png from the HoloLens to its picture folder using the 'save' command word. You can check the `LightPreviewController.SaveMap()` method for an implementation of the HoloLens save functionality if you want to trigger it from a different command.

This is the best way to get a test Cubemap, as the HoloLens' ability to lock the camera exposure will result in a better image!