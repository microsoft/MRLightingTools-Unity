# Mixed Reality Lighting Tools

## Features

Light Capture is a tool for estimating and replicating the current environment's light! As the user moves through their environment, their device will take pictures and build an internal representation of the lighting, which it then feeds into Unity's lighting system for use with your own preference of shaders.

![Building up a Cubemap, live!](/Documentation/Images/LightEstimationHow.gif)

Also included is a set of shaders that are streamlined for fast rendering of the lighting data. They're great if all you need is something that fits in with the lighting! They're also pretty simple and slim, so they're good to build on top of.

## Usage

From the dropdown menu:
>Mixed Reality Toolkit / Lighting Tools / Create Light Capture Object

That's it! This will create a `GameObject` in your scene with the `LightCapture` component on it, this should work on the HoloLens automatically. If you want this example to work in-editor on a laptop that has a camera and a gyroscope, add the `FollowGyroscope` component to your scene's `Main Camera`.

You can check out the `LightingTools_Capture` scene for an example of the bare minimum required for light capture to work.

## Installation and Configuration

Grab a .unitypackage over in the [releases](https://github.com/Microsoft/MRLightingTools-Unity/releases) tab, or copy the `MixedRealityToolkit.LightingTools` folder into your project's Assets folder. Please use Unity 2018.3 or greater, and ensure your application has permission to access the WebCam!

MR Lighting Tools does not require the [Mixed Reality Toolkit](https://github.com/Microsoft/MixedRealityToolkit-Unity), but does work well with it!

## Configuration for Example Scene Functionality

For the `LightingTools_Demo` scene running on HoloLens, permissions for Microphone (voice commands) and PicturesLibrary (saving active Cubemap to user's photo library) should be enabled.

The Demo scene uses the Spatial Mapping Collider for placing objects on surfaces in HoloLens. For this to work you need the WMR package:
>Package Manager->Show Preview Packages->Windows Mixed Reality->Install

For ARFoundation functionality, please make sure you add the ARFoundation package, along with the associated platform specific AR packge:
>Package Manager->Show Preview Packages->AR Foundation->Install<br/>
>Package Manager->Show Preview Packages->ARCore XR Plugin->Install

## How Does it Work?

As the user interacts with the environment, this tool creates a Cubemap using the device's camera. This Cubemap is then assigned to one of Unity's Reflection Probes for use by shaders for ambient light and reflection calculations!

When the component loads, it takes a single picture from the camera, and wraps it around the entire Cubemap! This provides an initial, immediate estimate of the lighting in the room that can be improved upon over time.

As the user rotates, the component will 'stamp' the current camera image onto the Cubemap, and save that rotation to a cache. As the user continues to rotate, the component will check the cache to see if there's already a stamp there, before adding another stamp. Settings can be configured to make stamps expire as the user moves from room to room.

**NOTE:** On HoloLens, the camera is locked to a specific exposure to accurately reflect lighting changes in each direction! Other devices **do not** do this due to API availability, which leads to a more even, muddy Cubemap. So, if your lighting captures don't look great on your non-HoloLens device, that would be why.

**NOTE:** Light Capture uses the camera for core functionality, and may interrupt, or be interrupted by other camera activities, such as marker tracking like Vuforia, or MRC (Mixed Reality Capture) camera streaming.

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

The lighting information works out of the box with Unity's Standard, Legacy, and Mobile shaders, as well as the MRTK Standard shaders!

While it is possible to use Unity's `Standard` shaders, we recommend using the `LightCapture IBL`, or `MRTK Standard` shaders, as they'll be much faster! The `LightCapture IBL` shader is a great reference of what features need to be present for Light Capture to work. Since it has more limited functionality, it may be even a little faster than `MRTK Standard`.

## Included Tools
### Camera Cubemap Creator

>Mixed Reality Toolkit / Light Capture / Camera Cubemap Creator

This is an editor window for putting together Cubemaps outside of runtime for debugging and fixed locations. It's a little buggy, but can be used to make some good stuff! It saves a Cubemap format .png to the Asset folder.

### Save Cubemap From Probe

>Mixed Reality Toolkit / Light Capture / Save Cubemap from Probe

If you're in the editor during runtime, and want to save the current Reflection Probe, use this menu item! Also saves a Cubemap format .png to the Asset folder.

### Save Cubemap on HoloLens

The 'Demo' scene can save a Cubemap .png from the HoloLens to its picture folder using the 'save' command word. You can check the `LightPreviewController.SaveMap()` method for an implementation of the HoloLens save functionality if you want to trigger it from a different command.

This is the best way to get a test Cubemap, as the HoloLens' ability to lock the camera exposure will result in a better image!

# Feedback

To file issues or suggestions, please use the [Issues](https://github.com/Microsoft/MRLightingTools-Unity/issues) page for this project on GitHub.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
