# 🎮 Instalación Automática del Sistema Nuclear Terminal

## 📋 Resumen Rápido
Tu Canvas se genera **automáticamente** con un script. Solo sigue estos pasos:

---

## 🚀 PASO 1: Crear el GameObject Principal

1. En la escena, crea un nuevo GameObject vacío
2. Nómbralo: **`NuclearTerminal`**
3. Posiciónalo en el mapa donde quieras la terminal

---

## 🚀 PASO 2: Agregar los 3 Scripts

### 📌 TerminalController.cs
**DÓNDE:** En el GameObject `NuclearTerminal`
- Click derecho → Add Component → TerminalController

### 📌 TerminalInteraction.cs  
**DÓNDE:** En el GameObject `NuclearTerminal`
- Click derecho → Add Component → TerminalInteraction

### 📌 TerminalUIGenerator.cs
**DÓNDE:** En el GameObject `NuclearTerminal`
- Click derecho → Add Component → TerminalUIGenerator

---

## 🚀 PASO 3: Configurar AudioSource

El GameObject `NuclearTerminal` necesita un **AudioSource**:

1. Click derecho → Add Component → AudioSource
2. En el Inspector:
   - ✅ Spatial Blend = **1.0** (para que suene en 3D)
   - ✅ Volume = **0.8**

---

## 🚀 PASO 4: Generar el Canvas

1. En el Inspector, ve al componente **TerminalUIGenerator**
2. En el campo "Terminal Controller", **arrastra** el mismo GameObject `NuclearTerminal`
3. Presiona el botón "Generate UI" en ese componente
4. ✅ ¡El Canvas se crea automáticamente!

```
📊 Resultado:
├── NuclearTerminal (con 3 scripts)
├── AudioSource
└── TerminalUI (Canvas - generado automáticamente)
    ├── BackgroundPanel
    ├── Title
    ├── CodeDisplay
    ├── CodeInput
    ├── SubmitButton
    └── ClearButton
```

---

## 🎵 PASO 5: Asignar Audios

En el Inspector del `NuclearTerminal`, ve a **TerminalController**:

### Números ( 1-9)
Arrastra 9 audios al array **"Number Audios"**:
- Índice 0 = sonido del número "1"
- Índice 1 = sonido del número "2"
- ... etc
- Índice 8 = sonido del número "9"

**Todas las carpetas de audio están en:**
```
Assets/Sonidos/
```

### Efectos
- **Correct Sound**: El audio de "correcto"
- **Error Sound**: El audio de "error"
- **Nuclear Siren**: La sirena nuclear prolongada

---

## 🚀 PASO 6: Crear GameObject de Explosión

1. En la escena, crea otro GameObject vacío
2. Nómbralo: **`NuclearExplosion`**
3. Posiciónalo donde quieres que aparezca la explosión
4. Agrega el script **NuclearExplosion.cs**:
   - Click derecho → Add Component → NuclearExplosion

---

## 🔗 PASO 7: Conectar los Eventos

### En TerminalController:
1. Abre el evento **"On Nuclear Activated"** (en Inspector)
2. Click en **"+"** para agregar listener
3. Arrastra el GameObject **`NuclearExplosion`** al campo
4. En el dropdown que aparece: **NuclearExplosion > Detonate()**

**Así se verá:**
```
On Nuclear Activated
  └─ Runtime Only
     └─ NuclearExplosion.Detonate()
```

---

## ✅ CHECKLIST FINAL

- [ ] GameObject `NuclearTerminal` creado
- [ ] TerminalController.cs agregado
- [ ] TerminalInteraction.cs agregado
- [ ] TerminalUIGenerator.cs agregado
- [ ] AudioSource configurado (Spatial Blend = 1.0)
- [ ] Canvas generado (botón "Generate UI")
- [ ] 9 audios asignados (números 1-9)
- [ ] 3 efectos asignados (correct, error, siren)
- [ ] GameObject `NuclearExplosion` creado
- [ ] NuclearExplosion.cs agregado
- [ ] Evento conectado: onNuclearActivated → Detonate()

---

## 🎮 PRUEBA

1. Acércate a la terminal (4m por defecto)
2. Verás: **"Pulsa E - INICIAR PROTOCOLO"** (en rojo)
3. Presiona **E**
4. Escucharás 6 números aleatorios
5. Escribe los números en el campo
6. Presiona **CONFIRMAR**
7. Si es correcto → **¡EXPLOSIÓN!**

---

## 🐛 TROUBLESHOOTING

| Problema | Solución |
|----------|----------|
| No aparece el texto "Pulsa E" | Asegúrate que TerminalInteraction.cs está en `NuclearTerminal` |
| No se generan audios | Verifica que los 9 audios están en el array "Number Audios" |
| No hay sonidos de números | Los audios deben estar en formato WAV/MP3 compatible con Unity |
| La explosión no funciona | Conecta el evento `onNuclearActivated` a `NuclearExplosion.Detonate()` |
| Canvas no aparece | Presiona "Generate UI" en TerminalUIGenerator |

---

## 📂 Estructura de Archivos

```
Assets/Scripts/
├── Interactables/
│   ├── TerminalController.cs ✅
│   ├── TerminalInteraction.cs ✅
│   ├── TerminalUIGenerator.cs ✅
│   └── NuclearExplosion.cs ✅
```

---

## 🎯 Comportamiento del Sistema

### Secuencia Correcta
```
1. Acercarse a terminal → Prompt "E"
2. Presionar E
3. Terminal reproduce 6 números (1-9 random)
   └─ Espera con display "_____"
4. Escedes 6 dígitos en campo de entrada
5. Presionas CONFIRMAR
6. Si coinciden → Sonido de éxito + EXPLOSIÓN
   Si NO coinciden → Sonido de error + se repiten números
```

### Zona de Daño
- Radio de explosión: **100 metros**
- Daño máximo: **999**
- Falloff: Disminuye con distancia

---

**¡Listo! El sistema está 100% automatizado.** 🚀
