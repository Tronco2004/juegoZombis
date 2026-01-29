using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class CreatePlayerAnimator : MonoBehaviour
{
    [MenuItem("Tools/Crear Animator del Jugador")]
    static void CreateAnimator()
    {
        // Crear el Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/PlayerAnimatorFinal.controller");

        // Obtener la state machine
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Diccionario para almacenar los clips
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        // Lista de animaciones a buscar (nombre del estado -> ruta del FBX)
        Dictionary<string, string> animationFiles = new Dictionary<string, string>
        {
            // === MOVIMIENTO BÁSICO (sin arma) ===
            { "idle", "Assets/AnimacionesJugador/idle.fbx" },
            { "run forward", "Assets/AnimacionesJugador/run forward.fbx" },
            { "run backward", "Assets/AnimacionesJugador/run backward.fbx" },
            { "run left", "Assets/AnimacionesJugador/run left.fbx" },
            { "run right", "Assets/AnimacionesJugador/run right.fbx" },
            
            // === PISTOLA ===
            { "pistol idle", "Assets/Pipa/source/pistol idle.fbx" },
            { "pistol run", "Assets/Pipa/source/pistol run.fbx" },
            { "pistol run backward", "Assets/Pipa/source/pistol run backward.fbx" },
            { "pistol walk", "Assets/Pipa/source/pistol walk.fbx" },
            { "pistol walk backward", "Assets/Pipa/source/pistol walk backward.fbx" },
            { "pistol strafe", "Assets/Pipa/source/pistol strafe.fbx" },
            { "pistol strafe left", "Assets/Pipa/source/pistol strafe.fbx" },
            { "pistol strafe right", "Assets/Pipa/source/pistol strafe (2).fbx" },
            { "pistol jump", "Assets/Pipa/source/pistol jump.fbx" },
            { "pistol kneeling", "Assets/Pipa/source/pistol kneeling idle.fbx" },
            
            // === RIFLE (apuntando) ===
            { "aim", "Assets/AnimacionesJugador/idle aiming.fbx" },
            { "rifle reload", "Assets/AnimacionesJugador/Reloading.fbx" },
            
            // === OTROS ===
            { "death", "Assets/AnimacionesJugador/death from front headshot.fbx" },
            { "crouch", "Assets/AnimacionesJugador/idle crouching.fbx" },
        };

        // Buscar clips en los FBX
        foreach (var kvp in animationFiles)
        {
            string stateName = kvp.Key;
            string fbxPath = kvp.Value;
            
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    clips[stateName] = clip;
                    break;
                }
            }
        }

        // Crear estados y asignar clips
        AnimatorState pistolIdleState = null;
        foreach (var kvp in clips)
        {
            AnimatorState state = stateMachine.AddState(kvp.Key);
            state.motion = kvp.Value;
            
            if (kvp.Key == "pistol idle")
            {
                pistolIdleState = state;
            }
        }

        // Hacer pistol idle el estado por defecto
        if (pistolIdleState != null)
        {
            stateMachine.defaultState = pistolIdleState;
        }

        // Guardar
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("¡Animator creado con " + clips.Count + " animaciones de pistola y rifle!");
    }
}
