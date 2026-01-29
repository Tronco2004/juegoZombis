using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SplitG17Animations
{
    [MenuItem("Tools/Split G17 Animation Clips")]
    public static void Split()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        
        if (importer == null)
        {
            Debug.LogError("No se encontró el FBX");
            return;
        }
        
        // Crear los clips separados
        ModelImporterClipAnimation[] clips = new ModelImporterClipAnimation[]
        {
            new ModelImporterClipAnimation
            {
                name = "Idle",
                takeName = "Scene",
                firstFrame = 0,
                lastFrame = 90,  // Primeros ~2 segundos (90 frames a 45fps)
                loop = true,
                loopTime = true
            },
            new ModelImporterClipAnimation
            {
                name = "Fire",
                takeName = "Scene",
                firstFrame = 90,
                lastFrame = 135,  // ~1 segundo
                loop = false,
                loopTime = false
            },
            new ModelImporterClipAnimation
            {
                name = "Reload",
                takeName = "Scene",
                firstFrame = 135,
                lastFrame = 396,  // Resto de la animación (~6 segundos)
                loop = false,
                loopTime = false
            }
        };
        
        importer.clipAnimations = clips;
        
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        
        Debug.Log("✓ Animaciones divididas!");
        Debug.Log("Ahora ejecuta: Tools > Setup G17 Controller");
    }
}
