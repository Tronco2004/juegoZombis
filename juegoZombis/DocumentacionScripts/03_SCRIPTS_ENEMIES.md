# 🧟 Scripts Enemies — `Assets/Scripts/Enemies/`

> 6 scripts que controlan la IA de los zombis, su salud, animaciones, spawning y oleadas.

---

## EnemyHealth.cs (336 líneas)

**Propósito:** Sistema de salud de los zombis con detección de headshot, salud aleatoria, barra de vida y muerte con ragdoll.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `maxHealth = 100`, `useRandomHealth = true` (rango 100–200), `pointsOnKill = 400`, `headshotMultiplier = 2f` |
| **Headshot** | Detecta por nombre de hueso (`headBoneNames[]`: "head", "Head", "cabeza") **Y** por posición (`headHeightPercent = 0.25f` — top 25% del collider) |

### Métodos clave:

| Método | Descripción |
|--------|-------------|
| `TakeDamage(damage)` | Versión simple sin posición |
| `TakeDamage(damage, hitPoint, isHeadshot)` | Versión completa con headshot |
| `IsHeadshot(Transform)` / `IsHeadshot(Collider)` | Verifica si el impacto es en la cabeza |
| `Die()` | Da puntos, reproduce animación de muerte, ragdoll o destroy en 3s |

### Flujo de daño:
```
Bala impacta → FPSWeaponController.ShootRaycast()
  → EnemyHealth.IsHeadshot() verifica cabeza
  → EnemyHealth.TakeDamage(damage, hitPoint, isHeadshot)
    → Aplica headshotMultiplier si corresponde
    → Crea DamagePopup.Create()
    → Notifica EnemyHealthBar
    → Notifica ZombieAI.OnTakeDamage()
    → Si health <= 0 → Die()
      → PlayerPoints.AddPoints(pointsOnKill)
      → ZombieAnimationController.PlayDeath()
```

**Interacciones:**
- → `PlayerPoints.AddPoints()`
- → `DamagePopup.Create()`
- → `ZombieAnimationController.PlayDeath()` / `PlayHitReaction()`
- → `ZombieAI.OnTakeDamage()`
- → `EnemyHealthBar.OnDamaged()`
- ← `FPSWeaponController`, `Bullet`, `BulletController`, `WeaponController`

---

## ZombieAI.cs (564 líneas)

**Propósito:** IA del zombi usando NavMeshAgent — persecución, ataque cuerpo a cuerpo, sonidos ambiente, crawl a baja vida.

| Elemento | Detalle |
|----------|---------|
| **Movimiento** | `speed = 20f`, `chaseRange = 50f`, NavMeshAgent |
| **Ataque** | `attackRange = 2.5f`, `attackCooldown = 1.0f`, `damage = 20f` |
| **Crawl** | `crawlSpeed = 1.5f` — activado cuando la vida es baja |

### Sistema de sonidos:

| Tipo | Variable | Uso |
|------|----------|-----|
| Idle | `idleSounds[]` | Sonidos aleatorios cuando está lejos |
| Chase | `chaseSounds[]` | Mientras persigue al jugador |
| Attack | `attackSounds[]` | Al atacar |
| Death | `deathSounds[]` | Al morir |
| Scream | `screamSounds[]` | Al detectar al jugador por primera vez |
| Crawl | `crawlSounds[]` | Arrastrándose |
| Hurt | `hurtSounds[]` | Al recibir daño |
| **Probabilidad** | `groanChance = 0.3f` | 30% de probabilidad de emitir sonido |

### Comportamiento:
1. **Siempre persigue** al jugador (no hay estado idle real)
2. **Grita** al detectar al jugador por primera vez
3. **Ataca** cuando está en rango (`attackRange`)
4. **Crawl** cuando la vida baja del umbral
5. **Reubicación:** Si está demasiado lejos → `ZombieSpawner.RelocateZombie()` lo mueve a un spawn point cercano

### Métodos:

| Método | Descripción |
|--------|-------------|
| `OnTakeDamage()` | Llamado por EnemyHealth al recibir daño |
| `ResetDetection()` | Reset al ser reubicado — puede volver a gritar |
| `SetDamage(float)` | Configura daño (llamado por ZombieSpawner al escalar oleadas) |

**Interacciones:**
- → `PlayerHealth.TakeDamage(damage, transform.position)`
- → `ZombieAnimationController` (estados de animación)
- → `ZombieSpawner.RelocateZombie()`
- ← `EnemyHealth.OnTakeDamage()`

---

## ZombieAnimationController.cs (379 líneas)

**Propósito:** Gestiona todos los estados de animación del zombi via parámetros del Animator.

| Elemento | Detalle |
|----------|---------|
| **Enum** | `ZombieAnimState`: Idle, Walking, Running, Attacking, Crawling, CrawlRunning, Screaming, Dead |
| **Optimización** | Hashes cacheados con `Animator.StringToHash()` |
| **Crawl** | `crawlHealthPercent = 0.25f` — bajo del 25% de vida |

### Métodos:

| Método | Descripción |
|--------|-------------|
| `UpdateLocomotion(speed)` | Actualiza blend idle/walk/run según velocidad |
| `PlayAttack()` | Randomiza entre Attack/Bite/NeckBite |
| `PlayScream()` | Solo una vez por vida |
| `SetCrawling(true/false)` | Activa modo arrastrarse |
| `PlayDeath()` | Animación de muerte |
| `PlayHitReaction()` | Reacción al impacto + knockback (stagger) temporal |

**Interacciones:** ← `ZombieAI`, ← `EnemyHealth`

---

## ZombieSpawner.cs (663 líneas)

**Propósito:** Spawner global de zombis con sistema de oleadas por zonas y soporte de zona infinita.

| Elemento | Detalle |
|----------|---------|
| **Singleton** | `ZombieSpawner.Instance` |
| **Enum** | `SpawnZone`: Zona1, Zona2, Mansion, AtrasMansion, Zona3_Infinitos |
| **Escalado** | `baseZombies = 10`, `healthPerWave = 20f`, `damageIncreaseEveryWaves = 4` |

### Sistema de zonas:

| Zona | Comportamiento |
|------|----------------|
| Zona1 | Oleadas normales |
| Zona2 | Oleadas normales |
| Mansion | Oleadas normales |
| AtrasMansion | Oleadas normales |
| **Zona3_Infinitos** | Spawn **continuo** (máx 20 vivos, `infiniteSpawnInterval = 1.5f`) |

### Coroutines principales:

| Coroutine | Descripción |
|-----------|-------------|
| `SpawnLoop()` | Oleada estándar — spawn con intervalo, espera a matar todos |
| `InfiniteSpawnLoop()` | Spawn continuo sin oleadas |
| `RelocateLoop()` | Cada 5s, mueve zombis lejanos a spawn points cercanos al jugador |
| `ZoneCheckLoop()` | Detecta cambios de zona del jugador |

### Escalado por oleada:
```
zombiesEnOleada = baseZombies + (wave - 1) * 3
healthZombie = baseHealth + (wave - 1) * healthPerWave
damageZombie = baseDamage (sube cada damageIncreaseEveryWaves oleadas)
```

**Interacciones:**
- → Instancia prefab de zombi con `ZombieAI`, `EnemyHealth`, `ZombieWaveMember`
- → `ConfigureWaveZombie()` escala stats
- → TMPro para texto de oleada (legacy, reemplazado por GameHUD)
- ← `ZombieWaveMember.OnDestroy()` → `NotifyZombieDestroyed()`

---

## ZombieSpawnPoint.cs (126 líneas)

**Propósito:** Punto de spawn individual con asignación de zona y área rectangular de spawn.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `SpawnZone zone`, `spawnZoneSize` (Vector2), `activationRange = 60f` |
| **Métodos** | `GetRandomSpawnPosition()` — posición aleatoria dentro del área, `IsPlayerInRange()` |
| **Gizmos** | Cubos coloreados por zona (Rojo=Zona1, Azul=Zona2, Verde=Mansion, etc.) |

**Interacciones:** ← `ZombieSpawner`

---

## ZombieWaveMember.cs (25 líneas)

**Propósito:** Componente añadido a cada zombi spawneado para notificar al spawner cuando muere.

| Elemento | Detalle |
|----------|---------|
| **Variables** | `isInfiniteZombie` (bool), `spawner` (ZombieSpawner ref) |
| **Lógica** | `OnDestroy()` → `spawner.NotifyZombieDestroyed(this)` |

**Interacciones:** → `ZombieSpawner.NotifyZombieDestroyed()`
