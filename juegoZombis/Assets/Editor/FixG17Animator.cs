using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class FixG17Animator
{
    [MenuItem("Tools/FIX G17 Animator NOW")]
    public static void Fix()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        string controllerPath = "Assets/brazosPipa/SimpleG17.controller";
        
        // Cargar controller
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("No se encontró el controller");
            return;
        }
        
        // Buscar TODOS los clips
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Debug.Log("=== BUSCANDO CLIPS EN FBX ===");
        foreach (Object asset in allAssets)
        {
            Debug.Log("Asset: " + asset.name + " (" + asset.GetType().Name + ")");
        }
        
        AnimationClip idleClip = null;
        AnimationClip fireClip = null;
        AnimationClip reloadClip = null;
        
        foreach (Object asset in allAssets)
        {
            if (asset is AnimationClip clip)
            {
                Debug.Log("CLIP: " + clip.name + " - Duración: " + clip.length);
                
                if (clip.name == "Idle") idleClip = clip;
                else if (clip.name == "Fire") fireClip = clip;
                else if (clip.name == "Reload") reloadClip = clip;
            }
        }
        
        if (idleClip == null && fireClip == null && reloadClip == null)
        {
            Debug.LogError("NO HAY CLIPS DE ANIMACIÓN EN EL FBX!");
            Debug.LogError("Ve a Assets/brazosPipa/source/G17 Pistol - Animated.fbx");
            Debug.LogError("Inspector > Animation > Import Animation = ON");
            Debug.LogError("Luego click en Apply");
            return;
        }
        
        // Asignar a los estados
        var stateMachine = controller.layers[0].stateMachine;
        
        foreach (var childState in stateMachine.states)
        {
            var state = childState.state;
            
            if (state.name == "Idle" && idleClip != null)
            {
                state.motion = idleClip;
                Debug.Log("✓ Idle asignado");
            }
            else if (state.name == "Fire" && fireClip != null)
            {
                state.motion = fireClip;
                Debug.Log("✓ Fire asignado");
            }
            else if (state.name == "Reload" && reloadClip != null)
            {
                state.motion = reloadClip;
                Debug.Log("✓ Reload asignado");
            }
        }
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log("=== LISTO ===");
    }
}
