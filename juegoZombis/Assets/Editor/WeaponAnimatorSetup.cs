using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Editor script para crear Animator Controllers para las armas
/// Uso: Window > Weapon Animator Setup
/// </summary>
public class WeaponAnimatorSetup : EditorWindow
{
    private AnimationClip idleClip;
    private AnimationClip fireClip;
    private AnimationClip reloadClip;
    private AnimationClip drawClip;
    private AnimationClip holsterClip;
    private string controllerName = "WeaponAnimator";
    private string savePath = "Assets/";

    [MenuItem("Window/Weapon Animator Setup")]
    public static void ShowWindow()
    {
        GetWindow<WeaponAnimatorSetup>("Weapon Animator Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Crear Animator Controller para Arma", EditorStyles.boldLabel);
        GUILayout.Space(10);

        controllerName = EditorGUILayout.TextField("Nombre del Controller:", controllerName);
        savePath = EditorGUILayout.TextField("Ruta de guardado:", savePath);
        
        GUILayout.Space(10);
        GUILayout.Label("Clips de Animación (arrastra aquí):", EditorStyles.boldLabel);

        idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle:", idleClip, typeof(AnimationClip), false);
        fireClip = (AnimationClip)EditorGUILayout.ObjectField("Fire (Shoot):", fireClip, typeof(AnimationClip), false);
        reloadClip = (AnimationClip)EditorGUILayout.ObjectField("Reload:", reloadClip, typeof(AnimationClip), false);
        drawClip = (AnimationClip)EditorGUILayout.ObjectField("Draw (Sacar arma):", drawClip, typeof(AnimationClip), false);
        holsterClip = (AnimationClip)EditorGUILayout.ObjectField("Holster (Guardar arma):", holsterClip, typeof(AnimationClip), false);

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Si no tienes animación de Draw/Holster:\n" +
            "- Draw: Puedes usar Idle o crear una simple bajando y subiendo el arma\n" +
            "- Holster: Puedes invertir Draw o dejarlo vacío", 
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Crear Animator Controller", GUILayout.Height(40)))
        {
            CreateAnimatorController();
        }

        GUILayout.Space(20);
        GUILayout.Label("Parámetros que se crearán:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Triggers:\n" +
            "  - Shoot (para disparar)\n" +
            "  - Reload (para recargar)\n" +
            "  - Draw (para sacar el arma)\n" +
            "  - Holster (para guardar el arma)\n\n" +
            "Bools:\n" +
            "  - IsAiming (apuntando)\n" +
            "  - IsRunning (corriendo)", 
            MessageType.None);
    }

    void CreateAnimatorController()
    {
        if (idleClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Necesitas al menos la animación Idle", "OK");
            return;
        }

        // Crear el controller
        string fullPath = Path.Combine(savePath, controllerName + ".controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        // Añadir parámetros
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Holster", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsAiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);

        // Obtener la state machine del layer base
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Crear estados
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 100, 0));
        idleState.motion = idleClip;
        stateMachine.defaultState = idleState;

        AnimatorState fireState = null;
        if (fireClip != null)
        {
            fireState = stateMachine.AddState("Fire", new Vector3(550, 100, 0));
            fireState.motion = fireClip;
        }

        AnimatorState reloadState = null;
        if (reloadClip != null)
        {
            reloadState = stateMachine.AddState("Reload", new Vector3(550, 200, 0));
            reloadState.motion = reloadClip;
        }

        AnimatorState drawState = null;
        if (drawClip != null)
        {
            drawState = stateMachine.AddState("Draw", new Vector3(100, 100, 0));
            drawState.motion = drawClip;
        }

        AnimatorState holsterState = null;
        if (holsterClip != null)
        {
            holsterState = stateMachine.AddState("Holster", new Vector3(100, 200, 0));
            holsterState.motion = holsterClip;
        }

        // Crear transiciones
        // Idle -> Fire
        if (fireState != null)
        {
            AnimatorStateTransition toFire = idleState.AddTransition(fireState);
            toFire.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            toFire.hasExitTime = false;
            toFire.duration = 0.05f;

            // Fire -> Idle
            AnimatorStateTransition fireToIdle = fireState.AddTransition(idleState);
            fireToIdle.hasExitTime = true;
            fireToIdle.exitTime = 0.9f;
            fireToIdle.duration = 0.1f;
        }

        // Idle -> Reload
        if (reloadState != null)
        {
            AnimatorStateTransition toReload = idleState.AddTransition(reloadState);
            toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            toReload.hasExitTime = false;
            toReload.duration = 0.1f;

            // Reload -> Idle
            AnimatorStateTransition reloadToIdle = reloadState.AddTransition(idleState);
            reloadToIdle.hasExitTime = true;
            reloadToIdle.exitTime = 0.95f;
            reloadToIdle.duration = 0.1f;
        }

        // Draw -> Idle
        if (drawState != null)
        {
            // Entry -> Draw (como estado inicial alternativo)
            AnimatorStateTransition drawToIdle = drawState.AddTransition(idleState);
            drawToIdle.hasExitTime = true;
            drawToIdle.exitTime = 0.9f;
            drawToIdle.duration = 0.1f;

            // Any State -> Draw (cuando se activa el trigger)
            AnimatorStateTransition anyToDraw = stateMachine.AddAnyStateTransition(drawState);
            anyToDraw.AddCondition(AnimatorConditionMode.If, 0, "Draw");
            anyToDraw.hasExitTime = false;
            anyToDraw.duration = 0.05f;
        }

        // Idle -> Holster
        if (holsterState != null)
        {
            AnimatorStateTransition toHolster = idleState.AddTransition(holsterState);
            toHolster.AddCondition(AnimatorConditionMode.If, 0, "Holster");
            toHolster.hasExitTime = false;
            toHolster.duration = 0.1f;

            // Holster se queda (no vuelve a Idle porque el arma se desactiva)
        }

        // Guardar
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("¡Éxito!", 
            $"Animator Controller creado en:\n{fullPath}\n\n" +
            "Ahora:\n" +
            "1. Asigna este controller al Animator de tu arma\n" +
            "2. Configura los tiempos (drawTime, holsterTime) en WeaponController", 
            "OK");

        // Seleccionar el controller creado
        Selection.activeObject = controller;
    }
}
