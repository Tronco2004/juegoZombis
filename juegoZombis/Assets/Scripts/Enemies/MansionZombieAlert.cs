using UnityEngine;

/// <summary>
/// Gestor de alertas para zombis de la mansion.
/// Coordina 3 estados de alerta:
/// - Dormido: zombis parados, no atacan
/// - AlertaBaja: zombis activan cuando jugador esta muy cerca (5m)
/// - AlertaCritica: TODOS atacan cuando se dispara dentro de la mansion
///
/// Setup en Unity:
/// 1. Crea un GameObject "MansionZombieManager" en la mansion
/// 2. Asigna este script
/// 3. Asigna el tag "MansionZombie" a TODOS los zombis de la mansion
/// 4. (Opcional) Arrastra la camara del jugador al inspector para calcular disparos
/// </summary>
public class MansionZombieAlert : MonoBehaviour
{
    [Header("=== GENERAL ===")]
    [Tooltip("Tag de los zombis que pertenecen a esta mansion")]
    public string zombieTag = "MansionZombie";
    
    [Tooltip("Collider del area de la mansion (para detectar disparos dentro)")]
    public Collider mansionArea;
    
    [Header("=== DEBUG ===")]
    [Tooltip("Mostrar informacion en consola")]
    public bool debugMode = true;

    private ZombieAI[] mansionZombies;
    private ZombieAI.AiState currentGlobalState = ZombieAI.AiState.Dormido;

    void Start()
    {
        // Buscar todos los zombis con el tag especificado
        GameObject[] zombieObjects = GameObject.FindGameObjectsWithTag(zombieTag);
        mansionZombies = new ZombieAI[zombieObjects.Length];

        for (int i = 0; i < zombieObjects.Length; i++)
        {
            ZombieAI ai = zombieObjects[i].GetComponent<ZombieAI>();
            if (ai != null)
            {
                mansionZombies[i] = ai;
                
                // Asegurar que estan marcados como zombis de mansion
                if (!ai.isMansionZombie)
                {
                    ai.isMansionZombie = true;
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"[MansionZombieAlert] {mansionZombies.Length} zombis de mansion encontrados");
        }
    }

    void Update()
    {
        // Detectar disparos mediante raycast o colisiones
        // (esto se integrara con el sistema de balas)
    }

    /// <summary>
    /// Cambiar el estado de alerta GLOBAL para todos los zombis de la mansion
    /// Llamar desde MansionZombieAlert o desde el sistema de balas
    /// </summary>
    public void SetGlobalAlertLevel(ZombieAI.AiState newState)
    {
        if (currentGlobalState == newState) return;
        
        currentGlobalState = newState;

        if (debugMode)
        {
            Debug.Log($"[MansionZombieAlert] ¡ALERTA GLOBAL: {newState}!");
        }

        // Aplicar el estado a todos los zombis
        foreach (ZombieAI zombie in mansionZombies)
        {
            if (zombie != null)
            {
                zombie.SetMansionState(newState);
            }
        }
    }

    /// <summary>
    /// Activar ALERTA CRITICA - Todos los zombis atacan!
    /// Llamar cuando se dispara dentro de la mansion
    /// </summary>
    public void TriggerCriticalAlert()
    {
        if (debugMode)
        {
            Debug.Log("[MansionZombieAlert] ¡¡¡ALERTA CRITICA!!! Cambiar a AlertaCritica");
        }
        SetGlobalAlertLevel(ZombieAI.AiState.AlertaCritica);
    }

    /// <summary>
    /// Resetear a estado dormido
    /// </summary>
    public void ResetToSleep()
    {
        if (debugMode)
        {
            Debug.Log("[MansionZombieAlert] Reseteando a estado Dormido");
        }
        SetGlobalAlertLevel(ZombieAI.AiState.Dormido);
    }

    /// <summary>
    /// Obtener estado global actual
    /// </summary>
    public ZombieAI.AiState GetCurrentAlertLevel()
    {
        return currentGlobalState;
    }

    /// <summary>
    /// Singleton estatico para acceder desde otras clases
    /// </summary>
    private static MansionZombieAlert instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static MansionZombieAlert Instance => instance;
}
