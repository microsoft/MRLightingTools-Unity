// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR || WINDOWS_UWP
using UnityEngine.Windows.Speech;
using UnityEngine.XR.WSA.Input;
#endif

namespace Microsoft.MixedReality.Toolkit.LightingTools.Examples
{
    public class LightPreviewController : MonoBehaviour
    {
        #region Constants
        private const string CMD_ADD = "add";
        private const string CMD_BALL = "ball";
        private const string CMD_CIRCLE = "circle";
        private const string CMD_CLEAR = "clear";
        private const string CMD_CUBE = "cube";
        private const string CMD_DISABLE = "disable";
        private const string CMD_ENABLE = "enable";
        private const string CMD_EXPOSURE_DOWN = "exposure down";
        private const string CMD_EXPOSURE_UP = "exposure up";
        private const string CMD_ISO_DOWN = "iso down";
        private const string CMD_ISO_UP = "iso up";
        private const string CMD_RESET = "reset";
        private const string CMD_SAVE = "save";
        private const string CMD_WHITE_DOWN = "white down";
        private const string CMD_WHITE_UP = "white up";

        private const double EXPOSURE_MIN = 0.0; // Min on all devices
        private const double EXPOSURE_MAX = 1.0; // Max on all devices
        private const double EXPOSURE_ADJUST = 0.1;

        private const uint ISO_MIN = 100; // Min on HoloLens 2
        private const uint ISO_MAX = 3200; // Max on HoloLens 2
        private const uint ISO_ADJUST = 100;

        private const uint WB_MIN = 2300; // Min on HoloLens 2
        private const uint WB_MAX = 7500; // Max on HoloLens 2
        private const uint WB_ADJUST = 250;
        #endregion // Constants

        #region Fields
#pragma warning disable 414, 649
        [Header("Scene/asset hooks")]
        [SerializeField] private GameObject   spheres      = null;
        [SerializeField] private GameObject   shaderBalls  = null;
        [SerializeField] private GameObject   cubes        = null;
        [SerializeField] private GameObject[] spawnPrefabs = null;

        [Header("Movement Values")]
        [SerializeField] private float velocityDecay = 4f;
        [SerializeField] private float moveScale     = 1.5f;
        [SerializeField] private float maxVelocity   = 1;
        [SerializeField] private float lerpSpeed     = 4;

        private Vector3 targetPos;
        private Vector3 handPos;
        private bool    pressed;
        private int     addIndex;
        private Vector3 handVelocity;
        private Vector3 lastPos;
        private float   lastTime;

        private double exposure   = 0.2; // Default for HoloLens 2 Indoors
        private uint iso = 800; // Default for HoloLens 2 Indoors
        private uint whitebalance = 5000; // Default for HoloLens 2 Indoors


        private LightCapture lightCapture;
        private Component    tts;
        private MethodInfo   speakMethod;

        #if UNITY_EDITOR || WINDOWS_UWP
        private KeywordRecognizer keywordRecognizer;
        #endif
#pragma warning restore 414, 649
        #endregion

        private void OnEnable()
        {
            #if UNITY_EDITOR || WINDOWS_UWP
            // Setup hand interaction events
            InteractionManager.InteractionSourceLost     += SourceLost;
            InteractionManager.InteractionSourcePressed  += SourcePressed;
            InteractionManager.InteractionSourceReleased += SourceReleased;
            InteractionManager.InteractionSourceUpdated  += SourceUpdated;

            // Setup voice control events
            keywordRecognizer = new KeywordRecognizer(new string[] { CMD_CIRCLE, CMD_BALL, CMD_CUBE, CMD_RESET, CMD_CLEAR, CMD_ENABLE, CMD_DISABLE, CMD_ADD, CMD_EXPOSURE_UP, CMD_EXPOSURE_DOWN, CMD_ISO_UP, CMD_ISO_DOWN, CMD_WHITE_UP, CMD_WHITE_DOWN, CMD_SAVE });
            keywordRecognizer.OnPhraseRecognized += HeardKeyword;
            keywordRecognizer.Start();
            #endif

            // Setup scene objects
            spheres    .SetActive(false);
            shaderBalls.SetActive(true);
            cubes      .SetActive(false);

            targetPos = transform.position;

            // Hook up resources
            lightCapture = FindObjectOfType<LightCapture>();

            // No hard dependence on the MRTK, just reflect into the parts we want to use
            Type textToSpeechType = Type.GetType("HoloToolkit.Unity.TextToSpeech");
            if (textToSpeechType != null)
            {
                tts = (Component)FindObjectOfType(textToSpeechType);
                speakMethod = textToSpeechType.GetMethod("StartSpeaking");
            }
        }
        private void OnDisable()
        {
            #if UNITY_EDITOR || WINDOWS_UWP
            // Remove hand event hooks
            InteractionManager.InteractionSourceLost     -= SourceLost;
            InteractionManager.InteractionSourcePressed  -= SourcePressed;
            InteractionManager.InteractionSourceReleased -= SourceReleased;
            InteractionManager.InteractionSourceUpdated  -= SourceUpdated;

            keywordRecognizer.Stop();
            keywordRecognizer = null;
            #endif
        }

        #if UNITY_EDITOR || WINDOWS_UWP
        private void SourceLost(InteractionSourceLostEventArgs obj)
        {
            pressed = false;
        }
        private void SourceReleased(InteractionSourceReleasedEventArgs obj)
        {
            pressed = false;
        }
        private void SourcePressed(InteractionSourcePressedEventArgs obj)
        {
            pressed  = true;

            handVelocity = Vector3.zero;
            lastPos      = handPos;
            lastTime     = Time.time;
        }
        private void SourceUpdated(InteractionSourceUpdatedEventArgs obj)
        {
            if (obj.state.source.kind != InteractionSourceKind.Hand)
            {
                return;
            }

            Vector3 pos;
            if (Time.time - lastTime > 0 && obj.state.sourcePose.TryGetPosition(out pos))
            {
                if (pressed)
                {
                    handVelocity = (pos - lastPos) / (Time.time - lastTime);
                    if (handVelocity.sqrMagnitude > maxVelocity*maxVelocity)
                    {
                        handVelocity = handVelocity.normalized* maxVelocity;
                    }
                }

                lastPos  = handPos;
                lastTime = Time.time;
                handPos  = pos;

                if (pressed)
                {
                    targetPos += (pos - lastPos) * moveScale;
                }
            }
        }

        private async void HeardKeyword(PhraseRecognizedEventArgs args)
        {
            string reply = "ok";

            // Execute the command we just heard
            if (args.text == CMD_CIRCLE)
            {
                spheres.SetActive(true);
                shaderBalls.SetActive(false);
                cubes.SetActive(false);
            }
            else if (args.text == CMD_BALL)
            {
                spheres.SetActive(false);
                shaderBalls.SetActive(true);
                cubes.SetActive(false);
            }
            else if (args.text == CMD_CUBE)
            {
                spheres.SetActive(false);
                shaderBalls.SetActive(false);
                cubes.SetActive(true);
            }
            else if (args.text == CMD_RESET)
            {
                transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3;
                transform.LookAt(Camera.main.transform.position);
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                targetPos = transform.position;
            }
            else if (args.text == "clear")
            {
                lightCapture.Clear();
            }
            else if (args.text == CMD_DISABLE)
            {
                lightCapture.enabled = false;
            }
            else if (args.text == CMD_ENABLE)
            {
                lightCapture.enabled = true;
            }
            else if (args.text == CMD_ADD)
            {
                RaycastHit hit;
                if (UnityEngine.Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
                {
                    GameObject prefab = spawnPrefabs[addIndex % spawnPrefabs.Length];
                    Instantiate(prefab, hit.point, prefab.transform.rotation);
                    addIndex++;
                }
            }
            else if (args.text == CMD_EXPOSURE_UP)
            {
                if (exposure + EXPOSURE_ADJUST <= EXPOSURE_MAX)
                {
                    exposure += EXPOSURE_ADJUST;
                    await lightCapture.SetExposureAsync(exposure);
                }
                reply = "" + exposure;
            }
            else if (args.text == CMD_EXPOSURE_DOWN)
            {
                if (exposure - EXPOSURE_ADJUST >= EXPOSURE_MIN)
                {
                    exposure -= EXPOSURE_ADJUST;
                    await lightCapture.SetExposureAsync(exposure);
                }
                reply = "" + exposure;
            }
            else if (args.text == CMD_ISO_UP)
            {
                if (iso + ISO_ADJUST <= ISO_MAX)
                {
                    iso += ISO_ADJUST;
                    await lightCapture.SetISOAsync(iso);
                    Debug.Log("ISO: " + iso);
                }
                reply = "" + iso;
            }
            else if (args.text == CMD_ISO_DOWN)
            {
                if (iso - ISO_ADJUST >= ISO_MIN)
                {
                    iso -= ISO_ADJUST;
                    await lightCapture.SetISOAsync(iso);
                    Debug.Log("ISO: " + iso);
                }
                reply = "" + iso;
            }
            else if (args.text == CMD_WHITE_UP)
            {
                if (whitebalance + WB_ADJUST <= WB_MAX)
                {
                    whitebalance += WB_ADJUST;
                    await lightCapture.SetWhiteBalanceAsync(whitebalance);
                    Debug.Log("WB: " + whitebalance);
                }
                reply = ""+whitebalance;
            }
            else if (args.text == CMD_WHITE_DOWN)
            {
                if (whitebalance - WB_ADJUST >= WB_MIN)
                {
                    whitebalance -= WB_ADJUST;
                    await lightCapture.SetWhiteBalanceAsync(whitebalance);
                    Debug.Log("WB: " + whitebalance);
                }
                reply = ""+whitebalance;
            }
            else if (args.text == CMD_SAVE)
            {
                #if WINDOWS_UWP
                SaveMap();
                reply = "Saving environment map to photo roll!";
                #else
                reply = "Saving to photo roll not supported on this platform.";
                #endif
            }

            // Output results of the command
            Debug.Log("Heard command: " + args.text);
            if (tts != null)
            {
                speakMethod.Invoke(tts, new object[] { reply });
            }
        }
        #endif

        #if WINDOWS_UWP
        private async void SaveMap()
        {
            Texture2D tex     = CubeMapper.CreateCubemapTex(lightCapture.CubeMapper.Map);
            byte[]    texData = tex.EncodeToPNG();

            var picturesLibrary = await global::Windows.Storage.StorageLibrary.GetLibraryAsync(global::Windows.Storage.KnownLibraryId.Pictures);
            // Fall back to the local app storage if the Pictures Library is not available
            var captureFolder = picturesLibrary.SaveFolder ?? global::Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await captureFolder.CreateFileAsync("EnvironmentMap.png", global::Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            await global::Windows.Storage.FileIO.WriteBytesAsync(file, texData);
        }
        #endif

        private void Update()
        {
            if (!pressed)
            {
                handVelocity = Vector3.Lerp(handVelocity, Vector3.zero, velocityDecay*Time.deltaTime);
                targetPos   += handVelocity * Time.deltaTime;
            }
            else
            {
                // Blend rotation towards the player
                Quaternion dest = Quaternion.LookRotation( transform.position-Camera.main.transform.position );
                Vector3    rot  = dest.eulerAngles;
                rot.x = rot.z = 0;
                dest = Quaternion.Euler(rot);

                transform.rotation = Quaternion.Slerp(transform.rotation, dest, 4 * Time.deltaTime);
            }
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
        }
    }
}