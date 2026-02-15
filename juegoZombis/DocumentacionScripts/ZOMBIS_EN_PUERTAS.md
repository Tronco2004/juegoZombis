# 🚪 Guía: Zombis Atravesando Puertas

## Problema
Los zombis se quedan atrapados en las puertas cuando el jugador cruza porque:
- ❌ El NavMesh no está bakeado en la zona de la puerta
- ❌ El NavMeshObstacle no se desactiva correctamente
- ❌ Los zombis no recalculan la ruta después de que se abre la puerta

---

## ✅ Solución Implementada

### Cambios en `DoubleDoor.cs` y `LockedDoubleDoor.cs`:

1. **Delay al desactivar obstáculos** - Se da 0.1s al NavMesh para recalcular
2. **Recarga de rutas automática** - Cuando se abre una puerta, todos los zombis en 20m de radio recalculan su ruta
3. **Logging mejorado** - Debug messages muestran qué está pasando

**Métodos nuevos:**
```csharp
IEnumerator SetNavMeshObstaclesActiveCoroutine(bool active)
    → Desactiva obstáculos con delay para actualizar NavMesh
    
void NotifyNearbyZombies()
    → Busca zombis cercanos y fuerza recálculo de ruta
```

---

## 🔧 CHECKLIST - Configuración en Unity Editor

### Paso 1: Verificar NavMesh alrededor de puertas

1. **Abre el Navigation Panel:**
   - `Window > AI > Navigation`

2. **Selecciona todas las puertas de la mansión:**
   - En el inspector, marca **TODOS** los colliders de puertas como `Walkable = TRUE`
   - Marca también los **pasillos** y **umbrales** como `Walkable = TRUE`

3. **Hacer Bake del NavMesh:**
   - Click en `Bake`
   - ⚠️ **IMPORTANTE:** El NavMesh debe formar un camino continuo a través de TODA la puerta
   - Verifica en la vista 3D que veas malla azul cubriendo:
     - ✅ Interior de salas
     - ✅ **Pasillos entre puertas** ← CRÍTICO
     - ✅ Zona de transición de puertas

### Paso 2: Configurar NavMeshObstacles en las Puertas

Para cada puerta interactiva (DoubleDoor o LockedDoubleDoor):

1. **En el prefab de la puerta IZQUIERDA:**
   - Agrega componente `NavMeshObstacle`
   - Shape: `Box`
   - Size: `[1.5, 2.5, 0.3]` (ajusta según tamaño real de tu puerta)
   - Center: `[0, 0, 0]`
   - ✅ Marca **Carving = ON**
   - Carving Move Threshold: `0.1`
   - Carving Time to Stationary: `0.2`

2. **Repite lo mismo para la puerta DERECHA**

3. **En el script (DoubleDoor o LockedDoubleDoor):**
   - ✅ Marca `useNavMeshObstacle = TRUE`
   - `obstacleSize = [1.5, 2.5, 0.3]` (debe coincidir)
   - `obstacleCenter = [0, 0, 0]`

### Paso 3: Verificar que el script está actualizado

Abre `Assets/Scripts/DoubleDoor.cs` y verifica que tiene:

```csharp
IEnumerator SetNavMeshObstaclesActiveCoroutine(bool active)
void NotifyNearbyZombies()
```

Si no los ves = descargate la versión actualizada ✅

---

## 🧪 Pruebas

### Test 1: NavMesh correcto
```
1. Abre escena en mansión
2. En Scene view: selecciona Window > AI > Navigation (o presiona N)
3. Verifica que ves malla AZUL en todo el camino de la puerta
   - Si NO la ves en la puerta → NavMesh mal bakeado
   - Solución: Marca todo como Walkable y rebakea
```

### Test 2: Zombis cruzan puertas
```
1. Spawnea un zombi en una sala
2. Abre la puerta con E
3. El zombi debería empezar a perseguirte
4. Cruza la puerta
5. ✅ El zombi debería cruzar tras de ti (NO quedarse atrapado)
```

### Test 3: Debug Visual
```
1. Abre Console (Ctrl + Shift + C)
2. Cruza una puerta mientras zombis te persiguen
3. Deberías ver en la consola:
   - [DoubleDoor] NavMeshObstacles DESACTIVADOS (paso libre)
   - [DoubleDoor] Notificados N zombis para recalcular rutas
```

---

## ⚠️ Solución de Problemas

### "Los zombis siguen atrapados en puertas"

**Causa 1: NavMesh no baqueado correctamente**
```
✓ Abre Navigation Window
✓ Verifica que TODO está marcado Walkable
✓ Especialmente el PASILLO de la puerta
✓ Rebakea
✓ Reinicia el juego
```

**Causa 2: NavMeshObstacle mal configurado**
```
✓ Selecciona puerta izquierda → Inspector
✓ Verifica que tiene NavMeshObstacle
✓ Verifica Size y Center coinciden con DoubleDoor
✓ Verify que Carving = ON
✓ Repite para puerta derecha
```

**Causa 3: Script no actualizado**
```
✓ Abre DoubleDoor.cs
✓ Busca "NotifyNearbyZombies"
✓ Si no existe → actualiza el script
```

**Causa 4: Tamaño de obstáculo incorrecto**
```
El tamaño del NavMeshObstacle DEBE coincidir con el tamaño real de tu puerta
- Demasiado pequeño: zombis todavía pasan por los lados
- Demasiado grande: bloquea el pasillo para siempre

✓ Prueba con valores diferentes:
  - Puertas estrechas: [1.0, 2.5, 0.2]
  - Puertas medias: [1.5, 2.5, 0.3]
  - Puertas grandes: [2.0, 2.8, 0.4]
```

---

## 📝 Cómo funciona la solución

```
1. Jugador abre puerta (pulsa E)
2. DoubleDoor.ToggleDoors() → StartCoroutine(AnimateDoors(true))
3. AnimateDoors() → StartCoroutine(SetNavMeshObstaclesActiveCoroutine(false))
4. SetNavMeshObstaclesActiveCoroutine():
   ✓ Desactiva NavMeshObstacle izq y der
   ✓ Espera 0.1 segundos (NavMesh recalcula)
   ✓ Llama a NotifyNearbyZombies()
5. NotifyNearbyZombies():
   ✓ Busca todos los ZombieAI en 20 metros
   ✓ Para cada uno: agent.ResetPath() → agent.CalculatePath()
   ✓ Los zombis recalculan rutas automáticamente
6. Zombis pueden cruzar la puerta abierta ✅
7. Jugador cierra puerta → NavMeshObstacle se reactiva
```

---

## 🚀 Configuración Avanzada

### Si los zombis SIGUEN LENTO en puertas:

Aumenta el radio de notificación en los scripts:

**DoubleDoor.cs línea 493:**
```csharp
float notifyRadius = 30f;  // Cambiar de 20f a 30f
```

**LockedDoubleDoor.cs línea -:**
```csharp
float notifyRadius = 30f;  // Misma línea aproximadamente
```

### Si quieres que sea MÁS AGRESIVO:

Reduce el delay de espera:

```csharp
yield return new WaitForSeconds(0.05f);  // Cambiar de 0.1f a 0.05f
```

---

## ✨ Resultado Final

**Antes:** 🧟🚪 (zombis atrapados)
**Después:** 🧟→🚪→🧟 (zombis cruzan puertas sin problemas)

¡Disfruta de zombis que sí pueden perseguirte por toda la mansión!
