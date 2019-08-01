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
	}
}