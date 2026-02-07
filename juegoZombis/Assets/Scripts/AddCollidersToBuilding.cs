using UnityEngine;

/// <summary>
/// Añade este script a un edificio/casa y luego haz clic en el botón
/// del Inspector para añadir colliders a todas las partes.
/// </summary>
public class AddCollidersToBuilding : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Usar Box Colliders (más eficiente) o Mesh Colliders (más preciso)")]
    public bool useMeshColliders = false;
    
    [Tooltip("Hacer los Mesh Colliders convexos (necesario si el jugador tiene Rigidbody)")]
    public bool makeConvex = true;
    
    /// <summary>
    /// Llama a esta función para añadir colliders a todos los hijos
    /// </summary>
    [ContextMenu("Añadir Colliders a Hijos")]
    public void AddColliders()
    {
        int count = 0;
        
        // Obtener todos los MeshRenderer en hijos
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer renderer in renderers)
        {
            GameObject obj = renderer.gameObject;
            
            // Verificar si ya tiene collider
            if (obj.GetComponent<Collider>() != null)
            {
                continue; // Ya tiene collider, saltar
            }
            
            if (useMeshColliders)
            {
                // Añadir Mesh Collider
                MeshCollider meshCol = obj.AddComponent<MeshCollider>();
                
                // Obtener el mesh del MeshFilter
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshCol.sharedMesh = meshFilter.sharedMesh;
                }
                
                meshCol.convex = makeConvex;
            }
            else
            {
                // Añadir Box Collider (más eficiente)
                obj.AddComponent<BoxCollider>();
            }
            
            count++;
        }
        
        Debug.Log($"Se añadieron {count} colliders al edificio '{gameObject.name}'");
    }
    
    /// <summary>
    /// Elimina todos los colliders de los hijos
    /// </summary>
    [ContextMenu("Eliminar Colliders de Hijos")]
    public void RemoveColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        int count = 0;
        
        foreach (Collider col in colliders)
        {
            DestroyImmediate(col);
            count++;
        }
        
        Debug.Log($"Se eliminaron {count} colliders del edificio '{gameObject.name}'");
    }
}
