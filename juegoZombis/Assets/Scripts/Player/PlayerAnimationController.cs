using UnityEngine;

/// <summary>
/// Controla las animaciones del jugador basándose en el movimiento y armas
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Configuración")]
    public float crossfadeTime = 0.1f;

    [Header("Teclas")]
    public KeyCode aimKey = KeyCode.Mouse1;    // Click derecho para apuntar
    public KeyCode reloadKey = KeyCode.R;      // R para recargar
    public KeyCode sprintKey = KeyCode.LeftShift; // Shift para correr

    [Header("Arma Actual")]
    public WeaponType currentWeapon = WeaponType.Pistol;

    public enum WeaponType { None, Pistol, Rifle }

    // Componentes
    private Animator animator;

    // Estado actual
    private string currentState = "";
    private bool isAiming = false;
    private bool isReloading = false;
    private bool isSprinting = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        PlayState("pistol idle");
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
        
        // Sprint con Shift
        isSprinting = Input.GetKey(sprintKey);

        // Apuntar con click derecho (mantener)
        isAiming = Input.GetKey(aimKey);

        // Recargar con R (solo dispara una vez)
        if (Input.GetKeyDown(reloadKey) && !isReloading)
        {
            StartReload();
            return;
        }

        // Si está recargando, no cambiar animación
        if (isReloading) return;

        bool isMoving = W || S || A || D;
        string targetState = GetIdleState();

        if (isMoving)
        {
            if (isSprinting)
            {
                // Correr
                if (W && !S)
                    targetState = GetRunState("forward");
                else if (S && !W)
                    targetState = GetRunState("backward");
                else if (A && !D)
                    targetState = GetStrafeState("left");
                else if (D && !A)
                    targetState = GetStrafeState("right");
            }
            else
            {
                // Caminar
                if (W && !S)
                    targetState = GetWalkState("forward");
                else if (S && !W)
                    targetState = GetWalkState("backward");
                else if (A && !D)
                    targetState = GetStrafeState("left");
                else if (D && !A)
                    targetState = GetStrafeState("right");
            }
        }

        // Cambiar si es diferente
        if (targetState != currentState)
        {
            PlayState(targetState);
        }
    }

    string GetIdleState()
    {
        switch (currentWeapon)
        {
            case WeaponType.Pistol: return "pistol idle";
            case WeaponType.Rifle: return "idle";
            default: return "idle";
        }
    }

    string GetRunState(string direction)
    {
        switch (currentWeapon)
        {
            case WeaponType.Pistol:
                if (direction == "forward") return "pistol run";
                if (direction == "backward") return "pistol run backward";
                return "pistol run";
            case WeaponType.Rifle:
                return "run " + direction;
            default:
                return "run " + direction;
        }
    }

    string GetWalkState(string direction)
    {
        switch (currentWeapon)
        {
            case WeaponType.Pistol:
                if (direction == "forward") return "pistol walk";
                if (direction == "backward") return "pistol walk backward";
                return "pistol walk";
            case WeaponType.Rifle:
                return "run " + direction; // Rifle no tiene walk, usa run
            default:
                return "run " + direction;
        }
    }

    string GetStrafeState(string direction)
    {
        switch (currentWeapon)
        {
            case WeaponType.Pistol:
                if (direction == "left") return "pistol strafe left";
                if (direction == "right") return "pistol strafe right";
                return "pistol strafe";
            case WeaponType.Rifle:
                return "run " + direction;
            default:
                return "run " + direction;
        }
    }

    void StartReload()
    {
        isReloading = true;
        // Por ahora no hay animación de reload de pistola, usar idle
        // PlayState("pistol reload");
        Invoke("EndReload", 2f);
    }

    void EndReload()
    {
        isReloading = false;
    }

    void PlayState(string stateName)
    {
        currentState = stateName;
        animator.Play(stateName, 0, 0f);
    }

    // Métodos públicos
    public bool IsAiming() => isAiming;
    public bool IsReloading() => isReloading;
    public bool IsSprinting() => isSprinting;
    
    public void SetWeapon(WeaponType weapon)
    {
        currentWeapon = weapon;
    }
}
