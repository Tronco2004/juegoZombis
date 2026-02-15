# 🚤 Scripts Vehicles — `Assets/Scripts/Vehicles/`

> 2 scripts que implementan un barco conducible.

---

## BoatController.cs (555 líneas)

**Propósito:** Controlador de barco — el jugador puede subirse y conducirlo.

| Elemento | Detalle |
|----------|---------|
| **Movimiento** | `maxSpeed = 15f`, `acceleration = 5f`, `deceleration = 3f`, `turnSpeed = 30f` |
| **Opciones** | `turnOnlyWhenMoving = true`, `useCameraDirection = true` |
| **Posiciones** | `driverSeat` (auto-crea), `exitPoint` (auto-crea) |

### Configuración de Agua:

| Variable | Detalle |
|----------|---------|
| `simulateFloating = true` | Flotación sinusoidal |
| `floatAmplitude = 0.15f` | Amplitud del movimiento |
| `floatSpeed = 1.5f` | Velocidad de flotación |
| `onlyMoveOnWater = true` | Restricción de movimiento |
| `waterCheckDistance = 5f` | Raycast para detectar agua |

### Detección de agua (`CheckForWater()`):
- Lanza rayos hacia abajo desde el centro y la proa
- Detecta agua por: tag "Water", `isTrigger`, o nombre contiene "water"
- Si no golpea nada sólido → asume que hay agua

### Sistema de cámara:
- `useBoatCamera = true` → Activa `boatCamera` al subirse
- Desactiva cámara y AudioListener del jugador
- Al bajar → restaura todo

### Métodos principales:

| Método | Descripción |
|--------|-------------|
| `EnterBoat(Transform)` | Jugador sube — desactiva CharacterController, FPC, cámara del jugador |
| `ExitBoat()` | Jugador baja — busca suelo sólido, restaura todo |
| `HandleDriving()` | Input WASD, aceleración/frenado, giro, flotación |
| `FindBestExitPosition()` | Raycast en 8 direcciones para encontrar suelo (no agua) |
| `IsBeingDriven()` → bool | Estado actual |
| `GetCurrentSpeed()` → float | Velocidad actual |

### Flujo de entrada al barco:
```
EnterBoat(player) →
  1. Desactiva CharacterController del jugador
  2. Desactiva FirstPersonController  
  3. Desactiva cámara del jugador
  4. Activa boatCamera
  5. Configura GameHUD.SetHeadingOverride(boatCamera.transform)
  6. Sonido de arranque
  7. Loop del motor de audio
```

### Flujo de salida del barco:
```
ExitBoat() →
  1. FindBestExitPosition() — busca suelo en 8 direcciones
  2. Teletransporta jugador al punto de salida
  3. Reactiva CharacterController, FirstPersonController
  4. Desactiva boatCamera, reactiva cámara del jugador
  5. Reactiva AudioListener del jugador
  6. Para motor de audio
  7. ClearHeadingOverrideNextFrame() — espera 2 frames para limpiar
```

**Interacciones:**
- → `FirstPersonController` (desactiva/reactiva)
- → `CharacterController` (desactiva/reactiva)
- → `GameHUD.SetHeadingOverride()` (brújula)
- ← `BoatInteraction`

---

## BoatInteraction.cs (130 líneas)

**Propósito:** Maneja la interacción del jugador con el barco (subir/bajar).

| Elemento | Detalle |
|----------|---------|
| **Variables** | `interactionRange = 3f`, `interactKey = E`, `exitKey = F` |
| **Mensajes** | `enterMessage = "Pulsa E - Subir al barco"`, `exitMessage = "Pulsa F - Bajar del barco"` |
| **Búsqueda jugador** | Tag "Player" o `FindObjectOfType<PlayerMoney>()` como fallback |

### Lógica de Update:
1. Si está conduciendo → escucha F o E para bajar
2. Si no conduce → mide distancia al barco → muestra prompt si en rango
3. Si en rango + E → `boatController.EnterBoat(player)`

### OnGUI:
- Cuando conduce: muestra velocidad + mensaje de salida (color cyan)
- Cuando en rango: muestra mensaje de entrada (color amarillo)
- Texto con sombra negra

**Interacciones:**
- → `BoatController.EnterBoat()` / `ExitBoat()`
- → `BoatController.IsBeingDriven()` / `GetCurrentSpeed()`
