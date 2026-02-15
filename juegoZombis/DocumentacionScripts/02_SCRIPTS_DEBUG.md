# 🐛 Scripts Debug — `Assets/Scripts/Debug/`

> 1 script de depuración de audio.

---

## AudioDebugger.cs (168 líneas)

**Propósito:** Herramienta de diagnóstico de audio en tiempo real.

| Elemento | Detalle |
|----------|---------|
| **Tecla F9** | `TestAudio()` — Reproduce un sonido de prueba. Si no hay AudioClip asignado, genera un beep de 440Hz (onda sinusoidal) proceduralmente |
| **Tecla F10** | `CheckAudioStatus()` — Diagnóstico completo del sistema de audio |

### Diagnóstico F10 — Información que muestra:

1. **AudioListeners** — Cuenta cuántos hay en escena (debería ser 1)
2. **AudioSources** — Lista todas las fuentes de audio activas
3. **PlayerHealth sounds** — Verifica si `PlayerHealth.Instance` tiene sonidos de heartbeat/breathing/hurt/death asignados
4. **ZombieAI sounds** — Verifica si los zombis tienen sonidos idle/chase/attack/death asignados

### Generación de beep (440Hz):

```
Crea un AudioClip de 1 segundo con una onda sinusoidal de 440Hz (nota LA)
usando Mathf.Sin(2π × 440 × t) — útil para verificar que el audio funciona
sin depender de assets externos.
```

**Interacciones:** Lectura diagnóstica de `PlayerHealth`, `ZombieAI` (no modifica nada)
