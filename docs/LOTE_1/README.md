# LOTE 1 — CORE SYSTEM

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