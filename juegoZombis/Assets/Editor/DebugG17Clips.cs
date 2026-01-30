using UnityEngine;
using UnityEditor;

public class DebugG17Clips
{
    [MenuItem("Tools/DEBUG - Listar clips G17")]
    public static void ListClips()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Debug.Log("=== CLIPS EN EL FBX ===");
        Debug.Log("Total assets encontrados: " + assets.Length);
        
        int clipCount = 0;
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                clipCount++;
                Debug.Log($"Clip #{clipCount}: '{clip.name}' - Duración: {clip.length}s - Frames: {clip.frameRate}fps");
            }
        }
        
        if (clipCount == 0)
        {
            Debug.LogWarning("NO SE ENCONTRARON CLIPS DE ANIMACIÓN!");
        }
        else
        {
            Debug.Log($"Total clips encontrados: {clipCount}");
        }
    }
}
