# 🎒 Sistema de Inventario — Hotbar Estilo Fortnite

> Sistema de inventario con barra horizontal de 5 slots al estilo Fortnite.  
> Incluye visualización de armas, granadas, items y vista de inspección 3D.  
> Creado enteramente por código (sin prefabs UI).

---

## Estructura de Archivos

```
Assets/Scripts/Inventory/
├── InventoryItemData.cs     → ScriptableObject: datos de cada item
├── InventorySystem.cs       → Singleton: lógica central de 5 slots
├── InventorySlotUI.cs       → Componente visual de un slot individual
├── InventoryBarUI.cs        → Hotbar completa (crea Canvas por código)
└── ItemInspector3D.cs       → Vista de inspección 3D / lectura de notas
```

---

## Distribución de Slots

| Slot | Índice | Tipo | Contenido |
|------|--------|------|-----------|
| 1 | 0 | Arma | Primera arma de fuego (sincronizada con WeaponSwitcher) |
| 2 | 1 | Arma | Segunda arma de fuego (sincronizada con WeaponSwitcher) |
| 3 | 2 | Granada | Granadas (con contador de cantidad) |
| 4 | 3 | Item/Nota | Item recogido (peluche, llave, etc.) |
| 5 | 4 | Item/Nota | Segundo item recogido |

---

## Clases y Responsabilidades

### `InventoryItemData` (ScriptableObject)

Define los datos de un item del inventario. Se crea desde el menú de Unity:
**Assets > Create > Inventory > Item Data**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `itemName` | string | Nombre del item |
| `description` | string | Descripción (se muestra en inspección) |
| `icon` | Sprite | Icono para el hotbar |
| `itemType` | ItemType | Weapon / Grenade / Item / Note |
| `inspectionPrefab` | GameObject | Modelo 3D para inspección |
| `inspectionScale` | float | Escala del modelo en inspección |
| `inspectionOffset` | Vector3 | Offset de posición |
| `inspectionRotation` | Vector3 | Rotación inicial |
| `noteText` | string | Texto legible (solo para tipo Note) |
| `noteBackground` | Sprite | Fondo visual de la nota |

### `InventorySystem` (Singleton)

Sistema central que gestiona los 5 slots. Se sincroniza automáticamente con `WeaponSwitcher` para los slots de armas.

**Constantes:**
- `TOTAL_SLOTS = 5`
- `WEAPON_SLOT_1 = 0`, `WEAPON_SLOT_2 = 1`
- `GRENADE_SLOT = 2`
- `ITEM_SLOT_1 = 3`, `ITEM_SLOT_2 = 4`

**Eventos:**
| Evento | Firma | Cuándo se dispara |
|--------|-------|-------------------|
| `OnSlotChanged` | `Action<int, InventorySlotData>` | Al añadir/quitar/actualizar un slot |
| `OnSelectionChanged` | `Action<int>` | Al cambiar el arma activa |
| `OnInspectRequested` | `Action<InventorySlotData>` | Al hacer click en un slot de items |

**API pública:**
```csharp
bool AddItem(InventoryItemData data, GameObject source = null)  // Añadir item
void RemoveItem(int slotIndex)                                   // Quitar por índice
void RemoveItem(string itemName)                                 // Quitar por nombre
bool UseGrenade()                                                // Gastar 1 granada
bool HasItem(string itemName)                                    // Comprobar existencia
InventorySlotData GetSlot(int index)                             // Leer slot
int GetGrenadeCount()                                            // Cantidad de granadas
void RequestInspect(int slotIndex)                               // Abrir inspección
```

### `InventorySlotUI`

Componente visual para un slot individual. Gestiona iconos, colores, texto de munición, animación de selección.

**Colores por tipo:**
- **Arma**: Dorado `(0.85, 0.75, 0.1)`
- **Granada**: Verde `(0.2, 0.7, 0.2)`
- **Item**: Azul `(0.3, 0.5, 0.85)`
- **Nota**: Marrón `(0.7, 0.5, 0.3)`

### `InventoryBarUI` (Singleton)

Crea toda la UI del hotbar por código (Canvas + 5 slots). Se posiciona centrado en la parte inferior de la pantalla. Incluye separadores visuales entre secciones.

### `ItemInspector3D` (Singleton)

Vista de inspección al estilo Resident Evil:
- **Items 3D**: Cámara exclusiva + RenderTexture, rotación con click izquierdo, zoom con scroll
- **Notas**: Panel de texto tipo pergamino con scroll
- Pausa el juego (`Time.timeScale = 0`) cuando está abierto
- Se cierra con ESC, E o click derecho

---

## Integración con Sistemas Existentes

### WeaponSwitcher → InventorySystem
La sincronización es **automática**. `InventorySystem.Update()` lee constantemente los datos de `WeaponSwitcher.weapons[]` y actualiza los slots 0-1 con nombre del arma, icono y munición.

### Pickups → InventorySystem
Los scripts de recogida (`PeluchePickup`, `KeyItem`) ahora tienen un campo `inventoryItemData` opcional:

```csharp
[Header("=== INVENTARIO ===")]
public InventoryItemData inventoryItemData;
```

Si se asigna un ScriptableObject, el item aparecerá en el hotbar al recogerlo. Compatibilidad total con el sistema antiguo: siguen llamando a `PlayerInventory.Instance.AddKey()`.

---

## Puesta en Marcha

### 1. Crear InventoryItemData para cada item

1. Click derecho en Project → **Create > Inventory > Item Data**
2. Configurar nombre, icono, tipo y datos de inspección
3. Para notas: rellenar `noteText`

### 2. Asignar a los pickups del mundo

En el Inspector de cada pickup (`PeluchePickup`, `KeyItem`):
- Arrastrar el `InventoryItemData` correspondiente al campo **"Inventory Item Data"**

### 3. Asignar iconos de armas

En cada `FPSWeaponController`:
- Arrastrar un Sprite al campo **"Weapon Icon"** (bajo "Info del Arma")

### 4. Añadir componentes al jugador

En el GameObject del jugador (o un GameObject persistente):
- Añadir `InventorySystem`
- Añadir `InventoryBarUI`
- Añadir `ItemInspector3D`

> **Nota**: Los tres son Singletons. Solo debe haber una instancia de cada uno.

---

## Jerarquía de UI Generada

```
InventoryBarCanvas (Canvas, sortOrder: 90)
└── InventoryBar (Image + HorizontalLayoutGroup)
    ├── Slot_0 (InventorySlotUI)
    │   ├── SlotBox (Image)
    │   │   ├── Border (Image + Outline)
    │   │   ├── SelectionGlow (Image)
    │   │   ├── Icon (Image)
    │   │   ├── AmmoText (TMP)
    │   │   └── QuantityText (TMP)
    │   └── Label (TMP: "Arma 1")
    ├── Separator_2
    ├── Slot_1 ... Slot_4
    └── ...

InspectorCanvas (Canvas, sortOrder: 200)
├── Overlay (Image + Button → cerrar)
├── ModelDisplay (RawImage → RenderTexture)
├── InfoPanel (Image + VLG)
│   ├── Title (TMP), Line, Description (TMP), Controls (TMP)
└── NotePanel (Image + ScrollRect) → solo para notas
```

---

## Datos de Slot: `InventorySlotData`

```csharp
public class InventorySlotData
{
    public SlotType slotType;      // Weapon, Grenade, Item, Note
    public string itemName;
    public string description;
    public Sprite icon;
    public bool isEmpty;
    public int currentAmmo;        // Solo armas
    public int reserveAmmo;        // Solo armas
    public int quantity;           // Solo granadas
    public InventoryItemData itemData;  // Referencia para inspección
}
```

---

## Flujo de Datos

```
WeaponSwitcher.weapons[] ──(Update)──→ InventorySystem.slots[0-1]
                                           ↓ OnSlotChanged
PeluchePickup.PickupPeluche() ──(AddItem)──→ InventorySystem.slots[3-4]
                                           ↓ OnSlotChanged
KeyItem.PickupKey() ──(AddItem)──→ InventorySystem.slots[3-4]
                                           ↓ OnSlotChanged
                                       InventoryBarUI
                                           ↓ OnSlotChanged
                                       InventorySlotUI.UpdateSlot()

Click en slot 3-4 → InventorySystem.RequestInspect()
                         ↓ OnInspectRequested
                     ItemInspector3D.OpenInspection()
```
