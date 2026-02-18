# Scripts Vehicles — `Assets/Scripts/Vehicles/`

> 7 scripts que implementan vehículos conducibles: barco, helicóptero y tanque.

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

---

## TankController.cs (~520 líneas)

**Propósito:** Controlador de tanque — movimiento arcade con torreta independiente controlada por ratón y disparo de misiles.

| Elemento | Detalle |
|----------|---------|
| **Chasis** | `maxSpeed = 12f`, `acceleration = 8f`, `deceleration = 12f`, `turnSpeed = 40f` |
| **Turbo** | `turboMultiplier = 1.5f`, `reverseSpeedFraction = 0.4f` |
| **Torreta** | `turretRotationSpeed = 120f`, `turretSmoothing = 8f`, `autoFindTurret = true` |
| **Disparo** | `fireRate = 1.5f`, `maxAimDistance = 500f` |
| **Posiciones** | `driverSeat` (auto-crea), `exitPoint` (auto-crea), `firePoint` (auto-crea) |

### Controles:

| Tecla | Acción |
|-------|--------|
| W/S | Avanzar / Retroceder (chasis) |
| A/D | Girar chasis izquierda / derecha (solo cuando se mueve) |
| Ratón | Rotar torreta (cabezal) de forma INDEPENDIENTE |
| Click izquierdo | Disparar misil hacia el punto del raycast |
| Left Shift | Turbo (×1.5 velocidad) |

### Sistema de torreta:
- **Clave:** La torreta se mueve INDEPENDIENTEMENTE del chasis
- El ratón controla la rotación de la torreta (yaw) mediante raycast desde la cámara
- El chasis se mueve con WASD sin afectar la dirección de la torreta
- Suavizado con `Mathf.LerpAngle` para rotación fluida
- Auto-búsqueda por nombres: "Turret", "turret", "Cabezal", "Tower", etc.

### Sistema de disparo:
- Raycast desde la cámara al mundo para determinar el punto de mira (`aimPoint`)
- El misil se instancia en `firePoint` orientado hacia `aimPoint`
- `fireRate = 1.5f` → cadencia entre disparos
- Si no se asigna `firePoint`, se crea automáticamente en la punta del cañón
- Auto-busca el cañón por nombres: "Cannon", "Barrel", "Gun", "Canon", etc.
- Efectos opcionales: `muzzleFlashEffect` (fogonazo), `fireSound` (sonido)

### Sistema de cámara:
- `cameraFollowsTurret = true` → la cámara sigue la dirección de la torreta
- Si `false`, sigue el chasis
- `cameraDistance = 8f`, `cameraHeight = 4f`
- Suavizado con `cameraSmoothSpeed = 5f`

### Física:
- Rigidbody con `mass = 2000f` (tanque pesado) y `useGravity = true`
- Velocidad horizontal aplicada en `FixedUpdate`
- Giro del chasis invertido automáticamente en marcha atrás
- Daño por impacto a alta velocidad (`crashSpeedThreshold = 10f`)

### Métodos principales:

| Método | Descripción |
|--------|------------|
| `EnterTank(Transform)` | Jugador sube — desactiva CC, FPC, cámara; libera cursor |
| `ExitTank()` | Jugador baja — restaura todo, busca suelo seguro |
| `Drive()` | Input WASD, aceleración/frenado, rotación chasis |
| `RotateTurret()` | Raycast desde cámara, rotación suavizada de la torreta |
| `HandleShooting()` | Click izquierdo → `FireMissile()` con cadencia |
| `FireMissile()` | Instancia misil en `firePoint` hacia `aimPoint` |
| `IsBeingDriven()` → bool | Estado actual |
| `GetCurrentSpeed()` → float | Velocidad absoluta |
| `GetTurretAngle()` → float | Ángulo Y de la torreta |
| `GetAimPoint()` → Vector3 | Punto de mira actual |

### Flujo de entrada al tanque:
```
EnterTank(player) →
  1. Desactiva CharacterController del jugador
  2. Desactiva FirstPersonController
  3. Desactiva cámara del jugador
  4. Activa tankCamera (snap instantáneo)
  5. Configura GameHUD.SetHeadingOverride()
  6. Marca jugador como invulnerable (isInVehicle)
  7. Libera cursor para apuntar torreta
  8. Sonido de arranque
```

### Flujo de salida del tanque:
```
ExitTank() →
  1. FindExitPosition() — busca suelo en 8 direcciones
  2. Teletransporta jugador al punto seguro
  3. Reactiva CharacterController, FirstPersonController
  4. Restaura cámaras y AudioListener
  5. Para motor de audio
  6. ClearHeadingOverride()
```

### Gizmos:
- **Verde:** Asiento del conductor
- **Amarillo:** Punto de salida
- **Rojo:** Punto de disparo + dirección
- **Azul:** Dirección del chasis
- **Cyan:** Dirección de la torreta
- **Magenta:** Punto de mira (solo en play)

**Interacciones:**
- → `FirstPersonController` (desactiva/reactiva)
- → `CharacterController` (desactiva/reactiva)
- → `GameHUD.SetHeadingOverride()` (brújula)
- → `PlayerHealth.TakeDamage()` (daño por impacto)
- → `PlayerHealth.isInVehicle` (invulnerabilidad)
- → `MissileController` (instancia misiles)
- ← `TankInteraction`

---

## MissileController.cs (~170 líneas)

**Propósito:** Controlador de misil — se mueve hacia el objetivo, explota al impactar con daño en área.

| Elemento | Detalle |
|----------|---------|
| **Movimiento** | `speed = 60f`, `lifetime = 5f` |
| **Daño** | `directDamage = 100f`, `explosionDamage = 50f`, `explosionRadius = 5f` |
| **Fuerza** | `explosionForce = 300f` |

### Comportamiento:
1. Se instancia orientado hacia el punto de mira
2. Vuela en línea recta a velocidad constante (`FixedUpdate`)
3. Al impactar → `Explode()`
4. Se autodestruye después de `lifetime` segundos si no impacta

### Sistema de explosión:
- `Physics.OverlapSphere()` detecta objetos en el radio
- Daño proporcional a la distancia (más cerca = más daño)
- Impacto directo (dist < 1m) → `directDamage + explosionDamage`
- `AddExplosionForce()` a rigidbodies cercanos
- Instancia efecto visual de explosión
- Sonido de explosión con `AudioSource.PlayClipAtPoint()`

### Efectos opcionales:
- `explosionEffectPrefab` → partículas de explosión
- `explosionSound` → sonido de impacto
- `trailEffect` → estela del misil (se separa al explotar para desvanecerse)

### Métodos principales:

| Método | Descripción |
|--------|------------|
| `SetTarget(Vector3)` | Configura punto objetivo (llamado por TankController) |
| `Explode(Vector3)` | Daño en área + efectos + destruye misil |

**Interacciones:**
- → `EnemyHealth.TakeDamage()` (daño a enemigos)
- ← `TankController.FireMissile()` (lo instancia)

---

## TankInteraction.cs (~115 líneas)

**Propósito:** Maneja la interacción del jugador con el tanque (subir/bajar).

| Elemento | Detalle |
|----------|---------|
| **Variables** | `interactionRange = 4f`, `interactKey = E`, `exitKey = F` |
| **Mensajes** | `enterMessage = "Pulsa E - Subir al tanque"`, `exitMessage = "Pulsa F - Bajar del tanque"` |
| **Búsqueda jugador** | Tag "Player" o `FindObjectOfType<PlayerMoney>()` como fallback |

### Lógica de Update:
1. Si está conduciendo → escucha F o E para bajar
2. Si no conduce → mide distancia al tanque → muestra prompt si en rango
3. Si en rango + E → `tankController.EnterTank(player)`

### OnGUI:
- **Conduciendo:** muestra mensaje de salida
- **En rango:** muestra mensaje de entrada
- Texto con sombra negra para legibilidad

**Interacciones:**
- → `TankController.EnterTank()` / `ExitTank()`
- → `TankController.IsBeingDriven()`
