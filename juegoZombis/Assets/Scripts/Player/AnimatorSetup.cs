using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

/// <summary>
/// Script de editor para crear automáticamente el Animator Controller
/// Solo funciona en el Editor de Unity
/// </summary>
public class AnimatorSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Clips de Animación")]
    public AnimationClip idle;
    public AnimationClip runForward;
    public AnimationClip runBackward;
    public AnimationClip runLeft;
    public AnimationClip runRight;
    public AnimationClip aim;
    public AnimationClip crouch;
    public AnimationClip death;

    [ContextMenu("Crear Animator Controller")]
    public void CreateAnimatorController()
    {
        // Crear el Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/PlayerAnimatorAuto.controller");

        // Obtener el root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Crear estados
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(0, 0, 0));
        AnimatorState runForwardState = rootStateMachine.AddState("RunForward", new Vector3(200, -100, 0));
        AnimatorState runBackwardState = rootStateMachine.AddState("RunBackward", new Vector3(200, 100, 0));
        AnimatorState runLeftState = rootStateMachine.AddState("RunLeft", new Vector3(-200, -100, 0));
        AnimatorState runRightState = rootStateMachine.AddState("RunRight", new Vector3(-200, 100, 0));
        AnimatorState aimState = rootStateMachine.AddState("Aim", new Vector3(0, -200, 0));
        AnimatorState crouchState = rootStateMachine.AddState("Crouch", new Vector3(0, 200, 0));
        AnimatorState deathState = rootStateMachine.AddState("Death", new Vector3(400, 0, 0));

        // Asignar clips
        if (idle) idleState.motion = idle;
        if (runForward) runForwardState.motion = runForward;
        if (runBackward) runBackwardState.motion = runBackward;
        if (runLeft) runLeftState.motion = runLeft;
        if (runRight) runRightState.motion = runRight;
        if (aim) aimState.motion = aim;
        if (crouch) crouchState.motion = crouch;
        if (death) deathState.motion = death;

        // Hacer idle el estado por defecto
        rootStateMachine.defaultState = idleState;

        Debug.Log("Animator Controller creado en Assets/PlayerAnimatorAuto.controller");
        Debug.Log("Ahora arrastra ese controller al componente Animator de tu personaje.");
    }
#endif
}
