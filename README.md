# MedievalThrones

# PROJETO DE DOCUMENTAÇÃO DETALHADA — Jogo RTS

**Versão:** 2.0 (Atualizada - Outubro 2025)  
**Status:** ✅ Refatorado e Otimizado

---

## LOTE 1 — Variáveis Globais & Módulo Factions

### Objetivo

Oferecer uma referência técnica clara, rastreável e reutilizável dos módulos "Variáveis Globais" (tempo/clock, eventos e configuração) e "Factions" (definições, banco e serviço de reputação), alinhada à sua base de código atual em Unity/C#.

---

## 1) VISÃO GERAL DO MÓDULO

### 1.1 Variáveis Globais (Tempo, Config, Eventos, Contexto)

Responsável por centralizar configurações globais (ex.: duração do dia, penalidades, baseline de economia), gestão do tempo de jogo (fração do dia, dia/hora/minuto, virada do dia) e eventos de gameplay (clock, economia, diplomacia, câmera). O `GameContext` atua como ponto orquestrador, inicializando serviços e injetando referências entre os sistemas.

### 1.2 Módulo Factions (Definições, Banco, Reputação)

Fornece o modelo de dados de Factions (id, nome, cor, banner e reputação inicial), um banco de Factions para lookup por id e um serviço de reputação para manter uma matriz de relacionamento A→B no runtime e emitir eventos de mudança, favorecendo IA/diplomacia reativa.

---

## 2) ESTRUTURA DAS CLASSES E RELACIONAMENTOS

### 2.1 Enums.cs

**Arquivo/Classe:** `Enums` (enums globais)

**Conteúdo:**
- **FactionId** — Identificadores de facção (Neutral, Player1… PvE)
- **ResourceType** — Tipos de recurso (Wood, Stone, ... Gold)
- **DamageType** — Tipos de dano (Slashing, Piercing, ... Magic)
- **TerrainType** — Tipos de terreno (Normal, Mud, ... RoadPaved)
- **UnitType** — Tipos de unidade (Worker, Warrior, ... Hero)

**Relacionamentos:** Usado de forma transversal por sistemas de economia, combate, pathing e UI. `FactionId` é chave no módulo de Factions e no `PlayerController`.

---

### 2.2 GameConfig.cs

**Tipo:** `ScriptableObject`

**Responsabilidade:** Armazenar configuração global do jogo (tempo de dia, fração diurna/nocturna, penalidades de clima/terreno, baseline de economia, custos/unidades especiais, reparos e helpers).

**Principais campos:**

**Tempo:**
- `secondsPerDay`
- `dayFraction`

**Clima/Mod.:**
- `nightVisionPenalty`
- `fogVisionPenalty`
- `mudSandMovePenalty`
- `snowExtraUpkeep`

**Economia:**
- `baselineGatherMinTotal`
- `baselineGatherPer3Min`
- `workerCarryCapacity`

**Unid. especiais:**
- `merchantSpeedHexPerSec`
- `merchantCapacity`
- `merchantCostGold`

**Reparos:**
- `structureRepairHpPerSec`

**Helpers:**
- `BaselinePerSecond { get; }`
- `SecondsPerDay { get; }`

**Relacionamentos:** Usado por `TimeManager` (tempo) e por outros sistemas que precisem de balizadores globais.

---

### 2.3 GameEvents.cs (ATUALIZADO)

**Tipo:** `static class`

**Responsabilidade:** Surface de eventos globais para desacoplamento entre sistemas.

**Eventos expostos:**

#### Tempo/Clock
- `Action<float> OnTimeOfDay01` — fração 0..1 do dia
- `Action<int> OnDayChanged` — incrementa no loop diário
- `Action<int, int, int> OnClockChanged` — dia, hora, minuto

#### Economia
- `Action<FactionId, ResourceType, int> OnResourceGathered`

#### Diplomacia
- `Action OnReputationMatrixReady` — reputação seed pronta
- `Action<FactionId, FactionId, float> OnReputationChanged` — mudança A→B

#### Câmera (NOVO)
- `Action<float, float, float> OnCameraShake` — amplitude, frequency, duration
- `Action<Vector3, bool, float> OnCameraFocus` — worldPos, snap, duration
- `Action<Vector2, bool, float> OnCameraFocusXZ` — worldXZ, snap, duration
- `Action<Transform, float, int> OnCutsceneStart` — target, fov, priority
- `Action OnCutsceneEnd`

#### Seleção/Minimap (NOVO)
- `Action<Vector2> OnMinimapPing` — worldXZ
- `Action<Transform> OnSelectionFocus` — target transform

**Helpers:** Raise* para disparo centralizado de todos os eventos acima.

**Relacionamentos:** Observado por `DayNightLightController` (cor/intensidade do sol), por sistemas de UI/clock, IA/economia/diplomacia e pelo sistema de câmera.

---

### 2.4 GameContext.cs

**Tipo:** `MonoBehaviour`

**Responsabilidade:** Ponto de orquestração na cena. Injeta config e inicializa serviços (`FactionService.Init`, configuração do `TimeManager`).

**Relacionamentos:** Guarda referências a `GameConfig`, `FactionDatabase`, `FactionService`, `TimeManager` e chama inicialização na `Awake`.

---

### 2.5 TimeManager.cs

**Tipo:** `MonoBehaviour`

**Responsabilidade:** Avança o relógio global, computa fração do dia (0..1), dia/hora/minuto e emite os eventos de tempo.

**Fluxo:**
- **Awake:** inicializa `Time01` (fração) e publica `RaiseTimeOfDay`
- **Update:** usa `config.SecondsPerDay` para avançar `Time01`, detecta virada de dia, converte para HH:MM, dispara "tic" por minuto (`RaiseClockChanged`) e `RaiseTimeOfDay` a cada frame
- **IsNight():** utilitário que compara `Time01` com `config.dayFraction`

**Relacionamentos:** Consome `GameConfig`; emite eventos de tempo; observado por `DayNightLightController` e sistemas dependentes de ciclo dia/noite.

---

### 2.6 DayNightLightController.cs

**Tipo:** `MonoBehaviour`

**Responsabilidade:** Ajustar cor e intensidade de uma Directional Light com base em `TimeOfDay` (0..1) e aplicar rotação simples do "sol".

**Relacionamentos:** Se inscreve em `GameEvents.OnTimeOfDay01` para reagir ao ciclo.

---

### 2.7 FactionDefinition.cs

**Tipo:** `ScriptableObject`

**Responsabilidade:** Metadados individuais de uma facção: id, displayName, color, banner e initialReputation (padrão neutro 50).

**Relacionamentos:** Instanciada no `FactionDatabase`; consumida por UI (cores/bandeiras) e por `FactionService` (seed de reputação).

---

### 2.8 FactionDatabase.cs

**Tipo:** `ScriptableObject`

**Responsabilidade:** Coleção de `FactionDefinition` e lookup por id.

**Relacionamentos:** Referenciada por `FactionService` e opcionalmente por UI/editor tools.

---

### 2.9 FactionService.cs

**Tipo:** `MonoBehaviour`

**Responsabilidade:** Manter, em runtime, a matriz de reputação A→B (0..100), inicializada a partir de `FactionDatabase` (via `initialReputation`). Expõe leitura/escrita e delta com eventos (`ReputationChanged`, `ReputationMatrixReady`).

**Relacionamentos:** Depende de `FactionDatabase`. Observado por IA/diplomacia/trigger systems e UI de relações.

---

### 2.10 PlayerController.cs

**Tipo:** `MonoBehaviour`

**Responsabilidade:** Define a facção do player (`myFaction`) e mantém referência à `mainCamera`.

**Relacionamentos:** Usa `FactionId` e integra com sistemas de input/câmera. Interage indiretamente com Factions (por exemplo, consultas de reputação player→outros).

---

## 3) REFERÊNCIA DE API (Membros Públicos)

### Enums.cs

```csharp
enum FactionId { Neutral, Player1, Player2, Player3, Player4, PvE }
enum ResourceType { Wood, Stone, Iron, Mithril, Food, Gold }
enum DamageType { Slashing, Piercing, Blunt, Siege, Fire, Magic }
enum TerrainType { Normal, Mud, Snow, Sand, RoadDirt, RoadPaved }
enum UnitType { Worker, Warrior, Spearman, Archer, CavalryLight, Ram, Catapult, Hero }
```

**Exemplos de uso:**

```csharp
// Selecionar cor da UI pela facção
var fdef = factionDb.Get(FactionId.Player1);
uiTeamBanner.color = fdef.color;

// Ajustar dano por tipo
if (attack.DamageType == DamageType.Siege) ApplyBonusVsStructures();
```

---

### GameConfig (ScriptableObject)

**Campos:**

**Tempo:**
- `float secondsPerDay`
- `float dayFraction`

**Clima/Mod.:**
- `float nightVisionPenalty`
- `float fogVisionPenalty`
- `float mudSandMovePenalty`
- `float snowExtraUpkeep`

**Economia:**
- `float baselineGatherMinTotal`
- `int baselineGatherPer3Min`
- `int workerCarryCapacity`

**Unid. especiais:**
- `float merchantSpeedHexPerSec`
- `int merchantCapacity`
- `int merchantCostGold`

**Reparos:**
- `float structureRepairHpPerSec`

**Helpers:**
- `float BaselinePerSecond { get; }`
- `float SecondsPerDay { get; }`

**Exemplos de uso:**

```csharp
// Converter baseline por segundo
float perSec = gameConfig.BaselinePerSecond;

// Checar se é noite com o threshold global
bool isNight = timeManager.IsNight();
```

---

### GameEvents (static) - ATUALIZADO

**Eventos (subscribe/unsubscribe):**

#### Tempo
- `Action<float> OnTimeOfDay01` — fração 0..1 do dia
- `Action<int> OnDayChanged` — incrementa no loop diário
- `Action<int,int,int> OnClockChanged` — dia, hora, minuto

#### Economia
- `Action<FactionId, ResourceType, int> OnResourceGathered`

#### Diplomacia
- `Action OnReputationMatrixReady` — reputação seed pronta
- `Action<FactionId,FactionId,float> OnReputationChanged` — mudança A→B

#### Câmera (NOVO)
- `Action<float, float, float> OnCameraShake` — shake com amplitude, frequency, duration
- `Action<Vector3, bool, float> OnCameraFocus` — foco em posição 3D
- `Action<Vector2, bool, float> OnCameraFocusXZ` — foco em posição XZ (mini-mapa)
- `Action<Transform, float, int> OnCutsceneStart` — iniciar cutscene
- `Action OnCutsceneEnd` — finalizar cutscene

#### Seleção/Minimap (NOVO)
- `Action<Vector2> OnMinimapPing` — ping no mini-mapa
- `Action<Transform> OnSelectionFocus` — foco em unidade selecionada

**Helpers (raise):**

```csharp
// Tempo
RaiseTimeOfDay(float)
RaiseDayChanged(int)
RaiseClockChanged(int, int, int)

// Economia
RaiseResourceGathered(FactionId, ResourceType, int)

// Diplomacia
RaiseReputationMatrixReady()
RaiseReputationChanged(FactionId, FactionId, float)

// Câmera
RaiseCameraShake(float amplitude, float frequency, float duration)
RaiseCameraFocus(Vector3 worldPos, bool snap, float duration)
RaiseCameraFocusXZ(Vector2 worldXZ, bool snap, float duration)
RaiseCutsceneStart(Transform target, float fov, int priority)
RaiseCutsceneEnd()

// Seleção/Minimap
RaiseMinimapPing(Vector2 worldXZ)
RaiseSelectionFocus(Transform target)
```

**Exemplo de uso (UI/Clock):**

```csharp
void OnEnable() {
    GameEvents.OnClockChanged += HandleClock;
}

void OnDisable() {
    GameEvents.OnClockChanged -= HandleClock;
}

void HandleClock(int day, int hour, int minute) {
    clockLabel.text = $"D{day} {hour:00}:{minute:00}";
}
```

**Exemplo de uso (Câmera):**

```csharp
// Sistema de Combate dispara shake
void OnExplosion() {
    GameEvents.RaiseCameraShake(amplitude: 2f, frequency: 3f, duration: 0.3f);
}

// UI de Mini-mapa dispara foco
void OnMinimapClick(Vector2 worldXZ) {
    GameEvents.RaiseMinimapPing(worldXZ);
}
```

---

### GameContext (MonoBehaviour)

**Campos públicos:**
- `GameConfig config`
- `FactionDatabase factions`
- `FactionService factionService`
- `TimeManager timeManager`

**Ciclo de vida:** `Awake()` chama `factionService.Init()` e injeta config no `timeManager`.

---

### TimeManager (MonoBehaviour)

**Config:**
- `GameConfig config`
- `float startTime01`

**Propriedades públicas:**
- `float Time01 { get; private set; }` — fração do dia 0..1
- `int DayCount { get; private set; }`
- `int Hour { get; private set; }`
- `int Minute { get; private set; }`

**Métodos:**
- `Awake()` — inicializa Time01 e emite primeiro OnTimeOfDay01
- `Update()` — avança tempo, computa HH:MM, dispara OnClockChanged, OnDayChanged quando vira o dia, e OnTimeOfDay01 a cada frame
- `bool IsNight()` — confere se Time01 >= config.dayFraction

---

### DayNightLightController (MonoBehaviour)

**Campos:**
- `Gradient colorOverDay`
- `AnimationCurve intensityOverDay`

**Inscrição:** `OnEnable()` subscreve `GameEvents.OnTimeOfDay01` e `OnDisable()` cancela.

---

### FactionDefinition (ScriptableObject)

**Campos:**
- `FactionId id`
- `string displayName`
- `Color color`
- `Sprite banner`
- `float initialReputation` (0..100)

---

### FactionDatabase (ScriptableObject)

**Campos:**
- `List<FactionDefinition> factions`

**Métodos:**
- `FactionDefinition Get(FactionId id)`

---

### FactionService (MonoBehaviour)

**Campos:**
- `FactionDatabase database`

**Métodos públicos:**
- `void Init()` — constrói matriz de reputação A→B usando initialReputation
- `float GetReputation(FactionId a, FactionId b)` — lê reputação A→B
- `void SetReputation(FactionId a, FactionId b, float value)` — clamp 0..100, atualiza matriz e dispara ReputationChanged
- `void DeltaReputation(FactionId a, FactionId b, float delta)` — aplica variação incremental

---

### PlayerController (MonoBehaviour)

**Campos:**
- `FactionId myFaction`
- `Camera mainCamera`

---

## 4) EVENTOS — EMISSÃO & ASSINATURA

**Disparados por:**
- **TimeManager:** RaiseTimeOfDay, RaiseDayChanged, RaiseClockChanged
- **FactionService:** RaiseReputationMatrixReady, RaiseReputationChanged
- **Sistemas de economia (futuros):** RaiseResourceGathered
- **Sistemas de gameplay:** eventos de câmera e seleção

**Escutados por:**
- **DayNightLightController:** OnTimeOfDay01
- **UI de clock/mini-mapa/log:** OnClockChanged, OnDayChanged
- **UI de diplomacia/IA:** OnReputation*
- **Sistema de câmera:** eventos de câmera e seleção

**Padrões recomendados:**
- **Desacoplamento:** proibir dependência direta entre UI e lógica de tempo/diplomacia; trabalhar via GameEvents
- **Unsubscribe:** garantir OnDisable()/OnDestroy() para evitar vazamento

---

## 5) VARIÁVEIS-CHAVE / CONFIGURAÇÕES (DESTAQUES)

- **GameConfig.secondsPerDay:** unidade de loop do ciclo diurno—impacta iluminação, spawn, AI schedules
- **GameConfig.dayFraction:** limiar entre dia/noite para utilitários como TimeManager.IsNight()
- **Penalidades globais** (nightVisionPenalty, mudSandMovePenalty, etc.): base para modificadores de visão/mobilidade/upkeep
- **Economia baseline** (baseline*, workerCarryCapacity): referência para tuning de gathering/pathing e balance
- **Reputação inicial** (FactionDefinition.initialReputation): seed da matriz A→B; ajustes mudam o "estado diplomático" inicial do mundo

---

## 6) EXEMPLOS COMPOSTOS (END-TO-END)

### 6.1 Iluminação diurna + UI de relógio

```csharp
void OnEnable() {
    GameEvents.OnTimeOfDay01 += t => dirLight.material.SetFloat("_Sun", t);
    GameEvents.OnClockChanged += (d,h,m) => clockTxt.text = $"D{d} {h:00}:{m:00}";
}

void OnDisable() {
    GameEvents.OnTimeOfDay01 -= ...;
    GameEvents.OnClockChanged -= ...;
}
```

### 6.2 Diplomacia reativa pós-evento de jogo

```csharp
// Ex.: ao concluir missão de escolta para facção PvE
factionService.DeltaReputation(player.myFaction, FactionId.PvE, +15f);
// Observadores recebem OnReputationChanged(a,b,value)
```

---

## 7) NOTAS DE IMPLEMENTAÇÃO & BOAS PRÁTICAS

- **Ordem de inicialização:** manter `_GameContext` como ponto único; assegurar referências via Inspector
- **ScriptableObjects:** preferir SO para dados estáticos (Config/Faction); favorece edição e serialização
- **Eventos:** usar GameEvents como "Event Bus" do projeto; manter handlers curtos e resilientes
- **Testabilidade:** onde possível, mover lógica pura (ex.: conversão Time01→HH:MM) para métodos estáticos/serviços testáveis

---

# LOTE 2 — Sistema de Câmeras (Cinemachine v3 + Input System)

**Versão:** 2.0 (Refatorado)  
**Status:** ✅ Implementado com arquitetura desacoplada e testável

---

## 1) VISÃO GERAL DO MÓDULO

Gerencia a movimentação do rig de câmera em um RTS usando **Cinemachine v3 (CM3)** e **Unity Input System**: pan (WASD/borda/drag), zoom (altura + tilt), rotação, limites de mundo (bounds) e utilitários como shake e cutscenes.

### Características Principais (Pós-Refatoração):

✅ Configurações centralizadas em ScriptableObject (`RTSCameraProfile`)  
✅ Comunicação desacoplada via eventos (`GameEvents`)  
✅ Lógica matemática testável isolada em classe estática (`CameraMath`)  
✅ Curvas de suavização customizáveis por perfil  
✅ Suporte a cutscenes, shakes e navegação via mini-mapa  
✅ Bounds automáticos calculados a partir de Terrains

O módulo separa entrada (leitura de ações do Input System) do controle da câmera (aplicação no rig/CM3), favorecendo testabilidade e substituição de camadas.

---

## 2) ESTRUTURA DAS CLASSES

### Arquitetura do Sistema:

```
RTSCameraProfile (ScriptableObject)
        ↓ (configurações)
RTSCameraCinemachineV3Controller (MonoBehaviour)
    ↑ (eventos)        ↓ (cálculos)
GameEvents (static)   CameraMath (static)
    ↑ (dispara)
RTSCameraInputSystem (MonoBehaviour)
        ↑ (lê)
Unity Input System
```

---

### 2.1 RTSCameraProfile.cs (NOVO)

**Tipo:** `ScriptableObject`  
**Localização:** Assets > Create > Game > Camera Profile

**Responsabilidade:** Centralizar todas as configurações de câmera em um asset reutilizável, permitindo criar múltiplos perfis (Default, Cinematic, Spectator, etc.).

#### Campos principais:

```csharp
[Header("Pan (WASD / Borda / Drag)")]
public float panSpeedNear = 20f;        // Velocidade quando próximo
public float panSpeedFar = 35f;         // Velocidade quando distante
public int edgeThickness = 12;          // Espessura da borda (px)
public float middleDragSensitivity = 1f; // Sensibilidade do arrasto

[Header("Zoom (Altura + Tilt)")]
public float zoomSpeed = 0.15f;         // Velocidade de zoom
public float minHeight = 10f;           // Altura mínima
public float maxHeight = 60f;           // Altura máxima
public float minTilt = 35f;             // Tilt mínimo (graus)
public float maxTilt = 75f;             // Tilt máximo (graus)

[Header("Rotation")]
public float rotateSpeed = 90f;         // Velocidade (graus/seg)

[Header("Curvas de Suavização")]
public AnimationCurve panSpeedCurve;    // Curva de velocidade por zoom
public AnimationCurve zoomCurve;        // Curva de suavização do zoom

[Header("Flags de Interação")]
public bool enableWASD = true;
public bool enableEdgePan = true;
public bool enableMiddleDrag = true;
public bool enableRotate = true;
public bool pauseWhenPointerOverUI = true;
```

#### Métodos públicos:

```csharp
// Calcula velocidade de pan baseada no zoom atual (0..1)
float GetPanSpeed(float zoom01)

// Aplica suavização no valor de zoom usando a curva
float ApplyZoomCurve(float rawZoom01)
```

**Exemplo de uso:**

```csharp
// Criar no Editor: Assets > Create > Game > Camera Profile
// Configurar valores no Inspector
// Arrastar para o campo "Profile" do RTSCameraCinemachineV3Controller
```

---

### 2.2 CameraMath.cs (NOVO)

**Tipo:** `static class`

**Responsabilidade:** Utilitários matemáticos puros para cálculos de câmera (testáveis unitariamente).

#### Métodos públicos:

```csharp
// Converte altura e tilt em offset 3D para CinemachineFollow
static Vector3 TiltToOffset(float height, float tiltDegrees, Vector3 forwardDirection)

// Suavização cúbica (ease in-out) 0..1
static float SmoothStep01(float t)

// Lerp com suavização cúbica
static Vector3 SmoothLerp(Vector3 start, Vector3 end, float t)

// Clamp de posição XZ dentro de bounds retangulares
static Vector3 ClampToBounds(Vector3 position, Vector2 boundsCenter, Vector2 boundsSize)

// Converte posição XZ (2D) em Vector3 mantendo altura Y
static Vector3 XZToVector3(Vector2 xz, float y)

// Calcula bounds a partir de múltiplos terrenos
static bool CalculateTerrainBounds(Terrain[] terrains, out Vector2 center, out Vector2 size)

// Detecta direção de edge-pan por posição de tela
static Vector2 GetEdgePanDirection(Vector2 screenPos, int edgeThickness, int screenWidth, int screenHeight)
```

**Exemplo de uso:**

```csharp
// Uso em sistemas customizados (IA, pathing, etc.)
Vector3 offset = CameraMath.TiltToOffset(height: 30f, tiltDegrees: 45f, transform.forward);

// Movimento suave customizado
Vector3 smoothPos = CameraMath.SmoothLerp(startPos, targetPos, time);

// Clampar unidade dentro do mapa
unitPos = CameraMath.ClampToBounds(unitPos, mapCenter, mapSize);
```

---

### 2.3 RTSCameraCinemachineV3Controller.cs (REFATORADO)

**Tipo:** `MonoBehaviour`

**Responsabilidade:** Controlador principal do rig da câmera. Usa `RTSCameraProfile`, escuta `GameEvents` e delega cálculos matemáticos para `CameraMath`.

#### Campos públicos (Inspector):

```csharp
[Header("Profile & References")]
public RTSCameraProfile profile;        // ScriptableObject de config
public CinemachineCamera vcam;          // Câmera virtual (auto-criada)
public Transform pivot;                 // Pivot para tilt (auto-criado)

[Header("Bounds (Mundo)")]
public Vector2 boundsCenter = Vector2.zero;     // Centro (x,z)
public Vector2 boundsSize = new Vector2(200, 200);

[Header("Initial Setup")]
public Vector3 initialPosition = Vector3.zero;
public float initialHeading = 0f;
[Range(0, 1)] public float initialZoom = 0.5f;

[Header("Runtime State (Read-Only)")]
[SerializeField, Range(0, 1)] private float _zoom = 0.5f;
public float Zoom => _zoom; // Getter público
```

#### Métodos públicos:

```csharp
// Processa entrada por frame (chamado pelo RTSCameraInputSystem)
void TickInput(Vector2 wasdMove, Vector2 pointerPosition, bool isMiddleDragging, 
               Vector2 pointerDelta, float rotateAxis, float zoomAxis, bool pointerOverUI)

// Dispara shake temporário (também chamável por eventos)
void PlayShake(float amplitude = 1.2f, float frequency = 2.0f, float duration = 0.25f)

// Iniciar/finalizar cutscene
void StartCutscene(Transform target, float fov = 50f, int priority = 100)
void EndCutscene()

// Navegação (mini-mapa / "jump to")
void GoToXZ(Vector2 worldXZ, bool snap = false, float duration = 0.4f)
void GoTo(Vector3 worldPos, bool snap = false, float duration = 0.4f)

// Calcular bounds automaticamente a partir de Terrains
[ContextMenu] void SetBoundsFromTerrains()
```

#### Eventos escutados:

```csharp
void OnEnable() {
    GameEvents.OnCameraShake += HandleCameraShake;
    GameEvents.OnCameraFocus += HandleCameraFocus;
    GameEvents.OnCameraFocusXZ += HandleCameraFocusXZ;
    GameEvents.OnCutsceneStart += HandleCutsceneStart;
    GameEvents.OnCutsceneEnd += HandleCutsceneEnd;
    GameEvents.OnMinimapPing += HandleMinimapPing;
    GameEvents.OnSelectionFocus += HandleSelectionFocus;
}
```

#### Fluxo interno:

1. **Awake()** → EnsureSetup() (cria pivot/câmeras/componentes CM3), aplica setup inicial
2. **TickInput()** → chama HandlePan/Rotate/Zoom(), depois ClampToBounds() e UpdateFollowOffset()
3. **HandlePan/Rotate/Zoom()** → aplicam lógica de movimento usando valores do profile
4. **UpdateFollowOffset()** → usa CameraMath.TiltToOffset() para converter zoom/tilt em offset espacial

**Exemplo de uso:**

```csharp
// Chamado automaticamente pelo RTSCameraInputSystem
_controller.TickInput(wasd, mousePos, middleHeld, mouseDelta, rotAxis, zoomAxis, uiOver);

// Ou chamado por eventos de outros sistemas:
GameEvents.RaiseCameraShake(1.5f, 3f, 0.2f);
GameEvents.RaiseMinimapPing(pingWorldXZ);
```

---

### 2.4 RTSCameraInputSystem.cs (REFATORADO)

**Tipo:** `MonoBehaviour`  
**Requer:** `RTSCameraCinemachineV3Controller`

**Responsabilidade:** Tradutor de ações do Input System para a API do controlador (TickInput).

#### Campos (Inspector):

```csharp
[Header("Action References (arraste do .inputactions)")]
public InputActionReference move;           // Vector2 (WASD)
public InputActionReference rotate;         // 1D Axis (Q/E)
public InputActionReference zoom;           // Axis (Mouse scroll)
public InputActionReference pointerPos;     // Vector2 (Mouse position)
public InputActionReference pointerDelta;   // Vector2 (Mouse delta)
public InputActionReference middleButton;   // Button (Mouse middle)
```

#### Ciclo de vida:

- **OnEnable()/OnDisable()** — habilita/desabilita ações; conecta callbacks do botão do meio
- **Update()** — lê ações, detecta hover de UI e invoca `_cam.TickInput(...)`

**Exemplo de uso:**

```csharp
// No prefab da câmera:
// 1) Anexe RTSCameraCinemachineV3Controller e RTSCameraInputSystem
// 2) Ligue cada ActionReference do .inputactions
// 3) Atribua um RTSCameraProfile ao controller
```

---

## 3) RELACIONAMENTOS E DEPENDÊNCIAS

### Dependências Externas:

- **Cinemachine 3:** CinemachineCamera, CinemachineFollow, CinemachineRotationComposer, CinemachineBasicMultiChannelPerlin
- **Unity Input System:** InputActionReference, InputAction
- **UI/EventSystem:** Detecção de ponteiro sobre UI (EventSystem.current.IsPointerOverGameObject())
- **Terrain:** Cálculo automático de bounds (Terrain.activeTerrains)

### Fluxo de Comunicação:

1. **Input System** → RTSCameraInputSystem (lê ações)
2. **RTSCameraInputSystem** → RTSCameraCinemachineV3Controller.TickInput() (repassa valores)
3. **RTSCameraCinemachineV3Controller** → RTSCameraProfile (consulta configs)
4. **RTSCameraCinemachineV3Controller** → CameraMath (cálculos matemáticos)
5. **RTSCameraCinemachineV3Controller** → Cinemachine Components (atualiza câmera)
6. **Outros Sistemas** → GameEvents (disparam shake/focus/cutscene)
7. **GameEvents** → RTSCameraCinemachineV3Controller (executa ações)

---

## 4) REFERÊNCIA DE API COMPLETA

### 4.1 RTSCameraProfile (ScriptableObject)

#### Campos de Configuração:

**Pan (Movimento)**
```csharp
public float panSpeedNear = 20f;            // Velocidade quando próximo (zoom 0)
public float panSpeedFar = 35f;             // Velocidade quando distante (zoom 1)
public int edgeThickness = 12;              // Espessura da borda em pixels
public float middleDragSensitivity = 1f;    // Sensibilidade do arrasto com botão do meio
```

**Zoom (Altura + Tilt)**
```csharp
public float zoomSpeed = 0.15f;     // Velocidade de zoom (scroll)
public float minHeight = 10f;       // Altura mínima da câmera
public float maxHeight = 60f;       // Altura máxima da câmera
public float minTilt = 35f;         // Ângulo de tilt mínimo (graus)
public float maxTilt = 75f;         // Ângulo de tilt máximo (graus)
```

**Rotação**
```csharp
public float rotateSpeed = 90f;     // Velocidade de rotação (graus/seg)
```

**Curvas de Suavização (Avançado)**
```csharp
public AnimationCurve panSpeedCurve;    // Curva de velocidade por zoom (X=zoom 0-1, Y=mult)
public AnimationCurve zoomCurve;        // Curva de suavização do zoom (X=entrada, Y=saída)
```

**Flags de Interação**
```csharp
public bool enableWASD = true;
public bool enableEdgePan = true;
public bool enableMiddleDrag = true;
public bool enableRotate = true;
public bool pauseWhenPointerOverUI = true;
```

#### Métodos Públicos:

```csharp
/// <summary>
/// Calcula a velocidade de pan interpolada baseada no zoom atual (0..1).
/// Aplica a curva customizada se configurada.
/// </summary>
public float GetPanSpeed(float zoom01)

/// <summary>
/// Aplica suavização no valor de zoom usando a curva configurada.
/// </summary>
public float ApplyZoomCurve(float rawZoom01)
```

#### Como Criar e Usar:

**1. Criar Profile:**
```
Unity Editor → Assets > Create > Game > Camera Profile
```

**2. Configurar no Inspector:**
- Ajuste velocidades, limites, curvas conforme necessário
- Configure flags de interação (WASD, edge-pan, etc.)

**3. Atribuir ao Controller:**
- Arraste o Profile para o campo "Profile" do RTSCameraCinemachineV3Controller

**4. Criar Variações (Opcional):**
- **Default:** Valores balanceados para gameplay normal
- **Cinematic:** Pan lento, curvas suaves, sem edge-pan
- **Spectator:** Pan rápido, zoom amplo, sem restrições de UI

---

### 4.2 CameraMath (static class)

#### Métodos Públicos:

```csharp
/// <summary>
/// Converte altura e ângulo de tilt em offset 3D para o CinemachineFollow.
/// Retorna o vetor de offset no espaço mundial.
/// </summary>
/// <param name="height">Altura da câmera acima do pivot</param>
/// <param name="tiltDegrees">Ângulo de inclinação em graus</param>
/// <param name="forwardDirection">Direção forward do rig</param>
public static Vector3 TiltToOffset(float height, float tiltDegrees, Vector3 forwardDirection)

/// <summary>
/// Suavização cúbica (ease in-out) para interpolação de movimento.
/// Entrada e saída normalizadas 0..1
/// </summary>
public static float SmoothStep01(float t)

/// <summary>
/// Interpola com suavização cúbica entre dois pontos.
/// </summary>
public static Vector3 SmoothLerp(Vector3 start, Vector3 end, float t)

/// <summary>
/// Clamp de posição 2D (XZ) dentro de bounds retangulares.
/// </summary>
public static Vector3 ClampToBounds(Vector3 position, Vector2 boundsCenter, Vector2 boundsSize)

/// <summary>
/// Converte posição XZ (2D do mundo) em Vector3 mantendo a altura Y.
/// </summary>
public static Vector3 XZToVector3(Vector2 xz, float y)

/// <summary>
/// Calcula o centro e tamanho de bounds a partir de múltiplos terrenos.
/// </summary>
public static bool CalculateTerrainBounds(Terrain[] terrains, out Vector2 center, out Vector2 size)

/// <summary>
/// Verifica se uma posição de tela está dentro da borda para edge-pan.
/// Retorna direção normalizada (-1, 0, 1) em X e Y.
/// </summary>
public static Vector2 GetEdgePanDirection(Vector2 screenPos, int edgeThickness, 
                                          int screenWidth, int screenHeight)
```

#### Exemplos de Uso:

**Sistemas de Câmera:**
```csharp
// Converter altura/tilt em offset do Cinemachine
Vector3 offset = CameraMath.TiltToOffset(30f, 45f, transform.forward);
_follow.FollowOffset = offset;

// Movimento suave para "jump to"
Vector3 smoothPos = CameraMath.SmoothLerp(startPos, targetPos, time);
```

**Sistemas de IA/Pathing:**
```csharp
// Garantir que unidade fique dentro do mapa
unitPos = CameraMath.ClampToBounds(unitPos, mapCenter, mapSize);

// Calcular bounds do mundo para path planning
if (CameraMath.CalculateTerrainBounds(Terrain.activeTerrains, out var center, out var size)) {
    pathfinder.SetBounds(center, size);
}
```

**Sistemas de UI:**
```csharp
// Detectar se mouse está na borda (para tooltips customizados)
Vector2 edgeDir = CameraMath.GetEdgePanDirection(mousePos, 20, Screen.width, Screen.height);
if (edgeDir != Vector2.zero) {
    ShowEdgeIndicator(edgeDir);
}
```

---

### 4.3 RTSCameraCinemachineV3Controller (MonoBehaviour)

#### Campos Públicos (Inspector):

```csharp
[Header("Profile & References")]
public RTSCameraProfile profile;        // ScriptableObject de configuração
public CinemachineCamera vcam;          // Câmera virtual (auto-criada se null)
public Transform pivot;                 // Pivot para tilt (auto-criado se null)

[Header("Bounds (Mundo)")]
public Vector2 boundsCenter = Vector2.zero;             // Centro do mundo (x,z)
public Vector2 boundsSize = new Vector2(200, 200);      // Tamanho do mundo (x,z)

[Header("Initial Setup")]
public Vector3 initialPosition = Vector3.zero;          // Posição inicial do rig
public float initialHeading = 0f;                       // Rotação Y inicial (graus)
[Range(0, 1)] public float initialZoom = 0.5f;          // Zoom inicial (0=perto, 1=longe)
```

#### Propriedades Públicas:

```csharp
public float Zoom { get; }  // Valor atual de zoom (0..1), read-only
```

#### Métodos Públicos:

**Entrada de Input:**
```csharp
/// <summary>
/// Processa entrada por frame. Chamado pelo RTSCameraInputSystem.
/// </summary>
public void TickInput(Vector2 wasdMove, Vector2 pointerPosition, bool isMiddleDragging, 
                      Vector2 pointerDelta, float rotateAxis, float zoomAxis, bool pointerOverUI)
```

**Efeitos Visuais:**
```csharp
/// <summary>
/// Dispara shake temporário (também chamável via eventos).
/// </summary>
public void PlayShake(float amplitude = 1.2f, float frequency = 2.0f, float duration = 0.25f)
```

**Cutscenes:**
```csharp
/// <summary>
/// Inicia cutscene apontando para um alvo.
/// </summary>
public void StartCutscene(Transform target, float fov = 50f, int priority = 100)

/// <summary>
/// Finaliza cutscene atual.
/// </summary>
public void EndCutscene()
```

**Navegação:**
```csharp
/// <summary>
/// Move o rig para uma posição XZ (mini-mapa).
/// </summary>
public void GoToXZ(Vector2 worldXZ, bool snap = false, float duration = 0.4f)

/// <summary>
/// Move o rig para uma posição 3D (mantém altura Y do rig).
/// </summary>
public void GoTo(Vector3 worldPos, bool snap = false, float duration = 0.4f)
```

**Utilitários:**
```csharp
/// <summary>
/// Calcula boundsCenter/Size encapsulando os Terrains ativos.
/// Menu de contexto: clique direito no componente > Set Bounds From Terrain(s)
/// </summary>
[ContextMenu("Set Bounds From Terrain(s)")]
public void SetBoundsFromTerrains()
```

#### Eventos Escutados:

```csharp
void OnEnable() {
    // Shake
    GameEvents.OnCameraShake += HandleCameraShake;
    
    // Foco
    GameEvents.OnCameraFocus += HandleCameraFocus;
    GameEvents.OnCameraFocusXZ += HandleCameraFocusXZ;
    
    // Cutscene
    GameEvents.OnCutsceneStart += HandleCutsceneStart;
    GameEvents.OnCutsceneEnd += HandleCutsceneEnd;
    
    // Minimap/Seleção
    GameEvents.OnMinimapPing += HandleMinimapPing;
    GameEvents.OnSelectionFocus += HandleSelectionFocus;
}
```

#### Exemplos de Uso:

**Setup Básico:**
```csharp
// 1. Criar GameObject vazio para o rig
GameObject camRig = new GameObject("CameraRig");

// 2. Adicionar controller
var controller = camRig.AddComponent<RTSCameraCinemachineV3Controller>();
var inputSystem = camRig.AddComponent<RTSCameraInputSystem>();

// 3. Atribuir profile
controller.profile = Resources.Load<RTSCameraProfile>("DefaultCameraProfile");

// 4. Configurar bounds (manual ou via terreno)
controller.SetBoundsFromTerrains(); // Se houver terreno na cena
```

**Uso por Eventos (Recomendado):**
```csharp
// Sistema de Combate
void OnExplosion(Vector3 pos) {
    GameEvents.RaiseCameraShake(amplitude: 2f, frequency: 3f, duration: 0.3f);
}

// UI de Mini-mapa
void OnMinimapClick(Vector2 worldXZ) {
    GameEvents.RaiseMinimapPing(worldXZ);
}

// Sistema de Seleção
void OnUnitSelected(Unit unit) {
    GameEvents.RaiseSelectionFocus(unit.transform);
}

// Sistema de Missões
void StartDialogue(NPC npc) {
    GameEvents.RaiseCutsceneStart(npc.transform, fov: 45f, priority: 100);
}

void EndDialogue() {
    GameEvents.RaiseCutsceneEnd();
}
```

**Uso Direto (Se Necessário):**
```csharp
// Referência direta ao controller (menos recomendado)
RTSCameraCinemachineV3Controller cam = 
    FindObjectOfType<RTSCameraCinemachineV3Controller>();

// Shake direto
cam.PlayShake(1.5f, 3f, 0.2f);

// Jump to direto
cam.GoToXZ(new Vector2(100f, 200f), snap: false, duration: 0.5f);
```

---

### 4.4 RTSCameraInputSystem (MonoBehaviour)

#### Campos (Inspector):

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

#### Ciclo de Vida:

```csharp
void Awake() {
    // Obtém referência ao controller
    _cam = GetComponent<RTSCameraCinemachineV3Controller>();
}

void OnEnable() {
    // Habilita todas as ações
    // Conecta callbacks do botão do meio
}

void OnDisable() {
    // Desabilita todas as ações
    // Desconecta callbacks
}

void Update() {
    // Lê valores das ações
    // Detecta hover de UI
    // Invoca _cam.TickInput(...)
}
```

#### Exemplo de Configuração:

**1. Criar Input Actions Asset:**
```
Assets > Create > Input Actions
```

**2. Configurar Actions:**
```
Action Map: Camera
- Move (Vector2, WASD)
- Rotate (Value, Q/E keys)
- Zoom (Value, Mouse Scroll Y)
- Pointer Position (Vector2, Mouse Position)
- Pointer Delta (Vector2, Mouse Delta)
- Middle Button (Button, Mouse Middle)
```

**3. Atribuir no Inspector:**
- Arraste cada action para o campo correspondente no RTSCameraInputSystem

---

## 5) INTEGRAÇÃO COM GameEvents

### Eventos de Câmera Disponíveis:

```csharp
// Shake (disparado por sistemas de combate, impactos, etc.)
Action<float, float, float> OnCameraShake; // amplitude, frequency, duration

// Foco em posição 3D (objetivos de missão, notificações)
Action<Vector3, bool, float> OnCameraFocus; // worldPos, snap, duration

// Foco em posição XZ (mini-mapa, pings táticos)
Action<Vector2, bool, float> OnCameraFocusXZ; // worldXZ, snap, duration

// Cutscene (diálogos, cinemáticas)
Action<Transform, float, int> OnCutsceneStart; // target, fov, priority
Action OnCutsceneEnd;

// Minimap/Seleção (UI, comandos)
Action<Vector2> OnMinimapPing; // worldXZ
Action<Transform> OnSelectionFocus; // target
```

### Como Disparar de Outros Sistemas:

**Sistema de Combate:**
```csharp
public class ExplosionEffect : MonoBehaviour {
    void Explode() {
        // Lógica da explosão...
        
        // Disparar shake
        GameEvents.RaiseCameraShake(
            amplitude: 1.8f,
            frequency: 3.5f,
            duration: 0.4f
        );
    }
}
```

**UI de Mini-mapa:**
```csharp
public class MinimapUI : MonoBehaviour {
    void OnMinimapClicked(Vector2 screenPos) {
        // Converter para posição do mundo
        Vector2 worldXZ = ScreenToWorldXZ(screenPos);
        
        // Disparar ping
        GameEvents.RaiseMinimapPing(worldXZ);
    }
}
```

**Sistema de Seleção:**
```csharp
public class SelectionManager : MonoBehaviour {
    void SelectUnit(Unit unit) {
        // Lógica de seleção...
        
        // Foco opcional na unidade
        if (shouldFocusOnSelection) {
            GameEvents.RaiseSelectionFocus(unit.transform);
        }
    }
}
```

**Sistema de Missões/Diálogo:**
```csharp
public class DialogueSystem : MonoBehaviour {
    void StartConversation(NPC npc) {
        // Iniciar cutscene
        GameEvents.RaiseCutsceneStart(
            target: npc.transform,
            fov: 40f,
            priority: 100
        );
        
        // Mostrar diálogo...
    }
    
    void EndConversation() {
        // Finalizar cutscene
        GameEvents.RaiseCutsceneEnd();
        
        // Fechar UI de diálogo...
    }
}
```

---

## 6) CONFIGURAÇÕES E VARIÁVEIS-CHAVE

### Velocidades Base (RTSCameraProfile):

- **panSpeedNear / panSpeedFar:** Controlam responsividade do pan em diferentes níveis de zoom
- **rotateSpeed:** Velocidade de rotação (impacta UX e sensação de controle)
- **zoomSpeed:** Velocidade de zoom (valores muito altos podem causar desorientação)

### Faixas de Zoom (RTSCameraProfile):

- **minHeight / maxHeight:** Definem envelope de câmera (clareza de combate vs. visão tática)
- **minTilt / maxTilt:** Controlam ângulo de visão (mais inclinado = mais estratégico)

### Bounds (RTSCameraCinemachineV3Controller):

- **boundsCenter / boundsSize:** Restringem navegação do rig ao mundo jogável
- Utilitário **SetBoundsFromTerrains()** calcula automaticamente

### Flags de Interação (RTSCameraProfile):

- **enableWASD / enableEdgePan / enableMiddleDrag / enableRotate:** Parametrizam UX
- **pauseWhenPointerOverUI:** Evita movimentos acidentais durante interação com UI

### Curvas de Suavização (RTSCameraProfile):

- **panSpeedCurve:** Ajusta velocidade de pan por nível de zoom (permite aceleração não-linear)
- **zoomCurve:** Suaviza transições de zoom (evita "pulos" bruscos)

---

## 7) EXEMPLOS COMPOSTOS (END-TO-END)

### 7.1 Setup Completo da Câmera

```csharp
// 1. Criar Profile
// Unity Editor: Assets > Create > Game > Camera Profile
// Nomeie como "DefaultCameraProfile"

// 2. Configurar Profile no Inspector:
// - panSpeedNear: 20
// - panSpeedFar: 35
// - edgeThickness: 12
// - zoomSpeed: 0.15
// - minHeight: 10, maxHeight: 60
// - minTilt: 35, maxTilt: 75
// - rotateSpeed: 90
// - Todas as flags: true

// 3. Criar GameObject do Rig na cena
GameObject camRig = new GameObject("CameraRig");
var controller = camRig.AddComponent<RTSCameraCinemachineV3Controller>();
var inputSys = camRig.AddComponent<RTSCameraInputSystem>();

// 4. Atribuir Profile
controller.profile = Resources.Load<RTSCameraProfile>("DefaultCameraProfile");

// 5. Configurar Input Actions (no Inspector do inputSys)
// Arrastar cada ActionReference do .inputactions

// 6. Configurar bounds
controller.initialPosition = new Vector3(0, 0, 0);
controller.initialHeading = 0f;
controller.initialZoom = 0.5f;
controller.SetBoundsFromTerrains(); // Se houver terreno

// 7. Remover Main Camera padrão
Destroy(Camera.main.gameObject);
```

---

### 7.2 Sistema de Combate com Shake

```csharp
public class CombatSystem : MonoBehaviour {
    void OnUnitAttack(Unit attacker, Unit target) {
        // Executar ataque...
        
        // Shake leve para ataques normais
        if (attacker.GetWeaponType() == WeaponType.Melee) {
            GameEvents.RaiseCameraShake(0.8f, 2f, 0.2f);
        }
        // Shake intenso para ataques especiais
        else if (attacker.GetWeaponType() == WeaponType.Siege) {
            GameEvents.RaiseCameraShake(2.5f, 4f, 0.5f);
        }
    }
    
    void OnBuildingDestroyed(Building building) {
        // Efeitos visuais...
        
        // Shake épico
        GameEvents.RaiseCameraShake(
            amplitude: 3f,
            frequency: 5f,
            duration: 0.8f
        );
    }
}
```

---

### 7.3 Mini-mapa Interativo

```csharp
public class MinimapController : MonoBehaviour {
    [SerializeField] RectTransform minimapRect;
    [SerializeField] Vector2 worldSize = new Vector2(500f, 500f);
    
    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            if (RectTransformUtility.RectangleContainsScreenPoint(minimapRect, Input.mousePosition)) {
                OnMinimapClick(Input.mousePosition);
            }
        }
    }
    
    void OnMinimapClick(Vector2 screenPos) {
        // Converter posição da tela do mini-mapa para mundo
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimapRect, screenPos, null, out Vector2 localPos
        );
        
        // Normalizar (-0.5 a 0.5)
        Vector2 normalized = new Vector2(
            localPos.x / minimapRect.rect.width,
            localPos.y / minimapRect.rect.height
        );
        
        // Converter para posição do mundo
        Vector2 worldXZ = new Vector2(
            normalized.x * worldSize.x,
            normalized.y * worldSize.y
        );
        
        // Disparar evento
        GameEvents.RaiseMinimapPing(worldXZ);
        
        // Feedback visual (opcional)
        ShowPingEffect(worldXZ);
    }
}
```

---

### 7.4 Sistema de Diálogo com Cutscenes

```csharp
public class DialogueManager : MonoBehaviour {
    private Transform currentSpeaker;
    
    public void StartDialogue(NPC npc, Dialogue dialogue) {
        currentSpeaker = npc.transform;
        
        // Iniciar cutscene focando no NPC
        GameEvents.RaiseCutsceneStart(
            target: currentSpeaker,
            fov: 45f,
            priority: 100
        );
        
        // Pausar jogo
        Time.timeScale = 0f;
        
        // Mostrar UI de diálogo
        ShowDialogueUI(dialogue);
    }
    
    public void EndDialogue() {
        // Finalizar cutscene
        GameEvents.RaiseCutsceneEnd();
        
        // Despausar jogo
        Time.timeScale = 1f;
        
        // Fechar UI
        HideDialogueUI();
        
        currentSpeaker = null;
    }
}
```

---

### 7.5 Sistema de Seleção com Foco Automático

```csharp
public class UnitSelectionManager : MonoBehaviour {
    [SerializeField] bool autoFocusOnSelection = true;
    [SerializeField] float focusDuration = 0.3f;
    
    private List<Unit> selectedUnits = new List<Unit>();
    
    public void SelectUnit(Unit unit) {
        // Limpar seleção anterior
        ClearSelection();
        
        // Adicionar à seleção
        selectedUnits.Add(unit);
        unit.SetSelected(true);
        
        // Foco automático (opcional)
        if (autoFocusOnSelection) {
            GameEvents.RaiseSelectionFocus(unit.transform);
        }
    }
    
    public void SelectUnits(List<Unit> units) {
        // Limpar seleção anterior
        ClearSelection();
        
        // Adicionar à seleção
        selectedUnits.AddRange(units);
        foreach (var unit in units) {
            unit.SetSelected(true);
        }
        
        // Foco no centro do grupo
        if (autoFocusOnSelection && units.Count > 0) {
            Vector3 center = CalculateGroupCenter(units);
            GameEvents.RaiseCameraFocus(center, snap: false, focusDuration);
        }
    }
    
    Vector3 CalculateGroupCenter(List<Unit> units) {
        Vector3 sum = Vector3.zero;
        foreach (var unit in units) {
            sum += unit.transform.position;
        }
        return sum / units.Count;
    }
}
```

---

## 8) NOTAS DE IMPLEMENTAÇÃO & BOAS PRÁTICAS

### Ordem de Inicialização:

- O `RTSCameraCinemachineV3Controller` valida o profile no `Awake()`
- Se o profile for null, loga erro claro e desabilita funcionalidade
- `EnsureSetup()` cria componentes necessários automaticamente (pivot, vcam, componentes CM3)

### ScriptableObjects (Profiles):

- Criar pelo menos um profile "Default" no projeto
- Profiles podem ser compartilhados entre cenas/modos de jogo
- Perfis especializados: "Cinematic" (lento, suave), "Spectator" (rápido, amplo)

### Eventos:

- Sempre usar `GameEvents` para comunicação entre sistemas
- Controllers nunca devem ser referenciados diretamente por UI/Gameplay
- Handlers de eventos devem ser leves e resilientes (usar try-catch se necessário)

### Testabilidade:

- `CameraMath` é pura → testes unitários triviais
- Profiles podem ser trocados em runtime para testes
- Eventos facilitam mocks (testar UI sem câmera real)

### Performance:

- Edge-pan usa pixels da tela (custo mínimo)
- Cálculos matemáticos delegados ao `CameraMath` (otimizado)
- Bounds clamp é O(1)
- Coroutines para shake/GoTo não acumulam (canceladas ao iniciar nova)

### Reutilização:

- `CameraMath` pode ser usado por IA, pathing, UI
- Profiles reutilizáveis entre projetos
- Eventos de câmera servem para qualquer sistema (não só UI)

---

## 9) ARQUIVOS DO PROJETO

### Arquivos Necessários:

**Scripts:**
- `RTSCameraProfile.cs` (novo)
- `CameraMath.cs` (novo)
- `RTSCameraCinemachineV3Controller.cs` (refatorado)
- `RTSCameraInputSystem.cs` (refatorado)
- `GameEvents.cs` (atualizado com eventos de câmera)

**Assets:**
- `DefaultCameraProfile.asset` (ScriptableObject)
- Input Actions Asset (`.inputactions`)

**Dependências:**
- Cinemachine 3 (Package Manager)
- Unity Input System (Package Manager)

---

## 10) CHECKLIST DE SETUP

### Setup Inicial:
- [ ] Instalar Cinemachine 3 via Package Manager
- [ ] Instalar Input System via Package Manager
- [ ] Criar scripts na pasta Scripts/Camera/
- [ ] Criar Input Actions Asset
- [ ] Configurar actions: Move, Rotate, Zoom, PointerPos, PointerDelta, MiddleButton

### Criar Profile:
- [ ] Assets > Create > Game > Camera Profile
- [ ] Nomear como "DefaultCameraProfile"
- [ ] Configurar velocidades, limites e flags
- [ ] Ajustar curvas de suavização (opcional)

### Setup na Cena:
- [ ] Criar GameObject "CameraRig"
- [ ] Adicionar RTSCameraCinemachineV3Controller
- [ ] Adicionar RTSCameraInputSystem
- [ ] Atribuir Profile ao controller
- [ ] Conectar ActionReferences no inputSystem
- [ ] Configurar posição/heading/zoom iniciais
- [ ] Executar "Set Bounds From Terrain(s)" (menu de contexto)
- [ ] Remover Main Camera padrão
- [ ] Testar em Play Mode

### Integração com Outros Sistemas:
- [ ] Adicionar eventos de shake em sistema de combate
- [ ] Integrar mini-mapa com GameEvents.RaiseMinimapPing
- [ ] Conectar seleção de unidades com GameEvents.RaiseSelectionFocus
- [ ] Implementar cutscenes em sistema de diálogo/missões

---

## 11) TROUBLESHOOTING (PROBLEMAS COMUNS)

### Problema: "Profile não atribuído" (erro no console)

**Solução:**
- Criar um RTSCameraProfile via Assets > Create > Game > Camera Profile
- Arrastar para o campo "Profile" do controller no Inspector

---

### Problema: Câmera não se move

**Possíveis causas:**
1. Input Actions não conectadas → Verificar ActionReferences no Inspector
2. Actions não habilitadas → Verificar se OnEnable() está sendo chamado
3. Profile null → Ver solução acima
4. Zoom fora dos limites → Ajustar minHeight/maxHeight no profile

---

### Problema: Edge-pan não funciona sobre UI

**Comportamento esperado:**
- `pauseWhenPointerOverUI = true` desabilita edge-pan quando mouse está sobre UI
- Se quiser permitir, definir flag como false no profile

---

### Problema: Rotação não funciona

**Possíveis causas:**
1. `enableRotate = false` no profile
2. Action "Rotate" não conectada
3. Teclas Q/E mapeadas para outra ação

---

### Problema: GoTo não respeita bounds

**Não é um bug:**
- GoTo e GoToXZ clampeiam automaticamente usando `CameraMath.ClampToBounds`
- Verificar se boundsCenter e boundsSize estão configurados corretamente

---

### Problema: Shake não tem efeito

**Possíveis causas:**
1. Duration muito curta (< 0.1s)
2. Amplitude muito baixa (< 0.5)
3. Componente CinemachineBasicMultiChannelPerlin não criado → EnsureSetup() deve resolver

---

### Problema: Cutscene não termina

**Solução:**
- Sempre chamar `GameEvents.RaiseCutsceneEnd()` ou `controller.EndCutscene()`
- Verificar se priority da cutscene camera foi resetada para 0

---

## 12) MELHORIAS FUTURAS (ROADMAP)

### Curto Prazo:
- [ ] Adicionar suporte a múltiplos perfis em runtime (troca dinâmica)
- [ ] Implementar interpolação entre perfis (transições suaves)
- [ ] Adicionar limites de rotação (min/max angle)
- [ ] Suporte a zoom discreto (níveis fixos)

### Médio Prazo:
- [ ] WorldBoundsService centralizado (múltiplas cenas)
- [ ] Sistema de Pause global integrado
- [ ] Foco por facção (cores de gizmos, FOV personalizado)
- [ ] Pool de câmeras de cutscene (reuso)

### Longo Prazo:
- [ ] Abstrair Screen.width/height para IViewportInfo (splitscreen)
- [ ] Separar Rig/Orbit em componentes independentes
- [ ] Editor customizado para RTSCameraProfile
- [ ] Sistema de replay com controle de câmera

---

## 13) GLOSSÁRIO

### Termos Técnicos:

- **Rig:** GameObject pai que controla a posição/rotação base da câmera
- **Pivot:** Transform filho usado para aplicar tilt (inclinação)
- **Tilt:** Ângulo de inclinação vertical da câmera (pitch)
- **Heading:** Ângulo de rotação horizontal (yaw)
- **Zoom:** Nível de afastamento (0=perto, 1=longe), controlado por altura + tilt
- **Pan:** Movimento lateral/frontal da câmera (WASD, edge, drag)
- **Edge-pan:** Movimento automático ao posicionar mouse na borda da tela
- **Middle-drag:** Arrasto da câmera com botão do meio do mouse
- **Bounds:** Limites retangulares (XZ) que restringem movimento do rig
- **Shake:** Efeito de vibração usando noise (Perlin)
- **Cutscene:** Câmera temporária focada em um alvo específico
- **Profile:** ScriptableObject com configurações de câmera
- **Follow Offset:** Vetor que define distância/ângulo entre câmera e pivot

### Componentes Cinemachine:

- **CinemachineCamera:** Câmera virtual (não renderiza, controla Main Camera)
- **CinemachineFollow:** Componente que faz câmera seguir um target
- **CinemachineRotationComposer:** Componente de aim/look-at
- **CinemachineBasicMultiChannelPerlin:** Gerador de noise para shakes

---

## 14) REFERÊNCIAS E RECURSOS

### Documentação Oficial:

- **Cinemachine 3:** https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html
- **Input System:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html

### Tutoriais Relacionados:

- Cinemachine Virtual Cameras
- Input System Action Assets
- ScriptableObject Workflow

### Assets Relacionados:

- Mini-mapa System (integração com OnMinimapPing)
- Unit Selection System (integração com OnSelectionFocus)
- Combat System (integração com OnCameraShake)

---

## 15) CÓDIGO COMPLETO RESUMIDO

### Estrutura de Arquivos:

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Enums.cs
│   │   ├── GameConfig.cs
│   │   ├── GameEvents.cs (atualizado)
│   │   ├── GameContext.cs
│   │   ├── TimeManager.cs
│   │   └── ...
│   ├── Camera/
│   │   ├── RTSCameraProfile.cs (novo)
│   │   ├── CameraMath.cs (novo)
│   │   ├── RTSCameraCinemachineV3Controller.cs (refatorado)
│   │   └── RTSCameraInputSystem.cs (refatorado)
│   └── ...
├── Settings/
│   ├── DefaultCameraProfile.asset
│   ├── CinematicCameraProfile.asset
│   └── ...
└── Input/
    └── RTSInputActions.inputactions
```

### Hierarquia da Cena:

```
Scene
├── _GameContext
│   ├── GameConfig (reference)
│   ├── FactionDatabase (reference)
│   ├── FactionService
│   └── TimeManager
├── CameraRig
│   ├── RTSCameraCinemachineV3Controller
│   │   └── Profile: DefaultCameraProfile
│   ├── RTSCameraInputSystem
│   │   └── ActionReferences...
│   └── Pivot (auto-criado)
│       └── RTS_Camera (CM3) (auto-criado)
├── Terrain
├── Units
└── UI
```

---

## APÊNDICE A — EVENTOS GLOBAIS (REFERÊNCIA RÁPIDA)

### GameEvents.cs — Lista Completa de Eventos:

```csharp
// TEMPO
Action<float> OnTimeOfDay01
Action<int> OnDayChanged
Action<int, int, int> OnClockChanged

// ECONOMIA
Action<FactionId, ResourceType, int> OnResourceGathered

// DIPLOMACIA
Action OnReputationMatrixReady
Action<FactionId, FactionId, float> OnReputationChanged

// CÂMERA
Action<float, float, float> OnCameraShake
Action<Vector3, bool, float> OnCameraFocus
Action<Vector2, bool, float> OnCameraFocusXZ
Action<Transform, float, int> OnCutsceneStart
Action OnCutsceneEnd

// SELEÇÃO/MINIMAP
Action<Vector2> OnMinimapPing
Action<Transform> OnSelectionFocus
```

### Métodos Raise Correspondentes:

```csharp
// TEMPO
RaiseTimeOfDay(float t01)
RaiseDayChanged(int day)
RaiseClockChanged(int day, int hour, int minute)

// ECONOMIA
RaiseResourceGathered(FactionId who, ResourceType type, int amount)

// DIPLOMACIA
RaiseReputationMatrixReady()
RaiseReputationChanged(FactionId a, FactionId b, float value)

// CÂMERA
RaiseCameraShake(float amplitude, float frequency, float duration)
RaiseCameraFocus(Vector3 worldPos, bool snap, float duration)
RaiseCameraFocusXZ(Vector2 worldXZ, bool snap, float duration)
RaiseCutsceneStart(Transform target, float fov, int priority)
RaiseCutsceneEnd()

// SELEÇÃO/MINIMAP
RaiseMinimapPing(Vector2 worldXZ)
RaiseSelectionFocus(Transform target)
```

---

## APÊNDICE B — SNIPPET DE CÓDIGO RÁPIDO

### Setup Rápido de Câmera (Runtime):

```csharp
public class QuickCameraSetup : MonoBehaviour {
    void Start() {
        // Criar rig
        GameObject rig = new GameObject("CameraRig");
        
        // Adicionar componentes
        var controller = rig.AddComponent<RTSCameraCinemachineV3Controller>();
        var input = rig.AddComponent<RTSCameraInputSystem>();
        
        // Carregar profile
        controller.profile = Resources.Load<RTSCameraProfile>("DefaultCameraProfile");
        
        // Configurar
        controller.initialPosition = Vector3.zero;
        controller.initialHeading = 0f;
        controller.initialZoom = 0.5f;
        
        // Calcular bounds automaticamente
        controller.SetBoundsFromTerrains();
        
        // Remover Main Camera
        if (Camera.main != null) {
            Destroy(Camera.main.gameObject);
        }
        
        Debug.Log("Câmera configurada!");
    }
}
```

### Template de Sistema com Eventos:

```csharp
public class MyGameSystem : MonoBehaviour {
    void OnEnable() {
        // Subscrever eventos relevantes
        GameEvents.OnTimeOfDay01 += HandleTimeChange;
        GameEvents.OnResourceGathered += HandleResourceGather;
    }
    
    void OnDisable() {
        // SEMPRE desinscrever!
        GameEvents.OnTimeOfDay01 -= HandleTimeChange;
        GameEvents.OnResourceGathered -= HandleResourceGather;
    }
    
    void HandleTimeChange(float t01) {
        // Reagir ao ciclo dia/noite
    }
    
    void HandleResourceGather(FactionId who, ResourceType type, int amount) {
        // Reagir a coleta de recursos
    }
    
    void TriggerExplosion(Vector3 pos) {
        // Disparar shake de câmera
        GameEvents.RaiseCameraShake(2f, 3f, 0.5f);
    }
}

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
**Versão do Documento:** 2.0


