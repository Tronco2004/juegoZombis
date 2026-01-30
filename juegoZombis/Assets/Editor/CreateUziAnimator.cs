using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateUziAnimator
{
    [MenuItem("Tools/Create UZI Animator")]
    public static void Create()
    {
        string fbxPath = "Assets/brazosUzi/source/Dual Mac10 - Animated.fbx";
        string controllerPath = "Assets/brazosUzi/DualMac10.controller";
        
        // Borrar si existe
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
            AssetDatabase.Refresh();
        }
        
        // Crear nuevo controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        
        var stateMachine = controller.layers[0].stateMachine;
        
        // Buscar clips
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
        
        // Estado Idle
        var idleState = stateMachine.AddState("Idle", new Vector3(300, 100, 0));
        if (idleClip != null)
        {
            idleState.motion = idleClip;
            Debug.Log("Idle clip: " + idleClip.length + "s");
        }
        stateMachine.defaultState = idleState;
        
        // Estado Fire
        if (fireClip != null)
        {
            var fireState = stateMachine.AddState("Fire", new Vector3(550, 100, 0));
            fireState.motion = fireClip;
            
            var toFire = idleState.AddTransition(fireState);
            toFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            toFire.hasExitTime = false;
            toFire.duration = 0;
            
            var fromFire = fireState.AddTransition(idleState);
            fromFire.hasExitTime = true;
            fromFire.exitTime = 1f;
            fromFire.duration = 0;
            
            Debug.Log("Fire clip: " + fireClip.length + "s");
        }
        else
        {
            Debug.LogWarning("No se encontró clip Fire!");
        }
        
        // Estado Reload
        if (reloadClip != null)
        {
            var reloadState = stateMachine.AddState("Reload", new Vector3(550, 250, 0));
            reloadState.motion = reloadClip;
            
            var toReload = idleState.AddTransition(reloadState);
            toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            toReload.hasExitTime = false;
            toReload.duration = 0;
            
            var fromReload = reloadState.AddTransition(idleState);
            fromReload.hasExitTime = true;
            fromReload.exitTime = 1f;
            fromReload.duration = 0;
            
            Debug.Log("Reload clip: " + reloadClip.length + "s");
        }
        else
        {
            Debug.LogWarning("No se encontró clip Reload!");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("=== DualMac10.controller creado! ===");
    }
}
