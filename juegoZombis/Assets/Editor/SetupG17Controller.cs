using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupG17Controller
{
    [MenuItem("Tools/Setup G17 Controller")]
    public static void Setup()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        string controllerPath = "Assets/brazosPipa/G17_AnimatorController.controller";
        
        // BORRAR el controller viejo y crear uno nuevo
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Añadir parámetros
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
        
        // Cargar animaciones del FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        AnimationClip idleClip = null;
        AnimationClip fireClip = null;
        AnimationClip reloadClip = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                string name = clip.name.ToLower();
                Debug.Log("Clip encontrado: " + clip.name);
                
                if (name.Contains("idle")) idleClip = clip;
                else if (name.Contains("fire")) fireClip = clip;
                else if (name.Contains("reload")) reloadClip = clip;
                else if (idleClip == null) idleClip = clip; // Usar el primero como idle si no hay
            }
        }
        
        // Si no hay clips separados, usar el mismo para todo
        if (fireClip == null) fireClip = idleClip;
        if (reloadClip == null) reloadClip = idleClip;
        
        var stateMachine = controller.layers[0].stateMachine;
        
        // Crear estado Idle
        var idleState = stateMachine.AddState("Idle", new Vector3(250, 100, 0));
        if (idleClip != null) idleState.motion = idleClip;
        stateMachine.defaultState = idleState;
        
        // Crear estado Fire
        var fireState = stateMachine.AddState("Fire", new Vector3(500, 0, 0));
        if (fireClip != null) fireState.motion = fireClip;
        
        // Crear estado Reload
        var reloadState = stateMachine.AddState("Reload", new Vector3(500, 200, 0));
        if (reloadClip != null) reloadState.motion = reloadClip;
        
        // Transición Idle -> Fire
        var idleToFire = idleState.AddTransition(fireState);
        idleToFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
        idleToFire.hasExitTime = false;
        idleToFire.duration = 0;
        
        // Transición Fire -> Idle
        var fireToIdle = fireState.AddTransition(idleState);
        fireToIdle.hasExitTime = true;
        fireToIdle.exitTime = 0.9f;
        fireToIdle.duration = 0.1f;
        
        // Transición Idle -> Reload
        var idleToReload = idleState.AddTransition(reloadState);
        idleToReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
        idleToReload.hasExitTime = false;
        idleToReload.duration = 0;
        
        // Transición Reload -> Idle
        var reloadToIdle = reloadState.AddTransition(idleState);
        reloadToIdle.hasExitTime = true;
        reloadToIdle.exitTime = 0.95f;
        reloadToIdle.duration = 0.1f;
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("========================================");
        Debug.Log("ANIMATOR CONTROLLER CREADO!");
        Debug.Log("Idle: " + (idleClip != null ? idleClip.name : "VACIO"));
        Debug.Log("Fire: " + (fireClip != null ? fireClip.name : "VACIO"));
        Debug.Log("Reload: " + (reloadClip != null ? reloadClip.name : "VACIO"));
        Debug.Log("========================================");
        Debug.Log("Ahora asigna el controller al Animator del G17");
    }
}
