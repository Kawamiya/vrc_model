#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class OldVersionCheck {
    [UnityEditor.Callbacks.DidReloadScripts]
    public static void OnScriptsReloaded() {
        if (System.IO.Directory.Exists(AssetDatabase.GUIDToAssetPath("66b81bbcb27723e4ea85b86918446223"))) {
            EditorUtility.DisplayDialog("OLD VERSION DETECTED", "OLD INSTALL DETECTED! You must delete both DynamicPenetrationSystem and RalivDynamicPenetrationSystem folders and reimport the package!", "OK");
        }
    }
}
#endif