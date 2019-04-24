// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LightingTools {
	public class LightingToolsMenus
	{
		/// <summary> Creates a starter Light Capture object in the scene. </summary>
		[MenuItem("Mixed Reality Toolkit/Lighting Tools/Create Light Capture Object", priority = 1)]
		private static void CreateLEObject()
		{
			GameObject go = new GameObject("LightCapture", typeof(LightCapture));
			EditorGUIUtility.PingObject(go);
		}

        /// <summary> Creates a starter Light Capture object in the scene. </summary>
		[MenuItem("Mixed Reality Toolkit/Lighting Tools/Create Additive Shadow Object", priority = 1)]
        private static void CreateASObject()
        {
            GameObject go = new GameObject("AdditiveShadows", typeof(ShadowsAdditive));
            EditorGUIUtility.PingObject(go);
            Debug.LogWarning("For additive shadows to work, you must have shadows enabled for your quality settings! You must also have a shadow catching surface with the 'MRTK/Shadow AR Transparent' shader on it, use the Windows Mixed Reality package and the Spatial Mapping Renderer visuals, or the MRTK's spatial mapping mesh.");
        }
    }
}