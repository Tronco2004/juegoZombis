using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inventario simple del jugador para llaves y otros objetos.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }
    
    [Header("Llaves")]
    [Tooltip("Lista de llaves que tiene el jugador")]
    public List<string> keys = new List<string>();
    
    [Header("Debug")]
    [Tooltip("Mostrar llaves en consola")]
    public bool debugMode = false;
    
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
        }
    }
    
    /// <summary>
    /// Verifica si el jugador tiene una llave específica
    /// </summary>
    public bool HasKey(string keyName)
    {
        return keys.Contains(keyName);
    }
    
    /// <summary>
    /// Añade una llave al inventario
    /// </summary>
    public void AddKey(string keyName)
    {
        if (!keys.Contains(keyName))
        {
            keys.Add(keyName);
            
            if (debugMode)
            {
                Debug.Log($"Llave añadida: {keyName}");
            }
        }
    }
    
    /// <summary>
    /// Elimina una llave del inventario
    /// </summary>
    public void RemoveKey(string keyName)
    {
        if (keys.Contains(keyName))
        {
            keys.Remove(keyName);
            
            if (debugMode)
            {
                Debug.Log($"Llave eliminada: {keyName}");
            }
        }
    }
    
    /// <summary>
    /// Elimina todas las llaves
    /// </summary>
    public void ClearKeys()
    {
        keys.Clear();
    }
}
