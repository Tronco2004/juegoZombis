# 📁 Guía de Ubicación de Scripts - Terminal Nuclear

## Estructura de Carpetas en Assets/

```
Assets/
├── Scripts/
│   ├── Interactables/                 ← AQUÍ VAN LOS 4 SCRIPTS NUEVOS
│   │   ├── TerminalController.cs ✅ (ya existe)
│   │   ├── TerminalInteraction.cs ✅ (ya existe)
│   │   ├── TerminalUIGenerator.cs ✅ (nuevo - lo acabo de crear)
│   │   └── NuclearExplosion.cs ✅ (ya existe)
│   │
│   ├── Vehicles/
│   │   ├── BoatController.cs
│   │   ├── BoatInteraction.cs
│   │   ├── HelicopterController.cs
│   │   └── HelicopterInteraction.cs
│   │
│   ├── (otros folders...)
│   │
│   └── (raíz)
```

---

## En el Proyecto Unity (Scene)

```
Hierarchy
├── [Player]
├── [Enemies]
├── [Weapons]
│
├── NuclearTerminal ✅ AGREGA ESTOS 4 SCRIPTS AQUÍ
│   ├── Transform (posición donde está la terminal en el mapa)
│   ├── AudioSource (para reproducir audios)
│   ├── TerminalController.cs ← Script principal
│   ├── TerminalInteraction.cs ← Prompt "Pulsa E"
│   └── TerminalUIGenerator.cs ← Genera Canvas automáticamente
│
├── NuclearExplosion ✅ AGREGA ESTE SCRIPT AQUÍ
│   ├── Transform (posición de explosión)
│   └── NuclearExplosion.cs ← Maneja el daño y efectos
│
└── TerminalUI (Canvas) ← SE CREA AUTOMÁTICAMENTE
    ├── BackgroundPanel
    ├── Title
    ├── CodeDisplay
    ├── CodeInput
    ├── SubmitButton
    └── ClearButton
```

---

## Paso a Paso Visual

### 1️⃣ Crear GameObject NuclearTerminal

```
Right Click en Hierarchy
  → Create Empty
  → Renombra a "NuclearTerminal"
  → Mueve a la posición deseada en el mapa
```

### 2️⃣ Agregar AudioSource

```
Selecciona NuclearTerminal en Hierarchy
  → En Inspector → Add Component
  → Busca "AudioSource"
  → Click en "AudioSource"
```

### 3️⃣ Agregar TerminalController.cs

```
Click en "Add Component"
  → Busca "TerminalController"
  → Click en "TerminalController"
```

**En el Inspector verás:**
- codeLength = 6
- numberAudios[] = array vacío
- correctSound (vacío)
- errorSound (vacío)
- nuclearSiren (vacío)
- terminalUI (vacío)
- codeInputField (vacío)
- codeDisplay (vacío)
- submitButton (vacío)
- clearButton (vacío)
- onNuclearActivated = evento

### 4️⃣ Agregar TerminalInteraction.cs

```
Click en "Add Component"
  → Busca "TerminalInteraction"
  → Click en "TerminalInteraction"
```

### 5️⃣ Agregar TerminalUIGenerator.cs

```
Click en "Add Component"
  → Busca "TerminalUIGenerator"
  → Click en "TerminalUIGenerator"
```

**En el Inspector verás:**
- Terminal Controller (vacío)

---

## 6️⃣ Generar el Canvas

```
En TerminalUIGenerator component:
  → Arrastra "NuclearTerminal" al campo "Terminal Controller"
  → Presiona el botón mágico que aparece "Generate UI"
  
✅ ¡El Canvas se crea automáticamente!
```

---

## 7️⃣ Asignar Audios

```
En TerminalController component:
  → Number Audios[] (tamaño 9)
     - [0] = Audio del número "1" ← Arrastra aquí
     - [1] = Audio del número "2" ← Arrastra aquí
     - ... etc
     - [8] = Audio del número "9" ← Arrastra aquí
  
  → Correct Sound = Audio "correcto"
  → Error Sound = Audio "error"  
  → Nuclear Siren = Audio "sirena"
```

Todos en: `Assets/Sonidos/`

---

## 8️⃣ Crear GameObject NuclearExplosion

```
Right Click en Hierarchy
  → Create Empty
  → Renombra a "NuclearExplosion"
  → Posiciona donde quieres explosión
```

### Agregar NuclearExplosion.cs

```
Click en "Add Component"
  → Busca "NuclearExplosion"
  → Click en "NuclearExplosion"
```

---

## 9️⃣ Conectar el Evento

```
Selecciona "NuclearTerminal" en Hierarchy

En TerminalController component:
  → Baja hasta "On Nuclear Activated"
  → Click en "+" para agregar listener
  → Arrastra "NuclearExplosion" (el GameObject)
     al campo que aparece
  → En dropdown: "NuclearExplosion > Detonate()"
```

**Debería verse así:**
```
On Nuclear Activated
├─ Size: 1
└─ Element 0: NuclearExplosion.Detonate()
```

---

## 🎯 Verificación Final

```
✅ NuclearTerminal
   ├─ Transform
   ├─ AudioSource
   ├─ TerminalController
   ├─ TerminalInteraction
   └─ TerminalUIGenerator

✅ NuclearExplosion
   ├─ Transform
   └─ NuclearExplosion

✅ Canvas (generado automáticamente)
```

---

## 📂 Rutas de Archivos Exactas

| Script | Ruta Exacta |
|--------|-----------|
| TerminalController.cs | `Assets/Scripts/Interactables/TerminalController.cs` |
| TerminalInteraction.cs | `Assets/Scripts/Interactables/TerminalInteraction.cs` |
| TerminalUIGenerator.cs | `Assets/Scripts/Interactables/TerminalUIGenerator.cs` |
| NuclearExplosion.cs | `Assets/Scripts/Interactables/NuclearExplosion.cs` |

---

## ⚙️ Campos a Llenar en Inspector

### NuclearTerminal → TerminalController

| Campo | Tipo | Ejemplo | Obligatorio |
|-------|------|---------|-------------|
| Code Length | int | 6 | ✅ |
| Number Audios | AudioClip[] | [9 audios] | ✅ |
| Correct Sound | AudioClip | correct_audio | ✅ |
| Error Sound | AudioClip | error_audio | ✅ |
| Nuclear Siren | AudioClip | siren_audio | ✅ |
| Time Between Numbers | float | 1.5 | ✅ |

---

**¡Listo! Todos los scripts están en la misma carpeta `Assets/Scripts/Interactables/`** 🎉
