using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateCleanG17Animator
{
    [MenuItem("Tools/Create CLEAN G17 Animator")]
    public static void Create()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        string controllerPath = "Assets/brazosPipa/CleanG17.controller";
        
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        
        var stateMachine = controller.layers[0].stateMachine;
        
        // Buscar clips
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip fireClip = null;
        AnimationClip reloadClip = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                if (clip.name == "Fire") fireClip = clip;
                else if (clip.name == "Reload") reloadClip = clip;
            }
        }
        
        // Estado Idle - sin animación (pose estática)
        var idleState = stateMachine.AddState("Idle", new Vector3(300, 100, 0));
        stateMachine.defaultState = idleState;
        
        // Estado Fire
        if (fireClip != null)
        {
            var fireState = stateMachine.AddState("Fire", new Vector3(550, 100, 0));
            fireState.motion = fireClip;
            
            // Idle -> Fire (con trigger)
            var toFire = idleState.AddTransition(fireState);
            toFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            toFire.hasExitTime = false;
            toFire.duration = 0;
            
            // Fire -> Idle (automático al terminar)
            var fromFire = fireState.AddTransition(idleState);
            fromFire.hasExitTime = true;
            fromFire.exitTime = 1f;
            fromFire.duration = 0;
            
            Debug.Log("Fire clip añadido: " + fireClip.length + "s");
        }
        
        // Estado Reload
        if (reloadClip != null)
        {
            var reloadState = stateMachine.AddState("Reload", new Vector3(550, 250, 0));
            reloadState.motion = reloadClip;
            
            // Idle -> Reload (con trigger)
            var toReload = idleState.AddTransition(reloadState);
            toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            toReload.hasExitTime = false;
            toReload.duration = 0;
            
            // Reload -> Idle (automático al terminar)
            var fromReload = reloadState.AddTransition(idleState);
            fromReload.hasExitTime = true;
            fromReload.exitTime = 1f;
            fromReload.duration = 0;
            
            Debug.Log("Reload clip añadido: " + reloadClip.length + "s");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("=== CleanG17.controller creado con Fire ===");
    }
}
