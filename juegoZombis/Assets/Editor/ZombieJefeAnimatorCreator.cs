using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Genera el AnimatorController del Zombi Jefe con Idle, Walk y Attack.
/// Menú: Tools → Crear Animator Zombi Jefe
/// </summary>
public static class ZombieJefeAnimatorCreator
{
    private const string WALK_FBX_PATH   = "Assets/Zombijefe/Zombie Walk.fbx";
    private const string ATTACK_FBX_PATH = "Assets/Zombijefe/Sword And Shield Slash.fbx";
    private const string OUTPUT_PATH     = "Assets/Zombijefe/ZombieJefeAnimator.controller";

    [MenuItem("Tools/Crear Animator Zombi Jefe")]
    public static void Create()
    {
        // ── 1. Cargar clips de animación ──────────────────────────────
        AnimationClip walkClip   = LoadFirstClip(WALK_FBX_PATH);
        AnimationClip attackClip = LoadFirstClip(ATTACK_FBX_PATH);

        if (walkClip == null)
        {
            Debug.LogError($"[ZombieJefeAnimatorCreator] No se encontró clip en {WALK_FBX_PATH}");
            return;
        }
        if (attackClip == null)
        {
            Debug.LogError($"[ZombieJefeAnimatorCreator] No se encontró clip en {ATTACK_FBX_PATH}");
            return;
        }

        Debug.Log($"[ZombieJefeAnimatorCreator] Walk clip: {walkClip.name}");
        Debug.Log($"[ZombieJefeAnimatorCreator] Attack clip: {attackClip.name}");

        // ── 2. Crear el controller ────────────────────────────────────
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(OUTPUT_PATH);

        // ── 3. Parámetros (mismos que ZombieAnimationController) ──────
        ac.AddParameter("Speed",      AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("IsDead",     AnimatorControllerParameterType.Bool);
        ac.AddParameter("IsHit",      AnimatorControllerParameterType.Bool);
        ac.AddParameter("IsCrawling", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine root = ac.layers[0].stateMachine;

        // ── 4. Estados ────────────────────────────────────────────────

        // Idle / Walk — BlendTree (0=idle, 1=walk)
        BlendTree locomotionTree;
        AnimatorState locomotionState = ac.CreateBlendTreeInController(
            "Locomotion", out locomotionTree);
        locomotionTree.blendType      = BlendTreeType.Simple1D;
        locomotionTree.blendParameter = "Speed";
        locomotionTree.AddChild(walkClip, 0f);   // Speed 0 = idle (primer frame del walk)
        locomotionTree.AddChild(walkClip, 1f);   // Speed 1 = walk a velocidad normal
        locomotionState.motion = locomotionTree;

        // Attack
        AnimatorState attackState = root.AddState("Attack");
        attackState.motion = attackClip;
        attackState.speed  = 1f;

        // Die (sin clip por defecto — asignar después en Inspector si quieres)
        AnimatorState dieState = root.AddState("Die");

        // Hit (sin clip — igual que zombi normal)
        AnimatorState hitState = root.AddState("Hit");

        // ── 5. Estado por defecto ─────────────────────────────────────
        root.defaultState = locomotionState;

        // ── 6. Transiciones LOCOMOTION → ATTACK ───────────────────────
        AnimatorStateTransition toAttack = locomotionState.AddTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAttack.duration        = 0.1f;
        toAttack.hasExitTime     = false;

        // ATTACK → LOCOMOTION (al terminar la animación)
        AnimatorStateTransition attackToLoco = attackState.AddTransition(locomotionState);
        attackToLoco.hasExitTime = true;
        attackToLoco.exitTime    = 0.9f;
        attackToLoco.duration    = 0.1f;

        // ── 7. Transiciones DIE ───────────────────────────────────────
        AnimatorStateTransition loco_toDie = locomotionState.AddTransition(dieState);
        loco_toDie.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        loco_toDie.hasExitTime = false;
        loco_toDie.duration    = 0.15f;

        AnimatorStateTransition attack_toDie = attackState.AddTransition(dieState);
        attack_toDie.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        attack_toDie.hasExitTime = false;
        attack_toDie.duration    = 0.15f;

        // ── 8. Transición HIT ─────────────────────────────────────────
        AnimatorStateTransition loco_toHit = locomotionState.AddTransition(hitState);
        loco_toHit.AddCondition(AnimatorConditionMode.If, 0, "IsHit");
        loco_toHit.hasExitTime = false;
        loco_toHit.duration    = 0.05f;

        AnimatorStateTransition hit_toIdle = hitState.AddTransition(locomotionState);
        hit_toIdle.hasExitTime = true;
        hit_toIdle.exitTime    = 0.8f;
        hit_toIdle.duration    = 0.1f;

        // ── 9. Guardar ────────────────────────────────────────────────
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ZombieJefeAnimatorCreator] ✅ AnimatorController creado en: {OUTPUT_PATH}");
        EditorGUIUtility.PingObject(ac);
        Selection.activeObject = ac;
    }

    static AnimationClip LoadFirstClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }
}
