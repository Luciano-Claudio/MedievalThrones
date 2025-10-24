# MedievalThrones

# LOTE 1 — VARIÁVEIS GLOBAIS & MÓDULO FACTIONS

**Versão:** 3.0 (Atualizada - Outubro 2025)  
**Status:** ✅ Refatorado e Validado com Código Atual

---

## 📋 ÍNDICE

1. [Visão Geral do Módulo](#1-visão-geral-do-módulo)
2. [GameEvents - Event Bus Global](#2-gameevents---event-bus-global)
3. [Enums Globais](#3-enums-globais)
4. [GameConfig - Configuração Global](#4-gameconfig---configuração-global)
5. [GameContext - Orquestrador Central](#5-gamecontext---orquestrador-central)
6. [TimeManager - Sistema de Tempo](#6-timemanager---sistema-de-tempo)
7. [Módulo Factions](#7-módulo-factions)
8. [PlayerController](#8-playercontroller)
9. [DayNightLightController](#9-daynightlightcontroller)
10. [Fluxo de Inicialização](#10-fluxo-de-inicialização)
11. [Integração entre Módulos](#11-integração-entre-módulos)
12. [Tabela de Relacionamentos Completa](#12-tabela-de-relacionamentos-completa)
13. [Changelog e Migrações](#13-changelog-e-migrações)

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Objetivo

Fornecer a **fundação técnica do projeto Medieval Thrones**, centralizando:
- ✅ **Variáveis Globais**: Enums, configurações, tempo de jogo
- ✅ **Event Bus Global**: Sistema de comunicação desacoplada entre módulos (GameEvents)
- ✅ **Sistema de Facções**: Definições, banco de dados e reputação em runtime
- ✅ **Orquestração**: Inicialização e injeção de dependências via GameContext

### 1.2 Responsabilidades Principais

O **Lote 1** é responsável por:

1. **GameEvents (Core dos Cores)**:
   - Event bus centralizado com 31+ eventos
   - Comunicação desacoplada entre todos os módulos
   - Substituição completa de eventos locais

2. **Configuração Global** (`GameConfig`):
   - Duração do dia, penalidades de clima/terreno
   - Baseline de economia, custos de unidades especiais
   - Helpers para conversões e cálculos

3. **Gestão de Tempo** (`TimeManager`):
   - Avança relógio global (fração 0..1, dia/hora/minuto)
   - Emite eventos de ciclo dia/noite via GameEvents
   - Detecção automática de virada de dia

4. **Sistema de Facções**:
   - Definições de facções (ScriptableObjects)
   - Banco de dados centralizado (FactionDatabase)
   - Matriz de reputação em runtime (FactionService)

5. **Orquestração** (`GameContext`):
   - Ponto único de inicialização
   - Injeção de dependências entre serviços
   - Setup automático no Awake()

### 1.3 Arquitetura Global do Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│                    GAMEEVENTS (Event Bus)                        │
│                Core dos Cores - 31+ Eventos                      │
│  Categorias: Tempo, Economia, Diplomacia, Câmera, Units, Input  │
└──┬────────┬─────────┬──────────┬──────────┬──────────┬─────────┘
   │        │         │          │          │          │
   ▼        ▼         ▼          ▼          ▼          ▼
┌────────┐┌────────┐┌─────────┐┌─────────┐┌────────┐┌─────────┐
│  Time  ││Factions││  Unit   ││Selection││ Camera ││   UI    │
│Manager ││Service ││(Lote 3) ││(Lote 4) ││(Lote 2)││ System  │
└───┬────┘└────┬───┘└────┬────┘└────┬────┘└───┬────┘└────┬────┘
    │          │         │          │         │          │
    │          │         │          │         │          │
    └──────────┴─────────┴──────────┴─────────┴──────────┘
                          │
                          ▼
                  ┌───────────────┐
                  │  GameContext  │ ← Orquestrador
                  │    (Awake)    │   Inicializa serviços
                  │               │   Injeta dependências
                  └───────┬───────┘
                          │
                ┌─────────┼─────────┐
                ▼         ▼         ▼
          ┌──────────┐┌─────────┐┌────────┐
          │GameConfig││Faction  ││  Time  │
          │          ││Database ││ Manager│
          └──────────┘└─────────┘└────────┘
```

**Fluxo de Comunicação:**
1. Sistema A dispara evento via `GameEvents.RaiseXXX()`
2. GameEvents propaga para todos os listeners inscritos
3. Sistemas B, C, D reagem ao evento sem conhecer A
4. **Desacoplamento total** entre módulos

---

## 2) GAMEEVENTS - EVENT BUS GLOBAL

### 2.1 Visão Geral e Importância

**GameEvents** é o **coração da arquitetura** do Medieval Thrones. É uma classe estática que funciona como **event bus centralizado**, permitindo comunicação desacoplada entre todos os módulos do jogo.

#### **Por que GameEvents é "Core dos Cores"?**

1. **Desacoplamento Total:**
   - Módulos nunca se referenciam diretamente
   - Exemplo: `SelectionManager` não conhece `UnitListUI`, mas ambos usam `OnSelectionChanged`

2. **Testabilidade:**
   - Eventos podem ser mockados em testes unitários
   - Sistemas podem ser testados isoladamente

3. **Escalabilidade:**
   - Adicionar novo listener não requer modificar emissor
   - Fácil adicionar/remover funcionalidades

4. **Manutenibilidade:**
   - Ponto único de documentação de eventos
   - Rastreamento facilitado de fluxos de comunicação

#### **Substituição de Eventos Locais:**

**Antes (Arquitetura Antiga - NÃO use):**
```csharp
// ❌ Evento local (obsoleto)
public class SelectionManager : MonoBehaviour {
    public event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;
    
    void FireChanged() {
        OnSelectionChanged?.Invoke(_selection); // Acoplamento direto
    }
}

// Consumer precisa de referência direta
public class UnitListUI : MonoBehaviour {
    [SerializeField] SelectionManager selectionManager; // ❌ Acoplado
    
    void OnEnable() {
        selectionManager.OnSelectionChanged += UpdateList;
    }
}
```

**Depois (Arquitetura Atual - ✅ Use isso):**
```csharp
// ✅ Evento via GameEvents
public class SelectionManager : MonoBehaviour {
    void FireChanged() {
        GameEvents.RaiseSelectionChanged(_selection); // Desacoplado
    }
}

// Consumer não precisa de referência
public class UnitListUI : MonoBehaviour {
    // ✅ Sem referência ao SelectionManager
    
    void OnEnable() {
        GameEvents.OnSelectionChanged += UpdateList; // Desacoplado
    }
    
    void OnDisable() {
        GameEvents.OnSelectionChanged -= UpdateList; // CRÍTICO: sempre desinscrever
    }
}
```

---

### 2.2 Arquitetura Centralizada

```
┌────────────────────────────────────────────────────────────┐
│                    GameEvents (static)                      │
├────────────────────────────────────────────────────────────┤
│  EVENTOS (31+)                    HELPERS (31+)            │
│  • OnTimeOfDay01                  • RaiseTimeOfDay()       │
│  • OnDayChanged                   • RaiseDayChanged()      │
│  • OnSelectionChanged             • RaiseSelectionChanged()│
│  • OnUnitClick                    • RaiseUnitClick()       │
│  • ... (27 outros eventos)        • ... (27 outros raises) │
└────────────────────────────────────────────────────────────┘
           ▲                                  │
           │ Subscribe                        │ Invoke
           │                                  ▼
  ┌────────┴────────┐              ┌─────────────────┐
  │   LISTENERS     │              │    EMITTERS     │
  ├─────────────────┤              ├─────────────────┤
  │ • UI Systems    │              │ • TimeManager   │
  │ • Audio         │              │ • SelectionMgr  │
  │ • Minimap       │              │ • Unit          │
  │ • IA/Triggers   │              │ • CameraCtrl    │
  │ • Analytics     │              │ • InputSystem   │
  └─────────────────┘              └─────────────────┘
```

---

### 2.3 Categorias de Eventos (Tabela Resumida)

| Categoria | Quantidade | Emissores Típicos | Listeners Típicos | Documentação |
|-----------|-----------|-------------------|-------------------|--------------|
| **Tempo** | 3 | `TimeManager` | UI (Clock), Luz, IA | [Seção 2.4.1](#241-eventos-de-tempo) |
| **Economia** | 1 | Sistemas de Coleta | UI, Analytics, Audio | [Seção 2.4.2](#242-eventos-de-economia) |
| **Diplomacia** | 2 | `FactionService` | UI, IA, Triggers | [Seção 2.4.3](#243-eventos-de-diplomacia) |
| **Câmera** | 5 | Combate, UI, Triggers | `RTSCameraController` | [Seção 2.4.4](#244-eventos-de-câmera) |
| **Unidades** | 4 | `Unit`, `UnitRegistry` | UI, Minimap, IA | [Seção 2.4.5](#245-eventos-de-unidades) |
| **Seleção** | 10 | `InputSelection`, `SelectionManager` | UI, Audio, Comandos | [Seção 2.4.6](#246-eventos-de-seleção) |
| **Input/Pointer** | 2 | `InputSelection` | Debug, Analytics | [Seção 2.4.7](#247-eventos-de-input-pointer) |
| **Minimap** | 2 | UI (Minimap), Comandos | Câmera, Audio | [Seção 2.4.4](#244-eventos-de-câmera) |
| **TOTAL** | **31** | - | - | - |

---

### 2.4 API Completa de Eventos

#### 2.4.1 Eventos de Tempo

**Responsabilidade:** Comunicar mudanças no relógio global do jogo.

| Evento | Assinatura | Quando Dispara | Emissor | Listeners Típicos |
|--------|-----------|----------------|---------|-------------------|
| `OnTimeOfDay01` | `Action<float>` | Continuamente (cada frame) | `TimeManager` | `DayNightLightController`, Shaders, Fog |
| `OnDayChanged` | `Action<int>` | Quando dia vira (0→1, 1→2...) | `TimeManager` | UI (Calendar), Save System, Analytics |
| `OnClockChanged` | `Action<int, int, int>` | Cada minuto do relógio | `TimeManager` | UI (Clock Display), Mission Timers |

**Exemplo de Uso (Listener):**

```csharp
// Sistema de UI que exibe relógio
public class ClockUI : MonoBehaviour {
    [SerializeField] TMP_Text clockText;
    
    void OnEnable() {
        GameEvents.OnClockChanged += UpdateClock;
    }
    
    void OnDisable() {
        GameEvents.OnClockChanged -= UpdateClock; // CRÍTICO
    }
    
    void UpdateClock(int day, int hour, int minute) {
        clockText.text = $"Dia {day} - {hour:00}:{minute:00}";
    }
}
```

**Exemplo de Uso (Emissor):**

```csharp
// TimeManager dispara evento
void Update() {
    // ... cálculo de tempo ...
    
    if (Minute != _lastMinute) {
        _lastMinute = Minute;
        GameEvents.RaiseClockChanged(DayCount, Hour, Minute); // ← Disparo
    }
}
```

---

#### 2.4.2 Eventos de Economia

| Evento | Assinatura | Quando Dispara | Emissor | Listeners Típicos |
|--------|-----------|----------------|---------|-------------------|
| `OnResourceGathered` | `Action<FactionId, ResourceType, int>` | Recurso coletado | Sistema de Coleta | UI, Analytics, Audio (som de moedas) |

**Parâmetros:**
- `FactionId who`: Facção que coletou o recurso
- `ResourceType type`: Tipo de recurso (Wood, Stone, Iron, etc.)
- `int amount`: Quantidade coletada

**Exemplo de Uso:**

```csharp
// Sistema de Coleta dispara evento
public class ResourceGatherer : MonoBehaviour {
    void OnGatherComplete(ResourceType type, int amount) {
        // Lógica de coleta...
        
        GameEvents.RaiseResourceGathered(
            myFaction, 
            type, 
            amount
        );
    }
}

// UI escuta e atualiza contador
public class ResourceUI : MonoBehaviour {
    Dictionary<ResourceType, int> _resources = new();
    
    void OnEnable() {
        GameEvents.OnResourceGathered += OnResourceGathered;
    }
    
    void OnDisable() {
        GameEvents.OnResourceGathered -= OnResourceGathered;
    }
    
    void OnResourceGathered(FactionId who, ResourceType type, int amount) {
        if (who != myFaction) return; // Filtrar apenas nossa facção
        
        _resources[type] += amount;
        UpdateResourceDisplay(type);
    }
}
```

---

#### 2.4.3 Eventos de Diplomacia

| Evento | Assinatura | Quando Dispara | Emissor | Listeners Típicos |
|--------|-----------|----------------|---------|-------------------|
| `OnReputationMatrixReady` | `Action` | Após inicialização da matriz | `FactionService.Init()` | IA, Triggers, UI |
| `OnReputationChanged` | `Action<FactionId, FactionId, float>` | Reputação A→B muda | `FactionService.SetReputation()` | UI, IA, Dialogue System |

**Exemplo de Uso:**

```csharp
// FactionService dispara eventos
public class FactionService : MonoBehaviour {
    public void Init() {
        // Inicializa matriz de reputação...
        GameEvents.RaiseReputationMatrixReady();
    }
    
    public void SetReputation(FactionId a, FactionId b, float value) {
        value = Mathf.Clamp(value, 0, 100);
        _rep[(a, b)] = value;
        GameEvents.RaiseReputationChanged(a, b, value); // ← Disparo
    }
}

// Sistema de IA escuta e reage
public class DiplomacyAI : MonoBehaviour {
    void OnEnable() {
        GameEvents.OnReputationChanged += OnReputationChanged;
    }
    
    void OnReputationChanged(FactionId a, FactionId b, float newValue) {
        if (a == myFaction) {
            // Reagir a mudança de reputação
            if (newValue < 30f) {
                PrepareForWar(b);
            } else if (newValue > 70f) {
                OfferAlliance(b);
            }
        }
    }
}
```

---

#### 2.4.4 Eventos de Câmera

**Responsabilidade:** Controlar movimentos e efeitos da câmera via eventos de gameplay.

| Evento | Assinatura | Quando Dispara | Emissor | Listener |
|--------|-----------|----------------|---------|----------|
| `OnCameraShake` | `Action<float, float, float>` | Impacto, explosão | Combate, Siege | `RTSCameraController` |
| `OnCameraFocus` | `Action<Vector3, bool, float>` | Foco em posição 3D | UI, Missões | `RTSCameraController` |
| `OnCameraFocusXZ` | `Action<Vector2, bool, float>` | Foco em posição XZ | Minimap, Pings | `RTSCameraController` |
| `OnCutsceneStart` | `Action<Transform, float, int>` | Iniciar cinemática | Dialogue, Triggers | `RTSCameraController` |
| `OnCutsceneEnd` | `Action` | Finalizar cinemática | Dialogue, Triggers | `RTSCameraController` |
| `OnMinimapPing` | `Action<Vector2>` | Ping no minimapa | UI (clique minimapa) | Câmera, Audio |
| `OnSelectionFocus` | `Action<Transform>` | Unidade selecionada | `Unit.SetSelected()` | Câmera (opcional) |

**Parâmetros Detalhados:**

**OnCameraShake:**
- `float amplitude`: Intensidade do shake (0.5-3.0)
- `float frequency`: Frequência da oscilação (1.0-5.0)
- `float duration`: Duração em segundos (0.1-1.0)

**OnCameraFocus / OnCameraFocusXZ:**
- `Vector3/Vector2 position`: Posição alvo
- `bool snap`: Se true, teleporta; se false, move suavemente
- `float duration`: Duração do movimento (0.0 = instantâneo)

**Exemplo de Uso:**

```csharp
// Sistema de Combate dispara shake
public class ExplosionEffect : MonoBehaviour {
    void Explode() {
        // VFX, som...
        
        GameEvents.RaiseCameraShake(
            amplitude: 2.0f,  // Shake forte
            frequency: 3.5f,  // Oscilação rápida
            duration: 0.4f    // Por 400ms
        );
    }
}

// RTSCameraController escuta e executa
public class RTSCameraController : MonoBehaviour {
    void OnEnable() {
        GameEvents.OnCameraShake += HandleCameraShake;
    }
    
    void HandleCameraShake(float amp, float freq, float dur) {
        StartCoroutine(CoShake(amp, freq, dur));
    }
}

// Minimap dispara foco
public class MinimapUI : MonoBehaviour {
    void OnMinimapClicked(Vector2 worldXZ) {
        GameEvents.RaiseMinimapPing(worldXZ);
        // Câmera move automaticamente
    }
}
```

---

#### 2.4.5 Eventos de Unidades

**Responsabilidade:** Comunicar mudanças de estado de unidades individuais.

| Evento | Assinatura | Quando Dispara | Emissor | Listeners Típicos |
|--------|-----------|----------------|---------|-------------------|
| `OnUnitSpawned` | `Action<Unit>` | Unidade criada/ativada | `UnitRegistry.Register()` | Minimap, Fog of War, IA |
| `OnUnitDespawned` | `Action<Unit>` | Unidade destruída/desativada | `UnitRegistry.Unregister()` | Minimap, Fog of War, IA |
| `OnUnitSelectionChanged` | `Action<Unit, bool>` | Unidade selecionada/desselecionada | `Unit.SetSelected()` | UI (painel de unidade), Audio |
| `OnUnitProgressChanged` | `Action<Unit>` | XP ou Level muda | `Unit.AddXp()` | UI (barra de XP), Audio (level up) |

**Exemplo de Uso:**

```csharp
// Unit dispara eventos automaticamente
public class Unit : MonoBehaviour {
    void OnEnable() {
        UnitRegistry.Register(this); // → Dispara OnUnitSpawned via Registry
    }
    
    void OnDisable() {
        UnitRegistry.Unregister(this); // → Dispara OnUnitDespawned via Registry
    }
    
    public void SetSelected(bool value) {
        if (IsSelected == value) return;
        IsSelected = value;
        
        // Highlight visual...
        
        GameEvents.RaiseUnitSelectionChanged(this, value); // ← Disparo direto
    }
    
    public void AddXp(float amount) {
        xp += amount;
        // Level up logic...
        
        GameEvents.RaiseUnitProgressChanged(this); // ← Disparo direto
    }
}

// Minimap escuta spawn/despawn
public class MinimapSystem : MonoBehaviour {
    Dictionary<Unit, GameObject> _icons = new();
    
    void OnEnable() {
        GameEvents.OnUnitSpawned += OnUnitSpawned;
        GameEvents.OnUnitDespawned += OnUnitDespawned;
    }
    
    void OnDisable() {
        GameEvents.OnUnitSpawned -= OnUnitSpawned;
        GameEvents.OnUnitDespawned -= OnUnitDespawned;
    }
    
    void OnUnitSpawned(Unit unit) {
        var icon = Instantiate(iconPrefab, minimapContainer);
        icon.GetComponent<Image>().color = GetFactionColor(unit.owner);
        _icons[unit] = icon;
    }
    
    void OnUnitDespawned(Unit unit) {
        if (_icons.TryGetValue(unit, out GameObject icon)) {
            Destroy(icon);
            _icons.Remove(unit);
        }
    }
}

// UI escuta progressão
public class UnitListItemUI : MonoBehaviour {
    Unit _unit;
    
    void OnEnable() {
        GameEvents.OnUnitProgressChanged += OnProgressChanged;
    }
    
    void OnDisable() {
        GameEvents.OnUnitProgressChanged -= OnProgressChanged;
    }
    
    void OnProgressChanged(Unit changedUnit) {
        // Filtrar apenas a unidade vinculada
        if (changedUnit == _unit) {
            UpdateXPBar(_unit.Xp01);
            UpdateLevelText(_unit.Level);
        }
    }
}
```

---

#### 2.4.6 Eventos de Seleção

**Responsabilidade:** Comunicar interações do jogador com input de seleção.

**NOTA:** Estes eventos foram adicionados na refatoração do Módulo Selection (Lote 4) para eliminar eventos locais.

| Evento | Assinatura | Quando Dispara | Emissor | Listeners Típicos |
|--------|-----------|----------------|---------|-------------------|
| `OnSelectionChanged` | `Action<IReadOnlyCollection<Unit>>` | Seleção muda (add/remove) | `SelectionManager` | UI (lista), Minimap, Audio |
| `OnUnitClick` | `Action<Unit, bool>` | Jogador clica unidade | `InputSelection` | `SelectionManager`, Debug |
| `OnUnitDoubleClick` | `Action<Unit>` | Jogador duplo-clica | `InputSelection` | `SelectionManager`, Debug |
| `OnGroundClick` | `Action<Vector3, bool>` | Jogador clica terreno | `InputSelection` | Comandos (futuro), Debug |
| `OnDragBegin` | `Action<Vector2>` | Inicia drag de seleção | `InputSelection` | `SelectionManager`, `DragRectRenderer` |
| `OnDragging` | `Action<Vector2>` | Durante drag | `InputSelection` | `DragRectRenderer` |
| `OnDragEnd` | `Action<Vector2>` | Finaliza drag | `InputSelection` | `SelectionManager`, `DragRectRenderer` |
| `OnPointerDown` | `Action<Vector2>` | LMB pressionado | `InputSelection` | Debug, Analytics |
| `OnPointerUp` | `Action<Vector2>` | LMB liberado | `InputSelection` | Debug, Analytics |

**Fluxo de Seleção via GameEvents:**

```
    Jogador clica unidade
           │
           ▼
   ┌───────────────┐
   │ InputSelection│ (detecta clique)
   └───────┬───────┘
           │ GameEvents.RaiseUnitClick(unit, ctrl)
           ▼
   ┌───────────────┐
   │  GameEvents   │ (propaga)
   └───────┬───────┘
           │
           ├─────────────────┬─────────────────┐
           ▼                 ▼                 ▼
   ┌──────────────┐  ┌─────────────┐  ┌──────────┐
   │ Selection    │  │ Debug       │  │ Analytics│
   │ Manager      │  │ Logger      │  │ System   │
   └───────┬──────┘  └─────────────┘  └──────────┘
           │ (processa seleção)
           │ GameEvents.RaiseSelectionChanged(units)
           ▼
   ┌───────────────┐
   │  GameEvents   │
   └───────┬───────┘
           │
           ├─────────────────┬─────────────────┐
           ▼                 ▼                 ▼
   ┌──────────────┐  ┌─────────────┐  ┌──────────┐
   │ UnitList UI  │  │ Minimap     │  │ Audio    │
   └──────────────┘  └─────────────┘  └──────────┘
```

**Exemplo de Uso:**

```csharp
// InputSelection dispara evento de clique
public class InputSelection : MonoBehaviour {
    void HandleClick(Vector2 screenPos) {
        if (picker.TryPickUnitAt(screenPos, out var unit)) {
            bool ctrl = IsCtrlPressed;
            GameEvents.RaiseUnitClick(unit, ctrl); // ← Disparo
        }
    }
}

// SelectionManager escuta e processa
public class SelectionManager : MonoBehaviour {
    void OnEnable() {
        GameEvents.OnUnitClick += HandleClickUnit;
    }
    
    void HandleClickUnit(Unit unit, bool ctrl) {
        if (ctrl) Toggle(unit);
        else { Clear(); Add(unit); }
        
        GameEvents.RaiseSelectionChanged(_selection); // ← Dispara outro evento
    }
}

// UI escuta seleção final
public class UnitListUI : MonoBehaviour {
    void OnEnable() {
        GameEvents.OnSelectionChanged += UpdateList;
    }
    
    void UpdateList(IReadOnlyCollection<Unit> units) {
        // Reconstruir lista de UI...
    }
}
```

---

#### 2.4.7 Eventos de Input/Pointer

**Responsabilidade:** Comunicar eventos brutos de input (usado para debug/analytics).

| Evento | Assinatura | Quando Dispara | Emissor | Listeners Típicos |
|--------|-----------|----------------|---------|-------------------|
| `OnPointerDown` | `Action<Vector2>` | LMB pressionado | `InputSelection` | Debug Systems, Analytics |
| `OnPointerUp` | `Action<Vector2>` | LMB liberado | `InputSelection` | Debug Systems, Analytics |

**Nota:** Estes eventos são de baixo nível e raramente usados diretamente. Prefira eventos de alto nível (`OnUnitClick`, `OnSelectionChanged`).

---

### 2.5 Raise Helpers (31 Métodos)

**Todos os eventos possuem um método helper `RaiseXXX()` para disparo:**

```csharp
// ==================== RAISE HELPERS ====================

// --- Tempo ---
public static void RaiseTimeOfDay(float t01)
    => OnTimeOfDay01?.Invoke(Mathf.Clamp01(t01));

public static void RaiseDayChanged(int day)
    => OnDayChanged?.Invoke(day);

public static void RaiseClockChanged(int day, int hour, int minute)
    => OnClockChanged?.Invoke(day, hour, minute);

// --- Economia ---
public static void RaiseResourceGathered(FactionId who, ResourceType type, int amount)
    => OnResourceGathered?.Invoke(who, type, amount);

// --- Diplomacia ---
public static void RaiseReputationMatrixReady()
    => OnReputationMatrixReady?.Invoke();

public static void RaiseReputationChanged(FactionId a, FactionId b, float v)
    => OnReputationChanged?.Invoke(a, b, v);

// --- Câmera ---
public static void RaiseCameraShake(float amplitude = 1.2f, float frequency = 2.0f, float duration = 0.25f)
    => OnCameraShake?.Invoke(amplitude, frequency, duration);

public static void RaiseCameraFocus(Vector3 worldPos, bool snap = false, float duration = 0.4f)
    => OnCameraFocus?.Invoke(worldPos, snap, duration);

public static void RaiseCameraFocusXZ(Vector2 worldXZ, bool snap = false, float duration = 0.4f)
    => OnCameraFocusXZ?.Invoke(worldXZ, snap, duration);

public static void RaiseCutsceneStart(Transform target, float fov = 50f, int priority = 100)
    => OnCutsceneStart?.Invoke(target, fov, priority);

public static void RaiseCutsceneEnd()
    => OnCutsceneEnd?.Invoke();

// --- Seleção / Minimap ---
public static void RaiseMinimapPing(Vector2 worldXZ)
    => OnMinimapPing?.Invoke(worldXZ);

public static void RaiseSelectionFocus(Transform target)
    => OnSelectionFocus?.Invoke(target);

// --- Unidades ---
public static void RaiseUnitSpawned(Unit unit)
    => OnUnitSpawned?.Invoke(unit);

public static void RaiseUnitDespawned(Unit unit)
    => OnUnitDespawned?.Invoke(unit);

public static void RaiseUnitSelectionChanged(Unit unit, bool isSelected)
    => OnUnitSelectionChanged?.Invoke(unit, isSelected);

public static void RaiseUnitProgressChanged(Unit unit)
    => OnUnitProgressChanged?.Invoke(unit);

// --- Seleção (Input) ---
public static void RaiseSelectionChanged(IReadOnlyCollection<Unit> selection)
    => OnSelectionChanged?.Invoke(selection);

public static void RaiseUnitClick(Unit unit, bool ctrlPressed)
    => OnUnitClick?.Invoke(unit, ctrlPressed);

public static void RaiseUnitDoubleClick(Unit unit)
    => OnUnitDoubleClick?.Invoke(unit);

public static void RaiseGroundClick(Vector3 worldPoint, bool ctrlPressed)
    => OnGroundClick?.Invoke(worldPoint, ctrlPressed);

public static void RaiseDragBegin(Vector2 screenPos)
    => OnDragBegin?.Invoke(screenPos);

public static void RaiseDragging(Vector2 screenPos)
    => OnDragging?.Invoke(screenPos);

public static void RaiseDragEnd(Vector2 screenPos)
    => OnDragEnd?.Invoke(screenPos);

public static void RaisePointerDown(Vector2 screenPos)
    => OnPointerDown?.Invoke(screenPos);

public static void RaisePointerUp(Vector2 screenPos)
    => OnPointerUp?.Invoke(screenPos);
```

**Convenção de Nomenclatura:**
- Evento: `OnXXX` (ex: `OnTimeOfDay01`)
- Helper: `RaiseXXX()` (ex: `RaiseTimeOfDay()`)

---

### 2.6 Padrões de Uso

#### **Padrão 1: Subscribe/Unsubscribe (CRÍTICO)**

**✅ SEMPRE faça subscribe em `OnEnable()` e unsubscribe em `OnDisable()`:**

```csharp
public class MySystem : MonoBehaviour {
    void OnEnable() {
        GameEvents.OnTimeOfDay01 += HandleTimeChange;
        GameEvents.OnSelectionChanged += HandleSelection;
    }
    
    void OnDisable() {
        // CRÍTICO: sempre desinscrever para evitar memory leaks
        GameEvents.OnTimeOfDay01 -= HandleTimeChange;
        GameEvents.OnSelectionChanged -= HandleSelection;
    }
    
    void HandleTimeChange(float t01) { /* ... */ }
    void HandleSelection(IReadOnlyCollection<Unit> units) { /* ... */ }
}
```

**❌ NÃO faça subscribe em `Start()` ou `Awake()`:**

```csharp
// ❌ ERRADO: Memory leak quando GameObject é destruído
void Start() {
    GameEvents.OnTimeOfDay01 += HandleTimeChange;
    // Faltou desinscrever no OnDisable!
}
```

---

#### **Padrão 2: Filtrar Eventos por Contexto**

**Nem todos os eventos são relevantes para todos os listeners. Filtre no handler:**

```csharp
public class PlayerUI : MonoBehaviour {
    [SerializeField] FactionId myFaction;
    
    void OnEnable() {
        GameEvents.OnResourceGathered += OnResourceGathered;
    }
    
    void OnResourceGathered(FactionId who, ResourceType type, int amount) {
        // Filtrar: só nos importamos com recursos da nossa facção
        if (who != myFaction) return;
        
        // Processar recurso...
        UpdateResourceDisplay(type, amount);
    }
}
```

---

#### **Padrão 3: Validação de Nulidade**

**Sempre valide parâmetros antes de processar:**

```csharp
void OnUnitSpawned(Unit unit) {
    if (unit == null) return; // Guard clause
    
    // Processar...
}
```

---

#### **Padrão 4: Evitar Lógica Pesada em Handlers**

**Handlers devem ser rápidos. Use flags ou filas para processamento posterior:**

```csharp
// ✅ BOM: Marca para processar no próximo Update()
bool _needsRefresh = false;

void OnEnable() {
    GameEvents.OnSelectionChanged += _ => _needsRefresh = true;
}

void Update() {
    if (_needsRefresh) {
        RefreshExpensiveUI(); // Processamento pesado aqui
        _needsRefresh = false;
    }
}

// ❌ RUIM: Lógica pesada no handler
void OnSelectionChanged(IReadOnlyCollection<Unit> units) {
    // Reconstruir toda UI aqui pode causar lag
    RebuildEntireUIFromScratch(); // ❌
}
```

---

### 2.7 Integração com Módulos

**Mapa de Dependências do GameEvents:**

```
                      ┌─────────────┐
                      │ GameEvents  │
                      │  (static)   │
                      └──────┬──────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌───────────────┐    ┌──────────────┐    ┌──────────────┐
│  EMISSORES    │    │  EVENT BUS   │    │  LISTENERS   │
├───────────────┤    ├──────────────┤    ├──────────────┤
│ TimeManager   │───▶│ OnTimeOfDay  │───▶│ Luz, UI      │
│ FactionService│───▶│ OnReputation │───▶│ IA, UI       │
│ Unit          │───▶│ OnUnitXXX    │───▶│ UI, Minimap  │
│ InputSelection│───▶│ OnUnitClick  │───▶│ Selection    │
│ SelectionMgr  │───▶│ OnSelection  │───▶│ UI, Audio    │
│ CombatSystem  │───▶│ OnCameraShake│───▶│ Camera       │
└───────────────┘    └──────────────┘    └──────────────┘
```

**Tabela de Integração por Módulo:**

| Módulo | Eventos Emitidos | Eventos Escutados | Arquivo Principal |
|--------|------------------|-------------------|-------------------|
| **Lote 1 - Variáveis Globais** | Tempo, Economia, Diplomacia | - | `TimeManager.cs`, `FactionService.cs` |
| **Lote 2 - Câmera** | - | OnCameraShake, OnCameraFocus, OnCutsceneXXX | `RTSCameraController.cs` |
| **Lote 3 - Unit** | OnUnitSpawned, OnUnitXXX | - | `Unit.cs`, `UnitRegistry.cs` |
| **Lote 4 - Selection** | OnSelectionChanged, OnUnitClick, OnDragXXX | OnUnitClick, OnGroundClick, OnDragXXX | `InputSelection.cs`, `SelectionManager.cs` |
| **UI System** | - | Todos (filtrado por relevância) | `UnitListUI.cs`, `ClockUI.cs`, etc. |

---

### 2.8 Boas Práticas (Subscribe/Unsubscribe)

#### **✅ Checklist de Boas Práticas:**

- [ ] **SEMPRE** subscribe em `OnEnable()` e unsubscribe em `OnDisable()`
- [ ] **NUNCA** subscribe em `Start()` ou `Awake()` sem desinscrever
- [ ] **FILTRAR** eventos irrelevantes no handler (ex: facção errada)
- [ ] **VALIDAR** nulidade de parâmetros antes de processar
- [ ] **EVITAR** lógica pesada em handlers (usar flags/filas)
- [ ] **USAR** lambda com cautela (dificulta unsubscribe)
- [ ] **DOCUMENTAR** quais eventos o sistema escuta (comentário na classe)

#### **Exemplo de Classe Bem Estruturada:**

```csharp
/// <summary>
/// Sistema de UI que exibe informações de tempo e seleção.
/// Escuta: OnClockChanged, OnSelectionChanged
/// </summary>
public class GameHUD : MonoBehaviour {
    [SerializeField] TMP_Text clockText;
    [SerializeField] TMP_Text selectionCountText;
    
    void OnEnable() {
        // ✅ Subscribe
        GameEvents.OnClockChanged += UpdateClock;
        GameEvents.OnSelectionChanged += UpdateSelectionCount;
    }
    
    void OnDisable() {
        // ✅ Unsubscribe (CRÍTICO)
        GameEvents.OnClockChanged -= UpdateClock;
        GameEvents.OnSelectionChanged -= UpdateSelectionCount;
    }
    
    void UpdateClock(int day, int hour, int minute) {
        clockText.text = $"D{day} {hour:00}:{minute:00}";
    }
    
    void UpdateSelectionCount(IReadOnlyCollection<Unit> units) {
        selectionCountText.text = $"Selecionadas: {units.Count}";
    }
}
```

---

## 3) ENUMS GLOBAIS

### 3.1 Visão Geral

**Responsabilidade:** Definir tipos enumerados usados transversalmente por todos os sistemas do jogo.

**Arquivo:** `Enums.cs`

### 3.2 Lista Completa de Enums

#### **FactionId**

```csharp
public enum FactionId { 
    Neutral = 0, 
    Player1 = 1, 
    Player2 = 2, 
    Player3 = 3, 
    Player4 = 4, 
    PvE = 3 
}
```

**Uso:** Identificar facção proprietária de unidades/construções, chave na matriz de reputação.

**Nota:** `PvE = 3` é alias para `Player3` (usado em missões de campanha).

---

#### **ResourceType**

```csharp
public enum ResourceType { 
    Wood, 
    Stone, 
    Iron, 
    Mithril, 
    Food, 
    Gold 
}
```

**Uso:** Tipo de recurso em eventos de economia, inventário, custos de construção/treino.

---

#### **DamageType**

```csharp
public enum DamageType { 
    Slashing,  // Espadas, machados
    Piercing,  // Flechas, lanças
    Blunt,     // Maças, martelos
    Siege,     // Catapultas, arietes
    Fire,      // Magia de fogo, flechas incendiárias
    Magic      // Magia arcana
}
```

**Uso:** Sistema de combate (futuro) para bônus/penalidades por tipo de armadura.

---

#### **TerrainType**

```csharp
public enum TerrainType { 
    Normal,    // Grama, terra
    Mud,       // Lama (penalidade de movimento)
    Snow,      // Neve (upkeep extra)
    Sand,      // Areia (penalidade de movimento)
    RoadDirt,  // Estrada de terra (bônus de movimento)
    RoadPaved  // Estrada pavimentada (bônus maior)
}
```

**Uso:** Sistema de pathfinding (futuro) para modificadores de velocidade.

**Relacionamento com GameConfig:**
- `mudSandMovePenalty`: Penalidade para Mud e Sand
- `snowExtraUpkeep`: Upkeep adicional em Snow

---

#### **UnitType**

```csharp
public enum UnitType { 
    Worker,       // Operário (coleta recursos)
    Warrior,      // Guerreiro corpo-a-corpo
    Spearman,     // Lanceiro (anti-cavalaria)
    Archer,       // Arqueiro (ranged)
    CavalryLight, // Cavalaria leve
    Ram,          // Aríete (siege)
    Catapult,     // Catapulta (siege ranged)
    Hero          // Herói (único, poderoso)
}
```

**Uso:** Classificação de unidades em `UnitDefinition`, filtros de UI, IA, balanceamento.

---

### 3.3 Exemplos de Uso

```csharp
// Selecionar cor da UI pela facção
var fdef = factionDb.Get(FactionId.Player1);
uiTeamBanner.color = fdef.color;

// Ajustar dano por tipo
if (attack.DamageType == DamageType.Siege) {
    ApplyBonusVsStructures();
}

// Filtrar unidades por tipo
var workers = UnitRegistry.All.Where(u => u.def.type == UnitType.Worker);

// Calcular penalidade de terreno
float moveSpeed = baseSpeed;
if (currentTerrain == TerrainType.Mud) {
    moveSpeed *= (1f - gameConfig.mudSandMovePenalty);
}
```

---

## 4) GAMECONFIG - CONFIGURAÇÃO GLOBAL

### 4.1 Visão Geral

**Tipo:** `ScriptableObject`  
**Responsabilidade:** Armazenar configurações globais do jogo (tempo, economia, penalidades, custos).

**Criação:** `Assets > Create > Game > Config`

### 4.2 Campos Públicos (Inspector)

#### **Tempo**

```csharp
[Header("Tempo")]
[Tooltip("Duração de 1 dia do jogo (em segundos reais). Demo: 600s = 10min")]
public float secondsPerDay = 600f;

[Range(0.1f, 0.9f)] 
public float dayFraction = 0.5f; // 50% dia / 50% noite
```

**Uso:**
- `secondsPerDay`: Controla velocidade do ciclo dia/noite
- `dayFraction`: Define quando noite começa (0.5 = metade do dia)

---

#### **Clima/Modificadores**

```csharp
[Header("Clima/Modificadores (demo - provisório)")]
[Tooltip("Penalidade de visão à noite (ex.: 0.2 = -20%)")]
[Range(0f, 1f)] public float nightVisionPenalty = 0.20f;

[Range(0f, 1f)] public float fogVisionPenalty = 0.10f;
[Range(0f, 1f)] public float mudSandMovePenalty = 0.20f;
[Range(0f, 1f)] public float snowExtraUpkeep = 0.15f;
```

**Uso:**
- `nightVisionPenalty`: Redução de visão à noite (Fog of War)
- `fogVisionPenalty`: Redução adicional em clima de neblina
- `mudSandMovePenalty`: Redução de velocidade em Mud/Sand
- `snowExtraUpkeep`: Custo extra de manutenção em Snow

---

#### **Economia - Baseline**

```csharp
[Header("Economia — baseline (demo)")]
[Tooltip("Em cenário ideal (depósito ~3 hex), um operário colhe 10 a cada 3 min.")]
public float baselineGatherMinTotal = 3;
public int baselineGatherPer3Min = 10;

[Tooltip("Capacidade do operário por viagem.")]
public int workerCarryCapacity = 10;
```

**Uso:**
- `baselineGatherMinTotal`: Tempo de coleta em cenário ideal (minutos)
- `baselineGatherPer3Min`: Quantidade coletada nesse tempo
- `workerCarryCapacity`: Máximo que operário carrega por viagem

---

#### **Unidades Especiais**

```csharp
[Header("Unidades especiais (demo)")]
public float merchantSpeedHexPerSec = 0.5f;
public int merchantCapacity = 20;
public int merchantCostGold = 30;
```

**Uso:** Configurações de unidades especiais (Mercadores, Heróis, etc.)

---

#### **Reparos**

```csharp
[Header("Reparos")]
public float structureRepairHpPerSec = 0.5f;
```

**Uso:** Taxa de reparo de estruturas (HP por segundo).

---

### 4.3 Helpers Públicos

```csharp
/// <summary>
/// Converte baseline de coleta para recursos por segundo.
/// Exemplo: 10 recursos / 180 segundos = ~0.055 recursos/segundo
/// </summary>
public float BaselinePerSecond => baselineGatherPer3Min / (baselineGatherMinTotal * 60);

/// <summary>
/// Alias para secondsPerDay (para consistência de nomenclatura).
/// </summary>
public float SecondsPerDay => secondsPerDay;
```

**Exemplo de Uso:**

```csharp
// Converter baseline para taxa por segundo
float perSec = gameConfig.BaselinePerSecond;
Debug.Log($"Taxa de coleta: {perSec:F3} recursos/segundo");

// Calcular recursos coletados em 1 minuto
float resourcesPer Minute = gameConfig.BaselinePerSecond * 60f;
```

---

### 4.4 Exemplo de Asset

**Arquivo:** `Assets/Settings/GameConfig.asset`

```
secondsPerDay: 600 (10 minutos de dia real = 1 dia de jogo)
dayFraction: 0.5 (metade do dia é luz, metade é noite)
nightVisionPenalty: 0.2 (-20% de visão à noite)
mudSandMovePenalty: 0.2 (-20% de velocidade em lama/areia)
baselineGatherPer3Min: 10 (10 recursos em 3 minutos)
workerCarryCapacity: 10 (10 recursos por viagem)
```

---

## 5) GAMECONTEXT - ORQUESTRADOR CENTRAL

### 5.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Ponto único de inicialização e orquestração. Injeta configurações e inicializa serviços na ordem correta.

**Localização na Cena:** GameObject `_GameContext` na raiz da hierarquia.

### 5.2 Campos Públicos (Inspector)

```csharp
public GameConfig config;              // Referência ao ScriptableObject de config
public FactionDatabase factions;       // Banco de dados de facções
public FactionService factionService;  // Serviço de reputação (runtime)
public TimeManager timeManager;        // Gerenciador de tempo
```

**Screenshot do Inspector:**

![GameContext Inspector](reference://1761266286926_image.png)

**Configuração Típica:**
- `Config`: Arraste o asset `GameConfig.asset`
- `Factions`: Arraste o asset `FactionDatabase.asset`
- `Faction Service`: Arraste o componente `FactionService` (mesmo GameObject ou filho)
- `Time Manager`: Arraste o componente `TimeManager` (mesmo GameObject ou filho)

---

### 5.3 Método `Awake()` (Inicialização)

```csharp
void Awake() {
    // 1. Inicializar matriz de reputação de facções
    if (factionService != null) 
        factionService.Init();
    
    // 2. Injetar config no TimeManager
    if (timeManager != null) 
        timeManager.config = config;
}
```

**Ordem de Inicialização:**
1. **FactionService.Init()**: Cria matriz de reputação baseada em `FactionDatabase`
2. **TimeManager.config**: Injeta configurações de tempo

**Nota:** `Awake()` é executado **antes** de qualquer `Start()`, garantindo que serviços estejam prontos quando outros sistemas iniciarem.

---

### 5.4 Fluxo de Inicialização Detalhado

```
Unity Scene Load
       │
       ▼
GameContext.Awake()
       │
       ├──▶ 1. FactionService.Init()
       │         │
       │         ├──▶ Carrega FactionDatabase
       │         ├──▶ Cria matriz A→B (todas combinações)
       │         ├──▶ Popula com initialReputation
       │         └──▶ GameEvents.RaiseReputationMatrixReady()
       │
       └──▶ 2. TimeManager.config = config
                 │
                 └──▶ TimeManager agora tem acesso a:
                      • secondsPerDay
                      • dayFraction
                      • Outros parâmetros de tempo
       │
       ▼
Todos os outros scripts executam Start()
       │
       ▼
Jogo começa (Update loop)
```

---

### 5.5 Exemplo de Uso

**Setup na Cena:**

1. Criar GameObject vazio: `_GameContext`
2. Adicionar componente `GameContext`
3. Adicionar componentes filhos:
   - `FactionService`
   - `TimeManager`
4. No Inspector de `GameContext`:
   - Arrastar `GameConfig.asset` para campo `Config`
   - Arrastar `FactionDatabase.asset` para campo `Factions`
   - Arrastar componentes filhos para campos respectivos

---

## 6) TIMEMANAGER - SISTEMA DE TEMPO

### 6.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Avança relógio global, converte fração do dia em HH:MM, emite eventos de tempo via GameEvents.

### 6.2 Campos Públicos (Inspector)

```csharp
public GameConfig config; // Injetado por GameContext
[Range(0f, 1f)] public float startTime01 = 0.25f; // 0 = amanhecer, 0.5 = pôr-do-sol
```

**Configuração:**
- `config`: **Não atribuir manualmente**. Injetado por `GameContext.Awake()`
- `startTime01`: Horário inicial do jogo (0 = meia-noite, 0.25 = 6h, 0.5 = meio-dia)

---

### 6.3 Propriedades Públicas (Read-Only)

```csharp
[field: SerializeField]
public float Time01 { get; private set; } // Fração do dia (0..1)

[field: SerializeField]
public int DayCount { get; private set; } // Dia atual (0, 1, 2...)

[field: SerializeField]
public int Hour { get; private set; } // Hora (0-23)

[field: SerializeField]
public int Minute { get; private set; } // Minuto (0-59)
```

**Nota:** `[field: SerializeField]` torna propriedades visíveis no Inspector para debug.

---

### 6.4 Métodos Públicos

```csharp
/// <summary>
/// Verifica se é noite baseado em Time01 e config.dayFraction.
/// </summary>
/// <returns>True se Time01 >= config.dayFraction</returns>
public bool IsNight()
```

**Exemplo:**
```csharp
if (timeManager.IsNight()) {
    float visionPenalty = gameConfig.nightVisionPenalty;
    ApplyVisionReduction(visionPenalty);
}
```

---

### 6.5 Lógica Interna (Update)

**Algoritmo:**

1. **Calcular Delta de Tempo:**
   ```csharp
   float delta01 = Time.deltaTime / config.SecondsPerDay;
   ```
   - Exemplo: Se `SecondsPerDay = 600s` e `Time.deltaTime = 0.016s` (60 FPS):
   - `delta01 = 0.016 / 600 = 0.0000266` (0.00266% do dia por frame)

2. **Avançar Relógio:**
   ```csharp
   float old = Time01;
   Time01 = Mathf.Repeat(Time01 + delta01, 1f);
   ```
   - `Mathf.Repeat()` faz loop automático (0.999 + 0.002 = 0.001)

3. **Detectar Virada de Dia:**
   ```csharp
   if (Time01 < old) { // Voltou para 0
       DayCount++;
       GameEvents.RaiseDayChanged(DayCount);
   }
   ```

4. **Converter para HH:MM:**
   ```csharp
   int totalMinutes = Mathf.FloorToInt(Time01 * 1440f); // 24h * 60min
   Hour = (totalMinutes / 60) % 24;
   Minute = totalMinutes % 60;
   ```

5. **Disparar Eventos:**
   ```csharp
   if (Minute != _lastMinute) {
       _lastMinute = Minute;
       GameEvents.RaiseClockChanged(DayCount, Hour, Minute); // Cada minuto
   }
   GameEvents.RaiseTimeOfDay(Time01); // Todo frame
   ```

---

### 6.6 Exemplo de Conversão Tempo

| Time01 | Hora (HH:MM) | Período |
|--------|-------------|---------|
| 0.00 | 00:00 | Meia-noite |
| 0.25 | 06:00 | Amanhecer |
| 0.50 | 12:00 | Meio-dia |
| 0.75 | 18:00 | Entardecer |
| 1.00 | 00:00 (próximo dia) | Meia-noite |

**Exemplo de Velocidade:**
- `secondsPerDay = 600` (10 minutos reais = 1 dia de jogo)
- 1 hora de jogo = 600s / 24h = **25 segundos reais**
- 1 minuto de jogo = 25s / 60min = **0.416 segundos reais**

---

## 7) MÓDULO FACTIONS

### 7.1 Visão Geral

**Responsabilidade:** Gerenciar facções (definições, banco de dados, reputação em runtime).

**Componentes:**
1. `FactionDefinition` (ScriptableObject) - Metadados de uma facção
2. `FactionDatabase` (ScriptableObject) - Coleção de definições
3. `FactionService` (MonoBehaviour) - Matriz de reputação em runtime

---

### 7.2 FactionDefinition

**Tipo:** `ScriptableObject`  
**Criação:** `Assets > Create > Game > Faction`

#### **Campos:**

```csharp
public FactionId id;                          // Identificador único
public string displayName = "Reino";          // Nome para UI
public Color color = Color.white;             // Cor da facção (UI, minimapa)
public Sprite banner;                         // Bandeira/estandarte
[Range(0, 100)] 
public float initialReputation = 50f;         // Reputação inicial (neutro)
```

#### **Exemplo de Uso:**

```csharp
// Obter definição de uma facção
FactionDefinition player1 = factionDatabase.Get(FactionId.Player1);

// Usar em UI
teamBanner.sprite = player1.banner;
teamBanner.color = player1.color;
nameText.text = player1.displayName;
```

---

### 7.3 FactionDatabase

**Tipo:** `ScriptableObject`  
**Criação:** `Assets > Create > Game > Faction Database`

#### **Campos:**

```csharp
public List<FactionDefinition> factions = new();
```

#### **Métodos:**

```csharp
/// <summary>
/// Busca definição por ID.
/// </summary>
/// <returns>FactionDefinition ou null se não encontrado</returns>
public FactionDefinition Get(FactionId id) => factions.Find(f => f.id == id);
```

#### **Setup Típico:**

1. Criar `FactionDatabase.asset`
2. Adicionar definições à lista:
   - `Faction_Player1.asset` (id: Player1, cor: Azul)
   - `Faction_Player2.asset` (id: Player2, cor: Vermelho)
   - `Faction_PvE.asset` (id: PvE, cor: Amarelo)
   - etc.

---

### 7.4 FactionService

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Manter matriz de reputação A→B em runtime, emitir eventos de mudanças.

#### **Campos:**

```csharp
[SerializeField] private FactionDatabase database; // Referência ao ScriptableObject
```

**Nota:** Campo privado, injetado via Inspector.

#### **Estrutura Interna:**

```csharp
// Matriz de reputação: (A, B) → valor (0..100)
private readonly Dictionary<(FactionId, FactionId), float> _rep = new();
```

**Exemplo de Matriz:**

| A ↓ B → | Player1 | Player2 | PvE |
|---------|---------|---------|-----|
| **Player1** | 100 | 50 | 50 |
| **Player2** | 50 | 100 | 30 |
| **PvE** | 50 | 30 | 100 |

**Interpretação:**
- Player1 → Player2: 50 (neutro)
- Player2 → PvE: 30 (hostil)
- Diagonal sempre 100 (facção consigo mesma)

---

#### **Métodos Públicos:**

```csharp
/// <summary>
/// Inicializa matriz de reputação. Chamado por GameContext.Awake().
/// </summary>
public void Init()

/// <summary>
/// Obtém reputação de A em relação a B (0..100).
/// </summary>
public float GetReputation(FactionId a, FactionId b)

/// <summary>
/// Define reputação de A em relação a B (clamp 0..100).
/// Dispara GameEvents.OnReputationChanged.
/// </summary>
public void SetReputation(FactionId a, FactionId b, float value)

/// <summary>
/// Aplica delta de reputação (incremento/decremento).
/// </summary>
public void DeltaReputation(FactionId a, FactionId b, float delta)
```

---

#### **Implementação de Init():**

```csharp
public void Init() {
    foreach (var fa in database.factions) {
        foreach (var fb in database.factions) {
            var key = (fa.id, fb.id);
            if (!_rep.ContainsKey(key))
                _rep[key] = fa.initialReputation;
        }
    }
    GameEvents.RaiseReputationMatrixReady();
}
```

**Fluxo:**
1. Loop duplo: para cada facção A, para cada facção B
2. Cria entrada `(A, B)` na matriz
3. Inicializa com `A.initialReputation`
4. Dispara evento `OnReputationMatrixReady`

---

#### **Exemplo de Uso:**

```csharp
// Obter reputação
float rep = factionService.GetReputation(FactionId.Player1, FactionId.PvE);
if (rep < 30f) {
    Debug.Log("Player1 é hostil a PvE!");
}

// Modificar reputação (missão concluída)
void OnQuestCompleted(FactionId questGiver) {
    factionService.DeltaReputation(
        myFaction, 
        questGiver, 
        +15f // Melhora reputação em 15 pontos
    );
    // GameEvents.OnReputationChanged será disparado automaticamente
}

// Escutar mudanças de reputação
void OnEnable() {
    GameEvents.OnReputationChanged += OnReputationChanged;
}

void OnReputationChanged(FactionId a, FactionId b, float newValue) {
    if (a == myFaction) {
        Debug.Log($"Nossa reputação com {b} mudou para {newValue}");
        UpdateDiplomacyUI();
    }
}
```

---

## 8) PLAYERCONTROLLER

### 8.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Definir facção do jogador local e manter referência à câmera principal.

**Nota:** Classe simples, mas essencial para filtros de seleção e UI.

### 8.2 Campos Públicos

```csharp
[Header("Quem sou eu")]
public FactionId myFaction = FactionId.Player1;

[Header("Referências")]
public Camera mainCamera;
```

**Uso:**
- `myFaction`: Usado por `SelectionManager` (filtro `onlyOwnUnits`)
- `mainCamera`: Usada por sistemas de input/raycasting

### 8.3 Método `Reset()`

```csharp
private void Reset() {
    mainCamera = Camera.main; // Auto-atribui no Inspector
}
```

**Funcionalidade:** Quando componente é adicionado no Inspector, busca automaticamente `Camera.main`.

---

## 9) DAYNIGHTLIGHTCONTROLLER

### 9.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Requer:** `Light` (Directional Light)  
**Responsabilidade:** Atualizar cor/intensidade da luz direcional baseado em ciclo dia/noite.

### 9.2 Campos Públicos

```csharp
public Gradient colorOverDay; // Gradiente de cores ao longo do dia
public AnimationCurve intensityOverDay; // Curva de intensidade
```

**Configuração Padrão:**

**colorOverDay (Gradient):**
| Time | Color (RGB) | Momento |
|------|------------|---------|
| 0.00 | (0.85, 0.75, 0.55) | Amanhecer (laranja suave) |
| 0.25 | (1.00, 0.95, 0.85) | Dia (branco quente) |
| 0.50 | (1.00, 0.85, 0.60) | Pôr-do-sol (laranja intenso) |
| 0.75 | (0.20, 0.25, 0.40) | Crepúsculo (azul escuro) |
| 1.00 | (0.10, 0.12, 0.20) | Noite (azul muito escuro) |

**intensityOverDay (AnimationCurve):**
- Keyframe 0.00: 0.15 (amanhecer suave)
- Keyframe 0.25: 1.0 (dia pleno)
- Keyframe 1.00: 0.15 (noite suave)

---

### 9.3 Integração com GameEvents

```csharp
void OnEnable() {
    _light = GetComponent<Light>();
    GameEvents.OnTimeOfDay01 += Apply; // ← Escuta evento de tempo
}

void OnDisable() {
    GameEvents.OnTimeOfDay01 -= Apply; // ← Desinscreve
}

private void Apply(float t01) {
    _light.color = colorOverDay.Evaluate(t01);
    _light.intensity = Mathf.Clamp01(intensityOverDay.Evaluate(t01));
    
    // Rotação simples do sol (opcional)
    transform.rotation = Quaternion.Euler(
        new Vector3((t01 * 360f) - 90f, 170f, 0f)
    );
}
```

**Resultado:**
- Cor da luz muda suavemente ao longo do dia
- Intensidade varia (mais forte ao meio-dia, fraca à noite)
- Sol "gira" no céu (opcional)

---

## 10) FLUXO DE INICIALIZAÇÃO

### 10.1 Diagrama Completo

```
┌──────────────────────────────────────────────────────────┐
│               Unity Scene Load                            │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │  GameContext.Awake()  │ (Primeira execução)
         └───────┬───────────────┘
                 │
      ┌──────────┼──────────┐
      │                     │
      ▼                     ▼
┌──────────────┐    ┌─────────────────┐
│FactionService│    │   TimeManager   │
│  .Init()     │    │ .config = config│
└──────┬───────┘    └─────────────────┘
       │
       ├──▶ 1. Carrega FactionDatabase
       ├──▶ 2. Cria matriz A→B
       ├──▶ 3. Popula com initialReputation
       └──▶ 4. GameEvents.RaiseReputationMatrixReady()
                             │
                             ▼
                 ┌──────────────────────┐
                 │ Sistemas escutam via │
                 │    GameEvents        │
                 └──────────┬───────────┘
                            │
                            ▼
         ┌──────────────────────────────────┐
         │ Todos os scripts executam Start()│
         └──────────────────┬───────────────┘
                            │
                            ▼
              ┌────────────────────────┐
              │ TimeManager.Awake()    │
              │ • Time01 = startTime01 │
              │ • RaiseTimeOfDay(Time01)│
              └────────┬───────────────┘
                       │
                       ▼
         ┌────────────────────────────────┐
         │ DayNightLightController escuta │
         │ OnTimeOfDay01 → Aplica cor/luz │
         └────────────────────────────────┘
                       │
                       ▼
              ┌────────────────┐
              │  Update Loop   │
              │ (Jogo começa)  │
              └────────────────┘
```

---

### 10.2 Ordem de Execução Crítica

**Unity garante que `Awake()` é executado antes de `Start()`:**

1. **Awake()** de TODOS os GameObjects (ordem indeterminada)
   - `GameContext.Awake()` executa primeiro (tipicamente)
   - `TimeManager.Awake()` pode executar antes ou depois

2. **OnEnable()** de TODOS os GameObjects
   - Listeners se inscrevem em `GameEvents`

3. **Start()** de TODOS os GameObjects (ordem indeterminada)
   - Sistemas já têm config injetada

4. **Update() Loop** começa
   - `TimeManager.Update()` avança relógio
   - Eventos são disparados continuamente

---

### 10.3 Screenshot do Setup

**GameContext Inspector:**

![GameContext Inspector](reference://1761266286926_image.png)

**Componentes Visíveis:**
1. **GameContext (Script)**
   - Config: `GameConfig (Game Config)`
   - Factions: `FactionDatabase (Faction Database)`
   - Faction Service: `GameContext (Faction Service)`
   - Time Manager: `GameContext (Time Manager)`

2. **TimeManager (Script)**
   - Config: `None (Game Config)` ← Injetado por GameContext
   - Start Time 01: `0.25` (6h da manhã)
   - Time01: `0` (atualizado em runtime)
   - Day Count: `0`
   - Hour: `0`
   - Minute: `0`

3. **FactionService (Script)**
   - Database: `FactionDatabase (Faction Database)`

---

## 11) INTEGRAÇÃO ENTRE MÓDULOS

### 11.1 Mapa de Dependências

```
                    ┌──────────────┐
                    │  GameEvents  │
                    │ (Event Bus)  │
                    └──────┬───────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌───────────────┐  ┌──────────────┐  ┌──────────────┐
│  Lote 1       │  │  Lote 2      │  │  Lote 3      │
│  (Foundation) │  │  (Camera)    │  │  (Unit)      │
├───────────────┤  ├──────────────┤  ├──────────────┤
│ • Enums       │  │ • RTS Camera │  │ • Unit       │
│ • GameConfig  │  │ • CameraMath │  │ • UnitDef    │
│ • TimeManager │  │ • Profiles   │  │ • Registry   │
│ • Factions    │  └──────┬───────┘  └──────┬───────┘
└───────┬───────┘         │                 │
        │                 │                 │
        │                 └─────────┬───────┘
        │                           │
        ▼                           ▼
┌──────────────────────────────────────────────┐
│             Lote 4 (Selection)               │
├──────────────────────────────────────────────┤
│ • InputSelection                             │
│ • SelectionManager                           │
│ • WorldPicker                                │
└────────────────┬─────────────────────────────┘
                 │
                 ▼
         ┌───────────────┐
         │  UI System    │
         │  (Consumer)   │
         └───────────────┘
```

---

### 11.2 Tabela de Dependências por Módulo

| Módulo Consumidor | Depende De (Lote 1) | Uso |
|-------------------|---------------------|-----|
| **Lote 2 - Câmera** | GameEvents | Escuta OnCameraShake, OnCameraFocus, etc. |
| **Lote 3 - Unit** | Enums (FactionId, UnitType), GameEvents | Emite OnUnitSpawned, OnUnitProgressChanged |
| **Lote 4 - Selection** | Unit, Enums (FactionId), GameEvents, PlayerController | Filtra por facção, emite eventos de seleção |
| **UI System** | GameEvents, Enums, UnitRegistry | Escuta OnClockChanged, OnSelectionChanged, etc. |
| **IA System** (futuro) | GameEvents (OnReputationChanged), Enums | Reage a diplomacia, classifica unidades |

---

### 11.3 Fluxo de Comunicação via GameEvents

**Exemplo: Seleção de Unidade → Atualização de UI**

```
Jogador clica unidade
        │
        ▼
InputSelection detecta clique
        │
        │ GameEvents.RaiseUnitClick(unit, ctrl)
        ▼
┌──────────────┐
│  GameEvents  │ (propaga)
└──────┬───────┘
       │
       ├─────────────────┐
       ▼                 ▼
SelectionManager    DebugLogger
 (processa)           (log)
       │
       │ GameEvents.RaiseSelectionChanged(units)
       ▼
┌──────────────┐
│  GameEvents  │ (propaga)
└──────┬───────┘
       │
       ├──────────┬──────────┬──────────┐
       ▼          ▼          ▼          ▼
  UnitListUI  Minimap  AudioManager  IA
  (atualiza)  (marca)  (som "beep")  (analisa)
```

**Desacoplamento:**
- `InputSelection` não conhece `SelectionManager`
- `SelectionManager` não conhece `UnitListUI`
- Fácil adicionar/remover listeners sem modificar emissores

---

## 12) TABELA DE RELACIONAMENTOS COMPLETA

### 12.1 Classes do Lote 1

| Classe | Tipo | Depende De | Dependentes | Eventos (Emit) | Eventos (Listen) |
|--------|------|-----------|-------------|----------------|------------------|
| **GameEvents** | static | - | TODOS | - | - |
| **Enums** | enum | - | TODOS | - | - |
| **GameConfig** | SO | - | TimeManager, GameContext | - | - |
| **GameContext** | MB | GameConfig, FactionDatabase, FactionService, TimeManager | - | - | - |
| **TimeManager** | MB | GameConfig | DayNightLightController, UI | OnTimeOfDay01, OnDayChanged, OnClockChanged | - |
| **FactionDefinition** | SO | Enums (FactionId) | FactionDatabase | - | - |
| **FactionDatabase** | SO | FactionDefinition | FactionService | - | - |
| **FactionService** | MB | FactionDatabase, Enums | IA, UI | OnReputationMatrixReady, OnReputationChanged | - |
| **PlayerController** | MB | Enums (FactionId) | SelectionManager, UnitQueries | - | - |
| **DayNightLightController** | MB | - | - | - | OnTimeOfDay01 |

**Legenda:**
- **SO:** ScriptableObject
- **MB:** MonoBehaviour
- **Emit:** Eventos que a classe dispara
- **Listen:** Eventos que a classe escuta

---

### 12.2 Integrações com Outros Lotes

| Lote | Usa de Lote 1 | Fornece para Lote 1 |
|------|---------------|---------------------|
| **Lote 2 - Câmera** | GameEvents (listen), GameConfig (bounds) | Eventos de câmera (OnCameraShake, etc.) |
| **Lote 3 - Unit** | Enums (FactionId, UnitType), GameEvents (emit) | Eventos de unidade (OnUnitSpawned, etc.) |
| **Lote 4 - Selection** | GameEvents (emit/listen), PlayerController, Enums | Eventos de seleção (OnSelectionChanged, etc.) |
| **UI System** | GameEvents (listen), Enums (todos) | - |

---

## 13) CHANGELOG E MIGRAÇÕES

### 13.1 Mudanças da v2.0 → v3.0

#### **✅ ADICIONADO:**

1. **GameEvents - Seção Separada e Expandida:**
   - Documentação completa de 31+ eventos (vs 12 na v2.0)
   - 10 novos eventos de Selection adicionados
   - Helpers `RaiseXXX()` para todos os eventos
   - Exemplos de uso detalhados para cada categoria

2. **Refatoração de Unit.cs:**
   - Eventos locais removidos (`OnSelectionChanged`, `OnProgressChanged`)
   - Migrado para `GameEvents.RaiseUnitSelectionChanged()` e `GameEvents.RaiseUnitProgressChanged()`
   - Método `TestList()` movido para `#if UNITY_EDITOR`

3. **Refatoração de SelectionManager.cs:**
   - Evento local removido (`OnSelectionChanged`)
   - Migrado para `GameEvents.RaiseSelectionChanged()`
   - Handlers agora escutam eventos via `GameEvents` ao invés de `InputSelection` diretamente

4. **Refatoração de RTSCameraController.cs:**
   - Handlers de eventos de câmera implementados
   - Integração completa com `GameEvents` (OnCameraShake, OnCameraFocus, etc.)

5. **Screenshot de GameContext:**
   - Inspector completo adicionado para referência visual

#### **🔄 MODIFICADO:**

1. **Estrutura da Documentação:**
   - GameEvents agora é seção 2 (antes estava diluído na seção 3)
   - "Core dos Cores" destacado na hierarquia

2. **Exemplos de Código:**
   - Todos os exemplos atualizados para usar `GameEvents`
   - Padrões de Subscribe/Unsubscribe enfatizados

#### **❌ REMOVIDO:**

1. **Eventos Locais (Obsoletos):**
   - `Unit.OnSelectionChanged` → Use `GameEvents.OnUnitSelectionChanged`
   - `Unit.OnProgressChanged` → Use `GameEvents.OnUnitProgressChanged`
   - `SelectionManager.OnSelectionChanged` → Use `GameEvents.OnSelectionChanged`

---

### 13.2 Guia de Migração (v2.0 → v3.0)

#### **Para Código que Usava Eventos Locais:**

**Antes (v2.0 - ❌ Obsoleto):**
```csharp
public class UnitListItemUI : MonoBehaviour {
    Unit _unit;
    
    void Bind(Unit unit) {
        if (_unit != null) {
            _unit.OnProgressChanged -= OnProgressChanged; // ❌ Evento local
        }
        _unit = unit;
        _unit.OnProgressChanged += OnProgressChanged; // ❌
    }
}
```

**Depois (v3.0 - ✅ Atual):**
```csharp
public class UnitListItemUI : MonoBehaviour {
    Unit _unit;
    
    void OnEnable() {
        GameEvents.OnUnitProgressChanged += OnProgressChanged; // ✅ GameEvents
    }
    
    void OnDisable() {
        GameEvents.OnUnitProgressChanged -= OnProgressChanged; // ✅
    }
    
    void OnProgressChanged(Unit changedUnit) {
        if (changedUnit == _unit) { // Filtrar
            Refresh();
        }
    }
}
```

---

#### **Para Código que Usava SelectionManager Diretamente:**

**Antes (v2.0 - ❌ Obsoleto):**
```csharp
public class UnitListUI : MonoBehaviour {
    [SerializeField] SelectionManager selectionManager; // ❌ Referência direta
    
    void OnEnable() {
        selectionManager.OnSelectionChanged += UpdateList; // ❌ Evento local
    }
}
```

**Depois (v3.0 - ✅ Atual):**
```csharp
public class UnitListUI : MonoBehaviour {
    // ✅ Sem referência ao SelectionManager
    
    void OnEnable() {
        GameEvents.OnSelectionChanged += UpdateList; // ✅ GameEvents
    }
    
    void OnDisable() {
        GameEvents.OnSelectionChanged -= UpdateList; // ✅
    }
}
```

---

### 13.3 Checklist de Migração

Use esta checklist para atualizar seu código:

- [ ] **Buscar eventos locais:** Procure por `.OnSelectionChanged`, `.OnProgressChanged` no projeto
- [ ] **Substituir por GameEvents:** Troque por `GameEvents.OnXXX`
- [ ] **Mover subscribe para OnEnable():** Se estava em `Start()`, mova para `OnEnable()`
- [ ] **Adicionar unsubscribe em OnDisable():** CRÍTICO para evitar memory leaks
- [ ] **Adicionar filtros em handlers:** Handlers de GameEvents recebem parâmetros, filtre o que é relevante
- [ ] **Remover referências diretas:** Se tinha `[SerializeField] SelectionManager`, pode remover
- [ ] **Testar:** Verifique que eventos ainda funcionam após migração

---

## 14) REFERÊNCIAS RÁPIDAS

### 14.1 Eventos por Categoria (Resumo)

| Categoria | Eventos | Raise Helpers |
|-----------|---------|---------------|
| **Tempo** | OnTimeOfDay01, OnDayChanged, OnClockChanged | RaiseTimeOfDay, RaiseDayChanged, RaiseClockChanged |
| **Economia** | OnResourceGathered | RaiseResourceGathered |
| **Diplomacia** | OnReputationMatrixReady, OnReputationChanged | RaiseReputationMatrixReady, RaiseReputationChanged |
| **Câmera** | OnCameraShake, OnCameraFocus, OnCameraFocusXZ, OnCutsceneStart, OnCutsceneEnd | RaiseCameraShake, RaiseCameraFocus, RaiseCameraFocusXZ, RaiseCutsceneStart, RaiseCutsceneEnd |
| **Unidades** | OnUnitSpawned, OnUnitDespawned, OnUnitSelectionChanged, OnUnitProgressChanged | RaiseUnitSpawned, RaiseUnitDespawned, RaiseUnitSelectionChanged, RaiseUnitProgressChanged |
| **Seleção** | OnSelectionChanged, OnUnitClick, OnUnitDoubleClick, OnGroundClick, OnDragBegin, OnDragging, OnDragEnd | RaiseSelectionChanged, RaiseUnitClick, RaiseUnitDoubleClick, RaiseGroundClick, RaiseDragBegin, RaiseDragging, RaiseDragEnd |
| **Minimap** | OnMinimapPing, OnSelectionFocus | RaiseMinimapPing, RaiseSelectionFocus |
| **Input** | OnPointerDown, OnPointerUp | RaisePointerDown, RaisePointerUp |

---

### 14.2 Estrutura de Arquivos

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Enums.cs
│   │   ├── GameConfig.cs
│   │   ├── GameEvents.cs ← "Core dos Cores"
│   │   ├── GameContext.cs
│   │   ├── TimeManager.cs
│   │   ├── DayNightLightController.cs
│   │   └── PlayerController.cs
│   │
│   └── Factions/
│       ├── FactionDefinition.cs
│       ├── FactionDatabase.cs
│       └── FactionService.cs
│
└── Settings/
    ├── GameConfig.asset
    ├── FactionDatabase.asset
    └── Factions/
        ├── Faction_Player1.asset
        ├── Faction_Player2.asset
        └── Faction_PvE.asset
```

---

### 14.3 Hierarquia de Cena Típica

```
SampleScene
├── _GameContext
│   ├── GameContext (Script)
│   ├── TimeManager (Script)
│   └── FactionService (Script)
│
├── Lights
│   └── Directional Light
│       └── DayNightLightController (Script)
│
├── Camera
│   └── RTS Camera Rig
│
├── Player
│   └── PlayerController (Script)
│
└── Units
    ├── Worker (1)
    └── Worker (2)
```

---

## 15) CONCLUSÃO

### 15.1 Resumo do Lote 1

O **Lote 1 - Variáveis Globais & Módulo Factions** estabelece a **fundação arquitetural** do Medieval Thrones:

✅ **GameEvents**: Event bus centralizado com 31+ eventos para comunicação desacoplada  
✅ **Enums Globais**: Tipagem forte para Facções, Recursos, Danos, Terrenos, Unidades  
✅ **GameConfig**: Configurações globais centralizadas em ScriptableObject  
✅ **TimeManager**: Sistema de tempo com ciclo dia/noite e relógio HH:MM  
✅ **Módulo Factions**: Definições, banco de dados e matriz de reputação em runtime  
✅ **GameContext**: Orquestrador central que inicializa tudo na ordem correta  

### 15.2 Qualidade da Arquitetura

**Pontos Fortes:**
- ✅ Desacoplamento total via GameEvents
- ✅ Testabilidade (lógica isolada, eventos mockáveis)
- ✅ Escalabilidade (adicionar listeners não requer modificar emissores)
- ✅ Manutenibilidade (ponto único de documentação de eventos)

**Padrões de Excelência:**
- ✅ ScriptableObjects para configurações
- ✅ Event-driven architecture
- ✅ Separation of concerns
- ✅ Dependency injection via GameContext

### 15.3 Próximos Passos

**Lotes Subsequentes:**
- **Lote 2 - Câmera RTS**: Documentação atualizada (próxima iteração)
- **Lote 3 - Unit**: Documentação atualizada (próxima iteração)
- **Lote 4 - Selection**: Documentação atualizada (próxima iteração)

**Novos Módulos (Futuro):**
- Sistema de Comandos (Move, Attack)
- Sistema de Combate (Dano, Morte, XP)
- Sistema de Construção (Placement, Custos)
- Sistema de IA (Pathfinding, Comportamento)

---

# LOTE 2 — CÂMERA SYSTEM RTS (CINEMACHINE V3)

**Versão:** 1.0  
**Status:** ✅ Implementado e Documentado  
**Data:** Outubro 2025

---

## 📋 ÍNDICE

1. [Visão Geral do Módulo](#1-visão-geral-do-módulo)
2. [RTSCameraProfile - Configuração Centralizada](#2-rtscameraprofile---configuração-centralizada)
3. [CameraMath - Utilitários Matemáticos](#3-cameramath---utilitários-matemáticos)
4. [RTSCameraCinemachineV3Controller - Controlador Principal](#4-rtscameracinemachinev3controller---controlador-principal)
5. [RTSCameraInputSystem - Tradutor de Input](#5-rtscamerainputsystem---tradutor-de-input)
6. [Fluxo de Execução Completo](#6-fluxo-de-execução-completo)
7. [Integração com Outros Módulos](#7-integração-com-outros-módulos)
8. [Configuração na Cena](#8-configuração-na-cena)
9. [Exemplos de Uso Avançados](#9-exemplos-de-uso-avançados)
10. [Funcionalidades Futuras (Roadmap)](#10-funcionalidades-futuras-roadmap)
11. [Troubleshooting e FAQ](#11-troubleshooting-e-faq)
12. [Tabela de Relacionamentos Completa](#12-tabela-de-relacionamentos-completa)
13. [Referências Rápidas](#13-referências-rápidas)
14. [Conclusão](#14-conclusão)

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Objetivo

Fornecer um **sistema de câmera RTS profissional** para Medieval Thrones, utilizando **Cinemachine v3** para máxima flexibilidade e qualidade visual.

**Funcionalidades Principais:**
- ✅ **Pan**: WASD, Edge Pan, Middle Mouse Drag
- ✅ **Zoom**: Scroll com interpolação de altura e tilt
- ✅ **Rotate**: Q/E para rotação horizontal
- ✅ **Bounds**: Limitação da área de movimento
- ✅ **Shake**: Sistema de tremor para impactos
- ✅ **GoTo**: Movimento suave para posição específica
- ✅ **Cutscenes**: Câmera cinemática para diálogos
- ✅ **Events**: Integração total com GameEvents (Lote 1)

### 1.2 Responsabilidades Principais

O **Lote 2 - Câmera System** é responsável por:

1. **Configuração Centralizada** (`RTSCameraProfile`):
   - Perfis reutilizáveis (Default, Cinematic, Spectator)
   - Velocidades de pan/zoom/rotate configuráveis
   - Curvas de suavização customizáveis
   - Flags de enable/disable por feature

2. **Lógica Matemática Pura** (`CameraMath`):
   - Cálculos de offset de tilt
   - Clamp de bounds
   - Suavização de movimento (ease in-out)
   - Edge pan detection
   - **100% testável** (sem dependências Unity)

3. **Controle Principal** (`RTSCameraCinemachineV3Controller`):
   - Setup automático de Cinemachine v3
   - Processamento de input via `TickInput()`
   - Handlers de eventos de gameplay (shake, foco, cutscenes)
   - Integração com GameEvents (Lote 1)

4. **Tradução de Input** (`RTSCameraInputSystem`):
   - Leitura do Unity Input System
   - Conversão para API unificada
   - Detecção de ponteiro sobre UI

### 1.3 Arquitetura do Sistema de Câmera

```
┌────────────────────────────────────────────────────────────┐
│                 LOTE 2 - CÂMERA SYSTEM                      │
└────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────────┐ ┌────────────┐ ┌──────────────────────┐
│RTSCameraProfile   │ │ CameraMath │ │RTSCameraController   │
│(ScriptableObject) │ │  (static)  │ │   (MonoBehaviour)    │
├───────────────────┤ ├────────────┤ ├──────────────────────┤
│• Pan Speeds       │ │• TiltOffset│ │• TickInput()         │
│• Zoom Range       │ │• Clamp     │ │• Shake/GoTo          │
│• Tilt Range       │ │• Lerp      │ │• Event Handlers      │
│• Rotate Speed     │ │• EdgePan   │ │• CM3 Setup           │
│• Curves           │ │• Bounds    │ │• Zoom/Pan/Rotate     │
│• Flags            │ └────────────┘ └──────────┬───────────┘
└─────────┬─────────┘                           │
          │                                     │
          │               ┌─────────────────────┘
          │               │
          │               ▼
          │      ┌────────────────────┐
          │      │RTSCameraInputSystem│
          │      │  (MonoBehaviour)   │
          │      ├────────────────────┤
          │      │• InputActionRefs   │
          │      │• Translate Input   │
          │      │• Pointer Over UI   │
          │      └────────┬───────────┘
          │               │
          └───────────────┘
                  │
                  ▼
          ┌──────────────┐
          │Cinemachine v3│
          │   (Unity)    │
          ├──────────────┤
          │• vcam        │
          │• Follow      │
          │• Noise/Shake │
          └──────────────┘
                  │
                  ▼
         ┌────────────────┐
         │  Unity Camera  │
         │  (renderiza)   │
         └────────────────┘
```

---

### 1.4 Filosofia de Design

#### **Por que Cinemachine v3?**

> "O Cinemachine é muito mais completo que a câmera da Unity. No futuro pretendemos trabalhar com cinematics, sistemas de tremer a tela... Enfim, muitas coisas para deixar o jogo mais agradável visualmente."
> 
> — Equipe de Desenvolvimento

**Benefícios:**
- ✅ Sistema de shake profissional (noise)
- ✅ Transições suaves entre câmeras
- ✅ Suporte a cutscenes nativo
- ✅ Composição avançada (dead zones, damping)

#### **Por que separar CameraMath?**

> "A lógica de todos os meus processos é deixar o máximo não acoplado, preciso que bugs sejam fáceis de se achar e corrigir e que a lógica do sistema fique fácil de se entender. Separando classes e funções dessas classes ajuda muito um programador a manter tudo, principalmente quando escala muito."
> 
> — Equipe de Desenvolvimento

**Benefícios:**
- ✅ **Testabilidade**: Funções puras podem ser testadas sem Unity
- ✅ **Reutilização**: `CameraMath` pode ser usado por outros sistemas
- ✅ **Debugging**: Erros matemáticos isolados de lógica de gameplay
- ✅ **Manutenibilidade**: Cada classe tem responsabilidade única

---

## 2) RTSCAMERAPROFILE - CONFIGURAÇÃO CENTRALIZADA

### 2.1 Visão Geral

**Tipo:** `ScriptableObject`  
**Responsabilidade:** Centralizar TODAS as configurações de câmera em um asset reutilizável.

**Criação:** `Assets > Create > Game > Camera Profile`

**Vantagens:**
- ✅ Criar múltiplos perfis (Default, Cinematic, Spectator)
- ✅ Trocar comportamento em runtime (ex: cutscene = perfil cinemático)
- ✅ Compartilhar entre cenas
- ✅ Testar valores sem recompilar código

### 2.2 Campos Públicos (Inspector)

**Screenshot de Referência:**

![RTSCameraProfile Inspector](reference://1761268110018_image.png)

#### **Pan (WASD / Borda / Drag)**

```csharp
[Header("Pan (WASD / Borda / Drag)")]
[Tooltip("Velocidade de pan quando a câmera está perto (zoom mínimo)")]
public float panSpeedNear = 20f;

[Tooltip("Velocidade de pan quando a câmera está longe (zoom máximo)")]
public float panSpeedFar = 35f;

[Tooltip("Espessura da borda em pixels para edge-pan")]
public int edgeThickness = 12;

[Tooltip("Sensibilidade do arrasto com botão do meio")]
public float middleDragSensitivity = 1f;
```

**Valores Padrão (baseado no screenshot):**
- `panSpeedNear`: **20** unidades/segundo (câmera próxima)
- `panSpeedFar`: **35** unidades/segundo (câmera distante)
- `edgeThickness`: **12** pixels (área de detecção na borda)
- `middleDragSensitivity`: **1** (sensibilidade padrão)

**Comportamento:**
- Pan speed **interpola** entre Near e Far baseado no zoom atual
- Edge pan ativa quando mouse está nos **12 pixels** da borda
- Middle drag usa delta do mouse × sensibilidade

---

#### **Zoom (Altura + Tilt)**

```csharp
[Header("Zoom (Altura + Tilt)")]
[Tooltip("Velocidade de zoom (scroll)")]
public float zoomSpeed = 0.15f;

[Tooltip("Altura mínima da câmera (zoom próximo)")]
public float minHeight = 10f;

[Tooltip("Altura máxima da câmera (zoom distante)")]
public float maxHeight = 60f;

[Tooltip("Ângulo de tilt mínimo em graus (visão mais plana)")]
public float minTilt = 35f;

[Tooltip("Ângulo de tilt máximo em graus (visão mais inclinada)")]
public float maxTilt = 75f;
```

**Valores Padrão:**
- `zoomSpeed`: **0.15** (15% do zoom por scroll)
- `minHeight`: **10** (altura quando zoom = 0, câmera próxima)
- `maxHeight`: **60** (altura quando zoom = 1, câmera distante)
- `minTilt`: **35°** (ângulo plano, visão de cima)
- `maxTilt`: **75°** (ângulo inclinado, visão mais cinematográfica)

**Relação Altura × Tilt:**

| Zoom | Altura | Tilt | Perspectiva |
|------|--------|------|-------------|
| 0.0 (próximo) | 10 | 35° | Visão tática (mais flat) |
| 0.5 (médio) | 35 | 55° | Balanceado |
| 1.0 (distante) | 60 | 75° | Visão estratégica (mais inclinada) |

---

#### **Rotation**

```csharp
[Header("Rotation")]
[Tooltip("Velocidade de rotação em graus por segundo")]
public float rotateSpeed = 90f;
```

**Valor Padrão:**
- `rotateSpeed`: **90°/segundo** (4 segundos para rotação completa)

---

#### **Opções de Curvas (Avançado)**

```csharp
[Header("Opções de Curvas (Avançado)")]
[Tooltip("Curva de suavização para movimento de pan por zoom. X=zoom(0-1), Y=multiplicador")]
public AnimationCurve panSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);

[Tooltip("Curva de suavização para zoom. X=entrada, Y=saída suavizada")]
public AnimationCurve zoomCurve = AnimationCurve.Linear(0, 0, 1, 1);
```

**Screenshot mostra curvas lineares (padrão):**
- `panSpeedCurve`: ![Curva Linear Verde](reference://1761268110018_image.png)
- `zoomCurve`: ![Curva Linear Verde](reference://1761268110018_image.png)

**Uso Avançado:**

**Pan Speed Curve:**
- X: Zoom atual (0..1)
- Y: Multiplicador de velocidade
- Exemplo: Curva com "bump" em zoom=0.5 faz pan mais rápido no meio termo

**Zoom Curve:**
- X: Entrada do scroll
- Y: Valor final do zoom
- Exemplo: Curva com ease-in faz zoom começar devagar e acelerar

---

#### **Flags de Interação**

```csharp
[Header("Flags de Interação")]
public bool enableWASD = true;
public bool enableEdgePan = true;
public bool enableMiddleDrag = true;
public bool enableRotate = true;
public bool pauseWhenPointerOverUI = true;
```

**Screenshot mostra TODAS ativadas (✓):**

| Flag | Valor Padrão | Descrição |
|------|--------------|-----------|
| `enableWASD` | ✅ `true` | Habilita movimento com teclado (WASD/Arrows) |
| `enableEdgePan` | ✅ `true` | Habilita pan ao encostar mouse na borda |
| `enableMiddleDrag` | ✅ `true` | Habilita arrastar com botão do meio |
| `enableRotate` | ✅ `true` | Habilita rotação com Q/E |
| `pauseWhenPointerOverUI` | ✅ `true` | Pausa input quando mouse sobre UI |

**Uso Típico:**
- Desabilitar `enableEdgePan` em menus (evitar pan acidental)
- Desabilitar `enableRotate` em missões de tutorial
- `pauseWhenPointerOverUI` **sempre ativado** (evita conflitos)

---

### 2.3 Métodos Helpers

#### **GetPanSpeed()**

```csharp
/// <summary>
/// Calcula a velocidade de pan interpolada baseada no zoom atual (0..1)
/// Aplica a curva customizada se configurada
/// </summary>
public float GetPanSpeed(float zoom01)
{
    float baseSpeed = Mathf.Lerp(panSpeedNear, panSpeedFar, zoom01);
    float curveMultiplier = panSpeedCurve.Evaluate(zoom01);
    return baseSpeed * curveMultiplier;
}
```

**Exemplo:**
```csharp
// Zoom médio (0.5)
float speed = profile.GetPanSpeed(0.5f);
// speed = Lerp(20, 35, 0.5) * 1.0 = 27.5 unidades/segundo
```

---

#### **ApplyZoomCurve()**

```csharp
/// <summary>
/// Aplica suavização no valor de zoom usando a curva configurada
/// </summary>
public float ApplyZoomCurve(float rawZoom01)
{
    return zoomCurve.Evaluate(Mathf.Clamp01(rawZoom01));
}
```

**Exemplo:**
```csharp
// Scroll raw = 0.5
float smoothZoom = profile.ApplyZoomCurve(0.5f);
// Com curva linear: smoothZoom = 0.5
// Com curva ease-in-out: smoothZoom = ~0.48 (mais suave)
```

---

### 2.4 Exemplos de Perfis

#### **Perfil Default (Gameplay)**

```
Pan Speed Near: 20
Pan Speed Far: 35
Edge Thickness: 12
Zoom Speed: 0.15
Min Height: 10
Max Height: 60
Min Tilt: 35°
Max Tilt: 75°
Rotate Speed: 90°/s
All Flags: ✅ Enabled
```

**Uso:** Gameplay normal, balanceado para RTS.

---

#### **Perfil Cinematic (Cutscenes)**

```
Pan Speed Near: 5
Pan Speed Far: 10
Edge Thickness: 0 (desabilitado)
Zoom Speed: 0.05 (mais lento)
Min Height: 5
Max Height: 30
Min Tilt: 45°
Max Tilt: 65°
Rotate Speed: 30°/s (mais suave)
enableWASD: ❌ Disabled
enableEdgePan: ❌ Disabled
enableMiddleDrag: ❌ Disabled
enableRotate: ❌ Disabled
```

**Uso:** Cutscenes controladas por script, movimentos suaves.

---

#### **Perfil Spectator (Observador)**

```
Pan Speed Near: 40
Pan Speed Far: 80
Edge Thickness: 20 (borda maior)
Zoom Speed: 0.25 (mais rápido)
Min Height: 20
Max Height: 100 (muito distante)
Min Tilt: 25°
Max Tilt: 85°
Rotate Speed: 180°/s (rotação rápida)
All Flags: ✅ Enabled
```

**Uso:** Modo espectador, movimentos rápidos e zoom extremo.

---

## 3) CAMERAMATH - UTILITÁRIOS MATEMÁTICOS

### 3.1 Visão Geral

**Tipo:** `static class`  
**Responsabilidade:** Funções matemáticas **puras** para cálculos de câmera, sem dependências de Unity exceto tipos básicos (Vector3, Mathf).

**Por que separar?**
> "A lógica de todos os meus processos é deixar o máximo não acoplado, preciso que bugs sejam fáceis de se achar e corrigir e que a lógica do sistema fique fácil de se entender."

**Vantagens:**
- ✅ **100% testável** em testes unitários
- ✅ **Reutilizável** em outros sistemas (minimap, fog of war, etc.)
- ✅ **Debugging facilitado**: Erros matemáticos isolados
- ✅ **Performance**: Funções static inline

### 3.2 API Completa de Funções

#### **TiltToOffset()**

```csharp
/// <summary>
/// Converte altura e ângulo de tilt em offset 3D para o CinemachineFollow.
/// Retorna o vetor de offset no espaço mundial.
/// </summary>
/// <param name="height">Altura da câmera acima do pivot</param>
/// <param name="tiltDegrees">Ângulo de inclinação em graus</param>
/// <param name="forwardDirection">Direção forward do rig (para calcular "trás")</param>
public static Vector3 TiltToOffset(float height, float tiltDegrees, Vector3 forwardDirection)
{
    float tiltRad = tiltDegrees * Mathf.Deg2Rad;
    float distance = (Mathf.Tan(tiltRad) > 0.0001f)
        ? height / Mathf.Tan(tiltRad)
        : height * 2f;

    Vector3 back = -forwardDirection.normalized;
    return back * distance + Vector3.up * height;
}
```

**Matemática:**

```
        Câmera (offset)
            ▲
            │\ 
    height  │ \ hypotenuse
            │  \
            │   \
            │tilt\
            │     \
            └──────▶ Pivot (origin)
            distance
```

**Fórmula:**
```
distance = height / tan(tilt)
offset = -forward * distance + up * height
```

**Exemplo:**
```csharp
// Zoom médio: height=35, tilt=55°
Vector3 offset = CameraMath.TiltToOffset(35f, 55f, transform.forward);
// offset ≈ (-24, 35, 0) (câmera atrás e acima do pivot)
```

---

#### **SmoothStep01()**

```csharp
/// <summary>
/// Suavização cúbica (ease in-out) para interpolação de movimento.
/// Entrada e saída normalizadas 0..1
/// </summary>
public static float SmoothStep01(float t)
{
    t = Mathf.Clamp01(t);
    return t * t * (3f - 2f * t);
}
```

**Curva Hermite (Smooth Step):**

```
1.0 ┤         ╭───────
    │       ╭─╯
0.5 ┤     ╭─╯
    │   ╭─╯
0.0 ┴───╯─────────────
    0   0.5   1.0
```

**Fórmula:** `f(t) = t² (3 - 2t)`

**Uso:** Movimentos suaves (GoTo, focus)

---

#### **SmoothLerp()**

```csharp
/// <summary>
/// Interpola com suavização cúbica entre dois pontos
/// </summary>
public static Vector3 SmoothLerp(Vector3 start, Vector3 end, float t)
{
    float smoothT = SmoothStep01(t);
    return Vector3.LerpUnclamped(start, end, smoothT);
}
```

**Exemplo:**
```csharp
// Movimento de (0,0,0) para (100,0,0) em 2 segundos
Vector3 pos = CameraMath.SmoothLerp(start, end, time / duration);
// Movimento começa devagar, acelera, desacelera no final
```

---

#### **ClampToBounds()**

```csharp
/// <summary>
/// Clamp de posição 2D (XZ) dentro de bounds retangulares
/// </summary>
public static Vector3 ClampToBounds(Vector3 position, Vector2 boundsCenter, Vector2 boundsSize)
{
    var half = boundsSize * 0.5f;
    float minX = boundsCenter.x - half.x;
    float maxX = boundsCenter.x + half.x;
    float minZ = boundsCenter.y - half.y;
    float maxZ = boundsCenter.y + half.y;

    return new Vector3(
        Mathf.Clamp(position.x, minX, maxX),
        position.y,
        Mathf.Clamp(position.z, minZ, maxZ)
    );
}
```

**Visualização:**

```
      maxZ ──────────────────────────┐
           │                          │
           │        Área Válida       │
           │                          │
      minZ └──────────────────────────┘
         minX                      maxX
```

**Exemplo:**
```csharp
// Bounds: center=(500,500), size=(1000,1000)
// Área válida: X=[0..1000], Z=[0..1000]
Vector3 clamped = CameraMath.ClampToBounds(
    new Vector3(1200, 10, -50), // Fora dos bounds
    new Vector2(500, 500),
    new Vector2(1000, 1000)
);
// clamped = (1000, 10, 0) // Ajustado para dentro
```

---

#### **XZToVector3()**

```csharp
/// <summary>
/// Converte posição XZ (2D do mundo) em Vector3 mantendo a altura Y
/// </summary>
public static Vector3 XZToVector3(Vector2 xz, float y)
{
    return new Vector3(xz.x, y, xz.y);
}
```

**Uso:** Converter cliques de minimap (2D) para posição 3D.

**Exemplo:**
```csharp
// Clique no minimap em (250, 350)
Vector2 minimapPos = new Vector2(250, 350);
Vector3 worldPos = CameraMath.XZToVector3(minimapPos, currentCameraHeight);
// worldPos = (250, currentHeight, 350)
```

---

#### **CalculateTerrainBounds()**

```csharp
/// <summary>
/// Calcula o centro e tamanho de bounds a partir de múltiplos terrenos
/// </summary>
public static bool CalculateTerrainBounds(Terrain[] terrains, out Vector2 center, out Vector2 size)
{
    center = Vector2.zero;
    size = Vector2.zero;

    if (terrains == null || terrains.Length == 0)
        return false;

    Bounds totalBounds;

    if (terrains.Length == 1)
    {
        var t = terrains[0];
        var pos = t.transform.position;
        var terrainSize = t.terrainData.size;
        totalBounds = new Bounds(
            pos + new Vector3(terrainSize.x, 0, terrainSize.z) * 0.5f,
            new Vector3(terrainSize.x, 0, terrainSize.z)
        );
    }
    else
    {
        totalBounds = new Bounds();
        for (int i = 0; i < terrains.Length; i++)
        {
            var t = terrains[i];
            var sz = t.terrainData.size;
            var p = t.transform.position;
            var bb = new Bounds(
                p + new Vector3(sz.x, 0, sz.z) * 0.5f,
                new Vector3(sz.x, 0, sz.z)
            );
            if (i == 0)
                totalBounds = bb;
            else
                totalBounds.Encapsulate(bb);
        }
    }

    center = new Vector2(totalBounds.center.x, totalBounds.center.z);
    size = new Vector2(totalBounds.size.x, totalBounds.size.z);
    return true;
}
```

**Uso:** Calcular bounds automaticamente baseado em terrenos da cena.

**Exemplo:**
```csharp
// Cena com 2 terrenos: Terrain1 (1000x1000) e Terrain2 (500x500)
if (CameraMath.CalculateTerrainBounds(Terrain.activeTerrains, out Vector2 center, out Vector2 size))
{
    Debug.Log($"Center: {center}, Size: {size}");
    // Center: (750, 500), Size: (1500, 1000)
}
```

---

#### **GetEdgePanDirection()**

```csharp
/// <summary>
/// Verifica se uma posição de tela está dentro da borda para edge-pan
/// </summary>
public static Vector2 GetEdgePanDirection(Vector2 screenPos, int edgeThickness, int screenWidth, int screenHeight)
{
    Vector2 direction = Vector2.zero;

    if (screenPos.x <= edgeThickness)
        direction.x = -1;
    else if (screenPos.x >= screenWidth - edgeThickness)
        direction.x = 1;

    if (screenPos.y <= edgeThickness)
        direction.y = -1;
    else if (screenPos.y >= screenHeight - edgeThickness)
        direction.y = 1;

    return direction;
}
```

**Visualização:**

```
┌─────────────────────────┐ ← edgeThickness (12px)
│  ↑                      │
│←   (área normal)       →│
│  ↓                      │
└─────────────────────────┘
```

**Retorno:**
- `(-1, 0)`: Mouse na borda esquerda
- `(1, 0)`: Mouse na borda direita
- `(0, -1)`: Mouse na borda inferior
- `(0, 1)`: Mouse na borda superior
- `(-1, 1)`: Mouse no canto superior esquerdo (diagonal)

**Exemplo:**
```csharp
// Mouse em (5, 600) em tela 1920x1080, edge=12
Vector2 dir = CameraMath.GetEdgePanDirection(new Vector2(5, 600), 12, 1920, 1080);
// dir = (-1, 0) // Borda esquerda
```

---

### 3.3 Exemplos de Uso

#### **Exemplo 1: Calcular Offset da Câmera**

```csharp
// Em RTSCameraCinemachineV3Controller
void UpdateFollowOffset()
{
    // Valores: height=35, tilt=55°, forward do rig
    _follow.FollowOffset = CameraMath.TiltToOffset(
        _cachedHeight,
        _cachedTilt,
        transform.forward
    );
}
```

#### **Exemplo 2: Movimento Suave para Posição**

```csharp
IEnumerator CoGoTo(Vector3 target, float duration)
{
    Vector3 start = transform.position;
    float t = 0f;

    while (t < 1f)
    {
        t += Time.deltaTime / duration;
        transform.position = CameraMath.SmoothLerp(start, target, t); // ← Uso
        yield return null;
    }
}
```

#### **Exemplo 3: Detectar Edge Pan**

```csharp
void HandlePan(Vector2 mousePos, ...)
{
    Vector2 edgeDir = CameraMath.GetEdgePanDirection(
        mousePos,
        profile.edgeThickness,
        Screen.width,
        Screen.height
    );

    if (edgeDir != Vector2.zero)
    {
        Vector3 move = transform.right * edgeDir.x + transform.forward * edgeDir.y;
        transform.position += move * speed * Time.deltaTime;
    }
}
```

---

### 3.4 Testabilidade

**Por que CameraMath é 100% testável:**

1. **Funções Puras:**
   - Entrada → Saída determinística
   - Sem side effects
   - Sem estado global

2. **Sem Dependências Unity:**
   - Usa apenas tipos básicos (Vector3, float, etc.)
   - Pode rodar em testes unitários (NUnit, xUnit)

**Exemplo de Teste Unitário:**

```csharp
[Test]
public void TiltToOffset_ShouldCalculateCorrectly()
{
    // Arrange
    float height = 35f;
    float tilt = 55f;
    Vector3 forward = Vector3.forward;

    // Act
    Vector3 offset = CameraMath.TiltToOffset(height, tilt, forward);

    // Assert
    Assert.AreEqual(35f, offset.y, 0.1f); // Altura correta
    Assert.IsTrue(offset.z < 0); // Câmera atrás do pivot
    Assert.Greater(Mathf.Abs(offset.z), 20f); // Distância razoável
}

[Test]
public void ClampToBounds_ShouldClampPosition()
{
    // Arrange
    Vector3 pos = new Vector3(1200, 10, -50); // Fora dos bounds
    Vector2 center = new Vector2(500, 500);
    Vector2 size = new Vector2(1000, 1000);

    // Act
    Vector3 clamped = CameraMath.ClampToBounds(pos, center, size);

    // Assert
    Assert.AreEqual(1000f, clamped.x); // X clampado ao máximo
    Assert.AreEqual(10f, clamped.y); // Y preservado
    Assert.AreEqual(0f, clamped.z); // Z clampado ao mínimo
}
```

---

## 4) RTSCAMERACINEMACHINEV3CONTROLLER - CONTROLADOR PRINCIPAL

### 4.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Requer:** Cinemachine v3 (Unity package)  
**Responsabilidade:** Controlador central que orquestra todo o sistema de câmera.

**Screenshot de Referência:**

![RTSCameraController Inspector](reference://1761268100915_image.png)

**Funcionalidades:**
- ✅ Setup automático de Cinemachine v3 (vcam, follow, aim, noise)
- ✅ Processamento de input via `TickInput()`
- ✅ Pan (WASD, Edge, Middle Drag)
- ✅ Zoom (scroll com altura + tilt)
- ✅ Rotate (Q/E)
- ✅ Bounds (clamp de posição)
- ✅ Shake (tremor de câmera)
- ✅ GoTo (movimento suave)
- ✅ Cutscenes (câmera cinemática)
- ✅ Integração com GameEvents (7 eventos)

### 4.2 Campos Públicos (Inspector)

#### **Profile & References**

```csharp
[Header("Profile & References")]
[Tooltip("Perfil de configuração da câmera (ScriptableObject)")]
public RTSCameraProfile profile;

[Tooltip("Câmera virtual do Cinemachine (criada automaticamente se null)")]
public CinemachineCamera vcam;

[Tooltip("Pivot usado para tilt (criado automaticamente se null)")]
public Transform pivot;
```

**Valores do Screenshot:**
- `profile`: **CameraProfile (RTS Camera Profile)** ← ScriptableObject
- `vcam`: **RTS_Camera (CM3) (Cinemachine Camera)** ← Auto-criado
- `pivot`: **Pivot (Transform)** ← Auto-criado

**Nota:** Se `vcam` ou `pivot` forem `null` no Inspector, o `EnsureSetup()` cria automaticamente.

---

#### **Bounds (Mundo)**

```csharp
[Header("Bounds (Mundo)")]
public Vector2 boundsCenter = Vector2.zero; // (x,z)
public Vector2 boundsSize = new Vector2(200, 200);
```

**Valores do Screenshot:**
- `boundsCenter`: **(500, 500)** ← Centro da área jogável
- `boundsSize`: **(1000, 1000)** ← Área de 1km²

**Cálculo de Área Válida:**
```
minX = center.x - size.x/2 = 500 - 500 = 0
maxX = center.x + size.x/2 = 500 + 500 = 1000
minZ = center.y - size.y/2 = 500 - 500 = 0
maxZ = center.y + size.y/2 = 500 + 500 = 1000

Área válida: X=[0..1000], Z=[0..1000]
```

**Visualização no Gizmo:**

![Gizmo de Bounds](reference://1761268245880_image.png)

- **Verde translúcido**: Área interna (válida)
- **Verde wireframe**: Borda dos bounds
- **Terreno**: Alinhado dentro dos bounds

---

#### **Initial Setup**

```csharp
[Header("Initial Setup")]
public Vector3 initialPosition = Vector3.zero;
public float initialHeading = 0f;
[Range(0, 1)] public float initialZoom = 0.5f;
```

**Valores do Screenshot:**
- `initialPosition`: **(127.24, 0, 47.8128)** ← Spawn customizado
- `initialHeading`: **75°** ← Rotação inicial (nordeste)
- `initialZoom`: **0.5** ← Zoom médio

**Aplicação:**
```csharp
void Awake()
{
    transform.position = initialPosition;
    transform.rotation = Quaternion.Euler(0f, initialHeading, 0f);
    _zoom = Mathf.Clamp01(initialZoom);
    ApplyZoomAndTilt();
}
```

---

#### **Runtime State (Read-Only)**

```csharp
[Header("Runtime State (Read-Only)")]
[SerializeField, Range(0, 1)]
private float _zoom = 0.5f;  // 0=perto 1=longe

public float Zoom => _zoom;
```

**Valor do Screenshot:**
- `_zoom`: **0.5** (leitura em tempo real no Inspector)

**Nota:** Campo `private` mas visível no Inspector para debug (`[SerializeField]`).

---

### 4.3 API Pública

#### **TickInput() - Processa Input por Frame**

```csharp
/// <summary>
/// Processa entrada por frame. Chamado pelo RTSCameraInputSystem.
/// </summary>
public void TickInput(
    Vector2 wasdMove,
    Vector2 pointerPosition,
    bool isMiddleDragging,
    Vector2 pointerDelta,
    float rotateAxis,
    float zoomAxis,
    bool pointerOverUI)
```

**Parâmetros:**
- `wasdMove`: Vector2 do Input System (WASD/Arrows)
- `pointerPosition`: Posição do mouse na tela (pixels)
- `isMiddleDragging`: Se botão do meio está pressionado
- `pointerDelta`: Delta do movimento do mouse
- `rotateAxis`: Eixo Q/E (-1, 0, +1)
- `zoomAxis`: Eixo do scroll (-1, 0, +1)
- `pointerOverUI`: Se mouse está sobre UI (EventSystem)

**Fluxo Interno:**
```csharp
public void TickInput(...)
{
    if (profile == null) return;
    
    bool pauseInput = profile.pauseWhenPointerOverUI && pointerOverUI;
    
    HandleZoom(zoomAxis, pauseInput);
    HandleRotate(rotateAxis, pauseInput);
    HandlePan(wasdMove, pointerPosition, isMiddleDragging, pointerDelta, pauseInput);
    ClampToBounds();
    UpdateFollowOffset();
}
```

---

#### **GoTo() / GoToXZ() - Movimento Suave**

```csharp
/// <summary>
/// Move a câmera para uma posição 3D específica.
/// </summary>
public void GoTo(Vector3 worldPos, bool snap = false, float duration = 0.4f)

/// <summary>
/// Move a câmera para uma posição XZ (2D), mantendo altura Y atual.
/// </summary>
public void GoToXZ(Vector2 worldXZ, bool snap = false, float duration = 0.4f)
```

**Parâmetros:**
- `worldPos` / `worldXZ`: Posição alvo
- `snap`: Se `true`, teleporta instantaneamente
- `duration`: Tempo de movimento (segundos)

**Exemplo:**
```csharp
// Focar câmera em unidade selecionada
Unit selectedUnit = GetSelectedUnit();
cameraController.GoTo(selectedUnit.transform.position, snap: false, duration: 0.5f);

// Clique no minimap (2D)
Vector2 minimapClick = new Vector2(250, 350);
cameraController.GoToXZ(minimapClick, snap: false, duration: 0.3f);
```

---

#### **PlayShake() - Tremor de Câmera**

```csharp
/// <summary>
/// Dispara efeito de shake (tremor) na câmera.
/// </summary>
public void PlayShake(float amplitude = 1.2f, float frequency = 2.0f, float duration = 0.25f)
```

**Parâmetros:**
- `amplitude`: Intensidade do tremor (0.5-3.0)
- `frequency`: Frequência da oscilação (1.0-5.0)
- `duration`: Duração em segundos (0.1-1.0)

**Exemplos de Uso:**

| Situação | Amplitude | Frequency | Duration | Descrição |
|----------|-----------|-----------|----------|-----------|
| Impacto leve | 0.5 | 2.0 | 0.15s | Unidade bate em parede |
| Explosão média | 1.2 | 3.5 | 0.35s | Granada explode |
| Terremoto | 2.5 | 4.0 | 1.0s | Boss entra em cena |

**Código:**
```csharp
// Explosão de catapulta
void OnCatapultHit(Vector3 impactPos)
{
    cameraController.PlayShake(
        amplitude: 1.8f,
        frequency: 3.0f,
        duration: 0.5f
    );
}
```

---

#### **StartCutscene() / EndCutscene() - Câmera Cinemática**

```csharp
/// <summary>
/// Inicia cutscene focando em um Transform específico.
/// </summary>
public void StartCutscene(Transform target, float fov = 50f, int priority = 100)

/// <summary>
/// Finaliza cutscene, retornando ao controle normal.
/// </summary>
public void EndCutscene()
```

**Parâmetros:**
- `target`: Transform a focar (personagem, objeto)
- `fov`: Field of View (45-60 típico)
- `priority`: Prioridade do Cinemachine (>10 para sobrepor gameplay)

**Exemplo:**
```csharp
// Diálogo de NPC
void StartDialogue(NPC npc)
{
    cameraController.StartCutscene(
        target: npc.headTransform,
        fov: 45f, // FOV mais fechado (close-up)
        priority: 100
    );
}

void EndDialogue()
{
    cameraController.EndCutscene(); // Volta ao gameplay
}
```

---

#### **SetBoundsFromTerrains() - Calcular Bounds Automaticamente**

```csharp
[ContextMenu("Set Bounds From Terrain(s)")]
public void SetBoundsFromTerrains()
```

**Uso:**
1. Adicionar terrenos à cena
2. Clicar com botão direito no componente
3. Selecionar "Set Bounds From Terrain(s)"
4. Bounds são calculados automaticamente

**Código:**
```csharp
public void SetBoundsFromTerrains()
{
    if (CameraMath.CalculateTerrainBounds(
        Terrain.activeTerrains,
        out Vector2 center,
        out Vector2 size))
    {
        boundsCenter = center;
        boundsSize = size;
        Debug.Log($"[RTSCamera] Bounds atualizados: Center={center}, Size={size}");
    }
}
```

---

### 4.4 Handlers de Input

#### **HandlePan() - Movimento Lateral**

```csharp
void HandlePan(Vector2 moveInput, Vector2 mousePos, bool dragging, Vector2 dragDelta, bool pauseInput)
{
    if (profile == null) return;

    Vector3 move = Vector3.zero;

    // WASD
    if (profile.enableWASD)
    {
        move += transform.forward * moveInput.y + transform.right * moveInput.x;
        move.y = 0;
    }

    // Edge Pan
    if (profile.enableEdgePan && !pauseInput)
    {
        Vector2 edgeDir = CameraMath.GetEdgePanDirection(
            mousePos,
            profile.edgeThickness,
            Screen.width,
            Screen.height
        );

        move += transform.right * edgeDir.x + transform.forward * edgeDir.y;
        move.y = 0;
    }

    // Middle Drag
    if (profile.enableMiddleDrag && dragging)
    {
        move += (-transform.right * dragDelta.x - transform.forward * dragDelta.y)
                * (0.01f * profile.middleDragSensitivity);
    }

    // Aplicar movimento com velocidade do profile
    if (move.sqrMagnitude > 0.0001f)
    {
        float speed = profile.GetPanSpeed(_zoom);
        transform.position += move.normalized * speed * Time.deltaTime;
    }
}
```

**Fluxo:**
1. Calcula vetor de movimento (WASD + Edge + Drag)
2. Obtém velocidade baseada no zoom (`GetPanSpeed()`)
3. Move transform do rig (não a vcam diretamente)

---

#### **HandleZoom() - Scroll**

```csharp
void HandleZoom(float axis, bool pauseInput)
{
    if (profile == null) return;
    if (Mathf.Abs(axis) < 0.0001f) return;
    if (pauseInput) return;

    _zoom = Mathf.Clamp01(_zoom - axis * profile.zoomSpeed);
    _zoom = profile.ApplyZoomCurve(_zoom); // Aplicar curva de suavização
    ApplyZoomAndTilt();
}

void ApplyZoomAndTilt()
{
    if (profile == null) return;

    _cachedHeight = Mathf.Lerp(profile.minHeight, profile.maxHeight, _zoom);
    _cachedTilt = Mathf.Lerp(profile.minTilt, profile.maxTilt, _zoom);
}
```

**Fluxo:**
1. Scroll → Atualiza `_zoom` (0..1)
2. Aplica curva de suavização
3. Interpola altura e tilt
4. `UpdateFollowOffset()` recalcula posição da vcam

---

#### **HandleRotate() - Q/E**

```csharp
void HandleRotate(float axis, bool pauseInput)
{
    if (profile == null || !profile.enableRotate) return;
    if (Mathf.Abs(axis) < 0.0001f) return;
    if (pauseInput) return;

    transform.Rotate(Vector3.up, axis * profile.rotateSpeed * Time.deltaTime, Space.World);
}
```

**Fluxo:**
1. Q/E → Rotação em Y (Space.World)
2. Velocidade configurada no profile (90°/s padrão)

---

### 4.5 Handlers de Eventos (GameEvents)

**Integração com Lote 1:**

```csharp
void OnEnable()
{
    GameEvents.OnCameraShake += HandleCameraShake;
    GameEvents.OnCameraFocus += HandleCameraFocus;
    GameEvents.OnCameraFocusXZ += HandleCameraFocusXZ;
    GameEvents.OnCutsceneStart += HandleCutsceneStart;
    GameEvents.OnCutsceneEnd += HandleCutsceneEnd;
    GameEvents.OnMinimapPing += HandleMinimapPing;
    GameEvents.OnSelectionFocus += HandleSelectionFocus;
}

void OnDisable()
{
    GameEvents.OnCameraShake -= HandleCameraShake;
    GameEvents.OnCameraFocus -= HandleCameraFocus;
    GameEvents.OnCameraFocusXZ -= HandleCameraFocusXZ;
    GameEvents.OnCutsceneStart -= HandleCutsceneStart;
    GameEvents.OnCutsceneEnd -= HandleCutsceneEnd;
    GameEvents.OnMinimapPing -= HandleMinimapPing;
    GameEvents.OnSelectionFocus -= HandleSelectionFocus;
}
```

**Tabela de Handlers:**

| Evento | Handler | Ação |
|--------|---------|------|
| `OnCameraShake` | `HandleCameraShake(float amp, float freq, float dur)` | Inicia coroutine de shake |
| `OnCameraFocus` | `HandleCameraFocus(Vector3 pos, bool snap, float dur)` | Chama `GoTo()` |
| `OnCameraFocusXZ` | `HandleCameraFocusXZ(Vector2 xz, bool snap, float dur)` | Chama `GoToXZ()` |
| `OnCutsceneStart` | `HandleCutsceneStart(Transform target, float fov, int priority)` | Chama `StartCutscene()` |
| `OnCutsceneEnd` | `HandleCutsceneEnd()` | Chama `EndCutscene()` |
| `OnMinimapPing` | `HandleMinimapPing(Vector2 xz)` | Foco suave (0.5s) |
| `OnSelectionFocus` | `HandleSelectionFocus(Transform target)` | Foco suave (0.3s) |

**Exemplo de Uso:**

```csharp
// Sistema de combate dispara shake
void OnExplosion(Vector3 pos)
{
    // Câmera reage automaticamente via evento
    GameEvents.RaiseCameraShake(amplitude: 2.0f, frequency: 3.5f, duration: 0.4f);
}

// Clique no minimap
void OnMinimapClicked(Vector2 worldXZ)
{
    // Câmera move automaticamente via evento
    GameEvents.RaiseMinimapPing(worldXZ);
}
```

---

### 4.6 Integração com Cinemachine v3

#### **Setup Automático (EnsureSetup)**

```csharp
void EnsureSetup()
{
    // 1. Criar Pivot se não existir
    if (!pivot)
    {
        pivot = new GameObject("Pivot").transform;
        pivot.SetParent(transform, false);
    }

    // 2. Criar vcam se não existir
    if (!vcam)
    {
        var go = new GameObject("RTS_Camera (CM3)");
        vcam = go.AddComponent<CinemachineCamera>();
    }

    // 3. Configurar vcam
    vcam.Follow = pivot;
    vcam.LookAt = pivot;

    // 4. Adicionar componentes CM3
    _follow = vcam.GetOrAddComponent<CinemachineFollow>();
    vcam.GetOrAddComponent<CinemachineRotationComposer>();
    _noise = vcam.GetOrAddComponent<CinemachineBasicMultiChannelPerlin>();

    _noise.AmplitudeGain = 0;
    _noise.FrequencyGain = 0;
}
```

**Hierarquia Criada:**

```
RTS Camera Rig (GameObject com RTSCameraController)
├─ Pivot (Transform)
└─ RTS_Camera (CM3) (CinemachineCamera)
   ├─ CinemachineFollow (Body)
   ├─ CinemachineRotationComposer (Aim)
   └─ CinemachineBasicMultiChannelPerlin (Noise/Shake)
```

---

#### **Componentes Cinemachine v3:**

**CinemachineFollow (Body):**
- Define como a vcam segue o `pivot`
- `FollowOffset`: Calculado por `CameraMath.TiltToOffset()`

**CinemachineRotationComposer (Aim):**
- Define como a vcam aponta para o `pivot`

**CinemachineBasicMultiChannelPerlin (Noise):**
- Usado para shake (tremor)
- `AmplitudeGain`: Intensidade do tremor
- `FrequencyGain`: Frequência da oscilação

---

### 4.7 Shake e Cutscenes

#### **CoShake() - Coroutine de Shake**

```csharp
IEnumerator CoShake(float amp, float freq, float dur)
{
    if (_noise == null) yield break;

    _noise.AmplitudeGain = amp;
    _noise.FrequencyGain = freq;

    float t = 0f;
    while (t < dur)
    {
        t += Time.deltaTime;
        yield return null;
    }

    _noise.AmplitudeGain = 0;
    _noise.FrequencyGain = 0;
}
```

**Fluxo:**
1. Ativa noise com amplitude/frequency
2. Aguarda duração
3. Desativa noise

---

#### **Cutscene - Câmera Secundária**

```csharp
public void StartCutscene(Transform target, float fov = 50f, int priority = 100)
{
    // Criar vcam de cutscene se não existir
    if (_cutscene == null)
    {
        var go = new GameObject("Cutscene (CM3)");
        _cutscene = go.AddComponent<CinemachineCamera>();
        _cutscene.gameObject.AddComponent<CinemachineFollow>();
        _cutscene.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
    }

    _cutscene.Follow = target;
    _cutscene.LookAt = target;
    _cutscene.Lens.FieldOfView = fov;
    _cutscene.Priority = priority; // Sobrepõe gameplay camera
}

public void EndCutscene()
{
    if (_cutscene)
        _cutscene.Priority = 0; // Gameplay camera volta
}
```

**Sistema de Prioridade do Cinemachine:**
- Gameplay vcam: Priority = 10 (padrão)
- Cutscene vcam: Priority = 100 (ativa)
- Cinemachine blend automaticamente entre câmeras

---

## 5) RTSCAMERAINPUTSYSTEM - TRADUTOR DE INPUT

### 5.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Requer:** `RTSCameraCinemachineV3Controller`  
**Responsabilidade:** Ler Unity Input System e traduzir para API unificada (`TickInput()`).

**Screenshot de Referência:**

![RTSCameraInputSystem Inspector](reference://1761268100915_image.png)

**Funcionalidades:**
- ✅ Leitura de Input Action References
- ✅ Conversão para tipos primitivos (Vector2, float)
- ✅ Detecção de ponteiro sobre UI
- ✅ Gerenciamento de enable/disable de ações

### 5.2 Input Action References (Inspector)

```csharp
[Header("Action References (arraste do .inputactions)")]
[Tooltip("Movimento WASD (Vector2 - 2D Vector)")]
public InputActionReference move;

[Tooltip("Rotação Q/E (1D Axis)")]
public InputActionReference rotate;

[Tooltip("Zoom com scroll do mouse (Axis)")]
public InputActionReference zoom;

[Tooltip("Posição do ponteiro na tela (Vector2)")]
public InputActionReference pointerPos;

[Tooltip("Delta de movimento do ponteiro (Vector2)")]
public InputActionReference pointerDelta;

[Tooltip("Botão do meio do mouse (Button)")]
public InputActionReference middleButton;
```

**Valores do Screenshot:**
- `move`: **Camera/Move (Input Action Reference)**
- `rotate`: **Camera/Rotate (Input Action Reference)**
- `zoom`: **Camera/Zoom (Input Action Reference)**
- `pointerPos`: **Camera/PointerPosition (Input Action Reference)**
- `pointerDelta`: **Camera/PointerDelta (Input Action Reference)**
- `middleButton`: **Camera/MiddleButton (Input Action Reference)**

---

### 5.3 Mapeamentos de Input

**Baseado no JSON fornecido:**

#### **Camera/Move (WASD/Arrows)**

```json
{
    "name": "Move",
    "type": "Value",
    "expectedControlType": "Vector2",
    "bindings": [
        "WASD Composite":
            "W": up,
            "S": down,
            "A": left,
            "D": right,
        "Arrow Composite":
            "↑": up,
            "↓": down,
            "←": left,
            "→": right
    ]
}
```

**Saída:** `Vector2` (-1..1, -1..1)

---

#### **Camera/Rotate (Q/E)**

```json
{
    "name": "Rotate",
    "type": "Value",
    "expectedControlType": "Axis",
    "bindings": [
        "1D Axis Composite":
            "Q": Negative (-1),
            "E": Positive (+1)
    ]
}
```

**Saída:** `float` (-1, 0, +1)

---

#### **Camera/Zoom (Scroll)**

```json
{
    "name": "Zoom",
    "type": "Value",
    "expectedControlType": "Axis",
    "bindings": [
        "<Mouse>/scroll/y"
    ]
}
```

**Saída:** `float` (-1..1, tipicamente -120 ou +120 em valores brutos)

---

#### **Camera/PointerPosition**

```json
{
    "name": "PointerPosition",
    "type": "Value",
    "expectedControlType": "Vector2",
    "bindings": [
        "<Mouse>/position"
    ]
}
```

**Saída:** `Vector2` (pixels na tela)

---

#### **Camera/PointerDelta**

```json
{
    "name": "PointerDelta",
    "type": "Value",
    "expectedControlType": "Vector2",
    "bindings": [
        "<Mouse>/delta"
    ]
}
```

**Saída:** `Vector2` (delta de movimento por frame)

---

#### **Camera/MiddleButton**

```json
{
    "name": "MiddleButton",
    "type": "Button",
    "bindings": [
        "<Mouse>/middleButton"
    ]
}
```

**Saída:** `bool` (pressed = true)

---

### 5.4 Fluxo de Tradução

```
Unity Input System
        │
        ▼
  InputActionReference (6 ações)
        │
        ▼
RTSCameraInputSystem (traduz)
        │
        ├─ Read() → Vector2
        ├─ ReadFloat() → float
        ├─ OnMiddle() → bool
        └─ EventSystem → bool (pointer over UI)
        │
        ▼
  TickInput() (API unificada)
        │
        ▼
RTSCameraController (processa)
```

**Código:**

```csharp
void Update()
{
    if (_cam == null) return;

    // Ler inputs
    Vector2 wasd = Read(move);
    float rot = ReadFloat(rotate);
    float zm = ReadFloat(zoom);
    Vector2 pos = Read(pointerPos);
    Vector2 del = _middleHeld ? Read(pointerDelta) : Vector2.zero;

    // Detectar se o ponteiro está sobre UI
    bool overUI = EventSystem.current != null
                  && EventSystem.current.IsPointerOverGameObject();

    // Repassar para o controlador
    _cam.TickInput(
        wasdMove: wasd,
        pointerPosition: pos,
        isMiddleDragging: _middleHeld,
        pointerDelta: del,
        rotateAxis: rot,
        zoomAxis: zm,
        pointerOverUI: overUI
    );
}
```

---

### 5.5 Configuração no Unity

**Passo a Passo:**

1. **Criar Input Actions Asset:**
   - Project → Create → Input Actions
   - Nomear: `GameInputActions.inputactions`

2. **Configurar Action Map "Camera":**
   - Adicionar ações: Move, Rotate, Zoom, PointerPosition, PointerDelta, MiddleButton
   - Configurar bindings (WASD, Q/E, Scroll, etc.)

3. **Arrastar para Inspector:**
   - Selecionar GameObject com `RTSCameraInputSystem`
   - Arrastar cada ação para o campo correspondente no Inspector

4. **Testar:**
   - Play mode
   - Verificar input funcionando

**Troubleshooting:**
- ❌ **Input não funciona:** Verificar se Input System package está instalado
- ❌ **Ações null:** Verificar se arrastate do .inputactions para o Inspector
- ❌ **WASD não move:** Verificar se `enableWASD` está ativado no Profile

---

## 6) FLUXO DE EXECUÇÃO COMPLETO

### 6.1 Setup Inicial (Awake/OnEnable)

```
Unity Scene Load
       │
       ▼
RTSCameraController.Awake()
       │
       ├──▶ EnsureSetup()
       │    ├─ Criar Pivot (se null)
       │    ├─ Criar vcam (se null)
       │    ├─ Adicionar CinemachineFollow
       │    ├─ Adicionar CinemachineRotationComposer
       │    └─ Adicionar CinemachineBasicMultiChannelPerlin
       │
       ├──▶ Validar Profile
       │    └─ Se null → LogError
       │
       └──▶ Aplicar Initial Setup
            ├─ transform.position = initialPosition
            ├─ transform.rotation = Quaternion.Euler(0, initialHeading, 0)
            ├─ _zoom = initialZoom
            ├─ ApplyZoomAndTilt()
            └─ UpdateFollowOffset()
       │
       ▼
RTSCameraController.OnEnable()
       │
       └──▶ Subscrever 7 eventos de GameEvents
            ├─ OnCameraShake
            ├─ OnCameraFocus
            ├─ OnCameraFocusXZ
            ├─ OnCutsceneStart
            ├─ OnCutsceneEnd
            ├─ OnMinimapPing
            └─ OnSelectionFocus
       │
       ▼
RTSCameraInputSystem.OnEnable()
       │
       └──▶ Enable Input Actions (6 ações)
            ├─ move.action.Enable()
            ├─ rotate.action.Enable()
            ├─ zoom.action.Enable()
            ├─ pointerPos.action.Enable()
            ├─ pointerDelta.action.Enable()
            └─ middleButton.action.Enable()
       │
       ▼
Jogo começa (Update Loop)
```

---

### 6.2 Input Loop (Update)

```
RTSCameraInputSystem.Update() (cada frame)
       │
       ├──▶ Read Input Actions
       │    ├─ wasd = Read(move)
       │    ├─ rot = ReadFloat(rotate)
       │    ├─ zm = ReadFloat(zoom)
       │    ├─ pos = Read(pointerPos)
       │    ├─ del = Read(pointerDelta) (se middle held)
       │    └─ overUI = EventSystem.IsPointerOverGameObject()
       │
       └──▶ _cam.TickInput(wasd, pos, _middleHeld, del, rot, zm, overUI)
              │
              ▼
RTSCameraController.TickInput()
       │
       ├──▶ pauseInput = profile.pauseWhenPointerOverUI && overUI
       │
       ├──▶ HandleZoom(zoomAxis, pauseInput)
       │    ├─ _zoom -= axis * zoomSpeed
       │    ├─ _zoom = ApplyZoomCurve(_zoom)
       │    └─ ApplyZoomAndTilt()
       │         ├─ _cachedHeight = Lerp(minHeight, maxHeight, _zoom)
       │         └─ _cachedTilt = Lerp(minTilt, maxTilt, _zoom)
       │
       ├──▶ HandleRotate(rotateAxis, pauseInput)
       │    └─ transform.Rotate(Vector3.up, axis * rotateSpeed * dt)
       │
       ├──▶ HandlePan(wasdMove, mousePos, dragging, delta, pauseInput)
       │    ├─ WASD: move += forward * y + right * x
       │    ├─ Edge: move += EdgePanDirection(mousePos)
       │    ├─ Drag: move += -right * deltaX - forward * deltaY
       │    └─ transform.position += move * GetPanSpeed(_zoom) * dt
       │
       ├──▶ ClampToBounds()
       │    └─ transform.position = CameraMath.ClampToBounds(...)
       │
       └──▶ UpdateFollowOffset()
            └─ _follow.FollowOffset = CameraMath.TiltToOffset(...)
```

---

### 6.3 Event Handlers (via GameEvents)

**Exemplo: Shake de Combate**

```
Sistema de Combate (explosão)
       │
       │ GameEvents.RaiseCameraShake(2.0f, 3.5f, 0.4f)
       ▼
GameEvents.OnCameraShake?.Invoke(2.0f, 3.5f, 0.4f)
       │
       ▼
RTSCameraController.HandleCameraShake(2.0f, 3.5f, 0.4f)
       │
       │ StartCoroutine(CoShake(2.0f, 3.5f, 0.4f))
       ▼
CoShake()
  ├─ _noise.AmplitudeGain = 2.0f
  ├─ _noise.FrequencyGain = 3.5f
  ├─ yield return null (0.4s)
  ├─ _noise.AmplitudeGain = 0
  └─ _noise.FrequencyGain = 0
       │
       ▼
Câmera treme por 0.4 segundos
```

---

**Exemplo: Foco de Minimap**

```
UI de Minimap (clique)
       │
       │ GameEvents.RaiseMinimapPing(new Vector2(250, 350))
       ▼
GameEvents.OnMinimapPing?.Invoke(new Vector2(250, 350))
       │
       ▼
RTSCameraController.HandleMinimapPing(Vector2(250, 350))
       │
       │ GoToXZ(new Vector2(250, 350), snap: false, duration: 0.5f)
       ▼
GoToXZ()
  ├─ Vector3 target = CameraMath.XZToVector3(xz, transform.position.y)
  ├─ target = CameraMath.ClampToBounds(target, boundsCenter, boundsSize)
  └─ StartCoroutine(CoGoTo(target, 0.5f))
       │
       ▼
CoGoTo()
  ├─ Lerp com SmoothStep (ease in-out)
  └─ transform.position = CameraMath.SmoothLerp(start, target, t)
       │
       ▼
Câmera move suavemente para posição do clique (0.5s)
```

---

## 7) INTEGRAÇÃO COM OUTROS MÓDULOS

### 7.1 Dependências do Lote 1 (GameEvents)

**Eventos que a Câmera ESCUTA:**

| Evento | Emissor Típico | Ação da Câmera |
|--------|---------------|----------------|
| `OnCameraShake` | Sistema de Combate | Shake com amplitude/frequency/duration |
| `OnCameraFocus` | Triggers de Missão | GoTo() para posição 3D |
| `OnCameraFocusXZ` | Triggers de Missão | GoToXZ() para posição XZ |
| `OnCutsceneStart` | Sistema de Diálogo | StartCutscene() focando NPC |
| `OnCutsceneEnd` | Sistema de Diálogo | EndCutscene() voltando ao gameplay |
| `OnMinimapPing` | UI de Minimap | GoToXZ() suave (0.5s) |
| `OnSelectionFocus` | Unit.SetSelected() | GoTo() suave (0.3s) quando unidade selecionada |

**Código de Integração:**

```csharp
void OnEnable()
{
    // Subscrever eventos do Lote 1
    GameEvents.OnCameraShake += HandleCameraShake;
    GameEvents.OnCameraFocus += HandleCameraFocus;
    GameEvents.OnCameraFocusXZ += HandleCameraFocusXZ;
    GameEvents.OnCutsceneStart += HandleCutsceneStart;
    GameEvents.OnCutsceneEnd += HandleCutsceneEnd;
    GameEvents.OnMinimapPing += HandleMinimapPing;
    GameEvents.OnSelectionFocus += HandleSelectionFocus;
}

void OnDisable()
{
    // CRÍTICO: Desinscrever para evitar memory leaks
    GameEvents.OnCameraShake -= HandleCameraShake;
    GameEvents.OnCameraFocus -= HandleCameraFocus;
    GameEvents.OnCameraFocusXZ -= HandleCameraFocusXZ;
    GameEvents.OnCutsceneStart -= HandleCutsceneStart;
    GameEvents.OnCutsceneEnd -= HandleCutsceneEnd;
    GameEvents.OnMinimapPing -= HandleMinimapPing;
    GameEvents.OnSelectionFocus -= HandleSelectionFocus;
}
```

---

### 7.2 Futuro: UI/Minimap, Combate, Diálogo

**Funcionalidades Implementadas mas Não Usadas:**

> "Sim! Mas a maioria já está implementada só não utilizada, por exemplo o cameraShake, o cutscenestart, o ping do minimapa, o selectionfocus... várias estão prontas só não utilizadas ainda, pois ainda não fiz o código delas. Pode pontuar isso na documentação inclusive. Mais pra frente eu quero implementá-las, porém estou indo por partes."
> 
> — Equipe de Desenvolvimento

#### **CameraShake (⏳ Aguardando Sistema de Combate)**

**Status:** ✅ Implementado e testável  
**Aguardando:** Sistema de combate/explosões

**Como usar quando pronto:**
```csharp
// Em CombatSystem.cs (futuro)
void OnExplosion(Vector3 pos, float radius)
{
    float intensity = Mathf.Clamp(radius / 10f, 0.5f, 3.0f);
    GameEvents.RaiseCameraShake(
        amplitude: intensity,
        frequency: 3.5f,
        duration: 0.3f
    );
}
```

---

#### **Cutscenes (⏳ Aguardando Sistema de Diálogo)**

**Status:** ✅ Implementado e testável  
**Aguardando:** Sistema de diálogo/narrativa

**Como usar quando pronto:**
```csharp
// Em DialogueSystem.cs (futuro)
void StartDialogue(NPC npc)
{
    GameEvents.RaiseCutsceneStart(
        target: npc.headTransform,
        fov: 45f,
        priority: 100
    );
    
    // Mostrar caixa de diálogo...
}

void EndDialogue()
{
    GameEvents.RaiseCutsceneEnd();
}
```

---

#### **MinimapPing (⏳ Aguardando UI de Minimap)**

**Status:** ✅ Implementado e testável  
**Aguardando:** UI de minimap

**Como usar quando pronto:**
```csharp
// Em MinimapUI.cs (futuro)
void OnMinimapClicked(Vector2 screenPos)
{
    Vector2 worldXZ = ScreenToWorldXZ(screenPos);
    GameEvents.RaiseMinimapPing(worldXZ);
    
    // Mostrar ping visual no minimap...
}
```

---

#### **SelectionFocus (⏳ Aguardando Refino de Seleção)**

**Status:** ✅ Implementado mas opcional  
**Aguardando:** Decisão de UX (sempre focar unidade selecionada?)

**Como usar se ativado:**
```csharp
// Em Unit.cs (futuro, se desejado)
public void SetSelected(bool value)
{
    if (IsSelected == value) return;
    IsSelected = value;
    
    if (value) // Apenas quando selecionado
    {
        GameEvents.RaiseSelectionFocus(transform);
    }
}
```

---

## 8) CONFIGURAÇÃO NA CENA

### 8.1 Hierarquia Típica

**Screenshot de Referência:**

![Hierarquia RTS Camera Rig](reference://1761268100915_image.png)

**Estrutura Recomendada:**

```
RTS Camera Rig (GameObject)
├─ RTSCameraCinemachineV3Controller (Script)
├─ RTSCameraInputSystem (Script)
│
├─ Pivot (Transform) ← Auto-criado
│
└─ RTS_Camera (CM3) (GameObject) ← Auto-criado
   └─ CinemachineCamera (Component)
      ├─ CinemachineFollow (Body)
      ├─ CinemachineRotationComposer (Aim)
      └─ CinemachineBasicMultiChannelPerlin (Noise)
```

**Passo a Passo:**

1. **Criar GameObject Raiz:**
   - Hierarchy → Create Empty
   - Nome: `RTS Camera Rig`
   - Position: (0, 0, 0)

2. **Adicionar Componentes:**
   - Add Component → `RTSCameraCinemachineV3Controller`
   - Add Component → `RTSCameraInputSystem`

3. **Configurar Controller:**
   - Profile: Arraste `CameraProfile.asset`
   - Vcam/Pivot: Deixar null (auto-criado)
   - Bounds: Configurar ou usar "Set Bounds From Terrain(s)"
   - Initial Position/Heading/Zoom: Ajustar para cena

4. **Configurar Input System:**
   - Arraste Input Actions do .inputactions para campos

5. **Play Mode:**
   - Verificar se Pivot e vcam foram criados
   - Testar WASD, Zoom, Rotate

---

### 8.2 Configuração de Bounds

#### **Método 1: Manual**

**No Inspector do Controller:**
```
Bounds Center: (500, 500) ← Centro da área jogável
Bounds Size: (1000, 1000) ← Tamanho da área
```

**Visualização:**

![Gizmo de Bounds](reference://1761268245880_image.png)

- Retângulo verde = Área válida
- Ajustar size/center até cobrir área desejada

---

#### **Método 2: Automático (Terrenos)**

**Passo a Passo:**

1. Adicionar terrenos à cena
2. Selecionar `RTS Camera Rig`
3. Botão direito no componente `RTSCameraCinemachineV3Controller`
4. Clicar "Set Bounds From Terrain(s)"
5. Bounds calculados automaticamente

**Código Executado:**
```csharp
[ContextMenu("Set Bounds From Terrain(s)")]
public void SetBoundsFromTerrains()
{
    if (CameraMath.CalculateTerrainBounds(
        Terrain.activeTerrains,
        out Vector2 center,
        out Vector2 size))
    {
        boundsCenter = center;
        boundsSize = size;
        Debug.Log($"[RTSCamera] Bounds atualizados: Center={center}, Size={size}");
    }
}
```

---

### 8.3 Input Actions Setup

**Screenshot de Referência:**

![Input Actions Editor](reference://1761268218092_image.png)

**Estrutura no Editor:**

```
Action Maps:
├─ Selection (para outro módulo)
└─ Camera
   ├─ Move (Vector2 - WASD Composite)
   ├─ Rotate (Axis - Q/E Composite)
   ├─ Zoom (Axis - Mouse Scroll Y)
   ├─ PointerPosition (Vector2 - Mouse Position)
   ├─ PointerDelta (Vector2 - Mouse Delta)
   └─ MiddleButton (Button - Mouse Middle)
```

**Configuração Detalhada:**

**Camera/Move:**
- Type: Value (Vector2)
- Control Type: Vector 2
- Binding: Dpad Composite
  - Up: W, ↑
  - Down: S, ↓
  - Left: A, ←
  - Right: D, →

**Camera/Rotate:**
- Type: Value (Axis)
- Control Type: Axis
- Binding: 1D Axis Composite
  - Negative: Q
  - Positive: E

**Camera/Zoom:**
- Type: Value (Axis)
- Control Type: Axis
- Binding: `<Mouse>/scroll/y`

**Camera/PointerPosition:**
- Type: Value (Vector2)
- Control Type: Vector 2
- Binding: `<Mouse>/position`

**Camera/PointerDelta:**
- Type: Value (Vector2)
- Control Type: Vector 2
- Binding: `<Mouse>/delta`

**Camera/MiddleButton:**
- Type: Button
- Binding: `<Mouse>/middleButton`

---

## 9) EXEMPLOS DE USO AVANÇADOS

### 9.1 Criar Perfil Customizado

**Cenário:** Criar um perfil "Cinematográfico" para cutscenes.

**Passo a Passo:**

1. **Criar Asset:**
   - Project → Create → Game → Camera Profile
   - Nome: `CameraProfile_Cinematic`

2. **Configurar Valores:**
```
Pan Speed Near: 5
Pan Speed Far: 10
Edge Thickness: 0 (desabilitado)
Zoom Speed: 0.05
Min Height: 5
Max Height: 30
Min Tilt: 45°
Max Tilt: 65°
Rotate Speed: 30°/s

Flags:
enableWASD: ❌ Disabled
enableEdgePan: ❌ Disabled
enableMiddleDrag: ❌ Disabled
enableRotate: ❌ Disabled
pauseWhenPointerOverUI: ✅ Enabled
```

3. **Usar em Runtime:**
```csharp
public class CutsceneController : MonoBehaviour
{
    public RTSCameraCinemachineV3Controller camera;
    public RTSCameraProfile gameplayProfile;
    public RTSCameraProfile cinematicProfile;

    void StartCutscene()
    {
        camera.profile = cinematicProfile; // Trocar perfil
        // Câmera agora usa configurações cinemáticas
    }

    void EndCutscene()
    {
        camera.profile = gameplayProfile; // Voltar ao normal
    }
}
```

---

### 9.2 Disparar Shake por Script

**Cenário:** Terremoto quando boss entra em cena.

**Código:**

```csharp
public class BossIntro : MonoBehaviour
{
    void OnBossEnter()
    {
        // Shake forte e longo
        GameEvents.RaiseCameraShake(
            amplitude: 2.5f,  // Muito intenso
            frequency: 4.0f,  // Oscilação rápida
            duration: 1.5f    // 1.5 segundos
        );

        // OU direto no controller:
        // cameraController.PlayShake(2.5f, 4.0f, 1.5f);
    }
}
```

---

### 9.3 Focar Câmera em Posição

**Cenário:** Tutorial mostrando diferentes áreas do mapa.

**Código:**

```csharp
public class TutorialManager : MonoBehaviour
{
    public Transform[] tutorialPoints;
    int currentPoint = 0;

    void ShowNextPoint()
    {
        if (currentPoint >= tutorialPoints.Length) return;

        Transform point = tutorialPoints[currentPoint];
        
        // Foco suave com duração customizada
        GameEvents.RaiseCameraFocus(
            worldPos: point.position,
            snap: false,
            duration: 1.0f // 1 segundo de movimento
        );

        // Mostrar texto de tutorial...
        
        currentPoint++;
    }
}
```

---

### 9.4 Iniciar Cutscene

**Cenário:** Diálogo com NPC após missão.

**Código:**

```csharp
public class QuestDialogue : MonoBehaviour
{
    public NPC questGiver;

    void OnQuestCompleted()
    {
        // Iniciar cutscene focando no NPC
        GameEvents.RaiseCutsceneStart(
            target: questGiver.headTransform, // Foca na cabeça
            fov: 45f, // FOV fechado (close-up)
            priority: 100 // Alta prioridade
        );

        StartCoroutine(CoDialogue());
    }

    IEnumerator CoDialogue()
    {
        // Mostrar caixas de diálogo...
        yield return ShowDialogue("Obrigado, herói!");
        yield return new WaitForSeconds(2f);
        yield return ShowDialogue("Aqui está sua recompensa.");
        
        // Finalizar cutscene
        GameEvents.RaiseCutsceneEnd();
    }
}
```

---

## 10) FUNCIONALIDADES FUTURAS (ROADMAP)

### 10.1 Visão Geral

> "A resposta rápida para essa questão é justamente as funções criadas e nunca utilizadas. Elas já estão lá justamente para eu utilizar em algum momento, porém ainda não implementei o todo."
> 
> — Equipe de Desenvolvimento

**Filosofia:**
- ✅ **API completa implementada** (shake, cutscenes, foco, etc.)
- ⏳ **Aguardando sistemas consumidores** (combate, UI, diálogo)
- 🎯 **Implementação incremental** por módulo

---

### 10.2 CameraShake (⏳ Aguardando Combate)

**Status:** ✅ **100% Implementado e Testável**

**O que está pronto:**
- `PlayShake(amplitude, frequency, duration)`
- Evento `GameEvents.OnCameraShake`
- Handler `HandleCameraShake()`
- Coroutine `CoShake()` usando Cinemachine Noise

**O que falta:**
- Sistema de combate para disparar shakes
- Balanceamento de intensidades por tipo de ataque

**Quando será usado:**
- Explosões de catapulta
- Impactos de siege weapons
- Habilidades especiais de heróis
- Terremoto de boss

**Exemplo Futuro:**
```csharp
// Em CombatSystem.cs (futuro)
void OnProjectileImpact(Projectile proj)
{
    float intensity = proj.damage / 100f; // Proporcional ao dano
    GameEvents.RaiseCameraShake(
        amplitude: Mathf.Clamp(intensity, 0.5f, 3.0f),
        frequency: 3.5f,
        duration: 0.3f
    );
}
```

---

### 10.3 Cutscenes (⏳ Aguardando Diálogo)

**Status:** ✅ **100% Implementado e Testável**

**O que está pronto:**
- `StartCutscene(target, fov, priority)`
- `EndCutscene()`
- Eventos `GameEvents.OnCutsceneStart/End`
- Câmera secundária com Cinemachine blending

**O que falta:**
- Sistema de diálogo para controlar cutscenes
- Timeline integration (opcional)

**Quando será usado:**
- Diálogos com NPCs importantes
- Cinematics de início/fim de missão
- Eventos narrativos (traição, revelação, etc.)

**Exemplo Futuro:**
```csharp
// Em DialogueSystem.cs (futuro)
IEnumerator PlayDialogueSequence(DialogueData data)
{
    foreach (var line in data.lines)
    {
        // Foco no NPC falando
        GameEvents.RaiseCutsceneStart(
            target: line.speaker.headTransform,
            fov: line.isCloseUp ? 40f : 50f,
            priority: 100
        );

        yield return ShowDialogue(line.text);
        yield return new WaitForSeconds(line.duration);
    }

    GameEvents.RaiseCutsceneEnd();
}
```

---

### 10.4 MinimapPing (⏳ Aguardando UI)

**Status:** ✅ **100% Implementado e Testável**

**O que está pronto:**
- `GoToXZ()` com movimento suave
- Evento `GameEvents.OnMinimapPing`
- Handler `HandleMinimapPing()` com duração de 0.5s

**O que falta:**
- UI de minimap
- Conversão de coordenadas tela → mundo

**Quando será usado:**
- Clique no minimap move câmera
- Ping de aliados (multiplayer)
- Alertas de ataque (defesa sob ataque!)

**Exemplo Futuro:**
```csharp
// Em MinimapUI.cs (futuro)
void OnMinimapClicked(Vector2 screenPos)
{
    // Converter coordenada do minimap para mundo
    Vector2 worldXZ = MinimapToWorld(screenPos);
    
    // Câmera move automaticamente
    GameEvents.RaiseMinimapPing(worldXZ);
    
    // Mostrar ping visual no minimap
    SpawnPingEffect(screenPos);
}
```

---

### 10.5 SelectionFocus (⏳ Aguardando Refino)

**Status:** ✅ **100% Implementado mas Opcional**

**O que está pronto:**
- `GoTo(transform.position)` com movimento suave
- Evento `GameEvents.OnSelectionFocus`
- Handler `HandleSelectionFocus()` com duração de 0.3s

**O que falta:**
- Decisão de UX: sempre focar unidade selecionada?
- Configuração por tipo de unidade (herói sim, worker não)

**Quando será usado (se ativado):**
- Duplo-clique em unidade foca nela
- Seleção de herói foca automaticamente
- Hotkey "centralizar seleção" (F1-F5)

**Exemplo Futuro:**
```csharp
// Em SelectionManager.cs (opcional)
void HandleDoubleClickUnit(Unit unit)
{
    // Selecionar unidade...
    SelectExactly(unit);
    
    // Focar câmera (opcional)
    if (unit.def.type == UnitType.Hero) // Apenas heróis
    {
        GameEvents.RaiseSelectionFocus(unit.transform);
    }
}
```

---

### 10.6 Outras Funcionalidades Planejadas

**Edge Scrolling Acceleration:**
- Quanto mais perto da borda, mais rápido o pan
- Curva de aceleração configurável

**Zoom to Cursor:**
- Zoom centralizado na posição do mouse
- Similar a Google Maps

**Camera Smoothing:**
- Damping configurável para movimentos
- Evitar "sacudir" em terrenos irregulares

**Multiple Camera Presets:**
- Hotkeys para trocar entre perfis (1-4)
- Ex: 1=Gameplay, 2=Estratégico, 3=Tático, 4=Livre

---

## 11) TROUBLESHOOTING E FAQ

### 11.1 Câmera não se move

**Sintomas:**
- WASD não funciona
- Edge pan não funciona
- Middle drag não funciona

**Soluções:**

1. **Verificar Profile:**
   ```csharp
   if (profile == null)
       Debug.LogError("Profile não atribuído!");
   ```
   - Solução: Arrastar `CameraProfile.asset` para campo `profile`

2. **Verificar Flags:**
   - `enableWASD` está ativado?
   - `enableEdgePan` está ativado?
   - `enableMiddleDrag` está ativado?
   - Solução: Ativar flags no Profile Inspector

3. **Verificar Input Actions:**
   - Input Actions estão atribuídos no `RTSCameraInputSystem`?
   - Ações estão habilitadas? (should auto-enable no OnEnable)
   - Solução: Arrastar ações do .inputactions para campos

4. **Verificar Ponteiro sobre UI:**
   - `pauseWhenPointerOverUI` ativado?
   - Mouse está sobre UI (botão, painel, etc.)?
   - Solução: Mover mouse para fora da UI ou desativar flag

---

### 11.2 Bounds não funcionam

**Sintomas:**
- Câmera sai do mapa
- Câmera não é limitada

**Soluções:**

1. **Verificar Bounds no Inspector:**
   ```
   Bounds Center: (500, 500) ← Deve estar no centro do mapa
   Bounds Size: (1000, 1000) ← Deve cobrir área jogável
   ```

2. **Usar "Set Bounds From Terrain(s)":**
   - Botão direito no componente → "Set Bounds From Terrain(s)"
   - Verifica se terrenos estão na cena

3. **Visualizar Gizmo:**
   - Selecionar RTS Camera Rig
   - Scene view deve mostrar retângulo verde
   - Ajustar bounds até cobrir área correta

---

### 11.3 Input não responde

**Sintomas:**
- Nenhum input funciona
- Console mostra erros de Input System

**Soluções:**

1. **Instalar Input System Package:**
   - Window → Package Manager
   - Search: "Input System"
   - Install/Update

2. **Verificar Input Actions Asset:**
   - Arquivo .inputactions existe?
   - Action Map "Camera" existe?
   - 6 ações configuradas?

3. **Verificar References:**
   - `RTSCameraInputSystem` tem todas as 6 referencias?
   - Arrastar novamente do .inputactions

4. **Restart Unity:**
   - Input System às vezes requer restart após instalação

---

### 11.4 Zoom inverte

**Sintomas:**
- Scroll up aproxima ao invés de afastar
- Scroll down afasta ao invés de aproximar

**Solução:**

**Inverter lógica no código:**
```csharp
// Antes:
_zoom = Mathf.Clamp01(_zoom - axis * profile.zoomSpeed);

// Depois (invertido):
_zoom = Mathf.Clamp01(_zoom + axis * profile.zoomSpeed);
```

**OU inverter no Input Action:**
- Processar: `Invert`

---

### 11.5 Shake não funciona

**Sintomas:**
- Chamar `PlayShake()` não treme câmera
- Nenhum efeito visível

**Soluções:**

1. **Verificar Cinemachine Noise:**
   ```csharp
   if (_noise == null)
       Debug.LogError("CinemachineBasicMultiChannelPerlin não encontrado!");
   ```

2. **Verificar Noise Profile:**
   - vcam → Noise → Noise Profile
   - Deve ter um profile (ex: "6D Shake")
   - Solução: Adicionar profile do Cinemachine Samples

3. **Valores muito baixos:**
   - Amplitude < 0.5 pode ser imperceptível
   - Tentar valores maiores (1.5-3.0)

---

## 12) TABELA DE RELACIONAMENTOS COMPLETA

### 12.1 Classes do Lote 2

| Classe | Tipo | Depende De | Dependentes | Eventos (Listen) |
|--------|------|-----------|-------------|------------------|
| **RTSCameraProfile** | SO | - | Controller | - |
| **CameraMath** | static | - | Controller | - |
| **RTSCameraController** | MB | Profile, CameraMath, Cinemachine, GameEvents | InputSystem | OnCameraShake, OnCameraFocus, OnCameraFocusXZ, OnCutsceneStart, OnCutsceneEnd, OnMinimapPing, OnSelectionFocus |
| **RTSCameraInputSystem** | MB | Controller, InputActionReferences | - | - |

---

### 12.2 Integrações com Outros Lotes

| Lote Consumidor | Usa do Lote 2 | Forma de Uso |
|-----------------|---------------|--------------|
| **Lote 1 - GameEvents** | Controller escuta eventos | Subscribe em OnEnable |
| **Lote 3 - Unit** (futuro) | SelectionFocus | GameEvents.RaiseSelectionFocus(transform) |
| **Lote 4 - Selection** (futuro) | SelectionFocus | GameEvents.RaiseSelectionFocus(transform) |
| **UI - Minimap** (futuro) | MinimapPing | GameEvents.RaiseMinimapPing(worldXZ) |
| **Combat System** (futuro) | Shake | GameEvents.RaiseCameraShake(...) |
| **Dialogue System** (futuro) | Cutscenes | GameEvents.RaiseCutsceneStart/End(...) |

---

### 12.3 Fluxo de Dados Completo

```
Unity Input System
        │
        ▼
InputActionReferences (6 ações)
        │
        ▼
RTSCameraInputSystem (traduz)
        │
        ▼
RTSCameraController.TickInput() (processa)
        │
        ├──▶ HandlePan() → transform.position
        ├──▶ HandleZoom() → _zoom → ApplyZoomAndTilt()
        ├──▶ HandleRotate() → transform.rotation
        ├──▶ ClampToBounds() → CameraMath
        └──▶ UpdateFollowOffset() → CameraMath → _follow.FollowOffset
        │
        ▼
CinemachineFollow (Body)
        │
        ▼
Unity Camera (renderiza)
        │
        ▼
Tela do Jogador
```

---

## 13) REFERÊNCIAS RÁPIDAS

### 13.1 Atalhos de Código

**Mover câmera para posição:**
```csharp
GameEvents.RaiseCameraFocus(worldPos, snap: false, duration: 0.5f);
```

**Tremor de câmera:**
```csharp
GameEvents.RaiseCameraShake(amplitude: 1.5f, frequency: 3.0f, duration: 0.3f);
```

**Iniciar cutscene:**
```csharp
GameEvents.RaiseCutsceneStart(npc.transform, fov: 45f, priority: 100);
```

**Finalizar cutscene:**
```csharp
GameEvents.RaiseCutsceneEnd();
```

---

### 13.2 Valores Típicos

| Parâmetro | Min | Típico | Max | Descrição |
|-----------|-----|--------|-----|-----------|
| Pan Speed Near | 10 | 20 | 40 | Velocidade próxima |
| Pan Speed Far | 20 | 35 | 80 | Velocidade distante |
| Zoom Speed | 0.05 | 0.15 | 0.3 | Sensibilidade scroll |
| Min Height | 5 | 10 | 20 | Altura mínima |
| Max Height | 30 | 60 | 100 | Altura máxima |
| Min Tilt | 25° | 35° | 45° | Ângulo plano |
| Max Tilt | 65° | 75° | 85° | Ângulo inclinado |
| Rotate Speed | 30°/s | 90°/s | 180°/s | Rotação |
| Shake Amplitude | 0.5 | 1.2 | 3.0 | Intensidade |
| Shake Frequency | 1.0 | 2.5 | 5.0 | Oscilação |
| Shake Duration | 0.1s | 0.3s | 1.0s | Tempo |

---

### 13.3 Input Actions (Resumo)

| Ação | Tipo | Binding | Saída |
|------|------|---------|-------|
| Move | Vector2 | WASD/Arrows | (-1..1, -1..1) |
| Rotate | Axis | Q/E | (-1, 0, +1) |
| Zoom | Axis | Scroll Y | (-1..1) |
| PointerPosition | Vector2 | Mouse Position | (pixels) |
| PointerDelta | Vector2 | Mouse Delta | (delta) |
| MiddleButton | Button | Mouse Middle | (bool) |

---

### 13.4 Estrutura de Arquivos

```
Assets/
├── Scripts/
│   └── Camera/
│       ├── RTSCameraProfile.cs
│       ├── CameraMath.cs
│       ├── RTSCameraCinemachineV3Controller.cs
│       └── RTSCameraInputSystem.cs
│
├── Settings/
│   └── Camera/
│       ├── CameraProfile.asset (Default)
│       ├── CameraProfile_Cinematic.asset
│       └── CameraProfile_Spectator.asset
│
└── Input/
    └── GameInputActions.inputactions
```

---

## 14) CONCLUSÃO

### 14.1 Resumo do Lote 2

O **Lote 2 - Câmera System RTS** estabelece um **sistema de câmera profissional** para Medieval Thrones usando **Cinemachine v3**:

✅ **RTSCameraProfile**: Configuração centralizada e reutilizável em ScriptableObjects  
✅ **CameraMath**: Lógica matemática pura, testável e reutilizável  
✅ **RTSCameraController**: Orquestrador principal com pan, zoom, rotate, shake, cutscenes  
✅ **RTSCameraInputSystem**: Tradutor de Unity Input System para API unificada  
✅ **Integração com GameEvents**: 7 eventos do Lote 1 totalmente integrados  
✅ **Funcionalidades Futuras**: API completa pronta, aguardando sistemas consumidores  

### 14.2 Qualidade da Arquitetura

**Pontos Fortes:**
- ✅ **Desacoplamento**: CameraMath separado, testável
- ✅ **Configurabilidade**: Múltiplos perfis reutilizáveis
- ✅ **Extensibilidade**: Fácil adicionar novos modos de câmera
- ✅ **Integração**: GameEvents permite comunicação com todos os módulos
- ✅ **Forward-Compatible**: API completa pronta para futuras features

**Padrões de Excelência:**
- ✅ ScriptableObjects para configuração
- ✅ Static utility classes para lógica pura
- ✅ Event-driven integration (GameEvents)
- ✅ Cinemachine v3 para qualidade visual
- ✅ Unity Input System para flexibilidade

### 14.3 Roadmap (Recap)

**Implementado e Testável:**
- ✅ Pan (WASD, Edge, Middle Drag)
- ✅ Zoom (Scroll com altura + tilt)
- ✅ Rotate (Q/E)
- ✅ Bounds (Clamp de posição)
- ✅ Shake (Cinemachine Noise)
- ✅ GoTo (Movimento suave)
- ✅ Cutscenes (Câmera cinemática)

**Aguardando Sistemas Consumidores:**
- ⏳ Shake → Sistema de Combate
- ⏳ Cutscenes → Sistema de Diálogo
- ⏳ MinimapPing → UI de Minimap
- ⏳ SelectionFocus → Refinamento de UX

### 14.4 Próximos Passos

**Lotes Subsequentes:**
- **Lote 3 - Unit**: Documentação atualizada (próxima iteração)
- **Lote 4 - Selection**: Documentação atualizada (próxima iteração)

**Novos Módulos (que usarão Câmera):**
- Sistema de Combate (shake)
- Sistema de Diálogo (cutscenes)
- UI de Minimap (ping)
- Sistema de Tutorial (focos)

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão do Documento:** 1.0  
**Compatibilidade:** Cinemachine v3, Unity Input System, Unity 2022.3+

---

## LOTE 3 — Módulo Unit (Unidades)

### Objetivo

Oferecer uma referência técnica clara, rastreável e reutilizável do módulo de **Unidades**, que gerencia entidades jogáveis/controláveis no RTS: dados base (HP, facção, definição), progressão (XP/Level), seleção visual, registro centralizado e consultas. O módulo utiliza o sistema centralizado de eventos `GameEvents` para comunicação desacoplada com outros sistemas.

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Responsabilidades Principais

O **Módulo Unit** é responsável por:

1. **Representação de Entidades**: Cada unidade no jogo (trabalhadores, guerreiros, heróis) é um GameObject com componente `Unit`.
2. **Dados Base**: HP, facção proprietária, referência a definição (ScriptableObject).
3. **Progressão RPG**: Sistema de XP e Level com eventos centralizados.
4. **Seleção Visual**: Estado de seleção com highlight visual e eventos via `GameEvents`.
5. **Registro Centralizado**: `UnitRegistry` mantém lista global e por facção em runtime.
6. **Consultas Otimizadas**: `UnitQueries` oferece helpers para filtrar unidades (por jogador, por facção, etc.).
7. **Integração com Event Bus**: Todos os eventos são disparados via `GameEvents` para máximo desacoplamento.

### 1.2 Arquitetura do Sistema

```
UnitDefinition (ScriptableObject)
        ↓ (define tipo/ícone/nome)
      Unit (MonoBehaviour)
        ↓ (auto-registro)
  UnitRegistry (static)
        ↓ (dispara eventos via GameEvents)
   GameEvents (static event bus)
        ↑ (escutado por sistemas)
  Sistemas (UI, Minimap, IA, etc.)
```

**Fluxo de Vida de uma Unidade:**
1. GameObject spawna com componente `Unit`
2. `OnEnable()` → registra no `UnitRegistry`
3. `UnitRegistry` → dispara `GameEvents.OnUnitSpawned`
4. Durante gameplay → HP, XP, seleção são gerenciados
5. Mudanças de estado → disparam eventos via `GameEvents`
6. `OnDisable()` → remove do `UnitRegistry`
7. `UnitRegistry` → dispara `GameEvents.OnUnitDespawned`

### 1.3 Integração com Outros Módulos

- **Enums.cs**: Usa `FactionId` e `UnitType`
- **GameEvents**: Sistema centralizado de eventos (CRÍTICO para arquitetura)
- **PlayerController**: `UnitQueries` filtra unidades por jogador
- **UI Sistema**: Escuta `GameEvents.OnUnitProgressChanged`, `GameEvents.OnUnitSelectionChanged`
- **Sistema de Seleção**: Usa `SetSelected()`, escuta `GameEvents.OnUnitSelectionChanged`
- **Sistema de Combate** (futuro): Modificará `hp` e disparará morte

### 1.4 Padrão de Eventos (Pós-Refatoração)

**Arquitetura Unificada:**
- ✅ **Todos os eventos** do módulo Unit usam `GameEvents` (event bus centralizado)
- ✅ **Nenhum evento local** (removidos na refatoração)
- ✅ **Consistente** com módulos de Câmera, Tempo, Economia e Diplomacia

---

## 2) ESTRUTURA DAS CLASSES E RELACIONAMENTOS

### 2.1 Enums.cs (Relevantes ao Módulo)

**Arquivo/Classe:** `Enums` (enums globais)

**Conteúdo Relevante:**

```csharp
public enum FactionId { 
    Neutral = 0, 
    Player1 = 1, 
    Player2 = 2, 
    Player3 = 3, 
    Player4 = 4, 
    PvE = 3 
}

public enum UnitType { 
    Worker, 
    Warrior, 
    Spearman, 
    Archer, 
    CavalryLight, 
    Ram, 
    Catapult, 
    Hero 
}
```

**Relacionamentos:**
- `FactionId`: Usado em `Unit.owner`, `UnitRegistry` (índice de dicionário), `PlayerController.myFaction`
- `UnitType`: Armazenado em `UnitDefinition.type` para classificação de unidades

---

### 2.2 UnitDefinition.cs

**Tipo:** `ScriptableObject`

**Responsabilidade:** Armazenar metadados estáticos de um tipo de unidade (nome de exibição, tipo, ícone). Funciona como "template" ou "ficha" reutilizável.

**Principais Campos:**

```csharp
public string displayName;     // Nome exibido na UI
public UnitType type;          // Classificação da unidade
public Sprite icon;            // Ícone para UI/mini-mapa
```

**Relacionamentos:**
- Criado via: `Assets > Create > Game > Unit Definition`
- Referenciado por: `Unit.def`
- Consumido por: UI (nome/ícone), IA (comportamento por tipo), balanceamento

**Exemplo de Uso:**

```csharp
// Criar no Editor: Assets > Create > Game > Unit Definition
// Configurar:
// - displayName: "Guerreiro Élfico"
// - type: UnitType.Warrior
// - icon: [sprite do guerreiro]

// No Inspector do GameObject da unidade:
// Unit.def = [arrastar UnitDefinition_GuerreiroElfico]
```

---

### 2.3 Unit.cs

**Tipo:** `MonoBehaviour` (componente principal)

**Atributos:**
- `[DisallowMultipleComponent]` — previne duplicação acidental

**Responsabilidade:** 
Representa uma unidade individual no jogo, gerenciando:
- Estado vital (HP)
- Propriedade (facção)
- Progressão (XP/Level)
- Seleção visual
- Auto-registro no `UnitRegistry`
- **Comunicação via `GameEvents`** (pós-refatoração)

**Campos Públicos (Inspector):**

```csharp
[Header("Dados")]
public UnitDefinition def;              // ScriptableObject de definição
public FactionId owner = FactionId.Player1;  // Facção proprietária
public float hp = 100, hpMax = 100;     // Vida atual e máxima

[Header("Seleção (visuais)")]
[SerializeField] GameObject selectionHighlight; // Ring/outline de seleção

[Header("Progressão")]
[SerializeField] int level = 1;         // Nível atual
[SerializeField] float xp = 0f;         // XP atual

[Tooltip("XP base para upar do Lv 1→2")]
[SerializeField] float baseXpToLevel = 100f;

[Tooltip("Multiplicador por nível (ex.: 1.35 => 35% a mais por nível)")]
[SerializeField] float xpGrowth = 1.35f;
```

**Propriedades Públicas:**

```csharp
public bool IsSelected { get; private set; }  // Estado de seleção
public int Level => level;                     // Level (read-only)
public float Xp => xp;                         // XP atual (read-only)
public float XpToNext                          // XP necessário para próximo nível
    => baseXpToLevel * Mathf.Pow(xpGrowth, Mathf.Max(0, level - 1));
public float Xp01                              // Progresso 0..1 para UI
    => XpToNext <= 0f ? 0f : Mathf.Clamp01(xp / XpToNext);
public string DisplayName                      // Nome para UI
    => def ? def.displayName : gameObject.name;
```

**Métodos Públicos:**

```csharp
/// <summary>
/// Define estado de seleção e atualiza highlight visual.
/// Dispara GameEvents.OnUnitSelectionChanged.
/// </summary>
public void SetSelected(bool value)

/// <summary>
/// Adiciona XP e lida com múltiplos level-ups automáticos.
/// Dispara GameEvents.OnUnitProgressChanged.
/// </summary>
/// <param name="amount">Quantidade de XP a adicionar</param>
public void AddXp(float amount)

#if UNITY_EDITOR
/// <summary>
/// Método de debug para listar unidades registradas (apenas Editor).
/// </summary>
public void TestList()
#endif
```

**Ciclo de Vida:**

```csharp
private void OnEnable() => UnitRegistry.Register(this);
private void OnDisable() => UnitRegistry.Unregister(this);
```

**Eventos Disparados (via GameEvents):**

```csharp
// Em SetSelected():
GameEvents.RaiseUnitSelectionChanged(this, value);

// Em AddXp():
GameEvents.RaiseUnitProgressChanged(this);
```

**Relacionamentos:**
- Consome: `UnitDefinition`, `FactionId`, `UnitRegistry`, `GameEvents`
- Dispara eventos para: Qualquer sistema inscrito em `GameEvents`
- Gerenciado por: `UnitRegistry` (registro automático)

**Mudanças Pós-Refatoração:**
- ❌ **Removido:** `public event Action<Unit, bool> OnSelectionChanged;`
- ❌ **Removido:** `public event Action<Unit> OnProgressChanged;`
- ✅ **Adicionado:** Integração com `GameEvents`
- ✅ **Melhorado:** `TestList()` agora só compila em Editor (`#if UNITY_EDITOR`)

---

### 2.4 UnitRegistry.cs

**Tipo:** `static class` (singleton lógico, sem instância de MonoBehaviour)

**Responsabilidade:** 
Registro centralizado de todas as unidades ativas na cena, oferecendo:
- Lista global de unidades
- Listas segmentadas por facção
- **Disparo de eventos via `GameEvents`** (pós-refatoração)
- Acesso O(1) por facção

**Estruturas Internas:**

```csharp
static readonly List<Unit> _all = new();
static readonly Dictionary<FactionId, List<Unit>> _perFaction = new();
```

**Propriedades Públicas:**

```csharp
// Lista read-only de todas as unidades
public static IReadOnlyList<Unit> All => _all;

// Obter unidades de uma facção específica
public static IReadOnlyList<Unit> GetByFaction(FactionId f) =>
    _perFaction.TryGetValue(f, out var list) ? list : Array.Empty<Unit>();
```

**Métodos Públicos:**

```csharp
/// <summary>
/// Registra unidade. Chamado automaticamente por Unit.OnEnable().
/// Garante que não há duplicatas.
/// Dispara GameEvents.OnUnitSpawned.
/// </summary>
public static void Register(Unit u)

/// <summary>
/// Remove unidade do registro. Chamado automaticamente por Unit.OnDisable().
/// Dispara GameEvents.OnUnitDespawned.
/// </summary>
public static void Unregister(Unit u)
```

**Eventos Disparados (via GameEvents):**

```csharp
// Em Register():
GameEvents.RaiseUnitSpawned(u);

// Em Unregister():
GameEvents.RaiseUnitDespawned(u);
```

**Relacionamentos:**
- Consumido por: `Unit` (auto-registro), `UnitQueries`, IA, UI, Sistemas de Gerenciamento
- Dispara eventos para: Qualquer sistema inscrito em `GameEvents`

**Características Importantes:**
- ✅ **Thread-safe para leitura** (IReadOnlyList)
- ✅ **Prevenção de duplicatas** (Contains check)
- ✅ **Limpeza automática** (OnDisable)
- ⚠️ **Não persiste entre cenas** (static limpa em domain reload)

**Mudanças Pós-Refatoração:**
- ❌ **Removido:** `public static event Action<Unit> OnUnitSpawned;`
- ❌ **Removido:** `public static event Action<Unit> OnUnitDespawned;`
- ✅ **Adicionado:** Integração com `GameEvents`

---

### 2.5 UnitQueries.cs

**Tipo:** `static class` (biblioteca de utilitários)

**Responsabilidade:** 
Fornecer consultas otimizadas e helpers para filtrar/buscar unidades, reduzindo código boilerplate em sistemas que precisam de listas específicas.

**Métodos Públicos:**

```csharp
/// <summary>
/// Retorna unidades pertencentes a um jogador específico.
/// OTIMIZADO: Se source for null, usa diretamente GetByFaction (mais rápido).
/// </summary>
/// <param name="player">PlayerController alvo</param>
/// <param name="source">Lista opcional para evitar varrer cena. 
/// Se null, usa GetByFaction diretamente</param>
/// <returns>Enumerável de unidades filtradas</returns>
public static IEnumerable<Unit> GetUnitsForPlayer(
    PlayerController player, 
    IEnumerable<Unit> source = null
)

/// <summary>
/// Fallback: varre cena atrás de Units usando API otimizada 
/// (FindObjectsByType no Unity 2023+).
/// </summary>
/// <returns>Enumerável de todas as Units na cena</returns>
public static IEnumerable<Unit> EnumerateSceneUnits()
```

**Implementação Interna (GetUnitsForPlayer - OTIMIZADA):**

```csharp
public static IEnumerable<Unit> GetUnitsForPlayer(
    PlayerController player, 
    IEnumerable<Unit> source = null)
{
    if (player == null) yield break;

    var faction = player.myFaction;

    // OTIMIZAÇÃO: Se source for null, usar diretamente GetByFaction (mais rápido)
    if (source == null)
    {
        var directList = UnitRegistry.GetByFaction(faction);
        foreach (var u in directList)
        {
            yield return u;
        }
        yield break;
    }

    // Caso contrário, filtrar source fornecido
    foreach (var u in source)
        if (u != null && u.owner == faction)
            yield return u;
}
```

**Relacionamentos:**
- Consome: `UnitRegistry` (opcionalmente), `PlayerController`, Unity Object API
- Consumido por: Sistemas de Seleção, IA, UI, Comandos de Grupo

**Padrões de Uso:**

```csharp
// RECOMENDADO: Com cache do UnitRegistry (O(1) acesso + O(n) iteração)
var minhasUnidades = UnitQueries.GetUnitsForPlayer(player, null);

// Ou especificar source:
var minhasUnidades = UnitQueries.GetUnitsForPlayer(player, UnitRegistry.All);

// Varredura direta (quando Registry não é confiável)
foreach (var u in UnitQueries.EnumerateSceneUnits()) {
    // processar...
}
```

**Mudanças Pós-Refatoração:**
- ✅ **Otimizado:** `GetUnitsForPlayer(player, null)` agora usa `GetByFaction` diretamente
- ✅ **Performance:** Evita `FindObjectsOfType` desnecessário

---

### 2.6 PlayerController.cs

**Tipo:** `MonoBehaviour`

**Responsabilidade:** 
Define a facção do jogador e mantém referência à câmera principal. Usado como contexto de "quem é o jogador" para filtros e consultas.

**Campos Públicos:**

```csharp
[Header("Quem sou eu")]
public FactionId myFaction = FactionId.Player1;

[Header("Referências")]
public Camera mainCamera;
```

**Método Especial:**

```csharp
private void Reset() {
    mainCamera = Camera.main;  // Auto-atribui no Inspector
}
```

**Relacionamentos:**
- Consumido por: `UnitQueries`, Sistemas de Input, Sistemas de Seleção, IA
- Usa: `FactionId`

---

### 2.7 GameEvents.cs (Seção de Unidades)

**Tipo:** `static class` (event bus centralizado)

**Responsabilidade:** 
Fornecer eventos globais para comunicação desacoplada entre sistemas. A seção de Unidades foi adicionada na refatoração do Módulo Unit.

**Eventos de Unidades:**

```csharp
// ========== UNIDADES ==========
/// <summary>Disparado quando unidade spawna na cena</summary>
public static event Action<Unit> OnUnitSpawned;

/// <summary>Disparado quando unidade é removida da cena</summary>
public static event Action<Unit> OnUnitDespawned;

/// <summary>Disparado quando seleção de unidade muda</summary>
public static event Action<Unit, bool> OnUnitSelectionChanged; // unit, isSelected

/// <summary>Disparado quando XP ou Level de unidade mudam</summary>
public static event Action<Unit> OnUnitProgressChanged;
```

**Métodos Raise:**

```csharp
public static void RaiseUnitSpawned(Unit unit)
    => OnUnitSpawned?.Invoke(unit);

public static void RaiseUnitDespawned(Unit unit)
    => OnUnitDespawned?.Invoke(unit);

public static void RaiseUnitSelectionChanged(Unit unit, bool isSelected)
    => OnUnitSelectionChanged?.Invoke(unit, isSelected);

public static void RaiseUnitProgressChanged(Unit unit)
    => OnUnitProgressChanged?.Invoke(unit);
```

**Relacionamentos:**
- Consumido por: Todos os sistemas que precisam reagir a eventos de unidades
- Disparado por: `Unit` (seleção/progressão), `UnitRegistry` (spawn/despawn)

**Integração com Módulo Unit:**
- ✅ Adicionado na refatoração para unificar eventos
- ✅ Substitui eventos locais e estáticos anteriores
- ✅ Consistente com outros módulos (Câmera, Tempo, Economia)

---

## 3) REFERÊNCIA DE API (Membros Públicos)

### 3.1 UnitDefinition (ScriptableObject)

**Campos:**

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `displayName` | `string` | Nome exibido na UI |
| `type` | `UnitType` | Classificação da unidade |
| `icon` | `Sprite` | Ícone para UI/mini-mapa |

**Exemplo de Uso:**

```csharp
// Criar definição
// Assets > Create > Game > Unit Definition

// Acessar em código
string nome = unit.def.displayName;
Sprite icone = unit.def.icon;
if (unit.def.type == UnitType.Worker) {
    // Lógica específica para trabalhadores
}
```

---

### 3.2 Unit (MonoBehaviour)

#### Campos Públicos

| Campo | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `def` | `UnitDefinition` | null | ScriptableObject de definição |
| `owner` | `FactionId` | `Player1` | Facção proprietária |
| `hp` | `float` | 100 | Vida atual |
| `hpMax` | `float` | 100 | Vida máxima |

#### Propriedades Públicas

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `IsSelected` | `bool` | Estado de seleção (read-only) |
| `Level` | `int` | Nível atual (read-only) |
| `Xp` | `float` | XP atual (read-only) |
| `XpToNext` | `float` | XP necessário para próximo nível |
| `Xp01` | `float` | Progresso 0..1 para barra de XP |
| `DisplayName` | `string` | Nome para UI (def.displayName ou gameObject.name) |

#### Métodos Públicos

**SetSelected(bool value)**
```csharp
/// <summary>
/// Define estado de seleção e atualiza highlight visual.
/// Não dispara evento se valor não mudou.
/// Dispara GameEvents.OnUnitSelectionChanged.
/// </summary>
/// <param name="value">Novo estado de seleção</param>
public void SetSelected(bool value)
```

**AddXp(float amount)**
```csharp
/// <summary>
/// Adiciona XP e lida com múltiplos level-ups automaticamente.
/// Usa guard counter (64 iterações) para evitar loops infinitos.
/// Dispara GameEvents.OnUnitProgressChanged.
/// </summary>
/// <param name="amount">Quantidade de XP (valores ≤0 são ignorados)</param>
public void AddXp(float amount)
```

**TestList() (Editor Only)**
```csharp
#if UNITY_EDITOR
/// <summary>
/// Método de debug para listar unidades registradas.
/// Disponível apenas no Editor.
/// </summary>
public void TestList()
#endif
```

**Exemplo de Uso:**

```csharp
// Sistema de Seleção
void OnUnitClicked(Unit unit) {
    // Limpar seleção anterior
    foreach (var u in selectedUnits) u.SetSelected(false);
    
    // Selecionar nova unidade (dispara GameEvents.OnUnitSelectionChanged)
    unit.SetSelected(true);
}

// Sistema de Combate (futuro)
void OnEnemyKilled(Unit killer, Unit victim) {
    float xpReward = victim.Level * 50f;
    killer.AddXp(xpReward); // Dispara GameEvents.OnUnitProgressChanged
}

// UI de Progressão
void OnEnable() {
    GameEvents.OnUnitProgressChanged += UpdateXPBar;
}

void UpdateXPBar(Unit unit) {
    if (unit != selectedUnit) return; // Filtrar apenas unidade alvo
    xpSlider.value = unit.Xp01;
    levelText.text = $"Lv {unit.Level}";
}
```

---

### 3.3 UnitRegistry (static class)

#### Propriedades Públicas

```csharp
// Lista read-only de todas as unidades ativas
public static IReadOnlyList<Unit> All { get; }

// Obter unidades de uma facção específica (retorna lista vazia se não houver)
public static IReadOnlyList<Unit> GetByFaction(FactionId f)
```

#### Métodos Públicos

```csharp
/// <summary>
/// Registra unidade (chamado automaticamente por Unit.OnEnable).
/// Dispara GameEvents.OnUnitSpawned.
/// </summary>
public static void Register(Unit u)

/// <summary>
/// Remove unidade (chamado automaticamente por Unit.OnDisable).
/// Dispara GameEvents.OnUnitDespawned.
/// </summary>
public static void Unregister(Unit u)
```

**Exemplo de Uso:**

```csharp
// Sistema de IA - contar unidades por facção
int unidadesInimigas = UnitRegistry.GetByFaction(FactionId.PvE).Count;
int minhasUnidades = UnitRegistry.GetByFaction(player.myFaction).Count;

if (unidadesInimigas > minhasUnidades * 2) {
    RequestReinforcements();
}

// Sistema de Mini-mapa - escutar spawn/despawn via GameEvents
void OnEnable() {
    GameEvents.OnUnitSpawned += AddMinimapIcon;
    GameEvents.OnUnitDespawned += RemoveMinimapIcon;
}

void AddMinimapIcon(Unit unit) {
    var icon = Instantiate(minimapIconPrefab, minimapContainer);
    icon.color = GetFactionColor(unit.owner);
    // ... vincular à posição da unidade
}

// Sistema de Estatísticas - rastrear população
void OnEnable() {
    GameEvents.OnUnitSpawned += u => {
        if (u.owner == myFaction) populationCount++;
    };
    GameEvents.OnUnitDespawned += u => {
        if (u.owner == myFaction) populationCount--;
    };
}
```

---

### 3.4 UnitQueries (static class)

#### Métodos Públicos

**GetUnitsForPlayer(PlayerController, IEnumerable<Unit>)**

```csharp
/// <summary>
/// Retorna unidades do jogador especificado.
/// OTIMIZADO: Se source for null, usa diretamente GetByFaction (mais rápido).
/// </summary>
/// <param name="player">PlayerController alvo</param>
/// <param name="source">Lista opcional. Se null, usa GetByFaction diretamente.</param>
/// <returns>Enumerável filtrado de unidades</returns>
public static IEnumerable<Unit> GetUnitsForPlayer(
    PlayerController player, 
    IEnumerable<Unit> source = null
)
```

**EnumerateSceneUnits()**

```csharp
/// <summary>
/// Fallback: varre cena usando FindObjectsByType (Unity 2023+) 
/// ou FindObjectsOfType (versões antigas).
/// </summary>
/// <returns>Enumerável de todas as Units na cena</returns>
public static IEnumerable<Unit> EnumerateSceneUnits()
```

**Exemplo de Uso:**

```csharp
// Sistema de Seleção - selecionar todas unidades do jogador
public class SelectAllCommand {
    public void Execute(PlayerController player) {
        // RECOMENDADO: usar otimização (source = null)
        var units = UnitQueries.GetUnitsForPlayer(player, null);
        
        foreach (var u in units) {
            u.SetSelected(true);
        }
    }
}

// Sistema de IA - contar trabalhadores do jogador
int workerCount = UnitQueries
    .GetUnitsForPlayer(player, null)
    .Count(u => u.def.type == UnitType.Worker);

// Debug - listar todas unidades na cena (varredura direta)
foreach (var u in UnitQueries.EnumerateSceneUnits()) {
    Debug.Log($"{u.DisplayName} ({u.owner})");
}
```

---

### 3.5 GameEvents (Eventos de Unidades)

#### Eventos Públicos

**OnUnitSpawned**
```csharp
/// <summary>
/// Disparado quando unidade é adicionada à cena.
/// Disparado por: UnitRegistry.Register()
/// </summary>
public static event Action<Unit> OnUnitSpawned;
```

**OnUnitDespawned**
```csharp
/// <summary>
/// Disparado quando unidade é removida da cena.
/// Disparado por: UnitRegistry.Unregister()
/// </summary>
public static event Action<Unit> OnUnitDespawned;
```

**OnUnitSelectionChanged**
```csharp
/// <summary>
/// Disparado quando seleção de unidade muda.
/// Disparado por: Unit.SetSelected()
/// </summary>
public static event Action<Unit, bool> OnUnitSelectionChanged; // unit, isSelected
```

**OnUnitProgressChanged**
```csharp
/// <summary>
/// Disparado quando XP ou Level de unidade mudam.
/// Disparado por: Unit.AddXp()
/// </summary>
public static event Action<Unit> OnUnitProgressChanged;
```

#### Métodos Raise

```csharp
public static void RaiseUnitSpawned(Unit unit);
public static void RaiseUnitDespawned(Unit unit);
public static void RaiseUnitSelectionChanged(Unit unit, bool isSelected);
public static void RaiseUnitProgressChanged(Unit unit);
```

**Exemplo de Uso:**

```csharp
// Sistema de UI - escutar eventos
void OnEnable() {
    GameEvents.OnUnitSpawned += OnNewUnit;
    GameEvents.OnUnitProgressChanged += OnUnitLevelUp;
    GameEvents.OnUnitSelectionChanged += OnSelectionChange;
}

void OnDisable() {
    GameEvents.OnUnitSpawned -= OnNewUnit;
    GameEvents.OnUnitProgressChanged -= OnUnitLevelUp;
    GameEvents.OnUnitSelectionChanged -= OnSelectionChange;
}

void OnNewUnit(Unit unit) {
    Debug.Log($"Nova unidade: {unit.DisplayName}");
}

void OnUnitLevelUp(Unit unit) {
    // Filtrar apenas unidades do jogador
    if (unit.owner == myFaction) {
        ShowLevelUpNotification(unit);
    }
}

void OnSelectionChange(Unit unit, bool selected) {
    if (selected) {
        ShowUnitInfo(unit);
    }
}
```

---

## 4) EVENTOS — EMISSÃO & ASSINATURA

### 4.1 Tabela de Eventos do Módulo Unit

| Evento | Tipo | Disparado Por | Parâmetros |
|--------|------|---------------|------------|
| `OnUnitSpawned` | `Action<Unit>` | `UnitRegistry.Register()` | `Unit unit` |
| `OnUnitDespawned` | `Action<Unit>` | `UnitRegistry.Unregister()` | `Unit unit` |
| `OnUnitSelectionChanged` | `Action<Unit, bool>` | `Unit.SetSelected()` | `Unit unit, bool isSelected` |
| `OnUnitProgressChanged` | `Action<Unit>` | `Unit.AddXp()` | `Unit unit` |

### 4.2 Onde Escutar (Sistemas Típicos)

#### OnUnitSpawned / OnUnitDespawned

**Escutado por:**
- Mini-mapa (adicionar/remover ícones)
- Fog of War (revelar/esconder área)
- Sistema de Estatísticas (população atual)
- Sistema de IA (detectar novos inimigos)

**Exemplo:**

```csharp
void OnEnable() {
    GameEvents.OnUnitSpawned += OnNewUnit;
    GameEvents.OnUnitDespawned += OnUnitRemoved;
}

void OnNewUnit(Unit unit) {
    // Mini-mapa
    CreateMinimapIcon(unit);
    
    // Fog of War
    if (unit.owner == myFaction) {
        fogOfWar.RevealArea(unit.transform.position, visionRadius);
    }
    
    // Estatísticas
    if (unit.owner == myFaction) {
        currentPopulation++;
        UpdatePopulationUI();
    }
}
```

---

#### OnUnitSelectionChanged

**Escutado por:**
- UI de Seleção (mostrar painel de informações)
- Sistema de Comandos (habilitar/desabilitar botões)
- Sistema de Câmera (foco opcional em unidade)

**Exemplo:**

```csharp
void OnEnable() {
    GameEvents.OnUnitSelectionChanged += HandleSelectionChange;
}

void HandleSelectionChange(Unit unit, bool selected) {
    if (selected) {
        ShowUnitPanel(unit);
        EnableCommandButtons(unit);
    } else {
        if (!AnyUnitSelected()) {
            HideUnitPanel();
        }
    }
}
```

---

#### OnUnitProgressChanged

**Escutado por:**
- UI de Progressão (barra de XP, texto de nível)
- Sistema de Notificações (level-up popup)
- Sistema de Estatísticas (XP total coletado)

**Exemplo:**

```csharp
void OnEnable() {
    GameEvents.OnUnitProgressChanged += UpdateProgressUI;
}

void UpdateProgressUI(Unit unit) {
    // Filtrar apenas a unidade selecionada
    if (unit != selectedUnit) return;
    
    xpBar.fillAmount = unit.Xp01;
    levelText.text = $"Nível {unit.Level}";
    xpText.text = $"{unit.Xp:F0} / {unit.XpToNext:F0}";
}
```

---

### 4.3 Padrões Recomendados

#### ✅ Sempre Desinscrever

```csharp
void OnEnable() {
    GameEvents.OnUnitProgressChanged += UpdateUI;
    GameEvents.OnUnitSpawned += TrackUnit;
}

void OnDisable() {
    GameEvents.OnUnitProgressChanged -= UpdateUI;  // CRÍTICO!
    GameEvents.OnUnitSpawned -= TrackUnit;  // CRÍTICO!
}
```

#### ✅ Filtrar Eventos Quando Necessário

```csharp
void OnEnable() {
    // Eventos globais - filtrar apenas o que interessa
    GameEvents.OnUnitProgressChanged += HandleProgress;
}

void HandleProgress(Unit unit) {
    // Filtro: apenas unidades do jogador
    if (unit.owner != myFaction) return;
    
    // Processar...
}
```

#### ✅ Verificar Nulos

```csharp
void HandleUnitSpawn(Unit unit) {
    if (unit == null) return;  // Unidade já destruída?
    if (unit.def == null) {
        Debug.LogWarning($"Unit {unit.name} sem UnitDefinition!");
        return;
    }
    // Processar...
}
```

#### ✅ Handlers Curtos e Resilientes

```csharp
void OnUnitSpawned(Unit unit) {
    try {
        CreateMinimapIcon(unit);
    } catch (Exception e) {
        Debug.LogError($"Erro ao criar ícone: {e.Message}");
        // Sistema continua funcionando mesmo com erro
    }
}
```

---

## 5) VARIÁVEIS-CHAVE / CONFIGURAÇÕES (DESTAQUES)

### 5.1 Progressão (Unit)

| Campo | Tipo | Padrão | Impacto |
|-------|------|--------|---------|
| `baseXpToLevel` | `float` | 100 | XP para Lv1→Lv2 (base da curva) |
| `xpGrowth` | `float` | 1.35 | Multiplicador exponencial (1.35 = +35% por nível) |

**Fórmula de XP:**
```csharp
XpToNext = baseXpToLevel * Pow(xpGrowth, Level - 1)
```

**Exemplos de Curva:**

| Level | XP Necessário (base=100, growth=1.35) |
|-------|---------------------------------------|
| 1→2   | 100                                   |
| 2→3   | 135                                   |
| 3→4   | 182                                   |
| 4→5   | 246                                   |
| 9→10  | 1,001                                 |

**Tuning:**
- `xpGrowth` baixo (1.1-1.2) → progressão linear/suave
- `xpGrowth` alto (1.5+) → progressão exponencial/agressiva

---

### 5.2 Dados Vitais (Unit)

| Campo | Tipo | Padrão | Uso |
|-------|------|--------|-----|
| `hp` | `float` | 100 | Vida atual (modificado por combate) |
| `hpMax` | `float` | 100 | Vida máxima (modificado por upgrades) |

**Nota:** Sistema de Combate (futuro) modificará estes valores.

---

### 5.3 Seleção Visual (Unit)

| Campo | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `selectionHighlight` | `GameObject` | null | Ring/outline ativado quando `IsSelected=true` |

**Padrões de Implementação:**

```csharp
// Opção 1: GameObject com MeshRenderer (ring no chão)
selectionHighlight = transform.Find("SelectionRing").gameObject;

// Opção 2: Shader outline (material swap)
// (implementar em SetSelected se necessário)

// Opção 3: UI World Space (canvas acima da unidade)
selectionHighlight = GetComponentInChildren<Canvas>().gameObject;
```

---

## 6) EXEMPLOS COMPOSTOS (END-TO-END)

### 6.1 Sistema de Seleção Básico

```csharp
public class SimpleSelectionSystem : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Camera cam;
    
    private List<Unit> selectedUnits = new List<Unit>();
    
    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            HandleClick();
        }
    }
    
    void HandleClick() {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Unit unit = hit.collider.GetComponent<Unit>();
            
            if (unit != null && unit.owner == player.myFaction) {
                SelectUnit(unit);
            }
        }
    }
    
    void SelectUnit(Unit unit) {
        // Limpar seleção anterior
        foreach (var u in selectedUnits) {
            u.SetSelected(false); // Dispara GameEvents.OnUnitSelectionChanged
        }
        selectedUnits.Clear();
        
        // Selecionar nova unidade
        unit.SetSelected(true); // Dispara GameEvents.OnUnitSelectionChanged
        selectedUnits.Add(unit);
    }
}
```

---

### 6.2 Sistema de Progressão com Notificações

```csharp
public class ProgressionSystem : MonoBehaviour
{
    [SerializeField] GameObject levelUpPrefab;
    
    void OnEnable() {
        GameEvents.OnUnitProgressChanged += HandleProgress;
    }
    
    void OnDisable() {
        GameEvents.OnUnitProgressChanged -= HandleProgress;
    }
    
    void HandleProgress(Unit unit) {
        // Detectar level-up (XP está próximo de 0 após level-up)
        if (unit.Xp < unit.XpToNext * 0.1f && unit.Level > 1) {
            ShowLevelUpNotification(unit);
        }
    }
    
    void ShowLevelUpNotification(Unit unit) {
        var popup = Instantiate(levelUpPrefab, unit.transform.position, Quaternion.identity);
        popup.GetComponent<TextMeshPro>().text = $"Level {unit.Level}!";
        Destroy(popup, 2f);
    }
}
```

---

### 6.3 Sistema de Estatísticas por Facção

```csharp
public class FactionStatsTracker : MonoBehaviour
{
    private Dictionary<FactionId, int> populationCount = new Dictionary<FactionId, int>();
    
    void OnEnable() {
        GameEvents.OnUnitSpawned += OnUnitSpawned;
        GameEvents.OnUnitDespawned += OnUnitDespawned;
    }
    
    void OnDisable() {
        GameEvents.OnUnitSpawned -= OnUnitSpawned;
        GameEvents.OnUnitDespawned -= OnUnitDespawned;
    }
    
    void OnUnitSpawned(Unit unit) {
        if (!populationCount.ContainsKey(unit.owner)) {
            populationCount[unit.owner] = 0;
        }
        populationCount[unit.owner]++;
        UpdateUI();
    }
    
    void OnUnitDespawned(Unit unit) {
        if (populationCount.ContainsKey(unit.owner)) {
            populationCount[unit.owner]--;
            UpdateUI();
        }
    }
    
    void UpdateUI() {
        foreach (var kvp in populationCount) {
            Debug.Log($"{kvp.Key}: {kvp.Value} unidades");
        }
    }
    
    public int GetPopulation(FactionId faction) {
        return populationCount.TryGetValue(faction, out int count) ? count : 0;
    }
}
```

---

### 6.4 Mini-mapa Integrado

```csharp
public class MinimapSystem : MonoBehaviour
{
    [SerializeField] GameObject iconPrefab;
    [SerializeField] Transform iconContainer;
    [SerializeField] Vector2 worldSize = new Vector2(500f, 500f);
    
    private Dictionary<Unit, GameObject> icons = new Dictionary<Unit, GameObject>();
    
    void OnEnable() {
        GameEvents.OnUnitSpawned += CreateIcon;
        GameEvents.OnUnitDespawned += RemoveIcon;
    }
    
    void OnDisable() {
        GameEvents.OnUnitSpawned -= CreateIcon;
        GameEvents.OnUnitDespawned -= RemoveIcon;
    }
    
    void CreateIcon(Unit unit) {
        var icon = Instantiate(iconPrefab, iconContainer);
        icon.GetComponent<Image>().color = GetFactionColor(unit.owner);
        icons[unit] = icon;
    }
    
    void RemoveIcon(Unit unit) {
        if (icons.TryGetValue(unit, out GameObject icon)) {
            Destroy(icon);
            icons.Remove(unit);
        }
    }
    
    void Update() {
        foreach (var kvp in icons) {
            Unit unit = kvp.Key;
            GameObject icon = kvp.Value;
            
            if (unit != null) {
                // Converter posição do mundo para mini-mapa
                Vector2 normalizedPos = new Vector2(
                    unit.transform.position.x / worldSize.x,
                    unit.transform.position.z / worldSize.y
                );
                
                icon.GetComponent<RectTransform>().anchoredPosition = 
                    normalizedPos * 200f; // Tamanho do mini-mapa
            }
        }
    }
    
    Color GetFactionColor(FactionId faction) {
        switch (faction) {
            case FactionId.Player1: return Color.blue;
            case FactionId.Player2: return Color.red;
            case FactionId.PvE: return Color.yellow;
            default: return Color.gray;
        }
    }
}
```

---

## 7) NOTAS DE IMPLEMENTAÇÃO & BOAS PRÁTICAS

### 7.1 Ordem de Inicialização

- ✅ `Unit.OnEnable()` registra automaticamente no `UnitRegistry`
- ✅ `UnitRegistry.Register()` dispara `GameEvents.OnUnitSpawned`
- ✅ `Unit.OnDisable()` limpa automaticamente do `UnitRegistry`
- ✅ `UnitRegistry.Unregister()` dispara `GameEvents.OnUnitDespawned`
- ⚠️ **Não** chamar `Register`/`Unregister` manualmente (a menos que haja caso especial)

### 7.2 Uso de UnitDefinition

- ✅ Criar UnitDefinitions no Editor via `Assets > Create > Game > Unit Definition`
- ✅ Reutilizar definições para múltiplas instâncias do mesmo tipo
- ✅ Usar `def.type` para lógica específica de tipo (IA, balanceamento)
- ⚠️ **Sempre** verificar `if (unit.def != null)` antes de acessar

### 7.3 Performance e Otimização

**UnitQueries:**
```csharp
// ✅ BOM: Usar otimização (source = null)
var units = UnitQueries.GetUnitsForPlayer(player, null);

// ❌ RUIM: Varrer cena toda frame
var units = UnitQueries.EnumerateSceneUnits();
```

**Consultas Frequentes:**
```csharp
// ✅ BOM: Cache por facção (O(1) acesso ao dicionário)
var inimigos = UnitRegistry.GetByFaction(FactionId.PvE);

// ❌ RUIM: Filtrar lista completa toda frame
var inimigos = UnitRegistry.All.Where(u => u.owner == FactionId.PvE);
```

### 7.4 Eventos e Memory Leaks

**⚠️ CRÍTICO: Sempre desinscrever eventos**

```csharp
// ✅ BOM: Par Subscribe/Unsubscribe
void OnEnable() {
    GameEvents.OnUnitProgressChanged += Handler;
}
void OnDisable() {
    GameEvents.OnUnitProgressChanged -= Handler;  // OBRIGATÓRIO
}

// ❌ RUIM: Só subscribe (causa memory leak)
void Start() {
    GameEvents.OnUnitProgressChanged += Handler;
    // GameObject destruído mas handler permanece na memória
}
```

### 7.5 Testabilidade

**Criar UnitDefinitions programaticamente (testes unitários):**

```csharp
[Test]
public void TestUnitProgression() {
    // Criar definição em runtime
    var def = ScriptableObject.CreateInstance<UnitDefinition>();
    def.displayName = "Test Warrior";
    def.type = UnitType.Warrior;
    
    // Criar unidade
    var go = new GameObject("TestUnit");
    var unit = go.AddComponent<Unit>();
    unit.def = def;
    unit.owner = FactionId.Player1;
    
    // Testar progressão
    unit.AddXp(150);
    Assert.AreEqual(2, unit.Level);
}
```

---

## 8) INTEGRAÇÃO COM OUTROS MÓDULOS

### 8.1 Sistema de Câmeras (Lote 2)

**Integração Existente:**

```csharp
// GameEvents já possui eventos de foco/seleção
public static event Action<Transform> OnSelectionFocus;

// Quando unidade é selecionada, focar câmera (opcional)
void OnEnable() {
    GameEvents.OnUnitSelectionChanged += HandleSelection;
}

void HandleSelection(Unit unit, bool selected) {
    if (selected && autoFocusEnabled) {
        GameEvents.RaiseSelectionFocus(unit.transform);
    }
}
```

### 8.2 Sistema de Tempo (Lote 1)

**Exemplo: Regeneração de HP por tempo**

```csharp
public class HealthRegenSystem : MonoBehaviour
{
    [SerializeField] float regenPerSecond = 1f;
    
    void Update() {
        foreach (var unit in UnitRegistry.All) {
            if (unit.hp < unit.hpMax) {
                unit.hp = Mathf.Min(unit.hp + regenPerSecond * Time.deltaTime, unit.hpMax);
            }
        }
    }
}
```

### 8.3 Sistema de Facções (Lote 1)

**Exemplo: XP bonus por reputação**

```csharp
public class ReputationXPBonus : MonoBehaviour
{
    [SerializeField] FactionService factionService;
    
    public void AwardXP(Unit unit, float baseXp) {
        float reputation = factionService.GetReputation(unit.owner, FactionId.Player1);
        float multiplier = 1f + (reputation / 100f) * 0.5f; // +50% max
        
        unit.AddXp(baseXp * multiplier); // Dispara GameEvents.OnUnitProgressChanged
    }
}
```

---

## 9) TROUBLESHOOTING (PROBLEMAS COMUNS)

### Problema: "Unit.def is null" (erro no console)

**Causa:** UnitDefinition não foi atribuído no Inspector

**Solução:**
1. Criar UnitDefinition: `Assets > Create > Game > Unit Definition`
2. Configurar campos (displayName, type, icon)
3. Arrastar para campo `def` no Inspector do GameObject

---

### Problema: Unidade não aparece em UnitRegistry.All

**Possíveis Causas:**
1. GameObject está desativado (OnEnable não foi chamado)
2. Componente Unit está desabilitado
3. OnEnable ainda não executou (verificar ordem de inicialização)

**Debug:**
```csharp
// No Unit.cs
private void OnEnable() {
    Debug.Log($"Registrando {gameObject.name}");
    UnitRegistry.Register(this);
}
```

---

### Problema: Eventos não disparam

**Causa:** Sistema não está inscrito em `GameEvents`

**Solução:**
```csharp
void OnEnable() {
    GameEvents.OnUnitProgressChanged += HandleProgress;  // Inscrever
}

void OnDisable() {
    GameEvents.OnUnitProgressChanged -= HandleProgress;  // SEMPRE desinscrever
}
```

---

### Problema: Level-up não dispara evento

**Causa:** Evento não foi assinado antes de chamar AddXp

**Solução:**
```csharp
void Start() {
    GameEvents.OnUnitProgressChanged += HandleProgress;  // ANTES de AddXp
    unit.AddXp(100);
}
```

---

### Problema: Memory leak com eventos

**Causa:** Não desinscrever eventos em OnDisable

**Solução:**
```csharp
void OnDisable() {
    GameEvents.OnUnitProgressChanged -= Handler;  // CRÍTICO
    GameEvents.OnUnitSpawned -= Handler2;  // CRÍTICO
}
```

---

### Problema: GetUnitsForPlayer retorna lista vazia

**Possíveis Causas:**
1. `player.myFaction` não corresponde a `unit.owner`
2. Nenhuma unidade spawnou ainda
3. Source é uma lista vazia (usar null para otimização)

**Debug:**
```csharp
var units = UnitQueries.GetUnitsForPlayer(player, null);
Debug.Log($"Player faction: {player.myFaction}");
Debug.Log($"Units found: {units.Count()}");
foreach (var u in UnitRegistry.All) {
    Debug.Log($"  {u.DisplayName} - Owner: {u.owner}");
}
```

---

## 10) CHECKLIST DE IMPLEMENTAÇÃO

### Setup Inicial
- [x] Criar `UnitDefinition.cs` (ScriptableObject)
- [x] Criar `Unit.cs` (MonoBehaviour)
- [x] Criar `UnitRegistry.cs` (static class)
- [x] Criar `UnitQueries.cs` (static class)
- [x] Adicionar `FactionId` e `UnitType` em `Enums.cs`
- [x] Adicionar eventos de Unit em `GameEvents.cs`

### Criação de Assets
- [ ] Criar UnitDefinitions para cada tipo (Worker, Warrior, etc.)
- [ ] Configurar displayName, type, icon para cada definição

### Integração com Cena
- [ ] Adicionar componente `Unit` aos prefabs de unidades
- [ ] Atribuir `UnitDefinition` correspondente
- [ ] Configurar `selectionHighlight` (ring/outline)
- [ ] Ajustar valores de `hp`, `hpMax`, `baseXpToLevel`, `xpGrowth`

### Sistemas Dependentes
- [ ] Atualizar sistemas existentes para usar `GameEvents`
- [ ] Implementar Sistema de Seleção
- [ ] Implementar UI de Progressão
- [ ] Implementar Mini-mapa com ícones dinâmicos

### Testes
- [ ] Testar spawn/despawn de unidades
- [ ] Testar seleção visual (highlight)
- [ ] Testar progressão (AddXp, level-up)
- [ ] Testar consultas por facção
- [ ] Testar eventos (spawn, despawn, seleção, progressão)
- [ ] Testar performance com 100+ unidades

---

## 11) GLOSSÁRIO

### Termos Técnicos

| Termo | Descrição |
|-------|-----------|
| **Unit** | Entidade jogável/controlável (trabalhador, guerreiro, herói) |
| **UnitDefinition** | ScriptableObject com metadados de tipo de unidade |
| **UnitRegistry** | Registro centralizado de unidades ativas |
| **UnitQueries** | Biblioteca de helpers para consultas de unidades |
| **Facção** | Grupo/time ao qual a unidade pertence (Player1, PvE, etc.) |
| **XP** | Experience Points (pontos de experiência) |
| **Level** | Nível atual da unidade (baseado em XP acumulado) |
| **Progressão** | Sistema de XP e Level-up |
| **Seleção** | Estado de unidade escolhida pelo jogador |
| **Highlight** | Indicador visual de seleção (ring, outline) |
| **Spawn** | Criação/ativação de unidade na cena |
| **Despawn** | Remoção/desativação de unidade da cena |
| **Event Bus** | Sistema centralizado de eventos (`GameEvents`) |

---

## 12) REFERÊNCIAS E RECURSOS

### Documentação Relacionada
- **Lote 1:** Variáveis Globais & Factions (Enums, GameEvents, FactionService)
- **Lote 2:** Sistema de Câmeras (GameEvents de foco/seleção)
- **Futuro:** Sistema de Seleção (SetSelected, highlight)
- **Futuro:** Sistema de Combate (hp, morte)
- **Futuro:** Sistema de UI (progressão, stats)

### Padrões de Design Utilizados
- **Registry Pattern**: `UnitRegistry` como registro centralizado
- **Event Bus Pattern**: `GameEvents` para comunicação desacoplada
- **ScriptableObject Pattern**: `UnitDefinition` como dados estáticos
- **Query Object Pattern**: `UnitQueries` como helpers de consulta

---

## 13) CÓDIGO COMPLETO RESUMIDO

### Estrutura de Arquivos

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Enums.cs              (FactionId, UnitType)
│   │   ├── GameEvents.cs         (event bus global + eventos de Unit)
│   │   └── PlayerController.cs   (contexto de jogador)
│   ├── Units/
│   │   ├── Unit.cs               (componente principal)
│   │   ├── UnitDefinition.cs     (ScriptableObject)
│   │   ├── UnitRegistry.cs       (registro estático)
│   │   └── UnitQueries.cs        (helpers de consulta)
│   └── ...
├── Definitions/
│   ├── Units/
│   │   ├── Worker_Def.asset
│   │   ├── Warrior_Def.asset
│   │   ├── Archer_Def.asset
│   │   └── ...
└── Prefabs/
    ├── Units/
    │   ├── Worker.prefab          (componente Unit)
    │   ├── Warrior.prefab         (componente Unit)
    │   └── ...
```

### Hierarquia da Cena (Exemplo)

```
Scene
├── _GameContext
│   ├── GameConfig
│   ├── FactionDatabase
│   ├── FactionService
│   └── TimeManager
├── Player
│   └── PlayerController (myFaction: Player1)
├── Units
│   ├── Worker_01 (Unit component)
│   ├── Worker_02 (Unit component)
│   ├── Warrior_01 (Unit component)
│   └── ...
├── CameraRig
└── UI
```

---

## CONCLUSÃO

O **Módulo Unit** estabelece a fundação para todos os sistemas relacionados a entidades no RTS: seleção, combate, IA, progressão e UI. 

### Status de Implementação

| Componente | Status | Notas |
|------------|--------|-------|
| UnitDefinition | ✅ Completo | ScriptableObject funcional |
| Unit | ✅ Completo | Integrado com GameEvents |
| UnitRegistry | ✅ Completo | Integrado com GameEvents |
| UnitQueries | ✅ Completo | Otimizado (GetByFaction direto) |
| GameEvents (Unit) | ✅ Completo | 4 eventos adicionados |
| **Arquitetura** | ✅ **Unificada** | Consistente com outros módulos |

### Próximos Passos Recomendados

1. Implementar Sistema de Seleção (próximo lote)
2. Implementar UI de Progressão (próximo lote)
3. Implementar Sistema de Mini-mapa com ícones dinâmicos
4. Preparar arquitetura para Sistema de Combate (modificação de HP)
5. Criar testes unitários para eventos de Unit

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão:** 2.0 (Pós-Refatoração)

---

## LOTE 4 — MÓDULO SELECTION - SISTEMA DE SELEÇÃO DE UNIDADES

**Versão:** 2.0 (Atualizada - Outubro 2025)  
**Status:** ✅ Implementado e Funcional | ⚠️ Pendente Refatoração (GameEvents)

---

## 📋 ÍNDICE

1. [Visão Geral do Módulo](#1-visão-geral-do-módulo)
2. [Estrutura e Relacionamentos das Classes](#2-estrutura-e-relacionamentos-das-classes)
3. [Referência de API (Membros Públicos)](#3-referência-de-api-membros-públicos)
4. [Eventos Chave](#4-eventos-chave)
5. [Configuração e Setup](#5-configuração-e-setup)
6. [Casos de Uso Práticos](#6-casos-de-uso-práticos)
7. [Integração com UI](#7-integração-com-ui)
8. [Troubleshooting](#8-troubleshooting)
9. [Sugestões de Refatoração](#9-sugestões-de-refatoração)
10. [Glossário](#10-glossário)
11. [Estrutura de Arquivos](#11-estrutura-de-arquivos)

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Objetivo

Fornecer um **sistema completo e robusto de seleção de unidades** para RTS, permitindo ao jogador:
- ✅ Clicar para selecionar unidades individuais
- ✅ Arrastar (drag) para seleção de múltiplas unidades (box selection)
- ✅ Duplo-clique para selecionar todas unidades do mesmo tipo visíveis na tela
- ✅ Modificadores (Ctrl) para seleção aditiva/toggle
- ✅ Proteção contra seleção acidental ao clicar em UI
- ✅ Feedback visual de seleção (highlight nas unidades + retângulo de drag)

### 1.2 Responsabilidades Principais

O Módulo Selection é responsável por:

1. **Captura de Input** (`InputSelection`):
   - Ler ações do Unity Input System (cliques, posição do mouse, modificadores)
   - Detectar padrões complexos (drag, double-click)
   - Filtrar input sobre UI (não selecionar se clicar em botões)

2. **Raycasting no Mundo** (`WorldPicker`):
   - Converter posição de tela em unidades/terreno no mundo 3D
   - Usar LayerMasks para otimização e precisão

3. **Gerenciamento de Estado** (`SelectionManager`):
   - Manter HashSet de unidades selecionadas
   - Aplicar regras de seleção (filtro por facção, modificadores)
   - Disparar eventos quando seleção muda

4. **Feedback Visual**:
   - `Unit.selectionHighlight` ativado/desativado automaticamente
   - `DragRectRenderer` desenha retângulo amarelo durante drag

5. **Suporte a Hierarquias Complexas** (`UnitHitProxy`):
   - Facilitar raycasting em prefabs com múltiplos colliders

### 1.3 Arquitetura do Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                    UNITY INPUT SYSTEM                        │
│          (Mouse/Keyboard → InputActionAsset)                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │   InputSelection      │  ← Captura input bruto
         │   (MonoBehaviour)     │     Detecta padrões (drag, double-click)
         └───────┬───────────────┘     Filtra UI (EventSystem)
                 │
                 │ Eventos:
                 │ • OnClickUnit
                 │ • OnClickGround
                 │ • OnBeginDrag / OnEndDrag
                 │ • OnDoubleClickUnit
                 ▼
         ┌───────────────────────┐
         │   WorldPicker         │  ← Raycasting 3D
         │   (MonoBehaviour)     │     Converte tela → mundo
         └───────┬───────────────┘     Usa LayerMasks
                 │
                 │ Unit? ou Vector3
                 ▼
         ┌───────────────────────┐
         │  SelectionManager     │  ← Lógica de seleção
         │  (MonoBehaviour)      │     Regras (facção, modificadores)
         └───────┬───────────────┘     HashSet<Unit>
                 │
                 │ FireChanged()
                 ▼
         ┌───────────────────────┐
         │ OnSelectionChanged    │  ← Evento LOCAL (atual)
         │   (Action<IReadOnly   │     ⚠️ REFATORAR para GameEvents
         │    Collection<Unit>>) │
         └───────┬───────────────┘
                 │
                 ├──────────────┬──────────────┬──────────────┐
                 ▼              ▼              ▼              ▼
            [UI Panel]    [Minimap]      [Audio]    [Sistemas Futuros]
```

**Fluxo de Seleção (Clique Simples):**
1. Jogador clica com LMB
2. `InputSelection` detecta clique (não é drag, não está sobre UI)
3. `InputSelection` chama `WorldPicker.TryPickUnitAt(screenPos)`
4. `WorldPicker` faz raycast na Layer "Unit"
5. Acerta `SphereCollider` (trigger) do `UnitHitProxy`
6. `UnitHitProxy` retorna referência ao componente `Unit`
7. `InputSelection` dispara evento `OnClickUnit(unit, ctrl)`
8. `SelectionManager` escuta e processa (Add/Toggle/Replace)
9. `SelectionManager.FireChanged()` → `OnSelectionChanged` → UI atualiza

### 1.4 Integração com Outros Módulos

| Módulo | Relação | Uso |
|--------|---------|-----|
| **Unit** (Lote 3) | ✅ Dependência Direta | `Unit.SetSelected(bool)`, `Unit.owner`, `Unit.def` |
| **UnitRegistry** (Lote 3) | ✅ Dependência Direta | `GetByFaction()` para double-click e drag |
| **PlayerController** (Lote 1) | ✅ Dependência Direta | `myFaction` para filtro "only own units" |
| **GameEvents** (Lote 1) | ⚠️ **Não usado ainda** | **REFATORAÇÃO PENDENTE:** `OnSelectionChanged` deve migrar |
| **UI System** | ✅ Consumer | `UnitListItemUI` escuta `OnSelectionChanged` |
| **Comandos** (Futuro) | 🔜 Consumer | Usará `SelectionManager.Selection` para mover/atacar |

---

## 2) ESTRUTURA E RELACIONAMENTOS DAS CLASSES

### 2.1 Diagrama de Classes

```
┌──────────────────────────────────────────────────────────────┐
│                        INPUT LAYER                            │
├──────────────────────────────────────────────────────────────┤
│  InputSelection                                               │
│  ├─ InputActionReference point, lmb, rmb, ctrl, shift       │
│  ├─ WorldPicker picker                                       │
│  ├─ event Action<Unit, bool> OnClickUnit                     │
│  ├─ event Action<Unit> OnDoubleClickUnit                     │
│  ├─ event Action<Vector3, bool> OnClickGround                │
│  ├─ event Action<Vector2> OnBeginDrag/OnEndDrag              │
│  └─ bool IsCtrlPressed, IsShiftPressed                       │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                      RAYCASTING LAYER                         │
├──────────────────────────────────────────────────────────────┤
│  WorldPicker                                                  │
│  ├─ Camera cam                                                │
│  ├─ LayerMask unitMask, groundMask                           │
│  ├─ bool TryPickUnitAt(Vector2, out Unit)                    │
│  └─ bool TryPickGroundAt(Vector2, out Vector3, out Vector3)  │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                    SELECTION LOGIC LAYER                      │
├──────────────────────────────────────────────────────────────┤
│  SelectionManager                                             │
│  ├─ HashSet<Unit> _selection                                 │
│  ├─ PlayerController player                                  │
│  ├─ bool onlyOwnUnits                                        │
│  ├─ IReadOnlyCollection<Unit> Selection { get; }             │
│  ├─ event Action<IReadOnlyCollection<Unit>> OnSelectionChanged│
│  ├─ void HandleClickUnit(Unit, bool ctrl)                    │
│  ├─ void HandleEndDrag(Vector2)                              │
│  ├─ void HandleDoubleClickUnit(Unit)                         │
│  └─ void SelectByWorldRect(Vector3 a, Vector3 b, bool add)   │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                      VISUAL FEEDBACK LAYER                    │
├──────────────────────────────────────────────────────────────┤
│  DragRectRenderer                                             │
│  ├─ GameObject quadPrefab                                     │
│  └─ Instancia/atualiza/destrói quad amarelo durante drag     │
│                                                               │
│  UnitHitProxy                                                 │
│  ├─ Unit unit                                                 │
│  └─ Facilita GetComponentInParent<Unit>() em raycasts        │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 Tabela de Componentes

| Classe | Tipo | Responsabilidade | Arquivo |
|--------|------|------------------|---------|
| `InputSelection` | MonoBehaviour | Captura input do Unity Input System e traduz em eventos de gameplay | `InputSelection.cs` |
| `WorldPicker` | MonoBehaviour | Raycasting para detectar unidades e terreno | `WorldPicker.cs` |
| `SelectionManager` | MonoBehaviour | Gerencia estado de seleção (HashSet) e aplica regras | `SelectionManager.cs` |
| `UnitHitProxy` | MonoBehaviour | Proxy para facilitar raycasting em hierarquias complexas | `UnitHitProxy.cs` |
| `DragRectRenderer` | MonoBehaviour | Feedback visual do retângulo de seleção | `DragRectRenderer.cs` |
| `SelectionDebugListener` | MonoBehaviour | Debug (comentado, não em produção) | `SelectionDebugListener.cs` |

---

## 3) REFERÊNCIA DE API (MEMBROS PÚBLICOS)

### 3.1 InputSelection

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Captura input bruto do Unity Input System e traduz em eventos de alto nível (clique em unidade, double-click, drag).

#### **Campos Públicos (Inspector):**

```csharp
[Header("Refs")]
public WorldPicker picker; // Referência ao raycaster

[Header("Config")]
public float dragThresholdPx = 6f;      // Distância mínima para considerar drag (pixels)
public float doubleClickWindow = 0.28f; // Janela de tempo para double-click (segundos)

[Header("Actions (arraste do seu asset)")]
public InputActionReference point;  // Vector2 - Posição do mouse
public InputActionReference lmb;    // Button - Left Mouse Button
public InputActionReference rmb;    // Button - Right Mouse Button
public InputActionReference ctrl;   // Button - Ctrl key
public InputActionReference shift;  // Button - Shift key
```

#### **Propriedades Públicas:**

```csharp
/// <summary>
/// Verifica se Ctrl está pressionado no momento.
/// </summary>
public bool IsCtrlPressed { get; }

/// <summary>
/// Verifica se Shift está pressionado no momento.
/// </summary>
public bool IsShiftPressed { get; }
```

#### **Eventos:**

```csharp
// ===== INPUT BRUTO =====
/// <summary>Disparado quando LMB é pressionado (down)</summary>
public event Action<Vector2> OnPointerDown;

/// <summary>Disparado quando LMB é solto (up)</summary>
public event Action<Vector2> OnPointerUp;

// ===== DRAG =====
/// <summary>Disparado quando drag inicia (após ultrapassar threshold)</summary>
public event Action<Vector2> OnBeginDrag;

/// <summary>Disparado continuamente durante o drag</summary>
public event Action<Vector2> OnDragging;

/// <summary>Disparado quando drag termina (LMB up após drag)</summary>
public event Action<Vector2> OnEndDrag;

// ===== CLIQUES INTERPRETADOS =====
/// <summary>
/// Disparado quando jogador clica em uma unidade.
/// </summary>
/// <param name="unit">Unidade clicada</param>
/// <param name="ctrl">Se Ctrl estava pressionado</param>
public event Action<Unit, bool> OnClickUnit;

/// <summary>
/// Disparado quando jogador duplo-clica em uma unidade.
/// </summary>
/// <param name="unit">Unidade duplo-clicada</param>
public event Action<Unit> OnDoubleClickUnit;

/// <summary>
/// Disparado quando jogador clica no terreno (ou RMB).
/// </summary>
/// <param name="worldPoint">Posição 3D no mundo</param>
/// <param name="ctrl">Se Ctrl estava pressionado</param>
public event Action<Vector3, bool> OnClickGround;
```

#### **Padrões de Uso:**

```csharp
// Sistema de Seleção escuta cliques
void OnEnable() {
    input.OnClickUnit += HandleClickUnit;
    input.OnClickGround += HandleClickGround;
    input.OnBeginDrag += HandleBeginDrag;
    input.OnEndDrag += HandleEndDrag;
    input.OnDoubleClickUnit += HandleDoubleClickUnit;
}

void OnDisable() {
    input.OnClickUnit -= HandleClickUnit;
    input.OnClickGround -= HandleClickGround;
    input.OnBeginDrag -= HandleBeginDrag;
    input.OnEndDrag -= HandleEndDrag;
    input.OnDoubleClickUnit -= HandleDoubleClickUnit;
}
```

---

### 3.2 WorldPicker

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Raycasting para converter coordenadas de tela em objetos 3D (unidades ou terreno).

#### **Campos Públicos (Inspector):**

```csharp
public Camera cam;              // Câmera usada para raycasting
public LayerMask unitMask;      // Layer "Unit" (User Layer 3)
public LayerMask groundMask;    // Layer "Ground" (User Layer 7)
```

#### **Métodos Públicos:**

```csharp
/// <summary>
/// Tenta detectar uma unidade na posição de tela fornecida.
/// Usa raycast na Layer "Unit".
/// </summary>
/// <param name="screenPos">Posição em coordenadas de tela (pixels)</param>
/// <param name="unit">Unidade detectada (out)</param>
/// <returns>True se acertou uma unidade</returns>
public bool TryPickUnitAt(Vector2 screenPos, out Unit unit)

/// <summary>
/// Tenta detectar terreno na posição de tela fornecida.
/// Usa raycast na Layer "Ground".
/// </summary>
/// <param name="screenPos">Posição em coordenadas de tela (pixels)</param>
/// <param name="point">Posição 3D do ponto de impacto (out)</param>
/// <param name="normal">Normal da superfície (out)</param>
/// <returns>True se acertou terreno</returns>
public bool TryPickGroundAt(Vector2 screenPos, out Vector3 point, out Vector3 normal)
```

#### **Implementação Interna:**

```csharp
// Simplificado para referência
public bool TryPickUnitAt(Vector2 screenPos, out Unit unit) {
    unit = null;
    var ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out var hit, maxDistance, unitMask, QueryTriggerInteraction.Collide)) {
        unit = hit.collider.GetComponentInParent<Unit>();
        return unit != null;
    }
    return false;
}
```

**Nota:** `QueryTriggerInteraction.Collide` permite detectar triggers (UnitHitProxy usa SphereCollider com `Is Trigger = true`).

---

### 3.3 SelectionManager

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Gerencia o estado de seleção (HashSet de unidades) e aplica regras de seleção (filtro por facção, modificadores Ctrl/Shift).

#### **Campos Públicos (Inspector):**

```csharp
[Header("Selection")]
[Tooltip("Px extras no retângulo para evitar perda por borda")]
public float rectInflatePx = 1.5f;

[Header("Refs")]
public PlayerController player;  // Define a facção local (Player1, etc.)
public Camera cam;               // Mesma câmera usada no WorldPicker
public InputSelection input;     // Referência ao input

[Header("Filtro")]
public bool onlyOwnUnits = true; // Nunca selecionar unidades de outra facção
```

#### **Propriedades Públicas:**

```csharp
/// <summary>
/// Seleção atual (read-only). Use métodos públicos para modificar.
/// </summary>
public IReadOnlyCollection<Unit> Selection { get; }

/// <summary>
/// Quantidade de unidades selecionadas.
/// </summary>
public int Count { get; }
```

#### **Eventos:**

```csharp
/// <summary>
/// Disparado sempre que a seleção muda (add, remove, clear).
/// ⚠️ ATENÇÃO: Evento LOCAL (não usa GameEvents ainda).
/// </summary>
public event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;
```

#### **Métodos Públicos (Para Integração com UI):**

```csharp
/// <summary>
/// Verifica se uma unidade está selecionada.
/// </summary>
public bool IsSelected(Unit u)

/// <summary>
/// Limpa seleção atual e seleciona exatamente as unidades fornecidas.
/// Dispara OnSelectionChanged.
/// </summary>
/// <param name="units">Coleção de unidades para selecionar</param>
public void SelectExactly(IEnumerable<Unit> units)

/// <summary>
/// Limpa seleção atual e seleciona exatamente uma unidade.
/// Dispara OnSelectionChanged.
/// </summary>
/// <param name="u">Unidade para selecionar</param>
public void SelectExactly(Unit u)

/// <summary>
/// Alterna estado de seleção (toggle) para cada unidade fornecida.
/// Dispara OnSelectionChanged.
/// </summary>
/// <param name="units">Coleção de unidades para alternar</param>
public void ToggleSet(IEnumerable<Unit> units)

/// <summary>
/// Adiciona unidades à seleção atual (união) sem limpar existentes.
/// Dispara OnSelectionChanged se houver mudanças.
/// </summary>
/// <param name="units">Coleção de unidades para adicionar</param>
public void AddToSelection(IEnumerable<Unit> units)

/// <summary>
/// Limpa a âncora de range (usada em seleção por shift-click - não implementado).
/// </summary>
public void ClearAnchor()
```

#### **Métodos Internos (Handlers de Input):**

```csharp
// Chamados automaticamente pelos eventos do InputSelection
void HandleClickUnit(Unit unit, bool ctrl)
void HandleClickGround(Vector3 worldPoint, bool ctrl)
void HandleEndDrag(Vector2 endScreenPos)
void HandleDoubleClickUnit(Unit unit)
```

#### **Padrões de Uso:**

```csharp
// UI de Lista de Unidades
void OnEnable() {
    selectionManager.OnSelectionChanged += UpdateListUI;
}

void UpdateListUI(IReadOnlyCollection<Unit> selectedUnits) {
    // Atualizar visual dos itens da lista
    foreach (var item in listItems) {
        bool selected = selectedUnits.Contains(item.Unit);
        item.SetSelected(selected);
    }
}

// Sistema de Comandos (futuro)
void OnRightClick(Vector3 destination) {
    foreach (var unit in selectionManager.Selection) {
        unit.MoveTo(destination);
    }
}
```

---

### 3.4 UnitHitProxy

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Facilitar raycasting em prefabs de unidades com hierarquias complexas. Atua como "ponte" entre o collider detectado e o componente `Unit`.

#### **Campos Públicos (Inspector):**

```csharp
public Unit unit; // Referência ao componente Unit (auto-atribuída no Reset)
```

#### **Método Especial:**

```csharp
void Reset() {
    // Auto-atribuição no Inspector
    unit = GetComponentInParent<Unit>();
}
```

#### **Hierarquia Típica:**

```
Worker (Unit component)
├── Armature (modelo 3D)
│   ├── Body (MeshRenderer)
│   └── ...
└── HitProxy (GameObject)
    ├── UnitHitProxy (Script)
    └── SphereCollider (Trigger, Layer: Unit)
```

#### **Por que é necessário?**

Em RTSs, prefabs de unidades frequentemente têm hierarquias complexas (modelo 3D, animações, VFX). O raycasting pode acertar um collider filho que não tem o componente `Unit` diretamente. `UnitHitProxy` garante que o raycast sempre retorne a unidade correta usando `GetComponentInParent<Unit>()`.

#### **Configuração Típica:**

1. Criar GameObject filho na raiz da unidade: `HitProxy`
2. Adicionar componente `UnitHitProxy`
3. Adicionar `SphereCollider`:
   - `Is Trigger`: ✅ Checked
   - `Radius`: ~1.1 (cobrir a unidade)
   - `Layer`: **Unit** (User Layer 3)
4. Script auto-atribui referência ao `Unit` pai

---

### 3.5 DragRectRenderer

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Feedback visual durante seleção por arrasto (desenha retângulo amarelo no chão).

#### **Campos Públicos (Inspector):**

```csharp
public InputSelection input; // Referência ao input para escutar drag
public Camera cam;           // Câmera para conversão tela→mundo
public GameObject quadPrefab; // Prefab do quad (plano 3D)
```

#### **Comportamento:**

1. **OnBeginDrag**: Instancia `quadPrefab` na posição inicial
2. **OnDragging**: Atualiza posição e escala do quad baseado em posição atual do mouse
3. **OnEndDrag**: Destrói o quad

#### **Implementação Simplificada:**

```csharp
void BeginRect(Vector2 screenStart) {
    // Converte tela → mundo via raycast
    if (Physics.Raycast(cam.ScreenPointToRay(screenStart), out var hit)) {
        _startWorld = hit.point;
        _activeQuad = Instantiate(quadPrefab);
        _activeQuad.SetActive(true);
    }
}

void UpdateRect(Vector2 screenPos) {
    if (_activeQuad == null) return;
    if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit)) {
        Vector3 endWorld = hit.point;
        Vector3 center = (_startWorld + endWorld) * 0.5f;
        Vector3 size = new Vector3(
            Mathf.Abs(endWorld.x - _startWorld.x),
            Mathf.Abs(endWorld.z - _startWorld.z),
            1f
        );
        _activeQuad.transform.position = center;
        _activeQuad.transform.localScale = size;
    }
}

void EndRect(Vector2 _) {
    if (_activeQuad != null) Destroy(_activeQuad);
}
```

#### **Prefab `Quad` (Configuração):**

- **Componentes**:
  - `Transform`
  - `Quad (Mesh Filter)` com mesh padrão do Unity
  - `Mesh Renderer` com material `SelectionMaterial`
- **Material**:
  - Shader: `Universal Render Pipeline/Lit` (ou `Unlit`)
  - Color: Amarelo semi-transparente (`RGBA: 1, 1, 0, 0.3`)
  - Rendering Mode: Transparent
- **Transform**:
  - Rotation: `(90, 0, 0)` (deitado no chão)
  - Scale: `(1, 1, 1)` (atualizado dinamicamente)

---

## 4) EVENTOS CHAVE

### 4.1 Eventos Disparados

| Componente | Evento | Assinatura | Quando Dispara | Listeners Típicos |
|------------|--------|-----------|----------------|-------------------|
| `InputSelection` | `OnClickUnit` | `Action<Unit, bool>` | LMB up em unidade (não é drag) | `SelectionManager` |
| `InputSelection` | `OnDoubleClickUnit` | `Action<Unit>` | Duplo-clique em unidade (< 280ms) | `SelectionManager` |
| `InputSelection` | `OnClickGround` | `Action<Vector3, bool>` | LMB/RMB up no terreno | `SelectionManager`, Comandos (futuro) |
| `InputSelection` | `OnBeginDrag` | `Action<Vector2>` | LMB arrasta > 6px | `SelectionManager`, `DragRectRenderer` |
| `InputSelection` | `OnDragging` | `Action<Vector2>` | Continuamente durante drag | `DragRectRenderer` |
| `InputSelection` | `OnEndDrag` | `Action<Vector2>` | LMB up após drag | `SelectionManager`, `DragRectRenderer` |
| `SelectionManager` | `OnSelectionChanged` | `Action<IReadOnlyCollection<Unit>>` | Seleção mudou (add/remove/clear) | UI, Minimap, Audio (futuro) |

### 4.2 Eventos Escutados

| Componente | Escuta | Proveniente De | Uso |
|------------|--------|---------------|-----|
| `SelectionManager` | `input.OnClickUnit` | `InputSelection` | Processar clique em unidade |
| `SelectionManager` | `input.OnClickGround` | `InputSelection` | Limpar seleção (ou comando futuro) |
| `SelectionManager` | `input.OnBeginDrag` | `InputSelection` | Armazenar posição inicial de drag |
| `SelectionManager` | `input.OnEndDrag` | `InputSelection` | Selecionar unidades no retângulo |
| `SelectionManager` | `input.OnDoubleClickUnit` | `InputSelection` | Selecionar todas do mesmo tipo |
| `DragRectRenderer` | `input.OnBeginDrag` | `InputSelection` | Começar a desenhar retângulo |
| `DragRectRenderer` | `input.OnDragging` | `InputSelection` | Atualizar retângulo |
| `DragRectRenderer` | `input.OnEndDrag` | `InputSelection` | Destruir retângulo |

### 4.3 Fluxograma de Eventos (Drag Selection)

```
┌─────────────┐
│  Jogador    │
│  Pressiona  │
│  LMB        │
└──────┬──────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│ InputSelection.OnLmbStarted()                    │
│  • _lmbDown = true                               │
│  • _pressedOverUI = IsPointerOverUI()            │
│  • if (!_pressedOverUI) OnPointerDown?.Invoke()  │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│ InputSelection.Update() (cada frame)             │
│  • if (!_dragging && Distance > 6px)             │
│    → _dragging = true                            │
│    → OnBeginDrag?.Invoke(_downPos) ───────────┐  │
└──────┬───────────────────────────────────────┐ │  │
       │                                       │ │  │
       ▼                                       ▼ ▼  ▼
┌─────────────────────────────────┐  ┌────────────────────────┐
│ SelectionManager                │  │ DragRectRenderer       │
│  .HandleBeginDrag()             │  │  .BeginRect()          │
│   • Armazena _dragStartWorld    │  │   • Instancia quadPrefab│
└─────────────────────────────────┘  └────────────────────────┘
       │
       │ (Jogador move o mouse)
       ▼
┌──────────────────────────────────────────────────┐
│ InputSelection.OnPointPerformed()                │
│  • if (_dragging) OnDragging?.Invoke(_pointer) ──┐
└──────────────────────────────────────────────────┘│
                                                    ▼
                                   ┌─────────────────────────────┐
                                   │ DragRectRenderer            │
                                   │  .UpdateRect()              │
                                   │   • Atualiza posição/escala │
                                   └─────────────────────────────┘
       │
       │ (Jogador solta LMB)
       ▼
┌──────────────────────────────────────────────────┐
│ InputSelection.OnLmbCanceled()                   │
│  • if (_dragging) OnEndDrag?.Invoke(upPos) ──────┤
└──────┬───────────────────────────────────────┬───┘
       │                                       │
       ▼                                       ▼
┌─────────────────────────────────┐  ┌────────────────────────┐
│ SelectionManager                │  │ DragRectRenderer       │
│  .HandleEndDrag()               │  │  .EndRect()            │
│   • SelectByWorldRect()         │  │   • Destroy(quadPrefab)│
│   • FireChanged() ─────────────┐│  └────────────────────────┘
└─────────────────────────────────┘│
                                   ▼
                      ┌──────────────────────────────┐
                      │ OnSelectionChanged           │
                      │   (IReadOnlyCollection<Unit>)│
                      └────────┬─────────────────────┘
                               │
                ┌──────────────┼──────────────┐
                ▼              ▼              ▼
          [UI Panel]      [Minimap]    [Audio Manager]
```

---

## 5) CONFIGURAÇÃO E SETUP

### 5.1 Layers do Projeto

O sistema de seleção depende de **Layers configuradas corretamente** para raycasting otimizado.

#### **Configuração Obrigatória:**

| Layer Number | Layer Name | Uso | Configuração |
|-------------|-----------|-----|--------------|
| **User Layer 3** | **Unit** | Colliders de unidades (UnitHitProxy) | ✅ Deve ser Trigger |
| **User Layer 7** | **Ground** | Terreno/chão (para cliques) | ✅ Pode ser sólido |

**Screenshot da Configuração:**
![Layers Configuration](reference://1761256546559_image.png)

#### **Passos para Configurar:**

1. Abra `Edit > Project Settings > Tags and Layers`
2. Expanda `Layers`
3. Configure:
   - `User Layer 3` = "Unit"
   - `User Layer 7` = "Ground"
4. **Aplique às Colliders:**
   - GameObject `HitProxy` (filho de cada unidade) → Layer "Unit"
   - GameObject `Terrain` → Layer "Ground"

---

### 5.2 Input Actions Asset

O sistema usa **Unity Input System** via `InputActionAsset`.

#### **Estrutura do Asset:**

```
InputActionAsset: "InputSystem"
└─ Action Map: "Selection"
   ├─ Point (Value, Vector2)     → Mouse Position
   ├─ LMB (Button)                → Left Mouse Button
   ├─ RMB (Button)                → Right Mouse Button
   ├─ Ctrl (Button)               → Ctrl Keys (left/right/generic)
   └─ Shift (Button)              → Shift Keys (left/right/generic)
```

#### **Bindings Detalhados:**

| Action | Type | Binding | Notes |
|--------|------|---------|-------|
| **Point** | Value (Vector2) | `<Mouse>/position` | Posição contínua do cursor |
| **LMB** | Button | `<Mouse>/leftButton` | Pressed/Released |
| **RMB** | Button | `<Mouse>/rightButton` | Pressed/Released |
| **Ctrl** | Button | `<Keyboard>/ctrl`<br>`<Keyboard>/leftCtrl`<br>`<Keyboard>/rightCtrl` | 3 bindings para cobrir ambos lados |
| **Shift** | Button | `<Keyboard>/shift`<br>`<Keyboard>/leftShift`<br>`<Keyboard>/rightShift` | 3 bindings para cobrir ambos lados |

**JSON Completo:**
```json
{
  "maps": [
    {
      "name": "Selection",
      "actions": [
        { "name": "Point", "type": "Value", "expectedControlType": "Vector2" },
        { "name": "LMB", "type": "Button" },
        { "name": "RMB", "type": "Button" },
        { "name": "Ctrl", "type": "Button" },
        { "name": "Shift", "type": "Button" }
      ],
      "bindings": [
        { "path": "<Mouse>/position", "action": "Point" },
        { "path": "<Mouse>/leftButton", "action": "LMB" },
        { "path": "<Mouse>/rightButton", "action": "RMB" },
        { "path": "<Keyboard>/ctrl", "action": "Ctrl" },
        { "path": "<Keyboard>/leftCtrl", "action": "Ctrl" },
        { "path": "<Keyboard>/rightCtrl", "action": "Ctrl" },
        { "path": "<Keyboard>/shift", "action": "Shift" },
        { "path": "<Keyboard>/leftShift", "action": "Shift" },
        { "path": "<Keyboard>/rightShift", "action": "Shift" }
      ]
    }
  ]
}
```

#### **Como Criar o Asset:**

1. `Assets > Create > Input Actions`
2. Nomeie como "InputSystem"
3. Adicione Action Map "Selection"
4. Configure Actions e Bindings conforme tabela acima
5. Clique em "Generate C# Class" (opcional, mas recomendado)
6. Salve o asset

---

### 5.3 Hierarquia de GameObjects na Cena

#### **Estrutura Recomendada:**

```
SampleScene
├─ GLOBALSCRIPTS
│  ├─ GameContext
│  ├─ PlayerSettings (PlayerController)
│  └─ Selection (GameObject raiz)
│     ├─ WorldPicker (Script)
│     ├─ InputSelection (Script)
│     ├─ SelectionManager (Script)
│     ├─ DragRectRenderer (Script)
│     └─ SelectionDebugListener (Script, opcional)
│
├─ LIGHTS
│  ├─ Global Volume
│  └─ Directional Light
│
├─ CAMERA
│  ├─ Main Camera
│  ├─ RTS_Camera (CM3)
│  └─ RTS Camera Rig
│
├─ UI
│  ├─ CanvasUI
│  ├─ EventSystem
│  └─ InputSelection (segundo GameObject, se necessário)
│
├─ TERRAIN
│  └─ Terrain (Layer: Ground)
│
└─ UNITS
   ├─ Worker
   │  └─ HitProxy (UnitHitProxy + SphereCollider, Layer: Unit)
   ├─ Archer
   │  └─ HitProxy (UnitHitProxy + SphereCollider, Layer: Unit)
   └─ ...
```

**Screenshot da Hierarquia Real:**
![Hierarchy](reference://1761256567365_image.png)

---

### 5.4 Configuração do GameObject "Selection"

#### **Inspector Completo:**

**Screenshot:**
![Selection Inspector](reference://1761256573516_image.png)

#### **WorldPicker (Script):**

| Field | Value | Description |
|-------|-------|-------------|
| **Cam** | `Main Camera (Camera)` | Arraste a Main Camera ou RTS Camera |
| **Unit Mask** | `Unit` (Layer 3) | Selecione apenas Layer "Unit" |
| **Ground Mask** | `Ground` (Layer 7) | Selecione apenas Layer "Ground" |

---

#### **InputSelection (Script):**

| Field | Value | Description |
|-------|-------|-------------|
| **Picker** | `Selection (World Picker)` | Referência ao WorldPicker no mesmo GameObject |
| **Drag Threshold Px** | `6` | Distância mínima para drag (pixels) |
| **Double Click Window** | `0.28` | Janela de tempo para double-click (segundos) |
| **Point** | `Selection/Point (Input Action Reference)` | Arraste do InputActions asset |
| **LMB** | `Selection/LMB (Input Action Reference)` | Arraste do InputActions asset |
| **RMB** | `Selection/RMB (Input Action Reference)` | Arraste do InputActions asset |
| **Ctrl** | `Selection/Ctrl (Input Action Reference)` | Arraste do InputActions asset |
| **Shift** | `Selection/Shift (Input Action Reference)` | Arraste do InputActions asset |

**Como Atribuir InputActionReference:**
1. No Inspector, clique no círculo ao lado de "Point"
2. Selecione "Selection/Point" da lista
3. Repita para LMB, RMB, Ctrl, Shift

---

#### **SelectionManager (Script):**

| Field | Value | Description |
|-------|-------|-------------|
| **Rect Inflate Px** | `1.5` | Margem extra no retângulo de drag |
| **Player** | `PlayerSettings (Player Controller)` | Referência ao PlayerController |
| **Cam** | `Main Camera (Camera)` | Mesma câmera do WorldPicker |
| **Input** | `Selection (Input Selection)` | Referência ao InputSelection no mesmo GameObject |
| **Only Own Units** | `☑ Checked` | Filtrar apenas unidades da facção do jogador |

---

### 5.5 Prefab "Quad" (Retângulo de Drag)

#### **Configuração do Prefab:**

**Screenshot do Inspector:**
![Quad Prefab](reference://1761255824463_image.png)

#### **Componentes:**

1. **Transform:**
   - Position: `(127.24, 2.036, 48.852)` (será sobrescrito dinamicamente)
   - Rotation: `(90, 0, 0)` (deitado no chão)
   - Scale: `(1, 1, 1)` (será sobrescrito dinamicamente)

2. **Quad (Mesh Filter):**
   - Mesh: `Quad` (built-in do Unity)

3. **Mesh Renderer:**
   - Material: `SelectionMaterial`
   - Cast Shadows: `Off`
   - Receive Shadows: `Off` (opcional)

#### **Material "SelectionMaterial":**

| Property | Value |
|----------|-------|
| **Shader** | `Universal Render Pipeline/Lit` |
| **Base Map** | None (cor sólida) |
| **Base Color** | Amarelo semi-transparente |
| **RGBA** | `(1, 1, 0, 0.3)` ou `#FFFF004D` |
| **Surface Type** | Transparent |
| **Rendering Mode** | Fade ou Transparent |
| **Alpha Clipping** | Off |

**Screenshot do Material:**
![Selection Material](reference://1761255824463_image.png)

---

### 5.6 Configuração de Prefab de Unidade (UnitHitProxy)

#### **Hierarquia do Prefab:**

```
Worker (Prefab)
├─ Unit (Script)
├─ Armature (Modelo 3D)
│  ├─ Body (SkinnedMeshRenderer)
│  └─ Animations...
├─ SelectionRing (GameObject, ativado quando selecionado)
└─ HitProxy (GameObject)
   ├─ UnitHitProxy (Script)
   └─ SphereCollider (Trigger)
```

**Screenshot da Hierarquia:**
![Unit Hierarchy](reference://1761256611570_image.png)

#### **HitProxy (GameObject):**

**Screenshot do Inspector:**
![HitProxy Inspector](reference://1761256627835_image.png)

| Component | Configuration |
|-----------|--------------|
| **Layer** | `Unit` (User Layer 3) |
| **UnitHitProxy (Script)** | |
| └─ Unit | `Worker (2) (Unit)` (auto-atribuído) |
| **SphereCollider** | |
| ├─ Is Trigger | `☑ Checked` |
| ├─ Center | `(0, 0.95, 0)` (ajustar para altura da unidade) |
| └─ Radius | `1.100337` (cobrir a unidade completamente) |

#### **Passos para Adicionar em Prefab Existente:**

1. Abra o prefab da unidade (ex: `Worker.prefab`)
2. Clique direito na raiz → `Create Empty`
3. Renomeie para "HitProxy"
4. Configure Transform:
   - Position: `(0, 0, 0)` (relativo à raiz)
   - Rotation: `(0, 0, 0)`
   - Scale: `(1, 1, 1)`
5. Adicione componente `UnitHitProxy`:
   - Clique em círculo de "Unit" → selecione `Worker (Unit)` (componente da raiz)
6. Adicione componente `Sphere Collider`:
   - `Is Trigger`: ✅ Checked
   - `Center`: ajustar Y para metade da altura da unidade
   - `Radius`: ajustar para cobrir a unidade (testar com Scene Gizmos)
7. Altere Layer do GameObject "HitProxy" para **"Unit"**
8. Salve o prefab

---

### 5.7 Setup Passo-a-Passo Completo

#### **Checklist de Setup:**

- [ ] **1. Configurar Layers:**
  - [ ] User Layer 3 = "Unit"
  - [ ] User Layer 7 = "Ground"

- [ ] **2. Criar Input Actions Asset:**
  - [ ] Action Map "Selection"
  - [ ] Actions: Point, LMB, RMB, Ctrl, Shift
  - [ ] Bindings configurados (ver seção 5.2)

- [ ] **3. Criar Prefab "Quad":**
  - [ ] Mesh: Quad (built-in)
  - [ ] Material: Amarelo transparente
  - [ ] Rotation: (90, 0, 0)

- [ ] **4. Adicionar UnitHitProxy aos Prefabs de Unidades:**
  - [ ] GameObject "HitProxy" em cada prefab
  - [ ] UnitHitProxy (Script)
  - [ ] SphereCollider (Trigger, Layer: Unit)

- [ ] **5. Criar GameObject "Selection" na Cena:**
  - [ ] Adicionar WorldPicker
  - [ ] Adicionar InputSelection
  - [ ] Adicionar SelectionManager
  - [ ] Adicionar DragRectRenderer (opcional)

- [ ] **6. Configurar Referências (Inspector):**
  - [ ] WorldPicker: cam, unitMask, groundMask
  - [ ] InputSelection: picker, InputActionReferences
  - [ ] SelectionManager: player, cam, input
  - [ ] DragRectRenderer: input, cam, quadPrefab

- [ ] **7. Configurar Terrain:**
  - [ ] Layer do Terrain = "Ground"

- [ ] **8. Testar:**
  - [ ] Clique simples seleciona unidade
  - [ ] Drag seleciona múltiplas
  - [ ] Duplo-clique seleciona todas do tipo
  - [ ] Ctrl adiciona/toggle
  - [ ] Clique em UI não afeta seleção

---

## 6) CASOS DE USO PRÁTICOS

### 6.1 Seleção Simples (Clique)

**Comportamento:**
- Jogador clica com LMB em uma unidade
- Seleção anterior é limpa
- Unidade clicada fica selecionada
- `selectionHighlight` é ativado

**Código Interno (SelectionManager):**

```csharp
void HandleClickUnit(Unit unit, bool ctrl) {
    // Filtro: só selecionar unidades próprias
    if (onlyOwnUnits && unit.owner != player.myFaction) return;

    if (ctrl) {
        // Ctrl: toggle
        Toggle(unit);
    } else {
        // Clique normal: substituir seleção
        Clear();
        Add(unit);
    }

    FireChanged(); // Dispara OnSelectionChanged
}
```

**Teste:**
1. Execute a cena
2. Clique em uma unidade (ex: Worker)
3. Verifique que `selectionHighlight` ficou visível
4. Clique em outra unidade
5. Primeira unidade deve desselecionar automaticamente

---

### 6.2 Seleção Aditiva (Ctrl + Clique)

**Comportamento:**
- Jogador segura Ctrl e clica em unidade
- Seleção anterior **não é limpa**
- Unidade clicada é **adicionada** (ou removida se já estava selecionada - toggle)

**Código:**

```csharp
void Toggle(Unit u) {
    if (_selection.Contains(u)) {
        Remove(u); // Remove se já estava
    } else {
        Add(u);    // Adiciona se não estava
    }
}
```

**Teste:**
1. Selecione Worker (1)
2. Segure Ctrl e clique em Worker (2)
3. Ambos devem estar selecionados
4. Segure Ctrl e clique em Worker (1) novamente
5. Worker (1) deve desselecionar (toggle)

---

### 6.3 Seleção por Arrasto (Drag)

**Comportamento:**
- Jogador clica, arrasta > 6px e solta LMB
- Todas unidades dentro do retângulo 3D são selecionadas
- Retângulo amarelo é desenhado durante o drag

**Fluxo:**

1. **OnBeginDrag** (quando arrasta > 6px):
   ```csharp
   void OnBeginDragHandler(Vector2 startScreenPos) {
       if (Physics.Raycast(cam.ScreenPointToRay(startScreenPos), out var hit))
           _dragStartWorld = hit.point; // Armazena posição 3D inicial
   }
   ```

2. **OnDragging** (contínuo):
   - `DragRectRenderer` atualiza visual do quad

3. **OnEndDrag** (quando solta LMB):
   ```csharp
   void HandleEndDrag(Vector2 endScreenPos) {
       bool ctrl = input.IsCtrlPressed;
       Vector3 endWorld = /* converte endScreenPos para 3D */;
       SelectByWorldRect(_dragStartWorld, endWorld, additive: ctrl);
       FireChanged();
   }
   ```

4. **SelectByWorldRect** (interno):
   ```csharp
   void SelectByWorldRect(Vector3 a, Vector3 b, bool additive) {
       if (!additive) Clear(); // Limpa se não for Ctrl

       // Criar bounds 3D (XZ, Y ignorado)
       var min = Vector3.Min(a, b);
       var max = Vector3.Max(a, b);
       var bounds = new Bounds();
       bounds.SetMinMax(
           new Vector3(min.x, float.MinValue, min.z),
           new Vector3(max.x, float.MaxValue, max.z)
       );

       // Testar unidades da facção do jogador
       var mine = UnitRegistry.GetByFaction(player.myFaction);
       foreach (var u in mine) {
           if (bounds.Contains(new Vector3(u.transform.position.x, 0f, u.transform.position.z))) {
               Add(u);
           }
       }
   }
   ```

**Teste:**
1. Clique e arraste sobre múltiplas unidades
2. Retângulo amarelo deve aparecer e crescer
3. Ao soltar, unidades dentro do retângulo devem ficar selecionadas
4. Teste com Ctrl para seleção aditiva

**Screenshot do Retângulo:**
![Drag Selection Visual](reference://1761255948847_image.png)

---

### 6.4 Duplo-Clique (Selecionar Todas do Tipo)

**Comportamento:**
- Jogador duplo-clica em uma unidade (< 280ms entre cliques)
- Todas unidades do **mesmo tipo** (mesmo `UnitDefinition`) **visíveis na tela** são selecionadas

**Código:**

```csharp
void HandleDoubleClickUnit(Unit unit) {
    // Filtro: só unidades próprias
    if (onlyOwnUnits && unit.owner != player.myFaction) return;

    Clear();

    // Buscar todas unidades da facção do jogador
    var mine = UnitRegistry.GetByFaction(player.myFaction);
    foreach (var u in mine) {
        // Filtro 1: Visível na tela
        if (!IsOnScreen(u.transform.position)) continue;
        
        // Filtro 2: Mesmo UnitDefinition
        if (u.def == unit.def) {
            Add(u);
        }
    }

    FireChanged();
}
```

**Helper (Verificar se está na tela):**

```csharp
bool IsOnScreen(Vector3 worldPos) {
    var sp = cam.WorldToScreenPoint(worldPos);
    return sp.z > 0 && 
           sp.x >= 0 && sp.x <= Screen.width &&
           sp.y >= 0 && sp.y <= Screen.height;
}
```

**Teste:**
1. Distribua vários Workers e Archers pela tela
2. Duplo-clique em um Worker
3. Todos Workers **visíveis** devem ser selecionados
4. Archers não devem ser afetados
5. Mova câmera para revelar Workers fora da tela inicial
6. Duplo-clique novamente: Workers fora da tela **não** são selecionados

**Nota:** Se quiser selecionar **todos** do tipo (mesmo fora da tela), remova a verificação `IsOnScreen()`.

---

### 6.5 Proteção de UI (Não Selecionar ao Clicar em Botões)

**Problema:**
- Jogador clica em botão da UI
- Input também detecta clique "no mundo"
- Unidade atrás do botão é selecionada acidentalmente

**Solução:**

`InputSelection` usa `EventSystem.IsPointerOverGameObject()` para filtrar cliques sobre UI.

**Implementação:**

```csharp
void OnLmbStarted(InputAction.CallbackContext _) {
    _lmbDown = true;
    _downPos = _pointer;

    // CRÍTICO: Marcar se começou sobre UI
    _pressedOverUI = IsPointerOverUI();
    _dragging = false;

    if (!_pressedOverUI)
        OnPointerDown?.Invoke(_downPos);
}

void OnLmbCanceled(InputAction.CallbackContext _) {
    if (_pressedOverUI) {
        // Ignorar: clique começou em UI
        _pressedOverUI = false;
        _lmbDown = false;
        return;
    }

    // Processar clique normalmente
    if (_dragging) OnEndDrag?.Invoke(_pointer);
    else HandleClick(_pointer);
}

bool IsPointerOverUI() => _overUIThisFrame;

bool ComputePointerOverUI() {
    if (EventSystem.current == null) return false;

    // Input System novo: passar deviceId
    if (Mouse.current != null)
        return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);

    // Fallback (toque)
    if (Touchscreen.current != null) {
        foreach (var t in Touchscreen.current.touches)
            if (t.isInProgress && EventSystem.current.IsPointerOverGameObject(t.touchId.ReadValue()))
                return true;
    }

    return EventSystem.current.IsPointerOverGameObject();
}
```

**Teste:**
1. Crie um botão na UI (Canvas)
2. Clique no botão
3. Unidades atrás do botão **não devem** ser selecionadas
4. Clique fora da UI
5. Unidades **devem** ser selecionadas normalmente

---

### 6.6 Integração com UI de Lista de Unidades

**Cenário:**
- Painel lateral mostra lista de unidades selecionadas
- Ao selecionar no mundo, lista atualiza
- Ao clicar na lista, unidade é focada (câmera)

**Código de UI (Exemplo Simplificado):**

```csharp
public class UnitListUI : MonoBehaviour {
    [SerializeField] SelectionManager selectionManager;
    [SerializeField] Transform listContainer;
    [SerializeField] GameObject listItemPrefab; // UnitListItemUI

    List<UnitListItemUI> items = new List<UnitListItemUI>();

    void OnEnable() {
        selectionManager.OnSelectionChanged += UpdateList;
    }

    void OnDisable() {
        selectionManager.OnSelectionChanged -= UpdateList;
    }

    void UpdateList(IReadOnlyCollection<Unit> selectedUnits) {
        // Limpar itens antigos
        foreach (var item in items) Destroy(item.gameObject);
        items.Clear();

        // Criar novos itens
        foreach (var unit in selectedUnits) {
            var itemGO = Instantiate(listItemPrefab, listContainer);
            var item = itemGO.GetComponent<UnitListItemUI>();
            item.Bind(unit);
            items.Add(item);
        }
    }
}
```

**UnitListItemUI (Fornecido):**

```csharp
public class UnitListItemUI : MonoBehaviour {
    public Image portrait;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Image barFill;    // Barra de XP (90%)
    public Image circleFill; // Círculo de XP (10%)
    public Outline outline;  // Visual de seleção

    Unit _unit;

    public void Bind(Unit unit) {
        // Desinscrever do GameEvents se já estava inscrito
        if (_unit != null) {
            GameEvents.OnUnitProgressChanged -= OnUnitProgressChanged;
        }

        _unit = unit;
        nameText.text = unit.DisplayName;
        portrait.sprite = unit.def ? unit.def.icon : null;

        Refresh();

        // Inscrever no GameEvents
        GameEvents.OnUnitProgressChanged += OnUnitProgressChanged;
    }

    void OnUnitProgressChanged(Unit changedUnit) {
        // Só atualizar se for a unidade vinculada
        if (changedUnit == _unit) {
            Refresh();
        }
    }

    void Refresh() {
        if (_unit == null) return;
        levelText.text = _unit.Level.ToString();
        
        float t = _unit.Xp01;
        barFill.fillAmount = Mathf.Min(t, 0.9f) / 0.9f;
        circleFill.fillAmount = (t <= 0.9f) ? 0f : (t - 0.9f) / 0.1f;
    }

    public void SetSelected(bool selected) {
        if (outline) outline.enabled = selected;
    }
}
```

---

## 7) INTEGRAÇÃO COM UI

### 7.1 UnitListItemUI (Classe Fornecida)

**Responsabilidade:** Exibir informações de uma unidade na UI (portrait, nome, level, barra de XP).

**Componentes:**
- **Portrait (Image)**: Ícone da unidade (`unit.def.icon`)
- **Name Text (TMP_Text)**: Nome (`unit.DisplayName`)
- **Level Text (TMP_Text)**: Nível (`unit.Level`)
- **Bar Fill (Image)**: Barra de XP (90% do progresso)
- **Circle Fill (Image)**: Círculo de XP (últimos 10%)
- **Outline**: Indicador visual de seleção

**Integração com GameEvents:**

```csharp
public void Bind(Unit unit) {
    // Limpar inscrição anterior
    if (_unit != null) {
        GameEvents.OnUnitProgressChanged -= OnUnitProgressChanged;
    }

    _unit = unit;
    
    // Atualizar dados iniciais
    nameText.text = unit.DisplayName;
    portrait.sprite = unit.def?.icon;
    Refresh();

    // Inscrever para receber atualizações de XP/Level
    GameEvents.OnUnitProgressChanged += OnUnitProgressChanged;
}

void OnUnitProgressChanged(Unit changedUnit) {
    // Filtrar: só atualizar se for a unidade vinculada
    if (changedUnit == _unit) {
        Refresh();
    }
}

void Refresh() {
    levelText.text = _unit.Level.ToString();
    
    // Barra: 0-90% = progresso linear
    // Círculo: 90-100% = progresso do círculo
    float t = _unit.Xp01;
    barFill.fillAmount = Mathf.Min(t, 0.9f) / 0.9f;
    circleFill.fillAmount = (t <= 0.9f) ? 0f : (t - 0.9f) / 0.1f;
}
```

**Nota sobre Refatoração:**
- ✅ Código já usa `GameEvents.OnUnitProgressChanged` (consistente com módulos anteriores)
- ❌ `SelectionManager.OnSelectionChanged` ainda não usa GameEvents (pendente refatoração)

---

## 8) TROUBLESHOOTING

### 8.1 "Clique não seleciona unidade"

**Possíveis Causas:**

1. **Layer incorreta:**
   - Verifique que `HitProxy` tem Layer "Unit"
   - Verifique que `WorldPicker.unitMask` inclui Layer "Unit"

2. **Collider não configurado:**
   - `HitProxy` deve ter `SphereCollider` com `Is Trigger = true`
   - Radius do collider deve cobrir a unidade

3. **UnitHitProxy.unit não atribuído:**
   - No Inspector do `HitProxy`, verifique que campo "Unit" aponta para o componente `Unit` da raiz

4. **InputActionReference não conectado:**
   - No Inspector do `InputSelection`, verifique que todos os campos (point, lmb, rmb, ctrl, shift) estão preenchidos

**Debug:**

```csharp
// Adicione log em WorldPicker.TryPickUnitAt():
if (Physics.Raycast(ray, out var hit, maxDistance, unitMask, QueryTriggerInteraction.Collide)) {
    Debug.Log($"Raycast acertou: {hit.collider.name} (Layer: {hit.collider.gameObject.layer})");
    unit = hit.collider.GetComponentInParent<Unit>();
    if (unit == null) {
        Debug.LogError("Acertou collider mas não achou Unit!");
    }
    return unit != null;
}
```

---

### 8.2 "Drag não seleciona nada"

**Possíveis Causas:**

1. **Threshold muito alto:**
   - `InputSelection.dragThresholdPx = 6` pode ser muito para toque
   - Teste com valor menor (3-4px)

2. **Terrain sem Layer "Ground":**
   - `DragRectRenderer` precisa de raycast para converter tela→mundo
   - Verifique que Terrain tem Layer "Ground"

3. **SelectByWorldRect não encontra unidades:**
   - Verifique que `player.myFaction` corresponde ao `unit.owner`
   - Adicione logs:

```csharp
void SelectByWorldRect(Vector3 a, Vector3 b, bool additive) {
    Debug.Log($"SelectByWorldRect: {a} → {b}, additive={additive}");
    
    var mine = UnitRegistry.GetByFaction(player.myFaction);
    Debug.Log($"Unidades da facção {player.myFaction}: {mine.Count}");
    
    foreach (var u in mine) {
        bool inside = bounds.Contains(new Vector3(u.transform.position.x, 0f, u.transform.position.z));
        Debug.Log($"  {u.DisplayName}: {u.transform.position} → inside={inside}");
        if (inside) Add(u);
    }
}
```

---

### 8.3 "Duplo-clique não funciona"

**Possíveis Causas:**

1. **Janela muito curta:**
   - `doubleClickWindow = 0.28` pode ser muito rápido
   - Teste com 0.4-0.5s

2. **Cliques acertam unidades diferentes:**
   - Duplo-clique precisa acertar **mesma unidade** duas vezes
   - Verifique com logs:

```csharp
void HandleClick(Vector2 screenPos) {
    if (picker.TryPickUnitAt(screenPos, out var unit)) {
        float timeSinceLastClick = Time.unscaledTime - _lastClickTime;
        bool sameUnit = unit == _lastClickedUnit;
        
        Debug.Log($"Clique: {unit.DisplayName}, tempo={timeSinceLastClick:F3}s, mesma={sameUnit}");
        
        if (sameUnit && timeSinceLastClick <= doubleClickWindow) {
            Debug.Log("→ DUPLO CLIQUE!");
            OnDoubleClickUnit?.Invoke(unit);
            // ...
        }
    }
}
```

---

### 8.4 "Clique em UI seleciona unidades"

**Causa:**
- `EventSystem` não detecta UI corretamente
- `InputSelection._pressedOverUI` não funciona

**Solução:**

1. **Verificar EventSystem na cena:**
   - Hierarchy deve ter GameObject `EventSystem`
   - Componente `EventSystem` deve estar ativo

2. **Verificar Raycaster no Canvas:**
   - Canvas deve ter componente `GraphicRaycaster`
   - Se Canvas está em World Space, precisa de `PhysicsRaycaster` na câmera

3. **Testar detecção de UI:**

```csharp
// Adicione log em InputSelection.OnLmbStarted():
void OnLmbStarted(InputAction.CallbackContext _) {
    _pressedOverUI = IsPointerOverUI();
    Debug.Log($"LMB Down: overUI={_pressedOverUI}");
    // ...
}
```

---

### 8.5 "Retângulo de drag não aparece"

**Possíveis Causas:**

1. **Prefab não atribuído:**
   - Verifique que `DragRectRenderer.quadPrefab` está preenchido no Inspector

2. **Material invisível:**
   - Verifique que material tem cor visível (alfa > 0)
   - Shader deve suportar transparência

3. **Quad fora da tela:**
   - Verifique Y do quad (deve estar próximo de Y=0 do terreno)
   - Adicione logs:

```csharp
void BeginRect(Vector2 screenStart) {
    if (Physics.Raycast(cam.ScreenPointToRay(screenStart), out var hit)) {
        _startWorld = hit.point;
        Debug.Log($"Drag start: {_startWorld}");
        _activeQuad = Instantiate(quadPrefab);
        _activeQuad.transform.position = _startWorld;
        Debug.Log($"Quad instanciado em: {_activeQuad.transform.position}");
    }
}
```

---

### 8.6 "Unidades de outras facções são selecionadas"

**Causa:**
- `SelectionManager.onlyOwnUnits = false` no Inspector

**Solução:**
1. Selecione GameObject "Selection" na Hierarchy
2. No Inspector, localize `SelectionManager (Script)`
3. Marque checkbox `Only Own Units`

---

## 9) SUGESTÕES DE REFATORAÇÃO

### 9.1 Migrar `OnSelectionChanged` para `GameEvents`

**Problema Atual:**
- `SelectionManager.OnSelectionChanged` é um evento **local**
- Inconsistente com outros módulos (Câmera, Unit, Factions usam `GameEvents`)
- UI precisa de referência direta ao `SelectionManager`

**Proposta de Refatoração:**

#### **Passo 1: Adicionar Evento em `GameEvents.cs`**

```csharp
// GameEvents.cs
public static class GameEvents {
    // ... outros eventos ...

    // ===== SELEÇÃO =====
    /// <summary>
    /// Disparado quando seleção de unidades muda.
    /// Disparado por: SelectionManager.FireChanged()
    /// </summary>
    public static event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;

    /// <summary>
    /// Helper para disparar evento de mudança de seleção.
    /// </summary>
    /// <param name="selectedUnits">Coleção de unidades selecionadas</param>
    public static void RaiseSelectionChanged(IReadOnlyCollection<Unit> selectedUnits) {
        OnSelectionChanged?.Invoke(selectedUnits);
    }
}
```

#### **Passo 2: Refatorar `SelectionManager.cs`**

```csharp
// SelectionManager.cs
public class SelectionManager : MonoBehaviour {
    // ... campos existentes ...

    // REMOVER: Evento local
    // public event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;

    // ... métodos existentes ...

    void FireChanged() {
        // ANTES: OnSelectionChanged?.Invoke(_selection);
        // DEPOIS:
        GameEvents.RaiseSelectionChanged(_selection);
    }
}
```

#### **Passo 3: Atualizar Consumers (UI, etc.)**

```csharp
// UnitListUI.cs (ANTES)
void OnEnable() {
    selectionManager.OnSelectionChanged += UpdateList; // ❌ Precisa de referência
}

// UnitListUI.cs (DEPOIS)
void OnEnable() {
    GameEvents.OnSelectionChanged += UpdateList; // ✅ Desacoplado
}

void OnDisable() {
    GameEvents.OnSelectionChanged -= UpdateList; // CRÍTICO: sempre desinscrever
}
```

**Benefícios:**
- ✅ Consistência com arquitetura do projeto
- ✅ UI não precisa de referência ao `SelectionManager`
- ✅ Múltiplos sistemas podem escutar (Minimap, Audio, IA)
- ✅ Facilita testes unitários (mock de `GameEvents`)

---

### 9.2 Remover Variável `_rangeAnchor` (Não Utilizada)

**Problema:**
- Campo `_rangeAnchor` é atribuído mas nunca lido
- Parece ser implementação incompleta de "shift-click range selection"

**Código Atual:**

```csharp
Unit _rangeAnchor; // ← Atribuído em vários lugares, nunca usado

void HandleClickUnit(Unit unit, bool ctrl) {
    // ...
    _rangeAnchor = unit; // ← Atribuído aqui
}

void HandleDoubleClickUnit(Unit unit) {
    // ...
    _rangeAnchor = unit; // ← E aqui
}

public void ClearAnchor() => _rangeAnchor = null; // ← Método público, nunca chamado
```

**Proposta:**

**Opção A: Remover completamente** (se não for usado no futuro próximo)

```csharp
// Deletar:
Unit _rangeAnchor;
public void ClearAnchor() => _rangeAnchor = null;

// Remover atribuições:
void HandleClickUnit(Unit unit, bool ctrl) {
    // ... código existente ...
    // _rangeAnchor = unit; ← DELETAR
}
```

**Opção B: Implementar funcionalidade de Shift-Click** (se for necessário)

Funcionalidade pretendida: Shift+Click seleciona **range** entre âncora e unidade clicada.

```csharp
void HandleClickUnit(Unit unit, bool ctrl, bool shift) {
    if (onlyOwnUnits && unit.owner != player.myFaction) return;

    if (shift && _rangeAnchor != null) {
        // Selecionar todas unidades entre âncora e unidade clicada
        SelectRange(_rangeAnchor, unit);
    } else if (ctrl) {
        Toggle(unit);
    } else {
        Clear();
        Add(unit);
        _rangeAnchor = unit; // Atualizar âncora apenas em clique normal
    }

    FireChanged();
}

void SelectRange(Unit from, Unit to) {
    // Implementação: buscar unidades em linha ou em área entre from e to
    // Exemplo: todas unidades em lista entre índices de from e to
}
```

**Recomendação:**
- Se funcionalidade de range não é prioritária: **Remover completamente**
- Se é funcionalidade futura: **Adicionar comentário TODO**

```csharp
// TODO: Implementar Shift-Click range selection
// Unit _rangeAnchor; // Âncora para seleção por range (shift-click)
```

---

### 9.3 Adicionar Documentação XML em Métodos Públicos

**Problema:**
- Métodos públicos de `SelectionManager` não têm comentários XML
- Dificulta IntelliSense e compreensão de API

**Proposta:**

```csharp
/// <summary>
/// Verifica se uma unidade está atualmente selecionada.
/// </summary>
/// <param name="u">Unidade para verificar</param>
/// <returns>True se a unidade está na seleção atual</returns>
public bool IsSelected(Unit u) => u != null && _selection.Contains(u);

/// <summary>
/// Limpa a seleção atual e seleciona exatamente as unidades fornecidas.
/// Dispara GameEvents.OnSelectionChanged.
/// </summary>
/// <param name="units">Coleção de unidades para selecionar. Null ou vazio limpa a seleção.</param>
public void SelectExactly(IEnumerable<Unit> units) { ... }

/// <summary>
/// Limpa a seleção atual e seleciona exatamente uma unidade.
/// Dispara GameEvents.OnSelectionChanged.
/// </summary>
/// <param name="u">Unidade para selecionar. Se null, apenas limpa a seleção.</param>
public void SelectExactly(Unit u) { ... }

/// <summary>
/// Alterna o estado de seleção (toggle) para cada unidade fornecida.
/// Se a unidade está selecionada, remove. Se não está, adiciona.
/// Dispara GameEvents.OnSelectionChanged.
/// </summary>
/// <param name="units">Coleção de unidades para alternar</param>
public void ToggleSet(IEnumerable<Unit> units) { ... }

/// <summary>
/// Adiciona unidades à seleção atual (união) sem limpar existentes.
/// Útil para seleção aditiva programática (ex: UI de lista).
/// Dispara GameEvents.OnSelectionChanged apenas se houver mudanças.
/// </summary>
/// <param name="units">Coleção de unidades para adicionar</param>
public void AddToSelection(IEnumerable<Unit> units) { ... }
```

---

### 9.4 Validação de Referências em `Awake()`

**Problema:**
- Se referências não forem atribuídas no Inspector, sistema falha silenciosamente
- Difícil debugar erros de configuração

**Proposta:**

```csharp
// SelectionManager.cs
void Awake() {
    ValidateReferences();
}

void ValidateReferences() {
    bool hasErrors = false;

    if (player == null) {
        Debug.LogError("[SelectionManager] PlayerController não atribuído!", this);
        hasErrors = true;
    }

    if (cam == null) {
        Debug.LogWarning("[SelectionManager] Camera não atribuída. Tentando usar Camera.main...", this);
        cam = Camera.main;
        if (cam == null) {
            Debug.LogError("[SelectionManager] Camera não encontrada!", this);
            hasErrors = true;
        }
    }

    if (input == null) {
        Debug.LogWarning("[SelectionManager] InputSelection não atribuído. Tentando GetComponent...", this);
        input = GetComponent<InputSelection>();
        if (input == null) {
            Debug.LogError("[SelectionManager] InputSelection não encontrado!", this);
            hasErrors = true;
        }
    }

    if (hasErrors) {
        Debug.LogError("[SelectionManager] Configuração inválida. Sistema pode não funcionar corretamente.", this);
        enabled = false; // Desabilitar componente para evitar NullReferenceExceptions
    }
}
```

**Aplicar também em:**
- `InputSelection` (validar `picker`)
- `WorldPicker` (validar `cam`)
- `DragRectRenderer` (validar `input`, `cam`, `quadPrefab`)

---

### 9.5 Otimização de `SelectByWorldRect`

**Problema Atual:**
- Usa `Bounds.Contains()` que testa inclusão em 3D (X, Y, Z)
- Y é ignorado, mas ainda testado (ineficiente)

**Proposta: Usar Rect2D (XZ)**

```csharp
void SelectByWorldRect(Vector3 a, Vector3 b, bool additive) {
    if (!additive) Clear();

    // Criar Rect2D (XZ)
    float minX = Mathf.Min(a.x, b.x);
    float maxX = Mathf.Max(a.x, b.x);
    float minZ = Mathf.Min(a.z, b.z);
    float maxZ = Mathf.Max(a.z, b.z);

    var mine = UnitRegistry.GetByFaction(player.myFaction);
    for (int i = 0; i < mine.Count; i++) {
        var u = mine[i];
        Vector3 pos = u.transform.position;
        
        // Teste 2D (mais rápido que Bounds.Contains)
        if (pos.x >= minX && pos.x <= maxX &&
            pos.z >= minZ && pos.z <= maxZ) {
            Add(u);
        }
    }
}
```

**Benefício:**
- ~10-15% mais rápido em seleções com 100+ unidades
- Código mais claro (intenção explícita de usar apenas XZ)

---

### 9.6 Adicionar Opção "Selecionar Todos Mesmo Fora da Tela" (Double-Click)

**Proposta:**

Adicionar flag configurável para double-click:

```csharp
[Header("Double-Click Behavior")]
[Tooltip("Se true, seleciona apenas unidades visíveis na tela. Se false, seleciona todas do tipo.")]
public bool doubleClickOnlyVisible = true;

void HandleDoubleClickUnit(Unit unit) {
    if (onlyOwnUnits && unit.owner != player.myFaction) return;

    Clear();
    var mine = UnitRegistry.GetByFaction(player.myFaction);
    foreach (var u in mine) {
        // Filtro de visibilidade (opcional)
        if (doubleClickOnlyVisible && !IsOnScreen(u.transform.position)) continue;
        
        // Filtro de tipo
        if (u.def == unit.def) Add(u);
    }
    
    FireChanged();
}
```

**Uso:**
- `doubleClickOnlyVisible = true`: Comportamento RTS clássico (Age of Empires)
- `doubleClickOnlyVisible = false`: Comportamento moderno (StarCraft II)

---

### 9.7 Adicionar Evento `OnHoverUnit` (Feature Futura)

**Proposta:**

Para suportar tooltips e highlight de unidades ao passar o mouse:

```csharp
// InputSelection.cs
public event Action<Unit> OnHoverUnit;    // Mouse sobre unidade
public event Action OnHoverExit;          // Mouse saiu de unidade

Unit _hoveredUnit;

void Update() {
    // ... código existente de drag ...

    // Detectar hover (apenas se não está arrastando)
    if (!_dragging && picker != null) {
        if (picker.TryPickUnitAt(_pointer, out Unit unit)) {
            if (unit != _hoveredUnit) {
                _hoveredUnit = unit;
                OnHoverUnit?.Invoke(unit);
            }
        } else {
            if (_hoveredUnit != null) {
                _hoveredUnit = null;
                OnHoverExit?.Invoke();
            }
        }
    }
}
```

**Consumer (Tooltip):**

```csharp
public class UnitTooltip : MonoBehaviour {
    [SerializeField] InputSelection input;
    [SerializeField] GameObject tooltipPanel;
    [SerializeField] TMP_Text tooltipText;

    void OnEnable() {
        input.OnHoverUnit += ShowTooltip;
        input.OnHoverExit += HideTooltip;
    }

    void OnDisable() {
        input.OnHoverUnit -= ShowTooltip;
        input.OnHoverExit -= HideTooltip;
    }

    void ShowTooltip(Unit unit) {
        tooltipPanel.SetActive(true);
        tooltipText.text = $"{unit.DisplayName}\nLv {unit.Level}\nHP: {unit.hp}/{unit.hpMax}";
    }

    void HideTooltip() {
        tooltipPanel.SetActive(false);
    }
}
```

---

### 9.8 Resumo de Prioridades de Refatoração

| # | Refatoração | Prioridade | Impacto | Esforço |
|---|-------------|-----------|---------|---------|
| 1 | Migrar `OnSelectionChanged` para `GameEvents` | 🔴 **ALTA** | Consistência arquitetural | 1-2h |
| 2 | Remover `_rangeAnchor` (ou implementar) | 🟡 Média | Limpeza de código | 15min |
| 3 | Adicionar Documentação XML | 🟡 Média | Developer Experience | 30min |
| 4 | Validação de Referências (`Awake`) | 🟡 Média | Debugging mais fácil | 45min |
| 5 | Otimização `SelectByWorldRect` | 🟢 Baixa | Performance (~10-15%) | 20min |
| 6 | Flag `doubleClickOnlyVisible` | 🟢 Baixa | Configurabilidade | 10min |
| 7 | Evento `OnHoverUnit` | 🟢 Baixa | Feature nova (tooltip) | 1-2h |

**Ordem Sugerida:**
1. Migrar para GameEvents (consistência crítica)
2. Validação de Referências (evita bugs silenciosos)
3. Remover `_rangeAnchor` (limpeza)
4. Documentação XML (melhora manutenção)
5. Otimizações e features (quando houver tempo)

---

## 10) GLOSSÁRIO

| Termo | Definição |
|-------|-----------|
| **Selection** | Conjunto de unidades atualmente escolhidas pelo jogador |
| **Box Selection** | Seleção por arrasto (drag) criando retângulo na tela |
| **Drag Threshold** | Distância mínima (pixels) para considerar movimento como drag |
| **Double-Click Window** | Janela de tempo (segundos) para detectar duplo-clique |
| **Additive Selection** | Seleção que adiciona unidades sem limpar anteriores (Ctrl) |
| **Toggle Selection** | Alternar estado (se selecionado → remove; se não → adiciona) |
| **UnitHitProxy** | Componente auxiliar para facilitar raycasting em hierarquias complexas |
| **LayerMask** | Filtro de layers para raycasting (otimização) |
| **EventSystem** | Sistema do Unity para detectar input em UI (botões, sliders) |
| **IsPointerOverGameObject** | Método do EventSystem para detectar se mouse está sobre UI |
| **QueryTriggerInteraction** | Flag de raycast (Collide = detecta triggers, Ignore = só sólidos) |
| **Anchor** | Unidade de referência para seleção por range (shift-click) |
| **Rect Inflate** | Margem extra em pixels no retângulo de drag (evita perda por borda) |
| **OnScreen** | Verificação se posição 3D está visível na viewport da câmera |
| **WorldPicker** | Componente para converter coordenadas de tela em posições/objetos 3D |
| **SelectionChanged** | Evento disparado quando conjunto de unidades selecionadas muda |

---

## 11) ESTRUTURA DE ARQUIVOS

### 11.1 Scripts

```
Assets/
└── Scripts/
    ├── Core/
    │   ├── Enums.cs (FactionId, UnitType)
    │   ├── GameEvents.cs (event bus global)
    │   └── PlayerController.cs
    │
    ├── Units/
    │   ├── Unit.cs
    │   ├── UnitDefinition.cs
    │   └── UnitRegistry.cs
    │
    ├── Selection/
    │   ├── InputSelection.cs          ← Input capture
    │   ├── WorldPicker.cs              ← Raycasting
    │   ├── SelectionManager.cs         ← Logic
    │   ├── UnitHitProxy.cs             ← Helper
    │   ├── DragRectRenderer.cs         ← Visual feedback
    │   └── SelectionDebugListener.cs   ← Debug (opcional)
    │
    └── UI/
        └── UnitListItemUI.cs           ← UI integration
```

### 11.2 Assets

```
Assets/
├── InputActions/
│   └── InputSystem.inputactions        ← Input Action Asset
│
├── Materials/
│   └── SelectionMaterial.mat           ← Material amarelo transparente
│
├── Prefabs/
│   ├── UI/
│   │   └── UnitListItem.prefab         ← Item da lista de UI
│   │
│   ├── Selection/
│   │   └── Quad.prefab                 ← Retângulo de drag
│   │
│   └── Units/
│       ├── Worker.prefab
│       │   └── HitProxy (UnitHitProxy)
│       ├── Archer.prefab
│       │   └── HitProxy (UnitHitProxy)
│       └── ...
│
└── Scenes/
    └── SampleScene.unity
```

### 11.3 Hierarquia de Cena

```
SampleScene
├── GLOBALSCRIPTS
│   ├── GameContext
│   ├── PlayerSettings (PlayerController)
│   └── Selection
│       ├── WorldPicker (Script)
│       ├── InputSelection (Script)
│       ├── SelectionManager (Script)
│       └── DragRectRenderer (Script)
│
├── CAMERA
│   └── Main Camera / RTS Camera
│
├── UI
│   ├── Canvas
│   └── EventSystem
│
├── TERRAIN
│   └── Terrain (Layer: Ground)
│
└── UNITS
    ├── Worker (1)
    │   └── HitProxy
    ├── Worker (2)
    │   └── HitProxy
    └── ...
```

---

## 12) INTEGRAÇÃO COM MÓDULOS EXISTENTES

### 12.1 Dependências Diretas

```
Selection Module
├─ Depende de: Unit (Lote 3)
│  └─ Unit.SetSelected(bool)
│  └─ Unit.owner (FactionId)
│  └─ Unit.def (UnitDefinition)
│
├─ Depende de: UnitRegistry (Lote 3)
│  └─ UnitRegistry.GetByFaction(FactionId)
│
├─ Depende de: PlayerController (Lote 1)
│  └─ player.myFaction
│
└─ Depende de: Unity Input System (externo)
   └─ InputActionReference
   └─ InputAction callbacks
```

### 12.2 Consumers (Quem Usa Selection)

```
Selection Module
├─ Consumido por: UI System
│  └─ UnitListItemUI escuta OnSelectionChanged
│  └─ UnitPanel (futuro)
│  └─ CommandButtons (futuro)
│
├─ Consumido por: Command System (futuro)
│  └─ MoveCommand usa Selection
│  └─ AttackCommand usa Selection
│
├─ Consumido por: Minimap (futuro)
│  └─ Desenha indicadores de seleção
│
└─ Consumido por: Audio System (futuro)
   └─ Toca sons de "unit selected"
```

### 12.3 Fluxo Completo: Seleção → UI → GameEvents

```
┌──────────────┐
│   Jogador    │
│ clica unidade│
└──────┬───────┘
       │
       ▼
┌──────────────────┐
│ InputSelection   │ (Captura input)
└──────┬───────────┘
       │ OnClickUnit(unit, ctrl)
       ▼
┌──────────────────┐
│ SelectionManager │ (Gerencia estado)
│  • Add(unit)     │
│  • FireChanged() │──────────┐
└──────────────────┘          │
                              │ ⚠️ ATUAL: OnSelectionChanged (local)
                              │ 🔄 REFATORAR: GameEvents.RaiseSelectionChanged
                              ▼
                    ┌──────────────────────┐
                    │  GameEvents          │ (Event bus)
                    │ .OnSelectionChanged  │
                    └──────┬───────────────┘
                           │
                ┌──────────┼──────────┬─────────────┐
                ▼          ▼          ▼             ▼
         ┌──────────┐ ┌────────┐ ┌────────┐  ┌──────────┐
         │ UnitList │ │Minimap │ │ Audio  │  │  Future  │
         │    UI    │ │        │ │        │  │ Systems  │
         └──────────┘ └────────┘ └────────┘  └──────────┘
                │
                ▼
         ┌──────────────────┐
         │ UnitListItemUI   │ (Escuta GameEvents.OnUnitProgressChanged)
         │  • Bind(unit)    │
         │  • Refresh XP    │
         └──────────────────┘
```

---

## 13) CHECKLIST DE IMPLEMENTAÇÃO

Use esta checklist para verificar se o módulo está configurado corretamente:

### Setup Inicial
- [ ] Layers configuradas (Unit = Layer 3, Ground = Layer 7)
- [ ] Input Actions Asset criado ("Selection" Action Map)
- [ ] Prefab "Quad" criado (material amarelo transparente)
- [ ] UnitHitProxy adicionado a todos prefabs de unidades

### GameObject "Selection" na Cena
- [ ] WorldPicker configurado (cam, unitMask, groundMask)
- [ ] InputSelection configurado (picker, InputActionReferences)
- [ ] SelectionManager configurado (player, cam, input)
- [ ] DragRectRenderer configurado (input, cam, quadPrefab) [opcional]

### Testes Funcionais
- [ ] Clique simples seleciona unidade (highlight aparece)
- [ ] Ctrl+Clique adiciona/remove (toggle)
- [ ] Drag seleciona múltiplas (retângulo amarelo aparece)
- [ ] Ctrl+Drag adiciona ao conjunto existente
- [ ] Duplo-clique seleciona todas do tipo visíveis
- [ ] Clique em UI não afeta seleção de unidades
- [ ] Clique em terreno limpa seleção (ou é ignorado, dependendo do setup)

### Integração com Outros Módulos
- [ ] UI escuta OnSelectionChanged (ou GameEvents após refatoração)
- [ ] UnitListItemUI atualiza corretamente
- [ ] GameEvents.OnUnitProgressChanged funciona (barra de XP)

### Refatorações Aplicadas
- [ ] OnSelectionChanged migrado para GameEvents
- [ ] _rangeAnchor removido (ou implementado)
- [ ] Documentação XML adicionada
- [ ] Validação de referências em Awake()

---

## 14) VERSÃO E STATUS

**Versão Atual:** 2.0  
**Data:** Outubro 2025  
**Status:**  
- ✅ **Implementado e Funcional**
- ⚠️ **Pendente Refatoração:** Migração para GameEvents

**Próximos Passos:**
1. Refatorar `OnSelectionChanged` → `GameEvents.RaiseSelectionChanged`
2. Implementar sistema de comandos (Move, Attack) usando `Selection`
3. Adicionar suporte a grupos de seleção (Ctrl+1-9)
4. Implementar formações (linha, coluna, quadrado)

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão do Documento:** 3.0 (Refatoração Completa)

---
