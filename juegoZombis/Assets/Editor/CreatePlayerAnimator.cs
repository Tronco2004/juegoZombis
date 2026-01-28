using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreatePlayerAnimator : MonoBehaviour
{
    [MenuItem("Tools/Crear Animator del Jugador")]
    static void CreateAnimator()
    {
        // Crear el Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/PlayerAnimatorFinal.controller");

        // Obtener la state machine
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Buscar los clips
        string[] allClips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/AnimacionesJugador" });
        
        AnimationClip idleClip = null;
        AnimationClip runForwardClip = null;
        AnimationClip runBackwardClip = null;
        AnimationClip runLeftClip = null;
        AnimationClip runRightClip = null;

        // Buscar clips en los FBX
        string[] fbxFiles = new string[] { 
            "Assets/AnimacionesJugador/idle.fbx",
            "Assets/AnimacionesJugador/run forward.fbx",
            "Assets/AnimacionesJugador/run backward.fbx",
            "Assets/AnimacionesJugador/run left.fbx",
            "Assets/AnimacionesJugador/run right.fbx"
        };

        foreach (string fbxPath in fbxFiles)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    if (fbxPath.Contains("idle.fbx")) idleClip = clip;
                    else if (fbxPath.Contains("run forward.fbx")) runForwardClip = clip;
                    else if (fbxPath.Contains("run backward.fbx")) runBackwardClip = clip;
                    else if (fbxPath.Contains("run left.fbx")) runLeftClip = clip;
                    else if (fbxPath.Contains("run right.fbx")) runRightClip = clip;
                }
            }
        }

        // Crear estados con los nombres exactos
        AnimatorState idleState = stateMachine.AddState("idle");
        AnimatorState runForwardState = stateMachine.AddState("run forward");
        AnimatorState runBackwardState = stateMachine.AddState("run backward");
        AnimatorState runLeftState = stateMachine.AddState("run left");
        AnimatorState runRightState = stateMachine.AddState("run right");

        // Asignar clips
        if (idleClip != null) idleState.motion = idleClip;
        if (runForwardClip != null) runForwardState.motion = runForwardClip;
        if (runBackwardClip != null) runBackwardState.motion = runBackwardClip;
        if (runLeftClip != null) runLeftState.motion = runLeftClip;
        if (runRightClip != null) runRightState.motion = runRightClip;

        // Hacer idle el estado por defecto
        stateMachine.defaultState = idleState;

        // Guardar
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("¡Animator creado! Arrastra 'PlayerAnimatorFinal' al Animator de tu personaje.");
    }
}
