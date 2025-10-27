---
layout: default
title: LOTE 6 - MOVIMENTAÇÃO E FORMAÇÕES DE UNIDADES
permalink: /lote-6/
---
# LOTE 6 - MOVIMENTAÇÃO E FORMAÇÕES DE UNIDADES

## Visão Geral

O **Lote 6** implementa um sistema completo de **movimentação inteligente** e **formações táticas** para unidades em Medieval Thrones. Este sistema utiliza o **A* Pathfinding Project** para navegação e introduz um **gerenciador de grupos de formação** que permite às unidades se moverem coordenadamente em diferentes configurações táticas.

### Objetivos do Lote

- ✅ Implementar movimentação baseada em pathfinding com A* Pathfinding Project
- ✅ Sistema de 6 formações táticas (Linha, Coluna, Triangular, Cunha, Circular, Quadrada)
- ✅ Gerenciamento automático de grupos de formação
- ✅ Detecção inteligente de gargalos e passagens estreitas
- ✅ Sistema de priorização de unidades nas formações
- ✅ Integração com Input System para controles de formação
- ✅ Visualização em tempo real de formações

---

## Arquitetura do Sistema

### Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────┐
│                     INPUT SYSTEM                            │
│  InputSelection → GameEvents.OnMoveCommand                  │
│  Keyboard (F1-F6, H) → Mudança de Formação                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              MovementCommandHandler                         │
│  • Recebe comandos de movimento                             │
│  • Detecta grupos na seleção                                │
│  • Determina formação adequada                              │
│  • Calcula posições de formação                             │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         ▼                       ▼
┌──────────────────┐    ┌─────────────────────┐
│ FormationGroup   │    │ FormationCalculator │
│ Manager          │    │ • 6 tipos de        │
│ • Cria grupos    │    │   formação          │
│ • Mescla grupos  │    │ • Priorização       │
│ • Destrói grupos │    │ • Centro inteligente│
└────────┬─────────┘    └─────────┬───────────┘
         │                        │
         └────────────┬───────────┘
                      ▼
         ┌─────────────────────────┐
         │   FormationGroup        │
         │   (Grupo Individual)    │
         │   • ID único            │
         │   • Tipo de formação    │
         │   • Lista de unidades   │
         └────────────┬────────────┘
                      │
                      ▼
         ┌─────────────────────────┐
         │   UnitMovement          │
         │   (Por Unidade)         │
         │   • RichAI (A*)         │
         │   • Offset de formação  │
         │   • Estado de movimento │
         └─────────────────────────┘
```

---

## Componentes Principais

### 1. MovementCommandHandler

**Localização:** `Scripts/Core/MovementCommandHandler.cs`

**Responsabilidades:**
- Gerenciar comandos de movimentação de unidades selecionadas
- Integrar com o sistema de grupos de formação
- Calcular posições de formação usando `FormationCalculator`
- Detectar gargalos e passagens estreitas

**Fluxo de Comando de Movimento:**

```csharp
// Recebe comando via evento
GameEvents.OnMoveCommand += OnMoveCommandReceived;

void OnMoveCommandReceived(Vector3 worldPosition)
{
    // 1. Detectar grupos na seleção
    var groupsInSelection = FormationGroupManager.Instance
        .GetGroupsInSelection(selectedUnits, out unitsWithoutGroup);
    
    // 2. Determinar formação (maior grupo vence)
    FormationType formation = DetermineFormation(groupsInSelection);
    
    // 3. Calcular posições de formação
    var formationOffsets = FormationCalculator
        .CalculateFormation(units, formation, spacing);
    
    // 4. Criar/atualizar grupo
    FormationGroup group = FormationGroupManager.Instance
        .CreateGroup(units, formation);
    
    // 5. Mover cada unidade
    foreach (var kvp in formationOffsets)
    {
        UnitMovement movement = kvp.Key.GetComponent<UnitMovement>();
        movement.MoveToWithOffset(worldPosition, kvp.Value);
    }
}
```

**Características Especiais:**

- **Detecção de Gargalos:** Sistema usa raycasts para detectar passagens estreitas e força formação em coluna
- **Centro Inteligente:** Ignora unidades outliers ao calcular centro do grupo
- **Formação Adaptativa:** Ajusta automaticamente baseado no número de unidades

---

### 2. FormationGroupManager

**Localização:** `Scripts/Core/FormationGroupManager.cs`

**Padrão:** Singleton

**Responsabilidades:**
- Criar e destruir grupos de formação
- Manter mapeamento Unit → FormationGroup
- Mesclar grupos automaticamente
- Limpar grupos vazios

**Regras de Grupos:**

| Situação | Comportamento |
|----------|--------------|
| Movimento de unidades soltas | Cria novo grupo com formação escolhida |
| Movimento de grupo inteiro | Mantém grupo existente |
| Movimento de subconjunto de grupo | Cria novo grupo (subdivisão) |
| Seleção com múltiplos grupos | Grupo maior prevalece, outros são mesclados |
| Formação None | Unidades ficam soltas (sem grupo) |
| Unidade morre | Automaticamente removida do grupo |
| Grupo fica vazio | Destruído automaticamente |

**API Principal:**

```csharp
// Criar grupo
FormationGroup CreateGroup(IEnumerable<Unit> units, FormationType formation);

// Consultar grupo de uma unidade
FormationGroup GetGroupForUnit(Unit unit);

// Determinar formação para seleção (regra de mescla)
FormationType DetermineFormationForSelection(IEnumerable<Unit> selectedUnits);

// Mesclar múltiplos grupos
FormationGroup MergeGroups(IEnumerable<FormationGroup> groups);

// Remover unidade de qualquer grupo
void RemoveUnitFromAnyGroup(Unit unit);
```

---

### 3. FormationCalculator

**Localização:** `Scripts/Core/FormationCalculator.cs`

**Tipo:** Classe estática (utilitário)

**Responsabilidades:**
- Calcular posições (offsets) para cada tipo de formação
- Ordenar unidades por prioridade
- Implementar lógica matemática de cada formação

**Formações Implementadas:**

#### 3.1. Linha (Line)
- **Uso:** Ofensivo, avanço coordenado
- **Estrutura:** 10 unidades por linha, 5 de cada lado com passagem central
- **Prioridade:** Maior prioridade nas primeiras linhas

```
        [FRENTE]
    x x x x x | x x x x x     ← Linha 1 (Guerreiros/Heróis)
    x x x x x | x x x x x     ← Linha 2 (Lanceiros)
    x x x x x | x x x x x     ← Linha 3 (Arqueiros)
        [TRÁS]
```

#### 3.2. Coluna (Column)
- **Uso:** Travessia de passagens estreitas
- **Estrutura:** Múltiplas colunas profundas com passagem central
- **Prioridade:** Frente de cada coluna

```
    x x | x x     ← Profundidade variável
    x x | x x     ← Ajusta automaticamente
    x x | x x     ← baseado no número de unidades
    x x | x x
```

#### 3.3. Triangular
- **Uso:** Romper linhas inimigas, impacto concentrado
- **Estrutura:** Triângulos inscritos, sem passagem central
- **Prioridade:** Ponta do triângulo (frente)

```
        x           ← Linha 1 (Herói)
       x x          ← Linha 2
      x x x         ← Linha 3
     x x x x        ← Linha 4
    x x x x x       ← Linha 5
```

#### 3.4. Cunha (Wedge)
- **Uso:** Penetração rápida, perseguição
- **Estrutura:** Versão alongada do triângulo
- **Prioridade:** Ponta (apenas 1 unidade na frente)

```
        x           ← 1 unidade
       x x x        ← 3 unidades
     x x x x x      ← 5 unidades
   x x x x x x x    ← 7 unidades
```

#### 3.5. Circular
- **Uso:** Defesa 360°, proteção de unidades frágeis
- **Estrutura:** Círculos concêntricos
- **Prioridade:** Anel externo (unidades fortes defendem centro)

```
       x x x
     x       x      ← Anel externo (Guerreiros)
    x    x    x     ← Centro (Arqueiros/Workers)
     x       x
       x x x
```

#### 3.6. Quadrada (Square)
- **Uso:** Máxima densidade, resistência
- **Estrutura:** Quadrado denso, sem passagem
- **Prioridade:** Primeira linha

```
    x x x x x
    x x x x x
    x x x x x
    x x x x x
    x x x x x
```

**Sistema de Priorização:**

```csharp
public enum FormationPriority
{
    VeryLow = 0,    // Workers (trás)
    Low = 1,        // Siege (Ram, Catapult)
    Medium = 2,     // Cavalry, Archers
    High = 3,       // Spearman
    VeryHigh = 4,   // Warrior
    Maximum = 5     // Hero (frente)
}
```

---

### 4. UnitMovement

**Localização:** `Scripts/Core/UnitMovement.cs`

**Responsabilidades:**
- Controlar movimento individual de cada unidade
- Integrar com RichAI (A* Pathfinding)
- Gerenciar offset de formação
- Detectar chegada ao destino

**Propriedades Principais:**

| Propriedade | Tipo | Descrição |
|------------|------|-----------|
| `Position` | Vector3 | Posição atual no mundo |
| `IsMoving` | bool | Está se movendo? |
| `IsIdle` | bool | Está parado? |
| `TargetPosition` | Vector3 | Destino final (sem offset) |
| `FormationOffset` | Vector3 | Offset de formação atribuído |
| `FinalDestination` | Vector3 | Destino real (target + offset) |

**Métodos Principais:**

```csharp
// Mover para posição simples
void MoveTo(Vector3 destination);

// Mover com offset de formação
void MoveToWithOffset(Vector3 destination, Vector3 offset);

// Parar movimento
void Stop();

// Atualizar apenas offset (mantém destino)
void UpdateFormationOffset(Vector3 newOffset);
```

**Integração com RichAI:**

```csharp
void Awake()
{
    richAI = GetComponent<RichAI>();
    
    // Configurar RichAI
    richAI.maxSpeed = unit.MaxSpeed;
    richAI.acceleration = unit.def.acceleration;
    richAI.rotationSpeed = unit.def.rotationSpeed;
    richAI.endReachedDistance = arrivalDistance;
}

void Update()
{
    // Verificar se chegou ao destino
    if (isMoving && richAI.reachedDestination)
    {
        OnReachedDestination();
    }
}
```

---

## Integração com A* Pathfinding Project

### Instalação e Configuração

O **A* Pathfinding Project** (versão 5.2.3) foi integrado ao Medieval Thrones para cálculo de rotas inteligentes.

**Documentação Oficial:** https://www.arongranberg.com/astar/docs

### Componentes Utilizados

#### RichAI
- **Função:** Componente de movimento de alto nível
- **Características:**
  - Navegação suave com aceleração/desaceleração
  - Evitação de obstáculos automática
  - Rotação para direção do movimento
  - Detecção de chegada ao destino

**Configuração Padrão:**
```csharp
richAI.maxSpeed = 3.5f;           // Velocidade máxima
richAI.acceleration = 8f;         // Aceleração
richAI.rotationSpeed = 120f;      // Graus/segundo
richAI.endReachedDistance = 0.5f; // Distância de chegada
richAI.slowdownTime = 0.5f;       // Tempo de desaceleração
richAI.wallDist = 1f;             // Distância de paredes
richAI.funnelSimplification = true; // Simplificar caminhos
```

#### Seeker
- **Função:** Calcula caminhos usando o grafo de navegação
- **Uso:** Anexado automaticamente às unidades com `UnitMovement`

### Workflow de Pathfinding

```
1. Jogador clica no terreno
   ↓
2. MovementCommandHandler calcula formação
   ↓
3. UnitMovement define destino no RichAI
   ↓
4. Seeker calcula caminho no grafo A*
   ↓
5. RichAI move a unidade seguindo o caminho
   ↓
6. Chegou ao destino → OnReachedDestination()
```

---

## Sistema de Input

### Unity Input System

O Lote 6 utiliza o **novo Input System** do Unity para controles de formação.

**Asset de Actions:** `Input/PlayerControls.inputactions`

### Mapeamento de Teclas

| Tecla | Ação | Formação |
|-------|------|----------|
| `F1` | Formação Linha | Line |
| `F2` | Formação Coluna | Column |
| `F3` | Formação Triangular | Triangular |
| `F4` | Formação Cunha | Wedge |
| `F5` | Formação Circular | Circular |
| `F6` | Formação Quadrada | Square |
| `H` | Hold (Parar/None) | None |

### Implementação de Controles de Formação

```csharp
[Header("Formation Input Actions")]
public InputActionReference formationLineAction;
public InputActionReference formationColumnAction;
public InputActionReference formationTriangularAction;
public InputActionReference formationWedgeAction;
public InputActionReference formationCircularAction;
public InputActionReference formationSquareAction;
public InputActionReference stopUnitsAction;

void OnEnable()
{
    // Habilitar e conectar actions
    formationLineAction.action.Enable();
    formationLineAction.action.performed += ctx => SetFormation(FormationType.Line);
    
    // ... (repetir para outras formações)
    
    stopUnitsAction.action.Enable();
    stopUnitsAction.action.performed += ctx => StopSelectedUnits();
}
```

### Fluxo de Comando de Formação

```
1. Jogador pressiona tecla (ex: F1)
   ↓
2. Input System dispara action
   ↓
3. MovementCommandHandler.SetFormation(FormationType.Line)
   ↓
4. Detecta grupos na seleção
   ↓
5. Cria/atualiza grupo com nova formação
   ↓
6. Se unidades estão em movimento:
   → Recalcula offsets
   → Atualiza destinos individuais
   Senão:
   → Aguarda próximo comando de movimento
```

---

## Classes Refatoradas

As seguintes classes foram modificadas/expandidas para suportar o sistema de movimentação e formações:

### GameEvents.cs

**Novos Eventos Adicionados:**

```csharp
// Comando de movimento (CRÍTICO para o sistema)
public static event Action<Vector3> OnMoveCommand;

// Morte de unidade (para remover de grupos)
public static event Action<Unit> OnUnitDied;
```

**Métodos Raise:**

```csharp
public static void RaiseMoveCommand(Vector3 worldPosition);
public static void RaiseUnitDied(Unit unit);
```

---

### Enums.cs

**Novos Enums:**

```csharp
/// <summary>
/// Tipos de formação para tropas
/// </summary>
public enum FormationType
{
    None = -1,      // Sem formação: movimentação livre
    Line = 0,       // Linha: múltiplas linhas com passagem central
    Column = 1,     // Coluna: múltiplas colunas com passagem central
    Triangular = 2, // Triangular: triângulos inscritos
    Wedge = 3,      // Cunha: apenas ponta do triângulo
    Circular = 4,   // Circular: círculos concêntricos
    Square = 5      // Quadrática: quadrado denso
}

/// <summary>
/// Prioridade de posicionamento na formação
/// </summary>
public enum FormationPriority
{
    VeryLow = 0,    // Workers
    Low = 1,        // Siege
    Medium = 2,     // Cavalry, Archers
    High = 3,       // Spearman
    VeryHigh = 4,   // Warrior
    Maximum = 5     // Hero
}
```

---

### Unit.cs

**Propriedades Adicionadas:**

```csharp
/// <summary>
/// Velocidade máxima da unidade
/// </summary>
public float MaxSpeed => def ? def.moveSpeed : 3.5f;

/// <summary>
/// Prioridade de formação da unidade
/// </summary>
public FormationPriority FormationPriority => 
    def ? def.formationPriority : FormationPriority.Medium;

/// <summary>
/// Grupo de formação ao qual pertence
/// </summary>
public FormationGroup CurrentFormationGroup
{
    get => FormationGroupManager.Instance?.GetGroupForUnit(this);
}

/// <summary>
/// Verifica se está em algum grupo
/// </summary>
public bool IsInFormationGroup => CurrentFormationGroup != null;

/// <summary>
/// Formação atual (do grupo ou None)
/// </summary>
public FormationType CurrentFormation
{
    get
    {
        var group = CurrentFormationGroup;
        return group != null ? group.Formation : FormationType.None;
    }
}
```

**Método de Morte Modificado:**

```csharp
void Die()
{
    // Notificar sistema de grupos ANTES de destruir
    GameEvents.RaiseUnitDied(this);
    
    Debug.Log($"{DisplayName} morreu!");
    Destroy(gameObject);
}
```

---

### UnitDefinition.cs

**Campos Adicionados:**

```csharp
[Header("Stats de Movimento")]
[Tooltip("Velocidade máxima em unidades/segundo")]
public float moveSpeed = 3.5f;

[Tooltip("Aceleração da unidade")]
public float acceleration = 8f;

[Tooltip("Velocidade de rotação em graus/segundo")]
public float rotationSpeed = 120f;

[Header("Formação")]
[Tooltip("Prioridade na formação (maior = mais à frente)")]
public FormationPriority formationPriority = FormationPriority.Medium;

[Tooltip("Espaçamento preferido entre unidades")]
public float spacing = 2f;
```

**Método de Configuração Automática:**

```csharp
[ContextMenu("AutoConfigureByType")]
public void AutoConfigureByType()
{
    switch (type)
    {
        case UnitType.Hero:
            formationPriority = FormationPriority.Maximum;
            moveSpeed = 4.5f;
            break;
            
        case UnitType.Warrior:
            formationPriority = FormationPriority.VeryHigh;
            moveSpeed = 3.5f;
            break;
            
        // ... outros tipos
    }
}
```

---

### InputSelection.cs

**Modificação Crítica: Sistema de Prioridade**

O `InputSelection` foi refatorado para implementar um **sistema de prioridade de ações** que evita conflitos entre clique em unidade e comando de movimento:

**PRIORIDADE DE AÇÕES (maior → menor):**
1. **UI** (EventSystem)
2. **Drag Selection** (retângulo de seleção)
3. **Unit Click** (selecionar unidade)
4. **Movement Command** (mover unidades)
5. **Ground Click** (desselecionar com Ctrl)

**Código Crítico:**

```csharp
void HandleClick(Vector2 screenPos)
{
    bool isCtrl = IsCtrlPressed;
    
    // PRIORIDADE 1: Clicar em UNIDADE (apenas seleciona)
    if (picker.TryPickUnitAt(screenPos, out var unit))
    {
        GameEvents.RaiseUnitClick(unit, isCtrl);
        return; // ← CRITICAL: Sai aqui, NÃO dispara movimento
    }
    
    // PRIORIDADE 2: Clicar no CHÃO
    if (picker.TryPickGroundAt(screenPos, out var point, out _))
    {
        // SEM modificadores = Comando de Movimento
        if (!isCtrl && !isShift)
        {
            GameEvents.RaiseMoveCommand(point);
        }
        else
        {
            // COM Ctrl/Shift = Desselecionar
            GameEvents.RaiseGroundClick(point, isCtrl);
        }
    }
}
```

**Proteções Implementadas:**

- ✅ Click em unidade NÃO dispara movimento
- ✅ Drag NÃO dispara movimento no início
- ✅ Click sobre UI é ignorado completamente
- ✅ Flag `_pressedOverUI` trava ações se começou sobre UI

---

## Ferramentas de Debug

### FormationVisualizer

**Localização:** `Scripts/Debug/FormationVisualizer.cs`

**Função:** Mostra preview da formação na Scene View enquanto o mouse se move.

**Recursos:**
- Preview em tempo real das posições de formação
- Raio de formação visualizado
- Label com tipo de formação e contagem de unidades
- Toggle para ligar/desligar (padrão: ligado)

```csharp
[SerializeField] private bool showPreview = true;
[SerializeField] private Color previewColor = new Color(0f, 1f, 0f, 0.3f);
[SerializeField] private float previewRadius = 0.4f;
```

**Gizmos Desenhados:**
- Esferas nas posições de cada unidade
- Linhas conectando ao centro da formação
- Círculo amarelo no centro
- Disco mostrando raio total da formação

---

### UnitMovementDebugUI

**Localização:** `Scripts/Debug/UnitMovementDebugUI.cs`

**Função:** Mostra informações de movimento das unidades selecionadas na tela.

**Informações Exibidas:**
- Nome da unidade
- Status (MOVENDO / PARADO)
- Posição atual
- Destino final (se movendo)
- Distância até destino

```csharp
[SerializeField] private bool showDebugUI = true;
[SerializeField] private Vector2 position = new Vector2(10, 200);
```

---

### Context Menu de Debug

**MovementCommandHandler:**
- `Debug: Show Groups In Selection` - Mostra grupos envolvidos na seleção
- `Debug: Test Formation None/Line/Column` - Testa formações rapidamente

**FormationGroupManager:**
- `Debug: Show All Groups` - Lista todos os grupos ativos
- `Debug: Clear All Groups` - Limpa todos os grupos

**Unit:**
- `Debug: Show Formation Info` - Mostra informações de formação da unidade

**UnitMovement:**
- `Debug: Move Forward 10 units` - Força movimento para teste
- `Debug: Show Status` - Mostra status detalhado de movimento

---

## Fluxo Completo de Movimentação

### Cenário 1: Movimento de Unidades Soltas (Sem Grupo)

```
1. Jogador seleciona 5 unidades (nenhuma em grupo)
   ↓
2. Jogador clica no terreno
   ↓
3. InputSelection.HandleClick()
   → GameEvents.RaiseMoveCommand(Vector3 worldPos)
   ↓
4. MovementCommandHandler.OnMoveCommandReceived(worldPos)
   → FormationGroupManager.GetGroupsInSelection()
   → Retorna: 0 grupos, 5 unidades soltas
   ↓
5. Usa formação padrão (None ou última usada)
   ↓
6. FormationCalculator.CalculateFormation()
   → Retorna Dictionary<Unit, Vector3> offsets
   ↓
7. FormationGroupManager.CreateGroup(units, formation)
   → Cria novo grupo (ex: ID=1)
   ↓
8. Para cada unidade:
   → UnitMovement.MoveToWithOffset(worldPos, offset)
   → RichAI calcula caminho
   → Unidade se move
   ↓
9. Quando chega:
   → UnitMovement.OnReachedDestination()
```

---

### Cenário 2: Mudança de Formação Durante Movimento

```
1. Unidades já estão se movendo em Formação Linha (Grupo ID=1)
   ↓
2. Jogador pressiona F3 (Triangular)
   ↓
3. MovementCommandHandler.SetFormation(FormationType.Triangular)
   ↓
4. Detecta grupo na seleção (Grupo ID=1, 10 unidades)
   ↓
5. FormationGroupManager.UpdateGroupFormation(grupo, Triangular)
   ↓
6. Recalcula offsets:
   → FormationCalculator.CalculateFormation(units, Triangular, spacing)
   ↓
7. Para cada unidade:
   → UnitMovement.UpdateFormationOffset(novoOffset)
   → RichAI atualiza destino (mantém targetPosition)
   ↓
8. Unidades ajustam trajetória em tempo real
```

---

### Cenário 3: Mescla de Múltiplos Grupos

```
1. Jogador tem:
   → Grupo A (ID=1, Linha, 8 unidades)
   → Grupo B (ID=2, Coluna, 5 unidades)
   ↓
2. Jogador seleciona ambos os grupos (13 unidades)
   ↓
3. Jogador pressiona F4 (Cunha)
   ↓
4. MovementCommandHandler.SetFormation(FormationType.Wedge)
   ↓
5. FormationGroupManager.GetGroupsInSelection()
   → Detecta 2 grupos
   ↓
6. FormationGroupManager.MergeGroups()
   → Destrói Grupo A e Grupo B
   → Cria novo Grupo C (ID=3, Cunha, 13 unidades)
   ↓
7. Aguarda próximo comando de movimento para aplicar formação
```

---

### Cenário 4: Subdivisão de Grupo

```
1. Grupo existente (ID=1, Linha, 15 unidades)
   ↓
2. Jogador seleciona apenas 5 unidades do grupo
   ↓
3. Jogador move essas 5 unidades para outro local
   ↓
4. MovementCommandHandler.OnMoveCommandReceived()
   ↓
5. Detecta:
   → 5 unidades do Grupo 1
   → 10 unidades restantes permanecem no Grupo 1
   ↓
6. FormationGroupManager.CreateGroup()
   → Remove 5 unidades do Grupo 1
   → Cria novo Grupo 2 (mesma formação: Linha)
   ↓
7. Grupo 1 continua com 10 unidades
8. Grupo 2 criado com 5 unidades
```

---

## Boas Práticas e Recomendações

### Performance

1. **Limite de Unidades por Grupo:** Recomendado máximo de 50 unidades por grupo
2. **Atualização de Formação:** Recalcular offsets apenas quando necessário
3. **Limpeza Automática:** Sistema limpa grupos vazios a cada segundo (60 frames)
4. **Centro Inteligente:** Ignora outliers para melhor performance em grupos grandes

### Gameplay

1. **Formação Padrão:** Use `None` para unidades novas (movimento livre)
2. **Gargalos:** Sistema detecta automaticamente e força coluna
3. **Priorização:** Configure corretamente `FormationPriority` em `UnitDefinition`
4. **Espaçamento:** Use 2.0f para combate, 1.5f para movimento rápido

### Debug

1. **Gizmos:** Habilite `showDebugGizmos` em MovementCommandHandler
2. **Logs:** Habilite `showDebugLogs` para acompanhar criação/destruição de grupos
3. **UI Debug:** Use `UnitMovementDebugUI` para monitorar movimento em tempo real
4. **Context Menu:** Use os comandos de debug para testar rapidamente

---

## Próximos Passos

O Lote 6 estabelece a base para sistemas futuros:

- **Lote 7:** Combate entre unidades (atacar inimigos, formações de combate)
- **Lote 8:** Coleta de recursos (workers, árvores, pedras)
- **Lote 9:** Construção (edifícios, muralhas)
- **Lote 10:** IA de unidades (comportamento autônomo, patrulha)

---

## Referências Técnicas

### Documentação Externa

- **A* Pathfinding Project:** https://www.arongranberg.com/astar/docs
- **Unity Input System:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html
- **Unity Events:** https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html

### Scripts Principais

| Script | Localização | Descrição |
|--------|-------------|-----------|
| MovementCommandHandler | Scripts/Core/ | Gerenciador de comandos |
| FormationGroupManager | Scripts/Core/ | Gerenciador de grupos |
| FormationCalculator | Scripts/Core/ | Cálculos de formação |
| FormationGroup | Scripts/Core/ | Classe de grupo |
| UnitMovement | Scripts/Core/ | Movimento individual |
| FormationVisualizer | Scripts/Debug/ | Visualização |
| UnitMovementDebugUI | Scripts/Debug/ | Debug UI |

---

## Conclusão

O **Lote 6** transforma Medieval Thrones em um RTS tático completo, com movimentação inteligente, formações realistas e gerenciamento automático de grupos. O sistema é extensível, performático e fácil de usar tanto para jogadores quanto para desenvolvedores.

**Status:** ✅ **CONCLUÍDO E TESTADO**

**Data de Conclusão:** 26/10/2025

**Versão:** 1.0
