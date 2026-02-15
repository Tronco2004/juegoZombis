# � Scripts Vehicles — `Assets/Scripts/Vehicles/`

> 4 scripts que implementan vehículos conducibles: barco y helicóptero.

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

---

## HelicopterController.cs (~500 líneas)

**Propósito:** Controlador de helicóptero — el jugador puede subirse y pilotarlo con controles de vuelo realistas.

| Elemento | Detalle |
|----------|--------|
| **Motor** | `liftForce = 25f`, `maxSpeed = 30f`, `horizontalAcceleration = 12f`, `drag = 2f`, `turboMultiplier = 1.6f` |
| **Rotación** | `pitchSpeed = 35f`, `rollSpeed = 35f`, `yawSpeed = 50f`, `maxTiltAngle = 30°` |
| **Altitud** | `verticalSpeed = 10f`, `maxAltitude = 200f`, `minGroundClearance = 2f` |
| **Posiciones** | `pilotSeat` (auto-crea), `exitPoint` (auto-crea) |

### Controles de vuelo:

| Tecla | Acción |
|-------|--------|
| W/S | Inclinar adelante/atrás (pitch) → genera movimiento horizontal |
| A/D | Inclinar izquierda/derecha (roll) → movimiento lateral |
| Espacio | Ascender |
| Left Ctrl | Descender |
| Q/E | Giro horizontal (yaw) |
| Left Shift | Turbo (×1.6 velocidad) |
| Ratón | Cámara orbital en tercera persona |

### Sistema de rotores:
- `mainRotor` y `tailRotor` con velocidades independientes
- Arranque progresivo (`engineSpoolTime = 3s`) → el rotor necesita alcanzar 50% para volar
- Frenado progresivo al apagar motor
- Ejes de rotación configurables (`mainRotorAxis`, `tailRotorAxis`)

### Física de vuelo:
- Inclinación del helicóptero genera fuerza horizontal (simulación realista)
- Hover automático cuando no hay input vertical
- Gravedad cuando el motor está apagado
- Estabilización automática (vuelve a nivelarse)
- Colisión con daño proporcional a la velocidad de impacto

### Sistema de cámara:
- Cámara orbital en tercera persona
- `cameraDistance = 12f`, `cameraHeight = 5f`
- Control con ratón, suavizado con `cameraSmoothSpeed`
- Siempre mira al helicóptero

### Métodos principales:

| Método | Descripción |
|--------|------------|
| `EnterHelicopter(Transform)` | Jugador sube — desactiva CC, FPC, cámara; enciende motor |
| `ExitHelicopter()` | Jugador baja — SOLO si está cerca del suelo (≤5m) |
| `HandleFlightControls()` | Input completo de vuelo, física, inclinación |
| `HandleEngine()` | Arranque/apagado progresivo del motor |
| `SpinRotors()` | Rotación visual de las hélices |
| `FindBestExitPosition()` | Raycast 8 direcciones para suelo seguro |
| `IsBeingPiloted()` → bool | Estado actual |
| `GetCurrentSpeed()` → float | Velocidad horizontal |
| `GetAltitude()` → float | Distancia al suelo |
| `GetVerticalSpeed()` → float | Velocidad vertical |
| `GetRotorPower()` → float | Potencia del rotor (0-1) |

### Flujo de entrada al helicóptero:
```
EnterHelicopter(player) →
  1. Desactiva CharacterController del jugador
  2. Desactiva FirstPersonController
  3. Desactiva cámara del jugador
  4. Activa helicopterCamera
  5. Configura GameHUD.SetHeadingOverride()
  6. Enciende motor (arranque progresivo)
  7. Sonido de arranque
```

### Flujo de salida del helicóptero:
```
ExitHelicopter() →
  1. Verifica altitud (solo si ≤ 5m del suelo)
  2. FindBestExitPosition() — busca suelo en 8 direcciones
  3. Teletransporta jugador al punto seguro
  4. Reactiva CharacterController, FirstPersonController
  5. Restaura cámaras y AudioListener
  6. Apaga motor (frenado progresivo del rotor)
  7. ClearHeadingOverrideNextFrame()
```

### Daño por impacto:
- `crashSpeedThreshold = 8f` → velocidad mínima para daño
- `crashDamageMultiplier = 5f` → daño proporcional
- Llama a `PlayerHealth.TakeDamage()` al impactar
- Frena al 30% de velocidad tras colisión

**Interacciones:**
- → `FirstPersonController` (desactiva/reactiva)
- → `CharacterController` (desactiva/reactiva)
- → `GameHUD.SetHeadingOverride()` (brújula)
- → `PlayerHealth.TakeDamage()` (daño por impacto)
- ← `HelicopterInteraction`

---

## HelicopterInteraction.cs (~140 líneas)

**Propósito:** Maneja la interacción del jugador con el helicóptero (subir/bajar).

| Elemento | Detalle |
|----------|--------|
| **Variables** | `interactionRange = 4f`, `interactKey = E`, `exitKey = F` |
| **Mensajes** | `enterMessage`, `exitMessage`, `tooHighMessage` |
| **Búsqueda jugador** | Tag "Player" o `FindObjectOfType<PlayerMoney>()` como fallback |

### Lógica de Update:
1. Si está pilotando → escucha F o E para bajar
2. Si no pilota → mide distancia al helicóptero → muestra prompt si en rango
3. Si en rango + E → `heliController.EnterHelicopter(player)`

### OnGUI (HUD de vuelo):
- **Pilotando:** muestra velocidad, altitud, velocidad vertical, potencia del rotor
  - Color cyan si puede bajar, naranja si está demasiado alto
- **En rango:** muestra mensaje de entrada (color amarillo)
- Texto con sombra negra para legibilidad

**Interacciones:**
- → `HelicopterController.EnterHelicopter()` / `ExitHelicopter()`
- → `HelicopterController.IsBeingPiloted()` / `GetCurrentSpeed()` / `GetAltitude()` / `GetVerticalSpeed()` / `GetRotorPower()`
