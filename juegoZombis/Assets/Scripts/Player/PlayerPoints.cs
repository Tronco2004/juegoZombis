using UnityEngine;
using System;

/// <summary>
/// Sistema de puntos del jugador
/// Singleton para acceder desde cualquier script
/// </summary>
public class PlayerPoints : MonoBehaviour
{
    public static PlayerPoints Instance { get; private set; }
    
    [Header("Configuración")]
    public int startingPoints = 500; // Puntos iniciales
    public int pointsPerKill = 400; // Puntos por matar un zombie
    
    // Puntos actuales
    private int currentPoints;
    
    // Evento cuando cambian los puntos
    public event Action<int> OnPointsChanged;
    
    public int CurrentPoints => currentPoints;
    
    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        currentPoints = startingPoints;
        OnPointsChanged?.Invoke(currentPoints);
    }
    
    /// <summary>
    /// Añadir puntos (al matar zombies, etc)
    /// </summary>
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        Debug.Log($"[Puntos] +{amount} = {currentPoints} puntos totales");
        OnPointsChanged?.Invoke(currentPoints);
    }
    
    /// <summary>
    /// Gastar puntos (comprar armas, etc)
    /// </summary>
    public bool SpendPoints(int amount)
    {
        if (currentPoints >= amount)
        {
            currentPoints -= amount;
            Debug.Log($"[Puntos] -{amount} = {currentPoints} puntos restantes");
            OnPointsChanged?.Invoke(currentPoints);
            return true;
        }
        
        Debug.Log($"[Puntos] No tienes suficientes puntos. Necesitas {amount}, tienes {currentPoints}");
        return false;
    }
    
    /// <summary>
    /// Verificar si tiene suficientes puntos
    /// </summary>
    public bool HasEnoughPoints(int amount)
    {
        return currentPoints >= amount;
    }
    
    /// <summary>
    /// Llamar cuando se mata un zombie
    /// </summary>
    public void OnZombieKilled()
    {
        AddPoints(pointsPerKill);
    }
}
