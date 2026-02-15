# 📋 Documentación Completa de Scripts — Juego de Zombis

> Análisis exhaustivo de **todos** los scripts C# del proyecto.  
> Generado automáticamente. Última revisión: junio 2025.

---

## Estructura del Proyecto

```
Assets/Scripts/
├── (Raíz)                  → 22 scripts — mecánicas generales, puertas, compras, puzzles
├── Debug/                  → 1 script  — herramientas de depuración de audio
├── Enemies/                → 6 scripts — IA zombi, salud, spawner, animaciones, oleadas
├── Player/                 → 6 scripts — movimiento FPS, animaciones, puntos, cámara
├── UI/                     → 10 scripts — HUD, pausa, salud, puntos, daño, diálogos
├── Vehicles/               → 2 scripts — barco (controlador + interacción)
└── Weapons/                → 6 scripts — armas FPS, cambio de arma, compra en pared, balas
```

**Total: 53 scripts C#**

---

## Índice de Documentos

| # | Documento | Contenido |
|---|-----------|-----------|
| 1 | [01_SCRIPTS_RAIZ.md](01_SCRIPTS_RAIZ.md) | Scripts raíz (puertas, compras, puzzles, inventario, dinero, salud) |
| 2 | [02_SCRIPTS_DEBUG.md](02_SCRIPTS_DEBUG.md) | AudioDebugger |
| 3 | [03_SCRIPTS_ENEMIES.md](03_SCRIPTS_ENEMIES.md) | IA Zombi, salud enemigo, spawner, oleadas, animaciones |
| 4 | [04_SCRIPTS_PLAYER.md](04_SCRIPTS_PLAYER.md) | Movimiento FPS, stamina, puntos, animaciones jugador |
| 5 | [05_SCRIPTS_UI.md](05_SCRIPTS_UI.md) | HUD completo, pausa, salud UI, diálogos, popups |
| 6 | [06_SCRIPTS_VEHICLES.md](06_SCRIPTS_VEHICLES.md) | Barco (controlador + interacción) |
| 7 | [07_SCRIPTS_WEAPONS.md](07_SCRIPTS_WEAPONS.md) | Armas FPS, cambio de arma, compra en pared, balas |
| 8 | [08_SISTEMAS_Y_MECANICAS.md](08_SISTEMAS_Y_MECANICAS.md) | Resumen de sistemas, interacciones entre scripts, singletons |
| 9 | [09_PROBLEMAS_Y_MEJORAS.md](09_PROBLEMAS_Y_MEJORAS.md) | Bugs conocidos, inconsistencias, áreas de mejora |

---

## Patrones Arquitectónicos Detectados

| Patrón | Uso |
|--------|-----|
| **Singleton** | PlayerMoney, PlayerHealth, PlayerPoints, PlayerInventory, GameHUD, ZombieSpawner, PauseManager, DialogueManager |
| **Observer (Eventos)** | `PlayerPoints.OnPointsChanged` → PointsUI, WeaponUI, GameHUD |
| **FindObjectOfType** | Usado extensivamente para auto-enlazar referencias en Start() |
| **OnGUI (Legacy UI)** | Prompts de interacción en casi todos los scripts de compra/puertas |
| **Programmatic UI** | GameHUD, PauseMenuCreator, PlayerHealthUI crean UI en código sin prefabs |
| **Coroutines** | Animaciones de puertas, recarga, spawn, diálogos |

---

## Sistemas Principales

1. **Movimiento FPS** — WASD + ratón, sprint con stamina, agacharse, saltar
2. **Salud Jugador** — Regeneración CoD-style, viñeta de sangre, heartbeat
3. **Economía Dual** — PlayerMoney (legacy) + PlayerPoints (nuevo) — **inconsistencia**
4. **Armas FPS** — Raycast + balas visuales, recarga, retroceso, casquillos
5. **Cambio de Arma** — WeaponSwitcher con holster/draw animados, máx 2 armas
6. **Oleadas de Zombis** — Por zonas, escala salud/daño, zona infinita
7. **IA Zombi** — NavMeshAgent, persecución, ataque cuerpo a cuerpo, crawl
8. **Puertas y Barreras** — Comprables, con llave, eléctricas, trampas
9. **Puzzle Simon Says** — Minijuego de memoria con 4 pantallas de colores
10. **HUD Programático** — Salud, stamina, munición, puntos, oleada, brújula, crosshair
11. **Vehículos** — Barco con flotación, cámaras, detección de agua
12. **Inventario** — Llaves para puertas cerradas
