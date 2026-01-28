using UnityEngine;

/// <summary>
/// Controla las animaciones del jugador basándose en el movimiento
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Configuración")]
    public float crossfadeTime = 0.1f;

    // Componentes
    private Animator animator;

    // Estado actual
    private string currentState = "";

    void Start()
    {
        animator = GetComponent<Animator>();
        // Empezar en idle
        PlayState("idle");
    }

    void Update()
    {
        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        // Leer input directo
        bool W = Input.GetKey(KeyCode.W);
        bool S = Input.GetKey(KeyCode.S);
        bool A = Input.GetKey(KeyCode.A);
        bool D = Input.GetKey(KeyCode.D);

        // Determinar estado a reproducir
        string targetState = "idle";

        if (W && !S)
            targetState = "run forward";
        else if (S && !W)
            targetState = "run backward";
        else if (A && !D)
            targetState = "run left";
        else if (D && !A)
            targetState = "run right";

        // Cambiar si es diferente
        if (targetState != currentState)
        {
            PlayState(targetState);
        }
    }

    void PlayState(string stateName)
    {
        currentState = stateName;
        animator.Play(stateName, 0, 0f);
    }
}
