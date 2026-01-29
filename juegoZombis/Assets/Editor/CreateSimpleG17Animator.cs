using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateSimpleG17Animator
{
    [MenuItem("Tools/Create Simple G17 Animator")]
    public static void Create()
    {
        string controllerPath = "Assets/brazosPipa/SimpleG17.controller";
        
        // Borrar si existe
        AssetDatabase.DeleteAsset(controllerPath);
        
        // Crear nuevo
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Añadir parámetros
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        
        var stateMachine = controller.layers[0].stateMachine;
        
        // Buscar el clip Idle
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        AnimationClip idleClip = null;
        AnimationClip fireClip = null;
        AnimationClip reloadClip = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                if (clip.name == "Idle") idleClip = clip;
                else if (clip.name == "Fire") fireClip = clip;
                else if (clip.name == "Reload") reloadClip = clip;
            }
        }
        
        // Estado Idle (por defecto)
        var idleState = stateMachine.AddState("Idle", new Vector3(300, 100, 0));
        if (idleClip != null) 
        {
            idleState.motion = idleClip;
            Debug.Log("Idle asignado: " + idleClip.name + " (" + idleClip.length + "s)");
        }
        else
        {
            Debug.LogError("NO SE ENCONTRO CLIP IDLE");
        }
        stateMachine.defaultState = idleState;
        
        // Estado Fire
        if (fireClip != null)
        {
            var fireState = stateMachine.AddState("Fire", new Vector3(550, 0, 0));
            fireState.motion = fireClip;
            
            var toFire = idleState.AddTransition(fireState);
            toFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            toFire.hasExitTime = false;
            toFire.duration = 0;
            
            var fromFire = fireState.AddTransition(idleState);
            fromFire.hasExitTime = true;
            fromFire.exitTime = 0.9f;
            fromFire.duration = 0.1f;
            
            Debug.Log("Fire asignado: " + fireClip.name);
        }
        
        // Estado Reload
        if (reloadClip != null)
        {
            var reloadState = stateMachine.AddState("Reload", new Vector3(550, 200, 0));
            reloadState.motion = reloadClip;
            
            var toReload = idleState.AddTransition(reloadState);
            toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            toReload.hasExitTime = false;
            toReload.duration = 0;
            
            var fromReload = reloadState.AddTransition(idleState);
            fromReload.hasExitTime = true;
            fromReload.exitTime = 0.95f;
            fromReload.duration = 0.1f;
            
            Debug.Log("Reload asignado: " + reloadClip.name);
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("=== CONTROLLER CREADO: " + controllerPath + " ===");
        Debug.Log("Asigna este controller al Animator del G17");
    }
}
