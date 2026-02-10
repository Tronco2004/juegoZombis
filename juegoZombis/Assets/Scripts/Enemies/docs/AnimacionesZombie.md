# Sistema de Animaciones Zombie - Mixamo

## Resumen

Integración completa del pack de animaciones Mixamo para los zombies del juego. Se ha creado un sistema modular con un **Animator Controller generado por código**, un **controlador de animaciones dedicado** y la **actualización de la IA** para usar todas las animaciones disponibles.

---

## Archivos del Pack (AnimacionesZombis/)

| Archivo | Uso en el Juego |
|---|---|
| `zombie idle.fbx` | Estado base cuando no se mueve |
| `zombie walk.fbx` | Caminando hacia el jugador |
| `zombie run.fbx` | Corriendo para perseguir |
| `zombie attack.fbx` | Ataque melee básico |
| `zombie biting.fbx` | Mordisco (variación ataque) |
| `zombie biting (2).fbx` | Mordisco alternativo |
| `zombie neck bite.fbx` | Mordisco al cuello (variación) |
| `zombie death.fbx` | Animación de muerte |
| `zombie dying.fbx` | Muerte alternativa |
| `zombie crawl.fbx` | Arrastrarse (vida baja) |
| `running crawl.fbx` | Arrastrarse rápido |
| `zombie scream.fbx` | Grito al detectar jugador |
| `ShirtlessZombie_BodyParts_FREE.fbx` | Modelo/mesh del zombie |

---

## Archivos Creados / Modificados

### Nuevos

| Archivo | Descripción |
|---|---|
| `Assets/Editor/ZombieAnimatorBuilder.cs` | Tool de editor que genera el Animator Controller automáticamente |
| `Assets/Scripts/Enemies/ZombieAnimationController.cs` | Componente que gestiona todos los estados de animación |

### Modificados

| Archivo | Cambios |
|---|---|
| `Assets/Scripts/Enemies/ZombieAI.cs` | Integración con `ZombieAnimationController`, grito al detectar, crawl por vida baja, ataques variados |
| `Assets/Scripts/Enemies/EnemyHealth.cs` | Trigger de muerte animada, verificación de crawl al recibir daño, delay de destrucción para animación |

---

## Máquina de Estados del Animator

```
                    ┌─────────┐
                    │  Scream │◄── (primera detección)
                    └────┬────┘
                         │ (exit time)
                         ▼
┌──────┐  IsWalking  ┌──────┐  IsRunning  ┌─────┐
│ Idle │────────────►│ Walk │────────────►│ Run │
│      │◄────────────│      │◄────────────│     │
└──┬───┘             └──┬───┘             └──┬──┘
   │                    │                    │
   │ IsCrawling         │ IsCrawling         │ IsCrawling
   ▼                    ▼                    ▼
┌──────┐  IsRunning  ┌──────────┐
│Crawl │────────────►│ RunCrawl │
│      │◄────────────│          │
└──────┘             └──────────┘

  Attack/Bite/NeckBite triggers (desde Idle/Walk/Run):
  ┌─────────┐  ┌──────┐  ┌──────────┐
  │ Attack  │  │ Bite │  │ NeckBite │
  └────┬────┘  └──┬───┘  └────┬─────┘
       │          │            │
       └──────────┴────────────┘
                  │ (exit time → Idle)

  Die trigger (desde cualquier estado):
  ┌───────┐
  │ Death │ (estado final)
  └───────┘
```

---

## Parámetros del Animator

| Parámetro | Tipo | Descripción |
|---|---|---|
| `Speed` | Float | Velocidad actual del NavMeshAgent |
| `IsWalking` | Bool | Zombie está caminando (speed > 0.1) |
| `IsRunning` | Bool | Zombie está corriendo (speed > 2.5) |
| `IsCrawling` | Bool | Zombie se arrastra (vida < 25%) |
| `IsDead` | Bool | Zombie está muerto |
| `Attack` | Trigger | Ataque básico |
| `Bite` | Trigger | Mordisco |
| `NeckBite` | Trigger | Mordisco al cuello |
| `Scream` | Trigger | Grito (primera detección) |
| `Die` | Trigger | Morir |
| `AttackIndex` | Int | Índice de ataque aleatorio (0-2) |

---

## Cómo Usar

### 1. Generar el Animator Controller

En Unity, ve al menú:

```
Tools > Zombie > Crear Animator Controller
```

Esto genera `Assets/AnimacionesZombis/ZombieAnimatorController.controller` con todos los estados y transiciones configurados automáticamente.

### 2. Configurar el Prefab del Zombie

Añade estos componentes al prefab del zombie:

1. **Animator** → Asigna el `ZombieAnimatorController` generado
2. **ZombieAnimationController** → Se configura solo, pero puedes ajustar:
   - `crawlHealthPercent`: porcentaje de vida para arrastrarse (default: 25%)
   - `randomizeAttacks`: ataques variados o solo básico
   - `enableCrawlSystem`: activar/desactivar sistema de crawl
3. **ZombieAI** → Ya existente, ahora busca `ZombieAnimationController` automáticamente
4. **EnemyHealth** → Ya existente, ahora integra animaciones

### 3. Configurar el Avatar del Modelo

Si usas el modelo `ShirtlessZombie_BodyParts_FREE.fbx`:

1. Selecciona el FBX en el Project
2. En el Inspector, pestaña **Rig**:
   - Animation Type: **Humanoid**
   - Avatar Definition: **Create From This Model**
3. Click **Apply**
4. En el Animator del prefab, asigna este Avatar

### 4. Importar las Animaciones Correctamente

Para cada FBX de animación:

1. Selecciona el FBX en el Project
2. Pestaña **Rig**:
   - Animation Type: **Humanoid**
   - Avatar Definition: **Copy From Other Avatar** → selecciona el avatar del modelo
3. Pestaña **Animation**:
   - Loop Time: **activar** para idle, walk, run, crawl
   - Loop Time: **desactivar** para attack, bite, death, scream
4. Click **Apply**

---

## Características del Sistema

### Ataques Variados
El sistema elige aleatoriamente entre 3 tipos de ataque:
- **Attack**: golpe básico
- **Bite**: mordisco
- **NeckBite**: mordisco al cuello

### Crawl (Arrastrarse)
Cuando la vida baja al 25%, el zombie:
- Cambia a animación de arrastre
- Reduce su velocidad de movimiento
- Reduce su rango de ataque
- Usa `running crawl` al perseguir

### Grito de Detección
La primera vez que el zombie detecta al jugador dentro de su rango de persecución, reproduce la animación de grito.

### Muerte Animada
Al morir, el zombie reproduce la animación de muerte antes de ser destruido (3 segundos de delay).

---

## Thresholds Configurables

| Propiedad | Componente | Default | Descripción |
|---|---|---|---|
| `walkThreshold` | ZombieAnimationController | 0.1 | Velocidad mín. para caminar |
| `runThreshold` | ZombieAnimationController | 2.5 | Velocidad mín. para correr |
| `crawlHealthPercent` | ZombieAnimationController | 0.25 | % de vida para arrastrarse |
| `crawlSpeed` | ZombieAI | 1.5 | Velocidad al arrastrarse |
| `crawlAttackRange` | ZombieAI | 1.5 | Rango ataque arrastrándose |
