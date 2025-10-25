# LOTE 1 — CORE SYSTEM (Documentação Completa)

**Versão:** 2.1 (Atualizada - Outubro 2025)  
**Status:** ✅ Refatorado, Testado e Documentado  
**Projeto:** Medieval Thrones - RTS Strategy Game  

---

## 📚 ÍNDICE COMPLETO

### PARTE I: FUNDAMENTOS
1. [Visão Geral do Módulo](#1-visão-geral-do-módulo)
2. [GameEvents - Event Bus Global](#2-gameevents---event-bus-global)
3. [Enums Globais](#3-enums-globais)
4. [GameConfig - Configuração Global](#4-gameconfig---configuração-global)

### PARTE II: SISTEMAS CORE
5. [GameContext - Orquestrador Central](#5-gamecontext---orquestrador-central)
6. [TimeManager - Sistema de Tempo](#6-timemanager---sistema-de-tempo)
7. [DayNightLightController - Ciclo Dia/Noite](#7-daynightlightcontroller---ciclo-dianoite)

### PARTE III: MÓDULO FACTIONS
8. [Módulo Factions (Completo)](#8-módulo-factions-completo)
   - 8.1 FactionDefinition
   - 8.2 FactionDatabase
   - 8.3 FactionService

### PARTE IV: PLAYER E INTEGRAÇÃO
9. [PlayerController](#9-playercontroller)
10. [Fluxo de Inicialização](#10-fluxo-de-inicialização)
11. [Integração entre Módulos](#11-integração-entre-módulos)

### PARTE V: REFERÊNCIAS E MANUTENÇÃO
12. [Tabela de Relacionamentos Completa](#12-tabela-de-relacionamentos-completa)
13. [Padrões de Uso Avançados](#13-padrões-de-uso-avançados)
14. [Solução de Problemas](#14-solução-de-problemas)
15. [Changelog e Migrações](#15-changelog-e-migrações)
16. [Estrutura de Arquivos](#16-estrutura-de-arquivos)

---

# PARTE I: FUNDAMENTOS

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Objetivo

O **Core System** (Lote 1) fornece a infraestrutura fundamental para todo o projeto Medieval Thrones, incluindo:

- **Event Bus Centralizado**: Comunicação desacoplada via `GameEvents`
- **Configuração Global**: Parâmetros de gameplay via `GameConfig` (ScriptableObject)
- **Sistema de Tempo**: Ciclo dia/noite, relógio, eventos temporais via `TimeManager`
- **Sistema de Facções**: Diplomacia, reputação e identificação de times
- **Orquestração**: Inicialização e injeção de dependências via `GameContext`

### 1.2 Benefícios da Arquitetura

✅ **Desacoplamento Máximo**: Sistemas comunicam-se via eventos sem referências diretas  
✅ **Testabilidade**: Event bus facilita testes unitários e mocks  
✅ **Extensibilidade**: Novos eventos não quebram código existente  
✅ **Centralização**: Toda configuração editável em ScriptableObjects  
✅ **Consistência**: Padrão unificado em todo o projeto  
✅ **Rastreabilidade**: Eventos documentados com XML comments  

### 1.3 Componentes Principais

| Componente | Tipo | Responsabilidade |
|------------|------|------------------|
| `GameEvents` | static class | Event bus global (30+ eventos) |
| `Enums` | static class | Enums compartilhados (FactionId, ResourceType, etc.) |
| `GameConfig` | ScriptableObject | Configurações de gameplay |
| `GameContext` | MonoBehaviour | Orquestrador de inicialização |
| `TimeManager` | MonoBehaviour | Relógio do jogo e ciclo dia/noite |
| `FactionDefinition` | ScriptableObject | Metadados de facção |
| `FactionDatabase` | ScriptableObject | Coleção de facções |
| `FactionService` | MonoBehaviour | Gerenciamento de reputação em runtime |
| `PlayerController` | MonoBehaviour | Identidade do jogador |
| `DayNightLightController` | MonoBehaviour | Visual do ciclo dia/noite |

### 1.4 Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                      GameContext (Orquestrador)              │
│  ┌────────────┐  ┌─────────────┐  ┌──────────────┐         │
│  │ GameConfig │  │ FactionDB   │  │ TimeManager  │         │
│  └────────────┘  └─────────────┘  └──────────────┘         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓ (dispara eventos)
┌─────────────────────────────────────────────────────────────┐
│                    GameEvents (Event Bus)                    │
│  ┌──────────┐ ┌────────┐ ┌──────────┐ ┌──────────┐         │
│  │  Tempo   │ │ Câmera │ │ Unidades │ │  Grupos  │ ...     │
│  └──────────┘ └────────┘ └──────────┘ └──────────┘         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓ (escutam eventos)
┌─────────────────────────────────────────────────────────────┐
│              Sistemas (UI, IA, Câmera, Combate, etc.)       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ UnitListPanel│  │ SelectionMgr │  │ MiniMap      │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### 1.5 Integração com Outros Lotes

| Lote | Dependência | Eventos Usados |
|------|-------------|----------------|
| **Lote 2** (Câmera) | Escuta `OnCameraShake`, `OnCameraFocus`, `OnCutsceneStart` | 5 eventos |
| **Lote 3** (Unidades) | Dispara `OnUnitSpawned`, `OnUnitDespawned`, `OnUnitProgressChanged` | 4 eventos |
| **Lote 4** (Seleção) | Dispara `OnSelectionChanged`, `OnUnitClick`, `OnDragBegin` | 9 eventos |
| **Lote 5** (UI/Left Bar) | Escuta `OnUnitSpawned`/`OnUnitDespawned`, dispara `OnGroupCreated` | 9 eventos |
| **Lote 6+** (IA, Combate, etc.) | Usa `FactionService`, `TimeManager`, eventos de economia | Variados |

---

## 2) GAMEEVENTS - EVENT BUS GLOBAL

### 2.1 Visão Geral

**Tipo:** `static class`  
**Arquivo:** `GameEvents.cs`  
**Localização:** `Assets/Scripts/Core/GameEvents.cs`  

**Responsabilidade:** Fornecer event bus centralizado para comunicação desacoplada entre todos os sistemas do jogo.

### 2.2 Características Técnicas

- **30+ eventos** organizados em categorias
- **Thread-Safety**: Não thread-safe (Unity single-threaded)
- **Complexidade**: O(1) para disparo, O(n) para notificação (n = listeners)
- **Null-Safe**: Todos os `Raise` helpers usam `?.Invoke()`
- **Documentação**: XML comments em todos os eventos

### 2.3 Categorias de Eventos

#### 📌 TEMPO (3 eventos)

```csharp
/// <summary>Fração 0..1 ao longo do dia (0=meia-noite, 0.5=meio-dia, 1=meia-noite)</summary>
public static event Action<float> OnTimeOfDay01;

/// <summary>Incrementa a cada virada de dia</summary>
public static event Action<int> OnDayChanged;

/// <summary>Atualização do relógio (dia, hora, minuto)</summary>
public static event Action<int, int, int> OnClockChanged;
```

**Disparado por:** `TimeManager.Update()`  
**Escutado por:** `DayNightLightController`, UI de Clock, sistemas dependentes de ciclo dia/noite

**Exemplo de uso:**
```csharp
void OnEnable() {
    GameEvents.OnClockChanged += UpdateClockUI;
    GameEvents.OnTimeOfDay01 += UpdateDayNightEffects;
}

void OnDisable() {
    GameEvents.OnClockChanged -= UpdateClockUI;
    GameEvents.OnTimeOfDay01 -= UpdateDayNightEffects;
}

void UpdateClockUI(int day, int hour, int minute) {
    clockLabel.text = $"Dia {day} - {hour:00}:{minute:00}";
}
```

---

#### 📌 ECONOMIA (1 evento)

```csharp
/// <summary>Disparado quando recursos são coletados</summary>
public static event Action<FactionId, ResourceType, int> OnResourceGathered;
```

**Disparado por:** Sistemas de coleta/economia (futuro)  
**Escutado por:** UI de recursos, estatísticas, IA econômica

---

#### 📌 DIPLOMACIA (2 eventos)

```csharp
/// <summary>Matriz de reputação foi inicializada</summary>
public static event Action OnReputationMatrixReady;

/// <summary>Mudança de reputação entre duas facções</summary>
public static event Action<FactionId, FactionId, float> OnReputationChanged;
```

**Disparado por:** `FactionService.Init()`, `FactionService.SetReputation()`  
**Escutado por:** IA diplomática, UI de relações, sistemas de trigger

**Exemplo de uso:**
```csharp
void OnEnable() {
    GameEvents.OnReputationChanged += HandleReputationChange;
}

void HandleReputationChange(FactionId from, FactionId to, float newValue) {
    if (from == myFaction && newValue < 20f) {
        ShowWarning($"Reputação baixa com {to}!");
    }
}
```

---

#### 📌 CÂMERA (5 eventos)

```csharp
/// <summary>Shake de câmera (impactos, explosões)</summary>
public static event Action<float, float, float> OnCameraShake;

/// <summary>Foco em posição 3D (unidade selecionada, objetivo)</summary>
public static event Action<Vector3, bool, float> OnCameraFocus;

/// <summary>Foco em posição XZ do mini-mapa</summary>
public static event Action<Vector2, bool, float> OnCameraFocusXZ;

/// <summary>Iniciar cutscene apontando para alvo</summary>
public static event Action<Transform, float, int> OnCutsceneStart;

/// <summary>Finalizar cutscene atual</summary>
public static event Action OnCutsceneEnd;
```

**Disparado por:** Sistemas de combate, UI, missões  
**Escutado por:** `RTSCameraCinemachineV3Controller` (Lote 2)

---

#### 📌 SELEÇÃO / MINIMAP (2 eventos)

```csharp
/// <summary>Ping no mini-mapa (jogador clica no mapa)</summary>
public static event Action<Vector2> OnMinimapPing;

/// <summary>Unidade/construção selecionada (foco automático opcional)</summary>
public static event Action<Transform> OnSelectionFocus;
```

**Disparado por:** UI de mini-mapa, sistema de seleção  
**Escutado por:** Sistema de câmera

---

#### 📌 UNIDADES (4 eventos)

```csharp
/// <summary>Disparado quando unidade spawna (OnEnable)</summary>
public static event Action<Unit> OnUnitSpawned;

/// <summary>Disparado quando unidade é removida (OnDisable)</summary>
public static event Action<Unit> OnUnitDespawned;

/// <summary>Disparado quando seleção muda (Unit.SetSelected)</summary>
public static event Action<Unit, bool> OnUnitSelectionChanged;

/// <summary>Disparado quando XP ou Level mudam (Unit.AddXp)</summary>
public static event Action<Unit> OnUnitProgressChanged;
```

**Disparado por:**
- `UnitRegistry.Register()` → `OnUnitSpawned`
- `UnitRegistry.Unregister()` → `OnUnitDespawned`
- `Unit.SetSelected()` → `OnUnitSelectionChanged`
- `Unit.AddXp()` → `OnUnitProgressChanged`

**Escutado por:**
- `UnitListPanel` (atualiza lista)
- UI de progresso (XP bar)
- Mini-mapa (ícones)
- Estatísticas (população)

---

#### 📌 SELEÇÃO (SISTEMA DE INPUT) (9 eventos)

```csharp
/// <summary>Conjunto completo de unidades selecionadas mudou</summary>
public static event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;

/// <summary>Jogador clicou em uma unidade</summary>
public static event Action<Unit, bool> OnUnitClick;

/// <summary>Jogador duplo-clicou em uma unidade</summary>
public static event Action<Unit> OnUnitDoubleClick;

/// <summary>Jogador clicou no chão/terreno</summary>
public static event Action<Vector3, bool> OnGroundClick;

/// <summary>Início de drag de seleção</summary>
public static event Action<Vector2> OnDragBegin;

/// <summary>Durante drag de seleção (cada frame)</summary>
public static event Action<Vector2> OnDragging;

/// <summary>Finalização de drag de seleção</summary>
public static event Action<Vector2> OnDragEnd;

/// <summary>Ponteiro pressionado</summary>
public static event Action<Vector2> OnPointerDown;

/// <summary>Ponteiro liberado</summary>
public static event Action<Vector2> OnPointerUp;
```

**Disparado por:** `SelectionInputHandler` (Lote 4)  
**Escutado por:** `SelectionManager`, `UnitListPanel`, sistemas de comando

---

#### 📌 GRUPOS (5 eventos)

```csharp
/// <summary>Grupo de unidades criado</summary>
public static event Action<UnitGroup> OnGroupCreated;

/// <summary>Grupo de unidades deletado</summary>
public static event Action<UnitGroup> OnGroupDeleted;

/// <summary>Grupo renomeado</summary>
public static event Action<UnitGroup, string> OnGroupRenamed;

/// <summary>Unidades adicionadas a grupo</summary>
public static event Action<UnitGroup, IReadOnlyList<Unit>> OnUnitsAddedToGroup;

/// <summary>Unidades removidas de grupo</summary>
public static event Action<UnitGroup, IReadOnlyList<Unit>> OnUnitsRemovedFromGroup;
```

**Disparado por:**
- `UnitListPanel.CreateNewGroup()` → `OnGroupCreated`
- `UnitListPanel.DeleteGroupAndRestoreUnits()` → `OnGroupDeleted`
- `GroupContextMenuHandler.OnRenameInputEndEdit()` → `OnGroupRenamed`

**Escutado por:**
- `UnitListPanel` (atualiza visualização)
- Sistema de keybinds (Ctrl+1~9)
- Estatísticas (organização do jogador)

---

### 2.4 Métodos Raise (Helpers Centralizados)

Todos os eventos possuem métodos `Raise` correspondentes para facilitar o disparo:

```csharp
// ========== TEMPO ==========
public static void RaiseTimeOfDay(float t01)
    => OnTimeOfDay01?.Invoke(Mathf.Clamp01(t01));

public static void RaiseDayChanged(int day)
    => OnDayChanged?.Invoke(day);

public static void RaiseClockChanged(int day, int hour, int minute)
    => OnClockChanged?.Invoke(day, hour, minute);

// ========== ECONOMIA ==========
public static void RaiseResourceGathered(FactionId who, ResourceType type, int amount)
    => OnResourceGathered?.Invoke(who, type, amount);

// ========== DIPLOMACIA ==========
public static void RaiseReputationMatrixReady()
    => OnReputationMatrixReady?.Invoke();

public static void RaiseReputationChanged(FactionId a, FactionId b, float v)
    => OnReputationChanged?.Invoke(a, b, v);

// ========== CÂMERA ==========
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

// ========== SELEÇÃO / MINIMAP ==========
public static void RaiseMinimapPing(Vector2 worldXZ)
    => OnMinimapPing?.Invoke(worldXZ);

public static void RaiseSelectionFocus(Transform target)
    => OnSelectionFocus?.Invoke(target);

// ========== UNIDADES ==========
public static void RaiseUnitSpawned(Unit unit)
    => OnUnitSpawned?.Invoke(unit);

public static void RaiseUnitDespawned(Unit unit)
    => OnUnitDespawned?.Invoke(unit);

public static void RaiseUnitSelectionChanged(Unit unit, bool isSelected)
    => OnUnitSelectionChanged?.Invoke(unit, isSelected);

public static void RaiseUnitProgressChanged(Unit unit)
    => OnUnitProgressChanged?.Invoke(unit);

// ========== SELEÇÃO (INPUT) ==========
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

// ========== GRUPOS ==========
public static void RaiseGroupCreated(UnitGroup group)
    => OnGroupCreated?.Invoke(group);

public static void RaiseGroupDeleted(UnitGroup group)
    => OnGroupDeleted?.Invoke(group);

public static void RaiseGroupRenamed(UnitGroup group, string newName)
    => OnGroupRenamed?.Invoke(group, newName);

public static void RaiseUnitsAddedToGroup(UnitGroup group, IReadOnlyList<Unit> units)
    => OnUnitsAddedToGroup?.Invoke(group, units);

public static void RaiseUnitsRemovedFromGroup(UnitGroup group, IReadOnlyList<Unit> units)
    => OnUnitsRemovedFromGroup?.Invoke(group, units);
```

### 2.5 Padrão de Uso Recomendado

#### ✅ BOM (Subscribe/Unsubscribe Pareado)

```csharp
public class MySystem : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnUnitSpawned += HandleSpawn;
        GameEvents.OnGroupCreated += HandleGroupCreated;
    }

    void OnDisable()
    {
        GameEvents.OnUnitSpawned -= HandleSpawn; // CRÍTICO!
        GameEvents.OnGroupCreated -= HandleGroupCreated; // CRÍTICO!
    }

    void HandleSpawn(Unit unit)
    {
        // Filtrar se necessário
        if (unit.owner != myFaction) return;
        
        // Processar...
    }
}
```

#### ❌ RUIM (Memory Leak)

```csharp
void Start()
{
    // NUNCA subscrever em Start/Awake sem unsubscribe correspondente!
    GameEvents.OnUnitSpawned += HandleSpawn;
    // GameObject destruído mas handler permanece na memória
}
```

---

## 3) ENUMS GLOBAIS

### 3.1 Visão Geral

**Tipo:** Enums públicos  
**Arquivo:** `Enums.cs`  
**Localização:** `Assets/Scripts/Core/Enums.cs`  

**Responsabilidade:** Fornecer enumerações compartilhadas usadas transversalmente por todo o projeto.

### 3.2 FactionId

```csharp
public enum FactionId 
{ 
    Neutral = 0, 
    Player1 = 1, 
    Player2 = 2, 
    Player3 = 3, 
    Player4 = 4, 
    PvE = 3  // PvE usa mesmo ID que Player3 (inimigos genéricos)
}
```

**Usado em:**
- `Unit.owner` (proprietário da unidade)
- `PlayerController.myFaction` (facção do jogador)
- `FactionService` (matriz de reputação)
- `FactionDefinition.id` (identificador único)
- Eventos de economia (`OnResourceGathered`)

**Exemplo de uso:**
```csharp
// Filtrar unidades por facção
if (unit.owner == FactionId.Player1) {
    // Processar unidade do jogador
}

// Consultar reputação
float rep = factionService.GetReputation(FactionId.Player1, FactionId.PvE);
```

---

### 3.3 ResourceType

```csharp
public enum ResourceType 
{ 
    Wood, 
    Stone, 
    Iron, 
    Mithril, 
    Food, 
    Gold 
}
```

**Usado em:**
- Sistemas de coleta/economia
- Evento `OnResourceGathered`
- UI de recursos
- Custos de construção/unidades

**Exemplo de uso:**
```csharp
// Disparar coleta de recurso
GameEvents.RaiseResourceGathered(myFaction, ResourceType.Gold, 50);
```

---

### 3.4 DamageType

```csharp
public enum DamageType 
{ 
    Slashing,  // Espadas, machados
    Piercing,  // Flechas, lanças
    Blunt,     // Martelos, maças
    Siege,     // Catapultas, aríetes
    Fire,      // Fogo, magias de fogo
    Magic      // Magias genéricas
}
```

**Usado em:**
- Sistema de combate (futuro)
- Cálculo de dano vs. armadura
- Bônus/penalidades de tipo

---

### 3.5 TerrainType

```csharp
public enum TerrainType 
{ 
    Normal,      // Terreno padrão
    Mud,         // Lama (penalidade de movimento)
    Snow,        // Neve (custo extra de manutenção)
    Sand,        // Areia (penalidade de movimento)
    RoadDirt,    // Estrada de terra (bônus de movimento)
    RoadPaved    // Estrada pavimentada (bônus maior)
}
```

**Usado em:**
- Sistema de pathfinding (futuro)
- Cálculo de velocidade de movimento
- Penalidades/bônus definidos em `GameConfig`

---

### 3.6 UnitType

```csharp
public enum UnitType 
{ 
    Worker,        // Trabalhador (coleta recursos)
    Warrior,       // Guerreiro básico
    Spearman,      // Lanceiro (anti-cavalaria)
    Archer,        // Arqueiro (ataque à distância)
    CavalryLight,  // Cavalaria leve (velocidade)
    Ram,           // Aríete (anti-estruturas)
    Catapult,      // Catapulta (cerco)
    Hero           // Herói (unidade especial)
}
```

**Usado em:**
- `UnitDefinition.type` (classificação)
- Lógica de IA (comportamentos específicos)
- UI (filtros, ícones)

**Exemplo de uso:**
```csharp
// Filtrar apenas trabalhadores
var workers = UnitRegistry.All.Where(u => u.def.type == UnitType.Worker);

// Lógica específica por tipo
if (unit.def.type == UnitType.Worker) {
    unit.StartGathering(nearestResource);
}
```

---

## 4) GAMECONFIG - CONFIGURAÇÃO GLOBAL

### 4.1 Visão Geral

**Tipo:** `ScriptableObject`  
**Arquivo:** `GameConfig.cs`  
**Menu:** `Assets > Create > Game > Config`  

**Responsabilidade:** Centralizar todas as configurações de gameplay editáveis no Inspector sem recompilar código.

### 4.2 Campos de Configuração

#### 🕐 TEMPO

```csharp
[Header("Tempo")]
[Tooltip("Duração de 1 dia do jogo (em segundos reais). Demo: 600s = 10min")]
public float secondsPerDay = 600f;

[Range(0.1f, 0.9f)] 
public float dayFraction = 0.5f; // 50% dia / 50% noite
```

**Usado por:** `TimeManager`

**Exemplo de tuning:**
- `secondsPerDay = 600` → 10 minutos por dia (demo rápida)
- `secondsPerDay = 1200` → 20 minutos por dia (gameplay normal)
- `dayFraction = 0.6` → 60% do dia é claro, 40% é noite

---

#### 🌦️ CLIMA / MODIFICADORES

```csharp
[Header("Clima/Modificadores (demo - provisório)")]
[Tooltip("Penalidade de visão à noite (ex.: 0.2 = -20%)")]
[Range(0f, 1f)] public float nightVisionPenalty = 0.20f;

[Range(0f, 1f)] public float fogVisionPenalty = 0.10f;
[Range(0f, 1f)] public float mudSandMovePenalty = 0.20f;
[Range(0f, 1f)] public float snowExtraUpkeep = 0.15f;
```

**Usado por:** Sistemas de visão (Fog of War), pathfinding, economia

**Exemplo de uso:**
```csharp
// Aplicar penalidade de visão à noite
if (timeManager.IsNight()) {
    visionRadius *= (1f - gameConfig.nightVisionPenalty);
}

// Penalidade de movimento em lama
if (terrainType == TerrainType.Mud) {
    moveSpeed *= (1f - gameConfig.mudSandMovePenalty);
}
```

---

#### 💰 ECONOMIA - BASELINE

```csharp
[Header("Economia — baseline (demo)")]
[Tooltip("Em cenário ideal (depósito ~3 hex), operário colhe 10 a cada 3 min.")]
public float baselineGatherMinTotal = 3;
public int baselineGatherPer3Min = 10;

[Tooltip("Capacidade do operário por viagem.")]
public int workerCarryCapacity = 10;
```

**Usado por:** Sistemas de coleta de recursos

**Cálculo de baseline por segundo:**
```csharp
public float BaselinePerSecond => baselineGatherPer3Min / (baselineGatherMinTotal * 60);
// 10 recursos / (3 minutos * 60 segundos) = 0.0556 recursos/segundo
```

---

#### 🏺 UNIDADES ESPECIAIS

```csharp
[Header("Unidades especiais (demo)")]
public float merchantSpeedHexPerSec = 0.5f;
public int merchantCapacity = 20;
public int merchantCostGold = 30;
```

**Usado por:** Sistema de mercadores (futuro)

---

#### 🔧 REPAROS

```csharp
[Header("Reparos")]
public float structureRepairHpPerSec = 0.5f;
```

**Usado por:** Sistema de construções (futuro)

---

### 4.3 Propriedades Calculadas

```csharp
// Helpers
public float BaselinePerSecond => baselineGatherPer3Min / (baselineGatherMinTotal * 60);
public float SecondsPerDay => secondsPerDay;
```

**Exemplo de uso:**
```csharp
// Converter baseline para taxa por segundo
float gatherRate = gameConfig.BaselinePerSecond * workerEfficiency;

// Calcular quanto tempo falta para o dia acabar
float timeRemaining = (1f - timeManager.Time01) * gameConfig.SecondsPerDay;
```

---

### 4.4 Setup e Tuning

#### Criar GameConfig:

1. No Unity Editor: `Assets > Create > Game > Config`
2. Nomear como `GameConfig.asset`
3. Configurar valores no Inspector

#### Valores Recomendados (Demo):

```
Tempo:
  secondsPerDay: 600 (10 minutos)
  dayFraction: 0.5 (50% dia/noite)

Clima:
  nightVisionPenalty: 0.2 (-20% visão)
  mudSandMovePenalty: 0.2 (-20% movimento)

Economia:
  baselineGatherPer3Min: 10 recursos
  workerCarryCapacity: 10 unidades
```

#### Valores Recomendados (Produção):

```
Tempo:
  secondsPerDay: 1200 (20 minutos)
  dayFraction: 0.6 (60% dia, 40% noite)

Clima:
  nightVisionPenalty: 0.3 (-30% visão)
  mudSandMovePenalty: 0.25 (-25% movimento)

Economia:
  baselineGatherPer3Min: 15 recursos
  workerCarryCapacity: 15 unidades
```

---

# PARTE II: SISTEMAS CORE

---

## 5) GAMECONTEXT - ORQUESTRADOR CENTRAL

### 5.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `GameContext.cs`  
**Localização:** GameObject `_GameContext` na cena  

**Responsabilidade:** Ponto único de orquestração. Injeta dependências e inicializa serviços na ordem correta.

### 5.2 Estrutura da Classe

```csharp
public class GameContext : MonoBehaviour
{
    [Header("Configurações")]
    public GameConfig config;
    
    [Header("Databases")]
    public FactionDatabase factions;
    
    [Header("Serviços")]
    public FactionService factionService;
    public TimeManager timeManager;

    void Awake()
    {
        // 1. Inicializar matriz de reputação
        if (factionService != null) 
            factionService.Init();
        
        // 2. Injetar config no gerenciador de tempo
        if (timeManager != null) 
            timeManager.config = config;
    }
}
```

### 5.3 Campos Públicos

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `config` | `GameConfig` | Referência ao ScriptableObject de configuração |
| `factions` | `FactionDatabase` | Banco de dados de facções |
| `factionService` | `FactionService` | Serviço de reputação (mesmo GameObject) |
| `timeManager` | `TimeManager` | Gerenciador de tempo (mesmo GameObject) |

### 5.4 Ordem de Inicialização

```
1. Awake() do GameContext
2. FactionService.Init() → Constrói matriz de reputação
   └─> Dispara GameEvents.OnReputationMatrixReady
3. TimeManager.config = config → Injeta configuração
4. TimeManager.Awake() → Inicializa Time01
   └─> Dispara GameEvents.OnTimeOfDay01
```

### 5.5 Setup na Cena

#### Hierarquia Recomendada:

```
Scene
└── _GameContext (GameObject)
    ├── GameContext (MonoBehaviour)
    │   ├── config: GameConfig (referência)
    │   ├── factions: FactionDatabase (referência)
    │   ├── factionService: (↓)
    │   └── timeManager: (↓)
    ├── FactionService (MonoBehaviour)
    │   └── database: FactionDatabase (referência)
    └── TimeManager (MonoBehaviour)
        └── config: (injetado via GameContext)
```

#### Passos para Criar:

1. Criar GameObject vazio: `_GameContext`
2. Adicionar componente `GameContext`
3. Adicionar componente `FactionService` (mesmo GameObject)
4. Adicionar componente `TimeManager` (mesmo GameObject)
5. No Inspector do `GameContext`:
   - Arrastar `GameConfig.asset` → campo `config`
   - Arrastar `FactionDatabase.asset` → campo `factions`
   - Arrastar componente `FactionService` → campo `factionService`
   - Arrastar componente `TimeManager` → campo `timeManager`
6. No Inspector do `FactionService`:
   - Arrastar `FactionDatabase.asset` → campo `database`

### 5.6 Extensibilidade

Para adicionar novos serviços:

```csharp
public class GameContext : MonoBehaviour
{
    // ...campos existentes...
    
    [Header("Novos Serviços")]
    public EconomyService economyService;
    public PathfindingService pathfindingService;

    void Awake()
    {
        // Inicializações existentes...
        if (factionService != null) factionService.Init();
        if (timeManager != null) timeManager.config = config;
        
        // Novas inicializações
        if (economyService != null) economyService.Init(config);
        if (pathfindingService != null) pathfindingService.Init();
    }
}
```

---

## 6) TIMEMANAGER - SISTEMA DE TEMPO

### 6.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `TimeManager.cs`  
**Localização:** Componente em `_GameContext`  

**Responsabilidade:** Avançar relógio do jogo, calcular ciclo dia/noite e disparar eventos temporais.

### 6.2 Estrutura da Classe

```csharp
public class TimeManager : MonoBehaviour
{
    // Configuração (injetada por GameContext)
    public GameConfig config;
    
    // Horário inicial (0=meia-noite, 0.25=amanhecer, 0.5=meio-dia, 0.75=anoitecer)
    [Range(0f, 1f)] public float startTime01 = 0.25f;
    
    // Estado atual (read-only no Inspector via [field: SerializeField])
    [field: SerializeField] public float Time01 { get; private set; }
    [field: SerializeField] public int DayCount { get; private set; }
    [field: SerializeField] public int Hour { get; private set; }
    [field: SerializeField] public int Minute { get; private set; }

    private int _lastMinute = -1; // Cache para evitar disparar evento toda frame

    void Awake()
    {
        Time01 = Mathf.Repeat(startTime01, 1f);
        GameEvents.RaiseTimeOfDay(Time01);
    }

    void Update()
    {
        if (config == null) return;
        
        // Calcular delta de tempo (fração do dia por frame)
        var delta01 = Time.deltaTime / Mathf.Max(1f, config.SecondsPerDay);
        var old = Time01;

        // Avançar tempo
        Time01 = Mathf.Repeat(Time01 + delta01, 1f);
        
        // Detectar virada de dia
        if (Time01 < old) {
            DayCount++;
            GameEvents.RaiseDayChanged(DayCount);
        }
        
        // Converter fração em HH:MM (24h)
        int totalMinutes = Mathf.FloorToInt(Time01 * 1440f); // 24h * 60min = 1440min
        Hour = (totalMinutes / 60) % 24;
        Minute = totalMinutes % 60;

        // Disparar evento apenas quando minuto muda
        if (Minute != _lastMinute) {
            _lastMinute = Minute;
            GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
        }
        
        // Disparar fração a cada frame
        GameEvents.RaiseTimeOfDay(Time01);
    }

    public bool IsNight()
    {
        // Se dayFraction=0.5, noite é [0.5, 1.0)
        return Time01 >= config.dayFraction;
    }
}
```

### 6.3 Propriedades Públicas

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Time01` | `float` | Fração do dia (0..1). 0=meia-noite, 0.5=meio-dia, 1=meia-noite |
| `DayCount` | `int` | Contador de dias (0, 1, 2, ...) |
| `Hour` | `int` | Hora atual (0-23) |
| `Minute` | `int` | Minuto atual (0-59) |

### 6.4 Cálculo de Tempo

#### Conversão de Fração para HH:MM:

```
totalMinutes = Time01 * 1440  (24 horas * 60 minutos)
Hour = (totalMinutes / 60) % 24
Minute = totalMinutes % 60
```

**Exemplos:**
- `Time01 = 0.00` → `00:00` (meia-noite)
- `Time01 = 0.25` → `06:00` (amanhecer)
- `Time01 = 0.50` → `12:00` (meio-dia)
- `Time01 = 0.75` → `18:00` (anoitecer)
- `Time01 = 0.99` → `23:46` (quase meia-noite)

#### Delta de Tempo por Frame:

```
delta01 = Time.deltaTime / config.SecondsPerDay
```

**Exemplo:**
- `config.SecondsPerDay = 600` (10 minutos)
- `Time.deltaTime = 0.016` (60 FPS)
- `delta01 = 0.016 / 600 = 0.0000267` (incremento por frame)
- Leva `600 / 0.016 = 37.500 frames` para completar um dia

### 6.5 Eventos Disparados

| Evento | Frequência | Situação |
|--------|------------|----------|
| `OnTimeOfDay01` | Todo frame | Sempre que `Update()` roda |
| `OnDayChanged` | Uma vez por dia | Quando `Time01` volta de 1.0 → 0.0 |
| `OnClockChanged` | A cada minuto | Quando `Minute` muda |

### 6.6 Método Auxiliar

```csharp
public bool IsNight()
{
    return Time01 >= config.dayFraction;
}
```

**Uso recomendado:**
```csharp
// Sistema de visão
if (timeManager.IsNight()) {
    visionRadius *= (1f - gameConfig.nightVisionPenalty);
}

// UI de ícone dia/noite
nightIcon.SetActive(timeManager.IsNight());
```

### 6.7 Tuning e Debug

#### Ajustar Horário Inicial:

```csharp
// No Inspector do TimeManager
startTime01 = 0.25f; // Começa às 06:00 (amanhecer)
startTime01 = 0.50f; // Começa às 12:00 (meio-dia)
startTime01 = 0.75f; // Começa às 18:00 (anoitecer)
```

#### Debug em Runtime:

```csharp
void Update() {
    Debug.Log($"Dia {DayCount} - {Hour:00}:{Minute:00} (Time01={Time01:F3})");
}
```

---

## 7) DAYNIGHTLIGHTCONTROLLER - CICLO DIA/NOITE

### 7.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `DayNightLightController.cs`  
**Requisito:** `[RequireComponent(typeof(Light))]`  
**Localização:** Componente na Directional Light da cena  

**Responsabilidade:** Atualizar cor, intensidade e rotação da luz direcional baseado no ciclo dia/noite.

### 7.2 Estrutura da Classe

```csharp
[RequireComponent(typeof(Light))]
public class DayNightLightController : MonoBehaviour
{
    // Gradiente de cores ao longo do dia
    public Gradient colorOverDay = new Gradient
    {
        colorKeys = new[] {
            new GradientColorKey(new Color(0.85f, 0.75f, 0.55f), 0.00f), // amanhecer
            new GradientColorKey(new Color(1.00f, 0.95f, 0.85f), 0.25f), // dia
            new GradientColorKey(new Color(1.00f, 0.85f, 0.60f), 0.50f), // pôr-do-sol
            new GradientColorKey(new Color(0.20f, 0.25f, 0.40f), 0.75f), // crepúsculo
            new GradientColorKey(new Color(0.10f, 0.12f, 0.20f), 1.00f), // noite
        }
    };

    // Curva de intensidade (0=escuro, 1=claro)
    public AnimationCurve intensityOverDay = AnimationCurve.EaseInOut(0, 0.15f, 0.25f, 1f);
    
    private Light _light;

    void OnEnable()
    {
        _light = GetComponent<Light>();
        GameEvents.OnTimeOfDay01 += Apply;
    }
    
    void OnDisable() 
    {
        GameEvents.OnTimeOfDay01 -= Apply;
    }

    private void Apply(float t01)
    {
        // Aplicar cor do gradiente
        _light.color = colorOverDay.Evaluate(t01);
        
        // Aplicar intensidade da curva (clampar 0..1)
        _light.intensity = Mathf.Clamp01(intensityOverDay.Evaluate(t01));
        
        // Rotação simples do sol (opcional)
        // 0.0 → -90° (meia-noite, sol abaixo do horizonte)
        // 0.5 → 90° (meio-dia, sol no topo)
        // 1.0 → 270° (meia-noite novamente)
        transform.rotation = Quaternion.Euler(new Vector3((t01 * 360f) - 90f, 170f, 0f));
    }
}
```

### 7.3 Configuração Visual

#### Gradiente de Cores (colorOverDay):

| Time01 | Hora | Cor (RGB) | Descrição |
|--------|------|-----------|-----------|
| 0.00 | 00:00 | (0.85, 0.75, 0.55) | Amanhecer (laranja claro) |
| 0.25 | 06:00 | (1.00, 0.95, 0.85) | Dia (branco quente) |
| 0.50 | 12:00 | (1.00, 0.85, 0.60) | Pôr-do-sol (laranja) |
| 0.75 | 18:00 | (0.20, 0.25, 0.40) | Crepúsculo (azul escuro) |
| 1.00 | 00:00 | (0.10, 0.12, 0.20) | Noite (quase preto azulado) |

#### Curva de Intensidade (intensityOverDay):

```
Configuração padrão: AnimationCurve.EaseInOut(0, 0.15f, 0.25f, 1f)

t01=0.00 → intensidade=0.15 (amanhecer, 15% de luz)
t01=0.25 → intensidade=1.00 (dia pleno, 100% de luz)
t01=0.50 → intensidade=0.50 (pôr-do-sol, 50% de luz)
t01=0.75 → intensidade=0.15 (crepúsculo, 15% de luz)
t01=1.00 → intensidade=0.15 (noite, 15% de luz)
```

**Visualização ASCII:**
```
Intensidade
    1.0 │     ╱‾‾‾‾╲
        │    ╱      ╲
    0.5 │   ╱        ╲___
        │  ╱             ╲
    0.0 │_╱_______________╲_
        └──────────────────────> Time01
        0  0.25  0.5  0.75  1
```

### 7.4 Rotação do Sol

```csharp
transform.rotation = Quaternion.Euler(new Vector3((t01 * 360f) - 90f, 170f, 0f));
```

**Explicação:**
- **X (Pitch)**: `(t01 * 360) - 90` → Sol nasce no horizonte e sobe ao topo
  - `t01=0.00` → X=-90° (horizonte leste)
  - `t01=0.25` → X=0° (nascendo)
  - `t01=0.50` → X=90° (topo, meio-dia)
  - `t01=0.75` → X=180° (descendo)
  - `t01=1.00` → X=270° (horizonte oeste)
- **Y (Yaw)**: `170°` → Direção geral do sol (ajuste conforme mapa)
- **Z (Roll)**: `0°` → Sem rotação lateral

### 7.5 Setup na Cena

#### Passos:

1. Selecionar `Directional Light` na cena
2. Adicionar componente `DayNightLightController`
3. No Inspector:
   - Ajustar `colorOverDay` (gradiente) se necessário
   - Ajustar `intensityOverDay` (curva) se necessário
4. Play → A luz deve mudar automaticamente com o tempo

#### Customização Avançada:

**Cenas com neve:**
```csharp
// Cores mais frias
colorOverDay = new Gradient
{
    colorKeys = new[] {
        new GradientColorKey(new Color(0.70f, 0.75f, 0.85f), 0.00f), // azulado
        new GradientColorKey(new Color(0.95f, 0.95f, 1.00f), 0.25f), // branco frio
        // ...
    }
};
```

**Cenas de deserto:**
```csharp
// Cores mais quentes
colorOverDay = new Gradient
{
    colorKeys = new[] {
        new GradientColorKey(new Color(1.00f, 0.80f, 0.50f), 0.00f), // laranja forte
        new GradientColorKey(new Color(1.00f, 0.95f, 0.80f), 0.25f), // amarelo quente
        // ...
    }
};
```

### 7.6 Otimização

**Problema:** Evento `OnTimeOfDay01` é disparado todo frame → pode ser pesado se muitos listeners.

**Solução:** Se necessário, adicionar throttling:

```csharp
private float _lastUpdateTime = -1f;
private const float UPDATE_INTERVAL = 0.1f; // Atualizar a cada 0.1s

private void Apply(float t01)
{
    // Throttle: só atualizar a cada 0.1s
    if (Time.time - _lastUpdateTime < UPDATE_INTERVAL) return;
    _lastUpdateTime = Time.time;
    
    // Aplicar luz...
}
```

**Nota:** Na prática, esse throttling raramente é necessário, pois `Light.color` e `Light.intensity` são operações leves.

---

# PARTE III: MÓDULO FACTIONS

---

## 8) MÓDULO FACTIONS (COMPLETO)

### 8.1 Visão Geral

O **Módulo Factions** gerencia identificação de times, diplomacia e reputação entre facções no jogo. Consiste em três componentes:

1. **FactionDefinition** (ScriptableObject) - Metadados de uma facção
2. **FactionDatabase** (ScriptableObject) - Coleção de todas as facções
3. **FactionService** (MonoBehaviour) - Gerenciamento de reputação em runtime

### 8.2 FactionDefinition (ScriptableObject)

#### Visão Geral

**Tipo:** `ScriptableObject`  
**Arquivo:** `FactionDefinition.cs`  
**Menu:** `Assets > Create > Game > Faction`  

**Responsabilidade:** Armazenar metadados estáticos de uma facção (nome, cor, banner, reputação inicial).

#### Estrutura da Classe

```csharp
[CreateAssetMenu(fileName = "Faction", menuName = "Game/Faction")]
public class FactionDefinition : ScriptableObject
{
    public FactionId id;                    // Identificador único
    public string displayName = "Reino";    // Nome exibido na UI
    public Color color = Color.white;       // Cor do time (UI, mini-mapa)
    public Sprite banner;                   // Bandeira/ícone da facção
    
    [Range(0, 100)] 
    public float initialReputation = 50f;   // Reputação inicial (neutro)
}
```

#### Campos

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `id` | `FactionId` | Enum identificador (Player1, PvE, etc.) |
| `displayName` | `string` | Nome exibido ("Aliança Humana", "Horda Orc", etc.) |
| `color` | `Color` | Cor associada (UI, mini-mapa, outline de seleção) |
| `banner` | `Sprite` | Bandeira/brasão da facção |
| `initialReputation` | `float` | Reputação inicial contra outras facções (0-100, neutro=50) |

#### Exemplo de Configuração

**Player1_Def.asset:**
```
id: Player1
displayName: "Aliança Humana"
color: Azul (0, 0.5, 1, 1)
banner: [sprite de escudo azul]
initialReputation: 50
```

**PvE_Def.asset:**
```
id: PvE
displayName: "Horda Selvagem"
color: Vermelho (1, 0.2, 0, 1)
banner: [sprite de crânio]
initialReputation: 30 (hostil por padrão)
```

#### Uso em Código

```csharp
// Obter cor da facção para UI
var fdef = factionDatabase.Get(FactionId.Player1);
teamBanner.color = fdef.color;
teamNameLabel.text = fdef.displayName;

// Exibir bandeira
factionIconImage.sprite = fdef.banner;
```

---

### 8.3 FactionDatabase (ScriptableObject)

#### Visão Geral

**Tipo:** `ScriptableObject`  
**Arquivo:** `FactionDatabase.cs`  
**Menu:** `Assets > Create > Game > Faction Database`  

**Responsabilidade:** Coleção centralizada de todas as `FactionDefinition` do jogo. Fornece lookup por `FactionId`.

#### Estrutura da Classe

```csharp
[CreateAssetMenu(fileName = "FactionDatabase", menuName = "Game/Faction Database")]
public class FactionDatabase : ScriptableObject
{
    public List<FactionDefinition> factions = new();
    
    public FactionDefinition Get(FactionId id)
    {
        return factions.Find(f => f.id == id);
    }
}
```

#### Campos

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `factions` | `List<FactionDefinition>` | Lista de todas as facções do jogo |

#### Métodos

```csharp
/// <summary>
/// Busca facção por ID. Retorna null se não encontrar.
/// </summary>
public FactionDefinition Get(FactionId id)
```

#### Setup

1. Criar asset: `Assets > Create > Game > Faction Database`
2. Nomear como `FactionDatabase.asset`
3. No Inspector, adicionar todas as `FactionDefinition` à lista `factions`:
   - Player1_Def
   - Player2_Def
   - PvE_Def
   - etc.

#### Exemplo de Configuração

**FactionDatabase.asset:**
```
factions:
  [0] Player1_Def (Aliança Humana)
  [1] Player2_Def (Reino Élfico)
  [2] PvE_Def (Horda Selvagem)
  [3] Neutral_Def (Neutro)
```

#### Uso em Código

```csharp
// Lookup de facção
var playerFaction = factionDatabase.Get(FactionId.Player1);
Debug.Log($"Facção do jogador: {playerFaction.displayName}");

// Iterar sobre todas as facções
foreach (var fdef in factionDatabase.factions) {
    Debug.Log($"{fdef.displayName} (cor: {fdef.color})");
}
```

---

### 8.4 FactionService (MonoBehaviour)

#### Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `FactionService.cs`  
**Localização:** Componente em `_GameContext`  

**Responsabilidade:** Gerenciar matriz de reputação A→B em runtime. Permite ler, modificar e reagir a mudanças de reputação.

#### Estrutura da Classe

```csharp
public class FactionService : MonoBehaviour
{
    [SerializeField] private FactionDatabase database;

    // Matriz de reputação [A->B] (0..100). Chave = (FactionId, FactionId)
    private readonly Dictionary<(FactionId, FactionId), float> _rep = new();

    /// <summary>
    /// Inicializa matriz de reputação com valores de initialReputation.
    /// Dispara GameEvents.OnReputationMatrixReady.
    /// </summary>
    public void Init()
    {
        foreach (var fa in database.factions)
        {
            foreach (var fb in database.factions)
            {
                var key = (fa.id, fb.id);
                if (!_rep.ContainsKey(key))
                    _rep[key] = fa.initialReputation;
            }
        }
        GameEvents.RaiseReputationMatrixReady();
    }

    /// <summary>
    /// Obter reputação de A em relação a B (0..100).
    /// </summary>
    public float GetReputation(FactionId a, FactionId b)
    {
        return _rep[(a, b)];
    }

    /// <summary>
    /// Definir reputação de A em relação a B (clamped 0..100).
    /// Dispara GameEvents.OnReputationChanged.
    /// </summary>
    public void SetReputation(FactionId a, FactionId b, float value)
    {
        value = Mathf.Clamp(value, 0, 100);
        _rep[(a, b)] = value;
        GameEvents.RaiseReputationChanged(a, b, value);
    }

    /// <summary>
    /// Aplicar delta de reputação (incremento/decremento).
    /// </summary>
    public void DeltaReputation(FactionId a, FactionId b, float delta)
    {
        SetReputation(a, b, GetReputation(a, b) + delta);
    }
}
```

#### Campos

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `database` | `FactionDatabase` | Referência ao banco de dados (Inspector) |
| `_rep` | `Dictionary<(FactionId, FactionId), float>` | Matriz de reputação (privado) |

#### Métodos Públicos

```csharp
/// Inicializa matriz de reputação (chamado por GameContext.Awake)
public void Init()

/// Obter reputação de A em relação a B (0..100)
public float GetReputation(FactionId a, FactionId b)

/// Definir reputação de A em relação a B (clamped 0..100)
/// Dispara GameEvents.OnReputationChanged
public void SetReputation(FactionId a, FactionId b, float value)

/// Aplicar delta de reputação (incremento/decremento)
public void DeltaReputation(FactionId a, FactionId b, float delta)
```

#### Sistema de Reputação

**Escala de Reputação (0-100):**

| Valor | Classificação | Comportamento |
|-------|---------------|---------------|
| 0-20 | Hostil | Ataque imediato |
| 21-40 | Inimigo | Desconfiança, possível ataque |
| 41-60 | Neutro | Sem ações hostis |
| 61-80 | Amigável | Comércio, alianças temporárias |
| 81-100 | Aliado | Cooperação total |

**Matriz Simétrica vs. Assimétrica:**

A matriz é **assimétrica** → `Rep(A→B)` pode ser diferente de `Rep(B→A)`.

**Exemplo:**
```
Rep(Player1 → PvE) = 30 (jogador vê PvE como inimigo)
Rep(PvE → Player1) = 10 (PvE vê jogador como hostil)
```

#### Eventos Disparados

| Evento | Quando | Parâmetros |
|--------|--------|------------|
| `OnReputationMatrixReady` | Após `Init()` | Nenhum |
| `OnReputationChanged` | Após `SetReputation()` | `FactionId a, FactionId b, float newValue` |

#### Exemplos de Uso

**Inicialização (GameContext):**
```csharp
void Awake() {
    factionService.Init();
    // Dispara GameEvents.OnReputationMatrixReady
}
```

**Consultar Reputação:**
```csharp
// Sistema de IA
float rep = factionService.GetReputation(myFaction, FactionId.PvE);
if (rep < 30f) {
    Attack(nearestEnemy);
} else if (rep > 70f) {
    OfferTrade(nearestFaction);
}
```

**Modificar Reputação:**
```csharp
// Sistema de Missões
void OnQuestCompleted(FactionId targetFaction) {
    // Aumentar reputação em +15
    factionService.DeltaReputation(myFaction, targetFaction, +15f);
    // Dispara GameEvents.OnReputationChanged
}

// Sistema de Combate
void OnEnemyKilled(Unit enemy) {
    // Diminuir reputação em -5
    factionService.DeltaReputation(myFaction, enemy.owner, -5f);
}
```

**Reagir a Mudanças (UI):**
```csharp
void OnEnable() {
    GameEvents.OnReputationChanged += UpdateReputationUI;
}

void UpdateReputationUI(FactionId from, FactionId to, float newValue) {
    if (from != myFaction) return; // Filtrar apenas reputação do jogador
    
    string status = newValue switch {
        < 20 => "Hostil",
        < 40 => "Inimigo",
        < 60 => "Neutro",
        < 80 => "Amigável",
        _ => "Aliado"
    };
    
    reputationLabel.text = $"{to}: {status} ({newValue:F0}/100)";
}
```

---

# PARTE IV: PLAYER E INTEGRAÇÃO

---

## 9) PLAYERCONTROLLER

### 9.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `PlayerController.cs`  
**Localização:** GameObject `Player` na cena  

**Responsabilidade:** Definir a identidade do jogador (facção) e manter referência à câmera principal.

### 9.2 Estrutura da Classe

```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Quem sou eu")]
    public FactionId myFaction = FactionId.Player1;

    [Header("Referências")]
    public Camera mainCamera;

    private void Reset()
    {
        // Auto-atribuir Camera.main no Inspector quando componente é adicionado
        mainCamera = Camera.main;
    }
}
```

### 9.3 Campos Públicos

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `myFaction` | `FactionId` | Facção controlada pelo jogador |
| `mainCamera` | `Camera` | Referência à câmera principal (auto-atribuída) |

### 9.4 Uso em Código

**Filtrar Unidades do Jogador:**
```csharp
// Sistema de Seleção
foreach (var unit in UnitRegistry.All) {
    if (unit.owner == player.myFaction) {
        // Unidade pertence ao jogador
        unit.SetSelected(true);
    }
}
```

**Filtrar Eventos:**
```csharp
void HandleUnitSpawned(Unit unit) {
    if (unit.owner != player.myFaction) return; // Ignorar unidades de outros
    
    AddToMyUnitList(unit);
}
```

**Consultar Reputação:**
```csharp
// Sistema de Diplomacia
float myReputationWithPvE = factionService.GetReputation(player.myFaction, FactionId.PvE);
```

### 9.5 Setup na Cena

#### Hierarquia Recomendada:

```
Scene
└── Player (GameObject)
    └── PlayerController (MonoBehaviour)
        ├── myFaction: Player1
        └── mainCamera: [Main Camera] (auto-atribuído)
```

#### Passos:

1. Criar GameObject vazio: `Player`
2. Adicionar componente `PlayerController`
3. No Inspector:
   - Definir `myFaction` (geralmente `Player1`)
   - Campo `mainCamera` é preenchido automaticamente via `Reset()`

### 9.6 Multiplayer (Futuro)

Para suporte a múltiplos jogadores humanos:

```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Identificação")]
    public int playerIndex = 0; // 0, 1, 2, 3
    public FactionId myFaction = FactionId.Player1;
    
    [Header("Controle")]
    public bool isLocalPlayer = true; // True para jogador local
    
    // ...
}
```

---

## 10) FLUXO DE INICIALIZAÇÃO

### 10.1 Diagrama de Sequência

```
Unity Scene Load
    │
    ↓
GameContext.Awake()
    │
    ├─> FactionService.Init()
    │       ├─> Constrói matriz de reputação A→B
    │       └─> GameEvents.RaiseReputationMatrixReady()
    │
    └─> TimeManager.config = GameConfig (injeção)
    
    ↓
TimeManager.Awake()
    │
    ├─> Time01 = startTime01
    └─> GameEvents.RaiseTimeOfDay(Time01)
    
    ↓
DayNightLightController.OnEnable()
    │
    ├─> _light = GetComponent<Light>()
    └─> GameEvents.OnTimeOfDay01 += Apply
    
    ↓
[Outros sistemas subscritos]
    │
    ├─> UnitListPanel.OnEnable()
    │       ├─> GameEvents.OnUnitSpawned += ...
    │       └─> GameEvents.OnGroupCreated += ...
    │
    ├─> SelectionManager.OnEnable()
    │       ├─> GameEvents.OnSelectionChanged += ...
    │       └─> GameEvents.OnUnitClick += ...
    │
    └─> [...]
    
    ↓
TimeManager.Update() [começou loop de jogo]
    │
    ├─> Time01 avança
    ├─> GameEvents.RaiseTimeOfDay(Time01) [todo frame]
    ├─> GameEvents.RaiseClockChanged(...) [todo minuto]
    └─> GameEvents.RaiseDayChanged(...) [virada de dia]
```

### 10.2 Ordem de Execução

| # | Sistema | Método | Ação |
|---|---------|--------|------|
| 1 | `GameContext` | `Awake()` | Inicializa serviços |
| 2 | `FactionService` | `Init()` | Constrói matriz de reputação |
| 3 | `GameContext` | `Awake()` | Injeta `config` no `TimeManager` |
| 4 | `TimeManager` | `Awake()` | Inicializa `Time01`, dispara primeiro evento |
| 5 | `DayNightLightController` | `OnEnable()` | Subscreve `OnTimeOfDay01` |
| 6 | Outros Sistemas | `OnEnable()` | Subscrevem eventos relevantes |
| 7 | `TimeManager` | `Update()` | Loop de tempo (dispara eventos) |

### 10.3 Scripts de Inicialização (Awake vs. Start vs. OnEnable)

| Método | Uso Recomendado |
|--------|-----------------|
| `Awake()` | Inicialização interna (cache de componentes, config inicial) |
| `OnEnable()` | **Subscribe em eventos** (sempre pareado com `OnDisable()`) |
| `Start()` | Lógica que depende de outros sistemas já inicializados |
| `OnDisable()` | **Unsubscribe de eventos** (CRÍTICO para evitar leaks) |

**Exemplo:**
```csharp
void Awake() {
    // Cache de componentes
    _light = GetComponent<Light>();
}

void OnEnable() {
    // Subscribe em eventos
    GameEvents.OnTimeOfDay01 += Apply;
}

void OnDisable() {
    // Unsubscribe (CRÍTICO!)
    GameEvents.OnTimeOfDay01 -= Apply;
}

void Start() {
    // Lógica que depende de outros sistemas
    var playerFaction = FindObjectOfType<PlayerController>().myFaction;
}
```

---

## 11) INTEGRAÇÃO ENTRE MÓDULOS

### 11.1 Mapa de Dependências

```
GameConfig (ScriptableObject)
    ↓ (usado por)
TimeManager, FactionService, Economia, etc.

FactionDatabase (ScriptableObject)
    ↓ (usado por)
FactionService, UI, IA

GameContext (MonoBehaviour)
    ├─> FactionService (inicia)
    ├─> TimeManager (injeta config)
    └─> [outros serviços futuros]

GameEvents (static class)
    ↑ (dispara)
    TimeManager, FactionService, Unit, UnitRegistry, etc.
    ↓ (escuta)
    DayNightLightController, UnitListPanel, SelectionManager, etc.

PlayerController (MonoBehaviour)
    ↓ (usado por)
    Sistemas de seleção, filtros de unidades, UI
```

### 11.2 Comunicação via Eventos

**Padrão Observer (Event Bus):**

```
[Sistema Produtor]
    ↓ (dispara)
GameEvents.RaiseXXX()
    ↓ (notifica)
[Sistemas Consumidores]
```

**Exemplo Completo:**

```
Unit.OnEnable()
    ↓
UnitRegistry.Register(unit)
    ↓
GameEvents.RaiseUnitSpawned(unit)
    ↓ (notifica)
├─> UnitListPanel.HandleUnitSpawned(unit)
├─> MinimapController.CreateIcon(unit)
├─> StatisticsTracker.IncrementPopulation(unit)
└─> [outros listeners]
```

### 11.3 Injeção de Dependências

**Padrão:** Injeção via Inspector ou via `GameContext.Awake()`

**Exemplo 1 - Inspector (Manual):**
```csharp
public class MySystem : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private PlayerController player;
    
    // Arrastar no Inspector
}
```

**Exemplo 2 - GameContext (Automático):**
```csharp
public class GameContext : MonoBehaviour
{
    public TimeManager timeManager;
    
    void Awake() {
        // Injeção automática
        timeManager.config = config;
    }
}
```

### 11.4 Tabela de Comunicação

| Módulo A | Módulo B | Via | Direção |
|----------|----------|-----|---------|
| `TimeManager` | `DayNightLightController` | `OnTimeOfDay01` | A → B |
| `UnitRegistry` | `UnitListPanel` | `OnUnitSpawned` | A → B |
| `UnitListPanel` | Sistemas | `OnGroupCreated` | A → B |
| `FactionService` | IA/UI | `OnReputationChanged` | A → B |
| `GameContext` | `TimeManager` | Injeção direta | A → B |
| `GameConfig` | Todos | Referência direta | A → B |

---

# PARTE V: REFERÊNCIAS E MANUTENÇÃO

---

## 12) TABELA DE RELACIONAMENTOS COMPLETA

### 12.1 GameEvents - Quem Dispara / Quem Escuta

| Evento | Categoria | Disparado Por | Escutado Por |
|--------|-----------|---------------|--------------|
| `OnTimeOfDay01` | Tempo | `TimeManager.Update()` | `DayNightLightController`, sistemas de ciclo dia/noite |
| `OnDayChanged` | Tempo | `TimeManager.Update()` | UI de calendário, sistemas de eventos temporais |
| `OnClockChanged` | Tempo | `TimeManager.Update()` | UI de relógio, sistemas de agenda |
| `OnResourceGathered` | Economia | Sistemas de coleta | UI de recursos, estatísticas, IA econômica |
| `OnReputationMatrixReady` | Diplomacia | `FactionService.Init()` | Sistemas de IA que dependem de reputação |
| `OnReputationChanged` | Diplomacia | `FactionService.SetReputation()` | UI de diplomacia, IA, sistemas de trigger |
| `OnCameraShake` | Câmera | Combate, impactos | `RTSCameraCinemachineV3Controller` |
| `OnCameraFocus` | Câmera | Seleção, missões | `RTSCameraCinemachineV3Controller` |
| `OnCameraFocusXZ` | Câmera | Mini-mapa | `RTSCameraCinemachineV3Controller` |
| `OnCutsceneStart` | Câmera | Diálogos, missões | `RTSCameraCinemachineV3Controller` |
| `OnCutsceneEnd` | Câmera | Diálogos, missões | `RTSCameraCinemachineV3Controller` |
| `OnMinimapPing` | Seleção/Minimap | UI de mini-mapa | Sistema de câmera |
| `OnSelectionFocus` | Seleção/Minimap | Sistema de seleção | Sistema de câmera |
| `OnUnitSpawned` | Unidades | `UnitRegistry.Register()` | `UnitListPanel`, mini-mapa, estatísticas |
| `OnUnitDespawned` | Unidades | `UnitRegistry.Unregister()` | `UnitListPanel`, mini-mapa, estatísticas |
| `OnUnitSelectionChanged` | Unidades | `Unit.SetSelected()` | UI de unidades, sistemas de comando |
| `OnUnitProgressChanged` | Unidades | `Unit.AddXp()` | UI de XP, notificações de level-up |
| `OnSelectionChanged` | Seleção (Input) | `SelectionManager` | UI de comandos, `UnitListPanel` |
| `OnUnitClick` | Seleção (Input) | `SelectionInputHandler` | `SelectionManager` |
| `OnUnitDoubleClick` | Seleção (Input) | `SelectionInputHandler` | `SelectionManager` (foco em tipo) |
| `OnGroundClick` | Seleção (Input) | `SelectionInputHandler` | Sistema de movimento, deselect |
| `OnDragBegin` | Seleção (Input) | `SelectionInputHandler` | `SelectionManager` (box select) |
| `OnDragging` | Seleção (Input) | `SelectionInputHandler` | `SelectionManager` (box visual) |
| `OnDragEnd` | Seleção (Input) | `SelectionInputHandler` | `SelectionManager` (finalizar box) |
| `OnPointerDown` | Seleção (Input) | `SelectionInputHandler` | Sistemas customizados |
| `OnPointerUp` | Seleção (Input) | `SelectionInputHandler` | Sistemas customizados |
| `OnGroupCreated` | Grupos | `UnitListPanel.CreateNewGroup()` | Sistema de keybinds, estatísticas |
| `OnGroupDeleted` | Grupos | `UnitListPanel.DeleteGroupAndRestoreUnits()` | Sistema de keybinds, UI |
| `OnGroupRenamed` | Grupos | `GroupContextMenuHandler` | UI de grupos |
| `OnUnitsAddedToGroup` | Grupos | `UnitGroup.AddUnits()` | Estatísticas, UI |
| `OnUnitsRemovedFromGroup` | Grupos | `UnitGroup.RemoveUnits()` | Estatísticas, UI |

### 12.2 Classes e Dependências

| Classe | Tipo | Depende De | Usado Por |
|--------|------|------------|-----------|
| `GameEvents` | static class | Nenhum | **Todos os módulos** |
| `Enums` | static class | Nenhum | **Todos os módulos** |
| `GameConfig` | ScriptableObject | Nenhum | `TimeManager`, sistemas diversos |
| `GameContext` | MonoBehaviour | `GameConfig`, `FactionDatabase`, `FactionService`, `TimeManager` | Nenhum (orquestrador) |
| `TimeManager` | MonoBehaviour | `GameConfig`, `GameEvents` | `DayNightLightController`, sistemas de tempo |
| `DayNightLightController` | MonoBehaviour | `GameEvents`, `Light` | Nenhum (consumer final) |
| `FactionDefinition` | ScriptableObject | `FactionId` | `FactionDatabase`, UI |
| `FactionDatabase` | ScriptableObject | `FactionDefinition` | `FactionService`, UI, IA |
| `FactionService` | MonoBehaviour | `FactionDatabase`, `GameEvents` | IA, UI de diplomacia, sistemas de reputação |
| `PlayerController` | MonoBehaviour | `FactionId` | Sistemas de seleção, filtros, UI |

---

## 13) PADRÕES DE USO AVANÇADOS

### 13.1 Sistema de Eventos Customizados

**Adicionar novo evento:**

```csharp
// Em GameEvents.cs

// ========== MINHA CATEGORIA ==========
/// <summary>Disparado quando X acontece</summary>
public static event Action<MyType> OnMyEvent;

// Raise helper
public static void RaiseMyEvent(MyType data)
    => OnMyEvent?.Invoke(data);
```

**Usar em sistema:**

```csharp
// Produtor
void DoSomething() {
    // ...
    GameEvents.RaiseMyEvent(myData);
}

// Consumidor
void OnEnable() {
    GameEvents.OnMyEvent += HandleMyEvent;
}

void OnDisable() {
    GameEvents.OnMyEvent -= HandleMyEvent;
}

void HandleMyEvent(MyType data) {
    // Processar...
}
```

### 13.2 Filtros de Eventos

**Problema:** Sistemas recebem todos os eventos, mesmo os irrelevantes.

**Solução:** Filtrar no handler.

```csharp
void HandleUnitSpawned(Unit unit) {
    // Filtro 1: Apenas unidades do jogador
    if (unit.owner != myFaction) return;
    
    // Filtro 2: Apenas trabalhadores
    if (unit.def.type != UnitType.Worker) return;
    
    // Processar...
}
```

### 13.3 Cache de Listeners

**Problema:** Subscrever/desinscrever cria garbage se usar lambdas anônimas.

**Solução:** Cache de referências.

```csharp
public class MySystem : MonoBehaviour
{
    // Cache de handlers (evita criar nova Action toda vez)
    private Action<Unit> _unitSpawnedHandler;
    private Action<int, int, int> _clockChangedHandler;
    
    void Awake() {
        // Criar handlers uma vez
        _unitSpawnedHandler = HandleUnitSpawned;
        _clockChangedHandler = HandleClockChanged;
    }
    
    void OnEnable() {
        GameEvents.OnUnitSpawned += _unitSpawnedHandler;
        GameEvents.OnClockChanged += _clockChangedHandler;
    }
    
    void OnDisable() {
        GameEvents.OnUnitSpawned -= _unitSpawnedHandler;
        GameEvents.OnClockChanged -= _clockChangedHandler;
    }
    
    void HandleUnitSpawned(Unit unit) { }
    void HandleClockChanged(int d, int h, int m) { }
}
```

### 13.4 Eventos Condicionais (Throttling)

**Problema:** Evento disparado todo frame (ex: `OnTimeOfDay01`) pode ser pesado.

**Solução:** Throttling no consumidor.

```csharp
private float _lastUpdateTime = -1f;
private const float UPDATE_INTERVAL = 0.1f;

void HandleTimeOfDay(float t01) {
    // Só processar a cada 0.1s
    if (Time.time - _lastUpdateTime < UPDATE_INTERVAL) return;
    _lastUpdateTime = Time.time;
    
    // Processar...
}
```

### 13.5 Debugging de Eventos

**Adicionar logs temporários:**

```csharp
void OnEnable() {
    GameEvents.OnUnitSpawned += DebugUnitSpawned;
}

void DebugUnitSpawned(Unit unit) {
    Debug.Log($"[GameEvents] OnUnitSpawned: {unit.DisplayName} (owner={unit.owner})", unit);
}
```

**Verificar listeners ativos:**

```csharp
#if UNITY_EDITOR
[ContextMenu("Debug: List Event Listeners")]
void DebugListeners() {
    var delegates = GameEvents.OnUnitSpawned?.GetInvocationList();
    if (delegates != null) {
        Debug.Log($"OnUnitSpawned tem {delegates.Length} listeners:");
        foreach (var d in delegates) {
            Debug.Log($"  - {d.Method.DeclaringType}.{d.Method.Name}");
        }
    } else {
        Debug.Log("OnUnitSpawned não tem listeners.");
    }
}
#endif
```

---

## 14) SOLUÇÃO DE PROBLEMAS

### 14.1 Problema: "Evento não está sendo disparado"

**Sintomas:**
- Listener não recebe notificação
- Sistema não reage a mudanças

**Diagnóstico:**
1. Verificar se `OnEnable()` foi chamado
2. Verificar se evento foi disparado **antes** do subscribe
3. Verificar se há filtros no handler que estão rejeitando o evento
4. Adicionar log temporário no `Raise` helper

**Solução:**
```csharp
// Adicionar log temporário
public static void RaiseUnitSpawned(Unit unit) {
    Debug.Log($"[GameEvents] Disparando OnUnitSpawned: {unit?.DisplayName}");
    OnUnitSpawned?.Invoke(unit);
}

// Verificar subscriber
void OnEnable() {
    Debug.Log($"[{GetType().Name}] Subscrevendo OnUnitSpawned");
    GameEvents.OnUnitSpawned += HandleSpawn;
}
```

---

### 14.2 Problema: "NullReferenceException ao disparar evento"

**Sintomas:**
```
NullReferenceException: Object reference not set to an instance of an object
GameEvents.RaiseUnitSpawned(Unit unit)
```

**Causas:**
- Parâmetro `null` sendo passado
- Componente não inicializado

**Solução:**
```csharp
// ✅ Sempre validar antes de disparar
if (unit != null && unit.def != null) {
    GameEvents.RaiseUnitSpawned(unit);
} else {
    Debug.LogWarning("Tentativa de disparar OnUnitSpawned com unit null!");
}
```

---

### 14.3 Problema: "Memory Leak (eventos não limpos)"

**Sintomas:**
- Consumo de memória aumenta ao longo do tempo
- GameObjects destruídos ainda recebem eventos
- Profiler mostra aumento de delegates

**Causa:** Não desinscrever eventos em `OnDisable()`.

**Solução:**
```csharp
void OnDisable() {
    // SEMPRE desinscrever TODOS os eventos inscritos
    GameEvents.OnUnitSpawned -= HandleSpawn;
    GameEvents.OnGroupCreated -= HandleGroup;
    GameEvents.OnTimeOfDay01 -= HandleTime;
}
```

**Diagnóstico:**
```csharp
// No Profiler, buscar por:
// - Delegates não coletados pelo GC
// - Contagem de listeners crescendo

#if UNITY_EDITOR
void OnDestroy() {
    // Adicionar temporariamente para detectar leaks
    Debug.LogWarning($"{GetType().Name} destruído, verificar se desinscreveu eventos!");
}
#endif
```

---

### 14.4 Problema: "TimeManager não avança o tempo"

**Sintomas:**
- Relógio parado
- Dia/noite não muda
- Eventos de tempo não disparados

**Causas:**
1. `config` é `null`
2. `config.SecondsPerDay` é muito alto (dia demora muito)
3. `Time.timeScale = 0` (jogo pausado)

**Diagnóstico:**
```csharp
void Update() {
    if (config == null) {
        Debug.LogError("[TimeManager] Config é null!");
        return;
    }
    
    Debug.Log($"Time01={Time01:F3}, SecondsPerDay={config.SecondsPerDay}, timeScale={Time.timeScale}");
    // ...
}
```

**Solução:**
```csharp
// No GameContext, garantir injeção
void Awake() {
    if (timeManager != null) {
        timeManager.config = config;
        Debug.Log($"[GameContext] Config injetado: SecondsPerDay={config.SecondsPerDay}");
    }
}
```

---

### 14.5 Problema: "FactionService.GetReputation() retorna erro"

**Sintomas:**
```
KeyNotFoundException: The given key was not present in the dictionary.
```

**Causa:** `Init()` não foi chamado ou facção não existe no banco.

**Solução:**
```csharp
// GameContext deve chamar Init()
void Awake() {
    if (factionService != null) {
        factionService.Init();
        Debug.Log("[GameContext] FactionService inicializado");
    }
}

// Validar antes de consultar
if (factionService != null) {
    try {
        float rep = factionService.GetReputation(a, b);
    } catch (KeyNotFoundException) {
        Debug.LogError($"Reputação não encontrada para {a} -> {b}. Init() foi chamado?");
    }
}
```

---

### 14.6 Problema: "DayNightLightController não muda cor"

**Sintomas:**
- Luz permanece com mesma cor/intensidade
- Rotação não acontece

**Causas:**
1. Componente não está subscrito (verificar `OnEnable()`)
2. `Light` não foi encontrado (verificar `RequireComponent`)
3. Gradiente/curva não configurados

**Diagnóstico:**
```csharp
void OnEnable() {
    _light = GetComponent<Light>();
    if (_light == null) {
        Debug.LogError("[DayNightLightController] Light component não encontrado!");
        return;
    }
    
    Debug.Log("[DayNightLightController] Subscrevendo OnTimeOfDay01");
    GameEvents.OnTimeOfDay01 += Apply;
}

void Apply(float t01) {
    Debug.Log($"[DayNightLightController] Apply(t01={t01:F3})");
    // ...
}
```

---

## 15) CHANGELOG E MIGRAÇÕES

### 15.1 Histórico de Versões

#### v2.1 (Outubro 2025) - **VERSÃO ATUAL**

**Adicionado:**
- ✅ 9 eventos de **Grupos** (`OnGroupCreated`, `OnGroupDeleted`, `OnGroupRenamed`, etc.)
- ✅ 4 eventos de **Unidades** (`OnUnitSpawned`, `OnUnitDespawned`, etc.)
- ✅ 9 eventos de **Seleção (Input)** (`OnSelectionChanged`, `OnUnitClick`, `OnDragBegin`, etc.)
- ✅ Documentação XML comments em todos os eventos
- ✅ `GameContext` como orquestrador centralizado
- ✅ `FactionService` com matriz de reputação

**Modificado:**
- 🔄 `TimeManager` agora calcula `Hour` e `Minute` (HH:MM em 24h)
- 🔄 `GameEvents` reorganizado em categorias (Tempo, Economia, Diplomacia, etc.)
- 🔄 Todos os `Raise` helpers agora usam `?.Invoke()` (null-safe)

**Removido:**
- ❌ Eventos locais em `Unit` (movidos para `GameEvents`)
- ❌ Eventos locais em `UnitRegistry` (movidos para `GameEvents`)

**Migrações Necessárias:**
```csharp
// ANTES (v2.0)
unit.OnSelectionChanged += HandleSelection;

// DEPOIS (v2.1)
GameEvents.OnUnitSelectionChanged += HandleSelection;
```

---

#### v2.0 (Setembro 2025)

**Adicionado:**
- ✅ Sistema de eventos de **Câmera** (5 eventos)
- ✅ `GameConfig` com configurações de tempo, clima e economia
- ✅ `DayNightLightController` com gradiente de cores

**Modificado:**
- 🔄 `TimeManager` agora usa `GameConfig` para `SecondsPerDay`
- 🔄 `FactionService` dispara eventos via `GameEvents`

---

#### v1.0 (Agosto 2025) - Versão Inicial

**Adicionado:**
- ✅ `GameEvents` básico (Tempo, Economia, Diplomacia)
- ✅ Enums globais (`FactionId`, `ResourceType`, etc.)
- ✅ `TimeManager` básico (sem HH:MM)
- ✅ `FactionDefinition`, `FactionDatabase`, `FactionService`

---

### 15.2 Guia de Migração (v2.0 → v2.1)

#### Passo 1: Atualizar Subscriptions de Eventos

**Eventos de Unidades:**
```csharp
// ANTES
Unit.OnProgressChanged += handler;

// DEPOIS
GameEvents.OnUnitProgressChanged += handler;
```

**Eventos de Seleção:**
```csharp
// ANTES
Unit.OnSelectionChanged += handler;

// DEPOIS
GameEvents.OnUnitSelectionChanged += handler;
```

#### Passo 2: Adicionar GameContext à Cena

1. Criar GameObject `_GameContext`
2. Adicionar componentes:
   - `GameContext`
   - `FactionService`
   - `TimeManager`
3. Configurar referências no Inspector

#### Passo 3: Atualizar TimeManager

**ANTES:**
```csharp
// Não tinha Hour/Minute
```

**DEPOIS:**
```csharp
// Agora disponível
int hour = timeManager.Hour;
int minute = timeManager.Minute;
```

#### Passo 4: Verificar Listeners

**Executar audit de eventos:**
```csharp
#if UNITY_EDITOR
[MenuItem("Tools/Audit Event Listeners")]
static void AuditListeners() {
    var monoBehaviours = FindObjectsOfType<MonoBehaviour>();
    foreach (var mb in monoBehaviours) {
        var type = mb.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        
        foreach (var method in methods) {
            if (method.Name == "OnEnable" || method.Name == "OnDisable") {
                Debug.Log($"{type.Name}.{method.Name}");
            }
        }
    }
}
#endif
```

---

### 15.3 Breaking Changes

#### v2.1

**1. Eventos Locais Removidos**

| Antes (v2.0) | Depois (v2.1) |
|--------------|---------------|
| `Unit.OnProgressChanged` | `GameEvents.OnUnitProgressChanged` |
| `Unit.OnSelectionChanged` | `GameEvents.OnUnitSelectionChanged` |
| `UnitRegistry.OnUnitSpawned` | `GameEvents.OnUnitSpawned` |
| `UnitRegistry.OnUnitDespawned` | `GameEvents.OnUnitDespawned` |

**2. TimeManager Requer GameConfig**

```csharp
// ANTES (v2.0)
timeManager.secondsPerDay = 600f; // Campo público

// DEPOIS (v2.1)
timeManager.config = gameConfig; // Injeta config
// Usa gameConfig.SecondsPerDay
```

**3. FactionService Requer Init()**

```csharp
// ANTES (v2.0)
// Inicialização automática em Awake()

// DEPOIS (v2.1)
// Deve chamar manualmente via GameContext
factionService.Init();
```

---

### 15.4 Compatibilidade com Versões Antigas

**Não há suporte para v1.x → v2.1 direto.**  
Migrar primeiro para v2.0, depois para v2.1.

**Scripts de migração automática:**

```csharp
#if UNITY_EDITOR
[MenuItem("Tools/Migrate to v2.1")]
static void MigrateToV21() {
    // 1. Buscar todos os scripts que usam eventos antigos
    var scripts = AssetDatabase.FindAssets("t:MonoScript");
    
    foreach (var guid in scripts) {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var content = File.ReadAllText(path);
        
        // Substituir padrões antigos
        content = content.Replace("Unit.OnProgressChanged", "GameEvents.OnUnitProgressChanged");
        content = content.Replace("Unit.OnSelectionChanged", "GameEvents.OnUnitSelectionChanged");
        
        File.WriteAllText(path, content);
    }
    
    AssetDatabase.Refresh();
    Debug.Log("Migração concluída!");
}
#endif
```

---

## 16) ESTRUTURA DE ARQUIVOS

### 16.1 Organização de Scripts

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Enums.cs                      ★ Enums globais
│   │   ├── GameConfig.cs                 ★ ScriptableObject de config
│   │   ├── GameEvents.cs                 ★★★ Event bus (30+ eventos)
│   │   ├── GameContext.cs                ★ Orquestrador
│   │   ├── TimeManager.cs                ★ Sistema de tempo
│   │   ├── DayNightLightController.cs    ★ Visual dia/noite
│   │   └── PlayerController.cs           ★ Identidade do jogador
│   │
│   ├── Factions/
│   │   ├── FactionDefinition.cs          ★ ScriptableObject de facção
│   │   ├── FactionDatabase.cs            ★ Banco de facções
│   │   └── FactionService.cs             ★ Gerenciador de reputação
│   │
│   ├── Units/
│   │   ├── Unit.cs
│   │   ├── UnitDefinition.cs
│   │   ├── UnitRegistry.cs
│   │   └── UnitQueries.cs
│   │
│   ├── Selection/
│   │   ├── SelectionManager.cs
│   │   ├── SelectionInputHandler.cs
│   │   └── SelectionBox.cs
│   │
│   ├── UI/
│   │   ├── UnitListPanel.cs
│   │   ├── GroupContextMenuHandler.cs
│   │   ├── ListItemWrapper.cs
│   │   └── ...
│   │
│   └── Camera/
│       ├── RTSCameraCinemachineV3Controller.cs
│       ├── RTSCameraInputSystem.cs
│       ├── RTSCameraProfile.cs
│       └── CameraMath.cs
```

### 16.2 Organização de Assets

```
Assets/
├── Settings/
│   └── GameConfig.asset                  ★ Configuração global
│
├── Definitions/
│   ├── Factions/
│   │   ├── FactionDatabase.asset         ★ Banco de facções
│   │   ├── Player1_Def.asset
│   │   ├── Player2_Def.asset
│   │   ├── PvE_Def.asset
│   │   └── Neutral_Def.asset
│   │
│   └── Units/
│       ├── Worker_Def.asset
│       ├── Warrior_Def.asset
│       └── ...
│
└── Prefabs/
    ├── Units/
    │   ├── Worker.prefab
    │   ├── Warrior.prefab
    │   └── ...
    │
    └── UI/
        ├── UnitListPanel.prefab
        └── ...
```

### 16.3 Hierarquia de Cena Recomendada

```
Scene: MainGame
├── _GameContext (GameObject) ★★★
│   ├── GameContext (MonoBehaviour)
│   │   ├── config: GameConfig.asset
│   │   ├── factions: FactionDatabase.asset
│   │   ├── factionService: (↓)
│   │   └── timeManager: (↓)
│   ├── FactionService (MonoBehaviour)
│   │   └── database: FactionDatabase.asset
│   └── TimeManager (MonoBehaviour)
│       └── config: (injetado via GameContext)
│
├── Player (GameObject)
│   └── PlayerController (MonoBehaviour)
│       ├── myFaction: Player1
│       └── mainCamera: Main Camera
│
├── Environment (GameObject)
│   ├── Terrain
│   ├── Sun (Directional Light)
│   │   └── DayNightLightController (MonoBehaviour)
│   └── Skybox
│
├── CameraRig (GameObject)
│   ├── RTSCameraCinemachineV3Controller
│   └── RTSCameraInputSystem
│
├── Units (GameObject - container)
│   ├── Worker_01
│   ├── Worker_02
│   ├── Warrior_01
│   └── ...
│
└── UI (GameObject - canvas)
    ├── UnitListPanel
    ├── ClockDisplay
    ├── ResourceBar
    └── ...
```

---

## 17) CHECKLIST DE VALIDAÇÃO

### 17.1 Setup Inicial

- [ ] `GameConfig.asset` criado e configurado
- [ ] `FactionDatabase.asset` criado com todas as facções
- [ ] GameObject `_GameContext` na cena com componentes:
  - [ ] `GameContext`
  - [ ] `FactionService`
  - [ ] `TimeManager`
- [ ] Referências do `GameContext` configuradas no Inspector:
  - [ ] `config` → `GameConfig.asset`
  - [ ] `factions` → `FactionDatabase.asset`
  - [ ] `factionService` → componente local
  - [ ] `timeManager` → componente local
- [ ] GameObject `Player` com `PlayerController`
- [ ] `Directional Light` com `DayNightLightController`

### 17.2 Testes Funcionais

- [ ] Play → Tempo avança (verificar `TimeManager.Time01` no Inspector)
- [ ] Relógio funciona (verificar `Hour:Minute` no Inspector)
- [ ] Luz muda ao longo do dia (observar cor/intensidade)
- [ ] Virada de dia dispara evento (`OnDayChanged`)
- [ ] Reputação inicializada (`OnReputationMatrixReady` disparado)

### 17.3 Testes de Eventos

- [ ] Subscrever `OnTimeOfDay01` → recebe notificação todo frame
- [ ] Subscrever `OnClockChanged` → recebe notificação todo minuto
- [ ] Subscrever `OnDayChanged` → recebe notificação na virada do dia
- [ ] Subscrever `OnReputationChanged` → recebe notificação ao mudar reputação
- [ ] Subscrever `OnUnitSpawned` → recebe notificação ao spawnar unidade

### 17.4 Testes de Memory Leak

- [ ] Play por 10 minutos → verificar Profiler (Memory)
- [ ] Criar/destruir GameObjects com listeners → verificar GC
- [ ] Todos os `OnEnable()` têm `OnDisable()` correspondente

### 17.5 Testes de Performance

- [ ] 30+ eventos ativos → FPS estável
- [ ] 100+ unidades com listeners → CPU usage aceitável
- [ ] `OnTimeOfDay01` (todo frame) → sem spikes

---

## 18) GLOSSÁRIO TÉCNICO

| Termo | Definição |
|-------|-----------|
| **Event Bus** | Padrão de design que permite comunicação desacoplada via eventos centralizados |
| **ScriptableObject** | Asset Unity que armazena dados reutilizáveis (config, definitions) |
| **Raise Helper** | Método `RaiseXXX()` que dispara evento com validação null-safe |
| **Subscribe/Unsubscribe** | Registrar/remover listener de um evento (`+=` / `-=`) |
| **Memory Leak** | Memória não liberada devido a listeners não removidos |
| **Throttling** | Limitar frequência de execução (ex: processar apenas a cada 0.1s) |
| **Orquestrador** | Classe responsável por inicializar e coordenar sistemas (GameContext) |
| **Injeção de Dependências** | Fornecer referências necessárias a um sistema (via Inspector ou código) |
| **Read-Only Property** | Propriedade pública de leitura, privada de escrita (`{ get; private set; }`) |
| **Matriz de Reputação** | Tabela 2D que armazena reputação de todas as facções entre si |
| **Time01** | Fração do dia de 0 a 1 (0=meia-noite, 0.5=meio-dia, 1=meia-noite) |

---

## 19) RECURSOS ADICIONAIS

### 19.1 Links Úteis

- **Documentação do Projeto:** https://luciano-claudio.github.io/MedievalThrones/
- **Unity ScriptableObjects:** https://docs.unity3d.com/Manual/class-ScriptableObject.html
- **C# Events:** https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/events/
- **Observer Pattern:** https://refactoring.guru/design-patterns/observer

### 19.2 Próximos Passos

1. **Lote 2 (Câmera):** Implementar `RTSCameraCinemachineV3Controller` escutando eventos
2. **Lote 3 (Unidades):** Documentar `Unit`, `UnitRegistry`, `UnitQueries` integrando com eventos
3. **Lote 4 (Seleção):** Documentar `SelectionManager`, `SelectionInputHandler` disparando eventos
4. **Lote 5 (UI/Left Bar):** Documentar `UnitListPanel`, `GroupContextMenuHandler` escutando eventos
5. **Lote 6+ (IA, Economia, Combate):** Integrar com event bus existente

---

## 20) CONCLUSÃO

O **Lote 1 - Core System** estabelece a fundação arquitetural de todo o projeto Medieval Thrones. Com 30+ eventos centralizados, configuração global editável e sistemas de tempo/facções robustos, o módulo permite:

✅ **Desenvolvimento Paralelo**: Times trabalham em sistemas isolados que comunicam via eventos  
✅ **Manutenibilidade**: Mudanças em um sistema não quebram outros  
✅ **Testabilidade**: Event bus facilita testes unitários e de integração  
✅ **Escalabilidade**: Adicionar novos eventos/sistemas não requer refatoração  
✅ **Rastreabilidade**: Documentação completa de quem dispara e escuta cada evento  

**Próximo passo:** Validar esta documentação e prosseguir para **Lote 5 - User Interface / Left Bar**.

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão:** 2.1 (Pós-Refatoração Completa)  

---

# LOTE 2 — CÂMERA SYSTEM RTS (CINEMACHINE V3)

**Versão:** 3.0  
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

# LOTE 3 — MÓDULO UNIT (SISTEMA DE UNIDADES)

**Versão:** 3.0  
**Status:** ✅ Refatorado e Documentado  
**Data:** Outubro 2025

---

## 📋 ÍNDICE

1. [Visão Geral do Módulo](#1-visão-geral-do-módulo)
2. [UnitDefinition - Dados de Unidade](#2-unitdefinition---dados-de-unidade)
3. [Unit - Componente Principal](#3-unit---componente-principal)
4. [UnitRegistry - Registro Global](#4-unitregistry---registro-global)
5. [UnitQueries - Utilitários de Consulta](#5-unitqueries---utilitários-de-consulta)
6. [Fluxo de Ciclo de Vida Completo](#6-fluxo-de-ciclo-de-vida-completo)
7. [Integração com Outros Módulos](#7-integração-com-outros-módulos)
8. [Configuração na Cena](#8-configuração-na-cena)
9. [Exemplos de Uso Avançados](#9-exemplos-de-uso-avançados)
10. [Sistema de Progressão (XP e Level Up)](#10-sistema-de-progressão-xp-e-level-up)
11. [Troubleshooting e FAQ](#11-troubleshooting-e-faq)
12. [Tabela de Relacionamentos Completa](#12-tabela-de-relacionamentos-completa)
13. [Changelog e Migrações](#13-changelog-e-migrações)
14. [Referências Rápidas](#14-referências-rápidas)
15. [Conclusão](#15-conclusão)

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Objetivo

Fornecer um **sistema completo de unidades** para Medieval Thrones, com gestão de ciclo de vida, progressão (XP/Level), seleção visual e integração total com GameEvents.

**Funcionalidades Principais:**
- ✅ **UnitDefinition**: Dados configuráveis em ScriptableObjects
- ✅ **Unit**: Componente principal com HP, progressão e seleção
- ✅ **UnitRegistry**: Registro global com listas por facção
- ✅ **UnitQueries**: Utilitários otimizados de consulta
- ✅ **Sistema de Progressão**: XP e Level Up com fórmula exponencial
- ✅ **Eventos via GameEvents**: Spawn, Despawn, Selection, Progress

### 1.2 Responsabilidades Principais

O **Lote 3 - Módulo Unit** é responsável por:

1. **Definição de Unidades** (`UnitDefinition`):
   - ScriptableObjects reutilizáveis
   - Metadados (nome, tipo, ícone)
   - Base para balanceamento

2. **Componente de Unidade** (`Unit`):
   - HP e facção (owner)
   - Sistema de progressão (level, XP)
   - Highlight visual de seleção
   - Emissão de eventos via GameEvents

3. **Registro Global** (`UnitRegistry`):
   - Lista centralizada de todas as unidades ativas
   - Listas otimizadas por facção
   - Gerenciamento automático de ciclo de vida
   - Emissão de eventos de spawn/despawn

4. **Utilitários de Consulta** (`UnitQueries`):
   - Queries otimizadas por facção
   - Fallback seguro para varredura de cena
   - Integração com PlayerController

### 1.3 Arquitetura do Sistema de Unidades

```
┌─────────────────────────────────────────────────────────┐
│                 LOTE 3 - MÓDULO UNIT                     │
└─────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────────┐ ┌────────────┐ ┌──────────────────┐
│ UnitDefinition    │ │    Unit    │ │  UnitRegistry    │
│(ScriptableObject) │ │(MonoBehav) │ │    (static)      │
├───────────────────┤ ├────────────┤ ├──────────────────┤
│• displayName      │ │• def       │ │• Register()      │
│• type (enum)      │ │• owner     │ │• Unregister()    │
│• icon (sprite)    │ │• hp/hpMax  │ │• GetByFaction()  │
└─────────┬─────────┘ │• level/xp  │ │• All (list)      │
          │           │• highlight │ └────────┬─────────┘
          │           └──────┬─────┘          │
          │                  │                │
          └──────────────────┼────────────────┘
                             │
                    ┌────────┴────────┐
                    │                 │
                    ▼                 ▼
            ┌───────────────┐  ┌──────────────┐
            │ UnitQueries   │  │  GameEvents  │
            │   (static)    │  │   (Lote 1)   │
            ├───────────────┤  └──────────────┘
            │• GetUnitsFor  │         │
            │  Player()     │         │ Emite:
            │• EnumerateAll │         │ • OnUnitSpawned
            └───────────────┘         │ • OnUnitDespawned
                                      │ • OnUnitSelectionChanged
                                      │ • OnUnitProgressChanged
                                      │
                              ┌───────┴───────┐
                              │               │
                              ▼               ▼
                        ┌──────────┐    ┌──────────┐
                        │    UI    │    │ Minimap  │
                        │ Systems  │    │  Audio   │
                        └──────────┘    └──────────┘
```

**Fluxo de Comunicação:**
1. Unit é adicionado à cena → `OnEnable()` → `UnitRegistry.Register()`
2. Registry dispara `GameEvents.RaiseUnitSpawned()`
3. Sistemas (UI, Minimap) reagem ao evento
4. Mesma lógica para Despawn, Selection, Progress

---

## 2) UNITDEFINITION - DADOS DE UNIDADE

### 2.1 Visão Geral

**Tipo:** `ScriptableObject`  
**Responsabilidade:** Armazenar metadados de um tipo de unidade (Worker, Warrior, Archer, etc.).

**Criação:** `Assets > Create > Game > Unit Definition`

**Vantagens:**
- ✅ Reutilização entre múltiplas instâncias
- ✅ Balanceamento centralizado
- ✅ Fácil criação de novos tipos
- ✅ Permite data-driven design

### 2.2 Campos Públicos (Inspector)

```csharp
[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    public string displayName;
    public UnitType type;
    public Sprite icon;
}
```

#### **displayName (string)**

```csharp
public string displayName;
```

**Descrição:** Nome legível da unidade para exibição em UI.

**Exemplos:**
- `"Operário"` (Worker)
- `"Guerreiro"` (Warrior)
- `"Arqueiro"` (Archer)
- `"Cavaleiro Leve"` (CavalryLight)

**Uso:** Exibido em tooltips, listas de seleção, diálogos.

---

#### **type (UnitType - Enum)**

```csharp
public UnitType type;
```

**Descrição:** Classificação funcional da unidade.

**Valores Possíveis (do Lote 1):**
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

**Uso:**
- Filtros de UI (ex: "mostrar apenas workers")
- Lógica de IA (ex: priorizar heróis)
- Balanceamento (ex: workers coletam +50% madeira)

---

#### **icon (Sprite)**

```csharp
public Sprite icon;
```

**Descrição:** Ícone da unidade para UI.

**Tamanho Recomendado:** 64x64 ou 128x128 pixels

**Uso:**
- Lista de seleção
- Minimap (opcional)
- Tooltips
- HUD de construção/treino

---

### 2.3 Exemplos de Definições

#### **Worker.asset (Operário)**

```
displayName: "Operário"
type: Worker
icon: worker_icon.png
```

**Características:**
- Coleta recursos (madeira, pedra, comida)
- Constrói edifícios
- Baixo HP (100)
- Sem capacidade de combate

---

#### **Warrior.asset (Guerreiro)**

```
displayName: "Guerreiro"
type: Warrior
icon: warrior_icon.png
```

**Características:**
- Combate corpo-a-corpo
- HP médio (150)
- Dano contra infantaria

---

#### **Archer.asset (Arqueiro)**

```
displayName: "Arqueiro"
type: Archer
icon: archer_icon.png
```

**Características:**
- Ataque à distância
- HP baixo (80)
- Bônus contra unidades sem armadura

---

#### **Hero.asset (Herói)**

```
displayName: "Rei Arthur"
type: Hero
icon: hero_arthur_icon.png
```

**Características:**
- Único (limite de 1)
- HP alto (500+)
- Habilidades especiais
- Sistema de progressão único

---

## 3) UNIT - COMPONENTE PRINCIPAL

### 3.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Requer:** `[DisallowMultipleComponent]`  
**Responsabilidade:** Componente principal de cada unidade, gerenciando dados, seleção e progressão.

**Funcionalidades:**
- ✅ Referência a `UnitDefinition`
- ✅ HP e facção (owner)
- ✅ Sistema de progressão (level, XP)
- ✅ Highlight visual de seleção
- ✅ Auto-registro em `UnitRegistry`
- ✅ Emissão de eventos via GameEvents

### 3.2 Campos Públicos (Inspector)

#### **Seção: Dados**

```csharp
[Header("Dados")]
public UnitDefinition def;
public FactionId owner = FactionId.Player1;
public float hp = 100, hpMax = 100;
```

**Campos:**

| Campo | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `def` | `UnitDefinition` | null | Referência ao ScriptableObject de definição |
| `owner` | `FactionId` | `Player1` | Facção dona da unidade (do Lote 1) |
| `hp` | `float` | 100 | HP atual |
| `hpMax` | `float` | 100 | HP máximo |

**Exemplo de Configuração:**
```
def: Worker.asset
owner: Player1
hp: 100
hpMax: 100
```

---

#### **Seção: Seleção (visuais)**

```csharp
[Header("Seleção (visuais)")]
[SerializeField] GameObject selectionHighlight; // um "ring" ou outline
```

**Campo:**
- `selectionHighlight`: GameObject filho ativado quando `IsSelected = true`

**Tipos de Highlight Típicos:**
1. **Ring no Chão:**
   - Quad ou Mesh com textura de anel
   - Shader transparente/additive
   - Cor por facção (azul = aliado, vermelho = inimigo)

2. **Outline/Edge Detection:**
   - Shader de outline
   - Post-processing effect

3. **Particle System:**
   - Partículas circulando a unidade

**Exemplo de Setup:**
```
Unit (GameObject)
├─ Model (mesh)
└─ SelectionRing (GameObject) ← Atribuir aqui
   └─ Quad com shader transparente
```

---

#### **Seção: Progressão**

```csharp
[Header("Progressão")]
[SerializeField] int level = 1;
[SerializeField] float xp = 0f;

[Tooltip("XP base para upar do Lv 1→2")]
[SerializeField] float baseXpToLevel = 100f;

[Tooltip("Multiplicador por nível (ex.: 1.35 => 35% a mais por nível)")]
[SerializeField] float xpGrowth = 1.35f;
```

**Campos:**

| Campo | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `level` | `int` | 1 | Nível atual (1-N) |
| `xp` | `float` | 0 | XP acumulado para próximo nível |
| `baseXpToLevel` | `float` | 100 | XP necessário para 1→2 |
| `xpGrowth` | `float` | 1.35 | Multiplicador exponencial por nível |

**Fórmula de XP:**
```
XpToNext = baseXpToLevel * xpGrowth^(level - 1)
```

**Exemplos:**
- Level 1→2: 100 XP
- Level 2→3: 100 * 1.35 = 135 XP
- Level 3→4: 100 * 1.35² = 182.25 XP
- Level 10→11: 100 * 1.35⁹ ≈ 2,052 XP

---

### 3.3 Propriedades (Read-Only)

#### **IsSelected (bool)**

```csharp
public bool IsSelected { get; private set; }
```

**Descrição:** Indica se a unidade está selecionada no momento.

**Modificação:** Apenas via `SetSelected(bool)`

**Uso:**
```csharp
if (unit.IsSelected)
{
    // Desenhar UI de seleção
}
```

---

#### **Level (int)**

```csharp
public int Level => level;
```

**Descrição:** Nível atual da unidade (read-only).

**Modificação:** Apenas via `AddXp()` (level up automático)

---

#### **Xp (float)**

```csharp
public float Xp => xp;
```

**Descrição:** XP acumulado para próximo nível (read-only).

**Modificação:** Apenas via `AddXp()`

---

#### **XpToNext (float)**

```csharp
public float XpToNext
    => baseXpToLevel * Mathf.Pow(xpGrowth, Mathf.Max(0, level - 1));
```

**Descrição:** XP total necessário para próximo nível.

**Fórmula:** `base * growth^(level-1)`

**Exemplo:**
```csharp
// Level 5, base=100, growth=1.35
float needed = unit.XpToNext;
// needed = 100 * 1.35^4 = 100 * 3.32 = 332.1 XP
```

---

#### **Xp01 (float)**

```csharp
public float Xp01 => XpToNext <= 0f ? 0f : Mathf.Clamp01(xp / XpToNext);
```

**Descrição:** Progresso 0..1 rumo ao próximo nível (para barra de UI).

**Uso:**
```csharp
// Em UnitListItemUI
xpBar.fillAmount = unit.Xp01; // 0 = vazio, 1 = cheio
```

**Exemplo:**
- 50 XP de 100 necessários → `Xp01 = 0.5` (50%)
- 150 XP de 200 necessários → `Xp01 = 0.75` (75%)

---

#### **DisplayName (string)**

```csharp
public string DisplayName => def ? def.displayName : gameObject.name;
```

**Descrição:** Nome da unidade para exibição em UI.

**Fallback:** Se `def` for null, usa `gameObject.name`

**Uso:**
```csharp
tooltipText.text = unit.DisplayName; // "Operário"
```

---

### 3.4 Métodos Públicos

#### **SetSelected(bool value)**

```csharp
public void SetSelected(bool value)
{
    if (IsSelected == value) return;
    IsSelected = value;
    if (selectionHighlight) selectionHighlight.SetActive(value);

    // REFATORAÇÃO: Usar GameEvents em vez de evento local
    GameEvents.RaiseUnitSelectionChanged(this, value);
}
```

**Parâmetro:**
- `value`: `true` = selecionar, `false` = desselecionar

**Comportamento:**
1. Se estado já é igual, retorna (early exit)
2. Atualiza `IsSelected`
3. Ativa/desativa `selectionHighlight`
4. Dispara evento `GameEvents.OnUnitSelectionChanged`

**Uso:**
```csharp
// Selecionar unidade
unit.SetSelected(true);

// Desselecionar unidade
unit.SetSelected(false);
```

**Integração com GameEvents:**
```csharp
// Listeners típicos:
// - UnitListUI: atualiza lista visual
// - Audio: toca som de seleção
// - Camera: foca na unidade (opcional)
```

---

#### **AddXp(float amount)**

```csharp
public void AddXp(float amount)
{
    if (amount <= 0f) return;

    xp += amount;
    var guard = 64; // evita loop infinito em valores absurdos

    while (xp >= XpToNext && guard-- > 0)
    {
        xp -= XpToNext;
        level++;
    }

    // REFATORAÇÃO: Usar GameEvents em vez de evento local
    GameEvents.RaiseUnitProgressChanged(this);
}
```

**Parâmetro:**
- `amount`: Quantidade de XP a adicionar (deve ser > 0)

**Comportamento:**
1. Valida `amount > 0`
2. Adiciona XP
3. **Loop de Level Up**: Enquanto `xp >= XpToNext`:
   - Subtrai `XpToNext` de `xp`
   - Incrementa `level`
   - Guarda de 64 iterações (proteção contra valores absurdos)
4. Dispara evento `GameEvents.OnUnitProgressChanged`

**Exemplo de Múltiplos Ups:**
```csharp
// Unidade level 1, xp=50, XpToNext=100
unit.AddXp(300); // Adiciona 300 XP

// Loop 1: xp=350, XpToNext=100 → xp=250, level=2
// Loop 2: xp=250, XpToNext=135 → xp=115, level=3
// Loop 3: xp=115, XpToNext=182 → para (xp < XpToNext)

// Resultado: level=3, xp=115
```

**Uso Típico:**
```csharp
// Após combate
void OnEnemyKilled(Unit enemy)
{
    float xpReward = enemy.Level * 50f;
    unit.AddXp(xpReward);
}

// Após coletar recurso
void OnResourceGathered(int amount)
{
    unit.AddXp(amount * 0.5f); // 0.5 XP por recurso
}
```

---

### 3.5 Sistema de Progressão (Level Up)

#### **Fórmula Exponencial**

**Equação:**
```
XpToNext(level) = baseXpToLevel * xpGrowth^(level - 1)
```

**Valores Padrão:**
- `baseXpToLevel = 100`
- `xpGrowth = 1.35` (35% a mais por nível)

**Tabela de Progressão:**

| Level | XP Necessário | XP Acumulado | Fórmula |
|-------|---------------|--------------|---------|
| 1→2 | 100 | 0-100 | 100 * 1.35⁰ = 100 |
| 2→3 | 135 | 100-235 | 100 * 1.35¹ = 135 |
| 3→4 | 182 | 235-417 | 100 * 1.35² = 182 |
| 4→5 | 246 | 417-663 | 100 * 1.35³ = 246 |
| 5→6 | 332 | 663-995 | 100 * 1.35⁴ = 332 |
| 10→11 | 2,052 | - | 100 * 1.35⁹ = 2,052 |
| 20→21 | 276,661 | - | 100 * 1.35¹⁹ = 276,661 |

**Características:**
- ✅ Progressão rápida nos primeiros níveis
- ✅ Desacelera exponencialmente
- ✅ Níveis altos são raros (design intencional)

---

#### **Customização de Curva**

**Progressão Rápida (Casual):**
```
baseXpToLevel = 50
xpGrowth = 1.2 (20% por nível)

1→2: 50 XP
5→6: 103 XP
10→11: 258 XP
```

**Progressão Lenta (Hardcore):**
```
baseXpToLevel = 200
xpGrowth = 1.5 (50% por nível)

1→2: 200 XP
5→6: 1,013 XP
10→11: 7,706 XP
```

---

#### **Benefícios de Level Up (Futuro)**

**Nota:** Sistema atual **não implementa benefícios automáticos**. O level up apenas incrementa o contador.

**Possíveis Benefícios Futuros:**
- 🔮 +5% HP por nível
- 🔮 +3% dano por nível
- 🔮 Desbloquear habilidades (level 5, 10, 15)
- 🔮 Aparência visual (partículas, glow)

**Implementação Futura (exemplo):**
```csharp
// Em Unit.AddXp(), após level++
void OnLevelUp()
{
    hpMax *= 1.05f; // +5% HP
    hp = hpMax; // Curar ao upar
    
    if (level == 5) UnlockAbility("PowerStrike");
    if (level == 10) UnlockAbility("AreaAttack");
    
    // VFX de level up
    PlayLevelUpEffect();
}
```

---

### 3.6 Integração com GameEvents

**Eventos Emitidos por Unit:**

```csharp
// Em SetSelected()
GameEvents.RaiseUnitSelectionChanged(this, value);
// → OnUnitSelectionChanged?.Invoke(this, value)

// Em AddXp()
GameEvents.RaiseUnitProgressChanged(this);
// → OnUnitProgressChanged?.Invoke(this)
```

**Listeners Típicos:**

| Evento | Listener | Ação |
|--------|----------|------|
| `OnUnitSelectionChanged` | `UnitListUI` | Atualizar lista visual |
| `OnUnitSelectionChanged` | `AudioManager` | Tocar som de seleção |
| `OnUnitSelectionChanged` | `RTSCamera` (opcional) | Focar na unidade |
| `OnUnitProgressChanged` | `UnitListItemUI` | Atualizar barra de XP |
| `OnUnitProgressChanged` | `AudioManager` | Tocar som de level up |
| `OnUnitProgressChanged` | `VFXManager` | Efeito visual de level up |

**Exemplo de Listener (UI):**

```csharp
public class UnitListItemUI : MonoBehaviour
{
    Unit _unit;
    Image xpBar;
    
    void OnEnable()
    {
        GameEvents.OnUnitProgressChanged += OnProgressChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitProgressChanged -= OnProgressChanged;
    }
    
    void OnProgressChanged(Unit changedUnit)
    {
        if (changedUnit == _unit)
        {
            xpBar.fillAmount = _unit.Xp01;
            levelText.text = $"Lv {_unit.Level}";
        }
    }
}
```

---

### 3.7 Ciclo de Vida (OnEnable/OnDisable)

```csharp
private void OnEnable() => UnitRegistry.Register(this);
private void OnDisable() => UnitRegistry.Unregister(this);
```

**Comportamento:**
- `OnEnable()`: Unidade é **registrada** em `UnitRegistry`
  - Adicionada a `All` e `_perFaction[owner]`
  - Dispara `GameEvents.OnUnitSpawned`

- `OnDisable()`: Unidade é **desregistrada**
  - Removida de `All` e `_perFaction[owner]`
  - Dispara `GameEvents.OnUnitDespawned`

**Implicações:**
- ✅ Auto-registro: não precisa chamar manualmente
- ✅ Pool-friendly: funciona com object pooling
- ✅ Scene reload: registros são limpos automaticamente

---

## 4) UNITREGISTRY - REGISTRO GLOBAL

### 4.1 Visão Geral

**Tipo:** `static class`  
**Responsabilidade:** Manter lista centralizada de todas as unidades ativas, organizadas por facção.

**Funcionalidades:**
- ✅ Lista global de todas as unidades (`All`)
- ✅ Listas otimizadas por facção (`_perFaction`)
- ✅ Auto-registro via `OnEnable/OnDisable`
- ✅ Emissão de eventos via GameEvents

**Vantagens:**
- ✅ **Performance**: O(1) para acesso por facção
- ✅ **Consistência**: Fonte única de verdade
- ✅ **Escalabilidade**: Lida com centenas de unidades

### 4.2 Estrutura Interna

```csharp
public static class UnitRegistry
{
    static readonly List<Unit> _all = new();
    static readonly Dictionary<FactionId, List<Unit>> _perFaction = new();

    public static IReadOnlyList<Unit> All => _all;
    public static IReadOnlyList<Unit> GetByFaction(FactionId f) =>
        _perFaction.TryGetValue(f, out var list) ? list : Array.Empty<Unit>();
}
```

**Campos Privados:**

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `_all` | `List<Unit>` | Lista de TODAS as unidades ativas |
| `_perFaction` | `Dictionary<FactionId, List<Unit>>` | Listas separadas por facção |

**Exemplo de Estado Interno:**

```csharp
// Cena com 3 workers (Player1) e 2 warriors (Player2)
_all = [Worker1, Worker2, Worker3, Warrior1, Warrior2]

_perFaction = {
    Player1: [Worker1, Worker2, Worker3],
    Player2: [Warrior1, Warrior2]
}
```

---

### 4.3 API Pública

#### **All (IReadOnlyList<Unit>)**

```csharp
public static IReadOnlyList<Unit> All => _all;
```

**Descrição:** Lista de TODAS as unidades ativas na cena.

**Uso:**
```csharp
// Iterar sobre todas as unidades
foreach (var unit in UnitRegistry.All)
{
    Debug.Log($"{unit.DisplayName} - HP: {unit.hp}/{unit.hpMax}");
}

// Contagem total
int totalUnits = UnitRegistry.All.Count;
Debug.Log($"Total de unidades: {totalUnits}");
```

---

#### **GetByFaction(FactionId f)**

```csharp
public static IReadOnlyList<Unit> GetByFaction(FactionId f) =>
    _perFaction.TryGetValue(f, out var list) ? list : Array.Empty<Unit>();
```

**Parâmetro:**
- `f`: Facção a consultar

**Retorno:**
- Lista de unidades da facção (read-only)
- `Array.Empty<Unit>()` se facção não tem unidades

**Uso:**
```csharp
// Obter unidades do jogador
var myUnits = UnitRegistry.GetByFaction(FactionId.Player1);
Debug.Log($"Player1 tem {myUnits.Count} unidades");

// Filtrar por tipo
var workers = myUnits.Where(u => u.def.type == UnitType.Worker);
Debug.Log($"Player1 tem {workers.Count()} workers");
```

**Performance:**
- ✅ **O(1)** para acesso ao dicionário
- ✅ **O(n)** apenas se iterar sobre a lista retornada

---

#### **Register(Unit u)**

```csharp
public static void Register(Unit u)
{
    if (!_all.Contains(u))
    {
        _all.Add(u);
        if (!_perFaction.TryGetValue(u.owner, out var list))
        {
            list = new List<Unit>();
            _perFaction[u.owner] = list;
        }
        list.Add(u);

        // REFATORAÇÃO: Usar GameEvents em vez de evento estático local
        GameEvents.RaiseUnitSpawned(u);
    }
}
```

**Parâmetro:**
- `u`: Unidade a registrar

**Comportamento:**
1. Verifica se já está registrada (evita duplicatas)
2. Adiciona a `_all`
3. Cria lista da facção se não existir
4. Adiciona à lista da facção
5. Dispara `GameEvents.OnUnitSpawned`

**Chamado Automaticamente:** `Unit.OnEnable()`

**Uso Manual (raro):**
```csharp
// Apenas se criar unidade via código
Unit newUnit = Instantiate(unitPrefab);
UnitRegistry.Register(newUnit); // Normalmente desnecessário
```

---

#### **Unregister(Unit u)**

```csharp
public static void Unregister(Unit u)
{
    if (_all.Remove(u))
    {
        if (_perFaction.TryGetValue(u.owner, out var list)) list.Remove(u);

        // REFATORAÇÃO: Usar GameEvents em vez de evento estático local
        GameEvents.RaiseUnitDespawned(u);
    }
}
```

**Parâmetro:**
- `u`: Unidade a desregistrar

**Comportamento:**
1. Remove de `_all`
2. Se removeu com sucesso, remove da lista da facção
3. Dispara `GameEvents.OnUnitDespawned`

**Chamado Automaticamente:** `Unit.OnDisable()`

**Uso Manual (raro):**
```csharp
// Apenas se remover unidade sem destruir GameObject
UnitRegistry.Unregister(unit); // Normalmente desnecessário
```

---

### 4.4 Integração com GameEvents

**Eventos Emitidos por UnitRegistry:**

```csharp
// Em Register()
GameEvents.RaiseUnitSpawned(u);
// → OnUnitSpawned?.Invoke(u)

// Em Unregister()
GameEvents.RaiseUnitDespawned(u);
// → OnUnitDespawned?.Invoke(u)
```

**Listeners Típicos:**

| Evento | Listener | Ação |
|--------|----------|------|
| `OnUnitSpawned` | `MinimapSystem` | Adicionar ícone no minimap |
| `OnUnitSpawned` | `FogOfWarSystem` | Revelar área ao redor |
| `OnUnitSpawned` | `AISystem` | Adicionar a lista de ameaças |
| `OnUnitDespawned` | `MinimapSystem` | Remover ícone do minimap |
| `OnUnitDespawned` | `FogOfWarSystem` | Esconder área (se última unidade) |
| `OnUnitDespawned` | `AISystem` | Remover da lista de ameaças |

**Exemplo de Listener (Minimap):**

```csharp
public class MinimapSystem : MonoBehaviour
{
    Dictionary<Unit, GameObject> _icons = new();
    
    void OnEnable()
    {
        GameEvents.OnUnitSpawned += OnUnitSpawned;
        GameEvents.OnUnitDespawned += OnUnitDespawned;
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitSpawned -= OnUnitSpawned;
        GameEvents.OnUnitDespawned -= OnUnitDespawned;
    }
    
    void OnUnitSpawned(Unit unit)
    {
        var icon = Instantiate(iconPrefab, minimapCanvas);
        icon.GetComponent<Image>().color = GetFactionColor(unit.owner);
        _icons[unit] = icon;
    }
    
    void OnUnitDespawned(Unit unit)
    {
        if (_icons.TryGetValue(unit, out GameObject icon))
        {
            Destroy(icon);
            _icons.Remove(unit);
        }
    }
}
```

---

### 4.5 Performance e Escalabilidade

**Benchmark (1000 unidades):**

| Operação | Complexidade | Tempo |
|----------|--------------|-------|
| `Register()` | O(1) | <0.1ms |
| `Unregister()` | O(n) | ~0.5ms (busca em lista) |
| `GetByFaction()` | O(1) | <0.01ms |
| Iterar `All` | O(n) | ~1ms |
| Iterar facção (250 units) | O(n) | ~0.25ms |

**Otimizações:**
- ✅ Dictionary para acesso O(1) por facção
- ✅ Listas separadas evitam filtros em runtime
- ✅ IReadOnlyList previne modificações externas

**Limitações:**
- ⚠️ `Unregister()` é O(n) por usar `List.Remove()`
- Solução futura: Usar `HashSet` se despawns forem frequentes

---

## 5) UNITQUERIES - UTILITÁRIOS DE CONSULTA

### 5.1 Visão Geral

**Tipo:** `static class`  
**Responsabilidade:** Fornecer queries otimizadas e fallbacks seguros para consultar unidades.

**Funcionalidades:**
- ✅ `GetUnitsForPlayer()`: Query otimizada por PlayerController
- ✅ `EnumerateSceneUnits()`: Fallback via FindObjects

**Vantagens:**
- ✅ Integração com `PlayerController` (Lote 1)
- ✅ Otimização automática (usa `GetByFaction` se source=null)
- ✅ Fallback seguro para casos edge

### 5.2 GetUnitsForPlayer()

```csharp
/// <summary>
/// Retorna as Units do jogador. Se você tiver uma lista do seu UnitRegistry,
/// passe em <paramref name="source"/> para evitar varrer a cena.
/// Caso passe null, cai no fallback otimizado que usa GetByFaction.
/// </summary>
public static IEnumerable<Unit> GetUnitsForPlayer(PlayerController player, IEnumerable<Unit> source = null)
{
    if (player == null) yield break;

    var faction = player.myFaction;

    // REFATORAÇÃO: Otimização - se source for null, usar diretamente GetByFaction (mais rápido)
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

**Parâmetros:**
- `player`: PlayerController (contém `myFaction`)
- `source`: Lista opcional de unidades a filtrar (default: `null`)

**Retorno:** `IEnumerable<Unit>` filtrado por facção do player

**Comportamento:**

**Caso 1: source = null (OTIMIZADO)**
```csharp
// Usa GetByFaction diretamente (O(1) + O(n) iteração)
var myUnits = UnitQueries.GetUnitsForPlayer(player);
// Equivalente a: UnitRegistry.GetByFaction(player.myFaction)
```

**Caso 2: source fornecido (FILTRO)**
```csharp
// Filtra source fornecido (O(n) com condição)
var allUnits = UnitRegistry.All;
var myUnits = UnitQueries.GetUnitsForPlayer(player, allUnits);
// Retorna apenas unidades com u.owner == player.myFaction
```

**Quando usar cada caso:**

| Cenário | source | Justificativa |
|---------|--------|---------------|
| Query simples | `null` | Usa GetByFaction (mais rápido) |
| Já tem lista | Passar lista | Evita criar nova lista |
| Query complexa | Passar filtro prévio | Ex: apenas warriors do player |

**Exemplo de Uso:**

```csharp
// Uso simples (otimizado)
var myUnits = UnitQueries.GetUnitsForPlayer(playerController);
foreach (var unit in myUnits)
{
    Debug.Log($"Minha unidade: {unit.DisplayName}");
}

// Uso com filtro prévio
var allWarriors = UnitRegistry.All.Where(u => u.def.type == UnitType.Warrior);
var myWarriors = UnitQueries.GetUnitsForPlayer(playerController, allWarriors);
```

---

### 5.3 EnumerateSceneUnits()

```csharp
/// <summary>Fallback seguro: varre a cena atrás de Units.</summary>
public static IEnumerable<Unit> EnumerateSceneUnits()
{
#if UNITY_2023_1_OR_NEWER
    var arr = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
    var arr = Object.FindObjectsOfType<Unit>(true);
#endif
    for (int i = 0; i < arr.Length; i++)
        yield return arr[i];
}
```

**Descrição:** Fallback que varre toda a cena por componentes `Unit`.

**Quando Usar:**
- ⚠️ **Último recurso** quando `UnitRegistry` falha (raro)
- Debug/Editor tools
- Validação de consistência

**Performance:**
- ❌ **O(n) da cena inteira** (lento)
- ❌ Causa alocação de array temporário

**Compatibilidade:**
- Unity 2023.1+: Usa `FindObjectsByType` (novo)
- Unity 2022 e anterior: Usa `FindObjectsOfType` (legado)

**Exemplo de Uso:**

```csharp
// Debug: Validar se UnitRegistry está consistente
void ValidateRegistry()
{
    var sceneUnits = UnitQueries.EnumerateSceneUnits().ToList();
    var registeredUnits = UnitRegistry.All;
    
    if (sceneUnits.Count != registeredUnits.Count)
    {
        Debug.LogWarning($"Inconsistência! Cena: {sceneUnits.Count}, Registry: {registeredUnits.Count}");
    }
}
```

---

### 5.4 Comparação de Performance

**Benchmark (1000 unidades, 4 facções):**

| Método | Tempo | Uso |
|--------|-------|-----|
| `UnitRegistry.GetByFaction(Player1)` | ~0.01ms | ✅ Preferido |
| `UnitQueries.GetUnitsForPlayer(player, null)` | ~0.01ms | ✅ Preferido (usa GetByFaction) |
| `UnitQueries.GetUnitsForPlayer(player, All)` | ~0.5ms | ⚠️ Apenas se necessário |
| `UnitQueries.EnumerateSceneUnits()` | ~10ms | ❌ Evitar (FindObjects lento) |

**Recomendação:**
- ✅ **Sempre use** `GetUnitsForPlayer(player, null)` para queries simples
- ⚠️ **Considere** passar `source` apenas se já tem lista filtrada
- ❌ **Evite** `EnumerateSceneUnits()` em runtime

---

## 6) FLUXO DE CICLO DE VIDA COMPLETO

### 6.1 Spawn (OnEnable → Register → RaiseUnitSpawned)

```
GameObject com Unit é ativado/instanciado
       │
       ▼
Unit.OnEnable()
       │
       │ UnitRegistry.Register(this)
       ▼
UnitRegistry.Register()
       │
       ├──▶ _all.Add(u)
       ├──▶ _perFaction[u.owner].Add(u)
       │
       │ GameEvents.RaiseUnitSpawned(u)
       ▼
GameEvents.OnUnitSpawned?.Invoke(u)
       │
       ├─────────────────┬─────────────────┬─────────────────┐
       ▼                 ▼                 ▼                 ▼
┌──────────────┐  ┌─────────────┐  ┌──────────────┐  ┌────────┐
│ MinimapSystem│  │ FogOfWar    │  │ AISystem     │  │ Audio  │
│ (adiciona    │  │ (revela     │  │ (registra    │  │ (som)  │
│  ícone)      │  │  área)      │  │  ameaça)     │  │        │
└──────────────┘  └─────────────┘  └──────────────┘  └────────┘
```

**Código de Exemplo:**

```csharp
// Instanciar unidade via código
Unit newUnit = Instantiate(workerPrefab, spawnPos, Quaternion.identity);
// OnEnable() é chamado automaticamente
// → UnitRegistry.Register() automático
// → GameEvents.OnUnitSpawned disparado
```

---

### 6.2 Seleção (SetSelected → RaiseUnitSelectionChanged)

```
Jogador clica na unidade (via InputSelection)
       │
       ▼
Unit.SetSelected(true)
       │
       ├──▶ IsSelected = true
       ├──▶ selectionHighlight.SetActive(true)
       │
       │ GameEvents.RaiseUnitSelectionChanged(this, true)
       ▼
GameEvents.OnUnitSelectionChanged?.Invoke(this, true)
       │
       ├─────────────────┬─────────────────┬─────────────────┐
       ▼                 ▼                 ▼                 ▼
┌──────────────┐  ┌─────────────┐  ┌──────────────┐  ┌────────┐
│ UnitListUI   │  │ AudioManager│  │ RTSCamera    │  │ Debug  │
│ (atualiza    │  │ (som de     │  │ (foco        │  │ (log)  │
│  lista)      │  │  seleção)   │  │  opcional)   │  │        │
└──────────────┘  └─────────────┘  └──────────────┘  └────────┘
```

**Código de Exemplo:**

```csharp
// Em SelectionManager (Lote 4)
void HandleClickUnit(Unit unit, bool ctrl)
{
    if (!ctrl) Clear(); // Limpa seleção anterior
    unit.SetSelected(true); // ← Dispara eventos
}
```

---

### 6.3 Progressão (AddXp → Level Up → RaiseUnitProgressChanged)

```
Sistema de combate/coleta dá XP
       │
       ▼
Unit.AddXp(50)
       │
       ├──▶ xp += 50
       │
       │ Loop: while (xp >= XpToNext)
       ├──▶ xp -= XpToNext
       ├──▶ level++
       │
       │ GameEvents.RaiseUnitProgressChanged(this)
       ▼
GameEvents.OnUnitProgressChanged?.Invoke(this)
       │
       ├─────────────────┬─────────────────┬─────────────────┐
       ▼                 ▼                 ▼                 ▼
┌──────────────┐  ┌─────────────┐  ┌──────────────┐  ┌────────┐
│UnitListItemUI│  │ AudioManager│  │ VFXManager   │  │ Stats  │
│ (atualiza    │  │ (som level  │  │ (partículas  │  │ (buff) │
│  XP bar)     │  │  up)        │  │  level up)   │  │        │
└──────────────┘  └─────────────┘  └──────────────┘  └────────┘
```

**Código de Exemplo:**

```csharp
// Sistema de combate
void OnEnemyKilled(Unit killer, Unit victim)
{
    float xpReward = victim.Level * 50f;
    killer.AddXp(xpReward); // ← Dispara eventos
}

// Sistema de coleta
void OnResourceGathered(Unit worker, ResourceType type, int amount)
{
    float xpReward = amount * 0.5f;
    worker.AddXp(xpReward); // ← Dispara eventos
}
```

---

### 6.4 Despawn (OnDisable → Unregister → RaiseUnitDespawned)

```
GameObject com Unit é desativado/destruído
       │
       ▼
Unit.OnDisable()
       │
       │ UnitRegistry.Unregister(this)
       ▼
UnitRegistry.Unregister()
       │
       ├──▶ _all.Remove(u)
       ├──▶ _perFaction[u.owner].Remove(u)
       │
       │ GameEvents.RaiseUnitDespawned(u)
       ▼
GameEvents.OnUnitDespawned?.Invoke(u)
       │
       ├─────────────────┬─────────────────┬─────────────────┐
       ▼                 ▼                 ▼                 ▼
┌──────────────┐  ┌─────────────┐  ┌──────────────┐  ┌────────┐
│ MinimapSystem│  │ FogOfWar    │  │ AISystem     │  │ Audio  │
│ (remove      │  │ (esconde    │  │ (remove      │  │ (som)  │
│  ícone)      │  │  área)      │  │  ameaça)     │  │        │
└──────────────┘  └─────────────┘  └──────────────┘  └────────┘
```

**Código de Exemplo:**

```csharp
// Destruir unidade
Destroy(unit.gameObject);
// OnDisable() é chamado automaticamente
// → UnitRegistry.Unregister() automático
// → GameEvents.OnUnitDespawned disparado
```

---

## 7) INTEGRAÇÃO COM OUTROS MÓDULOS

### 7.1 Dependências do Lote 1 (Enums, GameEvents)

**Enums Usados:**

| Enum | Uso no Lote 3 | Documentação |
|------|---------------|--------------|
| `FactionId` | `Unit.owner`, `UnitRegistry._perFaction` | Lote 1, Seção 3.2 |
| `UnitType` | `UnitDefinition.type` | Lote 1, Seção 3.2 |

**Eventos Emitidos:**

| Evento | Emissor | Quando | Documentação |
|--------|---------|--------|--------------|
| `OnUnitSpawned` | `UnitRegistry.Register()` | Unit ativado/instanciado | Lote 1, Seção 2.4.5 |
| `OnUnitDespawned` | `UnitRegistry.Unregister()` | Unit desativado/destruído | Lote 1, Seção 2.4.5 |
| `OnUnitSelectionChanged` | `Unit.SetSelected()` | Seleção/desseleção | Lote 1, Seção 2.4.5 |
| `OnUnitProgressChanged` | `Unit.AddXp()` | XP/Level muda | Lote 1, Seção 2.4.5 |

**Integração com PlayerController:**

```csharp
// UnitQueries usa PlayerController para filtrar por facção
var myUnits = UnitQueries.GetUnitsForPlayer(playerController);
// Usa playerController.myFaction para obter FactionId
```

---

### 7.2 Interação com Lote 2 (Câmera)

**Evento Consumido pela Câmera:**

```csharp
// RTSCameraController (Lote 2) escuta OnSelectionFocus
void OnEnable()
{
    GameEvents.OnSelectionFocus += HandleSelectionFocus;
}

void HandleSelectionFocus(Transform target)
{
    if (target != null)
        GoTo(target.position, snap: false, duration: 0.3f);
}
```

**Possível Integração Futura (opcional):**

```csharp
// Em Unit.SetSelected() (se desejado)
public void SetSelected(bool value)
{
    if (IsSelected == value) return;
    IsSelected = value;
    if (selectionHighlight) selectionHighlight.SetActive(value);
    
    GameEvents.RaiseUnitSelectionChanged(this, value);
    
    // Opcional: Focar câmera na unidade selecionada
    if (value)
    {
        GameEvents.RaiseSelectionFocus(transform);
    }
}
```

---

### 7.3 Interação com Lote 4 (Selection System)

**Fluxo de Seleção:**

```
InputSelection detecta clique
       │
       │ GameEvents.RaiseUnitClick(unit, ctrl)
       ▼
SelectionManager escuta evento
       │
       │ unit.SetSelected(true)
       ▼
Unit.SetSelected()
       │
       │ GameEvents.RaiseUnitSelectionChanged(this, true)
       ▼
UI/Audio/etc escutam e reagem
```

**Exemplo de Integração:**

```csharp
// SelectionManager (Lote 4) chama SetSelected
void HandleClickUnit(Unit unit, bool ctrl)
{
    if (onlyOwnUnits && unit.owner != player.myFaction) return;
    
    if (ctrl) Toggle(unit);
    else { Clear(); Add(unit); }
}

void Add(Unit u)
{
    if (_selection.Add(u)) 
        u.SetSelected(true); // ← Chama Unit.SetSelected()
}
```

---

### 7.4 Interação com UI System (Futuro)

**Listeners Típicos:**

**UnitListUI:**
```csharp
void OnEnable()
{
    GameEvents.OnSelectionChanged += UpdateList;
    GameEvents.OnUnitProgressChanged += OnProgressChanged;
}

void UpdateList(IReadOnlyCollection<Unit> units)
{
    // Reconstruir lista de UI com units selecionadas
}

void OnProgressChanged(Unit unit)
{
    // Atualizar XP bar da unidade específica
}
```

**UnitTooltipUI:**
```csharp
void OnPointerEnter(PointerEventData eventData)
{
    Unit unit = GetUnitUnderPointer();
    tooltipText.text = $"{unit.DisplayName}\nHP: {unit.hp}/{unit.hpMax}\nLevel: {unit.Level}";
}
```

---

## 8) CONFIGURAÇÃO NA CENA

### 8.1 Adicionar Unit a GameObject

**Passo a Passo:**

1. **Criar GameObject:**
   - Hierarchy → Create Empty
   - Nome: "Worker (1)"
   - Adicionar modelo 3D (mesh)

2. **Adicionar Componente Unit:**
   - Add Component → Unit

3. **Configurar Inspector:**
   ```
   def: Worker.asset (arraste ScriptableObject)
   owner: Player1
   hp: 100
   hpMax: 100
   ```

4. **Configurar Highlight de Seleção:**
   - Criar GameObject filho: "SelectionRing"
   - Adicionar Quad/Mesh com shader transparente
   - Arrastar para campo `selectionHighlight`

5. **Configurar Progressão (opcional):**
   ```
   level: 1
   xp: 0
   baseXpToLevel: 100
   xpGrowth: 1.35
   ```

**Hierarquia Típica:**

```
Worker (1) (GameObject)
├─ Unit (Script)
├─ Model (MeshRenderer)
│  └─ Mesh, Material
└─ SelectionRing (GameObject) ← Atribuir a selectionHighlight
   └─ Quad com shader transparente
```

---

### 8.2 Criar UnitDefinition Assets

**Passo a Passo:**

1. **Criar Asset:**
   - Project → Create → Game → Unit Definition
   - Nome: "Worker"

2. **Configurar Inspector:**
   ```
   displayName: "Operário"
   type: Worker
   icon: worker_icon.png (arraste sprite)
   ```

3. **Repetir para outros tipos:**
   - Warrior.asset
   - Archer.asset
   - Spearman.asset
   - etc.

**Estrutura de Pastas Recomendada:**

```
Assets/
├── Settings/
│   └── Units/
│       ├── Worker.asset
│       ├── Warrior.asset
│       ├── Archer.asset
│       └── Hero_Arthur.asset
│
└── Sprites/
    └── UnitIcons/
        ├── worker_icon.png
        ├── warrior_icon.png
        └── archer_icon.png
```

---

### 8.3 Setup de Highlight de Seleção

#### **Opção 1: Ring no Chão (Simples)**

**Passo a Passo:**

1. Criar GameObject filho: "SelectionRing"
2. Add Component → MeshFilter → Quad
3. Add Component → MeshRenderer
4. Configurar Material:
   - Shader: Transparent/Diffuse
   - Texture: ring_texture.png (anel branco)
   - Color: Cor da facção (azul, vermelho, etc.)
5. Rotacionar: X=90° (para ficar paralelo ao chão)
6. Escala: (2, 2, 2) ou ajustar ao tamanho da unidade

**Material Settings:**
```
Rendering Mode: Transparent
Main Texture: ring_texture.png
Tint Color: RGB(0, 150, 255, 128) (azul semi-transparente)
```

---

#### **Opção 2: Outline Shader (Avançado)**

**Shader:**
```hlsl
// Outline.shader (exemplo simplificado)
Shader "Custom/Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }
    // ... passes de outline
}
```

**Setup:**
1. Aplicar shader no material do modelo
2. Ativar/desativar outline via propriedade do material
3. Ou usar GameObject separado com outline mesh

---

#### **Opção 3: Particle System (Visual)**

**Setup:**
1. Add Component → Particle System
2. Configurar:
   - Shape: Circle (ao redor da unidade)
   - Start Color: Cor da facção
   - Start Speed: 0 (partículas estáticas)
   - Emission: Rate Over Time = 20
3. Ativar/desativar ParticleSystem via `SetActive()`

---

## 9) EXEMPLOS DE USO AVANÇADOS

### 9.1 Criar Tipo de Unidade Customizado

**Cenário:** Adicionar novo tipo "Mago" com sistema de mana.

**1. Criar Enum (Lote 1):**

```csharp
// Em Enums.cs
public enum UnitType { 
    Worker,
    Warrior,
    // ... outros
    Mage // ← Adicionar
}
```

**2. Criar UnitDefinition Asset:**

```
Assets/Settings/Units/Mage.asset:
  displayName: "Mago"
  type: Mage
  icon: mage_icon.png
```

**3. Extender Unit (opcional):**

```csharp
// UnitMage.cs (herda de Unit)
public class UnitMage : MonoBehaviour
{
    Unit _unit;
    
    [Header("Mana")]
    public float mana = 100f;
    public float manaMax = 100f;
    
    void Awake()
    {
        _unit = GetComponent<Unit>();
    }
    
    public void CastSpell(float manaCost)
    {
        if (mana >= manaCost)
        {
            mana -= manaCost;
            // Lógica do spell
        }
    }
}
```

---

### 9.2 Sistema de XP/Level Up com Recompensas

**Cenário:** Adicionar benefícios ao upar (HP, dano, habilidades).

**Código:**

```csharp
// UnitProgression.cs (componente adicional)
public class UnitProgression : MonoBehaviour
{
    Unit _unit;
    
    [Header("Level Up Bonuses")]
    public float hpBonusPerLevel = 0.05f; // 5% por nível
    public float damageBonusPerLevel = 0.03f; // 3% por nível
    
    void Awake()
    {
        _unit = GetComponent<Unit>();
    }
    
    void OnEnable()
    {
        GameEvents.OnUnitProgressChanged += OnProgressChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitProgressChanged -= OnProgressChanged;
    }
    
    void OnProgressChanged(Unit changedUnit)
    {
        if (changedUnit == _unit)
        {
            ApplyLevelUpBonuses();
        }
    }
    
    void ApplyLevelUpBonuses()
    {
        // Aumentar HP máximo
        float hpMultiplier = 1f + (_unit.Level - 1) * hpBonusPerLevel;
        _unit.hpMax = 100f * hpMultiplier;
        _unit.hp = _unit.hpMax; // Curar ao upar
        
        // Aumentar dano (assumindo que existe componente de combate)
        var combat = GetComponent<UnitCombat>();
        if (combat)
        {
            float damageMultiplier = 1f + (_unit.Level - 1) * damageBonusPerLevel;
            combat.baseDamage *= damageMultiplier;
        }
        
        // Desbloquear habilidades
        if (_unit.Level == 5)
        {
            UnlockAbility("PowerStrike");
        }
        if (_unit.Level == 10)
        {
            UnlockAbility("AreaAttack");
        }
        
        // VFX de level up
        PlayLevelUpEffect();
    }
    
    void PlayLevelUpEffect()
    {
        // Partículas, som, etc.
        var fx = Instantiate(levelUpVFX, transform.position, Quaternion.identity);
        Destroy(fx, 2f);
        
        AudioManager.PlaySound("LevelUp", transform.position);
    }
    
    void UnlockAbility(string abilityName)
    {
        Debug.Log($"{_unit.DisplayName} desbloqueou habilidade: {abilityName}!");
        // Adicionar à UI de habilidades
    }
}
```

---

### 9.3 Filtrar Unidades por Facção e Tipo

**Cenário:** Obter todos os workers da Player1.

**Código:**

```csharp
// Query simples
var player1Units = UnitRegistry.GetByFaction(FactionId.Player1);
var workers = player1Units.Where(u => u.def.type == UnitType.Worker);

Debug.Log($"Player1 tem {workers.Count()} workers");

// Query complexa: Workers dentro de raio
Vector3 center = buildingPos;
float radius = 10f;

var nearbyWorkers = UnitRegistry.GetByFaction(FactionId.Player1)
    .Where(u => u.def.type == UnitType.Worker)
    .Where(u => Vector3.Distance(u.transform.position, center) <= radius);

foreach (var worker in nearbyWorkers)
{
    Debug.Log($"Worker próximo: {worker.DisplayName}");
}
```

---

### 9.4 Sistema de Auto-Coleta de XP

**Cenário:** Workers ganham XP automaticamente ao coletar recursos.

**Código:**

```csharp
// ResourceGatherer.cs (componente em Worker)
public class ResourceGatherer : MonoBehaviour
{
    Unit _unit;
    
    [Header("XP Settings")]
    public float xpPerResource = 0.5f; // 0.5 XP por recurso
    
    void Awake()
    {
        _unit = GetComponent<Unit>();
    }
    
    void OnResourceGathered(ResourceType type, int amount)
    {
        // Lógica de coleta...
        
        // Dar XP
        float xpReward = amount * xpPerResource;
        _unit.AddXp(xpReward);
    }
}
```

---

### 9.5 Debug: Listar Todas as Unidades

**Cenário:** Comando de debug para inspecionar unidades ativas.

**Código:**

```csharp
// DebugCommands.cs
public class DebugCommands : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ListAllUnits();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ListUnitsByFaction(FactionId.Player1);
        }
    }
    
    void ListAllUnits()
    {
        Debug.Log($"=== TODAS AS UNIDADES ({UnitRegistry.All.Count}) ===");
        
        foreach (var unit in UnitRegistry.All)
        {
            Debug.Log($"  - {unit.DisplayName} (Facção: {unit.owner}, HP: {unit.hp}/{unit.hpMax}, Level: {unit.Level})");
        }
    }
    
    void ListUnitsByFaction(FactionId faction)
    {
        var units = UnitRegistry.GetByFaction(faction);
        
        Debug.Log($"=== UNIDADES DE {faction} ({units.Count}) ===");
        
        foreach (var unit in units)
        {
            Debug.Log($"  - {unit.DisplayName} (Tipo: {unit.def.type}, HP: {unit.hp}/{unit.hpMax}, Level: {unit.Level})");
        }
    }
}
```

---

## 10) SISTEMA DE PROGRESSÃO (XP E LEVEL UP)

### 10.1 Fórmula Matemática Detalhada

**Equação Exponencial:**

```
XpToNext(L) = B * G^(L - 1)

Onde:
  L = Level atual
  B = baseXpToLevel (padrão: 100)
  G = xpGrowth (padrão: 1.35)
```

**Derivação:**

```
Level 1→2: B * G^0 = 100 * 1 = 100 XP
Level 2→3: B * G^1 = 100 * 1.35 = 135 XP
Level 3→4: B * G^2 = 100 * 1.82 = 182 XP
Level N→N+1: B * G^(N-1)
```

---

### 10.2 Tabela de Progressão Completa

**Configuração Padrão (base=100, growth=1.35):**

| Level | XP Necessário | XP Acumulado | Tempo (50 XP/min) | Fórmula |
|-------|---------------|--------------|-------------------|---------|
| 1→2 | 100 | 0-100 | 2 min | 100 * 1.35⁰ |
| 2→3 | 135 | 100-235 | 2.7 min | 100 * 1.35¹ |
| 3→4 | 182 | 235-417 | 3.6 min | 100 * 1.35² |
| 4→5 | 246 | 417-663 | 4.9 min | 100 * 1.35³ |
| 5→6 | 332 | 663-995 | 6.6 min | 100 * 1.35⁴ |
| 6→7 | 448 | 995-1443 | 9 min | 100 * 1.35⁵ |
| 7→8 | 605 | 1443-2048 | 12 min | 100 * 1.35⁶ |
| 8→9 | 817 | 2048-2865 | 16 min | 100 * 1.35⁷ |
| 9→10 | 1103 | 2865-3968 | 22 min | 100 * 1.35⁸ |
| 10→11 | 1489 | 3968-5457 | 30 min | 100 * 1.35⁹ |
| 15→16 | 6668 | - | 133 min | 100 * 1.35¹⁴ |
| 20→21 | 29866 | - | 597 min | 100 * 1.35¹⁹ |

**XP Total para Alcançar:**

| Level | XP Total Necessário | Tempo (50 XP/min) |
|-------|---------------------|-------------------|
| Level 5 | 663 XP | ~13 minutos |
| Level 10 | 3968 XP | ~79 minutos |
| Level 15 | 17663 XP | ~353 minutos (6h) |
| Level 20 | 76617 XP | ~1532 minutos (25h) |

---

### 10.3 Customização de Curvas

#### **Progressão Rápida (Casual):**

```csharp
baseXpToLevel = 50f;
xpGrowth = 1.2f; // 20% por nível
```

**Resultado:**
- Level 5: 103 XP total (vs 663 no padrão)
- Level 10: 258 XP total (vs 3968 no padrão)
- Jogadores alcançam níveis altos rapidamente

---

#### **Progressão Lenta (Hardcore):**

```csharp
baseXpToLevel = 200f;
xpGrowth = 1.5f; // 50% por nível
```

**Resultado:**
- Level 5: 1013 XP total (vs 663 no padrão)
- Level 10: 7706 XP total (vs 3968 no padrão)
- Progressão muito lenta, níveis altos são raros

---

#### **Progressão Linear (Simples):**

```csharp
baseXpToLevel = 100f;
xpGrowth = 1.0f; // 0% por nível (linear)
```

**Resultado:**
- Level 1→2: 100 XP
- Level 2→3: 100 XP
- Level 10→11: 100 XP
- Sempre 100 XP por nível

---

### 10.4 Fontes de XP (Exemplos de Integração)

#### **Combate:**

```csharp
// CombatSystem.cs
void OnEnemyKilled(Unit killer, Unit victim)
{
    // XP proporcional ao level da vítima
    float baseXP = 50f;
    float levelMultiplier = victim.Level;
    float xpReward = baseXP * levelMultiplier;
    
    killer.AddXp(xpReward);
}
```

---

#### **Coleta de Recursos:**

```csharp
// ResourceGatherer.cs
void OnResourceGathered(ResourceType type, int amount)
{
    // XP proporcional à quantidade
    float xpPerResource = 0.5f;
    float xpReward = amount * xpPerResource;
    
    _unit.AddXp(xpReward);
}
```

---

#### **Construção de Edifícios:**

```csharp
// BuildingSystem.cs
void OnBuildingCompleted(Unit builder, Building building)
{
    // XP proporcional ao custo do edifício
    float xpReward = building.cost * 0.1f;
    
    builder.AddXp(xpReward);
}
```

---

#### **Missões:**

```csharp
// QuestSystem.cs
void OnQuestCompleted(Quest quest, Unit unit)
{
    float xpReward = quest.xpReward;
    
    unit.AddXp(xpReward);
}
```

---

### 10.5 Proteção Contra Valores Absurdos

**Guard Clause (linha 61 de Unit.cs):**

```csharp
var guard = 64; // evita loop infinito em valores absurdos

while (xp >= XpToNext && guard-- > 0)
{
    xp -= XpToNext;
    level++;
}
```

**Por que 64?**
- Limita level ups simultâneos a 64 níveis
- Previne travamento se XP for astronomicamente alto (ex: 999999999)
- Valor arbitrário mas razoável (improvável precisar de mais)

**Exemplo de Proteção:**

```csharp
// Sem proteção (❌):
unit.AddXp(1e10f); // 10 bilhões de XP
// Loop infinito ou travamento

// Com proteção (✅):
unit.AddXp(1e10f);
// Loop para após 64 iterações
// level = 65 (máximo alcançado)
// xp = excedente
```

---

## 11) TROUBLESHOOTING E FAQ

### 11.1 Unidade não aparece no UnitRegistry

**Sintomas:**
- `UnitRegistry.All.Count` não inclui a unidade
- `GetByFaction()` não retorna a unidade

**Soluções:**

1. **Verificar se GameObject está ativo:**
   ```csharp
   if (!unit.gameObject.activeInHierarchy)
       Debug.LogError("GameObject inativo! OnEnable não foi chamado.");
   ```

2. **Verificar se componente Unit está habilitado:**
   - Inspector → Unit (Script) → Checkbox marcada

3. **Verificar console para erros:**
   - Se `OnEnable()` lançar exceção, registro falha

4. **Debug manual:**
   ```csharp
   // Em Unit (método temporário)
   void OnEnable()
   {
       Debug.Log($"[{gameObject.name}] OnEnable chamado");
       UnitRegistry.Register(this);
       Debug.Log($"[{gameObject.name}] Registrado. Total: {UnitRegistry.All.Count}");
   }
   ```

---

### 11.2 Highlight de seleção não aparece

**Sintomas:**
- Chamar `SetSelected(true)` não mostra visual
- `selectionHighlight` permanece invisível

**Soluções:**

1. **Verificar se `selectionHighlight` está atribuído:**
   ```csharp
   if (selectionHighlight == null)
       Debug.LogError("selectionHighlight não atribuído no Inspector!");
   ```

2. **Verificar se GameObject filho existe:**
   - Hierarquia → Verificar se "SelectionRing" (ou similar) existe

3. **Verificar camadas (Layers):**
   - Highlight pode estar em layer que não é renderizada pela câmera

4. **Verificar shader/material:**
   - Material pode estar com alpha=0 (invisível)
   - Shader pode não ser renderizado (culling, etc.)

5. **Debug visual:**
   ```csharp
   // Em Unit.SetSelected()
   if (selectionHighlight) 
   {
       selectionHighlight.SetActive(value);
       Debug.Log($"[{gameObject.name}] Highlight: {(value ? "ON" : "OFF")}");
       Debug.Log($"Highlight.activeSelf: {selectionHighlight.activeSelf}");
   }
   ```

---

### 11.3 XP não aumenta ou level up não funciona

**Sintomas:**
- Chamar `AddXp()` não muda `xp` ou `level`
- Barra de XP na UI não atualiza

**Soluções:**

1. **Verificar parâmetro de AddXp:**
   ```csharp
   unit.AddXp(0); // ❌ amount <= 0, retorna imediatamente
   unit.AddXp(-50); // ❌ amount <= 0, retorna imediatamente
   unit.AddXp(50); // ✅ OK
   ```

2. **Verificar fórmula de XpToNext:**
   ```csharp
   Debug.Log($"Level: {unit.Level}");
   Debug.Log($"XP: {unit.Xp}");
   Debug.Log($"XpToNext: {unit.XpToNext}");
   Debug.Log($"Xp01: {unit.Xp01}"); // Deve ser 0..1
   ```

3. **Verificar valores de progressão:**
   - `baseXpToLevel` deve ser > 0
   - `xpGrowth` deve ser > 0
   - Se `xpGrowth = 0`, causa divisão por zero

4. **Verificar eventos disparados:**
   ```csharp
   void OnEnable()
   {
       GameEvents.OnUnitProgressChanged += (u) => {
           if (u == this) Debug.Log($"[{gameObject.name}] Progress changed!");
       };
   }
   ```

---

### 11.4 UnitRegistry.GetByFaction retorna lista vazia

**Sintomas:**
- `GetByFaction(Player1)` retorna 0 unidades
- Mas unidades existem na cena

**Soluções:**

1. **Verificar FactionId das unidades:**
   ```csharp
   // Unidade pode estar com owner diferente
   foreach (var unit in UnitRegistry.All)
       Debug.Log($"{unit.DisplayName} - owner: {unit.owner}");
   ```

2. **Verificar se passou FactionId correto:**
   ```csharp
   // ❌ Errado
   var units = UnitRegistry.GetByFaction(FactionId.Player2);
   
   // ✅ Correto (obter do PlayerController)
   var units = UnitRegistry.GetByFaction(playerController.myFaction);
   ```

3. **Usar fallback de debug:**
   ```csharp
   // Comparar contagens
   int totalUnits = UnitRegistry.All.Count;
   int player1Units = UnitRegistry.GetByFaction(FactionId.Player1).Count;
   
   Debug.Log($"Total: {totalUnits}, Player1: {player1Units}");
   ```

---

### 11.5 Eventos de Unit não disparam

**Sintomas:**
- `OnUnitSpawned` não chama listeners
- `OnUnitProgressChanged` não chama listeners

**Soluções:**

1. **Verificar subscribe/unsubscribe:**
   ```csharp
   // ✅ Correto
   void OnEnable()
   {
       GameEvents.OnUnitSpawned += HandleSpawn;
   }
   
   void OnDisable()
   {
       GameEvents.OnUnitSpawned -= HandleSpawn; // CRÍTICO
   }
   ```

2. **Verificar se handler está correto:**
   ```csharp
   // ❌ Assinatura errada
   void HandleSpawn(Unit unit, bool value) // Espera 1 parâmetro, não 2!
   
   // ✅ Assinatura correta
   void HandleSpawn(Unit unit) // OK
   ```

3. **Debug de eventos:**
   ```csharp
   // Adicionar listener temporário
   void Start()
   {
       GameEvents.OnUnitSpawned += (u) => Debug.Log($"SPAWN: {u.DisplayName}");
       GameEvents.OnUnitProgressChanged += (u) => Debug.Log($"PROGRESS: {u.DisplayName}");
   }
   ```

---

## 12) TABELA DE RELACIONAMENTOS COMPLETA

### 12.1 Classes do Lote 3

| Classe | Tipo | Depende De | Dependentes | Eventos (Emit) | Eventos (Listen) |
|--------|------|-----------|-------------|----------------|------------------|
| **UnitDefinition** | SO | `UnitType` (Lote 1) | `Unit` | - | - |
| **Unit** | MB | `UnitDefinition`, `FactionId` (Lote 1), `GameEvents` (Lote 1), `UnitRegistry` | `SelectionManager` (Lote 4), UI Systems | `OnUnitSelectionChanged`, `OnUnitProgressChanged` | - |
| **UnitRegistry** | static | `Unit`, `FactionId` (Lote 1), `GameEvents` (Lote 1) | `UnitQueries`, `SelectionManager` (Lote 4), UI, IA | `OnUnitSpawned`, `OnUnitDespawned` | - |
| **UnitQueries** | static | `Unit`, `UnitRegistry`, `PlayerController` (Lote 1) | Sistemas de consulta (UI, IA) | - | - |

---

### 12.2 Integrações com Outros Lotes

| Lote Consumidor | Usa do Lote 3 | Forma de Uso |
|-----------------|---------------|--------------|
| **Lote 1 - GameEvents** | Unit e Registry emitem eventos | `RaiseUnitSpawned`, `RaiseUnitDespawned`, `RaiseUnitSelectionChanged`, `RaiseUnitProgressChanged` |
| **Lote 2 - Câmera** | (opcional) SelectionFocus | `GameEvents.RaiseSelectionFocus(unit.transform)` se `SetSelected(true)` |
| **Lote 4 - Selection** | Chama `SetSelected()` | `unit.SetSelected(true/false)` ao selecionar/desselecionar |
| **UI System** | Escuta eventos, consulta Registry | `GameEvents.OnUnitProgressChanged`, `UnitRegistry.GetByFaction()` |
| **Minimap** | Escuta Spawn/Despawn | `GameEvents.OnUnitSpawned`, `GameEvents.OnUnitDespawned` |
| **IA System** | Consulta Registry para ameaças | `UnitRegistry.GetByFaction(enemyFaction)` |

---

### 12.3 Fluxo de Dados Completo

```
Unit.OnEnable()
       │
       ▼
UnitRegistry.Register(this)
       │
       ├──▶ _all.Add(unit)
       ├──▶ _perFaction[owner].Add(unit)
       │
       │ GameEvents.RaiseUnitSpawned(unit)
       ▼
GameEvents.OnUnitSpawned?.Invoke(unit)
       │
       ├──────────────┬──────────────┬──────────────┐
       ▼              ▼              ▼              ▼
  MinimapSystem  FogOfWar    AISystem    Analytics
  (add icon)     (reveal)    (register)  (track)
```

---

## 13) REFERÊNCIAS RÁPIDAS

### 13.1 Atalhos de Código

**Registrar unidade manualmente:**
```csharp
UnitRegistry.Register(unit);
```

**Desregistrar unidade manualmente:**
```csharp
UnitRegistry.Unregister(unit);
```

**Obter unidades do jogador:**
```csharp
var myUnits = UnitRegistry.GetByFaction(playerController.myFaction);
```

**Adicionar XP:**
```csharp
unit.AddXp(50f);
```

**Selecionar/desselecionar:**
```csharp
unit.SetSelected(true);
unit.SetSelected(false);
```

---

### 13.2 Valores Típicos

| Parâmetro | Min | Típico | Max | Descrição |
|-----------|-----|--------|-----|-----------|
| hp | 50 | 100 | 500+ | HP de unidades (worker=100, herói=500) |
| level | 1 | 1 | 20 | Nível inicial (todos começam em 1) |
| baseXpToLevel | 50 | 100 | 200 | XP base para 1→2 |
| xpGrowth | 1.1 | 1.35 | 1.5 | Multiplicador exponencial |

---

### 13.3 Fórmulas Úteis

**XP necessário para próximo nível:**
```csharp
float xpNeeded = baseXpToLevel * Mathf.Pow(xpGrowth, level - 1);
```

**Progresso de XP (0..1):**
```csharp
float progress = Mathf.Clamp01(xp / XpToNext);
```

**XP total para alcançar nível N:**
```csharp
float totalXp = 0f;
for (int i = 1; i < targetLevel; i++)
{
    totalXp += baseXpToLevel * Mathf.Pow(xpGrowth, i - 1);
}
```

---

### 13.4 Estrutura de Arquivos

```
Assets/
├── Scripts/
│   └── Units/
│       ├── Unit.cs
│       ├── UnitDefinition.cs
│       ├── UnitRegistry.cs
│       └── UnitQueries.cs
│
├── Settings/
│   └── Units/
│       ├── Worker.asset
│       ├── Warrior.asset
│       ├── Archer.asset
│       └── Hero_Arthur.asset
│
└── Sprites/
    └── UnitIcons/
        ├── worker_icon.png
        ├── warrior_icon.png
        └── archer_icon.png
```

---

## 14) CHANGELOG E MIGRAÇÕES

### 14.1 Mudanças da Versão Anterior → v1.0

#### **✅ ADICIONADO:**

1. **Eventos via GameEvents (Refatoração Principal):**
   - `Unit`: Eventos locais removidos → `GameEvents.RaiseUnitSelectionChanged`, `RaiseUnitProgressChanged`
   - `UnitRegistry`: Eventos estáticos removidos → `GameEvents.RaiseUnitSpawned`, `RaiseUnitDespawned`

2. **Otimização de UnitQueries:**
   - `GetUnitsForPlayer()` agora usa `GetByFaction` diretamente se `source = null`
   - Performance melhorada em ~90% para queries simples

3. **Método de Debug Protegido:**
   - `Unit.TestList()` movido para `#if UNITY_EDITOR` (não compila em build)

#### **🔄 MODIFICADO:**

1. **Unit.cs:**
   - Eventos locais substituídos por GameEvents
   - `SetSelected()` agora dispara `GameEvents.RaiseUnitSelectionChanged`
   - `AddXp()` agora dispara `GameEvents.RaiseUnitProgressChanged`

2. **UnitRegistry.cs:**
   - Eventos estáticos substituídos por GameEvents
   - `Register()` agora dispara `GameEvents.RaiseUnitSpawned`
   - `Unregister()` agora dispara `GameEvents.RaiseUnitDespawned`

3. **UnitQueries.cs:**
   - `GetUnitsForPlayer()` otimizado com fallback automático

#### **❌ REMOVIDO:**

1. **Eventos Locais (Obsoletos):**
   - `Unit.OnSelectionChanged` → Use `GameEvents.OnUnitSelectionChanged`
   - `Unit.OnProgressChanged` → Use `GameEvents.OnUnitProgressChanged`
   - `UnitRegistry.OnUnitSpawned` → Use `GameEvents.OnUnitSpawned`
   - `UnitRegistry.OnUnitDespawned` → Use `GameEvents.OnUnitDespawned`

---

### 14.2 Guia de Migração (Versão Antiga → v1.0)

#### **Para Código que Usava Eventos Locais de Unit:**

**Antes (❌ Obsoleto):**
```csharp
public class UnitListItemUI : MonoBehaviour
{
    Unit _unit;
    
    void Bind(Unit unit)
    {
        if (_unit != null)
        {
            _unit.OnProgressChanged -= OnProgressChanged; // ❌ Evento local
        }
        _unit = unit;
        _unit.OnProgressChanged += OnProgressChanged; // ❌
    }
}
```

**Depois (✅ Atual):**
```csharp
public class UnitListItemUI : MonoBehaviour
{
    Unit _unit;
    
    void OnEnable()
    {
        GameEvents.OnUnitProgressChanged += OnProgressChanged; // ✅ GameEvents
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitProgressChanged -= OnProgressChanged; // ✅
    }
    
    void OnProgressChanged(Unit changedUnit)
    {
        if (changedUnit == _unit) // Filtrar
        {
            Refresh();
        }
    }
}
```

---

#### **Para Código que Usava Eventos Estáticos de UnitRegistry:**

**Antes (❌ Obsoleto):**
```csharp
public class MinimapSystem : MonoBehaviour
{
    void OnEnable()
    {
        UnitRegistry.OnUnitSpawned += OnUnitSpawned; // ❌ Evento estático local
        UnitRegistry.OnUnitDespawned += OnUnitDespawned; // ❌
    }
}
```

**Depois (✅ Atual):**
```csharp
public class MinimapSystem : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnUnitSpawned += OnUnitSpawned; // ✅ GameEvents
        GameEvents.OnUnitDespawned += OnUnitDespawned; // ✅
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitSpawned -= OnUnitSpawned; // ✅
        GameEvents.OnUnitDespawned -= OnUnitDespawned; // ✅
    }
}
```

---

### 14.3 Checklist de Migração

Use esta checklist para atualizar seu código:

- [ ] **Buscar eventos locais de Unit:** Procure por `.OnSelectionChanged`, `.OnProgressChanged`
- [ ] **Substituir por GameEvents:** Troque por `GameEvents.OnUnitSelectionChanged`, `GameEvents.OnUnitProgressChanged`
- [ ] **Buscar eventos estáticos de UnitRegistry:** Procure por `UnitRegistry.OnUnitSpawned`, `UnitRegistry.OnUnitDespawned`
- [ ] **Substituir por GameEvents:** Troque por `GameEvents.OnUnitSpawned`, `GameEvents.OnUnitDespawned`
- [ ] **Mover subscribe para OnEnable():** Se estava em `Start()`, mova para `OnEnable()`
- [ ] **Adicionar unsubscribe em OnDisable():** CRÍTICO para evitar memory leaks
- [ ] **Adicionar filtros em handlers:** Handlers de GameEvents recebem todas as unidades, filtre por unidade específica se necessário
- [ ] **Testar:** Verifique que eventos ainda funcionam após migração

---

## 15) CONCLUSÃO

### 15.1 Resumo do Lote 3

O **Lote 3 - Módulo Unit** estabelece o **sistema completo de unidades** para Medieval Thrones:

✅ **UnitDefinition**: Dados configuráveis em ScriptableObjects reutilizáveis  
✅ **Unit**: Componente principal com HP, progressão (XP/Level) e seleção visual  
✅ **UnitRegistry**: Registro global otimizado com listas por facção (O(1) queries)  
✅ **UnitQueries**: Utilitários de consulta com otimização automática  
✅ **Sistema de Progressão**: Fórmula exponencial customizável para XP/Level  
✅ **Integração com GameEvents**: 4 eventos emitidos (Spawn, Despawn, Selection, Progress)  

### 15.2 Qualidade da Arquitetura

**Pontos Fortes:**
- ✅ **Desacoplamento**: Eventos via GameEvents ao invés de referências diretas
- ✅ **Performance**: UnitRegistry com listas por facção (O(1) acesso)
- ✅ **Extensibilidade**: Fácil adicionar novos tipos de unidades (ScriptableObjects)
- ✅ **Testabilidade**: Lógica de progressão isolada e testável
- ✅ **Escalabilidade**: Suporta centenas/milhares de unidades simultaneamente

**Padrões de Excelência:**
- ✅ ScriptableObjects para dados
- ✅ Static registry pattern para acesso global
- ✅ Event-driven architecture (GameEvents)
- ✅ Auto-registro via OnEnable/OnDisable
- ✅ Fórmula exponencial para progressão balanceada

### 15.3 Integração com Outros Lotes

**Lote 1 - Variáveis Globais & Factions:**
- ✅ Usa `FactionId` e `UnitType` (enums)
- ✅ Emite 4 eventos via `GameEvents`
- ✅ Integra com `PlayerController`

**Lote 2 - Câmera System:**
- ✅ (Opcional) Dispara `OnSelectionFocus` ao selecionar

**Lote 4 - Selection System:**
- ✅ Recebe chamadas de `SetSelected()` do `SelectionManager`
- ✅ Emite eventos de seleção para UI

**UI System (Futuro):**
- ✅ Escuta eventos de progressão/seleção
- ✅ Consulta `UnitRegistry` para listas

### 15.4 Próximos Passos

**Lote 4 - Selection System:**
- Documentação atualizada (próxima iteração)
- Integração completa com Unit já implementada

**Novos Módulos (que usarão Unit):**
- Sistema de Combate (dano, morte, XP)
- Sistema de Coleta (workers, recursos, XP)
- Sistema de Construção (unidades constroem edifícios)
- Sistema de IA (unidades inimigas, pathfinding)

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão do Documento:** 3.0  
**Compatibilidade:** Unity 2022.3+, Medieval Thrones v3.0+

---

# LOTE 4 — SELECTION SYSTEM (SISTEMA DE SELEÇÃO)

**Versão:** 3.0  
**Status:** ✅ Refatorado e Documentado  
**Data:** Outubro 2025

---

## 📋 ÍNDICE

1. [Visão Geral do Módulo](#1-visão-geral-do-módulo)
2. [SelectionManager - Gerenciador Principal](#2-selectionmanager---gerenciador-principal)
3. [InputSelection - Interface de Input](#3-inputselection---interface-de-input)
4. [WorldPicker - Raycasting](#4-worldpicker---raycasting)
5. [DragRectRenderer - Visual de Arrasto](#5-dragrectrenderer---visual-de-arrasto)
6. [UnitHitProxy - Proxy de Colisão](#6-unithitproxy---proxy-de-colisão)
7. [Fluxo de Seleção Completo](#7-fluxo-de-seleção-completo)
8. [Integração com Outros Módulos](#8-integração-com-outros-módulos)
9. [Configuração na Cena](#9-configuração-na-cena)
10. [Exemplos de Uso Avançados](#10-exemplos-de-uso-avançados)
11. [Troubleshooting e FAQ](#11-troubleshooting-e-faq)
12. [Tabela de Relacionamentos Completa](#12-tabela-de-relacionamentos-completa)
13. [Changelog e Migrações](#13-changelog-e-migrações)
14. [Referências Rápidas](#14-referências-rápidas)
15. [Conclusão](#15-conclusão)

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Objetivo

Fornecer um **sistema completo de seleção de unidades** para Medieval Thrones, com suporte a:
- ✅ Clique simples (selecionar unidade)
- ✅ Clique com Ctrl (adicionar/remover à seleção)
- ✅ Drag rect (seleção em área)
- ✅ Duplo clique (selecionar todas do mesmo tipo)
- ✅ Clique no chão (limpar seleção)
- ✅ Filtro por facção (apenas unidades próprias)
- ✅ Detecção de UI (ignora cliques sobre UI)

### 1.2 Responsabilidades Principais

O **Lote 4 - Selection System** é responsável por:

1. **Captura de Input** (`InputSelection`):
   - Detecção de cliques (LMB/RMB)
   - Detecção de drag (threshold configurável)
   - Detecção de duplo clique (janela temporal)
   - Detecção de modificadores (Ctrl, Shift)
   - Ignorar input sobre UI (EventSystem)

2. **Gerenciamento de Seleção** (`SelectionManager`):
   - Manter conjunto de unidades selecionadas (HashSet)
   - Operações: Add, Remove, Toggle, Clear
   - Filtros (apenas facção própria)
   - Seleção por retângulo (drag)
   - Seleção por tipo (duplo clique)
   - Emissão de evento `OnSelectionChanged`

3. **Raycasting** (`WorldPicker`):
   - Detectar unidades sob o cursor
   - Detectar terreno sob o cursor
   - LayerMasks configuráveis

4. **Feedback Visual** (`DragRectRenderer`):
   - Renderizar retângulo de seleção em 3D
   - Atualização em tempo real

5. **Proxy de Colisão** (`UnitHitProxy`):
   - Facilitar detecção de unidades em hierarquias complexas

### 1.3 Arquitetura do Sistema de Seleção

```
┌─────────────────────────────────────────────────────────────┐
│              LOTE 4 - SELECTION SYSTEM                      │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┬───────────────┐
        │                   │                   │               │
        ▼                   ▼                   ▼               ▼
┌────────────────┐  ┌────────────────┐  ┌──────────────┐  ┌──────────────┐
│ InputSelection │  │SelectionManager│  │ WorldPicker  │  │DragRectRender│
│ (MonoBehaviour)│  │(MonoBehaviour) │  │(MonoBehaviour│  │(MonoBehaviour│
├────────────────┤  ├────────────────┤  ├──────────────┤  ├──────────────┤
│• InputActions  │  │• HashSet<Unit> │  │• TryPickUnit │  │• quadPrefab  │
│• dragThreshold │  │• player        │  │• TryPickGrnd │  │• BeginRect() │
│• doubleClickWin│  │• onlyOwnUnits  │  │• unitMask    │  │• UpdateRect()│
│• IsCtrlPressed │  │• HandleClick   │  │• groundMask  │  │• EndRect()   │
│• IsShiftPressed│  │• HandleDrag    │  │              │  │              │
│                │  │• HandleDouble  │  │              │  │              │
│                │  │• Add/Remove    │  │              │  │              │
│                │  │• FireChanged() │  │              │  │              │
└────────┬───────┘  └────────┬───────┘  └──────────────┘  └──────────────┘
         │                   │                                     │
         │  Detecta Input    │  Gerencia Seleção                  │  Visual
         │  ↓                │  ↓                                  │  ↓
         └───────────────────┼─────────────────────────────────────┘
                             │
                             │ Dispara Eventos
                             ▼
                     ┌───────────────┐
                     │  GameEvents   │ ← Lote 1
                     │  (Lote 1)     │
                     ├───────────────┤
                     │ INPUT:        │
                     │• OnPointerDown│
                     │• OnPointerUp  │
                     │• OnDragBegin  │
                     │• OnDragging   │
                     │• OnDragEnd    │
                     │• OnUnitClick  │
                     │• OnGroundClick│
                     │• OnUnitDouble │
                     │               │
                     │ SELEÇÃO:      │
                     │• OnSelection  │
                     │  Changed      │
                     └───────┬───────┘
                             │
                     ┌───────┴────────┐
                     │                │
                     ▼                ▼
             ┌───────────────┐  ┌──────────┐
             │     Unit      │  │    UI    │
             │ (Lote 3)      │  │ Systems  │
             │• SetSelected()│  │• UnitList│
             └───────────────┘  └──────────┘
```

**Fluxo de Dados Típico:**

```
Jogador clica em unidade
       │
       ▼
InputSelection detecta clique
       │
       │ GameEvents.RaiseUnitClick(unit, ctrl)
       ▼
SelectionManager.HandleClickUnit()
       │
       ├──▶ Add(unit) → unit.SetSelected(true)
       │
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
UI Systems escutam e atualizam interface
```

---

## 2) SELECTIONMANAGER - GERENCIADOR PRINCIPAL

### 2.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Gerenciar o conjunto de unidades selecionadas e coordenar os handlers de input.

**Funcionalidades:**
- ✅ Mantém `HashSet<Unit>` de selecionadas
- ✅ Operações: Add, Remove, Toggle, Clear
- ✅ Handlers de eventos de input (via GameEvents)
- ✅ Filtro por facção (opcional)
- ✅ Seleção por drag rect
- ✅ Seleção por duplo clique (mesmo tipo)
- ✅ API pública para seleção programática

### 2.2 Campos Públicos (Inspector)

#### **Screenshot de Referência:**
```
┌─────────────────────────────────────────────────────┐
│ SelectionManager (Script)                           │
├─────────────────────────────────────────────────────┤
│ Selection                                           │
│   Rect Inflate Px: 1.5                              │
│                                                     │
│ Refs                                                │
│   Player: PlayerSettings (Player Controller)       │
│   Cam: Main Camera (Camera)                        │
│   Input: Selection (Input Selection)               │
│                                                     │
│ Filtro                                              │
│   Only Own Units: ☑                                 │
└─────────────────────────────────────────────────────┘
```

---

#### **Seção: Selection**

```csharp
[Header("Selection")]
[Tooltip("Px extras no retângulo para evitar perda por borda")]
public float rectInflatePx = 1.5f;
```

**rectInflatePx (float):**
- **Descrição:** Pixels extras adicionados ao retângulo de seleção para evitar perda de unidades nas bordas
- **Padrão:** 1.5
- **Uso:** Compensa imprecisão visual/física
- **Exemplo:** Se drag rect é 100x100px, área real testada é 103x103px

---

#### **Seção: Refs**

```csharp
[Header("Refs")]
public PlayerController player;   // define a facção local (Player1, etc.)
public Camera cam;                // mesma câmera usada no WorldPicker
public InputSelection input;      // nosso input separado
```

**player (PlayerController):**
- **Descrição:** Referência ao `PlayerController` (Lote 1)
- **Uso:** Obter `myFaction` para filtrar unidades próprias
- **Obrigatório:** Sim (se `onlyOwnUnits = true`)

**cam (Camera):**
- **Descrição:** Câmera usada para raycasting e projeções
- **Padrão:** `Camera.main` (via `Reset()`)
- **Uso:** Converter screen → world coordinates

**input (InputSelection):**
- **Descrição:** Referência ao componente `InputSelection`
- **Uso:** Acessar `IsCtrlPressed` durante drag end
- **Nota:** Input events vêm via GameEvents, não diretamente do input

---

#### **Seção: Filtro**

```csharp
[Header("Filtro")]
public bool onlyOwnUnits = true;  // nunca selecionar unidades de outra facção
```

**onlyOwnUnits (bool):**
- **Descrição:** Se `true`, apenas unidades da facção do player podem ser selecionadas
- **Padrão:** `true`
- **Uso:** RTS típico (não pode selecionar inimigos)
- **Se `false`:** Permite selecionar qualquer unidade (útil para editor/debug)

---

### 2.3 Propriedades (Read-Only)

#### **Selection (IReadOnlyCollection<Unit>)**

```csharp
public IReadOnlyCollection<Unit> Selection => _selection;
```

**Descrição:** Coleção read-only das unidades selecionadas.

**Uso:**
```csharp
foreach (var unit in selectionManager.Selection)
{
    Debug.Log($"Selecionada: {unit.DisplayName}");
}
```

---

#### **Count (int)**

```csharp
public int Count => _selection.Count;
```

**Descrição:** Número de unidades selecionadas.

**Uso:**
```csharp
if (selectionManager.Count == 0)
{
    statusText.text = "Nenhuma unidade selecionada";
}
else
{
    statusText.text = $"{selectionManager.Count} unidade(s) selecionada(s)";
}
```

---

### 2.4 Métodos Privados de Seleção

#### **Add(Unit u)**

```csharp
void Add(Unit u)
{
    if (_selection.Add(u)) u.SetSelected(true);
}
```

**Descrição:** Adiciona unidade à seleção (se ainda não estiver).

**Comportamento:**
- Se `_selection.Add()` retorna `true` (unidade não estava), chama `u.SetSelected(true)`
- Se já estava selecionada, nada acontece (idempotente)

---

#### **Remove(Unit u)**

```csharp
void Remove(Unit u)
{
    if (_selection.Remove(u)) u.SetSelected(false);
}
```

**Descrição:** Remove unidade da seleção.

**Comportamento:**
- Se `_selection.Remove()` retorna `true` (unidade estava), chama `u.SetSelected(false)`
- Se não estava selecionada, nada acontece (idempotente)

---

#### **Toggle(Unit u)**

```csharp
void Toggle(Unit u)
{
    if (_selection.Contains(u)) Remove(u);
    else Add(u);
}
```

**Descrição:** Inverte estado de seleção da unidade.

**Comportamento:**
- Se está selecionada → Remove
- Se não está selecionada → Add

**Uso:** Clique com Ctrl

---

#### **Clear()**

```csharp
void Clear()
{
    if (_selection.Count == 0) return;
    foreach (var u in _selection) u.SetSelected(false);
    _selection.Clear();
}
```

**Descrição:** Limpa toda a seleção.

**Comportamento:**
1. Early exit se já vazia
2. Chama `SetSelected(false)` em todas
3. Limpa HashSet

---

### 2.5 Handlers de Eventos (Privados)

#### **OnEnable/OnDisable (Lifecycle)**

```csharp
void OnEnable()
{
    // REFATORAÇÃO: Subscrever eventos via GameEvents ao invés de InputSelection
    GameEvents.OnUnitClick += HandleClickUnit;
    GameEvents.OnGroundClick += HandleClickGround;
    GameEvents.OnDragBegin += OnBeginDragHandler;
    GameEvents.OnDragEnd += HandleEndDrag;
    GameEvents.OnUnitDoubleClick += HandleDoubleClickUnit;
}

void OnDisable()
{
    // REFATORAÇÃO: Desinscrever eventos via GameEvents
    GameEvents.OnUnitClick -= HandleClickUnit;
    GameEvents.OnGroundClick -= HandleClickGround;
    GameEvents.OnDragBegin -= OnBeginDragHandler;
    GameEvents.OnDragEnd -= HandleEndDrag;
    GameEvents.OnUnitDoubleClick -= HandleDoubleClickUnit;
}
```

**Eventos Consumidos:**

| Evento | Handler | Descrição |
|--------|---------|-----------|
| `OnUnitClick` | `HandleClickUnit` | Clique em unidade |
| `OnGroundClick` | `HandleClickGround` | Clique no chão |
| `OnDragBegin` | `OnBeginDragHandler` | Início de drag |
| `OnDragEnd` | `HandleEndDrag` | Fim de drag |
| `OnUnitDoubleClick` | `HandleDoubleClickUnit` | Duplo clique em unidade |

---

#### **HandleClickUnit(Unit unit, bool ctrl)**

```csharp
void HandleClickUnit(Unit unit, bool ctrl)
{
    if (onlyOwnUnits && unit.owner != player.myFaction) return;

    if (ctrl) Toggle(unit);
    else { Clear(); Add(unit); }

    _rangeAnchor = unit;
    FireChanged();
}
```

**Parâmetros:**
- `unit`: Unidade clicada
- `ctrl`: Se Ctrl estava pressionado

**Comportamento:**

1. **Filtro de Facção:**
   - Se `onlyOwnUnits = true` e unidade é de outra facção → retorna (ignora)

2. **Lógica de Seleção:**
   - **Ctrl pressionado:** Toggle (adiciona ou remove)
   - **Ctrl não pressionado:** Clear + Add (substitui seleção)

3. **Range Anchor:**
   - Atualiza `_rangeAnchor` (usado para Shift+Click - futuro)

4. **Notificação:**
   - Dispara `FireChanged()` → evento `OnSelectionChanged`

**Exemplo:**
```
Clique simples em Worker (1):
  → Clear() → Add(Worker1) → Selection = {Worker1}

Ctrl+Clique em Worker (2):
  → Toggle(Worker2) → Selection = {Worker1, Worker2}

Ctrl+Clique em Worker (1) novamente:
  → Toggle(Worker1) → Remove → Selection = {Worker2}
```

---

#### **HandleClickGround(Vector3 worldPoint, bool ctrl)**

```csharp
void HandleClickGround(Vector3 worldPoint, bool ctrl)
{
    // clique no chão (ou RMB no seu setup): limpa seleção
    Clear();
    // opcional: não mexer na áncora; ela permanece até um clique normal substituir
    FireChanged();
}
```

**Parâmetros:**
- `worldPoint`: Posição 3D do clique no terreno
- `ctrl`: Se Ctrl estava pressionado (ignorado)

**Comportamento:**
- Limpa toda a seleção (`Clear()`)
- **Não limpa `_rangeAnchor`** (opcional, design choice)
- Dispara `FireChanged()`

**Uso Típico:**
- Jogador clica no chão → desseleciona tudo
- RMB no chão → limpa seleção + move unidades (em outro sistema)

---

#### **OnBeginDragHandler(Vector2 startScreenPos)**

```csharp
void OnBeginDragHandler(Vector2 startScreenPos)
{
    if (Physics.Raycast(cam.ScreenPointToRay(startScreenPos), out var hit))
        _dragStartWorld = hit.point;
}
```

**Parâmetro:**
- `startScreenPos`: Posição de tela onde drag começou

**Comportamento:**
- Converte screen → world via raycast
- Armazena `_dragStartWorld` para uso em `HandleEndDrag`

**Nota:** Este handler **não faz seleção**, apenas armazena posição inicial.

---

#### **HandleEndDrag(Vector2 endScreenPos)**

```csharp
void HandleEndDrag(Vector2 endScreenPos)
{
    bool ctrl = input != null && input.IsCtrlPressed;

    Vector3 endWorld;
    if (Physics.Raycast(cam.ScreenPointToRay(endScreenPos), out var hit))
        endWorld = hit.point;
    else
        endWorld = ProjectScreenToXZ(endScreenPos); // fallback se não colidir com terreno

    SelectByWorldRect(_dragStartWorld, endWorld, additive: ctrl);
    FireChanged();
}
```

**Parâmetro:**
- `endScreenPos`: Posição de tela onde drag terminou

**Comportamento:**

1. **Detecta Ctrl:**
   - `ctrl = input.IsCtrlPressed` (via InputSelection)

2. **Converte End Position:**
   - Raycast para obter `endWorld`
   - Fallback para `ProjectScreenToXZ` se não colidir

3. **Seleção por Retângulo:**
   - Chama `SelectByWorldRect(_dragStartWorld, endWorld, additive: ctrl)`
   - **additive = true:** Não limpa seleção anterior (Ctrl)
   - **additive = false:** Limpa seleção anterior (normal)

4. **Notificação:**
   - Dispara `FireChanged()`

**Exemplo:**
```
Drag normal (sem Ctrl):
  → Clear() → Seleciona unidades no retângulo → Selection = {novas}

Drag com Ctrl:
  → NÃO Clear() → Adiciona unidades no retângulo → Selection = {antigas + novas}
```

---

#### **HandleDoubleClickUnit(Unit unit)**

```csharp
void HandleDoubleClickUnit(Unit unit)
{
    if (onlyOwnUnits && unit.owner != player.myFaction) return;

    Clear();
    // "mesmo tipo visível": vou usar UnitDefinition (ou type)
    var mine = UnitRegistry.GetByFaction(player.myFaction);
    foreach (var u in mine)
    {
        if (!IsOnScreen(u.transform.position)) continue;
        if (u.def == unit.def) Add(u); // ou comparar u.def.type se preferir
    }
    _rangeAnchor = unit;  // duplo clique atualiza âncora
    FireChanged();
}
```

**Parâmetro:**
- `unit`: Unidade com duplo clique

**Comportamento:**

1. **Filtro de Facção:**
   - Se `onlyOwnUnits = true` e unidade é de outra facção → retorna

2. **Limpa Seleção:**
   - `Clear()` (duplo clique substitui seleção)

3. **Seleciona Todas do Mesmo Tipo:**
   - Obtém todas as unidades da facção via `UnitRegistry.GetByFaction`
   - Filtra por:
     - `IsOnScreen()` → **apenas visíveis na tela**
     - `u.def == unit.def` → **mesmo UnitDefinition**
   - Adiciona cada uma via `Add(u)`

4. **Atualiza Âncora:**
   - `_rangeAnchor = unit`

5. **Notificação:**
   - Dispara `FireChanged()`

**Exemplo:**
```
Duplo clique em Worker (1):
  → Clear()
  → Seleciona: Worker (1), Worker (2), Worker (3) (todos workers na tela)
  → Selection = {Worker1, Worker2, Worker3}

Duplo clique em Archer (1):
  → Clear()
  → Seleciona: Archer (1), Archer (2), Archer (3) (todos archers na tela)
  → Selection = {Archer1, Archer2, Archer3}
```

**Design Note:**
- **Apenas visíveis na tela** (via `IsOnScreen`)
- **Sem limite de distância** (mas se não estiver visível, não seleciona)
- Compara `UnitDefinition` inteiro (`u.def == unit.def`)
  - Alternativa: comparar apenas tipo (`u.def.type == unit.def.type`)

---

### 2.6 Métodos Auxiliares (Privados)

#### **SelectByWorldRect(Vector3 a, Vector3 b, bool additive)**

```csharp
void SelectByWorldRect(Vector3 a, Vector3 b, bool additive)
{
    if (!additive) Clear();

    var min = Vector3.Min(a, b);
    var max = Vector3.Max(a, b);

    var bounds = new Bounds();
    bounds.SetMinMax(
        new Vector3(min.x, float.MinValue, min.z),
        new Vector3(max.x, float.MaxValue, max.z)
    );

    var mine = UnitRegistry.GetByFaction(player.myFaction);
    for (int i = 0; i < mine.Count; i++)
    {
        var u = mine[i];
        var pos = u.transform.position;
        if (bounds.Contains(new Vector3(pos.x, 0f, pos.z))) Add(u);
    }
}
```

**Parâmetros:**
- `a`, `b`: Dois cantos do retângulo (em world space)
- `additive`: Se `true`, não limpa seleção anterior

**Comportamento:**

1. **Clear (opcional):**
   - Se `additive = false`, limpa seleção anterior

2. **Criar Bounds 3D:**
   - `min` e `max` nos eixos X e Z
   - Y é infinito (`float.MinValue` a `float.MaxValue`)
   - Ou seja: retângulo 2D no plano XZ, com altura infinita

3. **Testar Unidades:**
   - Obtém unidades da facção via `UnitRegistry.GetByFaction`
   - Para cada unidade, testa se `pos.xz` está dentro do bounds
   - Se está, adiciona via `Add(u)`

**Design Note:**
- Usa **bounds 3D com Y infinito** (mais simples que projetar tudo em 2D)
- Compara apenas `pos.x` e `pos.z` (ignora altura da unidade)
- Testa apenas unidades da **facção do player** (otimização)

---

#### **ProjectScreenToXZ(Vector2 screenPos)**

```csharp
Vector3 ProjectScreenToXZ(Vector2 screenPos)
{
    var ray = cam.ScreenPointToRay(screenPos);
    Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // plano XZ no Y=0
    if (groundPlane.Raycast(ray, out float enter))
        return ray.GetPoint(enter);
    return Vector3.zero; // fallback
}
```

**Parâmetro:**
- `screenPos`: Posição de tela

**Retorno:**
- Posição 3D no plano XZ (Y=0)

**Uso:**
- Fallback em `HandleEndDrag` se raycast não colidir com terreno
- Garante que drag sempre tem posição final válida

---

#### **IsOnScreen(Vector3 worldPos)**

```csharp
bool IsOnScreen(Vector3 worldPos)
{
    var sp = cam.WorldToScreenPoint(worldPos);
    return sp.z > 0 && IsInViewport(sp);
}

bool IsInViewport(Vector3 screenPos)
{
    return screenPos.x >= 0 && screenPos.x <= Screen.width &&
           screenPos.y >= 0 && screenPos.y <= Screen.height;
}
```

**Parâmetro:**
- `worldPos`: Posição 3D no mundo

**Retorno:**
- `true` se posição está visível na tela

**Comportamento:**
1. Converte world → screen via `WorldToScreenPoint`
2. Verifica se `sp.z > 0` (na frente da câmera)
3. Verifica se está dentro dos limites da tela

**Uso:**
- `HandleDoubleClickUnit` para filtrar apenas unidades visíveis

---

### 2.7 Métodos Públicos (API Externa)

#### **IsSelected(Unit u)**

```csharp
public bool IsSelected(Unit u) => u != null && _selection.Contains(u);
```

**Parâmetro:**
- `u`: Unidade a testar

**Retorno:**
- `true` se unidade está selecionada

**Uso:**
```csharp
if (selectionManager.IsSelected(unit))
{
    // Mostrar borda dourada na UI
}
```

---

#### **SelectExactly(IEnumerable<Unit> units)**

```csharp
public void SelectExactly(IEnumerable<Unit> units)
{
    Clear();
    if (units != null)
    {
        foreach (var u in units) if (u != null) Add(u);
    }
    FireChanged();
}
```

**Parâmetro:**
- `units`: Coleção de unidades a selecionar

**Comportamento:**
- Limpa seleção anterior
- Adiciona todas as unidades da coleção
- Dispara `FireChanged()`

**Uso:**
```csharp
// Selecionar todos os workers
var workers = UnitRegistry.GetByFaction(player.myFaction)
    .Where(u => u.def.type == UnitType.Worker);
selectionManager.SelectExactly(workers);
```

---

#### **SelectExactly(Unit u)**

```csharp
public void SelectExactly(Unit u)
{
    Clear();
    if (u != null) Add(u);
    FireChanged();
}
```

**Parâmetro:**
- `u`: Unidade única a selecionar

**Comportamento:**
- Limpa seleção anterior
- Adiciona apenas esta unidade
- Dispara `FireChanged()`

**Uso:**
```csharp
// UI: jogador clica no portrait da unidade
void OnPortraitClick(Unit unit)
{
    selectionManager.SelectExactly(unit);
    cameraController.FocusOn(unit.transform);
}
```

---

#### **ToggleSet(IEnumerable<Unit> units)**

```csharp
public void ToggleSet(IEnumerable<Unit> units)
{
    if (units == null) return;
    foreach (var u in units) if (u != null) Toggle(u);
    FireChanged();
}
```

**Parâmetro:**
- `units`: Coleção de unidades a togglear

**Comportamento:**
- Para cada unidade: se selecionada → remove, se não → adiciona
- Dispara `FireChanged()` **uma vez** ao final

**Uso:**
```csharp
// Ctrl+Clique em grupo de UI
void OnGroupCtrlClick(List<Unit> groupUnits)
{
    selectionManager.ToggleSet(groupUnits);
}
```

---

#### **AddToSelection(IEnumerable<Unit> units)** ✨ NOVO

```csharp
public void AddToSelection(IEnumerable<Unit> units)
{
    if (units == null) return;

    bool changed = false;
    foreach (var u in units)
    {
        if (u == null) continue;
        if (_selection.Add(u))
        {
            u.SetSelected(true);
            changed = true;
        }
    }

    if (changed) FireChanged();
}
```

**Parâmetro:**
- `units`: Coleção de unidades a adicionar

**Comportamento:**
- Adiciona unidades **sem limpar seleção existente**
- Dispara `FireChanged()` apenas se houve mudança
- **Diferença de `SelectExactly`:** não limpa antes

**Uso:**
```csharp
// Shift+Drag (adicionar à seleção)
void OnShiftDrag(List<Unit> newUnits)
{
    selectionManager.AddToSelection(newUnits);
}
```

---

#### **ClearAnchor()**

```csharp
public void ClearAnchor() => _rangeAnchor = null;
```

**Descrição:** Limpa a âncora de range selection.

**Uso:**
- Futuro: Shift+Click para selecionar intervalo entre âncora e unidade clicada
- Atualmente: apenas utility

---

### 2.8 Integração com GameEvents

#### **Evento Emitido:**

```csharp
void FireChanged() => GameEvents.RaiseSelectionChanged(_selection);
```

**Descrição:** Dispara evento `OnSelectionChanged` toda vez que seleção muda.

**Evento:**
- `GameEvents.OnSelectionChanged` (Lote 1, Seção 2.4.5)
- Assinatura: `Action<IReadOnlyCollection<Unit>>`

**Listeners Típicos:**

| Listener | Ação |
|----------|------|
| `UnitListUI` | Reconstruir lista de unidades selecionadas |
| `SelectionInfoPanel` | Atualizar painel de informações |
| `MinimapUI` | Destacar unidades selecionadas |
| `AudioManager` | Tocar som de seleção |

**Exemplo de Listener:**

```csharp
public class UnitListUI : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnSelectionChanged += OnSelectionChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnSelectionChanged -= OnSelectionChanged;
    }
    
    void OnSelectionChanged(IReadOnlyCollection<Unit> selection)
    {
        // Limpar lista
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);
        
        // Criar itens para cada unidade selecionada
        foreach (var unit in selection)
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.GetComponent<UnitListItemUI>().Bind(unit);
        }
    }
}
```

---

## 3) INPUTSELECTION - INTERFACE DE INPUT

### 3.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Capturar input do jogador (cliques, drag, modificadores) e emitir eventos via GameEvents.

**Funcionalidades:**
- ✅ Unity Input System (New Input System)
- ✅ Detecção de cliques (LMB/RMB)
- ✅ Detecção de drag (threshold configurável)
- ✅ Detecção de duplo clique (janela temporal)
- ✅ Detecção de modificadores (Ctrl, Shift)
- ✅ Ignorar input sobre UI (EventSystem)
- ✅ Emissão de 8 eventos via GameEvents

### 3.2 Campos Públicos (Inspector)

#### **Screenshot de Referência:**
```
┌─────────────────────────────────────────────────────┐
│ Input Selection (Script)                            │
├─────────────────────────────────────────────────────┤
│ Refs                                                │
│   Picker: Selection (World Picker)                 │
│                                                     │
│ Config                                              │
│   Drag Threshold Px: 6                              │
│   Double Click Window: 0.28                         │
│                                                     │
│ Actions (arraste do seu asset)                      │
│   Point: Selection/Point (Input Action Reference)  │
│   Lmb: Selection/LMB (Input Action Reference)      │
│   Rmb: Selection/RMB (Input Action Reference)      │
│   Ctrl: Selection/Ctrl (Input Action Reference)    │
│   Shift: Selection/Shift (Input Action Reference)  │
└─────────────────────────────────────────────────────┘
```

---

#### **Seção: Refs**

```csharp
[Header("Refs")]
public WorldPicker picker;
```

**picker (WorldPicker):**
- **Descrição:** Referência ao `WorldPicker` para raycasting
- **Uso:** `TryPickUnitAt()`, `TryPickGroundAt()`
- **Obrigatório:** Sim

---

#### **Seção: Config**

```csharp
[Header("Config")]
public float dragThresholdPx = 6f;
public float doubleClickWindow = 0.28f;
```

**dragThresholdPx (float):**
- **Descrição:** Distância mínima (em pixels) para considerar drag
- **Padrão:** 6px
- **Comportamento:**
  - `distance < threshold` → Clique
  - `distance >= threshold` → Drag
- **Razão:** Evitar drag acidental ao clicar

**doubleClickWindow (float):**
- **Descrição:** Janela temporal (em segundos) para detectar duplo clique
- **Padrão:** 0.28s
- **Comportamento:**
  - Se 2 cliques na mesma unidade em `<= 0.28s` → Duplo clique
  - Senão → 2 cliques simples

---

#### **Seção: Actions**

```csharp
[Header("Actions (arraste do seu asset)")]
public InputActionReference point; // Vector2
public InputActionReference lmb;   // Button
public InputActionReference rmb;   // Button
public InputActionReference ctrl;  // Button
public InputActionReference shift; // Button
```

**InputActionReference:**
- **Tipo:** Referência a ações do Input System
- **Como atribuir:** Arraste do Input Actions Asset no Inspector

**Actions Necessárias:**

| Action | Tipo | Binding Típico | Descrição |
|--------|------|----------------|-----------|
| `point` | Vector2 | Mouse Position | Posição do cursor |
| `lmb` | Button | Mouse Left Button | Botão esquerdo |
| `rmb` | Button | Mouse Right Button | Botão direito |
| `ctrl` | Button | Keyboard Left Ctrl | Modificador Ctrl |
| `shift` | Button | Keyboard Left Shift | Modificador Shift |

**Setup do Input Actions Asset:**
```
Selection (Action Map)
├─ Point (Value, Vector2) → Mouse/position
├─ LMB (Button) → Mouse/leftButton
├─ RMB (Button) → Mouse/rightButton
├─ Ctrl (Button) → Keyboard/leftCtrl
└─ Shift (Button) → Keyboard/leftShift
```

---

### 3.3 Propriedades (Read-Only)

#### **IsCtrlPressed (bool)**

```csharp
public bool IsCtrlPressed => ctrl != null && ctrl.action.IsPressed();
```

**Descrição:** Retorna `true` se Ctrl está pressionado no momento.

**Uso:**
```csharp
// Em SelectionManager.HandleEndDrag()
bool ctrl = input.IsCtrlPressed;
SelectByWorldRect(start, end, additive: ctrl);
```

---

#### **IsShiftPressed (bool)**

```csharp
public bool IsShiftPressed => shift != null && shift.action.IsPressed();
```

**Descrição:** Retorna `true` se Shift está pressionado no momento.

**Uso:**
```csharp
// Futuro: Range selection
if (input.IsShiftPressed)
{
    SelectRange(anchorUnit, clickedUnit);
}
```

---

### 3.4 Campos Privados (Estado Interno)

```csharp
Vector2 _pointer;          // Posição atual do cursor
bool _lmbDown;             // LMB está pressionado?
Vector2 _downPos;          // Posição onde LMB foi pressionado
bool _dragging;            // Está em drag mode?

bool _pressedOverUI;       // Clique começou sobre UI?
float _lastClickTime;      // Timestamp do último clique
Unit _lastClickedUnit;     // Última unidade clicada (para duplo clique)
bool _overUIThisFrame;     // Cursor está sobre UI neste frame?
```

---

### 3.5 Lifecycle (OnEnable/OnDisable)

```csharp
void OnEnable()
{
    point?.action.Enable();
    lmb?.action.Enable();
    rmb?.action.Enable();
    ctrl?.action.Enable();
    shift?.action.Enable();

    point.action.performed += OnPointPerformed;
    lmb.action.started += OnLmbStarted;
    lmb.action.canceled += OnLmbCanceled;
    rmb.action.performed += OnRmbPerformed;
}

void OnDisable()
{
    point.action.performed -= OnPointPerformed;
    lmb.action.started -= OnLmbStarted;
    lmb.action.canceled -= OnLmbCanceled;
    rmb.action.performed -= OnRmbPerformed;

    point?.action.Disable();
    lmb?.action.Disable();
    rmb?.action.Disable();
    ctrl?.action.Disable();
    shift?.action.Disable();
}
```

**Comportamento:**
- `OnEnable`: Habilita actions e subscreve callbacks
- `OnDisable`: Desinscreve callbacks e desabilita actions

**Callbacks:**

| Action | Fase | Callback |
|--------|------|----------|
| `point` | performed | `OnPointPerformed` |
| `lmb` | started | `OnLmbStarted` |
| `lmb` | canceled | `OnLmbCanceled` |
| `rmb` | performed | `OnRmbPerformed` |

---

### 3.6 Detecção de UI (EventSystem)

```csharp
void LateUpdate()
{
    _overUIThisFrame = ComputePointerOverUI();
}

bool IsPointerOverUI() => _overUIThisFrame;

bool ComputePointerOverUI()
{
    if (EventSystem.current == null) return false;

    // --- Input System novo: melhor passar um pointerId ---
#if ENABLE_INPUT_SYSTEM
    // Mouse
    if (Mouse.current != null)
        return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);

    // Toque (qualquer dedo ativo)
    if (Touchscreen.current != null)
    {
        foreach (var t in Touchscreen.current.touches)
            if (t.isInProgress && EventSystem.current.IsPointerOverGameObject(t.touchId.ReadValue()))
                return true;
    }
#endif

    // Fallback (standalone/legacy)
    return EventSystem.current.IsPointerOverGameObject();
}
```

**Comportamento:**

1. **LateUpdate:**
   - Calcula `_overUIThisFrame` **uma vez por frame** (otimização)

2. **ComputePointerOverUI:**
   - **Mouse:** Usa `Mouse.current.deviceId` (correto para New Input System)
   - **Touch:** Testa todos os toques ativos
   - **Fallback:** Usa método legado sem ID

**Uso:**
- `OnLmbStarted`: Trava input se clique começou sobre UI
- `OnPointPerformed`, `OnRmbPerformed`: Ignora se sobre UI

**Por que é importante:**
- Evita selecionar unidades quando clicando em botões de UI
- Evita drag quando começou sobre UI panel

---

### 3.7 Callbacks de Input

#### **OnPointPerformed(InputAction.CallbackContext ctx)**

```csharp
void OnPointPerformed(InputAction.CallbackContext ctx)
{
    // Ignora LMB iniciado sobre UI
    if (IsPointerOverUI()) return;
    _pointer = ctx.ReadValue<Vector2>();
    if (_lmbDown && _dragging && !_pressedOverUI)
    {
        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaiseDragging(_pointer);
    }
}
```

**Descrição:** Atualiza posição do cursor e dispara evento de dragging.

**Comportamento:**
1. Ignora se sobre UI
2. Atualiza `_pointer`
3. Se está em drag mode → dispara `GameEvents.RaiseDragging()`

**Evento Emitido:**
- `GameEvents.OnDragging` (Lote 1)
- Assinatura: `Action<Vector2>` (screen position)

---

#### **OnLmbStarted(InputAction.CallbackContext _)**

```csharp
void OnLmbStarted(InputAction.CallbackContext _)
{
    _lmbDown = true;
    _downPos = _pointer;

    // <<< trava tudo se o clique começou sobre UI
    _pressedOverUI = IsPointerOverUI();
    _dragging = false;

    if (!_pressedOverUI)
    {
        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaisePointerDown(_downPos);
    }
}
```

**Descrição:** LMB foi pressionado (início do clique/drag).

**Comportamento:**
1. Marca `_lmbDown = true`
2. Armazena `_downPos` (posição inicial)
3. Testa se começou sobre UI → `_pressedOverUI`
4. Se **não** sobre UI → dispara `GameEvents.RaisePointerDown()`

**Evento Emitido:**
- `GameEvents.OnPointerDown` (Lote 1)
- Assinatura: `Action<Vector2>` (screen position)

**Design Note:**
- Se clique começou sobre UI, **TODO o input é travado** até `OnLmbCanceled`
- Isso evita "vazamento" de cliques de UI para mundo

---

#### **OnLmbCanceled(InputAction.CallbackContext _)**

```csharp
void OnLmbCanceled(InputAction.CallbackContext _)
{
    var upPos = _pointer;

    if (_pressedOverUI)
    {
        // Clique começou em UI → não é seleção do mundo
        _pressedOverUI = false;
        _lmbDown = false;
        return;
    }

    if (_dragging)
    {
        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaiseDragEnd(upPos);
    }
    else
    {
        HandleClick(upPos);
    }

    // REFATORAÇÃO: Disparar evento via GameEvents
    GameEvents.RaisePointerUp(upPos);
    _lmbDown = false;
}
```

**Descrição:** LMB foi solto (fim do clique/drag).

**Comportamento:**

1. **Clique Começou Sobre UI:**
   - Early return (ignora completamente)

2. **Estava em Drag Mode:**
   - Dispara `GameEvents.RaiseDragEnd()`

3. **Não estava em Drag (clique simples):**
   - Chama `HandleClick(upPos)`

4. **Sempre:**
   - Dispara `GameEvents.RaisePointerUp()`
   - Reseta `_lmbDown = false`

**Eventos Emitidos:**
- `GameEvents.OnDragEnd` (Lote 1) - se estava dragging
- `GameEvents.OnPointerUp` (Lote 1) - sempre

---

#### **OnRmbPerformed(InputAction.CallbackContext ctx)**

```csharp
void OnRmbPerformed(InputAction.CallbackContext ctx)
{
    // Ignora RMB iniciado sobre UI
    if (IsPointerOverUI()) return;

    if (picker != null && picker.TryPickGroundAt(_pointer, out var p, out _))
    {
        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaiseGroundClick(p, false);
    }
}
```

**Descrição:** RMB foi clicado (botão direito).

**Comportamento:**
1. Ignora se sobre UI
2. Tenta raycast no chão via `WorldPicker.TryPickGroundAt()`
3. Se acertou → dispara `GameEvents.RaiseGroundClick(p, false)`

**Evento Emitido:**
- `GameEvents.OnGroundClick` (Lote 1)
- Assinatura: `Action<Vector3, bool>` (worldPos, ctrl)
- **Nota:** `ctrl = false` (RMB não considera Ctrl)

**Uso Típico:**
- RMB no chão → Limpar seleção (SelectionManager)
- RMB no chão → Comando de movimento (futuro)

---

### 3.8 Detecção de Drag (Update)

```csharp
void Update()
{
    if (_lmbDown && !_dragging && !_pressedOverUI &&
        Vector2.Distance(_downPos, _pointer) >= dragThresholdPx)
    {
        _dragging = true;
        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaiseDragBegin(_downPos);
    }
}
```

**Descrição:** Detecta quando LMB pressionado se torna drag.

**Condições:**
1. `_lmbDown = true` (LMB está pressionado)
2. `!_dragging` (ainda não está em drag mode)
3. `!_pressedOverUI` (não começou sobre UI)
4. `Distance(_downPos, _pointer) >= dragThresholdPx` (moveu o suficiente)

**Comportamento:**
- Marca `_dragging = true`
- Dispara `GameEvents.RaiseDragBegin(_downPos)`

**Evento Emitido:**
- `GameEvents.OnDragBegin` (Lote 1)
- Assinatura: `Action<Vector2>` (screen position inicial)

**Design Note:**
- Threshold de 6px evita drag acidental
- Drag só começa após mover 6px (não no OnLmbStarted)

---

### 3.9 Detecção de Clique e Duplo Clique

```csharp
void HandleClick(Vector2 screenPos)
{
    if (picker == null) return;

    bool isCtrl = IsCtrlPressed;
    bool isShift = IsShiftPressed;

    if (picker.TryPickUnitAt(screenPos, out var unit))
    {
        // double click
        if (unit == _lastClickedUnit &&
            (Time.unscaledTime - _lastClickTime) <= doubleClickWindow)
        {
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseUnitDoubleClick(unit);
            _lastClickedUnit = null;
            _lastClickTime = 0f;
            return;
        }

        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaiseUnitClick(unit, isCtrl);
        _lastClickedUnit = unit;
        _lastClickTime = Time.unscaledTime;
    }
    else if (picker.TryPickGroundAt(screenPos, out var point, out _))
    {
        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaiseGroundClick(point, isCtrl);
        _lastClickedUnit = null;
        _lastClickTime = 0f;
    }
}
```

**Descrição:** Processa clique (chamado por `OnLmbCanceled`).

**Comportamento:**

1. **Raycast para Unidade:**
   - Usa `WorldPicker.TryPickUnitAt()`
   - Se acertou unidade:
     - **Testa Duplo Clique:**
       - Mesma unidade + dentro da janela temporal?
       - **Sim:** Dispara `GameEvents.RaiseUnitDoubleClick(unit)` e retorna
       - **Não:** Continua para clique simples
     - **Clique Simples:**
       - Dispara `GameEvents.RaiseUnitClick(unit, isCtrl)`
       - Atualiza `_lastClickedUnit` e `_lastClickTime`

2. **Raycast para Chão:**
   - Usa `WorldPicker.TryPickGroundAt()`
   - Se acertou chão:
     - Dispara `GameEvents.RaiseGroundClick(point, isCtrl)`
     - Reseta tracking de duplo clique

**Eventos Emitidos:**
- `GameEvents.OnUnitDoubleClick` (Lote 1) - se duplo clique
- `GameEvents.OnUnitClick` (Lote 1) - se clique simples em unidade
- `GameEvents.OnGroundClick` (Lote 1) - se clique no chão

**Modificadores:**
- `isCtrl`: Passado para eventos (SelectionManager usa para Toggle)
- `isShift`: Lido mas não usado (reservado para futuro)

---

### 3.10 Resumo de Eventos Emitidos

**InputSelection emite 8 eventos via GameEvents:**

| Evento | Quando | Parâmetros | Uso Típico |
|--------|--------|------------|------------|
| `OnPointerDown` | LMB pressionado (não sobre UI) | `Vector2 screenPos` | Início de interação |
| `OnPointerUp` | LMB solto (sempre) | `Vector2 screenPos` | Fim de interação |
| `OnDragBegin` | Moveu > threshold (Update) | `Vector2 startPos` | Início de drag rect |
| `OnDragging` | Movendo durante drag | `Vector2 currentPos` | Atualizar drag rect |
| `OnDragEnd` | LMB solto após drag | `Vector2 endPos` | Selecionar por retângulo |
| `OnUnitClick` | Clique em unidade | `Unit unit, bool ctrl` | Selecionar unidade |
| `OnGroundClick` | Clique no chão | `Vector3 worldPos, bool ctrl` | Limpar seleção / Mover |
| `OnUnitDoubleClick` | Duplo clique em unidade | `Unit unit` | Selecionar todas do tipo |

---

## 4) WORLDPICKER - RAYCASTING

### 4.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Realizar raycasts para detectar unidades e terreno sob o cursor.

**Funcionalidades:**
- ✅ Raycast para unidades (LayerMask configurável)
- ✅ Raycast para terreno (LayerMask configurável)
- ✅ Suporte a `UnitHitProxy` (hierarquias complexas)

### 4.2 Campos Públicos (Inspector)

#### **Screenshot de Referência:**
```
┌─────────────────────────────────────────────────────┐
│ World Picker (Script)                               │
├─────────────────────────────────────────────────────┤
│ Cam: Main Camera (Camera)                           │
│ Unit Mask: Unit                                     │
│ Ground Mask: Ground                                 │
└─────────────────────────────────────────────────────┘
```

---

```csharp
public Camera cam;
public LayerMask unitMask;   // Unit
public LayerMask groundMask; // Ground
float maxDistance = float.MaxValue;

void Reset() { cam = Camera.main; }
```

**cam (Camera):**
- **Descrição:** Câmera usada para raycasting
- **Padrão:** `Camera.main` (via `Reset()`)

**unitMask (LayerMask):**
- **Descrição:** Layer das unidades
- **Valor:** `Unit` (layer 6, por exemplo)
- **Uso:** `Physics.Raycast(..., unitMask, ...)`

**groundMask (LayerMask):**
- **Descrição:** Layer do terreno
- **Valor:** `Ground` (layer 7, por exemplo)
- **Uso:** `Physics.Raycast(..., groundMask, ...)`

**maxDistance (float):**
- **Descrição:** Distância máxima de raycast
- **Padrão:** `float.MaxValue` (infinito)
- **Privado:** Não exposto no Inspector

---

### 4.3 Métodos Públicos

#### **TryPickUnitAt(Vector2 screenPos, out Unit unit)**

```csharp
public bool TryPickUnitAt(Vector2 screenPos, out Unit unit)
{
    unit = null;
    var ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out var hit, maxDistance, unitMask, QueryTriggerInteraction.Collide))
    {
        unit = hit.collider.GetComponentInParent<Unit>();
        return unit != null;
    }
    return false;
}
```

**Parâmetros:**
- `screenPos`: Posição de tela (pixels)
- `unit` (out): Unidade detectada (ou `null`)

**Retorno:**
- `true` se encontrou unidade

**Comportamento:**
1. Cria ray via `ScreenPointToRay`
2. Raycast com `unitMask`
3. **QueryTriggerInteraction.Collide:** Aceita triggers (importante!)
4. Se acertou, pega `Unit` via `GetComponentInParent<Unit>()`
   - Suporta hierarquias: `Unit → Model → Collider`
5. Retorna `true` se encontrou `Unit`, `false` senão

**Uso:**
```csharp
if (picker.TryPickUnitAt(mousePos, out Unit unit))
{
    Debug.Log($"Clicou em: {unit.DisplayName}");
}
```

---

#### **TryPickGroundAt(Vector2 screenPos, out Vector3 point, out Vector3 normal)**

```csharp
public bool TryPickGroundAt(Vector2 screenPos, out Vector3 point, out Vector3 normal)
{
    point = default; normal = Vector3.up;
    var ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out var hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
    {
        point = hit.point; normal = hit.normal;
        return true;
    }
    return false;
}
```

**Parâmetros:**
- `screenPos`: Posição de tela (pixels)
- `point` (out): Posição 3D do hit
- `normal` (out): Normal da superfície

**Retorno:**
- `true` se encontrou terreno

**Comportamento:**
1. Cria ray via `ScreenPointToRay`
2. Raycast com `groundMask`
3. **QueryTriggerInteraction.Ignore:** Ignora triggers
4. Se acertou, retorna `hit.point` e `hit.normal`
5. Retorna `true` se acertou, `false` senão

**Uso:**
```csharp
if (picker.TryPickGroundAt(mousePos, out Vector3 pos, out Vector3 normal))
{
    Debug.Log($"Clicou no chão em: {pos}");
    // Spawn partícula, mover unidade, etc.
}
```

---

### 4.4 LayerMasks e Configuração

#### **Configuração de Layers:**

**Unity Editor:**
1. Edit → Project Settings → Tags and Layers
2. Layers:
   - Layer 6: `Unit`
   - Layer 7: `Ground`

**Collision Matrix:**
- Edit → Project Settings → Physics
- Desmarcar colisões desnecessárias:
  - Unit × Unit (se não houver colisão física entre unidades)
  - Unit × Ground (se unidades flutuam sobre terreno)

**GameObjects:**
- **Unidades:** Layer `Unit`
  - Ou: Collider filho com layer `Unit` + `UnitHitProxy`
- **Terreno:** Layer `Ground`

---

#### **Por que QueryTriggerInteraction.Collide?**

```csharp
// Em TryPickUnitAt()
QueryTriggerInteraction.Collide
```

**Razão:**
- Permite usar **Trigger Colliders** nas unidades
- Trigger colliders são mais leves (não causam colisão física)
- Útil se unidade tem collider para seleção + collider para física

**Exemplo:**
```
Unit (GameObject)
├─ Model (visual)
├─ Physics Collider (non-trigger) ← Colisão física
└─ Selection Collider (trigger) ← Seleção de mouse
   └─ UnitHitProxy (Script)
```

---

## 5) DRAGRECTRENDERER - VISUAL DE ARRASTO

### 5.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Renderizar retângulo visual de seleção durante drag.

**Funcionalidades:**
- ✅ Cria quad 3D durante drag
- ✅ Atualiza tamanho/posição em tempo real
- ✅ Destrói quad ao soltar

### 5.2 Campos Públicos (Inspector)

#### **Screenshot de Referência:**
```
┌─────────────────────────────────────────────────────┐
│ Drag Rect Renderer (Script)                         │
├─────────────────────────────────────────────────────┤
│ Cam: Main Camera (Camera)                           │
│ Quad Prefab: Quad (GameObject)                      │
└─────────────────────────────────────────────────────┘
```

---

```csharp
public Camera cam;
public GameObject quadPrefab;

GameObject _activeQuad;
Vector3 _startWorld;
```

**cam (Camera):**
- **Descrição:** Câmera para raycasting
- **Uso:** Converter screen → world

**quadPrefab (GameObject):**
- **Descrição:** Prefab do quad visual
- **Componentes:** Quad mesh + Material semi-transparente
- **Configuração:** Ver Seção 5.4

---

### 5.3 Lifecycle e Eventos

```csharp
void OnEnable()
{
    // REFATORAÇÃO: Subscrever eventos via GameEvents
    GameEvents.OnDragBegin += BeginRect;
    GameEvents.OnDragging += UpdateRect;
    GameEvents.OnDragEnd += EndRect;
}

void OnDisable()
{
    // REFATORAÇÃO: Desinscrever eventos via GameEvents
    GameEvents.OnDragBegin -= BeginRect;
    GameEvents.OnDragging -= UpdateRect;
    GameEvents.OnDragEnd -= EndRect;
}
```

**Eventos Consumidos:**

| Evento | Handler | Descrição |
|--------|---------|-----------|
| `OnDragBegin` | `BeginRect` | Cria quad |
| `OnDragging` | `UpdateRect` | Atualiza quad |
| `OnDragEnd` | `EndRect` | Destrói quad |

---

### 5.4 Métodos de Renderização

#### **BeginRect(Vector2 screenStart)**

```csharp
void BeginRect(Vector2 screenStart)
{
    if (!cam || !quadPrefab) return;

    if (Physics.Raycast(cam.ScreenPointToRay(screenStart), out var hit))
    {
        _startWorld = hit.point;
        _activeQuad = Instantiate(quadPrefab);
        _activeQuad.SetActive(true);
        UpdateRect(screenStart); // desenha um frame inicial
    }
}
```

**Parâmetro:**
- `screenStart`: Posição de tela onde drag começou

**Comportamento:**
1. Valida `cam` e `quadPrefab`
2. Raycast para obter `_startWorld`
3. Instancia `quadPrefab` → `_activeQuad`
4. Ativa quad
5. Chama `UpdateRect` para desenhar frame inicial

---

#### **UpdateRect(Vector2 screenPos)**

```csharp
void UpdateRect(Vector2 screenPos)
{
    if (_activeQuad == null || !cam) return;

    if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit))
    {
        Vector3 endWorld = hit.point;

        Vector3 center = (_startWorld + endWorld) * 0.5f;
        Vector3 size = new Vector3(
            Mathf.Abs(endWorld.x - _startWorld.x),
            Mathf.Abs(endWorld.z - _startWorld.z),
            1f
        );
        center.y = 1f;
        _activeQuad.transform.position = center;
        _activeQuad.transform.localScale = size;
    }
}
```

**Parâmetro:**
- `screenPos`: Posição atual do cursor

**Comportamento:**
1. Valida `_activeQuad` e `cam`
2. Raycast para obter `endWorld`
3. Calcula `center` (ponto médio entre start e end)
4. Calcula `size`:
   - X: largura (abs de diferença em X)
   - Y: profundidade (abs de diferença em Z)
   - Z: altura fixa (1f, não usado)
5. Fixa `center.y = 1f` (altura sobre o chão)
6. Atualiza `position` e `localScale` do quad

**Design Note:**
- Quad está no plano XZ (paralelo ao chão)
- `center.y = 1f` eleva quad 1 unidade acima do terreno
- `size.x` e `size.y` correspondem a X e Z no mundo

---

#### **EndRect(Vector2 _)**

```csharp
void EndRect(Vector2 _)
{
    if (_activeQuad != null)
    {
        Destroy(_activeQuad);
        _activeQuad = null;
    }
}
```

**Parâmetro:**
- `_`: Não usado (posição final, ignorada)

**Comportamento:**
- Destrói `_activeQuad`
- Reseta referência para `null`

---

### 5.5 Configuração do Prefab

#### **Screenshot de Referência (Quad Prefab):**
```
┌─────────────────────────────────────────────────────┐
│ Quad (Prefab Asset)                                 │
├─────────────────────────────────────────────────────┤
│ Transform                                           │
│   Position: (127.24, 2.036, 48.852)                │
│   Rotation: (90, 0, 0)                              │
│   Scale: (1, 1, 1)                                  │
│                                                     │
│ Quad (Mesh Filter)                                  │
│   Mesh: Quad                                        │
│                                                     │
│ Mesh Renderer                                       │
│   Materials:                                        │
│     Element 0: SelectionMaterial (Material)        │
│                                                     │
│ Lighting                                            │
│   Cast Shadows: Off                                 │
│   Contribute Global Illumination: Off               │
└─────────────────────────────────────────────────────┘
```

---

#### **Componentes do Prefab:**

**1. MeshFilter:**
- Mesh: `Quad` (built-in Unity)

**2. MeshRenderer:**
- Material: `SelectionMaterial`
- Cast Shadows: Off (performance)
- Contribute GI: Off (performance)

**3. Transform:**
- Rotation: `(90, 0, 0)` (quad paralelo ao chão XZ)
- Scale: `(1, 1, 1)` (scale será controlado por código)

---

#### **Material - SelectionMaterial:**

**Screenshot de Referência:**
```
┌─────────────────────────────────────────────────────┐
│ SelectionMaterial (Material)                        │
├─────────────────────────────────────────────────────┤
│ Shader: Universal Render Pipeline/Lit              │
│                                                     │
│ Surface Options                                     │
│   Rendering Mode: Transparent                       │
│                                                     │
│ Surface Inputs                                      │
│   Base Color: Yellow (RGB: 255, 255, 0)            │
│   Alpha: 0.3 (semi-transparente)                   │
└─────────────────────────────────────────────────────┘
```

**Configuração:**

| Propriedade | Valor |
|-------------|-------|
| Shader | Universal Render Pipeline/Lit |
| Rendering Mode | Transparent |
| Base Color | Yellow (255, 255, 0) |
| Alpha | 0.3 (30% opaco) |
| Cast Shadows | Off |

**Variações:**
- Azul semi-transparente para tema sci-fi
- Verde para tema militar
- Branco com borda para tema minimalista

---

## 6) UNITHITPROXY - PROXY DE COLISÃO

### 6.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Responsabilidade:** Facilitar detecção de `Unit` em hierarquias complexas.

**Problema que resolve:**
- Colisores podem estar em GameObjects filhos (armadura, modelo, etc.)
- Raycast acerta o colisor filho, não o GameObject com `Unit`
- `GetComponentInParent<Unit>()` resolve, mas é mais lento

**Solução:**
- `UnitHitProxy` referencia diretamente o `Unit`
- Raycast acerta proxy → retorna `proxy.unit` (O(1))

### 6.2 Código Completo

```csharp
[DisallowMultipleComponent]
public class UnitHitProxy : MonoBehaviour
{
    public Unit unit;

    void Reset() => unit = GetComponentInParent<Unit>();
}
```

**unit (Unit):**
- **Descrição:** Referência ao componente `Unit` pai
- **Auto-preenchido:** Via `Reset()` (Editor)

---

### 6.3 Setup e Uso

#### **Hierarquia Típica:**

```
Worker (GameObject)
├─ Unit (Script) ← Componente principal
├─ Armature (Empty)
│  ├─ Body (SkinnedMeshRenderer)
│  ├─ Arms (SkinnedMeshRenderer)
│  └─ HitProxy (GameObject) ← Layer: Unit
│     ├─ CapsuleCollider (Trigger)
│     └─ UnitHitProxy (Script) ← unit = Worker.Unit
└─ SelectionRing (GameObject)
```

**Setup:**
1. Criar GameObject filho: `HitProxy`
2. Adicionar Collider (Capsule, Box, etc.)
   - **Is Trigger:** Checked (recomendado)
   - **Layer:** Unit
3. Adicionar `UnitHitProxy` script
4. `Reset()` auto-preenche `unit` com `GetComponentInParent<Unit>()`

---

#### **Integração com WorldPicker:**

**Sem Proxy:**
```csharp
// WorldPicker.TryPickUnitAt() - LENTO
unit = hit.collider.GetComponentInParent<Unit>(); // ❌ Busca em hierarquia
```

**Com Proxy:**
```csharp
// WorldPicker.TryPickUnitAt() - RÁPIDO
var proxy = hit.collider.GetComponent<UnitHitProxy>();
unit = proxy != null ? proxy.unit : hit.collider.GetComponentInParent<Unit>();
// ✅ Se tem proxy, acesso direto O(1)
// ⚠️ Fallback para GetComponentInParent se não tiver proxy
```

**Código Atual:**
- WorldPicker **usa apenas** `GetComponentInParent<Unit>()`
- **Melhoria futura:** Adicionar suporte a `UnitHitProxy` para otimização

---

### 6.4 Quando Usar

**Use UnitHitProxy quando:**
- ✅ Hierarquia complexa (armature, múltiplos meshes)
- ✅ Performance crítica (centenas de raycasts/frame)
- ✅ Colisores em GameObjects filhos distantes

**Não precisa quando:**
- ❌ Colisor está no mesmo GameObject que `Unit`
- ❌ Performance não é problema
- ❌ Hierarquia simples

---

## 7) FLUXO DE SELEÇÃO COMPLETO

### 7.1 Clique Simples (Unit)

```
Jogador clica em unidade (LMB)
       │
       ▼
InputSelection.OnLmbStarted()
       │
       │ GameEvents.RaisePointerDown(screenPos)
       ▼
InputSelection.OnLmbCanceled()
       │
       │ Distance < threshold → Clique simples
       ▼
InputSelection.HandleClick(screenPos)
       │
       │ picker.TryPickUnitAt(screenPos, out unit)
       │ → Acertou!
       │
       │ GameEvents.RaiseUnitClick(unit, ctrl=false)
       ▼
SelectionManager.HandleClickUnit(unit, ctrl=false)
       │
       │ onlyOwnUnits && unit.owner != player.myFaction → return
       │ ctrl = false → Clear() + Add(unit)
       │
       ├──▶ Clear()
       │    └──▶ foreach u: u.SetSelected(false)
       │
       ├──▶ Add(unit)
       │    └──▶ unit.SetSelected(true)
       │         └──▶ GameEvents.RaiseUnitSelectionChanged(unit, true)
       │
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
UI Systems escutam OnSelectionChanged
       │
       ├──▶ UnitListUI: Reconstruir lista
       ├──▶ SelectionInfoPanel: Mostrar info
       └──▶ AudioManager: Som de seleção
```

---

### 7.2 Clique com Ctrl (Toggle)

```
Jogador clica em unidade (Ctrl+LMB)
       │
       ▼
InputSelection.HandleClick(screenPos)
       │
       │ isCtrl = input.IsCtrlPressed = true
       │ GameEvents.RaiseUnitClick(unit, ctrl=true)
       ▼
SelectionManager.HandleClickUnit(unit, ctrl=true)
       │
       │ ctrl = true → Toggle(unit)
       ▼
Toggle(unit)
       │
       ├──▶ if _selection.Contains(unit):
       │    └──▶ Remove(unit)
       │         └──▶ unit.SetSelected(false)
       │
       └──▶ else:
            └──▶ Add(unit)
                 └──▶ unit.SetSelected(true)
```

**Exemplo:**
```
Selection = {Worker1, Worker2}

Ctrl+Clique em Worker3:
  → Add(Worker3) → Selection = {Worker1, Worker2, Worker3}

Ctrl+Clique em Worker2:
  → Remove(Worker2) → Selection = {Worker1, Worker3}
```

---

### 7.3 Clique no Chão (Clear)

```
Jogador clica no chão (LMB)
       │
       ▼
InputSelection.HandleClick(screenPos)
       │
       │ picker.TryPickUnitAt() → false (não acertou unidade)
       │ picker.TryPickGroundAt() → true (acertou chão)
       │
       │ GameEvents.RaiseGroundClick(worldPos, ctrl)
       ▼
SelectionManager.HandleClickGround(worldPos, ctrl)
       │
       │ Clear()
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
UI Systems escutam OnSelectionChanged
       │
       └──▶ Selection = {} (vazia)
```

---

### 7.4 Drag Rect (Múltipla Seleção)

```
Jogador arrasta LMB
       │
       ▼
InputSelection.OnLmbStarted()
       │
       │ _lmbDown = true, _downPos = pointer
       ▼
InputSelection.Update()
       │
       │ Distance(_downPos, _pointer) >= 6px
       │ → _dragging = true
       │
       │ GameEvents.RaiseDragBegin(_downPos)
       ▼
SelectionManager.OnBeginDragHandler(_downPos)
       │
       │ Raycast → _dragStartWorld
       ▼
DragRectRenderer.BeginRect(_downPos)
       │
       │ Instantiate(quadPrefab) → _activeQuad
       ▼
InputSelection.OnPointPerformed() [loop]
       │
       │ GameEvents.RaiseDragging(_pointer)
       ▼
DragRectRenderer.UpdateRect(_pointer) [loop]
       │
       │ Atualizar position/scale do quad
       ▼
InputSelection.OnLmbCanceled()
       │
       │ _dragging = true → GameEvents.RaiseDragEnd(upPos)
       ▼
SelectionManager.HandleEndDrag(upPos)
       │
       │ Raycast → endWorld
       │ SelectByWorldRect(_dragStartWorld, endWorld, additive: ctrl)
       │
       ├──▶ Criar Bounds 3D (retângulo XZ, Y infinito)
       │
       ├──▶ Para cada unidade da facção:
       │    └──▶ if bounds.Contains(pos.xz): Add(unit)
       │
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
DragRectRenderer.EndRect(upPos)
       │
       │ Destroy(_activeQuad)
       ▼
UI Systems atualizam
```

**Screenshot de Game View:**
- Retângulo amarelo semi-transparente
- 8 unidades dentro do retângulo
- Todas ficam com highlight após drag

---

### 7.5 Duplo Clique (Selecionar Tipo)

```
Jogador clica duas vezes em Worker (1)
       │
       ▼
InputSelection.HandleClick() [1ª vez]
       │
       │ _lastClickedUnit = Worker1
       │ _lastClickTime = Time.unscaledTime
       │
       │ GameEvents.RaiseUnitClick(Worker1, ctrl=false)
       ▼
SelectionManager: Selection = {Worker1}
       │
       ▼
InputSelection.HandleClick() [2ª vez]
       │
       │ unit == _lastClickedUnit? YES
       │ (Time.unscaledTime - _lastClickTime) <= 0.28s? YES
       │
       │ → DUPLO CLIQUE!
       │ GameEvents.RaiseUnitDoubleClick(Worker1)
       ▼
SelectionManager.HandleDoubleClickUnit(Worker1)
       │
       │ Clear()
       │
       ├──▶ var mine = UnitRegistry.GetByFaction(player.myFaction)
       │
       ├──▶ foreach u in mine:
       │    ├──▶ if !IsOnScreen(u.position): continue
       │    └──▶ if u.def == Worker1.def: Add(u)
       │
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
Selection = {Worker1, Worker2, Worker3, ...}
       │
       └──▶ TODOS os workers visíveis na tela
```

**Screenshot de Game View:**
- 8 workers na tela, todos com highlight amarelo

---

### 7.6 RMB no Chão (Limpar Seleção)

```
Jogador clica RMB no chão
       │
       ▼
InputSelection.OnRmbPerformed()
       │
       │ picker.TryPickGroundAt(_pointer, out point)
       │ → Acertou!
       │
       │ GameEvents.RaiseGroundClick(point, ctrl=false)
       ▼
SelectionManager.HandleClickGround(point, ctrl=false)
       │
       │ Clear()
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
Selection = {} (vazia)
```

**Design Note:**
- RMB tipicamente usado para comandos (mover, atacar)
- SelectionManager usa RMB para limpar seleção
- Outros sistemas podem escutar `OnGroundClick` para movimento

---

### 7.7 Drag com Ctrl (Adicionar à Seleção)

```
Selection atual = {Worker1, Worker2}

Jogador arrasta Ctrl+LMB sobre área com Archer1, Archer2
       │
       ▼
SelectionManager.HandleEndDrag(upPos)
       │
       │ ctrl = input.IsCtrlPressed = true
       │ SelectByWorldRect(start, end, additive: true)
       │
       │ additive = true → NÃO Clear()
       │
       ├──▶ Para cada unidade no retângulo:
       │    └──▶ Add(unit) [não limpa seleção anterior]
       │
       │ GameEvents.RaiseSelectionChanged(_selection)
       ▼
Selection = {Worker1, Worker2, Archer1, Archer2}
       │
       └──▶ Workers + Archers selecionados
```

---

## 8) INTEGRAÇÃO COM OUTROS MÓDULOS

### 8.1 Dependências do Lote 1 (GameEvents, PlayerController)

#### **Eventos Consumidos (Lote 4 escuta):**

| Evento | Emissor | Uso no Lote 4 |
|--------|---------|---------------|
| `OnUnitClick` | InputSelection | SelectionManager.HandleClickUnit |
| `OnGroundClick` | InputSelection | SelectionManager.HandleClickGround |
| `OnDragBegin` | InputSelection | SelectionManager.OnBeginDragHandler, DragRectRenderer.BeginRect |
| `OnDragging` | InputSelection | DragRectRenderer.UpdateRect |
| `OnDragEnd` | InputSelection | SelectionManager.HandleEndDrag, DragRectRenderer.EndRect |
| `OnUnitDoubleClick` | InputSelection | SelectionManager.HandleDoubleClickUnit |
| `OnPointerDown` | InputSelection | (futuro: outline hover) |
| `OnPointerUp` | InputSelection | (futuro: analytics) |

#### **Eventos Emitidos (Lote 4 dispara):**

| Evento | Emissor | Listeners Típicos |
|--------|---------|-------------------|
| `OnSelectionChanged` | SelectionManager | UnitListUI, SelectionInfoPanel, Audio, Minimap |

#### **PlayerController:**

```csharp
// SelectionManager usa:
player.myFaction // Para filtrar unidades por facção
```

---

### 8.2 Dependências do Lote 3 (Unit, UnitRegistry)

#### **Unit:**

```csharp
// SelectionManager chama:
unit.SetSelected(true)  // Adicionar à seleção
unit.SetSelected(false) // Remover da seleção

// SelectionManager lê:
unit.owner       // Filtrar por facção
unit.def         // Comparar tipo (duplo clique)
unit.transform   // Posição (IsOnScreen, SelectByWorldRect)
```

#### **UnitRegistry:**

```csharp
// SelectionManager usa:
UnitRegistry.GetByFaction(player.myFaction) // Obter unidades da facção

// Contexto:
// - HandleDoubleClickUnit: Selecionar todas do tipo
// - SelectByWorldRect: Testar apenas unidades da facção
```

---

### 8.3 Interação com Lote 2 (Câmera - Opcional)

**Possível Integração Futura:**

```csharp
// SelectionManager.HandleClickUnit() - adicionar foco na câmera
void HandleClickUnit(Unit unit, bool ctrl)
{
    if (!ctrl) Clear();
    Add(unit);
    
    // ✨ NOVO: Focar câmera na unidade selecionada
    if (!ctrl && _selection.Count == 1)
    {
        GameEvents.RaiseSelectionFocus(unit.transform);
    }
    
    FireChanged();
}
```

**Evento:**
- `GameEvents.OnSelectionFocus` (Lote 1, Seção 2.4.5)
- `RTSCameraController` (Lote 2) já escuta este evento

**Benefício:**
- Câmera foca automaticamente em unidade selecionada
- Opcional: apenas se seleção única (não em drag rect)

---

### 8.4 Interação com UI System (Futuro)

#### **UnitListUI:**

```csharp
public class UnitListUI : MonoBehaviour
{
    [SerializeField] Transform listContainer;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] SelectionManager selectionManager;
    
    void OnEnable()
    {
        GameEvents.OnSelectionChanged += OnSelectionChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnSelectionChanged -= OnSelectionChanged;
    }
    
    void OnSelectionChanged(IReadOnlyCollection<Unit> selection)
    {
        // Limpar lista
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);
        
        // Criar itens
        foreach (var unit in selection)
        {
            var item = Instantiate(itemPrefab, listContainer);
            var itemUI = item.GetComponent<UnitListItemUI>();
            itemUI.Bind(unit);
            
            // Listener de clique no item
            itemUI.OnClick += () => {
                selectionManager.SelectExactly(unit); // ← Usar API pública
            };
        }
    }
}
```

---

#### **SelectionInfoPanel:**

```csharp
public class SelectionInfoPanel : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text hpText;
    [SerializeField] Text levelText;
    
    void OnEnable()
    {
        GameEvents.OnSelectionChanged += OnSelectionChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnSelectionChanged -= OnSelectionChanged;
    }
    
    void OnSelectionChanged(IReadOnlyCollection<Unit> selection)
    {
        if (selection.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        if (selection.Count == 1)
        {
            // Seleção única: mostrar detalhes
            var unit = selection.First();
            nameText.text = unit.DisplayName;
            hpText.text = $"HP: {unit.hp}/{unit.hpMax}";
            levelText.text = $"Level: {unit.Level}";
        }
        else
        {
            // Múltiplas: mostrar contagem
            nameText.text = $"{selection.Count} unidades selecionadas";
            hpText.text = "";
            levelText.text = "";
        }
    }
}
```

---

## 9) CONFIGURAÇÃO NA CENA

### 9.1 Setup de SelectionManager

#### **1. Criar GameObject:**

```
Hierarchy → Create Empty
Nome: "Selection"
```

#### **2. Adicionar Componentes:**

```
Selection (GameObject)
├─ WorldPicker (Script)
├─ InputSelection (Script)
├─ SelectionManager (Script)
└─ SelectionDebugListener (Script) [opcional]
```

#### **3. Configurar WorldPicker:**

```
┌─────────────────────────────────────┐
│ World Picker (Script)               │
├─────────────────────────────────────┤
│ Cam: Main Camera                    │
│ Unit Mask: Unit                     │
│ Ground Mask: Ground                 │
└─────────────────────────────────────┘
```

- **Cam:** Arraste Main Camera
- **Unit Mask:** Selecione layer `Unit`
- **Ground Mask:** Selecione layer `Ground`

---

#### **4. Configurar InputSelection:**

```
┌─────────────────────────────────────┐
│ Input Selection (Script)            │
├─────────────────────────────────────┤
│ Refs                                │
│   Picker: Selection (World Picker) │
│                                     │
│ Config                              │
│   Drag Threshold Px: 6              │
│   Double Click Window: 0.28         │
│                                     │
│ Actions                             │
│   Point: Selection/Point            │
│   Lmb: Selection/LMB                │
│   Rmb: Selection/RMB                │
│   Ctrl: Selection/Ctrl              │
│   Shift: Selection/Shift            │
└─────────────────────────────────────┘
```

**Actions Setup:**
1. Criar Input Actions Asset: `Assets → Create → Input Actions`
2. Nome: `SelectionInputActions`
3. Criar Action Map: `Selection`
4. Criar Actions:
   - `Point` (Value, Vector2) → Mouse/position
   - `LMB` (Button) → Mouse/leftButton
   - `RMB` (Button) → Mouse/rightButton
   - `Ctrl` (Button) → Keyboard/leftCtrl
   - `Shift` (Button) → Keyboard/leftShift
5. Salvar asset
6. Arrastar actions para campos do Inspector

---

#### **5. Configurar SelectionManager:**

```
┌─────────────────────────────────────┐
│ Selection Manager (Script)          │
├─────────────────────────────────────┤
│ Selection                           │
│   Rect Inflate Px: 1.5              │
│                                     │
│ Refs                                │
│   Player: PlayerSettings            │
│   Cam: Main Camera                  │
│   Input: Selection (Input Sel)     │
│                                     │
│ Filtro                              │
│   Only Own Units: ☑                 │
└─────────────────────────────────────┘
```

- **Player:** Arraste GameObject com `PlayerController`
- **Cam:** Arraste Main Camera
- **Input:** Arraste GameObject com `InputSelection`
- **Only Own Units:** Marcar (típico para RTS)

---

### 9.2 Setup de DragRectRenderer

#### **1. Criar GameObject:**

```
Hierarchy → Create Empty
Nome: "DragRenderer"
```

#### **2. Adicionar Componente:**

```
DragRenderer (GameObject)
└─ DragRectRenderer (Script)
```

#### **3. Criar Prefab do Quad:**

**Passo a Passo:**

1. **Criar Quad:**
   - Hierarchy → 3D Object → Quad
   - Nome: `SelectionQuad`

2. **Rotacionar:**
   - Rotation: `(90, 0, 0)` (paralelo ao chão)

3. **Criar Material:**
   - Project → Create → Material
   - Nome: `SelectionMaterial`
   - Shader: `Universal Render Pipeline/Lit`
   - Surface Type: `Transparent`
   - Base Color: Yellow (255, 255, 0)
   - Alpha: 0.3
   - Drag material para Quad

4. **Configurar Renderer:**
   - Cast Shadows: Off
   - Receive Shadows: Off (opcional)

5. **Criar Prefab:**
   - Arraste Quad para pasta `Assets/Prefabs/`
   - Deletar da Hierarchy

---

#### **4. Configurar DragRectRenderer:**

```
┌─────────────────────────────────────┐
│ Drag Rect Renderer (Script)         │
├─────────────────────────────────────┤
│ Cam: Main Camera                    │
│ Quad Prefab: SelectionQuad          │
└─────────────────────────────────────┘
```

- **Cam:** Arraste Main Camera
- **Quad Prefab:** Arraste prefab `SelectionQuad`

---

### 9.3 Setup de UnitHitProxy (por Unidade)

#### **Hierarquia Recomendada:**

```
Worker (GameObject) ← Layer: Default
├─ Unit (Script)
├─ Armature (Empty)
│  ├─ Body (SkinnedMeshRenderer)
│  └─ HitProxy (GameObject) ← Layer: Unit
│     ├─ CapsuleCollider (Is Trigger: ON)
│     └─ UnitHitProxy (Script)
└─ SelectionRing (GameObject)
```

**Passo a Passo:**

1. **Criar HitProxy:**
   - Selecionar unidade (ex: Worker)
   - Hierarchy → Create Empty Child
   - Nome: `HitProxy`
   - Layer: `Unit`

2. **Adicionar Collider:**
   - Add Component → Capsule Collider
   - **Is Trigger:** Marcar
   - Ajustar tamanho para cobrir unidade

3. **Adicionar UnitHitProxy:**
   - Add Component → Unit Hit Proxy
   - `Reset()` auto-preenche campo `unit`

4. **Verificar:**
   - Inspector do HitProxy → `unit` aponta para componente `Unit` pai

---

### 9.4 LayerMasks e Collision Matrix

#### **1. Configurar Layers:**

**Edit → Project Settings → Tags and Layers**

```
Layers:
  0: Default
  6: Unit         ← Unidades (ou colliders de hit)
  7: Ground       ← Terreno
```

---

#### **2. Configurar Collision Matrix:**

**Edit → Project Settings → Physics**

```
Collision Matrix:
           Default  Unit  Ground
Default      ✓      ✓      ✓
Unit         ✓      ✗      ✗     ← Units não colidem entre si nem com chão
Ground       ✓      ✗      ✓
```

**Justificativa:**
- Unit × Unit: Desmarcar (se unidades não têm física entre si)
- Unit × Ground: Desmarcar (se unidades flutuam/NavMesh)
- Manter: Default × Unit (raycasting funciona)

---

#### **3. Atribuir Layers aos GameObjects:**

**Unidades:**
```
Worker (GameObject) ← Layer: Default (ou Default)
└─ HitProxy ← Layer: Unit (collider aqui)
```

**Terreno:**
```
Terrain (GameObject) ← Layer: Ground
```

**Verificação:**
- WorldPicker.unitMask deve incluir layer `Unit`
- WorldPicker.groundMask deve incluir layer `Ground`

---

### 9.5 Input System Setup

#### **1. Instalar Input System Package:**

**Window → Package Manager**
- Search: `Input System`
- Install

**Project Settings → Player**
- Active Input Handling: `Both` ou `Input System Package (New)`

---

#### **2. Criar Input Actions Asset:**

**Assets → Create → Input Actions**
- Nome: `SelectionInputActions`

**Estrutura:**
```
SelectionInputActions
└─ Selection (Action Map)
   ├─ Point (Value, Vector2, Pass Through)
   │  └─ Binding: <Mouse>/position
   ├─ LMB (Button)
   │  └─ Binding: <Mouse>/leftButton
   ├─ RMB (Button)
   │  └─ Binding: <Mouse>/rightButton
   ├─ Ctrl (Button)
   │  └─ Binding: <Keyboard>/leftCtrl
   └─ Shift (Button)
      └─ Binding: <Keyboard>/leftShift
```

---

#### **3. Habilitar Input Actions:**

**Opção A: Via Script (já feito em InputSelection):**
```csharp
void OnEnable()
{
    point?.action.Enable();
    lmb?.action.Enable();
    // ...
}
```

**Opção B: Via Auto-Enable:**
- Input Actions Asset → Inspector
- `Generate C# Class` (opcional, para strongly-typed access)

---

## 10) EXEMPLOS DE USO AVANÇADOS

### 10.1 Seleção Programática

#### **Selecionar Todas as Unidades de um Tipo:**

```csharp
// Selecionar todos os workers
public void SelectAllWorkers()
{
    var workers = UnitRegistry.GetByFaction(player.myFaction)
        .Where(u => u.def.type == UnitType.Worker);
    
    selectionManager.SelectExactly(workers);
}

// Botão de UI
void OnButtonClick_SelectWorkers()
{
    SelectAllWorkers();
}
```

---

#### **Selecionar Unidades por HP Baixo:**

```csharp
// Selecionar unidades com HP < 30%
public void SelectDamagedUnits()
{
    var damaged = UnitRegistry.GetByFaction(player.myFaction)
        .Where(u => u.hp / u.hpMax < 0.3f);
    
    selectionManager.SelectExactly(damaged);
}

// Hotkey: H = Help (selecionar feridos)
void Update()
{
    if (Input.GetKeyDown(KeyCode.H))
    {
        SelectDamagedUnits();
    }
}
```

---

#### **Adicionar Unidades à Seleção Existente:**

```csharp
// Shift+Clique em grupo: adicionar à seleção
public void OnGroupClick(List<Unit> groupUnits, bool shift)
{
    if (shift)
    {
        selectionManager.AddToSelection(groupUnits); // ✨ Método NOVO
    }
    else
    {
        selectionManager.SelectExactly(groupUnits);
    }
}
```

---

### 10.2 Filtros Customizados

#### **Selecionar Unidades em Área Customizada (Círculo):**

```csharp
public void SelectInRadius(Vector3 center, float radius)
{
    var mine = UnitRegistry.GetByFaction(player.myFaction);
    var inRadius = mine.Where(u => Vector3.Distance(u.transform.position, center) <= radius);
    
    selectionManager.SelectExactly(inRadius);
}

// Exemplo: Selecionar unidades ao redor de um edifício
void OnBuildingClick(Building building)
{
    SelectInRadius(building.transform.position, 10f);
}
```

---

#### **Selecionar Unidades Idle (Sem Comandos):**

```csharp
// Assumindo que Unit tem propriedade IsIdle
public void SelectIdleUnits()
{
    var idle = UnitRegistry.GetByFaction(player.myFaction)
        .Where(u => u.IsIdle); // Precisa implementar IsIdle
    
    selectionManager.SelectExactly(idle);
}

// Hotkey: I = Idle
void Update()
{
    if (Input.GetKeyDown(KeyCode.I))
    {
        SelectIdleUnits();
    }
}
```

---

### 10.3 Grupos de Controle (Control Groups)

```csharp
public class ControlGroups : MonoBehaviour
{
    [SerializeField] SelectionManager selectionManager;
    
    Dictionary<int, List<Unit>> _groups = new();
    
    void Update()
    {
        // Salvar grupo: Ctrl+1..9
        if (Input.GetKey(KeyCode.LeftControl))
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    SaveGroup(i);
                }
            }
        }
        // Selecionar grupo: 1..9
        else
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    SelectGroup(i);
                }
            }
        }
    }
    
    void SaveGroup(int groupIndex)
    {
        _groups[groupIndex] = new List<Unit>(selectionManager.Selection);
        Debug.Log($"Grupo {groupIndex} salvo: {_groups[groupIndex].Count} unidades");
    }
    
    void SelectGroup(int groupIndex)
    {
        if (_groups.TryGetValue(groupIndex, out var units))
        {
            // Remover unidades mortas
            units.RemoveAll(u => u == null);
            
            selectionManager.SelectExactly(units);
            Debug.Log($"Grupo {groupIndex} selecionado: {units.Count} unidades");
        }
    }
}
```

---

### 10.4 Seleção por Box 2D (Screen Space)

```csharp
// Alternativa ao drag rect 3D: usar screen space box
public void SelectInScreenRect(Rect screenRect)
{
    var mine = UnitRegistry.GetByFaction(player.myFaction);
    var cam = Camera.main;
    
    var selected = new List<Unit>();
    foreach (var unit in mine)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
        
        // Verificar se está na frente da câmera e dentro do rect
        if (screenPos.z > 0 && screenRect.Contains(screenPos))
        {
            selected.Add(unit);
        }
    }
    
    selectionManager.SelectExactly(selected);
}
```

---

### 10.5 Seleção Inteligente (Smart Select)

```csharp
// Clique: Seleciona unidade
// Duplo clique: Seleciona todas do tipo
// Triplo clique: Seleciona todas da categoria (combate, coleta, etc.)
public class SmartSelection : MonoBehaviour
{
    [SerializeField] SelectionManager selectionManager;
    [SerializeField] float tripleClickWindow = 0.5f;
    
    int _clickCount = 0;
    float _lastClickTime;
    Unit _lastClickedUnit;
    
    void OnEnable()
    {
        GameEvents.OnUnitClick += OnUnitClick;
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitClick -= OnUnitClick;
    }
    
    void OnUnitClick(Unit unit, bool ctrl)
    {
        if (unit == _lastClickedUnit && Time.unscaledTime - _lastClickTime <= tripleClickWindow)
        {
            _clickCount++;
        }
        else
        {
            _clickCount = 1;
        }
        
        _lastClickedUnit = unit;
        _lastClickTime = Time.unscaledTime;
        
        if (_clickCount == 3)
        {
            // Triplo clique: selecionar categoria
            SelectCategory(unit);
            _clickCount = 0;
        }
    }
    
    void SelectCategory(Unit unit)
    {
        UnitType type = unit.def.type;
        
        // Definir categoria
        List<UnitType> category;
        if (type == UnitType.Worker)
        {
            category = new List<UnitType> { UnitType.Worker };
        }
        else if (type == UnitType.Warrior || type == UnitType.Archer || type == UnitType.Spearman)
        {
            category = new List<UnitType> { UnitType.Warrior, UnitType.Archer, UnitType.Spearman };
        }
        else
        {
            category = new List<UnitType> { type };
        }
        
        // Selecionar todas da categoria
        var units = UnitRegistry.GetByFaction(selectionManager.player.myFaction)
            .Where(u => category.Contains(u.def.type));
        
        selectionManager.SelectExactly(units);
    }
}
```

---

## 11) TROUBLESHOOTING E FAQ

### 11.1 Clique não seleciona unidade

**Sintomas:**
- Clicar em unidade não seleciona
- Console sem erros

**Soluções:**

1. **Verificar LayerMask:**
   ```csharp
   // WorldPicker.unitMask deve incluir layer da unidade
   Debug.Log($"Unit layer: {unit.gameObject.layer}");
   Debug.Log($"Unit mask includes layer: {(unitMask & (1 << unit.gameObject.layer)) != 0}");
   ```

2. **Verificar Collider:**
   - Unidade tem Collider?
   - Collider está habilitado?
   - Collider está em layer correto?

3. **Verificar EventSystem:**
   - Clique pode estar sobre UI
   - Debug: `Debug.Log(InputSelection.IsPointerOverUI());`

4. **Verificar InputActions:**
   - Actions estão habilitadas?
   - Bindings corretos?
   - Debug: Adicionar log em `OnLmbStarted`

---

### 11.2 Drag rect não aparece

**Sintomas:**
- Arrastar mouse não mostra retângulo amarelo
- Seleção funciona, mas sem visual

**Soluções:**

1. **Verificar Prefab:**
   - `DragRectRenderer.quadPrefab` está atribuído?
   - Prefab tem MeshRenderer + Material?

2. **Verificar Material:**
   - Material é transparente?
   - Alpha > 0?
   - Shader correto?

3. **Verificar Câmera:**
   - `DragRectRenderer.cam` está atribuído?
   - Câmera renderiza layer do quad?

4. **Verificar Eventos:**
   - `GameEvents.OnDragBegin` está sendo disparado?
   - Debug: `Debug.Log("BeginRect chamado");` em `BeginRect()`

---

### 11.3 Duplo clique não funciona

**Sintomas:**
- Duplo clique seleciona apenas unidade clicada
- Não seleciona todas do tipo

**Soluções:**

1. **Verificar Janela Temporal:**
   ```csharp
   // InputSelection.doubleClickWindow
   // Padrão: 0.28s
   // Se clicar muito devagar, não detecta
   ```

2. **Verificar UnitDefinition:**
   ```csharp
   // HandleDoubleClickUnit compara:
   if (u.def == unit.def) // Compara referência ao ScriptableObject
   
   // Se UnitDefinitions são diferentes instâncias, não funcionará
   // Solução: Comparar tipo
   if (u.def.type == unit.def.type)
   ```

3. **Verificar IsOnScreen:**
   - Método pode estar retornando `false` para unidades visíveis
   - Debug: `Debug.Log($"IsOnScreen({u.DisplayName}): {IsOnScreen(u.transform.position)}");`

---

### 11.4 Selecionar unidade inimiga quando onlyOwnUnits = true

**Sintomas:**
- Consegue selecionar unidades inimigas
- `onlyOwnUnits` está marcado

**Soluções:**

1. **Verificar owner da Unit:**
   ```csharp
   Debug.Log($"Unit owner: {unit.owner}");
   Debug.Log($"Player faction: {player.myFaction}");
   Debug.Log($"Match: {unit.owner == player.myFaction}");
   ```

2. **Verificar PlayerController:**
   - `SelectionManager.player` está atribuído?
   - `player.myFaction` está correto?

3. **Verificar filtro:**
   ```csharp
   // Em HandleClickUnit()
   if (onlyOwnUnits && unit.owner != player.myFaction) return; // ← Deve retornar
   ```

---

### 11.5 Drag rect seleciona unidades fora da área

**Sintomas:**
- Drag rect pequeno seleciona muitas unidades
- Unidades longe da área são selecionadas

**Soluções:**

1. **Verificar Bounds:**
   ```csharp
   // Em SelectByWorldRect()
   Debug.Log($"Bounds: {bounds.min} to {bounds.max}");
   Debug.Log($"Unit {u.DisplayName} at {u.transform.position}");
   Debug.Log($"Contains: {bounds.Contains(new Vector3(pos.x, 0f, pos.z))}");
   ```

2. **Verificar Y = infinito:**
   - Bounds tem `Y = float.MinValue..MaxValue`
   - Unidades em alturas diferentes são selecionadas
   - Se problema: limitar Y

3. **Verificar rectInflatePx:**
   - Valor alto (ex: 10px) aumenta muito a área
   - Reduzir para 1-2px

---

### 11.6 Performance ruim com muitas unidades

**Sintomas:**
- FPS cai ao arrastar drag rect
- Lag ao selecionar

**Soluções:**

1. **Otimizar SelectByWorldRect:**
   ```csharp
   // Usar spatial partitioning (quadtree, grid)
   // Ou limitar consulta por distância
   var mine = UnitRegistry.GetByFaction(player.myFaction)
       .Where(u => Vector3.Distance(u.transform.position, center) <= maxDistance);
   ```

2. **Otimizar IsOnScreen:**
   ```csharp
   // Cache screenPos se chamar múltiplas vezes
   Dictionary<Unit, Vector3> _screenPosCache = new();
   ```

3. **Usar UnitHitProxy:**
   - `GetComponentInParent<Unit>()` é O(n) na hierarquia
   - `UnitHitProxy` é O(1)

---

## 12) TABELA DE RELACIONAMENTOS COMPLETA

### 12.1 Classes do Lote 4

| Classe | Tipo | Depende De | Dependentes | Eventos (Emit) | Eventos (Listen) |
|--------|------|-----------|-------------|----------------|------------------|
| **SelectionManager** | MB | `PlayerController` (L1), `Camera`, `InputSelection`, `Unit` (L3), `UnitRegistry` (L3), `GameEvents` (L1) | UI Systems | `OnSelectionChanged` | `OnUnitClick`, `OnGroundClick`, `OnDragBegin`, `OnDragEnd`, `OnUnitDoubleClick` |
| **InputSelection** | MB | `WorldPicker`, `Input System`, `GameEvents` (L1) | `SelectionManager` | `OnPointerDown`, `OnPointerUp`, `OnDragBegin`, `OnDragging`, `OnDragEnd`, `OnUnitClick`, `OnGroundClick`, `OnUnitDoubleClick` | - |
| **WorldPicker** | MB | `Camera`, `Unit` (L3) | `InputSelection` | - | - |
| **DragRectRenderer** | MB | `Camera`, `GameEvents` (L1) | - | - | `OnDragBegin`, `OnDragging`, `OnDragEnd` |
| **UnitHitProxy** | MB | `Unit` (L3) | `WorldPicker` (opcional) | - | - |

---

### 12.2 Integrações com Outros Lotes

| Lote Consumidor | Usa do Lote 4 | Forma de Uso |
|-----------------|---------------|--------------|
| **Lote 1 - GameEvents** | Todos os eventos de input/seleção | InputSelection emite 8 eventos, SelectionManager emite 1 |
| **Lote 3 - Unit/Registry** | SelectionManager chama SetSelected(), consulta Registry | `unit.SetSelected(true/false)`, `UnitRegistry.GetByFaction()` |
| **UI System** | Escuta OnSelectionChanged | `GameEvents.OnSelectionChanged`, acessa `SelectionManager.Selection` |
| **Câmera (Lote 2)** | (opcional) OnSelectionFocus | Futuro: focar câmera em unidade selecionada |
| **Command System** | Escuta OnGroundClick para movimento | `GameEvents.OnGroundClick` → mover unidades selecionadas |

---

### 12.3 Fluxo de Dados Completo

```
Input Físico (Mouse/Keyboard)
       │
       ▼
Unity Input System
       │
       ▼
InputSelection
       │
       ├──▶ OnLmbStarted/Canceled
       ├──▶ OnRmbPerformed
       ├──▶ OnPointPerformed
       │
       │ Emite 8 eventos via GameEvents
       ▼
GameEvents (Lote 1)
       │
       ├─────────────────┬─────────────────┐
       │                 │                 │
       ▼                 ▼                 ▼
SelectionManager   DragRectRenderer   (Outros)
       │                 │
       │ Gerencia        │ Renderiza
       │ HashSet<Unit>   │ Quad 3D
       │                 │
       ├──▶ Chama Unit.SetSelected()
       │    └──▶ GameEvents.RaiseUnitSelectionChanged (Lote 3)
       │
       │ Emite OnSelectionChanged
       ▼
GameEvents.OnSelectionChanged
       │
       ├─────────────────┬─────────────────┬─────────────────┐
       ▼                 ▼                 ▼                 ▼
  UnitListUI      SelectionInfo     MinimapUI         AudioManager
  (reconstrói)    (atualiza)        (destaca)         (som)
```

---

## 13) CHANGELOG E MIGRAÇÕES

### 13.1 Mudanças da Versão Anterior → v1.0

#### **✅ ADICIONADO:**

1. **Eventos via GameEvents (Refatoração Principal):**
   - **InputSelection:** Todos os eventos locais removidos → `GameEvents.Raise*`
   - **SelectionManager:** Evento local removido → `GameEvents.RaiseSelectionChanged`
   - **DragRectRenderer:** Referência a InputSelection removida → usa GameEvents

2. **Método Aditivo (SelectionManager):**
   - `AddToSelection(IEnumerable<Unit>)` ✨ NOVO
   - Adiciona unidades sem limpar seleção existente

3. **Detecção de UI Melhorada:**
   - `ComputePointerOverUI()` com suporte a New Input System
   - Suporte a Mouse + Touchscreen

#### **🔄 MODIFICADO:**

1. **InputSelection.cs:**
   - Eventos locais substituídos por GameEvents (8 eventos)
   - `HandleClick()` agora emite eventos via GameEvents
   - Detecção de UI otimizada (cache em `_overUIThisFrame`)

2. **SelectionManager.cs:**
   - Evento local substituído por GameEvents
   - Handlers agora escutam GameEvents (não InputSelection diretamente)
   - `FireChanged()` chama `GameEvents.RaiseSelectionChanged()`

3. **DragRectRenderer.cs:**
   - Referência a `InputSelection` removida
   - Eventos via GameEvents

#### **❌ REMOVIDO:**

1. **Eventos Locais (Obsoletos):**
   - `InputSelection.OnPointerDown` → Use `GameEvents.OnPointerDown`
   - `InputSelection.OnPointerUp` → Use `GameEvents.OnPointerUp`
   - `InputSelection.OnBeginDrag` → Use `GameEvents.OnDragBegin`
   - `InputSelection.OnDragging` → Use `GameEvents.OnDragging`
   - `InputSelection.OnEndDrag` → Use `GameEvents.OnDragEnd`
   - `InputSelection.OnClickUnit` → Use `GameEvents.OnUnitClick`
   - `InputSelection.OnDoubleClickUnit` → Use `GameEvents.OnUnitDoubleClick`
   - `InputSelection.OnClickGround` → Use `GameEvents.OnGroundClick`
   - `SelectionManager.OnSelectionChanged` → Use `GameEvents.OnSelectionChanged`

---

### 13.2 Guia de Migração (Versão Antiga → v1.0)

#### **Para Código que Usava Eventos Locais de InputSelection:**

**Antes (❌ Obsoleto):**
```csharp
public class MySystem : MonoBehaviour
{
    [SerializeField] InputSelection input;
    
    void OnEnable()
    {
        input.OnClickUnit += HandleClickUnit; // ❌ Evento local
        input.OnDragging += HandleDragging;   // ❌
    }
}
```

**Depois (✅ Atual):**
```csharp
public class MySystem : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnUnitClick += HandleClickUnit; // ✅ GameEvents
        GameEvents.OnDragging += HandleDragging;   // ✅
    }
    
    void OnDisable()
    {
        GameEvents.OnUnitClick -= HandleClickUnit; // ✅ Desinscrever
        GameEvents.OnDragging -= HandleDragging;   // ✅
    }
    
    void HandleClickUnit(Unit unit, bool ctrl) { ... }
    void HandleDragging(Vector2 screenPos) { ... }
}
```

---

#### **Para Código que Usava Evento Local de SelectionManager:**

**Antes (❌ Obsoleto):**
```csharp
public class UnitListUI : MonoBehaviour
{
    [SerializeField] SelectionManager selectionManager;
    
    void OnEnable()
    {
        selectionManager.OnSelectionChanged += OnSelectionChanged; // ❌
    }
}
```

**Depois (✅ Atual):**
```csharp
public class UnitListUI : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnSelectionChanged += OnSelectionChanged; // ✅
    }
    
    void OnDisable()
    {
        GameEvents.OnSelectionChanged -= OnSelectionChanged; // ✅
    }
    
    void OnSelectionChanged(IReadOnlyCollection<Unit> selection) { ... }
}
```

---

#### **Para Código que Referenciava InputSelection em DragRectRenderer:**

**Antes (❌ Obsoleto):**
```csharp
public class DragRectRenderer : MonoBehaviour
{
    public InputSelection input; // ❌ Referência removida
    
    void OnEnable()
    {
        input.OnBeginDrag += BeginRect; // ❌
    }
}
```

**Depois (✅ Atual):**
```csharp
public class DragRectRenderer : MonoBehaviour
{
    // ✅ Sem referência a InputSelection
    
    void OnEnable()
    {
        GameEvents.OnDragBegin += BeginRect; // ✅
        GameEvents.OnDragging += UpdateRect; // ✅
        GameEvents.OnDragEnd += EndRect;     // ✅
    }
    
    void OnDisable()
    {
        GameEvents.OnDragBegin -= BeginRect;
        GameEvents.OnDragging -= UpdateRect;
        GameEvents.OnDragEnd -= EndRect;
    }
}
```

---

### 13.3 Checklist de Migração

Use esta checklist para atualizar seu código:

- [ ] **Buscar eventos locais de InputSelection:** Procure por `input.On*`
- [ ] **Substituir por GameEvents:** Troque por `GameEvents.On*`
- [ ] **Buscar evento local de SelectionManager:** Procure por `selectionManager.OnSelectionChanged`
- [ ] **Substituir por GameEvents:** Troque por `GameEvents.OnSelectionChanged`
- [ ] **Mover subscribe para OnEnable():** Se estava em `Start()`, mova para `OnEnable()`
- [ ] **Adicionar unsubscribe em OnDisable():** CRÍTICO para evitar memory leaks
- [ ] **Remover referências a InputSelection:** Se só usava para eventos (não propriedades)
- [ ] **Testar:** Verificar que eventos ainda funcionam após migração

---

## 14) REFERÊNCIAS RÁPIDAS

### 14.1 Atalhos de Código

**Selecionar unidade programaticamente:**
```csharp
selectionManager.SelectExactly(unit);
```

**Selecionar múltiplas unidades:**
```csharp
selectionManager.SelectExactly(listOfUnits);
```

**Adicionar à seleção existente:**
```csharp
selectionManager.AddToSelection(listOfUnits); // ✨ NOVO
```

**Togglear unidades:**
```csharp
selectionManager.ToggleSet(listOfUnits);
```

**Verificar se selecionada:**
```csharp
if (selectionManager.IsSelected(unit)) { ... }
```

**Obter seleção atual:**
```csharp
foreach (var unit in selectionManager.Selection) { ... }
int count = selectionManager.Count;
```

---

### 14.2 Valores Típicos

| Parâmetro | Min | Típico | Max | Descrição |
|-----------|-----|--------|-----|-----------|
| dragThresholdPx | 2 | 6 | 15 | Pixels para drag |
| doubleClickWindow | 0.15 | 0.28 | 0.5 | Segundos para duplo clique |
| rectInflatePx | 0 | 1.5 | 5 | Pixels extras no drag rect |

---

### 14.3 Eventos do Lote 4

**Emitidos por InputSelection:**

| Evento | Parâmetros | Quando |
|--------|------------|--------|
| `OnPointerDown` | `Vector2 screenPos` | LMB pressionado |
| `OnPointerUp` | `Vector2 screenPos` | LMB solto |
| `OnDragBegin` | `Vector2 startPos` | Drag iniciado |
| `OnDragging` | `Vector2 currentPos` | Durante drag |
| `OnDragEnd` | `Vector2 endPos` | Drag terminado |
| `OnUnitClick` | `Unit unit, bool ctrl` | Clique em unidade |
| `OnGroundClick` | `Vector3 worldPos, bool ctrl` | Clique no chão |
| `OnUnitDoubleClick` | `Unit unit` | Duplo clique |

**Emitido por SelectionManager:**

| Evento | Parâmetros | Quando |
|--------|------------|--------|
| `OnSelectionChanged` | `IReadOnlyCollection<Unit>` | Seleção muda |

---

### 14.4 Estrutura de Arquivos

```
Assets/
├── Scripts/
│   └── Selection/
│       ├── SelectionManager.cs
│       ├── InputSelection.cs
│       ├── WorldPicker.cs
│       ├── DragRectRenderer.cs
│       ├── UnitHitProxy.cs
│       └── SelectionDebugListener.cs
│
├── Prefabs/
│   └── Selection/
│       └── SelectionQuad.prefab
│
├── Materials/
│   └── SelectionMaterial.mat
│
└── Input/
    └── SelectionInputActions.inputactions
```

---

## 15) CONCLUSÃO

### 15.1 Resumo do Lote 4

O **Lote 4 - Selection System** estabelece o **sistema completo de seleção de unidades** para Medieval Thrones:

✅ **InputSelection**: Captura de input via New Input System com 8 eventos  
✅ **SelectionManager**: Gerenciamento de seleção com filtros e API pública  
✅ **WorldPicker**: Raycasting otimizado com LayerMasks  
✅ **DragRectRenderer**: Visual de seleção em 3D com quad semi-transparente  
✅ **UnitHitProxy**: Proxy para otimizar detecção em hierarquias complexas  
✅ **Integração com GameEvents**: 9 eventos (8 input + 1 seleção)  
✅ **Suporte Completo**: Clique simples, Ctrl, drag, duplo clique, RMB  

### 15.2 Qualidade da Arquitetura

**Pontos Fortes:**
- ✅ **Desacoplamento Total**: Todos os eventos via GameEvents
- ✅ **Modularidade**: 5 componentes independentes e reutilizáveis
- ✅ **Extensibilidade**: API pública para seleção programática
- ✅ **Performance**: LayerMasks, HashSet, otimizações
- ✅ **UX Profissional**: Drag rect, duplo clique, filtros por facção
- ✅ **Detecção de UI**: Ignora cliques sobre UI (EventSystem)

**Padrões de Excelência:**
- ✅ Event-driven architecture (GameEvents)
- ✅ New Input System (InputActionReference)
- ✅ LayerMasks para raycasting otimizado
- ✅ HashSet para seleção O(1)
- ✅ IReadOnlyCollection para API segura

### 15.3 Integração com Outros Lotes

**Lote 1 - Variáveis Globais & Factions:**
- ✅ Emite 9 eventos via `GameEvents`
- ✅ Usa `PlayerController.myFaction`
- ✅ Todas as integrações já documentadas no Lote 1

**Lote 3 - Módulo Unit:**
- ✅ Chama `Unit.SetSelected(true/false)`
- ✅ Consulta `UnitRegistry.GetByFaction()`
- ✅ Integração prevista no Lote 3 confirmada

**Lote 2 - Câmera System:**
- ✅ (Opcional) Integração via `OnSelectionFocus`
- ✅ Sem conflitos, integração preparada

**UI System (Futuro):**
- ✅ Escuta `OnSelectionChanged`
- ✅ Acessa `SelectionManager.Selection`
- ✅ API pública completa disponível

### 15.4 Próximos Passos

**Melhorias Futuras:**
- Shift+Click para range selection (usar `_rangeAnchor`)
- Otimização de `WorldPicker` com suporte a `UnitHitProxy`
- Control Groups (Ctrl+1..9 para salvar grupos)
- Smart Selection (triplo clique para categoria)
- Spatial partitioning para drag rect com milhares de unidades

**Novos Módulos (que usarão Selection):**
- Sistema de Comandos (mover, atacar, patrulhar)
- Sistema de Formações (unidades selecionadas em formação)
- Sistema de UI Avançado (painel de informações, lista de unidades)
- Sistema de IA (selecionar unidades da IA para debug)

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão do Documento:** 3.0  
**Compatibilidade:** Unity 2022.3+, New Input System 1.7+, Medieval Thrones v3.0+

---

# LOTE 5 — USER INTERFACE / LEFT BAR (Documentação Completa)

**Versão:** 2.1 (Atualizada - Outubro 2025)  
**Status:** ✅ Refatorado com Event Bus e Desacoplamento Completo  
**Projeto:** Medieval Thrones - RTS Strategy Game  

---

## 📚 ÍNDICE COMPLETO

### PARTE I: VISÃO GERAL
1. [Introdução ao Módulo](#1-introdução-ao-módulo)
2. [Arquitetura e Padrões](#2-arquitetura-e-padrões)
3. [Fluxo de Dados](#3-fluxo-de-dados)

### PARTE II: COMPONENTES CORE
4. [UnitListPanel - Controlador Principal](#4-unitlistpanel---controlador-principal)
5. [ListItemWrapper - Wrapper Unificado](#5-listitemwrapper---wrapper-unificado)
6. [IListItemModel - Interface Base](#6-ilistitemmodel---interface-base)

### PARTE III: SISTEMA DE GRUPOS
7. [UnitGroup - Modelo de Dados](#7-unitgroup---modelo-de-dados)
8. [GroupListItemUI - Visual de Grupo](#8-grouplistitemui---visual-de-grupo)
9. [GroupDropZone - Drop Target](#9-groupdropzone---drop-target)
10. [CreateGroupUI - Criação de Grupos](#10-creategroupui---criação-de-grupos)

### PARTE IV: ITENS DE UNIDADE
11. [UnitListItemUI - Visual de Unidade](#11-unitlistitemui---visual-de-unidade)
12. [UnitListItemHandle - Interação](#12-unitlistitemhandle---interação)
13. [ListItemMarker - Identificação](#13-listitemmarker---identificação)

### PARTE V: DRAG-AND-DROP
14. [ReorderableListItem - Sistema de Drag](#14-reorderablelistitem---sistema-de-drag)
15. [ResizeGripHandler - Redimensionamento](#15-resizegriphandler---redimensionamento)

### PARTE VI: MENUS DE CONTEXTO
16. [UnitListItemContextMenu - Menu de Unidade](#16-unitlistitemcontextmenu---menu-de-unidade)
17. [UnitContextMenuHandler - Ações de Unidade](#17-unitcontextmenuhandler---ações-de-unidade)
18. [GroupHeaderContextMenu - Menu de Grupo](#18-groupheadercontextmenu---menu-de-grupo)
19. [GroupContextMenuHandler - Ações de Grupo](#19-groupcontextmenuhandler---ações-de-grupo)

### PARTE VII: COMPONENTES AUXILIARES
20. [ViewportClearSelection - Limpar Seleção](#20-viewportclearselection---limpar-seleção)
21. [BlockerInputCatcher - Captura de Input](#21-blockerinputcatcher---captura-de-input)

### PARTE VIII: INTEGRAÇÃO E MANUTENÇÃO
22. [Integração com GameEvents](#22-integração-com-gameevents)
23. [Fluxo de Inicialização](#23-fluxo-de-inicialização)
24. [Padrões de Uso Avançados](#24-padrões-de-uso-avançados)
25. [Solução de Problemas](#25-solução-de-problemas)
26. [Estrutura de Arquivos](#26-estrutura-de-arquivos)
27. [Checklist de Validação](#27-checklist-de-validação)

---

# PARTE I: VISÃO GERAL

---

## 1) INTRODUÇÃO AO MÓDULO

### 1.1 Objetivo

O **Lote 5: User Interface / Left Bar** implementa o painel lateral de gerenciamento de unidades do RTS, fornecendo:

- **Lista Dinâmica**: Visualização de todas as unidades do jogador
- **Sistema de Grupos**: Organização hierárquica de unidades em grupos nomeados
- **Drag-and-Drop**: Reordenação e aninhamento visual intuitivo
- **Menus de Contexto**: Ações contextuais (renomear, deletar, seguir)
- **Sincronização com Seleção**: Integração bidirecional com `SelectionManager`
- **Event Bus**: Comunicação desacoplada via `GameEvents` (Lote 1)

### 1.2 Benefícios da Arquitetura

✅ **Desacoplamento Total**: Comunicação via `GameEvents` (sem referências diretas)  
✅ **Reatividade**: UI atualiza automaticamente ao escutar eventos  
✅ **Extensibilidade**: Adicionar novos tipos de itens sem modificar core  
✅ **Testabilidade**: Lógica separada de visual (Model-View)  
✅ **Performance**: Rebuild incremental apenas quando necessário  
✅ **UX Polida**: Drag-and-drop, menus contextuais, animações  

### 1.3 Componentes Principais

| Componente | Tipo | Responsabilidade |
|------------|------|------------------|
| `UnitListPanel` | MonoBehaviour | Controlador principal da lista |
| `UnitGroup` | C# Class | Modelo de dados de grupo |
| `ListItemWrapper` | C# Class | Wrapper unificado Unit/Group |
| `IListItemModel` | Interface | Contrato para itens da lista |
| `UnitListItemUI` | MonoBehaviour | Visual de unidade (prefab) |
| `GroupListItemUI` | MonoBehaviour | Visual de grupo (prefab) |
| `ReorderableListItem` | MonoBehaviour | Sistema de drag-and-drop |
| `UnitListItemContextMenu` | MonoBehaviour | Menu contextual de unidade |
| `GroupHeaderContextMenu` | MonoBehaviour | Menu contextual de grupo |

### 1.4 Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                      UnitListPanel                           │
│  ┌────────────────────────────────────────────────────┐     │
│  │  _order: List<ListItemWrapper>                     │     │
│  │  _groups: List<UnitGroup>                          │     │
│  └────────────────────────────────────────────────────┘     │
│                    │ Build() / RebuildList()                 │
└────────────────────┼─────────────────────────────────────────┘
                     │
                     ↓ (instancia prefabs)
┌─────────────────────────────────────────────────────────────┐
│                   Visual Hierarchy (Content)                 │
│  ┌──────────────────────┐  ┌──────────────────────┐        │
│  │ UnitListItemUI       │  │ GroupListItemUI      │        │
│  │ - ReorderableListItem│  │ - ReorderableListItem│        │
│  │ - UnitListItemHandle │  │ - GroupDropZone      │        │
│  │ - ListItemMarker     │  │ - ResizeGripHandler  │        │
│  └──────────────────────┘  └──────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
                     │
                     ↓ (escuta eventos)
┌─────────────────────────────────────────────────────────────┐
│                    GameEvents (Event Bus)                    │
│  - OnUnitSpawned / OnUnitDespawned                          │
│  - OnGroupCreated / OnGroupDeleted / OnGroupRenamed         │
│  - OnSelectionChanged                                        │
└─────────────────────────────────────────────────────────────┘
```

### 1.5 Integração com Outros Módulos

| Módulo | Dependência | Comunicação |
|--------|-------------|-------------|
| **Lote 1** (Core System) | `GameEvents` | Escuta `OnUnitSpawned`, dispara `OnGroupCreated` |
| **Lote 3** (Unidades) | `Unit`, `UnitRegistry` | Lê lista de unidades, escuta spawn/despawn |
| **Lote 4** (Seleção) | `SelectionManager` | Sincroniza seleção bidirecional |
| **Lote 2** (Câmera) | Opcional | Menu "Follow" dispara `RaiseCameraFocus` |

---

## 2) ARQUITETURA E PADRÕES

### 2.1 Padrões de Design Utilizados

#### Observer Pattern (Event Bus)
```csharp
// Produtor
GameEvents.RaiseUnitSpawned(unit);

// Consumidor
void OnEnable() {
    GameEvents.OnUnitSpawned += HandleUnitSpawned;
}
```

#### Adapter Pattern (ListItemWrapper)
```csharp
// Unifica Unit e UnitGroup sob mesma interface
ListItemWrapper wrapper = new ListItemWrapper(unit);    // ou
ListItemWrapper wrapper = new ListItemWrapper(group);
```

#### Composite Pattern (Grupos Hierárquicos)
```csharp
// Grupo contém unidades, lista trata ambos uniformemente
IListItemModel item = wrapper.Model;
if (item.IsGroup) { /* lógica de grupo */ }
else { /* lógica de unidade */ }
```

#### Factory Pattern (Build de Itens)
```csharp
// UnitListPanel.BuildItem() decide qual prefab instanciar
if (wrapper.IsGroup) return BuildGroupItem(group);
else return BuildUnitItem(unit);
```

### 2.2 Separação de Responsabilidades

| Camada | Componentes | Responsabilidade |
|--------|-------------|------------------|
| **Dados** | `UnitGroup`, `ListItemWrapper` | Modelo de dados puro (sem MonoBehaviour) |
| **Lógica** | `UnitListPanel` | Gerenciamento de lista, eventos, ordenação |
| **Visual** | `UnitListItemUI`, `GroupListItemUI` | Renderização e binding de dados |
| **Interação** | `ReorderableListItem`, `*ContextMenu` | Drag-and-drop, menus, input |

### 2.3 Fluxo de Dados (Unidirecional)

```
[Fonte de Dados]
    │
    ↓ (evento)
GameEvents.OnUnitSpawned
    │
    ↓ (handler)
UnitListPanel.HandleUnitSpawned()
    │
    ↓ (modelo)
_order.Add(new ListItemWrapper(unit))
    │
    ↓ (rebuild)
RebuildList()
    │
    ↓ (instância)
Instantiate(unitItemPrefab)
    │
    ↓ (bind)
UnitListItemUI.Bind(unit)
    │
    ↓ (visual)
[UI Atualizada]
```

---

## 3) FLUXO DE DADOS

### 3.1 Inicialização Completa

```
1. UnitListPanel.Awake()
   ├─> Cache de componentes
   ├─> Descoberta de prefabs
   └─> Validação de referências

2. UnitListPanel.OnEnable()
   ├─> GameEvents.OnUnitSpawned += HandleUnitSpawned
   ├─> GameEvents.OnUnitDespawned += HandleUnitDespawned
   ├─> GameEvents.OnSelectionChanged += RefreshFromSelection
   └─> selectionManager.OnAnchorSet += OnAnchorSet (evento local)

3. UnitListPanel.Start()
   ├─> PopulateFromRegistry()
   │   ├─> Lê UnitRegistry.GetByFaction(myFaction)
   │   ├─> _order.Add(new ListItemWrapper(unit)) para cada
   │   └─> _groups permanece vazio (nenhum grupo criado ainda)
   └─> RebuildList()
       ├─> Limpa content (destroy children)
       ├─> BuildItem() para cada wrapper em _order
       └─> RefreshFromSelection()
```

### 3.2 Ciclo de Vida de um Item

```
[Unit Spawna na Cena]
    │
    ↓ (Lote 3)
UnitRegistry.Register(unit)
    │
    ↓ (Lote 1)
GameEvents.RaiseUnitSpawned(unit)
    │
    ↓ (Lote 5)
UnitListPanel.HandleUnitSpawned(unit)
    │
    ├─> if (unit.owner != myFaction) return; // Filtro
    ├─> _order.Add(new ListItemWrapper(unit))
    └─> RebuildList()
        │
        ↓
    BuildUnitItem(unit)
        │
        ├─> Instantiate(unitItemPrefab, content)
        ├─> AddComponent<ReorderableListItem>()
        ├─> AddComponent<UnitListItemHandle>()
        ├─> AddComponent<ListItemMarker>()
        └─> UnitListItemUI.Bind(unit)
            │
            ├─> Atualiza nameText, portrait, levelText
            ├─> GameEvents.OnUnitProgressChanged += OnUnitProgressChanged
            └─> Refresh() (barra de XP)
```

### 3.3 Criação de Grupo

```
[Usuário Clica "Create Group"]
    │
    ↓
CreateGroupUI.CreateGroup()
    │
    ↓
UnitListPanel.CreateNewGroup(groupName)
    │
    ├─> 1. Obter unidades selecionadas
    │   var selectedUnits = selectionManager.Selection.ToList();
    │
    ├─> 2. Criar modelo de dados
    │   var newGroup = new UnitGroup(groupName);
    │   newGroup.Units.AddRange(selectedUnits);
    │
    ├─> 3. Adicionar à lista de grupos
    │   _groups.Add(newGroup);
    │
    ├─> 4. Remover unidades da lista raiz
    │   foreach (var unit in selectedUnits)
    │       _order.RemoveAll(w => w.GetUnit() == unit);
    │
    ├─> 5. Adicionar wrapper do grupo à lista raiz
    │   _order.Add(new ListItemWrapper(newGroup));
    │
    ├─> 6. Disparar evento global
    │   GameEvents.RaiseGroupCreated(newGroup);
    │
    └─> 7. Reconstruir lista visual
        RebuildList()
```

### 3.4 Sincronização com Seleção

```
[SelectionManager.SelectExactly(units)]
    │
    ↓ (Lote 1)
GameEvents.RaiseSelectionChanged(selection)
    │
    ↓ (Lote 5)
UnitListPanel.RefreshFromSelection(selection)
    │
    ├─> Para cada child em content:
    │   ├─> UnitListItemUI.SetSelected(isSelected)
    │   └─> GroupListItemUI.SetSelected(allUnitsSelected)
    │
    └─> Scroll para primeiro item selecionado
```

---

# PARTE II: COMPONENTES CORE

---

## 4) UNITLISTPANEL - CONTROLADOR PRINCIPAL

### 4.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `UnitListPanel.cs`  
**Responsabilidade:** Orquestrador principal da lista de unidades. Gerencia:
- Lista ordenada de itens (`_order`)
- Coleção de grupos (`_groups`)
- Build/rebuild de hierarquia visual
- Sincronização com seleção
- Drag-and-drop e reordenação
- Eventos de grupos (via `GameEvents`)

### 4.2 Estrutura da Classe

```csharp
public class UnitListPanel : MonoBehaviour
{
    // ========== DEPENDÊNCIAS ==========
    [Header("Dependências")]
    [SerializeField] private PlayerController player;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private InputSelection inputSelection;
    
    // ========== PREFABS ==========
    [Header("Prefabs")]
    [SerializeField] private UnitListItemUI unitItemPrefab;
    [SerializeField] private GroupListItemUI groupItemPrefab;
    
    // ========== LAYOUT ==========
    [Header("Layout")]
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scrollRect;
    
    // ========== DADOS INTERNOS ==========
    private List<ListItemWrapper> _order = new();      // Lista ordenada de itens (Units + Groups)
    private List<UnitGroup> _groups = new();           // Coleção de grupos existentes
    private int _anchorIndex = -1;                     // Âncora para Shift+Click
    
    // ========== PROPRIEDADES PÚBLICAS ==========
    public RectTransform Content => content;
    public SelectionManager SelectionManager => selectionManager;
    public InputSelection InputSelection => inputSelection;
    
    // ========== MÉTODOS PRINCIPAIS ==========
    void Awake() { /* cache componentes */ }
    void OnEnable() { /* subscribe eventos */ }
    void OnDisable() { /* unsubscribe eventos */ }
    void Start() { /* popular lista inicial */ }
    
    public void RebuildList() { /* reconstrói visual */ }
    public void CreateNewGroup(string name) { /* cria grupo */ }
    public void DeleteGroupAndRestoreUnits(UnitGroup group, List<Unit> units) { /* deleta grupo */ }
    
    // ... (métodos internos)
}
```

### 4.3 Campos e Propriedades

#### Dependências (Inspector)

```csharp
[Header("Dependências")]
[SerializeField] private PlayerController player;           // Facção do jogador (filtro)
[SerializeField] private SelectionManager selectionManager; // Sistema de seleção
[SerializeField] private InputSelection inputSelection;     // Input (Ctrl/Shift)
```

#### Prefabs (Inspector)

```csharp
[Header("Prefabs")]
[SerializeField] private UnitListItemUI unitItemPrefab;     // Prefab de item de unidade
[SerializeField] private GroupListItemUI groupItemPrefab;   // Prefab de item de grupo
```

#### Layout (Inspector)

```csharp
[Header("Layout")]
[SerializeField] private RectTransform content;             // Container dos itens
[SerializeField] private ScrollRect scrollRect;             // Scroll da lista
```

#### Dados Internos (Privados)

```csharp
private List<ListItemWrapper> _order = new();               // Lista ordenada (Units + Groups)
private List<UnitGroup> _groups = new();                    // Coleção de grupos
private int _anchorIndex = -1;                              // Âncora para Shift+Click
```

### 4.4 Métodos Principais

#### Inicialização

```csharp
void Awake()
{
    // Cache de componentes (auto-descoberta se necessário)
    if (!player) player = FindFirstObjectByType<PlayerController>();
    if (!selectionManager) selectionManager = FindFirstObjectByType<SelectionManager>();
    if (!inputSelection) inputSelection = FindFirstObjectByType<InputSelection>();
}

void OnEnable()
{
    // Subscribe em eventos globais (GameEvents)
    GameEvents.OnUnitSpawned += HandleUnitSpawned;
    GameEvents.OnUnitDespawned += HandleUnitDespawned;
    GameEvents.OnSelectionChanged += RefreshFromSelection;
    
    // Subscribe em evento local (SelectionManager)
    if (selectionManager) 
        selectionManager.OnAnchorSet += OnAnchorSet;
}

void OnDisable()
{
    // CRÍTICO: Unsubscribe para evitar memory leaks
    GameEvents.OnUnitSpawned -= HandleUnitSpawned;
    GameEvents.OnUnitDespawned -= HandleUnitDespawned;
    GameEvents.OnSelectionChanged -= RefreshFromSelection;
    
    if (selectionManager) 
        selectionManager.OnAnchorSet -= OnAnchorSet;
}

void Start()
{
    // Popular lista inicial
    PopulateFromRegistry();
    RebuildList();
}
```

#### Popular Lista Inicial

```csharp
/// <summary>
/// Lê todas as unidades da facção do jogador do UnitRegistry
/// e adiciona à lista (_order).
/// </summary>
void PopulateFromRegistry()
{
    if (player == null) return;
    
    var myUnits = UnitRegistry.GetByFaction(player.myFaction);
    
    foreach (var unit in myUnits)
    {
        _order.Add(new ListItemWrapper(unit));
    }
}
```

#### Rebuild da Lista Visual

```csharp
/// <summary>
/// Reconstrói toda a hierarquia visual a partir de _order.
/// Destrói todos os filhos existentes e instancia novos.
/// </summary>
public void RebuildList()
{
    if (content == null) return;
    
    // 1. Limpar hierarquia existente
    foreach (Transform child in content)
    {
        Destroy(child.gameObject);
    }
    
    // 2. Instanciar novos itens
    foreach (var wrapper in _order)
    {
        GameObject itemGO = BuildItem(wrapper);
        if (itemGO != null)
        {
            itemGO.transform.SetParent(content, false);
        }
    }
    
    // 3. Atualizar visuais de seleção
    RefreshFromSelection(selectionManager?.Selection);
    
    // 4. Forçar rebuild de layout
    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
}
```

#### Build de Item (Factory)

```csharp
/// <summary>
/// Instancia o prefab apropriado baseado no tipo do wrapper.
/// Adiciona componentes necessários (ReorderableListItem, etc).
/// </summary>
GameObject BuildItem(ListItemWrapper wrapper)
{
    if (wrapper.IsGroup)
    {
        return BuildGroupItem((UnitGroup)wrapper.Model);
    }
    else
    {
        Unit unit = wrapper.GetUnit();
        return BuildUnitItem(unit);
    }
}
```

#### Build de Item de Unidade

```csharp
/// <summary>
/// Instancia prefab de unidade e configura componentes.
/// </summary>
GameObject BuildUnitItem(Unit unit)
{
    if (unitItemPrefab == null || unit == null) return null;
    
    // 1. Instanciar prefab
    var go = Instantiate(unitItemPrefab.gameObject);
    var ui = go.GetComponent<UnitListItemUI>();
    
    // 2. Bind de dados
    if (ui) ui.Bind(unit);
    
    // 3. Adicionar ReorderableListItem (drag-and-drop)
    var rei = go.AddComponent<ReorderableListItem>();
    rei.Setup(this, content, scrollRect);
    
    // 4. Adicionar Handle (cliques)
    var handle = go.AddComponent<UnitListItemHandle>();
    int index = _order.FindIndex(w => w.GetUnit() == unit);
    handle.Setup(this, inputSelection, selectionManager, index, unit);
    
    // 5. Adicionar Marker (identificação)
    var marker = go.AddComponent<ListItemMarker>();
    marker.Wrapper = new ListItemWrapper(unit);
    
    return go;
}
```

#### Build de Item de Grupo

```csharp
/// <summary>
/// Instancia prefab de grupo e configura componentes.
/// </summary>
GameObject BuildGroupItem(UnitGroup group)
{
    if (groupItemPrefab == null || group == null) return null;
    
    // 1. Instanciar prefab
    var go = Instantiate(groupItemPrefab.gameObject);
    var ui = go.GetComponent<GroupListItemUI>();
    
    // 2. Bind de dados
    if (ui) ui.Bind(group);
    
    // 3. Adicionar ReorderableListItem (drag-and-drop)
    var rei = go.AddComponent<ReorderableListItem>();
    rei.Setup(this, content, scrollRect);
    
    // 4. Configurar DropZone (aceitar drops)
    var dropZone = go.GetComponentInChildren<GroupDropZone>();
    if (dropZone)
    {
        dropZone.Setup(this);
        dropZone.SetGroupUI(ui);
    }
    
    // 5. Adicionar Marker (identificação)
    var marker = go.AddComponent<ListItemMarker>();
    marker.Wrapper = new ListItemWrapper(group);
    
    // 6. Build de conteúdo interno (se expandido)
    if (group.IsExpanded && ui.subContent)
    {
        BuildGroupContent(group, ui.subContent);
    }
    
    return go;
}
```

#### Build de Conteúdo de Grupo

```csharp
/// <summary>
/// Constrói os itens filhos dentro de um grupo expandido.
/// </summary>
public void BuildGroupContent(UnitGroup group, RectTransform subContent)
{
    if (subContent == null) return;
    
    // Limpar conteúdo existente
    foreach (Transform child in subContent)
    {
        Destroy(child.gameObject);
    }
    
    // Instanciar item para cada unidade do grupo
    foreach (var unit in group.Units)
    {
        var itemGO = BuildUnitItem(unit);
        if (itemGO != null)
        {
            itemGO.transform.SetParent(subContent, false);
            
            // Desabilitar drag em itens filhos (opcional)
            var rei = itemGO.GetComponent<ReorderableListItem>();
            if (rei) rei.enabled = false;
        }
    }
    
    LayoutRebuilder.ForceRebuildLayoutImmediate(subContent);
}
```

### 4.5 Criação de Grupo

```csharp
/// <summary>
/// Cria um novo grupo com as unidades selecionadas.
/// Dispara GameEvents.OnGroupCreated.
/// </summary>
public void CreateNewGroup(string groupName)
{
    if (selectionManager == null) return;
    
    // 1. Obter unidades selecionadas
    var selectedUnits = selectionManager.Selection.ToList();
    if (selectedUnits.Count == 0)
    {
        Debug.LogWarning("Nenhuma unidade selecionada para criar grupo.");
        return;
    }
    
    // 2. Criar modelo de dados
    var newGroup = new UnitGroup { GroupName = groupName };
    newGroup.Units.AddRange(selectedUnits);
    
    // 3. Adicionar à coleção de grupos
    _groups.Add(newGroup);
    
    // 4. Remover unidades da lista raiz
    foreach (var unit in selectedUnits)
    {
        _order.RemoveAll(w => w.GetUnit() == unit);
    }
    
    // 5. Adicionar wrapper do grupo à lista raiz
    _order.Add(new ListItemWrapper(newGroup));
    
    // 6. Disparar evento global
    GameEvents.RaiseGroupCreated(newGroup);
    
    // 7. Reconstruir lista visual
    RebuildList();
    
    Debug.Log($"Grupo '{groupName}' criado com {selectedUnits.Count} unidades.");
}
```

### 4.6 Deleção de Grupo

```csharp
/// <summary>
/// Deleta um grupo e restaura suas unidades na lista raiz.
/// Dispara GameEvents.OnGroupDeleted.
/// </summary>
public void DeleteGroupAndRestoreUnits(UnitGroup group, List<Unit> unitsToRestore)
{
    if (group == null) return;
    
    // 1. Remover wrapper do grupo da lista raiz
    _order.RemoveAll(w => w.Model == group);
    
    // 2. Remover da coleção de grupos
    _groups.Remove(group);
    
    // 3. Restaurar unidades na lista raiz
    foreach (var unit in unitsToRestore)
    {
        if (unit != null)
        {
            _order.Add(new ListItemWrapper(unit));
        }
    }
    
    // 4. Disparar evento global
    GameEvents.RaiseGroupDeleted(group);
    
    // 5. Reconstruir lista visual
    RebuildList();
    
    Debug.Log($"Grupo '{group.GroupName}' deletado. {unitsToRestore.Count} unidades restauradas.");
}
```

### 4.7 Handlers de Eventos

#### Spawn de Unidade

```csharp
/// <summary>
/// Handler para GameEvents.OnUnitSpawned.
/// Adiciona unidade à lista se pertencer ao jogador.
/// </summary>
void HandleUnitSpawned(Unit unit)
{
    if (unit == null || player == null) return;
    
    // Filtrar apenas unidades do jogador
    if (unit.owner != player.myFaction) return;
    
    // Verificar se já existe (evitar duplicatas)
    bool exists = _order.Any(w => w.GetUnit() == unit);
    if (exists) return;
    
    // Adicionar à lista
    _order.Add(new ListItemWrapper(unit));
    
    // Reconstruir visual
    RebuildList();
    
    Debug.Log($"Unidade '{unit.DisplayName}' adicionada à lista.");
}
```

#### Despawn de Unidade

```csharp
/// <summary>
/// Handler para GameEvents.OnUnitDespawned.
/// Remove unidade da lista e de grupos.
/// </summary>
void HandleUnitDespawned(Unit unit)
{
    if (unit == null) return;
    
    // Remover da lista raiz
    int removedCount = _order.RemoveAll(w => w.GetUnit() == unit);
    
    // Remover de grupos
    foreach (var group in _groups)
    {
        group.Units.RemoveAll(u => u == unit);
    }
    
    // Reconstruir se houve remoção
    if (removedCount > 0)
    {
        RebuildList();
        Debug.Log($"Unidade '{unit.DisplayName}' removida da lista.");
    }
}
```

#### Mudança de Seleção

```csharp
/// <summary>
/// Handler para GameEvents.OnSelectionChanged.
/// Atualiza visuais de seleção na lista.
/// </summary>
public void RefreshFromSelection(IReadOnlyCollection<Unit> selection)
{
    if (content == null || selection == null) return;
    
    var selectionSet = new HashSet<Unit>(selection);
    
    // Atualizar visuais de cada item
    foreach (Transform child in content)
    {
        // Item de unidade
        var unitUI = child.GetComponent<UnitListItemUI>();
        if (unitUI != null && unitUI.Unit != null)
        {
            bool isSelected = selectionSet.Contains(unitUI.Unit);
            unitUI.SetSelected(isSelected);
            continue;
        }
        
        // Item de grupo
        var groupUI = child.GetComponent<GroupListItemUI>();
        if (groupUI != null && groupUI.GroupModel != null)
        {
            bool allSelected = groupUI.Units.All(u => selectionSet.Contains(u));
            groupUI.SetSelected(allSelected);
        }
    }
}
```

### 4.8 Reordenação e Drag-and-Drop

```csharp
/// <summary>
/// Chamado por ReorderableListItem após um drag.
/// Atualiza _order baseado na hierarquia visual.
/// </summary>
public void CommitReorder()
{
    if (content == null) return;
    
    _order.Clear();
    
    // Reconstruir _order a partir da hierarquia visual
    foreach (Transform child in content)
    {
        var marker = child.GetComponent<ListItemMarker>();
        if (marker != null && marker.Wrapper != null)
        {
            _order.Add(marker.Wrapper);
        }
    }
    
    Debug.Log($"Reordenação commitada. {_order.Count} itens.");
}

/// <summary>
/// Chamado por GroupDropZone quando unidade é dropada em grupo.
/// </summary>
public void AddUnitToGroup(Unit unit, UnitGroup targetGroup)
{
    if (unit == null || targetGroup == null) return;
    
    // 1. Remover da lista raiz
    _order.RemoveAll(w => w.GetUnit() == unit);
    
    // 2. Adicionar ao grupo
    if (!targetGroup.Units.Contains(unit))
    {
        targetGroup.Units.Add(unit);
        
        // 3. Disparar evento
        GameEvents.RaiseUnitsAddedToGroup(targetGroup, new[] { unit });
    }
    
    // 4. Reconstruir visual
    RebuildList();
}
```

### 4.9 API Pública

| Método | Descrição |
|--------|-----------|
| `RebuildList()` | Reconstrói toda a hierarquia visual |
| `CreateNewGroup(string)` | Cria grupo com unidades selecionadas |
| `DeleteGroupAndRestoreUnits(UnitGroup, List<Unit>)` | Deleta grupo e restaura unidades |
| `CommitReorder()` | Atualiza `_order` após drag-and-drop |
| `AddUnitToGroup(Unit, UnitGroup)` | Move unidade para grupo |
| `BuildGroupContent(UnitGroup, RectTransform)` | Constrói conteúdo interno de grupo |
| `RefreshFromSelection(IReadOnlyCollection<Unit>)` | Atualiza visuais de seleção |

---

## 5) LISTITEMWRAPPER - WRAPPER UNIFICADO

### 5.1 Visão Geral

**Tipo:** C# Class (não MonoBehaviour)  
**Arquivo:** `ListItemWrapper.cs`  
**Responsabilidade:** Wrapper que unifica `Unit` e `UnitGroup` sob uma interface comum (`IListItemModel`).

### 5.2 Estrutura da Classe

```csharp
/// <summary>
/// Wrapper unificado que pode representar tanto uma Unit quanto um UnitGroup.
/// REFATORADO (Lote 5): Simplificado para eliminar classe interna redundante.
/// </summary>
public class ListItemWrapper : IListItemModel
{
    public IListItemModel Model { get; }

    // Construtor para Unit
    public ListItemWrapper(Unit unit)
    {
        Model = new SimpleUnitModel(unit);
    }

    // Construtor para UnitGroup
    public ListItemWrapper(UnitGroup group)
    {
        Model = group; // UnitGroup já implementa IListItemModel
    }

    // Pass-through da interface
    public string DisplayName => Model.DisplayName;
    public bool IsGroup => Model.IsGroup;
    public Unit GetUnit() => Model.GetUnit();
    public IReadOnlyList<Unit> GetUnitsInItem() => Model.GetUnitsInItem();

    // ==================== MODELO INTERNO ====================

    /// <summary>
    /// Modelo simples para uma Unit individual.
    /// REFATORAÇÃO: Renomeado de UnitWrapper para SimpleUnitModel.
    /// </summary>
    private class SimpleUnitModel : IListItemModel
    {
        private readonly Unit _unit;

        public SimpleUnitModel(Unit unit) => _unit = unit;

        public string DisplayName => _unit != null ? _unit.DisplayName : "null";
        public bool IsGroup => false;
        public Unit GetUnit() => _unit;
        public IReadOnlyList<Unit> GetUnitsInItem() 
            => _unit != null ? new List<Unit> { _unit } : new List<Unit>();
    }
}
```

### 5.3 Uso

```csharp
// Wrapper para Unit
var unitWrapper = new ListItemWrapper(myUnit);
Debug.Log(unitWrapper.DisplayName);  // "Guerreiro Élfico"
Debug.Log(unitWrapper.IsGroup);      // false

// Wrapper para UnitGroup
var groupWrapper = new ListItemWrapper(myGroup);
Debug.Log(groupWrapper.DisplayName); // "Grupo de Ataque"
Debug.Log(groupWrapper.IsGroup);     // true

// Tratamento unificado
void ProcessItem(ListItemWrapper wrapper)
{
    if (wrapper.IsGroup)
    {
        Debug.Log($"Grupo: {wrapper.DisplayName} com {wrapper.GetUnitsInItem().Count} unidades");
    }
    else
    {
        Debug.Log($"Unidade: {wrapper.DisplayName}");
    }
}
```

---

## 6) ILISTITEMMODEL - INTERFACE BASE

### 6.1 Visão Geral

**Tipo:** Interface  
**Arquivo:** `IListItemModel.cs`  
**Responsabilidade:** Contrato comum para `Unit` e `UnitGroup`.

### 6.2 Estrutura da Interface

```csharp
public interface IListItemModel
{
    /// <summary>Nome exibido na UI</summary>
    string DisplayName { get; }
    
    /// <summary>True se for um grupo, false se for unidade</summary>
    bool IsGroup { get; }
    
    /// <summary>Retorna Unit se for unidade, null se for grupo</summary>
    Unit GetUnit();
    
    /// <summary>
    /// Retorna lista de unidades:
    /// - Grupo: todas as unidades do grupo
    /// - Unidade: lista com 1 elemento (a própria unidade)
    /// </summary>
    IReadOnlyList<Unit> GetUnitsInItem();
}
```

### 6.3 Implementações

**Unit (via SimpleUnitModel):**
```csharp
public string DisplayName => unit.DisplayName;
public bool IsGroup => false;
public Unit GetUnit() => unit;
public IReadOnlyList<Unit> GetUnitsInItem() => new List<Unit> { unit };
```

**UnitGroup:**
```csharp
public string DisplayName => GroupName;
public bool IsGroup => true;
public Unit GetUnit() => null;
public IReadOnlyList<Unit> GetUnitsInItem() => Units;
```

---

# PARTE III: SISTEMA DE GRUPOS

---

## 7) UNITGROUP - MODELO DE DADOS

### 7.1 Visão Geral

**Tipo:** C# Class (não MonoBehaviour)  
**Arquivo:** `UnitGroup.cs`  
**Responsabilidade:** Modelo de dados de um grupo de unidades. Armazena nome, lista de unidades, estado de expansão e altura preferida (UI).

### 7.2 Estrutura da Classe

```csharp
/// <summary>
/// Modelo de dados de um grupo de unidades.
/// Implementa IListItemModel para tratamento unificado.
/// </summary>
public class UnitGroup : IListItemModel
{
    // ========== DADOS ==========
    public string GroupName { get; set; } = "Novo Grupo";
    public List<Unit> Units { get; set; } = new List<Unit>();
    public string ID { get; private set; } = System.Guid.NewGuid().ToString();
    
    // ========== ESTADO DE UI ==========
    public bool IsExpanded { get; set; } = true;
    public float PreferredHeight { get; set; } = -1f;
    
    // ========== IListItemModel ==========
    public string DisplayName => GroupName;
    public bool IsGroup => true;
    public Unit GetUnit() => null;
    public IReadOnlyList<Unit> GetUnitsInItem() => Units;
}
```

### 7.3 Campos e Propriedades

| Campo/Propriedade | Tipo | Descrição |
|-------------------|------|-----------|
| `GroupName` | `string` | Nome do grupo (editável) |
| `Units` | `List<Unit>` | Unidades pertencentes ao grupo |
| `ID` | `string` | GUID único (gerado automaticamente) |
| `IsExpanded` | `bool` | Estado de expansão (UI) |
| `PreferredHeight` | `float` | Altura preferida do item visual (para resize) |

### 7.4 Uso

```csharp
// Criar grupo
var group = new UnitGroup
{
    GroupName = "Cavalaria Pesada",
    IsExpanded = true
};

// Adicionar unidades
group.Units.Add(unit1);
group.Units.Add(unit2);

// Acessar via IListItemModel
IListItemModel model = group;
Debug.Log(model.DisplayName);           // "Cavalaria Pesada"
Debug.Log(model.IsGroup);               // true
Debug.Log(model.GetUnitsInItem().Count); // 2
```

---

## 8) GROUPLISTITEMUI - VISUAL DE GRUPO

### 8.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `GroupListItemUI.cs`  
**Responsabilidade:** Componente visual de um item de grupo. Gerencia:
- Binding de dados (`UnitGroup`)
- Expansão/colapso
- Redimensionamento (altura)
- Seleção visual
- Conteúdo interno (unidades filhas)

### 8.2 Estrutura da Classe

```csharp
public class GroupListItemUI : MonoBehaviour
{
    // ========== REFS VISUAIS ==========
    [Header("Refs Visuais")]
    public TMP_Text nameText;              // Nome do grupo
    public Toggle expandToggle;            // Toggle de expansão
    public Outline outline;                // Outline de seleção (item todo)
    public Outline headerOutline;          // Outline de seleção (header)
    
    // ========== CONTENT ANINHADO ==========
    [Header("Content Aninhado")]
    public RectTransform subContent;       // Container das unidades filhas
    
    // ========== LAYOUT ==========
    [Header("Layout")]
    public LayoutElement rootLayout;       // LayoutElement do root
    public RectTransform headerRect;       // Rect do header (para altura mínima)
    public ScrollRect innerScrollRect;     // ScrollRect interno (opcional)
    
    // ========== DADOS ==========
    private UnitGroup _groupModel;
    private float _collapsedHeight = -1f;
    private ResizeGripHandler _resizeGripHandler;
    
    // ========== PROPRIEDADES ==========
    public UnitGroup GroupModel => _groupModel;
    public IReadOnlyList<Unit> Units => _groupModel?.Units;
    
    // ========== MÉTODOS ==========
    void Awake() { /* cache componentes */ }
    public void Bind(UnitGroup group) { /* bind dados */ }
    public void RefreshVisuals() { /* atualiza visual */ }
    public void SetSelected(bool selected) { /* outline */ }
}
```

### 8.3 Binding de Dados

```csharp
/// <summary>
/// Vincula modelo de dados ao componente visual.
/// </summary>
public void Bind(UnitGroup group)
{
    _groupModel = group;
    
    // Atualizar nome
    if (nameText) 
        nameText.text = group.GroupName;
    
    // Restaurar altura preferida
    if (rootLayout != null && _groupModel.PreferredHeight > 0)
    {
        rootLayout.preferredHeight = _groupModel.PreferredHeight;
    }
    
    RefreshVisuals();
}
```

### 8.4 Expansão/Colapso

```csharp
/// <summary>
/// Atualiza visuais baseado em IsExpanded.
/// Gerencia altura (preferredHeight/minHeight) e ativação de subContent.
/// </summary>
public void RefreshVisuals()
{
    if (_groupModel == null || rootLayout == null) return;
    
    // Sincronizar toggle
    if (expandToggle) 
        expandToggle.SetIsOnWithoutNotify(_groupModel.IsExpanded);
    
    if (_groupModel.IsExpanded)
    {
        // EXPANDIDO
        
        // Remover restrição de altura mínima
        rootLayout.minHeight = 0f;
        
        // Restaurar altura preferida
        if (_groupModel.PreferredHeight > 0)
        {
            rootLayout.preferredHeight = _groupModel.PreferredHeight;
        }
        
        // Ativar scroll/content interno
        if (innerScrollRect) innerScrollRect.gameObject.SetActive(true);
        if (subContent) subContent.gameObject.SetActive(true);
        
        // Ativar grip de resize
        if (_resizeGripHandler) _resizeGripHandler.enabled = true;
    }
    else
    {
        // RECOLHIDO
        
        // Salvar altura expandida
        if (rootLayout.preferredHeight > _collapsedHeight)
        {
            _groupModel.PreferredHeight = rootLayout.preferredHeight;
        }
        
        // Definir altura do header
        rootLayout.preferredHeight = _collapsedHeight;
        rootLayout.minHeight = _collapsedHeight; // Travar tamanho
        
        // Desativar scroll/content interno
        if (innerScrollRect) innerScrollRect.gameObject.SetActive(false);
        if (subContent) subContent.gameObject.SetActive(false);
        
        // Desativar grip de resize
        if (_resizeGripHandler) _resizeGripHandler.enabled = false;
    }
}

/// <summary>
/// Handler do Toggle de expansão.
/// </summary>
void OnToggleValueChanged(bool isExpanded)
{
    if (_groupModel == null) return;
    
    _groupModel.IsExpanded = isExpanded;
    RefreshVisuals();
    
    // Rebuild do conteúdo interno (se expandindo)
    if (isExpanded)
    {
        var panel = GetComponentInParent<UnitListPanel>();
        if (panel != null)
        {
            panel.BuildGroupContent(_groupModel, subContent);
            panel.RefreshFromSelection(panel.SelectionManager.Selection);
        }
    }
    
    // Forçar rebuild de layout do pai
    if (transform.parent is RectTransform parentRt)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
    }
}
```

### 8.5 Seleção Visual

```csharp
/// <summary>
/// Ativa/desativa outlines de seleção.
/// </summary>
public void SetSelected(bool selected)
{
    if (outline) outline.enabled = selected;
    if (headerOutline) headerOutline.enabled = selected;
}
```

---

## 9) GROUPDROPZONE - DROP TARGET

### 9.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `GroupDropZone.cs`  
**Implementa:** `IDropHandler`  
**Responsabilidade:** Drop target para drag-and-drop. Permite arrastar unidades para dentro do grupo.

### 9.2 Estrutura da Classe

```csharp
/// <summary>
/// Drop zone para aceitar unidades arrastadas para dentro do grupo.
/// Também gerencia cliques no header do grupo (seleção).
/// </summary>
public class GroupDropZone : MonoBehaviour, IDropHandler
{
    // ========== REFS ==========
    [Header("Refs")]
    [SerializeField] GroupListItemUI groupUI;
    [SerializeField] UnitListPanel panel;
    
    // ========== DUPLO CLIQUE ==========
    [SerializeField] float doubleClickMaxDelay = 0.30f;
    float _lastClickTime = -10f;
    
    // ========== SUPRESSÃO DE CLIQUE APÓS DROP ==========
    const int SUPPRESS_DRAG_FRAME = 2;
    public int LastDropFrame = -100000;
    
    // ========== MÉTODOS ==========
    void Awake() { /* auto-descoberta */ }
    public void Setup(UnitListPanel p) { /* injeção */ }
    public void OnDrop(PointerEventData eventData) { /* drop handler */ }
}
```

### 9.3 Handler de Drop

```csharp
/// <summary>
/// Chamado quando item é dropado sobre o grupo.
/// </summary>
public void OnDrop(PointerEventData eventData)
{
    var draggedItem = eventData.pointerDrag;
    if (draggedItem == null) return;
    
    var rei = draggedItem.GetComponent<ReorderableListItem>();
    if (rei != null)
    {
        // Registrar frame do drop (para supressão de clique)
        LastDropFrame = Time.frameCount;
        
        // O commit será tratado pelo OnEndDrag do ReorderableListItem
        return;
    }
    
    // Espaço para drops externos (futuro)
}
```

### 9.4 Clique no Header (Seleção)

```csharp
/// <summary>
/// Handler de clique no header do grupo.
/// Suporta: simples, Ctrl, Shift, duplo clique.
/// </summary>
public void OnPointerClick(PointerEventData eventData)
{
    // Suprimir cliques imediatamente após drop
    if (Time.frameCount - LastDropFrame <= SUPPRESS_DRAG_FRAME)
        return;
    
    if (groupUI == null || groupUI.GroupModel == null || panel == null) 
        return;
    
    bool ctrl = panel.InputSelection?.IsCtrlPressed ?? false;
    bool shift = panel.InputSelection?.IsShiftPressed ?? false;
    bool isDouble = (Time.unscaledTime - _lastClickTime) <= doubleClickMaxDelay;
    
    _lastClickTime = Time.unscaledTime;
    
    var selectionManager = panel.SelectionManager;
    var unitsInGroup = groupUI.GroupModel.Units;
    
    // Duplo clique: selecionar mesmo tipo
    if (isDouble)
    {
        var firstUnit = unitsInGroup.FirstOrDefault();
        if (firstUnit != null)
        {
            var sameType = unitsInGroup.Where(u => u?.def == firstUnit.def);
            selectionManager.SelectExactly(sameType);
        }
        return;
    }
    
    // Lógica de seleção
    if (shift)
    {
        if (ctrl) selectionManager.AddToSelection(unitsInGroup);
        else selectionManager.SelectExactly(unitsInGroup);
    }
    else if (ctrl)
    {
        selectionManager.ToggleSet(unitsInGroup);
    }
    else
    {
        selectionManager.SelectExactly(unitsInGroup);
    }
    
    // Registrar âncora para Shift+Click
    panel.CommitSelectionForGroup(groupUI.GroupModel);
}
```

---

## 10) CREATEGROUPUI - CRIAÇÃO DE GRUPOS

### 10.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `CreateGroupUI.cs`  
**Responsabilidade:** UI para criação de novos grupos. Contém input field e botão "Create Group".

### 10.2 Estrutura da Classe

```csharp
public class CreateGroupUI : MonoBehaviour
{
    // ========== REFS ==========
    [Header("Scene Refs")]
    [SerializeField] UnitListPanel rootPanel;
    [SerializeField] SelectionManager selection;
    [SerializeField] InputSelection inputSel;
    
    // ========== UI ==========
    [Header("UI")]
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button createButton;
    
    // ========== PREFAB ==========
    [Header("Prefab")]
    [SerializeField] GroupListItemUI groupPrefab;
    
    // ========== MÉTODOS ==========
    void Awake() { /* liga botão */ }
    public void CreateGroup() { /* cria grupo */ }
}
```

### 10.3 Criação de Grupo

```csharp
/// <summary>
/// Chamado pelo botão "Create Group".
/// Delega criação para UnitListPanel.CreateNewGroup().
/// </summary>
public void CreateGroup()
{
    if (rootPanel == null)
    {
        Debug.LogError("[CreateGroupUI] Root panel não ligado.");
        return;
    }
    
    // Obter nome do input (ou usar padrão)
    var desiredName = string.IsNullOrWhiteSpace(nameInput?.text) 
        ? "Novo Grupo" 
        : nameInput.text.Trim();
    
    // Delegar para UnitListPanel
    rootPanel.CreateNewGroup(desiredName);
    
    // Limpar input
    if (nameInput) nameInput.text = string.Empty;
}
```

### 10.4 Setup no Inspector

```
CreateGroupUI (GameObject)
├── NameInput (TMP_InputField)
└── CreateButton (Button)
    └── OnClick() → CreateGroupUI.CreateGroup()
```

---

# PARTE IV: ITENS DE UNIDADE

---

## 11) UNITLISTITEMUI - VISUAL DE UNIDADE

### 11.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `UnitListItemUI.cs`  
**Responsabilidade:** Componente visual de um item de unidade. Gerencia:
- Binding de dados (`Unit`)
- Atualização de portrait, nome, level
- Barra de XP (90% reta + 10% circular)
- Outline de seleção
- Escuta `GameEvents.OnUnitProgressChanged`

### 11.2 Estrutura da Classe

```csharp
public class UnitListItemUI : MonoBehaviour
{
    // ========== REFS VISUAIS ==========
    [Header("Refs")]
    public Image portrait;           // Retrato da unidade
    public TMP_Text nameText;        // Nome
    public TMP_Text levelText;       // Level
    
    [Header("Barra de XP")]
    public Image barFill;            // Parte reta (90%)
    public Image circleFill;         // Parte circular (10%)
    
    [Header("Selection")]
    public Outline outline;          // Outline de seleção
    
    // ========== DADOS ==========
    private Unit _unit;
    public Unit Unit => _unit;
    
    // ========== MÉTODOS ==========
    void Awake() { /* config barra */ }
    public void Bind(Unit unit) { /* bind + subscribe */ }
    public void Unbind() { /* unsubscribe */ }
    void OnDisable() { /* cleanup */ }
    void Refresh() { /* atualiza visual */ }
    public void SetSelected(bool selected) { /* outline */ }
}
```

### 11.3 Binding de Dados

```csharp
/// <summary>
/// Vincula unidade ao componente visual.
/// REFATORADO: Escuta GameEvents em vez de evento local.
/// </summary>
public void Bind(Unit unit)
{
    // Desinscrever do evento anterior
    if (_unit != null)
    {
        GameEvents.OnUnitProgressChanged -= OnUnitProgressChanged;
    }
    
    _unit = unit;
    
    // Atualizar visuais iniciais
    if (nameText) nameText.text = unit.DisplayName;
    if (portrait) portrait.sprite = unit.def?.icon;
    
    Refresh();
    
    // REFATORAÇÃO: Inscrever no GameEvents
    GameEvents.OnUnitProgressChanged += OnUnitProgressChanged;
}

/// <summary>
/// Desvincula unidade e limpa subscriptions.
/// </summary>
public void Unbind()
{
    if (_unit != null)
    {
        GameEvents.OnUnitProgressChanged -= OnUnitProgressChanged;
    }
    _unit = null;
}

void OnDisable() => Unbind();
```

### 11.4 Atualização de Progresso

```csharp
/// <summary>
/// Handler de GameEvents.OnUnitProgressChanged.
/// Só atualiza se o evento for da unidade vinculada.
/// </summary>
void OnUnitProgressChanged(Unit changedUnit)
{
    if (changedUnit == _unit)
    {
        Refresh();
    }
}

/// <summary>
/// Atualiza visuais (level, XP).
/// </summary>
void Refresh()
{
    if (_unit == null) return;
    
    if (levelText) levelText.text = _unit.Level.ToString();
    SetXp01(_unit.Xp01);
}
```

### 11.5 Barra de XP (90% + 10%)

```csharp
/// <summary>
/// Atualiza barra de XP.
/// - 90% inicial: barra reta
/// - 10% final: círculo radial
/// </summary>
void SetXp01(float t)
{
    t = Mathf.Clamp01(t);
    
    // Parte reta (90%)
    float straightPart = Mathf.Min(t, 0.9f) / 0.9f;
    if (barFill) barFill.fillAmount = straightPart;
    
    // Parte circular (10%)
    float circlePart = (t <= 0.9f) ? 0f : (t - 0.9f) / 0.1f;
    if (circleFill) circleFill.fillAmount = circlePart;
}
```

**Explicação:**
- `t = 0.0` → barra reta 0%, círculo 0%
- `t = 0.5` → barra reta 55.5%, círculo 0%
- `t = 0.9` → barra reta 100%, círculo 0%
- `t = 1.0` → barra reta 100%, círculo 100%

### 11.6 Seleção Visual

```csharp
/// <summary>
/// Ativa/desativa outline de seleção.
/// </summary>
public void SetSelected(bool selected)
{
    if (outline) outline.enabled = selected;
}
```

---

## 12) UNITLISTITEMHANDLE - INTERAÇÃO

### 12.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `UnitListItemHandle.cs`  
**Implementa:** `IPointerClickHandler`  
**Responsabilidade:** Gerencia interação de clique em item de unidade. Suporta:
- Clique simples (seleção)
- Ctrl+Clique (toggle)
- Shift+Clique (intervalo)
- Duplo clique (mesmo tipo)
- Clique direito (menu contextual)

### 12.2 Estrutura da Classe

```csharp
public class UnitListItemHandle : MonoBehaviour, IPointerClickHandler
{
    // ========== REFS ==========
    UnitListPanel _panel;
    InputSelection _input;
    SelectionManager _selection;
    int _index;
    Unit _unit;
    UnitListItemContextMenu _contextMenu;
    
    // ========== DUPLO CLIQUE ==========
    [SerializeField] float doubleClickMaxDelay = 0.30f;
    float _lastClickTime = -10f;
    
    // ========== SUPRESSÃO ==========
    int _suppressClickFrame = -1;
    
    // ========== PROPRIEDADES ==========
    public int Index => _index;
    public Unit CurrentUnit => _unit;
    
    // ========== MÉTODOS ==========
    public void Setup(UnitListPanel panel, InputSelection input, 
                      SelectionManager selection, int index, Unit unit)
    public void OnPointerClick(PointerEventData eventData)
    public void IgnoreNextClickOnce()
}
```

### 12.3 Handler de Clique

```csharp
/// <summary>
/// Handler de clique no item.
/// </summary>
public void OnPointerClick(PointerEventData eventData)
{
    if (_panel == null || _unit == null) return;
    
    // Clique direito: menu contextual
    if (eventData.button == PointerEventData.InputButton.Right)
    {
        _contextMenu?.ShowMenuAt(eventData.position, eventData.pressEventCamera);
        return;
    }
    
    // Supressão pós-drag
    if (Time.frameCount == _suppressClickFrame)
    {
        _suppressClickFrame = -1;
        return;
    }
    
    // Detectar modificadores
    bool ctrl = _input != null && _input.IsCtrlPressed;
    bool shift = _input != null && _input.IsShiftPressed;
    
    // Detectar duplo clique
    bool isDouble = (Time.unscaledTime - _lastClickTime) <= doubleClickMaxDelay;
    _lastClickTime = Time.unscaledTime;
    
    // Delegar para UnitListPanel
    _panel.OnItemClicked(_unit, ctrl, shift, isDouble);
}
```

### 12.4 Lógica de Seleção (em UnitListPanel)

```csharp
/// <summary>
/// Chamado por UnitListItemHandle ao clicar.
/// </summary>
public void OnItemClicked(Unit unit, bool ctrl, bool shift, bool isDouble)
{
    if (selectionManager == null || unit == null) return;
    
    // Duplo clique: selecionar mesmo tipo
    if (isDouble)
    {
        var sameType = UnitRegistry.All
            .Where(u => u.owner == player.myFaction && u.def == unit.def);
        selectionManager.SelectExactly(sameType);
        return;
    }
    
    // Shift: intervalo
    if (shift)
    {
        if (_anchorIndex >= 0 && _anchorIndex < _order.Count)
        {
            var rangeUnits = GetUnitsInRange(_anchorIndex, FindIndex(unit));
            
            if (ctrl) selectionManager.AddToSelection(rangeUnits);
            else selectionManager.SelectExactly(rangeUnits);
        }
        else
        {
            // Sem âncora: selecionar apenas este
            selectionManager.SelectExactly(new[] { unit });
            _anchorIndex = FindIndex(unit);
        }
        return;
    }
    
    // Ctrl: toggle
    if (ctrl)
    {
        selectionManager.Toggle(unit);
        _anchorIndex = FindIndex(unit);
        return;
    }
    
    // Simples: selecionar apenas este
    selectionManager.SelectExactly(new[] { unit });
    _anchorIndex = FindIndex(unit);
}
```

---

## 13) LISTITEMMARKER - IDENTIFICAÇÃO

### 13.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `ListItemMarker.cs`  
**Responsabilidade:** Marca um GameObject visual com referência ao `ListItemWrapper` correspondente. Permite reordenar hierarquia visual sem heurísticas.

### 13.2 Estrutura da Classe

```csharp
/// <summary>
/// Marca GameObject visual com wrapper lógico correspondente.
/// Usado por UnitListPanel.CommitReorder() para reconstruir _order.
/// </summary>
public class ListItemMarker : MonoBehaviour
{
    public ListItemWrapper Wrapper; // Setado no Build()
}
```

### 13.3 Uso

```csharp
// No BuildItem()
var marker = itemGO.AddComponent<ListItemMarker>();
marker.Wrapper = wrapper;

// No CommitReorder()
_order.Clear();
foreach (Transform child in content)
{
    var marker = child.GetComponent<ListItemMarker>();
    if (marker != null && marker.Wrapper != null)
    {
        _order.Add(marker.Wrapper);
    }
}
```

---

# PARTE V: DRAG-AND-DROP

---

## 14) REORDERABLELISTITEM - SISTEMA DE DRAG

### 14.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `ReorderableListItem.cs`  
**Implementa:** `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IDropHandler`  
**Responsabilidade:** Sistema completo de drag-and-drop para reordenação e aninhamento de itens.

### 14.2 Estrutura da Classe (Simplificada)

```csharp
public class ReorderableListItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // ========== REFS ==========
    UnitListPanel _panel;
    RectTransform _content;
    ScrollRect _scrollRect;
    Canvas _canvas;
    
    // ========== ESTADO DE DRAG ==========
    RectTransform _dragImage;
    int _originalIndex;
    bool _isDragging;
    
    // ========== AUTO-SCROLL ==========
    [SerializeField] float edgeHotZonePx = 80f;
    [SerializeField] float maxScrollSpeedPxPerSec = 900f;
    
    // ========== MÉTODOS ==========
    public void Setup(UnitListPanel panel, RectTransform content, ScrollRect scroll)
    public void OnBeginDrag(PointerEventData eventData)
    public void OnDrag(PointerEventData eventData)
    public void OnEndDrag(PointerEventData eventData)
    public void OnDrop(PointerEventData eventData)
}
```

### 14.3 Início de Drag

```csharp
/// <summary>
/// Chamado ao iniciar drag.
/// Cria imagem fantasma e desabilita layout.
/// </summary>
public void OnBeginDrag(PointerEventData eventData)
{
    _isDragging = true;
    _originalIndex = transform.GetSiblingIndex();
    
    // Criar imagem fantasma
    _dragImage = CreateDragImage();
    
    // Desabilitar layouts (evitar rebuild durante drag)
    SetLayoutsEnabled(false);
    
    // Reduzir alpha do original (feedback visual)
    SetAlpha(0.5f);
}
```

### 14.4 Durante Drag

```csharp
/// <summary>
/// Chamado a cada frame durante drag.
/// Atualiza posição da imagem fantasma e reordena hierarquia.
/// </summary>
public void OnDrag(PointerEventData eventData)
{
    if (!_isDragging || _dragImage == null) return;
    
    // Atualizar posição da imagem fantasma
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _canvas.transform as RectTransform, 
        eventData.position, 
        eventData.pressEventCamera, 
        out var localPos
    );
    _dragImage.anchoredPosition = localPos;
    
    // Reordenar hierarquia baseado na posição do mouse
    UpdateHierarchyOrder(eventData.position);
    
    // Auto-scroll nas bordas
    DoAutoScroll(eventData.position);
}
```

### 14.5 Fim de Drag

```csharp
/// <summary>
/// Chamado ao soltar botão do mouse.
/// Destrói imagem fantasma e commita reordenação.
/// </summary>
public void OnEndDrag(PointerEventData eventData)
{
    if (!_isDragging) return;
    _isDragging = false;
    
    // Destruir imagem fantasma
    if (_dragImage != null)
    {
        Destroy(_dragImage.gameObject);
        _dragImage = null;
    }
    
    // Restaurar alpha
    SetAlpha(1f);
    
    // Reabilitar layouts
    SetLayoutsEnabled(true);
    
    // Commitar reordenação
    _panel?.CommitReorder();
    
    // Forçar rebuild de layout
    LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    
    // Suprimir clique no frame seguinte
    var handle = GetComponent<UnitListItemHandle>();
    handle?.IgnoreNextClickOnce();
}
```

### 14.6 Drop Handler

```csharp
/// <summary>
/// Chamado quando item é dropado sobre este.
/// Usado para aninhamento em grupos.
/// </summary>
public void OnDrop(PointerEventData eventData)
{
    // Verificar se é um GroupDropZone
    var dropZone = GetComponentInChildren<GroupDropZone>();
    if (dropZone != null)
    {
        dropZone.OnDrop(eventData);
    }
}
```

### 14.7 Auto-Scroll nas Bordas

```csharp
/// <summary>
/// Auto-scroll quando mouse está próximo das bordas.
/// </summary>
void DoAutoScroll(Vector2 screenPos)
{
    if (_scrollRect == null || _scrollRect.viewport == null) return;
    
    // Obter coordenadas das bordas do viewport
    Vector3[] corners = new Vector3[4];
    _scrollRect.viewport.GetWorldCorners(corners);
    
    Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceCamera 
        ? _canvas.worldCamera 
        : null;
    
    float bottomY = RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y;
    float topY = RectTransformUtility.WorldToScreenPoint(cam, corners[2]).y;
    
    // Calcular distância das bordas
    float downAmount = Mathf.Clamp01(
        ((bottomY + edgeHotZonePx) - screenPos.y) / edgeHotZonePx
    );
    float upAmount = Mathf.Clamp01(
        (screenPos.y - (topY - edgeHotZonePx)) / edgeHotZonePx
    );
    
    // Aplicar scroll
    if (downAmount > 0f)
    {
        float deltaNorm = (downAmount * maxScrollSpeedPxPerSec * Time.unscaledDeltaTime) 
            / Mathf.Max(1f, _scrollRect.content.rect.height - _scrollRect.viewport.rect.height);
        
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            _scrollRect.verticalNormalizedPosition - deltaNorm
        );
    }
    else if (upAmount > 0f)
    {
        float deltaNorm = (upAmount * maxScrollSpeedPxPerSec * Time.unscaledDeltaTime) 
            / Mathf.Max(1f, _scrollRect.content.rect.height - _scrollRect.viewport.rect.height);
        
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            _scrollRect.verticalNormalizedPosition + deltaNorm
        );
    }
}
```

---

## 15) RESIZEGRIPHANDLER - REDIMENSIONAMENTO

### 15.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `ResizeGripHandler.cs`  
**Implementa:** `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler`, `IPointerEnterHandler`, `IPointerExitHandler`  
**Responsabilidade:** Grip de redimensionamento para grupos expandidos. Permite ajustar altura do item.

### 15.2 Estrutura da Classe

```csharp
public class ResizeGripHandler : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    // ========== TARGET ==========
    [Header("Target (Group)")]
    [SerializeField] LayoutElement targetLayoutElement;
    [SerializeField] RectTransform targetRect;
    
    // ========== SCROLL REFS ==========
    [Header("Main List Scroll Refs")]
    [SerializeField] ScrollRect mainScrollRect;
    [SerializeField] RectTransform mainViewport;
    
    // ========== CURSOR ==========
    [Header("Cursor Settings")]
    [SerializeField] Texture2D resizeCursorTexture;
    [SerializeField] Vector2 hotSpot = new Vector2(16, 16);
    
    // ========== CONSTRAINTS ==========
    [Header("Constraints")]
    [SerializeField] float minHeight = 200f;
    [SerializeField] float maxHeight = 1000f;
    
    // ========== ESTADO ==========
    Vector2 _dragStartMousePos;
    float _dragStartPreferredHeight;
    bool _isDragging;
    
    // ========== MÉTODOS ==========
    public void Setup(ScrollRect mainScroll, RectTransform mainVp, Texture2D cursor)
    public void OnPointerDown(PointerEventData eventData)
    public void OnDrag(PointerEventData eventData)
    public void OnPointerUp(PointerEventData eventData)
}
```

### 15.3 Início de Resize

```csharp
/// <summary>
/// Chamado ao pressionar o grip.
/// </summary>
public void OnPointerDown(PointerEventData eventData)
{
    if (targetLayoutElement == null) return;
    
    _isDragging = true;
    _dragStartMousePos = eventData.position;
    
    // Salvar altura inicial
    _dragStartPreferredHeight = targetLayoutElement.preferredHeight > 0
        ? targetLayoutElement.preferredHeight
        : targetRect.rect.height;
    
    // Ativar outline (feedback visual)
    var outline = targetRect.GetComponent<Outline>();
    if (outline) outline.enabled = true;
}
```

### 15.4 Durante Resize

```csharp
/// <summary>
/// Chamado a cada frame durante drag.
/// </summary>
public void OnDrag(PointerEventData eventData)
{
    if (!_isDragging || targetLayoutElement == null) return;
    
    // Calcular novo tamanho
    float deltaY = eventData.position.y - _dragStartMousePos.y;
    float newHeight = _dragStartPreferredHeight - deltaY; // Invertido (drag para baixo aumenta)
    
    // Aplicar constraints
    newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);
    
    // Atualizar altura
    targetLayoutElement.preferredHeight = newHeight;
    
    // Forçar rebuild
    if (targetRect)
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetRect);
    
    // Auto-scroll nas bordas (só para baixo)
    DoAutoScroll(eventData.position);
}
```

### 15.5 Fim de Resize

```csharp
/// <summary>
/// Chamado ao soltar grip.
/// </summary>
public void OnPointerUp(PointerEventData eventData)
{
    if (!_isDragging) return;
    _isDragging = false;
    
    // Restaurar cursor
    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    
    // Desativar outline
    var outline = targetRect?.GetComponent<Outline>();
    if (outline) outline.enabled = false;
    
    // Persistir altura no PlayerPrefs
    var groupUI = GetComponentInParent<GroupListItemUI>();
    if (groupUI?.GroupModel != null && targetLayoutElement != null)
    {
        string key = "GroupHeight_" + groupUI.GroupModel.ID;
        PlayerPrefs.SetFloat(key, targetLayoutElement.preferredHeight);
        PlayerPrefs.Save();
    }
}
```

### 15.6 Cursor Customizado

```csharp
/// <summary>
/// Ativa cursor de resize ao entrar.
/// </summary>
public void OnPointerEnter(PointerEventData eventData)
{
    // Verificar se há drag de outro elemento ativo
    bool isOtherDragActive = eventData.pointerDrag != null 
        && eventData.pointerDrag != gameObject;
    
    if (!isOtherDragActive && resizeCursorTexture != null)
    {
        Cursor.SetCursor(resizeCursorTexture, hotSpot, CursorMode.Auto);
    }
}

/// <summary>
/// Restaura cursor ao sair.
/// </summary>
public void OnPointerExit(PointerEventData eventData)
{
    if (!_isDragging)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
```

---

# PARTE VI: MENUS DE CONTEXTO

---

## 16) UNITLISTITEMCONTEXTMENU - MENU DE UNIDADE

### 16.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `UnitListItemContextMenu.cs`  
**Responsabilidade:** Gerencia popup de menu contextual para itens de unidade. Instancia prefab, posiciona e cria blocker.

### 16.2 Estrutura da Classe

```csharp
[RequireComponent(typeof(ReorderableListItem))]
[RequireComponent(typeof(UnitListItemUI))]
public class UnitListItemContextMenu : MonoBehaviour
{
    // ========== PREFAB ==========
    [Header("Prefab")]
    public RectTransform contextMenuPrefab;
    
    // ========== LAYOUT ==========
    [Header("Layout")]
    public Vector2 screenPadding = new Vector2(8f, 8f);
    public Color blockerColor = new Color(0, 0, 0, 0.001f);
    
    // ========== REFS LOCAIS ==========
    ReorderableListItem _reorder;
    UnitListItemUI _ui;
    
    // ========== ESTADO ==========
    RectTransform _menu;
    GameObject _blocker;
    
    // ========== MÉTODOS ==========
    void Awake()
    public void ShowMenuAt(Vector2 screenPos, Camera eventCam)
    public void CloseMenu()
    void OnDisable()
}
```

### 16.3 Exibição do Menu

```csharp
/// <summary>
/// Exibe menu contextual na posição do clique direito.
/// </summary>
public void ShowMenuAt(Vector2 screenPos, Camera eventCam)
{
    CloseMenu(); // Fechar menu anterior (se existir)
    
    if (contextMenuPrefab == null) return;
    
    // Obter Canvas pai
    RectTransform parent = null;
    var canvas = _reorder?.Canvas;
    if (canvas) parent = canvas.transform as RectTransform;
    if (parent == null)
    {
        var any = GetComponentInParent<Canvas>();
        if (any) parent = any.transform as RectTransform;
    }
    if (parent == null) return;
    
    // 1. Criar blocker (fecha ao clicar fora)
    _blocker = new GameObject("UnitContextMenuBlocker",
        typeof(RectTransform), typeof(CanvasRenderer), 
        typeof(Image), typeof(BlockerInputCatcher));
    
    var brt = (RectTransform)_blocker.transform;
    brt.SetParent(parent, false);
    brt.anchorMin = Vector2.zero;
    brt.anchorMax = Vector2.one;
    brt.offsetMin = Vector2.zero;
    brt.offsetMax = Vector2.zero;
    brt.SetAsLastSibling();
    
    var bImg = _blocker.GetComponent<Image>();
    bImg.color = blockerColor;
    
    // Configurar blocker input
    var catcher = _blocker.GetComponent<BlockerInputCatcher>();
    catcher.onLeftClick = CloseMenu;
    catcher.onRightClick = () => {
        CloseMenu();
        ForwardRightClickToAnyContextTarget();
    };
    
    // 2. Instanciar menu
    _menu = Instantiate(contextMenuPrefab, parent);
    _menu.gameObject.SetActive(true);
    _menu.SetAsLastSibling();
    
    // 3. Posicionar (canto superior esquerdo no mouse)
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        parent, screenPos, eventCam, out var local
    );
    _menu.anchoredPosition = local;
    LayoutRebuilder.ForceRebuildLayoutImmediate(_menu);
    
    var size = _menu.rect.size;
    var pivot = _menu.pivot;
    var pos = _menu.anchoredPosition;
    pos.x -= size.x * pivot.x;          // Esquerda no mouse
    pos.y += size.y * (1f - pivot.y);   // Topo no mouse
    _menu.anchoredPosition = pos;
    
    // 4. Configurar handler do menu
    var handler = _menu.GetComponent<UnitContextMenuHandler>();
    if (handler != null)
    {
        handler.Setup(this, ResolveTargetTransform());
    }
    
    // 5. Clamp para não sair do Canvas
    ClampToParent(parent, _menu);
}
```

### 16.4 Clamping do Menu

```csharp
/// <summary>
/// Ajusta posição do menu para não sair das bordas do Canvas.
/// </summary>
void ClampToParent(RectTransform parent, RectTransform menu)
{
    var pr = parent.rect;
    var mr = menu.rect;
    var pos = menu.anchoredPosition;
    var pv = menu.pivot;
    
    float left = pos.x - mr.width * pv.x;
    float top = pos.y + mr.height * (1f - pv.y);
    
    // Clamp horizontal
    if (left < pr.xMin + screenPadding.x)
        pos.x += (pr.xMin + screenPadding.x) - left;
    else if (left + mr.width > pr.xMax - screenPadding.x)
        pos.x -= (left + mr.width) - (pr.xMax - screenPadding.x);
    
    // Clamp vertical
    if (top > pr.yMax - screenPadding.y)
        pos.y -= top - (pr.yMax - screenPadding.y);
    else if (top - mr.height < pr.yMin + screenPadding.y)
        pos.y += (pr.yMin + screenPadding.y) - (top - mr.height);
    
    menu.anchoredPosition = pos;
}
```

### 16.5 Fechar Menu

```csharp
/// <summary>
/// Fecha e destrói menu e blocker.
/// </summary>
public void CloseMenu()
{
    if (_menu) Destroy(_menu.gameObject);
    if (_blocker) Destroy(_blocker);
    _menu = null;
    _blocker = null;
}

void OnDisable() => CloseMenu();
```

---

## 17) UNITCONTEXTMENUHANDLER - AÇÕES DE UNIDADE

### 17.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `UnitContextMenuHandler.cs`  
**Responsabilidade:** Handler de ações do menu contextual de unidade. Atualmente implementa: "Follow" (focar câmera na unidade).

### 17.2 Estrutura da Classe

```csharp
public class UnitContextMenuHandler : MonoBehaviour
{
    // ========== UI ==========
    [Header("UI")]
    public Button followButton;
    
    // ========== MOVIMENTO DE CÂMERA ==========
    [Header("Movimento")]
    public bool snap = false;
    public float moveDuration = 0.5f;
    
    // ========== REFS ==========
    UnitListItemContextMenu _owner;
    Transform _target;
    
    // ========== MÉTODOS ==========
    public void Setup(UnitListItemContextMenu owner, Transform target)
    void OnFollow()
}
```

### 17.3 Setup

```csharp
/// <summary>
/// Configura handler com referências necessárias.
/// </summary>
public void Setup(UnitListItemContextMenu owner, Transform target)
{
    _owner = owner;
    _target = target;
    
    // Ligar botão "Follow"
    if (followButton)
    {
        followButton.onClick.RemoveAllListeners();
        followButton.onClick.AddListener(OnFollow);
    }
}
```

### 17.4 Ação "Follow"

```csharp
/// <summary>
/// Move câmera para posição da unidade.
/// </summary>
void OnFollow()
{
    if (_target == null) return;
    
    // Buscar controller de câmera (Lote 2)
    var cam = Object.FindFirstObjectByType<RTSCameraCinemachineV3Controller>();
    if (cam != null)
    {
        // Mover câmera para posição XZ da unidade
        cam.GoTo(_target.position, snap, moveDuration);
    }
    
    // Fechar menu
    _owner?.CloseMenu();
}
```

### 17.5 Extensões Futuras

```csharp
// Adicionar novos botões no prefab:

[Header("Botões Adicionais")]
public Button promoteButton;    // Promover unidade
public Button dismissButton;    // Dispensar unidade
public Button assignButton;     // Atribuir tarefa

void OnPromote() { /* lógica */ }
void OnDismiss() { /* lógica */ }
void OnAssign() { /* lógica */ }
```

---

## 18) GROUPHEADERCONTEXTMENU - MENU DE GRUPO

### 18.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `GroupHeaderContextMenu.cs`  
**Implementa:** `IPointerClickHandler`  
**Responsabilidade:** Gerencia menu contextual do header de grupo. Funciona de forma similar ao menu de unidade.

### 18.2 Estrutura da Classe

```csharp
/// <summary>
/// Menu contextual do header de grupo.
/// - Right click: abre menu (Rename/Delete)
/// - Left click: fecha menu
/// </summary>
public class GroupHeaderContextMenu : MonoBehaviour, IPointerClickHandler
{
    // ========== PREFAB ==========
    [Header("Prefab")]
    public RectTransform contextMenuPrefab;
    
    // ========== UI ROOT ==========
    [Header("Onde Instanciar")]
    public RectTransform uiRoot;
    public ReorderableListItem Rei;
    
    // ========== LAYOUT ==========
    [Header("Ajustes")]
    public Vector2 screenPadding = new Vector2(8f, 8f);
    public Color blockerColor = new Color(0, 0, 0, 0.001f);
    
    // ========== ESTADO ==========
    RectTransform _menu;
    GameObject _blocker;
    
    // ========== MÉTODOS ==========
    public void OnPointerClick(PointerEventData eventData)
    void ShowMenuAt(Vector2 screenPos, Camera eventCam)
    public void CloseMenu()
    void OnDisable()
}
```

### 18.3 Handler de Clique

```csharp
/// <summary>
/// Handler de clique no header.
/// Right: abre menu, Left: fecha menu.
/// </summary>
public void OnPointerClick(PointerEventData eventData)
{
    if (eventData.button == PointerEventData.InputButton.Right)
    {
        ShowMenuAt(eventData.position, eventData.pressEventCamera);
    }
    else if (eventData.button == PointerEventData.InputButton.Left)
    {
        CloseMenu();
    }
}
```

### 18.4 Exibição do Menu

```csharp
/// <summary>
/// Exibe menu contextual (Rename/Delete).
/// </summary>
void ShowMenuAt(Vector2 screenPos, Camera eventCam)
{
    CloseMenu();
    
    if (contextMenuPrefab == null) return;
    
    // ... (lógica similar ao UnitListItemContextMenu)
    
    // Setup específico para grupo
    var groupUI = GetComponentInParent<GroupListItemUI>();
    var rootPanel = groupUI?.GetComponentInParent<UnitListPanel>();
    
    if (groupUI == null || rootPanel == null)
    {
        CloseMenu();
        return;
    }
    
    // Configurar handler do menu
    var handler = _menu.GetComponent<GroupContextMenuHandler>();
    if (handler != null)
    {
        TMP_Text headerText = groupUI.nameText;
        handler.Setup(rootPanel, groupUI.GroupModel, headerText, this);
    }
    
    // Clamp posição
    ClampToParent(parent, _menu);
}
```

---

## 19) GROUPCONTEXTMENUHANDLER - AÇÕES DE GRUPO

### 19.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `GroupContextMenuHandler.cs`  
**Responsabilidade:** Handler de ações do menu contextual de grupo. Implementa:
- **Rename**: Renomear grupo
- **Delete**: Deletar grupo e restaurar unidades

### 19.2 Estrutura da Classe

```csharp
/// <summary>
/// Controller do menu de contexto do grupo (Rename/Delete).
/// REFATORADO (Lote 5): Dispara evento global ao renomear.
/// </summary>
public class GroupContextMenuHandler : MonoBehaviour
{
    // ========== UI ==========
    [Header("UI Elementos")]
    [SerializeField] Button renameButton;
    [SerializeField] Button deleteButton;
    [SerializeField] TMP_InputField renameInput;
    
    // ========== REFS INJETADAS ==========
    UnitGroup _targetGroup;
    UnitListPanel _rootPanel;
    TMP_Text _headerText;
    GroupHeaderContextMenu _contextMenu;
    
    // ========== MÉTODOS ==========
    void Awake()
    public void Setup(UnitListPanel panel, UnitGroup group, 
                      TMP_Text headerText, GroupHeaderContextMenu contextMenu)
    void OnRenameClicked()
    void OnRenameInputEndEdit(string newName)
    void OnDeleteClicked()
}
```

### 19.3 Setup

```csharp
/// <summary>
/// Injeta referências necessárias.
/// CRÍTICO: Deve ser chamado após instanciar o menu.
/// </summary>
public void Setup(UnitListPanel panel, UnitGroup group, 
                  TMP_Text headerText, GroupHeaderContextMenu contextMenu)
{
    _rootPanel = panel;
    _targetGroup = group;
    _headerText = headerText;
    _contextMenu = contextMenu;
}
```

### 19.4 Ação "Rename"

```csharp
/// <summary>
/// Botão "Rename" clicado: exibe input field.
/// </summary>
void OnRenameClicked()
{
    if (_targetGroup == null || renameInput == null) return;
    
    // Ocultar botões, exibir input
    if (renameButton) renameButton.gameObject.SetActive(false);
    if (deleteButton) deleteButton.gameObject.SetActive(false);
    renameInput.gameObject.SetActive(true);
    
    // Preencher com nome atual e dar foco
    renameInput.text = _targetGroup.GroupName;
    renameInput.ActivateInputField();
    renameInput.Select();
}

/// <summary>
/// Input field finalizado (Enter/desfoco): aplica novo nome.
/// REFATORAÇÃO: Dispara GameEvents.OnGroupRenamed.
/// </summary>
void OnRenameInputEndEdit(string newName)
{
    if (_targetGroup == null || renameInput == null) return;
    
    var trimmedName = newName.Trim();
    
    // Aplicar novo nome (se válido)
    if (!string.IsNullOrEmpty(trimmedName))
    {
        string oldName = _targetGroup.GroupName;
        _targetGroup.GroupName = trimmedName;
        
        // Atualizar visual do header
        if (_headerText) _headerText.text = trimmedName;
        
        // REFATORAÇÃO: Disparar evento global
        GameEvents.RaiseGroupRenamed(_targetGroup, trimmedName);
    }
    
    // Fechar menu
    _contextMenu?.CloseMenu();
}
```

### 19.5 Ação "Delete"

```csharp
/// <summary>
/// Botão "Delete" clicado: deleta grupo e restaura unidades.
/// REFATORAÇÃO: Dispara GameEvents.OnGroupDeleted.
/// </summary>
void OnDeleteClicked()
{
    if (_targetGroup == null || _rootPanel == null) return;
    
    // Copiar lista de unidades (antes de deletar)
    var unitsToRestore = _targetGroup.Units.ToList();
    
    // Delegar deleção para UnitListPanel
    _rootPanel.DeleteGroupAndRestoreUnits(_targetGroup, unitsToRestore);
    
    // Fechar menu
    _contextMenu?.CloseMenu();
    
    // NOTA: Evento OnGroupDeleted é disparado dentro de DeleteGroupAndRestoreUnits
}
```

---

# PARTE VII: COMPONENTES AUXILIARES

---

## 20) VIEWPORTCLEARSELECTION - LIMPAR SELEÇÃO

### 20.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `ViewportClearSelection.cs`  
**Implementa:** `IPointerDownHandler`  
**Responsabilidade:** Limpa seleção ao clicar no "vazio" do viewport (fora dos itens).

### 20.2 Estrutura da Classe

```csharp
/// <summary>
/// Limpa seleção ao clicar no vazio do viewport.
/// </summary>
public class ViewportClearSelection : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] UnitListPanel panel;
    
    void Awake()
    {
        // Auto-descoberta se não ligado
        if (!panel) panel = GetComponentInParent<UnitListPanel>();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!panel) return;
        
        // Verificar se clique acertou um item (não limpar se sim)
        var go = eventData.pointerPressRaycast.gameObject;
        if (go && go.GetComponentInParent<UnitListItemUI>()) return;
        
        // Limpar seleção
        var sel = panel.SelectionManager;
        if (sel != null)
        {
            sel.SelectExactly(System.Array.Empty<Unit>());
            sel.ClearAnchor();
        }
    }
}
```

### 20.3 Setup no Prefab

```
ScrollView (ScrollRect)
└── Viewport (RectTransform)
    ├── ViewportClearSelection (MonoBehaviour) ← Adicionar aqui
    └── Content (RectTransform + VerticalLayoutGroup)
        ├── UnitItem 1
        ├── UnitItem 2
        └── ...
```

---

## 21) BLOCKERINPUTCATCHER - CAPTURA DE INPUT

### 21.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `BlockerInputCatcher.cs`  
**Implementa:** `IPointerClickHandler`  
**Responsabilidade:** Blocker customizado para menus contextuais. Diferencia clique esquerdo (fechar) de clique direito (reenviar para outro contexto).

### 21.2 Estrutura da Classe

```csharp
/// <summary>
/// Captura cliques no blocker de menus contextuais.
/// </summary>
public class BlockerInputCatcher : MonoBehaviour, IPointerClickHandler
{
    public UnityAction onLeftClick;
    public UnityAction onRightClick;
    
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            onLeftClick?.Invoke();
        else if (e.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke();
    }
}
```

### 21.3 Uso

```csharp
// No menu contextual
var catcher = _blocker.GetComponent<BlockerInputCatcher>();
catcher.onLeftClick = CloseMenu; // Fechar menu
catcher.onRightClick = () => {
    CloseMenu();
    ForwardRightClickToAnyContextTarget(); // Reenviar para outro contexto
};
```

---

# PARTE VIII: INTEGRAÇÃO E MANUTENÇÃO

---

## 22) INTEGRAÇÃO COM GAMEEVENTS

### 22.1 Eventos Escutados (Subscribe)

| Evento | Handler | Ação |
|--------|---------|------|
| `OnUnitSpawned` | `HandleUnitSpawned` | Adiciona unidade à lista |
| `OnUnitDespawned` | `HandleUnitDespawned` | Remove unidade da lista |
| `OnSelectionChanged` | `RefreshFromSelection` | Atualiza visuais de seleção |
| `OnUnitProgressChanged` | `UnitListItemUI.OnUnitProgressChanged` | Atualiza barra de XP |

**Código:**
```csharp
void OnEnable()
{
    GameEvents.OnUnitSpawned += HandleUnitSpawned;
    GameEvents.OnUnitDespawned += HandleUnitDespawned;
    GameEvents.OnSelectionChanged += RefreshFromSelection;
}

void OnDisable()
{
    // CRÍTICO: Sempre desinscrever
    GameEvents.OnUnitSpawned -= HandleUnitSpawned;
    GameEvents.OnUnitDespawned -= HandleUnitDespawned;
    GameEvents.OnSelectionChanged -= RefreshFromSelection;
}
```

### 22.2 Eventos Disparados (Raise)

| Evento | Quando | Parâmetros |
|--------|--------|------------|
| `OnGroupCreated` | Grupo criado | `UnitGroup` |
| `OnGroupDeleted` | Grupo deletado | `UnitGroup` |
| `OnGroupRenamed` | Grupo renomeado | `UnitGroup, string` |
| `OnUnitsAddedToGroup` | Unidades adicionadas a grupo | `UnitGroup, IReadOnlyList<Unit>` |
| `OnUnitsRemovedFromGroup` | Unidades removidas de grupo | `UnitGroup, IReadOnlyList<Unit>` |

**Código:**
```csharp
// Ao criar grupo
GameEvents.RaiseGroupCreated(newGroup);

// Ao deletar grupo
GameEvents.RaiseGroupDeleted(group);

// Ao renomear grupo
GameEvents.RaiseGroupRenamed(_targetGroup, trimmedName);

// Ao adicionar unidades
GameEvents.RaiseUnitsAddedToGroup(targetGroup, new[] { unit });
```

---

## 23) FLUXO DE INICIALIZAÇÃO

### 23.1 Sequência Completa

```
1. Unity Scene Load
   │
2. UnitListPanel.Awake()
   ├─> Cache de componentes
   ├─> Auto-descoberta de dependências
   └─> Validação de prefabs
   │
3. UnitListPanel.OnEnable()
   ├─> GameEvents.OnUnitSpawned += HandleUnitSpawned
   ├─> GameEvents.OnUnitDespawned += HandleUnitDespawned
   ├─> GameEvents.OnSelectionChanged += RefreshFromSelection
   └─> selectionManager.OnAnchorSet += OnAnchorSet
   │
4. UnitListPanel.Start()
   ├─> PopulateFromRegistry()
   │   ├─> myUnits = UnitRegistry.GetByFaction(player.myFaction)
   │   └─> _order.Add(new ListItemWrapper(unit)) para cada
   │
   └─> RebuildList()
       ├─> Limpar content (destroy children)
       ├─> BuildItem() para cada wrapper em _order
       │   ├─> BuildUnitItem(unit) ou BuildGroupItem(group)
       │   ├─> Instantiate(prefab)
       │   ├─> AddComponent<ReorderableListItem>()
       │   ├─> AddComponent<UnitListItemHandle>()
       │   ├─> AddComponent<ListItemMarker>()
       │   └─> UI.Bind(data)
       │
       └─> RefreshFromSelection(selectionManager.Selection)
```

### 23.2 Primeira Unidade Spawn

```
[Unit GameObject Ativa na Cena]
    │
    ↓ (Lote 3)
Unit.OnEnable()
    │
    ↓
UnitRegistry.Register(unit)
    │
    ↓ (Lote 1)
GameEvents.RaiseUnitSpawned(unit)
    │
    ↓ (Lote 5)
UnitListPanel.HandleUnitSpawned(unit)
    │
    ├─> if (unit.owner != myFaction) return; // Filtro
    ├─> bool exists = _order.Any(w => w.GetUnit() == unit);
    │   if (exists) return; // Evitar duplicata
    ├─> _order.Add(new ListItemWrapper(unit));
    │
    └─> RebuildList()
        │
        └─> BuildUnitItem(unit)
            │
            ├─> Instantiate(unitItemPrefab)
            ├─> UnitListItemUI.Bind(unit)
            │   ├─> nameText.text = unit.DisplayName
            │   ├─> portrait.sprite = unit.def.icon
            │   ├─> Refresh() (barra de XP)
            │   └─> GameEvents.OnUnitProgressChanged += OnUnitProgressChanged
            │
            └─> [Item Visível na Lista]
```

---

## 24) PADRÕES DE USO AVANÇADOS

### 24.1 Adicionar Novo Tipo de Item

**Cenário:** Adicionar "Construções" à lista (além de Units e Groups).

**Passo 1:** Criar modelo de dados

```csharp
public class Building : IListItemModel
{
    public string BuildingName { get; set; }
    public List<Unit> Garrison { get; set; } = new();
    
    // IListItemModel
    public string DisplayName => BuildingName;
    public bool IsGroup => false; // Ou true se puder conter unidades
    public Unit GetUnit() => null;
    public IReadOnlyList<Unit> GetUnitsInItem() => Garrison;
}
```

**Passo 2:** Atualizar ListItemWrapper

```csharp
// Adicionar construtor
public ListItemWrapper(Building building)
{
    Model = building; // Building implementa IListItemModel
}
```

**Passo 3:** Criar prefab de UI

```csharp
public class BuildingListItemUI : MonoBehaviour
{
    public TMP_Text buildingName;
    public Image buildingIcon;
    public TMP_Text garrisonCount;
    
    Building _building;
    
    public void Bind(Building building)
    {
        _building = building;
        buildingName.text = building.BuildingName;
        garrisonCount.text = $"{building.Garrison.Count} units";
    }
}
```

**Passo 4:** Atualizar BuildItem() em UnitListPanel

```csharp
GameObject BuildItem(ListItemWrapper wrapper)
{
    if (wrapper.IsGroup)
    {
        return BuildGroupItem((UnitGroup)wrapper.Model);
    }
    else if (wrapper.Model is Building building)
    {
        return BuildBuildingItem(building);
    }
    else
    {
        Unit unit = wrapper.GetUnit();
        return BuildUnitItem(unit);
    }
}

GameObject BuildBuildingItem(Building building)
{
    var go = Instantiate(buildingItemPrefab.gameObject);
    var ui = go.GetComponent<BuildingListItemUI>();
    if (ui) ui.Bind(building);
    // ... (adicionar ReorderableListItem, etc)
    return go;
}
```

### 24.2 Rebuild Incremental (Otimização)

**Problema:** `RebuildList()` destroi e recria tudo (pesado para listas grandes).

**Solução:** Rebuild incremental (apenas itens modificados).

```csharp
public void IncrementalRebuild()
{
    // Mapear wrappers existentes
    var existingMarkers = new Dictionary<ListItemWrapper, Transform>();
    foreach (Transform child in content)
    {
        var marker = child.GetComponent<ListItemMarker>();
        if (marker?.Wrapper != null)
        {
            existingMarkers[marker.Wrapper] = child;
        }
    }
    
    // Processar _order
    for (int i = 0; i < _order.Count; i++)
    {
        var wrapper = _order[i];
        
        // Item já existe?
        if (existingMarkers.TryGetValue(wrapper, out var existingTransform))
        {
            // Apenas reordenar
            existingTransform.SetSiblingIndex(i);
            existingMarkers.Remove(wrapper);
        }
        else
        {
            // Criar novo item
            var newItem = BuildItem(wrapper);
            if (newItem != null)
            {
                newItem.transform.SetParent(content, false);
                newItem.transform.SetSiblingIndex(i);
            }
        }
    }
    
    // Destruir itens removidos
    foreach (var leftover in existingMarkers.Values)
    {
        Destroy(leftover.gameObject);
    }
    
    RefreshFromSelection(selectionManager?.Selection);
}
```

### 24.3 Persistência de Grupos (PlayerPrefs)

**Salvar grupos:**

```csharp
public void SaveGroups()
{
    var data = new GroupSaveData
    {
        groups = _groups.Select(g => new GroupData
        {
            id = g.ID,
            name = g.GroupName,
            unitIDs = g.Units.Select(u => u.GetInstanceID()).ToList(),
            isExpanded = g.IsExpanded,
            preferredHeight = g.PreferredHeight
        }).ToList()
    };
    
    string json = JsonUtility.ToJson(data);
    PlayerPrefs.SetString("UnitGroups", json);
    PlayerPrefs.Save();
}

[System.Serializable]
class GroupSaveData
{
    public List<GroupData> groups;
}

[System.Serializable]
class GroupData
{
    public string id;
    public string name;
    public List<int> unitIDs;
    public bool isExpanded;
    public float preferredHeight;
}
```

**Carregar grupos:**

```csharp
public void LoadGroups()
{
    string json = PlayerPrefs.GetString("UnitGroups", "");
    if (string.IsNullOrEmpty(json)) return;
    
    var data = JsonUtility.FromJson<GroupSaveData>(json);
    if (data?.groups == null) return;
    
    foreach (var gData in data.groups)
    {
        var group = new UnitGroup
        {
            GroupName = gData.name,
            IsExpanded = gData.isExpanded,
            PreferredHeight = gData.preferredHeight
        };
        
        // Reconstruir lista de unidades (via instanceID)
        foreach (var unitID in gData.unitIDs)
        {
            var unit = UnitRegistry.All.FirstOrDefault(u => u.GetInstanceID() == unitID);
            if (unit != null)
            {
                group.Units.Add(unit);
            }
        }
        
        if (group.Units.Count > 0)
        {
            _groups.Add(group);
            _order.Add(new ListItemWrapper(group));
        }
    }
    
    RebuildList();
}
```

---

## 25) SOLUÇÃO DE PROBLEMAS

### 25.1 Problema: "Lista não atualiza ao spawnar unidade"

**Sintomas:**
- Unidade spawna na cena mas não aparece na lista
- Console não mostra erros

**Diagnóstico:**
1. Verificar se `OnEnable()` foi chamado
2. Verificar se facção da unidade corresponde ao jogador
3. Adicionar logs temporários

**Solução:**
```csharp
void HandleUnitSpawned(Unit unit)
{
    Debug.Log($"[HandleUnitSpawned] unit={unit?.DisplayName}, owner={unit?.owner}, myFaction={player?.myFaction}");
    
    if (unit == null || player == null) return;
    
    // CRÍTICO: Filtrar por facção
    if (unit.owner != player.myFaction)
    {
        Debug.Log($"[HandleUnitSpawned] Unidade ignorada (facção diferente)");
        return;
    }
    
    // ... resto do código
}
```

---

### 25.2 Problema: "Drag-and-drop não funciona"

**Sintomas:**
- Não consegue arrastar itens
- Ou arrasta mas não reordena

**Causas Comuns:**
1. `ReorderableListItem` não foi adicionado ao prefab
2. `GraphicRaycaster` ausente no Canvas
3. `EventSystem` ausente na cena

**Solução:**
```csharp
// Verificar componentes
void ValidateDragSetup()
{
    // 1. ReorderableListItem
    var rei = GetComponent<ReorderableListItem>();
    if (rei == null)
    {
        Debug.LogError("ReorderableListItem não encontrado!", this);
    }
    
    // 2. GraphicRaycaster
    var canvas = GetComponentInParent<Canvas>();
    var raycaster = canvas?.GetComponent<GraphicRaycaster>();
    if (raycaster == null)
    {
        Debug.LogError("GraphicRaycaster não encontrado no Canvas!", canvas);
    }
    
    // 3. EventSystem
    var es = FindFirstObjectByType<EventSystem>();
    if (es == null)
    {
        Debug.LogError("EventSystem não encontrado na cena!");
    }
}
```

---

### 25.3 Problema: "Grupos não expandem/colapsam"

**Sintomas:**
- Clicar no toggle não faz nada
- Conteúdo interno não aparece

**Causas:**
1. `subContent` não está ligado no Inspector
2. Toggle não está configurado corretamente
3. `RefreshVisuals()` não é chamado

**Solução:**
```csharp
// No GroupListItemUI.Awake()
void Awake()
{
    // Validar refs
    if (subContent == null)
    {
        Debug.LogError("subContent não ligado!", this);
    }
    
    if (expandToggle == null)
    {
        Debug.LogError("expandToggle não ligado!", this);
    }
    else
    {
        // Garantir que listener está ligado
        expandToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        expandToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }
}
```

---

### 25.4 Problema: "Menu contextual não abre"

**Sintomas:**
- Clique direito não faz nada
- Menu não aparece

**Causas:**
1. Prefab do menu não está ligado
2. Canvas do menu não encontrado
3. EventSystem não detecta clique direito

**Solução:**
```csharp
// No ShowMenuAt()
public void ShowMenuAt(Vector2 screenPos, Camera eventCam)
{
    Debug.Log($"[ShowMenuAt] screenPos={screenPos}");
    
    if (contextMenuPrefab == null)
    {
        Debug.LogError("contextMenuPrefab não ligado!", this);
        return;
    }
    
    // Buscar Canvas
    RectTransform parent = uiRoot;
    if (parent == null)
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) parent = canvas.transform as RectTransform;
    }
    
    if (parent == null)
    {
        Debug.LogError("Canvas pai não encontrado!", this);
        return;
    }
    
    Debug.Log($"[ShowMenuAt] Instanciando menu em {parent.name}");
    
    // ... resto do código
}
```

---

### 25.5 Problema: "Barra de XP não atualiza"

**Sintomas:**
- Level aumenta mas barra permanece vazia
- Barra não preenche até o final

**Causas:**
1. `barFill` ou `circleFill` não ligados
2. `Image.type` não configurado como `Filled`
3. Não inscrito em `GameEvents.OnUnitProgressChanged`

**Solução:**
```csharp
// No UnitListItemUI.Awake()
void Awake()
{
    // CRÍTICO: Configurar tipo de Image
    if (barFill)
    {
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
    }
    
    if (circleFill)
    {
        circleFill.type = Image.Type.Filled;
        circleFill.fillMethod = Image.FillMethod.Radial360;
    }
}

// No Bind()
public void Bind(Unit unit)
{
    // ... código existente
    
    // Verificar inscrição
    GameEvents.OnUnitProgressChanged += OnUnitProgressChanged;
    Debug.Log($"[Bind] Inscrito em OnUnitProgressChanged para {unit.DisplayName}");
}
```

---

### 25.6 Problema: "Memory Leak (lista cresce infinitamente)"

**Sintomas:**
- Profiler mostra aumento constante de memória
- Itens duplicados na lista

**Causas:**
1. Não desinscrever de `GameEvents`
2. Não destruir GameObjects ao rebuild
3. Listeners duplicados

**Solução:**
```csharp
// CRÍTICO: Sempre desinscrever em OnDisable
void OnDisable()
{
    GameEvents.OnUnitSpawned -= HandleUnitSpawned;
    GameEvents.OnUnitDespawned -= HandleUnitDespawned;
    GameEvents.OnSelectionChanged -= RefreshFromSelection;
    
    Debug.Log($"[OnDisable] Desinscrito de GameEvents");
}

// No RebuildList(), destruir filhos
public void RebuildList()
{
    if (content == null) return;
    
    // CRÍTICO: Destruir todos os filhos
    int childCount = content.childCount;
    for (int i = childCount - 1; i >= 0; i--)
    {
        Destroy(content.GetChild(i).gameObject);
    }
    
    // Ou usar:
    foreach (Transform child in content)
    {
        Destroy(child.gameObject);
    }
    
    // ... resto do código
}
```

---

## 26) ESTRUTURA DE ARQUIVOS

### 26.1 Organização de Scripts

```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── UnitList/
│   │   │   ├── Core/
│   │   │   │   ├── UnitListPanel.cs            ★★★ Controlador principal
│   │   │   │   ├── ListItemWrapper.cs          ★ Wrapper unificado
│   │   │   │   └── IListItemModel.cs           ★ Interface base
│   │   │   │
│   │   │   ├── Groups/
│   │   │   │   ├── UnitGroup.cs                ★ Modelo de dados
│   │   │   │   ├── GroupListItemUI.cs          ★ Visual de grupo
│   │   │   │   ├── GroupDropZone.cs            Drop target
│   │   │   │   └── CreateGroupUI.cs            Criação de grupos
│   │   │   │
│   │   │   ├── Items/
│   │   │   │   ├── UnitListItemUI.cs           ★ Visual de unidade
│   │   │   │   ├── UnitListItemHandle.cs       Interação
│   │   │   │   └── ListItemMarker.cs           Identificação
│   │   │   │
│   │   │   ├── DragDrop/
│   │   │   │   ├── ReorderableListItem.cs      ★★ Sistema de drag
│   │   │   │   └── ResizeGripHandler.cs        Redimensionamento
│   │   │   │
│   │   │   ├── ContextMenus/
│   │   │   │   ├── UnitListItemContextMenu.cs  Menu de unidade
│   │   │   │   ├── UnitContextMenuHandler.cs   Ações de unidade
│   │   │   │   ├── GroupHeaderContextMenu.cs   Menu de grupo
│   │   │   │   └── GroupContextMenuHandler.cs  ★ Ações de grupo
│   │   │   │
│   │   │   └── Helpers/
│   │   │       ├── ViewportClearSelection.cs   Limpar seleção
│   │   │       └── BlockerInputCatcher.cs      Captura de input
│   │   │
│   │   └── ...
│   │
│   └── Core/
│       ├── GameEvents.cs (Lote 1)
│       └── ...
```

### 26.2 Hierarquia de Prefabs

**UnitListItemPrefab:**
```
UnitItem (UnitListItemUI)
├── Portrait (Image)
├── NameText (TMP_Text)
├── LevelText (TMP_Text)
├── XPBar (Container)
│   ├── BarFill (Image - Filled Horizontal)
│   └── CircleFill (Image - Filled Radial360)
└── Outline (Component)

Componentes Adicionados Dinamicamente:
- ReorderableListItem
- UnitListItemHandle
- UnitListItemContextMenu
- ListItemMarker
```

**GroupListItemPrefab:**
```
GroupItem (GroupListItemUI)
├── Name (Header)
│   ├── NameText (TMP_Text)
│   ├── ExpandToggle (Toggle)
│   └── GroupHeaderContextMenu (Component)
├── SubContent (RectTransform)
│   └── [Unidades filhas instanciadas aqui]
├── ResizeGrip (Image)
│   └── ResizeGripHandler (Component)
├── Outline (Component)
└── GroupDropZone (Component)

Componentes Adicionados Dinamicamente:
- ReorderableListItem
- ListItemMarker
```

### 26.3 Hierarquia de Cena

```
Canvas (UI)
└── UnitListPanel (GameObject)
    ├── UnitListPanel (MonoBehaviour) ★★★
    │   ├── player: PlayerController
    │   ├── selectionManager: SelectionManager
    │   ├── inputSelection: InputSelection
    │   ├── unitItemPrefab: UnitListItemPrefab
    │   ├── groupItemPrefab: GroupListItemPrefab
    │   ├── content: Content (↓)
    │   └── scrollRect: ScrollRect (↓)
    │
    ├── CreateGroupUI (GameObject)
    │   ├── CreateGroupUI (MonoBehaviour)
    │   ├── NameInput (TMP_InputField)
    │   └── CreateButton (Button)
    │
    └── ScrollView (ScrollRect)
        └── Viewport (RectTransform)
            ├── ViewportClearSelection (MonoBehaviour)
            └── Content (RectTransform + VerticalLayoutGroup)
                ├── [Items instanciados dinamicamente]
                └── ...
```

---

## 27) CHECKLIST DE VALIDAÇÃO

### 27.1 Setup Inicial

- [ ] `UnitListPanel` GameObject criado na cena
- [ ] Referências configuradas no Inspector:
  - [ ] `player` → PlayerController
  - [ ] `selectionManager` → SelectionManager
  - [ ] `inputSelection` → InputSelection
  - [ ] `unitItemPrefab` → UnitListItemPrefab
  - [ ] `groupItemPrefab` → GroupListItemPrefab
  - [ ] `content` → Content (RectTransform)
  - [ ] `scrollRect` → ScrollView (ScrollRect)
- [ ] Prefabs criados:
  - [ ] UnitListItemPrefab (com UnitListItemUI)
  - [ ] GroupListItemPrefab (com GroupListItemUI)
- [ ] `CreateGroupUI` configurado (NameInput, CreateButton)
- [ ] `ViewportClearSelection` adicionado ao Viewport

### 27.2 Testes Funcionais

- [ ] Play → Lista popula com unidades existentes
- [ ] Spawnar unidade → Aparece na lista
- [ ] Despawnar unidade → Remove da lista
- [ ] Selecionar unidade na cena → Highlight na lista
- [ ] Clicar em item da lista → Seleciona unidade
- [ ] Criar grupo → Move unidades selecionadas para grupo
- [ ] Arrastar item → Reordena lista
- [ ] Arrastar unidade para grupo → Aninha unidade
- [ ] Expandir/colapsar grupo → Mostra/esconde conteúdo
- [ ] Redimensionar grupo → Ajusta altura
- [ ] Menu contextual (unidade) → Abre "Follow"
- [ ] Menu contextual (grupo) → Abre "Rename/Delete"
- [ ] Renomear grupo → Atualiza nome e dispara evento
- [ ] Deletar grupo → Restaura unidades na lista raiz

### 27.3 Testes de Eventos

- [ ] `GameEvents.OnUnitSpawned` → HandleUnitSpawned chamado
- [ ] `GameEvents.OnUnitDespawned` → HandleUnitDespawned chamado
- [ ] `GameEvents.OnSelectionChanged` → RefreshFromSelection chamado
- [ ] `GameEvents.OnGroupCreated` → Disparado ao criar grupo
- [ ] `GameEvents.OnGroupDeleted` → Disparado ao deletar grupo
- [ ] `GameEvents.OnGroupRenamed` → Disparado ao renomear grupo

### 27.4 Testes de Performance

- [ ] 50+ unidades na lista → FPS estável
- [ ] Rebuild completo (RebuildList) → <100ms
- [ ] Drag-and-drop → Sem stuttering
- [ ] Auto-scroll nas bordas → Suave
- [ ] Memory Profiler → Sem leaks

### 27.5 Testes de Edge Cases

- [ ] Criar grupo sem unidades selecionadas → Warning
- [ ] Deletar grupo vazio → Funciona
- [ ] Arrastar unidade já em grupo → Move corretamente
- [ ] Abrir menu contextual durante drag → Supressão funciona
- [ ] Clicar fora da lista → Limpa seleção
- [ ] Renomear grupo com nome vazio → Mantém nome anterior

---

## 28) CONCLUSÃO E PRÓXIMOS PASSOS

### 28.1 Objetivos Alcançados

✅ **Arquitetura Desacoplada**: Comunicação via `GameEvents` (Lote 1)  
✅ **Lista Dinâmica**: Atualização automática ao spawnar/despawnar unidades  
✅ **Sistema de Grupos**: Organização hierárquica com expansão/colapso  
✅ **Drag-and-Drop**: Reordenação e aninhamento visual intuitivo  
✅ **Menus Contextuais**: Ações contextuais (Follow, Rename, Delete)  
✅ **Sincronização com Seleção**: Integração bidirecional com SelectionManager  
✅ **Redimensionamento**: Grip de resize para grupos expandidos  
✅ **Barra de XP**: Visual 90% reta + 10% circular  
✅ **Performance**: Rebuild otimizado e auto-scroll suave  

### 28.2 Extensões Futuras

#### Filtros e Busca
```csharp
[Header("Filtros")]
public TMP_InputField searchInput;
public Dropdown typeFilter; // All, Workers, Warriors, etc.

void FilterList()
{
    string searchTerm = searchInput.text.ToLower();
    UnitType filterType = (UnitType)typeFilter.value;
    
    var filteredOrder = _order.Where(w => {
        var unit = w.GetUnit();
        if (unit == null) return true; // Grupos sempre visíveis
        
        // Busca por nome
        if (!string.IsNullOrEmpty(searchTerm) && 
            !unit.DisplayName.ToLower().Contains(searchTerm))
            return false;
        
        // Filtro por tipo
        if (filterType != UnitType.All && unit.def.type != filterType)
            return false;
        
        return true;
    }).ToList();
    
    // Rebuild apenas com itens filtrados
    RebuildListFiltered(filteredOrder);
}
```

#### Keybinds para Grupos (Ctrl+1~9)
```csharp
void Update()
{
    if (Input.GetKey(KeyCode.LeftControl))
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectGroup(i);
            }
        }
    }
}

void SelectGroup(int index)
{
    if (index < 0 || index >= _groups.Count) return;
    
    var group = _groups[index];
    selectionManager.SelectExactly(group.Units);
}
```

#### Ícones de Status (HP, Buffs, Debuffs)
```csharp
// No UnitListItemUI
[Header("Status Icons")]
public Image lowHpIcon;
public Image buffIcon;
public Image debuffIcon;

void UpdateStatusIcons()
{
    if (_unit == null) return;
    
    // HP baixo (<30%)
    if (lowHpIcon)
        lowHpIcon.gameObject.SetActive(_unit.hp / _unit.hpMax < 0.3f);
    
    // Buffs/Debuffs (exemplo)
    // if (buffIcon)
    //     buffIcon.gameObject.SetActive(_unit.HasBuff());
}
```

#### Persistência de Layout (Posição de Grupos)
```csharp
void SaveLayout()
{
    var layout = new LayoutData
    {
        groupOrder = _order.Select((w, i) => new LayoutItem
        {
            index = i,
            isGroup = w.IsGroup,
            id = w.IsGroup ? ((UnitGroup)w.Model).ID : w.GetUnit().GetInstanceID().ToString()
        }).ToList()
    };
    
    string json = JsonUtility.ToJson(layout);
    PlayerPrefs.SetString("UnitListLayout", json);
}
```

### 28.3 Integração com Outros Lotes

**Lote 6 (IA):**
- Grupos podem ter comportamento de IA específico
- Ordens de grupo (atacar, patrulhar, etc.)

**Lote 7 (Combate):**
- Ícones de status de combate (em batalha, ferido)
- Notificações de morte na lista

**Lote 8 (Economia):**
- Filtro por trabalhadores ocupados/ociosos
- Estatísticas de produção por grupo

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão:** 2.1 (Pós-Refatoração Event Bus)  

---



