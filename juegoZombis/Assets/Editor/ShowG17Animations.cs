using UnityEngine;
using UnityEditor;

public class ShowG17Animations
{
    [MenuItem("Tools/Show G17 Animations")]
    public static void Show()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Debug.Log("=== ANIMACIONES ENCONTRADAS EN G17 ===");
        int count = 0;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                count++;
                Debug.Log(count + ". " + clip.name + " (" + clip.length.ToString("F2") + "s)");
            }
        }
        
        if (count == 0)
        {
            Debug.LogWarning("No se encontraron animaciones en el FBX. Verifica que el FBX tenga animaciones importadas.");
        }
    }
}
