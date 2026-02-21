using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Ventana de editor para configurar el AnimatorController de un zombi.
/// Permite asignar clips de Idle, Walk, Run, Attack y Die,
/// y genera un controller 100% compatible con ZombieAnimationController.cs.
///
/// Menú: Tools → Zombie Animator Setup
/// </summary>
public class ZombieAnimatorSetup : EditorWindow
{
    // ── Clips de animación ────────────────────────────────────
    private AnimationClip idleClip;
    private AnimationClip walkClip;
    private AnimationClip runClip;
    private AnimationClip attackClip;
    private AnimationClip dieClip;

    // ── Clips opcionales ──────────────────────────────────────
    private AnimationClip hitClip;
    private AnimationClip crawlClip;

    // ── Configuración de salida ───────────────────────────────
    private string outputFolder = "Assets/AnimacionesZombis";
    private string controllerName = "ZombieAnimatorController";

    // ── Opciones avanzadas ────────────────────────────────────
    private bool showAdvanced = false;
    private float transitionDuration = 0.15f;
    private float attackExitTime = 0.85f;
    private bool loopIdle = true;
    private bool loopWalk = true;
    private bool loopRun  = true;
    private bool loopDie  = false;

    // ── Asignar a prefab ──────────────────────────────────────
    private GameObject targetPrefab;

    // ── Scroll ────────────────────────────────────────────────
    private Vector2 scrollPos;

    [MenuItem("Tools/Zombie Animator Setup")]
    public static void ShowWindow()
    {
        var w = GetWindow<ZombieAnimatorSetup>("Zombie Animator Setup");
        w.minSize = new Vector2(420, 600);
    }

    // ══════════════════════════════════════════════════════════
    //  GUI
    // ══════════════════════════════════════════════════════════

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawHeader();
        EditorGUILayout.Space(8);

        DrawClipFields();
        EditorGUILayout.Space(8);

        DrawOutputSettings();
        EditorGUILayout.Space(8);

        DrawAdvancedOptions();
        EditorGUILayout.Space(8);

        DrawPrefabAssignment();
        EditorGUILayout.Space(12);

        DrawGenerateButton();

        EditorGUILayout.EndScrollView();
    }

    // ── Header ────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Zombie Animator Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Arrastra los clips de animación y pulsa \"Generar Animator\".\n" +
            "El controller generado es 100% compatible con ZombieAnimationController.cs.\n\n" +
            "Obligatorios: Idle, Walk, Attack, Die.\n" +
            "Opcionales : Run (si no se asigna, Walk se usa para correr), Hit, Crawl.",
            MessageType.Info);
    }

    // ── Clip fields ───────────────────────────────────────────

    private void DrawClipFields()
    {
        EditorGUILayout.LabelField("Animaciones Principales", EditorStyles.boldLabel);

        idleClip   = ClipField("Idle *",   idleClip);
        walkClip   = ClipField("Walk *",   walkClip);
        runClip    = ClipField("Run",       runClip);
        attackClip = ClipField("Attack *",  attackClip);
        dieClip    = ClipField("Die *",     dieClip);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Animaciones Opcionales", EditorStyles.boldLabel);
        hitClip   = ClipField("Hit",   hitClip);
        crawlClip = ClipField("Crawl", crawlClip);
    }

    private AnimationClip ClipField(string label, AnimationClip clip)
    {
        return (AnimationClip)EditorGUILayout.ObjectField(label, clip, typeof(AnimationClip), false);
    }

    // ── Output settings ───────────────────────────────────────

    private void DrawOutputSettings()
    {
        EditorGUILayout.LabelField("Ruta de Salida", EditorStyles.boldLabel);
        outputFolder    = EditorGUILayout.TextField("Carpeta", outputFolder);
        controllerName  = EditorGUILayout.TextField("Nombre", controllerName);

        string fullPath = $"{outputFolder}/{controllerName}.controller";
        EditorGUILayout.HelpBox($"Se guardará en: {fullPath}", MessageType.None);
    }

    // ── Advanced ──────────────────────────────────────────────

    private void DrawAdvancedOptions()
    {
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Opciones Avanzadas", true);
        if (!showAdvanced) return;

        EditorGUI.indentLevel++;
        transitionDuration = EditorGUILayout.Slider("Duración Transiciones", transitionDuration, 0f, 0.5f);
        attackExitTime     = EditorGUILayout.Slider("Attack Exit Time", attackExitTime, 0.5f, 1f);
        loopIdle = EditorGUILayout.Toggle("Loop Idle", loopIdle);
        loopWalk = EditorGUILayout.Toggle("Loop Walk", loopWalk);
        loopRun  = EditorGUILayout.Toggle("Loop Run",  loopRun);
        loopDie  = EditorGUILayout.Toggle("Loop Die",  loopDie);
        EditorGUI.indentLevel--;
    }

    // ── Prefab assignment ─────────────────────────────────────

    private void DrawPrefabAssignment()
    {
        EditorGUILayout.LabelField("Asignar a Prefab (opcional)", EditorStyles.boldLabel);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab Zombi", targetPrefab, typeof(GameObject), false);

        if (targetPrefab != null)
        {
            EditorGUILayout.HelpBox(
                "Al generar, se asignará automáticamente el Animator Controller al " +
                "componente Animator del prefab.", MessageType.None);
        }
    }

    // ── Generate button ───────────────────────────────────────

    private void DrawGenerateButton()
    {
        // Validar obligatorios
        bool valid = idleClip != null && walkClip != null &&
                     attackClip != null && dieClip != null;

        EditorGUI.BeginDisabledGroup(!valid);
        if (GUILayout.Button("Generar Animator Controller", GUILayout.Height(36)))
        {
            GenerateAnimatorController();
        }
        EditorGUI.EndDisabledGroup();

        if (!valid)
        {
            EditorGUILayout.HelpBox(
                "Asigna al menos los clips marcados con * (Idle, Walk, Attack, Die).",
                MessageType.Warning);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  GENERACIÓN DEL ANIMATOR CONTROLLER
    // ══════════════════════════════════════════════════════════

    private void GenerateAnimatorController()
    {
        // ── 0. Crear carpeta si no existe ─────────────────────────────
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            CreateFolderRecursive(outputFolder);
        }

        string fullPath = $"{outputFolder}/{controllerName}.controller";

        // Borrar si ya existe para recrear limpio
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(fullPath) != null)
        {
            AssetDatabase.DeleteAsset(fullPath);
        }

        // ── 1. Crear controller ───────────────────────────────────────
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        // ── 2. Parámetros (compatibles con ZombieAnimationController) ─
        ac.AddParameter("Speed",      AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("IsDead",     AnimatorControllerParameterType.Bool);
        ac.AddParameter("IsHit",      AnimatorControllerParameterType.Bool);
        ac.AddParameter("IsCrawling", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine root = ac.layers[0].stateMachine;

        // ── 3. LOCOMOTION — BlendTree 1D ──────────────────────────────
        //    Speed 0 = Idle, Speed 0.5 = Walk, Speed 1 = Run
        BlendTree locomotionTree;
        AnimatorState locomotionState = ac.CreateBlendTreeInController(
            "Locomotion", out locomotionTree);

        locomotionTree.blendType      = BlendTreeType.Simple1D;
        locomotionTree.blendParameter = "Speed";
        locomotionTree.useAutomaticThresholds = false;

        locomotionTree.AddChild(idleClip, 0f);     // Speed = 0
        locomotionTree.AddChild(walkClip, 0.5f);   // Speed = 0.5

        if (runClip != null)
            locomotionTree.AddChild(runClip, 1f);   // Speed = 1
        else
            locomotionTree.AddChild(walkClip, 1f);  // Sin run → reutilizar walk

        locomotionState.motion = locomotionTree;

        // ── 4. ATTACK ─────────────────────────────────────────────────
        AnimatorState attackState = root.AddState("Attack");
        attackState.motion = attackClip;
        attackState.tag    = "Attack";

        // ── 5. DIE ────────────────────────────────────────────────────
        AnimatorState dieState = root.AddState("Die");
        dieState.motion = dieClip;
        dieState.tag    = "Death";

        // ── 6. HIT (opcional) ─────────────────────────────────────────
        AnimatorState hitState = root.AddState("Hit");
        if (hitClip != null)
            hitState.motion = hitClip;
        hitState.tag = "Hit";

        // ── 7. CRAWL (opcional) ───────────────────────────────────────
        AnimatorState crawlState = null;
        if (crawlClip != null)
        {
            crawlState = root.AddState("Crawl");
            crawlState.motion = crawlClip;
        }

        // ── 8. Estado por defecto ─────────────────────────────────────
        root.defaultState = locomotionState;

        // ══════════════════════════════════════════════════════════
        //  TRANSICIONES
        // ══════════════════════════════════════════════════════════

        // --- Locomotion → Attack ---
        var t_loco_attack = locomotionState.AddTransition(attackState);
        t_loco_attack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        t_loco_attack.hasExitTime = false;
        t_loco_attack.duration    = transitionDuration;

        // --- Attack → Locomotion (al terminar) ---
        var t_attack_loco = attackState.AddTransition(locomotionState);
        t_attack_loco.hasExitTime = true;
        t_attack_loco.exitTime    = attackExitTime;
        t_attack_loco.duration    = transitionDuration;

        // --- Locomotion → Die ---
        var t_loco_die = locomotionState.AddTransition(dieState);
        t_loco_die.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        t_loco_die.hasExitTime = false;
        t_loco_die.duration    = transitionDuration;

        // --- Attack → Die ---
        var t_attack_die = attackState.AddTransition(dieState);
        t_attack_die.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        t_attack_die.hasExitTime = false;
        t_attack_die.duration    = transitionDuration;

        // --- Hit → Die (por si muere en medio de un hit) ---
        var t_hit_die = hitState.AddTransition(dieState);
        t_hit_die.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        t_hit_die.hasExitTime = false;
        t_hit_die.duration    = transitionDuration;

        // --- Locomotion → Hit ---
        var t_loco_hit = locomotionState.AddTransition(hitState);
        t_loco_hit.AddCondition(AnimatorConditionMode.If, 0, "IsHit");
        t_loco_hit.hasExitTime = false;
        t_loco_hit.duration    = 0.05f;

        // --- Hit → Locomotion (al terminar) ---
        var t_hit_loco = hitState.AddTransition(locomotionState);
        t_hit_loco.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHit");
        t_hit_loco.hasExitTime = true;
        t_hit_loco.exitTime    = 0.8f;
        t_hit_loco.duration    = transitionDuration;

        // --- Crawl (si existe) ---
        if (crawlState != null)
        {
            // Locomotion → Crawl
            var t_loco_crawl = locomotionState.AddTransition(crawlState);
            t_loco_crawl.AddCondition(AnimatorConditionMode.If, 0, "IsCrawling");
            t_loco_crawl.hasExitTime = false;
            t_loco_crawl.duration    = transitionDuration;

            // Crawl → Locomotion (si se cura)
            var t_crawl_loco = crawlState.AddTransition(locomotionState);
            t_crawl_loco.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrawling");
            t_crawl_loco.hasExitTime = false;
            t_crawl_loco.duration    = transitionDuration;

            // Crawl → Die
            var t_crawl_die = crawlState.AddTransition(dieState);
            t_crawl_die.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            t_crawl_die.hasExitTime = false;
            t_crawl_die.duration    = transitionDuration;

            // Crawl → Attack (puede atacar arrastrándose)
            var t_crawl_attack = crawlState.AddTransition(attackState);
            t_crawl_attack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            t_crawl_attack.hasExitTime = false;
            t_crawl_attack.duration    = transitionDuration;
        }

        // ══════════════════════════════════════════════════════════
        //  GUARDAR
        // ══════════════════════════════════════════════════════════

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Asignar a prefab si se indicó ─────────────────────────────
        if (targetPrefab != null)
        {
            AssignToPrefab(ac);
        }

        Debug.Log($"[ZombieAnimatorSetup] ✅ AnimatorController generado en: {fullPath}");
        EditorGUIUtility.PingObject(ac);
        Selection.activeObject = ac;
    }

    // ══════════════════════════════════════════════════════════
    //  UTILIDADES
    // ══════════════════════════════════════════════════════════

    private void AssignToPrefab(AnimatorController ac)
    {
        // Abrir prefab para edición
        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("[ZombieAnimatorSetup] El prefab no tiene asset path. " +
                             "Asegúrate de usar un prefab del proyecto, no de la escena.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        Animator animator = prefabRoot.GetComponent<Animator>();
        if (animator == null)
            animator = prefabRoot.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = ac;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[ZombieAnimatorSetup] ✅ Animator asignado al prefab: {prefabPath}");
        }
        else
        {
            Debug.LogWarning($"[ZombieAnimatorSetup] No se encontró Animator en el prefab: {prefabPath}");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private void CreateFolderRecursive(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
