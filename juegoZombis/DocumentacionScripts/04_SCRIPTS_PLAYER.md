# 🎮 Scripts Player — `Assets/Scripts/Player/`

> 6 scripts que controlan el movimiento FPS, animaciones del jugador, puntos y cámara.

---

## AnimatorSetup.cs (69 líneas)

**Propósito:** Herramienta de editor para crear un Animator Controller con estados predefinidos.

| Elemento | Detalle |
|----------|---------|
| **Condición** | `#if UNITY_EDITOR` — solo funciona en el editor |
| **Output** | Crea `"Assets/PlayerAnimatorAuto.controller"` |
| **Estados** | idle, run, aim, crouch, death |

**Interacciones:** Standalone — herramienta de editor

---

## FirstPersonController.cs (289 líneas)

**Propósito:** Controlador FPS completo — WASD, ratón, salto, agacharse, sprint con stamina.

| Elemento | Detalle |
|----------|---------|
| **Movimiento** | `walkSpeed = 4f`, `runSpeed = 8f`, CharacterController |
| **Salto** | `jumpForce = 8f`, gravedad manual |
| **Agacharse** | `crouchHeight = 1f`, con `CanStandUp()` check (SphereCast) |
| **Stamina** | `maxStamina = 100f`, `staminaDrainRate = 15f`, `staminaRegenRate = 10f` |
| **Ratón** | `mouseSensitivity = 2f`, clamp vertical -90° a 90° |

### Propiedades públicas (usadas por otros scripts):

| Propiedad | Tipo | Uso |
|-----------|------|-----|
| `IsGrounded` | bool | ← GameHUD, armas |
| `IsRunning` | bool | ← FPSWeaponController (animación correr) |
| `IsCrouching` | bool | ← UI |
| `IsMoving` | bool | ← FPSWeaponController |
| `StaminaPercentage` | float | ← GameHUD, WeaponUI |
| `HasStamina` | bool | ← interno |
| `currentStamina` | float | ← WeaponUI |

### Métodos:

| Método | Descripción |
|--------|-------------|
| `HandleMovement()` | WASD + sprint, aplica CharacterController.Move() |
| `HandleCrouch()` | Toggle C, verifica CanStandUp() |
| `HandleMouseLook()` | Rotación X (cuerpo) + Y (cámara) |
| `HandleStamina()` | Drena al correr, regenera parado |
| `SetMouseSensitivity(float)` | Llamado desde PauseManager |

**Interacciones:**
- ← `PauseManager.IsPaused` — pausa todo el input
- ← `PlayerHealth.Die()` — desactiva este script

---

## PlayerAnimationController.cs (193 líneas)

**Propósito:** Controla animaciones del personaje basadas en movimiento y tipo de arma.

| Elemento | Detalle |
|----------|---------|
| **Enum** | `WeaponType` (None / Pistol / Rifle) |
| **Input** | Lee WASD + Shift + Mouse1 + R |
| **Sistema** | Usa `Animator.Play()` con nombres como "pistol idle", "pistol run", "rifle walk" |

**Lógica:** Combina estado de movimiento (idle/walk/run) con tipo de arma y acción (disparar/recargar) para seleccionar la animación correcta del Animator.

---

## PlayerPoints.cs (86 líneas)

**Propósito:** Sistema de puntos del jugador (versión más nueva que PlayerMoney) con eventos.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `PlayerPoints.Instance` |
| **Variables** | `startingPoints = 500`, `pointsPerKill = 400` |
| **Evento** | `OnPointsChanged` (Action<int>) — suscripciones: PointsUI, WeaponUI, GameHUD |

### Métodos:

| Método | Descripción |
|--------|-------------|
| `AddPoints(amount)` | Suma puntos, dispara evento |
| `SpendPoints(amount)` → bool | Gasta si hay suficientes |
| `HasEnoughPoints(amount)` → bool | Verifica fondos |
| `CurrentPoints` → int | Propiedad getter |

**⚠️ Problema:** Coexiste con `PlayerMoney` — algunos scripts usan uno, otros el otro. Ver [09_PROBLEMAS_Y_MEJORAS.md](09_PROBLEMAS_Y_MEJORAS.md).

**Interacciones:**
- ← `EnemyHealth.Die()` — da puntos por kill
- → `PointsUI`, `WeaponUI`, `GameHUD` (via evento)

---

## SpineLookAt.cs (55 líneas)

**Propósito:** Hace que el torso del personaje siga la rotación vertical de la cámara.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `spinebone` (Transform), `spineInfluence = 0.5f`, `maxAngle = 30f` |
| **Ejecución** | `LateUpdate()` — después de que el Animator actualice los huesos |
| **Lógica** | Aplica rotación adicional al hueso de la espina dorsal basada en el pitch de la cámara |

---

## WeaponFollowCamera.cs (55 líneas)

**Propósito:** Hace que el modelo del arma siga el look vertical de la cámara (arriba/abajo).

| Elemento | Detalle |
|----------|---------|
| **Variables** | `weaponFollowAmount = 0.8f`, `smoothSpeed = 8f` |
| **Lógica** | Ajusta la posición Y local del arma basándose en el pitch de la cámara |
