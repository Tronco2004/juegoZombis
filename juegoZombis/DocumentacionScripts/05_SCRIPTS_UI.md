# 🖥️ Scripts UI — `Assets/Scripts/UI/`

> 10 scripts que gestionan el HUD, pausa, barras de salud, diálogos, popups de daño y puntos.

---

## DamagePopup.cs (66 líneas)

**Propósito:** Números de daño flotantes sobre los enemigos. Headshots se muestran más grandes y dorados.

| Elemento | Detalle |
|----------|---------|
| **Método estático** | `Create(Vector3 position, float damage, bool isHeadshot)` |
| **Rendering** | `TextMesh` (3D world space) |
| **Animación** | Flota hacia arriba + fade out en 1.2 segundos |
| **Headshot** | Texto más grande, color dorado, añade "!" |

**Interacciones:** ← `EnemyHealth.TakeDamage()`

---

## DialogueManager.cs (175 líneas)

**Propósito:** Sistema singleton de diálogos con fade in/out.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `DialogueManager.Instance` + `EnsureExists()` (auto-crea si no existe) |
| **Rendering** | `OnGUI` — caja semi-transparente en la parte inferior de la pantalla |
| **Métodos** | `ShowDialogue(text, duration)`, `HideDialogue()`, `IsShowingDialogue()` |
| **Efectos** | Fade in/out gradual del texto |

**Interacciones:**
- ← `ElectricDoorDialogue`
- ← `CuadroElectrico`
- ← `HouseExitDialogue`

---

## EnemyHealthBar.cs (94 líneas)

**Propósito:** Barra de vida en world-space sobre los enemigos usando Canvas.

| Elemento | Detalle |
|----------|---------|
| **Comportamiento** | Billboard — siempre mira a la cámara |
| **Colores** | Verde → Amarillo → Rojo según % de vida |
| **Visibilidad** | Se oculta cuando la vida está al 100%, aparece `visibleTime = 3f` tras recibir daño |
| **Método** | `OnDamaged()` — muestra la barra temporalmente |

**Interacciones:** ← `EnemyHealth`

---

## GameHUD.cs (1028 líneas)

**Propósito:** HUD completo del juego — creado 100% programáticamente sin prefabs.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `GameHUD.Instance` |
| **Canvas** | Crea su propio Canvas en `Awake()` |

### Componentes del HUD:

| Componente | Descripción |
|------------|-------------|
| **Barra de Salud** | Rojo, con borde, color cambia según %, pulso cuando < 20% |
| **Barra de Stamina** | Azul, naranja cuando baja del 30% |
| **Munición** | Cargador actual / reserva + nombre del arma. Rojo cuando ≤ 5 balas |
| **Puntos** | Con animación suave de conteo + flash verde/rojo al cambiar |
| **Oleada** | "OLEADA X" — lee via Reflection `currentWave` field del ZombieSpawner |
| **Crosshair** | 4 líneas con gap y outline, se expande al disparar |
| **Brújula** | Tira horizontal con N/S/E/W, marcadores dinámicos, heading override para vehículos |
| **Indicador de daño** | `ShowDamageIndicator(source)` — stub (no implementado completamente) |

### Sistema de Brújula:

- **Tira continua** de 360° × 2 (para loop) con marcas cada 5°
- **Cardinales** (N en rojo, E/S/W en blanco) + grados cada 15°
- **RectMask2D** para clipear (compatible con TMPro)
- **Heading:** Prioridad: 1) `headingOverride` (barco), 2) `playerController.transform`, 3) `Camera.main`

### Compass Markers API:

| Método | Descripción |
|--------|-------------|
| `AddCompassMarker(id, target, color, label)` | Añade marcador dinámico |
| `RemoveCompassMarker(id)` | Quita por ID |
| `ClearCompassMarkers()` | Quita todos |
| `SetHeadingOverride(Transform)` | Override para vehículos (null = normal) |

### Otros métodos:

| Método | Descripción |
|--------|-------------|
| `GetCrosshairScreenPosition()` | Posición pixel del crosshair (para raycast) |
| `ExpandCrosshair(amount, duration)` | Expande al disparar |

**Interacciones:**
- ← `PlayerHealth` (barra salud)
- ← `PlayerPoints` (puntos, via evento)
- ← `FirstPersonController` (stamina)
- ← `ZombieSpawner` (oleada, via Reflection)
- ← `FPSWeaponController` / `WeaponSwitcher` (munición)
- ← `ElectricDoorDialogue`, `CuadroElectrico` (compass markers)
- ← `BoatController` (heading override)

---

## HUDInitializer.cs (163 líneas)

**Propósito:** Crea el GameHUD automáticamente y limpia UI legacy.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `autoCreateHUD = true`, `removeOldHealthUI`, `hideSpawnerWaveText`, `disableDebugTexts` |

### Limpieza que realiza:
1. Elimina `PlayerHealthUI` (legacy)
2. Oculta texto de oleada del `ZombieSpawner` (legacy TMPro)
3. Desactiva textos debug de `InteractablePurchasable`
4. Desactiva `AudioDebugger`

**Interacciones:** → `GameHUD`, limpia `PlayerHealthUI`, `AudioDebugger`

---

## PauseManager.cs (343 líneas)

**Propósito:** Sistema de pausa con menú: sensibilidad, volumen, botones de resume/menú/salir.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `PauseManager.Instance` |
| **Variable estática** | `static bool IsPaused` — verificada por muchos scripts |
| **Tecla** | ESC para pausar/reanudar |

### Configuración:
- **Slider sensibilidad** → `FirstPersonController.SetMouseSensitivity()`
- **Slider volumen** → `AudioListener.volume`
- **Persistencia** → `PlayerPrefs.SetFloat("MouseSensitivity")`, `PlayerPrefs.SetFloat("Volume")`

### Métodos:

| Método | Descripción |
|--------|-------------|
| `PauseGame()` | `Time.timeScale = 0`, muestra cursor, pausa audio |
| `ResumeGame()` | `Time.timeScale = 1`, oculta cursor, resume audio |
| `PauseAllAudio()` | Pausa todos los AudioSource |
| `ResumeAllAudio()` | Reanuda todos los AudioSource |

**Interacciones:**
- → `FirstPersonController.mouseSensitivity`
- → `AudioListener.volume`
- ← Verificado por `FirstPersonController`, `FPSWeaponController`

---

## PauseMenuCreator.cs (344 líneas)

**Propósito:** Crea todo el menú de pausa programáticamente (sin prefabs).

| Elemento | Detalle |
|----------|---------|
| **Crea** | Canvas, Panel oscuro, título "PAUSA", sliders, botones |
| **Estilo** | Temática rojo oscuro (colores horror) |
| **Helpers** | `CreateSlider()`, `CreateButton()` con hover effects |

**Interacciones:** → Crea componente `PauseManager` en runtime

---

## PlayerHealthUI.cs (328 líneas)

**Propósito:** Barra de salud legacy del jugador (reemplazada por GameHUD).

| Elemento | Detalle |
|----------|---------|
| **Enum** | `BarPosition`: TopLeft, TopRight, BottomLeft, BottomRight, TopCenter, BottomCenter |
| **Creación** | Crea Canvas + barra con borde + icono corazón procedural |
| **Colores** | Verde (>60%) → Amarillo (30-60%) → Rojo (<30%) con parpadeo <20% |
| **Flash** | Destello rojo al recibir daño |

**Estado:** ⚠️ **LEGACY** — `HUDInitializer` lo elimina automáticamente a favor de `GameHUD`

**Interacciones:** → `PlayerHealth`

---

## PointsUI.cs (112 líneas)

**Propósito:** Muestra puntos del jugador con animación de escala al cambiar.

| Elemento | Detalle |
|----------|---------|
| **Suscripción** | `PlayerPoints.OnPointsChanged` (evento) |
| **Animación** | Escala grow/shrink al recibir/gastar puntos |
| **Color** | Verde al ganar, rojo al perder |

**Interacciones:** ← `PlayerPoints.OnPointsChanged`

---

## WeaponUI.cs (133 líneas)

**Propósito:** UI alternativa para munición, stamina y puntos (puede coexistir con GameHUD).

| Elemento | Detalle |
|----------|---------|
| **Referencias** | `ammoText`, `crosshair`, `pointsText`, `staminaBar`, `staminaBarImage`, `staminaText` |
| **Auto-búsqueda** | `FindObjectOfType<FPSWeaponController>()`, `FindObjectOfType<FirstPersonController>()` |
| **Colores stamina** | Lerp entre `staminaFullColor` (verde) y `staminaLowColor` (rojo) |

**Interacciones:**
- ← `FPSWeaponController.GetAmmoText()`
- ← `WeaponSwitcher.CurrentWeapon`
- ← `FirstPersonController.StaminaPercentage`
- ← `PlayerPoints.OnPointsChanged`
