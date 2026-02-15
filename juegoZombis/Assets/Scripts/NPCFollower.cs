using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sistema de NPC que sigue al jugador (escolta).
/// Necesita un NavMeshAgent en el GameObject.
/// </summary>
public class NPCFollower : MonoBehaviour
{
    [Header("--- SEGUIMIENTO ---")]
    [Tooltip("Distancia a la que se mantiene del jugador")]
    public float followDistance = 2.5f;
    [Tooltip("Distancia mínima antes de moverse")]
    public float stopDistance = 1.5f;
    [Tooltip("Velocidad al seguir")]
    public float followSpeed = 3f;
    [Tooltip("Velocidad de rotación hacia el jugador")]
    public float rotationSpeed = 5f;

    [Header("--- ESTADO ---")]
    public bool isFollowing = false;

    private NavMeshAgent agent;
    private Transform player;
    private Animation anim;
    private Animator animator;

    void Start()
    {
        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Configurar NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = followSpeed;
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = true;
        agent.isStopped = true;

        // Buscar animaciones
        anim = GetComponent<Animation>();
        if (anim == null)
            anim = GetComponentInChildren<Animation>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!isFollowing || player == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > followDistance)
        {
            // Seguir al jugador
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else if (distance <= stopDistance)
        {
            // Cerca del jugador, parar
            agent.isStopped = true;

            // Mirar al jugador
            Vector3 lookDir = (player.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Empieza a seguir al jugador
    /// </summary>
    public void StartFollowing()
    {
        isFollowing = true;
        if (agent != null)
        {
            agent.isStopped = false;
        }
        Debug.Log(gameObject.name + " empieza a seguir al jugador");
    }

    /// <summary>
    /// Deja de seguir al jugador
    /// </summary>
    public void StopFollowing()
    {
        isFollowing = false;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        Debug.Log(gameObject.name + " deja de seguir al jugador");
    }

    /// <summary>
    /// ¿Está siguiendo al jugador?
    /// </summary>
    public bool IsFollowing()
    {
        return isFollowing;
    }
}
