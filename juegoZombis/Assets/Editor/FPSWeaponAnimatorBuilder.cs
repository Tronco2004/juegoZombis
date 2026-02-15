using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Genera Animator Controllers automáticamente para armas FPS con animaciones del FBX.
/// Crea estados: Idle, Fire, Reload, Draw (si existe), Holster (si existe)
/// y parámetros: Fire (trigger), Reload (trigger), IsRunning (bool), IsMoving (bool)
/// 
/// Uso: Tools > FPS Weapon Animator Builder
/// </summary>
public class FPSWeaponAnimatorBuilder : EditorWindow
{
    // Datos de cada arma
    private struct WeaponData
    {
        public string name;
        public string fbxPath;
        public string outputPath;
    }

    [MenuItem("Tools/FPS Weapon Animator Builder")]
    static void ShowWindow()
    {
        GetWindow<FPSWeaponAnimatorBuilder>("FPS Weapon Animator Builder");
    }

    private Vector2 scrollPos;
    private string customFbxPath = "";
    private string customName = "";

    void OnGUI()
    {
        GUILayout.Label("🔫 FPS Weapon Animator Builder", EditorStyles.boldLabel);
        GUILayout.Space(5);
        GUILayout.Label("Genera Animator Controllers con las animaciones de tus FBX de armas.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Botones para armas existentes
        GUILayout.Label("=== Armas Detectadas ===", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("🔫 Generar Animator - Pistola G17 (brazosPipa)", GUILayout.Height(30)))
        {
            BuildAnimator("G17_Pistol", "Assets/brazosPipa/source/G17 Pistol - Animated.fbx", "Assets/brazosPipa/");
        }
        if (GUILayout.Button("🔫 Generar Animator - AKM (brazosAk)", GUILayout.Height(30)))
        {
            BuildAnimator("AKM", "Assets/brazosAk/source/AKM_Animated.fbx", "Assets/brazosAk/");
        }
        if (GUILayout.Button("🔫 Generar Animator - M16 A2 (brazosM16)", GUILayout.Height(30)))
        {
            BuildAnimator("M16A2", "Assets/brazosM16/source/M16 A2 Rifle - Animated.fbx", "Assets/brazosM16/");
        }
        if (GUILayout.Button("🔫 Generar Animator - Dual Mac10 (brazosUzi)", GUILayout.Height(30)))
        {
            BuildAnimator("DualMac10", "Assets/brazosUzi/source/Dual Mac10 - Animated.fbx", "Assets/brazosUzi/");
        }

        GUILayout.Space(10);
        if (GUILayout.Button("⚡ GENERAR TODOS", GUILayout.Height(40)))
        {
            BuildAnimator("G17_Pistol", "Assets/brazosPipa/source/G17 Pistol - Animated.fbx", "Assets/brazosPipa/");
            BuildAnimator("AKM", "Assets/brazosAk/source/AKM_Animated.fbx", "Assets/brazosAk/");
            BuildAnimator("M16A2", "Assets/brazosM16/source/M16 A2 Rifle - Animated.fbx", "Assets/brazosM16/");
            BuildAnimator("DualMac10", "Assets/brazosUzi/source/Dual Mac10 - Animated.fbx", "Assets/brazosUzi/");
            EditorUtility.DisplayDialog("¡Listo!", "Se han generado los 4 Animator Controllers.\nRevisa la consola para más detalles.", "OK");
        }

        GUILayout.Space(20);
        GUILayout.Label("=== Arma Personalizada ===", EditorStyles.boldLabel);
        customName = EditorGUILayout.TextField("Nombre:", customName);
        customFbxPath = EditorGUILayout.TextField("Ruta FBX:", customFbxPath);
        
        EditorGUILayout.HelpBox(
            "Arrastra tu FBX aquí o escribe la ruta.\n" +
            "El FBX debe tener clips llamados: Idle, Fire, Reload (y opcionalmente Draw, Holster)", 
            MessageType.Info);

        if (GUILayout.Button("Generar Animator Personalizado", GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(customName) && !string.IsNullOrEmpty(customFbxPath))
            {
                string dir = Path.GetDirectoryName(customFbxPath).Replace("\\", "/") + "/";
                BuildAnimator(customName, customFbxPath, dir);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Rellena el nombre y la ruta del FBX", "OK");
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Construye un Animator Controller con las animaciones del FBX
    /// </summary>
    static void BuildAnimator(string weaponName, string fbxPath, string outputDir)
    {
        // Verificar que el FBX existe
        if (!File.Exists(Path.GetFullPath(fbxPath)))
        {
            Debug.LogError($"[FPSWeaponAnimatorBuilder] No se encontró el FBX: {fbxPath}");
            return;
        }

        // Cargar todas las animaciones del FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                clips[clip.name] = clip;
                Debug.Log($"[FPSWeaponAnimatorBuilder] {weaponName} - Clip encontrado: '{clip.name}' ({clip.length:F2}s)");
            }
        }

        if (clips.Count == 0)
        {
            Debug.LogError($"[FPSWeaponAnimatorBuilder] No se encontraron animaciones en: {fbxPath}");
            return;
        }

        // Crear el Animator Controller
        string controllerPath = outputDir + weaponName + "_FPS.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Añadir parámetros
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Holster", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // === CREAR ESTADOS ===
        
        // IDLE (estado por defecto)
        AnimatorState idleState = null;
        if (clips.ContainsKey("Idle"))
        {
            idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
            idleState.motion = clips["Idle"];
            sm.defaultState = idleState;
        }

        // FIRE
        AnimatorState fireState = null;
        if (clips.ContainsKey("Fire"))
        {
            fireState = sm.AddState("Fire", new Vector3(550, -50, 0));
            fireState.motion = clips["Fire"];
        }

        // RELOAD
        AnimatorState reloadState = null;
        if (clips.ContainsKey("Reload"))
        {
            reloadState = sm.AddState("Reload", new Vector3(550, 50, 0));
            reloadState.motion = clips["Reload"];
        }

        // DRAW
        AnimatorState drawState = null;
        if (clips.ContainsKey("Draw"))
        {
            drawState = sm.AddState("Draw", new Vector3(50, -50, 0));
            drawState.motion = clips["Draw"];
        }

        // HOLSTER
        AnimatorState holsterState = null;
        if (clips.ContainsKey("Holster"))
        {
            holsterState = sm.AddState("Holster", new Vector3(50, 50, 0));
            holsterState.motion = clips["Holster"];
        }

        // === TRANSICIONES ===

        if (idleState != null)
        {
            // IDLE → FIRE (trigger Fire)
            if (fireState != null)
            {
                var toFire = idleState.AddTransition(fireState);
                toFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
                toFire.hasExitTime = false;
                toFire.duration = 0.02f;

                // FIRE → IDLE (cuando termina la animación)
                var fireToIdle = fireState.AddTransition(idleState);
                fireToIdle.hasExitTime = true;
                fireToIdle.exitTime = 0.85f;
                fireToIdle.duration = 0.1f;
            }

            // IDLE → RELOAD (trigger Reload)
            if (reloadState != null)
            {
                var toReload = idleState.AddTransition(reloadState);
                toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
                toReload.hasExitTime = false;
                toReload.duration = 0.1f;

                // RELOAD → IDLE
                var reloadToIdle = reloadState.AddTransition(idleState);
                reloadToIdle.hasExitTime = true;
                reloadToIdle.exitTime = 0.95f;
                reloadToIdle.duration = 0.1f;
            }
        }

        // DRAW → IDLE
        if (drawState != null && idleState != null)
        {
            var drawToIdle = drawState.AddTransition(idleState);
            drawToIdle.hasExitTime = true;
            drawToIdle.exitTime = 0.9f;
            drawToIdle.duration = 0.1f;

            // Any State → Draw
            var anyToDraw = sm.AddAnyStateTransition(drawState);
            anyToDraw.AddCondition(AnimatorConditionMode.If, 0, "Draw");
            anyToDraw.hasExitTime = false;
            anyToDraw.duration = 0.05f;
        }

        // IDLE → HOLSTER (trigger Holster) 
        if (holsterState != null && idleState != null)
        {
            var toHolster = idleState.AddTransition(holsterState);
            toHolster.AddCondition(AnimatorConditionMode.If, 0, "Holster");
            toHolster.hasExitTime = false;
            toHolster.duration = 0.1f;
        }

        // Guardar
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"[FPSWeaponAnimatorBuilder] ✅ Animator Controller creado: {controllerPath} ({clips.Count} animaciones)");
    }
}
