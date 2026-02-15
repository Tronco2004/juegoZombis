# 🔴 SISTEMA DE TERMINAL NUCLEAR

## Descripción General

Sistema interactivo de **activación de bomba nuclear** con código de seguridad de 6 números. El jugador debe:

1. **Acercarse** a la terminal (radio ~4m)
2. **Pulsar E** para iniciar el protocolo
3. **Escuchar** 6 números reproducidos con audio
4. **Escribir** los números en el campo de entrada
5. **Confirmar** el código
6. Si **acierta**: ¡SIRENA NUCLEAR! 💣
7. Si **falla**: Reinicia desde cero

## Componentes

### 1. **TerminalController.cs**
- **Función**: Genera el código, reproduce audios, valida entrada
- **Responsabilidades**:
  - Generar código aleatorio (1-9, 6 dígitos)
  - Reproducir audios de números secuencialmente
  - Validar código introducido
  - Disparar evento cuando acierte

**Propiedades principales**:
```csharp
public int codeLength = 6;                    // Largo del código
public AudioClip[] numberAudios = ...;        // Audios 1-9
public AudioClip correctSound;                // Sonido de acierto
public AudioClip errorSound;                  // Sonido de error
public AudioClip nuclearSiren;                // Sirena nuclear
public float timeBetweenNumbers = 1.5f;       // Tiempo entre números
public Canvas terminalUI;                     // Canvas de UI
public TMP_InputField codeInputField;         // Campo de entrada
public UnityEngine.Events.UnityEvent onNuclearActivated;  // Evento
```

### 2. **TerminalInteraction.cs**
- **Función**: Maneja la interacción del jugador (distancia, tecla)
- **Responsabilidades**:
  - Detectar si el jugador está en rango
  - Mostrar mensaje "Pulsa E"
  - Disparar protocolo al presionar E

### 3. **NuclearExplosion.cs**
- **Función**: Efecto visual y daño de la explosión
- **Responsabilidades**:
  - Flash de luz
  - Daño a zombies cercanos
  - Shake de cámara
  - Explosión visual

## Instalación Rápida

### Paso 1: Crear GameObject de Terminal
```
Crea un GameObject vacío: "NuclearTerminal"
├─ Add Component: Sphere Collider (Is Trigger: true, Radius: 4)
├─ Add Component: Audio Source (Spatial Blend: 1.0)
├─ Add Component: TerminalController
├─ Add Component: TerminalInteraction
└─ Add Component: NuclearExplosion
```

### Paso 2: Asignar Audios en Inspector
En **TerminalController**:
- **Number Audios[0-8]**: Audios de números 1-9
- **Correct Sound**: Sonido de confirmación
- **Error Sound**: Sonido de error
- **Nuclear Siren**: Sirena

### Paso 3: Crear Canvas
```
Hierarchy: Right-click → UI → Canvas
├─ Panel (Fondo negro semi-transparente)
├─ TextMeshPro "Title" → "CÓDIGO DE SEGURIDAD"
├─ TMP_InputField "CodeInput"
├─ TextMeshPro "CodeDisplay"
├─ Button "SubmitButton" → "CONFIRMAR"
└─ Button "ClearButton" → "LIMPIAR"
```

### Paso 4: Conectar UI en TerminalController Inspector
```
Terminal UI: Canvas
Code Input Field: CodeInput (TMP_InputField)
Code Display: CodeDisplay (TextMeshPro)
Submit Button: SubmitButton
Clear Button: ClearButton
```

### Paso 5: Conectar Evento
En **TerminalController**:
```
On Nuclear Activated → +
  Object: NuclearTerminal
  Function: NuclearExplosion.Detonate()
```

### Paso 6: Configurar Efectos
En **NuclearExplosion**:
```
Blast Radius: 100
Max Damage: 999
Flash Light: (Asigna una luz existente)
Flash Intensity: 3
Flash Duration: 0.5
```

## Flujo de Uso

### Flujo Normal:
1. Jugador se acerca a la terminal
2. Aparece: `"📍 Pulsa E - INICIAR PROTOCOLO"` (texto rojo)
3. Pulsa E
4. Se muestra Canvas con "CÓDIGO DE SEGURIDAD"
5. Suena: *número 1, número 2, número 3...*
6. Campo de entrada activado, jugador escribe
7. Pulsa "CONFIRMAR"
8. **¡CORRECTO!** → Sirena nuclear 🔊
9. NuclearExplosion se dispara

### Flujo de Error:
1. Jugador escribe mal un número
2. Pulsa "CONFIRMAR"
3. Suena error ❌
4. Se limpia el campo
5. Se reproduce el código de nuevo
6. Reintentar...

## Customización

### Cambiar largo del código:
```csharp
// En TerminalController Inspector:
Code Length: 8 (en lugar de 6)
```

### Cambiar rango de detección:
```csharp
// En TerminalInteraction Inspector:
Interaction Range: 6 (en lugar de 4)
```

### Cambiar tiempo entre números:
```csharp
// En TerminalController Inspector:
Time Between Numbers: 2.0 (en lugar de 1.5)
```

### Cambiar daño de explosión:
```csharp
// En NuclearExplosion Inspector:
Blast Radius: 150
Max Damage: 1500
```

## Notas Técnicas

- El código se genera **aleatoriamente** cada vez que se inicia el protocolo
- Los números son **1-9** (sin 0 para evitar confusiones)
- Si falla, **reinicia desde 0** sin ofrecer más intentos
- El daño tiene **falloff** según la distancia
- La sirena se reproduce en **loop** hasta que se apague

## Sonidos Requeridos

Necesitas proporcionar:
- ✅ 9 audios de números (1-9)
- ✅ 1 sonido de confirmación
- ✅ 1 sonido de error
- ✅ 1 sirena nuclear (para loopear)

## Compatibilidad

- ✅ Unity 2020+
- ✅ TextMeshPro
- ✅ Compatible con sistema de enemigos existente (ZombieHealth)
- ✅ Compatible con cámaras de primera persona

---

**Creado:** 15 de febrero de 2026  
**Estado:** Sistema completo y funcional
