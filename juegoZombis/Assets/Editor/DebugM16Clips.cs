using UnityEngine;
using UnityEditor;

public class DebugM16Clips : MonoBehaviour
{
    [MenuItem("Tools/Debug M16 Animation")]
    static void DebugM16()
    {
        string path = "Assets/brazosM16/source/M16 A2 Rifle - Animated.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        
        Debug.Log("=== M16 A2 Rifle - Clips de Animación ===");
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                Debug.Log($"Clip: {clip.name} | Duración: {clip.length}s | FPS: {clip.frameRate} | Frames: {clip.length * clip.frameRate}");
            }
        }
        
        Debug.Log("=== Fin ===");
    }
}
