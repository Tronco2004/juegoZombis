using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class CreateM16Animator : MonoBehaviour
{
    [MenuItem("Tools/Create M16 Animator Controller")]
    static void CreateAnimator()
    {
        // Ruta del FBX
        string fbxPath = "Assets/brazosM16/source/M16 A2 Rifle - Animated.fbx";
        
        // Cargar los clips del FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        AnimationClip idleClip = null;
        AnimationClip fireClip = null;
        AnimationClip reloadClip = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                string clipName = clip.name.ToLower();
                
                if (clipName.Contains("idle"))
                    idleClip = clip;
                else if (clipName.Contains("fire") || clipName.Contains("shoot"))
                    fireClip = clip;
                else if (clipName.Contains("reload"))
                    reloadClip = clip;
                    
                Debug.Log($"Clip encontrado: {clip.name}");
            }
        }
        
        if (idleClip == null)
        {
            Debug.LogError("No se encontró clip Idle!");
            return;
        }
        
        // Crear el Animator Controller
        string controllerPath = "Assets/brazosM16/M16A2.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Obtener la capa base
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Crear estado Idle (estado por defecto)
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(0, 0, 0));
        idleState.motion = idleClip;
        rootStateMachine.defaultState = idleState;
        
        // Crear estado Fire
        if (fireClip != null)
        {
            AnimatorState fireState = rootStateMachine.AddState("Fire", new Vector3(250, 0, 0));
            fireState.motion = fireClip;
            
            // Parámetro trigger para Fire
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            
            // Transición Idle -> Fire
            AnimatorStateTransition toFire = idleState.AddTransition(fireState);
            toFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            toFire.duration = 0.05f;
            toFire.hasExitTime = false;
            
            // Transición Fire -> Idle
            AnimatorStateTransition fireToIdle = fireState.AddTransition(idleState);
            fireToIdle.hasExitTime = true;
            fireToIdle.exitTime = 0.9f;
            fireToIdle.duration = 0.1f;
        }
        
        // Crear estado Reload
        if (reloadClip != null)
        {
            AnimatorState reloadState = rootStateMachine.AddState("Reload", new Vector3(250, 100, 0));
            reloadState.motion = reloadClip;
            
            // Parámetro trigger para Reload
            controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
            
            // Transición Idle -> Reload
            AnimatorStateTransition toReload = idleState.AddTransition(reloadState);
            toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            toReload.duration = 0.1f;
            toReload.hasExitTime = false;
            
            // Transición Reload -> Idle
            AnimatorStateTransition reloadToIdle = reloadState.AddTransition(idleState);
            reloadToIdle.hasExitTime = true;
            reloadToIdle.exitTime = 0.95f;
            reloadToIdle.duration = 0.1f;
        }
        
        // Guardar cambios
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✓ Animator Controller creado en: " + controllerPath);
        Debug.Log("  - Idle: " + (idleClip != null ? "✓" : "✗"));
        Debug.Log("  - Fire: " + (fireClip != null ? "✓" : "✗"));
        Debug.Log("  - Reload: " + (reloadClip != null ? "✓" : "✗"));
        
        // Seleccionar el controller creado
        Selection.activeObject = controller;
    }
}
