using UnityEngine;
using UnityEditor;

/// <summary>
/// Script de editor para configurar las animaciones del G17 Pistol
/// Menú: Tools > Setup G17 Animations
/// </summary>
public class SetupG17Animations : MonoBehaviour
{
    [MenuItem("Tools/Setup G17 Animations")]
    public static void Setup()
    {
        // Ruta del FBX
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        
        if (importer == null)
        {
            Debug.LogError("No se encontró el FBX en: " + fbxPath);
            return;
        }
        
        // Configurar como Generic (no Humanoid porque son solo brazos)
        importer.animationType = ModelImporterAnimationType.Generic;
        
        // Obtener los clips existentes del FBX
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        
        if (clips.Length == 0)
        {
            Debug.LogWarning("No se encontraron animaciones en el FBX. Creando clips por defecto...");
            
            // Si no hay clips, crear uno por defecto
            clips = new ModelImporterClipAnimation[]
            {
                new ModelImporterClipAnimation
                {
                    name = "Idle",
                    takeName = "Take 001",
                    firstFrame = 0,
                    lastFrame = 60,
                    loop = true,
                    loopTime = true
                }
            };
        }
        else
        {
            Debug.Log("Animaciones encontradas: " + clips.Length);
            foreach (var clip in clips)
            {
                Debug.Log(" - " + clip.name + " (Take: " + clip.takeName + ", Frames: " + clip.firstFrame + "-" + clip.lastFrame + ")");
            }
        }
        
        // Aplicar configuración de loop según el nombre
        for (int i = 0; i < clips.Length; i++)
        {
            string clipName = clips[i].name.ToLower();
            
            // Idle debe tener loop
            if (clipName.Contains("idle"))
            {
                clips[i].loop = true;
                clips[i].loopTime = true;
            }
            else
            {
                // Fire, Reload, Draw no deben tener loop
                clips[i].loop = false;
                clips[i].loopTime = false;
            }
        }
        
        importer.clipAnimations = clips;
        
        // Guardar cambios
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        
        Debug.Log("✓ G17 configurado correctamente!");
        Debug.Log("Ahora ejecuta: Tools > Create G17 Animator Controller");
    }
    
    [MenuItem("Tools/Create G17 Animator Controller")]
    public static void CreateAnimatorController()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        string controllerPath = "Assets/brazosPipa/G17_AnimatorController.controller";
        
        // Crear Animator Controller
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Añadir parámetros
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
        
        // Obtener la capa base
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Cargar las animaciones del FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        AnimationClip idleClip = null;
        AnimationClip fireClip = null;
        AnimationClip reloadClip = null;
        AnimationClip drawClip = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                string clipName = clip.name.ToLower();
                Debug.Log("Clip encontrado: " + clip.name);
                
                if (clipName.Contains("idle") || clipName.Contains("pose"))
                    idleClip = clip;
                else if (clipName.Contains("fire") || clipName.Contains("shoot"))
                    fireClip = clip;
                else if (clipName.Contains("reload"))
                    reloadClip = clip;
                else if (clipName.Contains("draw") || clipName.Contains("equip"))
                    drawClip = clip;
            }
        }
        
        // Si no hay idle, usar el primer clip disponible
        if (idleClip == null)
        {
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    idleClip = clip;
                    break;
                }
            }
        }
        
        // Crear estados
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        if (idleClip != null) idleState.motion = idleClip;
        rootStateMachine.defaultState = idleState;
        
        if (fireClip != null)
        {
            var fireState = rootStateMachine.AddState("Fire", new Vector3(550, -50, 0));
            fireState.motion = fireClip;
            
            // Transición Idle -> Fire
            var toFire = idleState.AddTransition(fireState);
            toFire.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Fire");
            toFire.hasExitTime = false;
            toFire.duration = 0.05f;
            
            // Transición Fire -> Idle
            var fromFire = fireState.AddTransition(idleState);
            fromFire.hasExitTime = true;
            fromFire.exitTime = 0.9f;
            fromFire.duration = 0.1f;
        }
        
        if (reloadClip != null)
        {
            var reloadState = rootStateMachine.AddState("Reload", new Vector3(550, 50, 0));
            reloadState.motion = reloadClip;
            
            // Transición Idle -> Reload
            var toReload = idleState.AddTransition(reloadState);
            toReload.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Reload");
            toReload.hasExitTime = false;
            toReload.duration = 0.1f;
            
            // Transición Reload -> Idle
            var fromReload = reloadState.AddTransition(idleState);
            fromReload.hasExitTime = true;
            fromReload.exitTime = 0.95f;
            fromReload.duration = 0.1f;
        }
        
        if (drawClip != null)
        {
            var drawState = rootStateMachine.AddState("Draw", new Vector3(300, -100, 0));
            drawState.motion = drawClip;
            
            // Transición Entry -> Draw (opcional)
            var toDraw = idleState.AddTransition(drawState);
            toDraw.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Draw");
            toDraw.hasExitTime = false;
            toDraw.duration = 0.05f;
            
            // Transición Draw -> Idle
            var fromDraw = drawState.AddTransition(idleState);
            fromDraw.hasExitTime = true;
            fromDraw.exitTime = 0.95f;
            fromDraw.duration = 0.1f;
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("✓ Animator Controller creado en: " + controllerPath);
    }
    
    [MenuItem("Tools/Show G17 Animation Info")]
    public static void ShowAnimationInfo()
    {
        string fbxPath = "Assets/brazosPipa/source/G17 Pistol - Animated.fbx";
        
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Debug.Log("=== Contenido del FBX G17 ===");
        
        int clipCount = 0;
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                clipCount++;
                Debug.Log($"Animación: {clip.name} | Duración: {clip.length:F2}s | Loop: {clip.isLooping}");
            }
            else if (asset is Mesh mesh)
            {
                Debug.Log($"Mesh: {mesh.name}");
            }
        }
        
        Debug.Log($"Total animaciones: {clipCount}");
    }
}
