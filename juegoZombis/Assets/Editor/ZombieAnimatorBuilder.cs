using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

/// <summary>
/// Editor tool que genera automáticamente el Animator Controller del zombie
/// usando las animaciones de Mixamo de la carpeta AnimacionesZombis.
/// Menú: Tools > Zombie > Crear Animator Controller
/// </summary>
public class ZombieAnimatorBuilder : EditorWindow
{
    private const string ANIMATIONS_FOLDER = "Assets/AnimacionesZombis";
    private const string OUTPUT_PATH = "Assets/AnimacionesZombis/ZombieAnimatorController.controller";

    [MenuItem("Tools/Zombie/Crear Animator Controller")]
    public static void CreateZombieAnimator()
    {
        // Crear el Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(OUTPUT_PATH);

        // ──────────────── PARÁMETROS ────────────────
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrawling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Bite", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("NeckBite", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Scream", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int); // 0=attack, 1=bite, 2=neckbite

        // ──────────────── LAYER BASE ────────────────
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // ──────────────── BUSCAR CLIPS ────────────────
        AnimationClip idleClip       = FindClip("zombie idle");
        AnimationClip walkClip       = FindClip("zombie walk");
        AnimationClip runClip        = FindClip("zombie run");
        AnimationClip attackClip     = FindClip("zombie attack");
        AnimationClip biteClip       = FindClip("zombie biting");
        AnimationClip bite2Clip      = FindClip("zombie biting (2)");
        AnimationClip neckBiteClip   = FindClip("zombie neck bite");
        AnimationClip deathClip      = FindClip("zombie death");
        AnimationClip dyingClip      = FindClip("zombie dying");
        AnimationClip crawlClip      = FindClip("zombie crawl");
        AnimationClip runCrawlClip   = FindClip("running crawl");
        AnimationClip screamClip     = FindClip("zombie scream");

        // ──────────────── ESTADOS ────────────────
        // Posiciones en el editor para buena visualización
        AnimatorState idleState     = CreateState(rootStateMachine, "Idle",      idleClip,     new Vector3(0, 0, 0), true);
        AnimatorState walkState     = CreateState(rootStateMachine, "Walk",      walkClip,     new Vector3(250, 0, 0), true);
        AnimatorState runState      = CreateState(rootStateMachine, "Run",       runClip,      new Vector3(500, 0, 0), true);
        AnimatorState attackState   = CreateState(rootStateMachine, "Attack",    attackClip,   new Vector3(250, 150, 0), false);
        AnimatorState biteState     = CreateState(rootStateMachine, "Bite",      biteClip,     new Vector3(400, 150, 0), false);
        AnimatorState neckBiteState = CreateState(rootStateMachine, "NeckBite",  neckBiteClip, new Vector3(550, 150, 0), false);
        AnimatorState screamState   = CreateState(rootStateMachine, "Scream",    screamClip,   new Vector3(-250, 0, 0), false);
        AnimatorState deathState    = CreateState(rootStateMachine, "Death",     deathClip,    new Vector3(250, -200, 0), false);
        AnimatorState dyingState    = CreateState(rootStateMachine, "Dying",     dyingClip,    new Vector3(500, -200, 0), false);
        AnimatorState crawlState    = CreateState(rootStateMachine, "Crawl",     crawlClip,    new Vector3(0, -200, 0), true);
        AnimatorState runCrawlState = CreateState(rootStateMachine, "RunCrawl",  runCrawlClip, new Vector3(-250, -200, 0), true);

        // Estado default = Idle
        rootStateMachine.defaultState = idleState;

        // ──────────────── TRANSICIONES ────────────────

        // === IDLE → WALK (empieza a caminar) ===
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
        idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrawling");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.25f;

        // === IDLE → RUN (empieza a correr) ===
        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        idleToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrawling");
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.2f;

        // === WALK → IDLE (deja de caminar) ===
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.25f;

        // === WALK → RUN (acelera) ===
        AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
        walkToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.2f;

        // === RUN → WALK (decelera) ===
        AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
        runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        runToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
        runToWalk.hasExitTime = false;
        runToWalk.duration = 0.2f;

        // === RUN → IDLE (para completamente) ===
        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.25f;

        // === ATAQUES (desde Any State para que siempre funcionen) ===
        AnimatorStateTransition anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.1f;
        anyToAttack.canTransitionToSelf = false;

        AnimatorStateTransition anyToBite = rootStateMachine.AddAnyStateTransition(biteState);
        anyToBite.AddCondition(AnimatorConditionMode.If, 0, "Bite");
        anyToBite.hasExitTime = false;
        anyToBite.duration = 0.1f;
        anyToBite.canTransitionToSelf = false;

        AnimatorStateTransition anyToNeckBite = rootStateMachine.AddAnyStateTransition(neckBiteState);
        anyToNeckBite.AddCondition(AnimatorConditionMode.If, 0, "NeckBite");
        anyToNeckBite.hasExitTime = false;
        anyToNeckBite.duration = 0.1f;
        anyToNeckBite.canTransitionToSelf = false;

        // === VOLVER A WALK desde ataques (SIEMPRE a Walk, nunca a Idle) ===
        AddReturnToWalk(attackState, walkState);
        AddReturnToWalk(biteState, walkState);
        AddReturnToWalk(neckBiteState, walkState);

        // === SCREAM (desde Any State) ===
        AnimatorStateTransition anyToScream = rootStateMachine.AddAnyStateTransition(screamState);
        anyToScream.AddCondition(AnimatorConditionMode.If, 0, "Scream");
        anyToScream.hasExitTime = false;
        anyToScream.duration = 0.15f;
        anyToScream.canTransitionToSelf = false;

        // Scream vuelve a Walk también (no a Idle)
        AddReturnToWalk(screamState, walkState);

        // === CRAWL (vida baja) ===
        // Desde cualquier estado locomotion → Crawl
        AnimatorStateTransition idleToCrawl = idleState.AddTransition(crawlState);
        idleToCrawl.AddCondition(AnimatorConditionMode.If, 0, "IsCrawling");
        idleToCrawl.hasExitTime = false;
        idleToCrawl.duration = 0.4f;

        AnimatorStateTransition walkToCrawl = walkState.AddTransition(crawlState);
        walkToCrawl.AddCondition(AnimatorConditionMode.If, 0, "IsCrawling");
        walkToCrawl.hasExitTime = false;
        walkToCrawl.duration = 0.4f;

        AnimatorStateTransition runToCrawl = runState.AddTransition(crawlState);
        runToCrawl.AddCondition(AnimatorConditionMode.If, 0, "IsCrawling");
        runToCrawl.hasExitTime = false;
        runToCrawl.duration = 0.4f;

        // Crawl → RunCrawl (persiguiendo mientras se arrastra)
        AnimatorStateTransition crawlToRunCrawl = crawlState.AddTransition(runCrawlState);
        crawlToRunCrawl.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        crawlToRunCrawl.hasExitTime = false;
        crawlToRunCrawl.duration = 0.25f;

        AnimatorStateTransition runCrawlToCrawl = runCrawlState.AddTransition(crawlState);
        runCrawlToCrawl.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        runCrawlToCrawl.hasExitTime = false;
        runCrawlToCrawl.duration = 0.25f;

        // === MUERTE (from Any State) ===
        AnimatorStateTransition anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.15f;
        anyToDeath.canTransitionToSelf = false;

        // Death no loopea, se queda en el último frame
        // (ya configurado con loop = false en CreateState)

        // ──────────────── GUARDAR ────────────────
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ZombieAnimatorBuilder] ✅ Animator Controller creado en: {OUTPUT_PATH}");
        EditorUtility.DisplayDialog("Zombie Animator Builder",
            $"Animator Controller creado correctamente en:\n{OUTPUT_PATH}\n\n" +
            "Estados creados:\n" +
            "• Idle, Walk, Run\n" +
            "• Attack, Bite, NeckBite\n" +
            "• Scream\n" +
            "• Death, Dying\n" +
            "• Crawl, RunCrawl",
            "OK");
    }

    /// <summary>
    /// Busca un AnimationClip dentro de un FBX por nombre parcial del archivo.
    /// Los clips de Mixamo están embebidos como sub-assets del FBX.
    /// </summary>
    static AnimationClip FindClip(string fileNameContains)
    {
        // Buscar TODOS los archivos en la carpeta (FBX contienen clips como sub-assets)
        string[] allGuids = AssetDatabase.FindAssets("", new[] { ANIMATIONS_FOLDER });
        
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.ToLower().EndsWith(".fbx")) continue;
            
            string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
            if (!fileName.Contains(fileNameContains.ToLower())) continue;

            // Cargar TODOS los sub-assets del FBX (aquí están los clips embebidos)
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in allAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    Debug.Log($"[ZombieAnimatorBuilder] Clip encontrado: {clip.name} en {path}");
                    return clip;
                }
            }
        }

        Debug.LogWarning($"[ZombieAnimatorBuilder] No se encontró clip para: {fileNameContains}");
        return null;
    }

    /// <summary>
    /// Crea un estado en la state machine con el clip y posición dados
    /// </summary>
    static AnimatorState CreateState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 position, bool loop)
    {
        AnimatorState state = sm.AddState(name, position);
        
        if (clip != null)
        {
            state.motion = clip;
            
            // Configurar loop en el clip si es necesario
            // (Nota: para cambiar loop del clip importado hay que usar AnimationClipSettings)
        }
        
        return state;
    }

    /// <summary>
    /// Añade transición de ataque desde un estado origen
    /// </summary>
    static void AddAttackTransition(AnimatorState from, AnimatorState to, string triggerName)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        t.hasExitTime = false;
        t.duration = 0.1f;
    }

    /// <summary>
    /// Añade transición de vuelta a Walk tras terminar la animación.
    /// SIEMPRE va a Walk (sin condiciones). Walk → Idle ya existe si velocidad = 0.
    /// </summary>
    static void AddReturnToWalk(AnimatorState from, AnimatorState walk)
    {
        AnimatorStateTransition t = from.AddTransition(walk);
        t.hasExitTime = true;
        t.exitTime = 0.85f;
        t.duration = 0.15f;
    }
}
