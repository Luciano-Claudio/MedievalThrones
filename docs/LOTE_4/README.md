---
layout: default
title: LOTE 4 — SELECTION SYSTEM (SISTEMA DE SELEÇÃO)
permalink: /lote-4/
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
