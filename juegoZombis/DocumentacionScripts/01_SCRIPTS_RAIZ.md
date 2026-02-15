# 📂 Scripts Raíz — `Assets/Scripts/`

> 22 scripts con mecánicas generales: puertas, compras, puzzles, inventario, dinero, salud del jugador.

---

## AddCollidersToBuilding.cs (82 líneas)

**Propósito:** Herramienta de editor para añadir Box/Mesh Colliders a todos los hijos de un edificio.

| Elemento | Detalle |
|----------|---------|
| **Tipo** | Utilidad de Editor (ContextMenu) |
| **Variables clave** | `useMeshColliders` (bool), `makeConvex` (bool) |
| **Métodos** | `AddColliders()`, `RemoveColliders()` |
| **Interacciones** | Ninguna — herramienta standalone |

**Lógica:** Recorre todos los `MeshRenderer` hijos y les añade un `BoxCollider` o `MeshCollider` según la configuración. `RemoveColliders()` los elimina.

---

## BulletController.cs (33 líneas)

**Propósito:** Controlador básico de balas (versión legacy, reemplazada por `Bullet.cs`).

| Elemento | Detalle |
|----------|---------|
| **Variables** | `speed = 200f`, `lifetime = 3f` |
| **Movimiento** | Rigidbody velocity en `Start()` |
| **Daño** | `OnCollisionEnter` → `EnemyHealth.TakeDamage(speed)` |

**⚠️ Bug:** Usa `speed` (200) como valor de daño en lugar de una variable `damage` separada.

**Interacciones:** → `EnemyHealth.TakeDamage()`

---

## CuadroElectrico.cs (384 líneas)

**Propósito:** Panel eléctrico interactivo con proceso de 3 pasos: abrir tapa → activar palanca → cerrar.

| Elemento | Detalle |
|----------|---------|
| **Variables clave** | `price` (int), `openAngle`, `rotationAxis`, `animationSpeed`, `knobAngle`, `puertasDobles[]` (DoubleDoor[]), `activationDialogue` (string) |
| **Estados** | `EstadoCuadro` enum: Cerrado, AbiertoSinPalanca, Activado, Cerrándose |
| **Interacción** | Tecla E + distancia, 3 pasos secuenciales |

**Lógica:**
1. **Paso 1:** Abrir tapa (rotación animada via `RotateAround`)
2. **Paso 2:** Activar palanca (rotación del knob)
3. **Paso 3:** Cerrar tapa

Al activar la palanca → abre todas las `DoubleDoor` conectadas en `puertasDobles[]`.

**Interacciones:**
- → `DoubleDoor.ForceOpen()` / `ForceClose()`
- → `DialogueManager.ShowDialogue()`
- → `GameHUD.AddCompassMarker()` / `RemoveCompassMarker()`
- → `PlayerMoney.SpendMoney()`

---

## DoubleDoor.cs (492 líneas)

**Propósito:** Sistema de puerta doble con visagras, precio opcional, bloqueo/desbloqueo.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `leftDoor`, `rightDoor`, `openAngle = 90`, `price`, `isLocked`, `visagraIzquierda`, `visagraDerecha` |
| **Métodos públicos** | `ForceOpen()`, `ForceClose()`, `LockDoors()`, `UnlockDoors()` |
| **Animación** | `RotateAround` con visagras o `localRotation` simple |
| **OnGUI** | Muestra prompt "Pulsa E — Abrir ($X)" |

**Interacciones:**
- ← `CuadroElectrico`, `TrapHouseTrigger`, `SimonSaysManager`
- → `PlayerMoney.SpendMoney()`, `GameHUD.AddCompassMarker()`

---

## ElectricDoorDialogue.cs (120 líneas)

**Propósito:** Muestra diálogo cuando el jugador examina una puerta que necesita electricidad.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `dialogueText`, `cuadroElectricoTarget` (Transform), `dialogueDuration = 3f` |
| **Métodos** | `DisableDialogue()` — desactiva permanentemente |
| **Lógica** | Al pulsar E cerca → muestra diálogo + añade marcador de brújula al cuadro eléctrico |

**Interacciones:**
- → `DialogueManager.ShowDialogue()`
- → `GameHUD.AddCompassMarker()`
- → Auto-busca `CuadroElectrico` en escena

---

## HangarDoorSwap.cs (88 líneas)

**Propósito:** Intercambia un GameObject de hangar cerrado por uno abierto (para puertas no animables).

| Elemento | Detalle |
|----------|---------|
| **Variables** | `hangarCerrado`, `hangarAbierto`, `isOpen` |
| **Métodos** | `Swap()`, `Close()` |

**Interacciones:** ← `SimonSaysManager.Victoria()`

---

## HouseExitDialogue.cs (104 líneas)

**Propósito:** Diálogo activado por trigger cuando el jugador sale de una casa.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `dialogueText`, `triggerOnce = true`, `delay = 0.5f` |
| **Lógica** | `OnTriggerEnter` → espera `delay` → muestra diálogo |

**Interacciones:** → `DialogueManager.ShowDialogue()`

---

## InteractableBoxAnimated.cs (350 líneas)

**Propósito:** Cajas comprables de munición/salud con animación de tapa.

| Elemento | Detalle |
|----------|---------|
| **Enum** | `BoxType` (Ammo / Health) |
| **Variables** | `price = 100`, `ammoAmount = 30`, `healthAmount = 50`, `boxLid` (Transform), `RotationAxis` enum |
| **Animaciones** | Abrir/cerrar tapa, rebote (sin tapa), sacudida (sin dinero) |

**Lógica:**
- **Health box:** `PlayerHealth.Heal(healthAmount)`
- **Ammo box:** Solo `Debug.Log` — **⚠️ NO añade munición realmente**

**Interacciones:**
- → `PlayerMoney.SpendMoney()`
- → `PlayerHealth.Heal()` (solo health boxes)

---

## InteractablePurchasable.cs (243 líneas)

**Propósito:** Objeto comprable genérico con 3 tipos de animación.

| Elemento | Detalle |
|----------|---------|
| **Enum** | `AnimationType` (Door / MoveUp / Disappear) |
| **Variables** | `price`, `objectName`, `interactionDistance = 10f`, `showDebugInfo` |

**Interacciones:** → `PlayerMoney`

---

## KeyItem.cs (132 líneas)

**Propósito:** Llave recogible con efecto visual de flotación y rotación.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `keyName = "LlaveCasa"`, `pickupRange = 2f` |
| **Efectos** | Rotación continua + flotación sinusoidal |
| **Lógica** | Distancia < `pickupRange` + tecla E → recoge llave |

**Interacciones:** → `PlayerInventory.AddKey(keyName)`

---

## LockedDoubleDoor.cs (280 líneas)

**Propósito:** Puerta doble que requiere llave específica para abrirse.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `requiredKeyName = "LlaveCasa"`, `consumeKey = false`, `isUnlocked` |
| **Lógica** | Verifica `PlayerInventory.HasKey()` → desbloquea → abre automáticamente |

**Interacciones:**
- → `PlayerInventory.HasKey()` / `RemoveKey()`
- Misma animación de puerta que `DoubleDoor`

---

## PlayerHealth.cs (566 líneas)

**Propósito:** Sistema de salud del jugador estilo CoD — regeneración automática, viñeta de sangre, heartbeat.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `PlayerHealth.Instance` |
| **Variables** | `maxHealth = 100`, `regenDelay = 4f`, `regenRate = 15`, `lowHealthThreshold = 0.3f` |
| **Sonidos** | `heartbeatSound`, `breathingSound`, `hurtSounds[]`, `deathSounds[]` |
| **Overlay** | Viñeta de sangre procedural creada con `CreateBloodVignetteSprite()` |

**Métodos clave:**
- `TakeDamage(damage, damageSource)` — reduce salud, muestra overlay, inicia retardo de regeneración
- `Heal(amount)` — cura al jugador
- `Die()` — desactiva `FirstPersonController`, reproduce sonido de muerte

**Interacciones:**
- → `GameHUD.ShowDamageIndicator()`
- → `FirstPersonController` (desactiva al morir)
- ← `ZombieAI.TakeDamage()`, `InteractableBoxAnimated.Heal()`

---

## PlayerInventory.cs (81 líneas)

**Propósito:** Inventario simple de llaves usando `List<string>`.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `PlayerInventory.Instance` |
| **Métodos** | `HasKey(name)`, `AddKey(name)`, `RemoveKey(name)`, `ClearKeys()` |

**Interacciones:** ← `KeyItem`, ← `LockedDoubleDoor`

---

## PlayerMoney.cs (60 líneas)

**Propósito:** Sistema de dinero del jugador (versión legacy — `PlayerPoints` es el más nuevo).

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `PlayerMoney.Instance` |
| **Variables** | `currentMoney = 500`, UI Text opcional |
| **Métodos** | `AddMoney(amount)`, `SpendMoney(amount)` → bool, `HasEnoughMoney(amount)` → bool |

**⚠️ Nota:** Coexiste con `PlayerPoints` — ver [09_PROBLEMAS_Y_MEJORAS.md](09_PROBLEMAS_Y_MEJORAS.md).

---

## PurchasableBarrier.cs (328 líneas)

**Propósito:** Barrera comprable que cae y se destruye. Usa el punto más cercano del collider para medir distancia.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `price = 1000`, `fallBeforeDestroy = true` |
| **Animaciones** | Caída con gravedad simulada, sacudida cuando no hay dinero |

**Interacciones:** → `PlayerMoney.SpendMoney()`

---

## PurchasableTest.cs (112 líneas)

**Propósito:** Script de prueba/debug para verificar que el sistema de compra por raycast funciona.

| Elemento | Detalle |
|----------|---------|
| **Detección** | `Camera.main` raycast (en lugar de distancia) |

**Interacciones:** → `PlayerMoney` (solo para testing)

---

## RaycastDebug.cs (36 líneas)

**Propósito:** Herramienta debug que muestra en consola qué objeto golpea el raycast central de la cámara.

---

## SimonSaysManager.cs (445 líneas)

**Propósito:** Minijuego puzzle Simon Says — genera secuencia aleatoria de 4 colores, el jugador debe reproducirla mirando pantallas y pulsando E.

| Elemento | Detalle |
|----------|---------|
| **Enum** | `EstadoSimon` (Esperando / Mostrando / Turno / Completado / Fallido) |
| **Variables** | `pantallas[]` (SimonSaysScreen[4]), `secuenciaAleatoria`, `raycastDistance = 10f`, `recompensaPuntos = 500` |
| **Detección** | `RaycastAll` para detectar qué pantalla mira el jugador |

**Al ganar (Victoria):**
- Da puntos al jugador
- Abre `DoubleDoor[]` conectadas con `ForceOpen()`
- Intercambia `HangarDoorSwap` con `Swap()`

**Interacciones:**
- → `SimonSaysScreen.Encender()` / `Apagar()`
- → `DoubleDoor.ForceOpen()`
- → `HangarDoorSwap.Swap()`
- → `PlayerMoney.AddMoney()`

---

## SimonSaysScreen.cs (132 líneas)

**Propósito:** Pantalla individual del puzzle Simon Says con estados encendido/apagado.

| Elemento | Detalle |
|----------|---------|
| **Enum** | `SimonColor` (Rojo / Verde / Azul / Amarillo) |
| **Variables** | `colorPanel` (GameObject) |
| **Métodos** | `Encender()`, `Apagar()`, `FlashFeedback()` |
| **Efecto** | Soporte de emisión en material |

**Interacciones:** ← `SimonSaysManager`

---

## SimpleTest.cs (23 líneas)

**Propósito:** Script de diagnóstico básico — verifica que Awake/Start/Update y la tecla E funcionan.

---

## TimsDoorSimple.cs (267 líneas)

**Propósito:** Puerta simple con precio, rotación via `RotateAround` con visagra.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `doorPrice = 1000f`, `interactionDistance = 500f` ⚠️ (muy grande) |
| **Detección** | Basada en trigger + distancia |
| **Lógica** | Busca hijo `Door_Wood` automáticamente |

**Interacciones:** → `PlayerMoney`

---

## TrapHouseTrigger.cs (156 líneas)

**Propósito:** Trampa que cierra y bloquea puertas cuando el jugador entra en un área.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `mainDoors[]` (DoubleDoor[]), `delayBeforeClose = 1.5f`, `triggerOnce = true` |
| **Lógica** | `OnTriggerEnter` → espera delay → cierra y bloquea puertas → muestra mensaje de advertencia |

**Interacciones:**
- → `DoubleDoor.ForceClose()`
- → `DoubleDoor.LockDoors()`

---

## VallaPurchasable.cs (97 líneas)

**Propósito:** Valla/barrera comprable con detección por raycast de cámara. Se mueve hacia arriba al comprarla.

| Elemento | Detalle |
|----------|---------|
| **Detección** | `Camera.main` raycast + `IsPartOfMe()` check |
| **Animación** | Coroutine `MoveUp` — desplaza Y hacia arriba |

**Interacciones:** → `PlayerMoney`

---

## VallaPurchasableTag.cs (119 líneas)

**Propósito:** Igual que `VallaPurchasable` pero usa detección por distancia al jugador (tag "Player") en vez de raycast.

| Elemento | Detalle |
|----------|---------|
| **Detección** | `FindGameObjectWithTag("Player")` + distancia < 5f |

**Interacciones:** → `PlayerMoney`
