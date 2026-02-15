using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Herramienta de Editor para crear Animator Controllers para armas melee FPS
/// Detecta automáticamente las animaciones del FBX (Idle, Attack, Draw, etc.)
/// </summary>
public class FPSMeleeAnimatorBuilder : EditorWindow
{
    private GameObject selectedFBX;
    private string outputPath = "Assets/AnimacionesMelee";
    private string animatorName = "MeleeAnimator";
    
    // Animaciones detectadas
    private AnimationClip idleClip;
    private AnimationClip attackClip;
    private AnimationClip attack2Clip;
    private AnimationClip drawClip;
    
    private Vector2 scrollPos;
    
    [MenuItem("Tools/FPS Melee Animator Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<FPSMeleeAnimatorBuilder>("Melee Animator Builder");
        window.minSize = new Vector2(400, 500);
    }
    
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        EditorGUILayout.LabelField("FPS Melee Animator Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Esta herramienta crea automáticamente un Animator Controller para armas melee " +
            "(cuchillo, machete, etc.) a partir de un modelo FBX con animaciones embebidas.", 
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Selección de FBX
        EditorGUILayout.LabelField("1. Selecciona el modelo FBX del arma:", EditorStyles.boldLabel);
        selectedFBX = (GameObject)EditorGUILayout.ObjectField("FBX Model", selectedFBX, typeof(GameObject), false);
        
        if (selectedFBX != null)
        {
            // Detectar animaciones
            DetectAnimations();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. Animaciones detectadas:", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            DisplayClipField("Idle", ref idleClip, "Idle|idle|IDLE");
            DisplayClipField("Attack", ref attackClip, "Attack|attack|Fire|fire|Slash|slash|Stab|stab");
            DisplayClipField("Attack2 (opcional)", ref attack2Clip, "Attack2|attack2|Fire2|Slash2|Stab2");
            DisplayClipField("Draw", ref drawClip, "Draw|draw|Equip|equip|Take|take");
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3. Configuración de salida:", EditorStyles.boldLabel);
            
            outputPath = EditorGUILayout.TextField("Carpeta de salida", outputPath);
            animatorName = EditorGUILayout.TextField("Nombre del Animator", animatorName);
            
            EditorGUILayout.Space(20);
            
            // Validación
            bool canCreate = idleClip != null && attackClip != null;
            
            if (!canCreate)
            {
                EditorGUILayout.HelpBox("Se requieren al menos las animaciones Idle y Attack para crear el Animator.", MessageType.Warning);
            }
            
            EditorGUI.BeginDisabledGroup(!canCreate);
            if (GUILayout.Button("Crear Animator Controller", GUILayout.Height(40)))
            {
                CreateAnimatorController();
            }
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            EditorGUILayout.HelpBox("Arrastra un modelo FBX con animaciones de arma melee aquí.", MessageType.Info);
        }
        
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Modelos FBX de armas melee en el proyecto:", EditorStyles.boldLabel);
        
        // Buscar FBXs de armas melee
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("knife") || path.ToLower().Contains("cuchillo") || 
                path.ToLower().Contains("melee") || path.ToLower().Contains("machete") ||
                path.ToLower().Contains("sword") || path.ToLower().Contains("axe"))
            {
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbx != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Path.GetFileName(path));
                    if (GUILayout.Button("Seleccionar", GUILayout.Width(80)))
                    {
                        selectedFBX = fbx;
                    }
                    EditorGUILayout.EndHorizontal();
                    count++;
                }
            }
        }
        
        if (count == 0)
        {
            EditorGUILayout.HelpBox("No se encontraron modelos de armas melee. Importa un FBX que contenga 'knife', 'cuchillo', 'melee', 'machete', 'sword' o 'axe' en el nombre.", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void DisplayClipField(string label, ref AnimationClip clip, string patterns)
    {
        EditorGUILayout.BeginHorizontal();
        clip = (AnimationClip)EditorGUILayout.ObjectField(label, clip, typeof(AnimationClip), false);
        
        if (clip != null)
        {
            EditorGUILayout.LabelField("✓", GUILayout.Width(20));
        }
        else
        {
            EditorGUILayout.LabelField("✗", GUILayout.Width(20));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DetectAnimations()
    {
        if (selectedFBX == null) return;
        
        string assetPath = AssetDatabase.GetAssetPath(selectedFBX);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        
        // Resetear
        idleClip = null;
        attackClip = null;
        attack2Clip = null;
        drawClip = null;
        
        foreach (Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__")) continue;
            
            string clipName = clip.name.ToLower();
            
            // Detectar Idle
            if (idleClip == null && (clipName.Contains("idle") || clipName.Contains("stance")))
            {
                idleClip = clip;
            }
            // Detectar Attack principal
            else if (attackClip == null && (clipName.Contains("attack") || clipName.Contains("fire") || 
                     clipName.Contains("slash") || clipName.Contains("stab") || clipName.Contains("swing")))
            {
                // Verificar que no sea attack2
                if (!clipName.Contains("2") && !clipName.Contains("two") && !clipName.Contains("second"))
                {
                    attackClip = clip;
                }
            }
            // Detectar Attack2
            else if (attack2Clip == null && (clipName.Contains("attack2") || clipName.Contains("fire2") || 
                     clipName.Contains("slash2") || clipName.Contains("stab2") || clipName.Contains("swing2") ||
                     clipName.Contains("attack_2") || clipName.Contains("secondary")))
            {
                attack2Clip = clip;
            }
            // Detectar Draw
            else if (drawClip == null && (clipName.Contains("draw") || clipName.Contains("equip") || 
                     clipName.Contains("take") || clipName.Contains("pullout") || clipName.Contains("deploy")))
            {
                drawClip = clip;
            }
        }
        
        // Si no encontró attack2 pero hay un segundo clip de ataque
        if (attack2Clip == null && attackClip != null)
        {
            foreach (Object asset in assets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null || clip == attackClip || clip.name.StartsWith("__preview__")) continue;
                
                string clipName = clip.name.ToLower();
                if (clipName.Contains("attack") || clipName.Contains("slash") || clipName.Contains("stab"))
                {
                    attack2Clip = clip;
                    break;
                }
            }
        }
        
        // Generar nombre automático
        animatorName = selectedFBX.name + "_Animator";
    }
    
    void CreateAnimatorController()
    {
        // Crear directorio si no existe
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            string[] folders = outputPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
        
        // Crear el Animator Controller
        string controllerPath = $"{outputPath}/{animatorName}.controller";
        
        // Verificar si ya existe
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Sobrescribir", 
                $"Ya existe un Animator Controller en {controllerPath}. ¿Deseas sobrescribirlo?", 
                "Sí", "No"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(controllerPath);
        }
        
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Añadir parámetros
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        if (attack2Clip != null)
        {
            controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        }
        if (drawClip != null)
        {
            controller.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
        }
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        
        // Obtener la state machine del layer base
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Crear estado Idle (default)
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idleClip;
        rootStateMachine.defaultState = idleState;
        
        // Crear estado Attack
        AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(550, -100, 0));
        attackState.motion = attackClip;
        
        // Crear estado Attack2 si existe
        AnimatorState attack2State = null;
        if (attack2Clip != null)
        {
            attack2State = rootStateMachine.AddState("Attack2", new Vector3(550, 100, 0));
            attack2State.motion = attack2Clip;
        }
        
        // Crear estado Draw si existe
        AnimatorState drawState = null;
        if (drawClip != null)
        {
            drawState = rootStateMachine.AddState("Draw", new Vector3(100, 0, 0));
            drawState.motion = drawClip;
        }
        
        // === TRANSICIONES ===
        
        // Draw -> Idle (si existe Draw)
        if (drawState != null)
        {
            var drawToIdle = drawState.AddTransition(idleState);
            drawToIdle.hasExitTime = true;
            drawToIdle.exitTime = 0.9f;
            drawToIdle.duration = 0.1f;
            drawToIdle.hasFixedDuration = true;
        }
        
        // Any State -> Attack (trigger)
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.duration = 0.05f;
        anyToAttack.hasFixedDuration = true;
        anyToAttack.canTransitionToSelf = false;
        
        // Attack -> Idle
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.85f;
        attackToIdle.duration = 0.1f;
        attackToIdle.hasFixedDuration = true;
        
        // Si existe Attack2
        if (attack2State != null)
        {
            // Any State -> Attack2 (trigger)
            var anyToAttack2 = rootStateMachine.AddAnyStateTransition(attack2State);
            anyToAttack2.AddCondition(AnimatorConditionMode.If, 0, "Attack2");
            anyToAttack2.duration = 0.05f;
            anyToAttack2.hasFixedDuration = true;
            anyToAttack2.canTransitionToSelf = false;
            
            // Attack2 -> Idle
            var attack2ToIdle = attack2State.AddTransition(idleState);
            attack2ToIdle.hasExitTime = true;
            attack2ToIdle.exitTime = 0.85f;
            attack2ToIdle.duration = 0.1f;
            attack2ToIdle.hasFixedDuration = true;
        }
        
        // Any State -> Draw (si existe)
        if (drawState != null)
        {
            var anyToDraw = rootStateMachine.AddAnyStateTransition(drawState);
            anyToDraw.AddCondition(AnimatorConditionMode.If, 0, "Draw");
            anyToDraw.duration = 0f;
            anyToDraw.hasFixedDuration = true;
            anyToDraw.canTransitionToSelf = false;
        }
        
        // Guardar cambios
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Mostrar resultado
        EditorUtility.DisplayDialog("¡Éxito!", 
            $"Animator Controller creado en:\n{controllerPath}\n\n" +
            $"Estados creados:\n" +
            $"- Idle ✓\n" +
            $"- Attack ✓\n" +
            $"{(attack2Clip != null ? "- Attack2 ✓\n" : "")}" +
            $"{(drawClip != null ? "- Draw ✓\n" : "")}\n" +
            "Asigna este Animator al componente Animator del arma melee.", 
            "OK");
        
        // Seleccionar el asset creado
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
    }
}
