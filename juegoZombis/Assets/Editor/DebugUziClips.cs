using UnityEngine;
using UnityEditor;

public class DebugUziClips
{
    [MenuItem("Tools/DEBUG - Listar clips Uzi")]
    public static void ListClips()
    {
        string fbxPath = "Assets/brazosUzi/source/Dual Mac10 - Animated.fbx";
        
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Debug.Log("=== CLIPS EN EL FBX UZI ===");
        Debug.Log("Total assets encontrados: " + assets.Length);
        
        int clipCount = 0;
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                clipCount++;
                // Mostrar info detallada del clip
                Debug.Log($"Clip #{clipCount}: '{clip.name}' - Duración: {clip.length}s - Frames: {clip.length * clip.frameRate}");
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
