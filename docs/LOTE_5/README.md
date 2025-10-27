---
layout: default
title: LOTE 5 — USER INTERFACE / LEFT BAR
permalink: /lote-5/
---
# LOTE 5 — USER INTERFACE / LEFT BAR

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
# LOTE 5 (PARTE 2) — TOP HUD, BUTTON HUD & MINIMAPA

**Versão:** 2.1 (Atualizada - Outubro 2025)  
**Status:** 🔄 Parcialmente Implementado (Time/Minimapa) + 📐 Arquitetura Futura (Recursos/Comandos)  

---

## 📚 ÍNDICE COMPLETO

### PARTE I: VISÃO GERAL
1. [Introdução ao Subsistema de UI](#1-introdução-ao-subsistema-de-ui)
2. [Arquitetura Event-Driven](#2-arquitetura-event-driven)
3. [Estado Atual vs. Roadmap](#3-estado-atual-vs-roadmap)

### PARTE II: TOP HUD (SISTEMA DE TEMPO)
4. [TimeManager - Gerenciador de Tempo](#4-timemanager---gerenciador-de-tempo)
5. [ClockUI - Display de Relógio](#5-clockui---display-de-relógio)
6. [Integração Time/Clock via GameEvents](#6-integração-timeclock-via-gameevents)

### PARTE III: TOP HUD (RECURSOS - ARQUITETURA FUTURA)
7. [ResourceManager - Arquitetura Proposta](#7-resourcemanager---arquitetura-proposta)
8. [ResourceDisplayUI - Display de Recursos](#8-resourcedisplayui---display-de-recursos)
9. [Integração com GameEvents](#9-integração-com-gameevents)

### PARTE IV: TOP HUD (RANKING & CHAT - ARQUITETURA FUTURA)
10. [RankingSystem - Sistema de Ranking](#10-rankingsystem---sistema-de-ranking)
11. [ChatSystem - Sistema de Chat](#11-chatsystem---sistema-de-chat)
12. [UI Modular (Ranking/Chat)](#12-ui-modular-rankingchat)

### PARTE V: BUTTON HUD (COMANDOS - ARQUITETURA FUTURA)
13. [CommandController - Controlador de Comandos](#13-commandcontroller---controlador-de-comandos)
14. [UnitActionButtons - Botões de Ação](#14-unitactionbuttons---botões-de-ação)
15. [GroupHotkeyManager - Atalhos Ctrl+1~9](#15-grouphotkeymanager---atalhos-ctrl19)
16. [Integração com SelectionManager](#16-integração-com-selectionmanager)

### PARTE VI: MINIMAPA
17. [MinimapController - Controlador Principal](#17-minimapcontroller---controlador-principal)
18. [Sistema de Ícones e Pool](#18-sistema-de-ícones-e-pool)
19. [Zoom e Navegação](#19-zoom-e-navegação)
20. [Click-to-Move (Go To)](#20-click-to-move-go-to)
21. [DisableMinimapShadows - Otimização](#21-disableminimapshadows---otimização)

### PARTE VII: INTEGRAÇÃO E PATTERNS
22. [FactionDatabase - Sistema de Facções](#22-factiondatabase---sistema-de-facções)
23. [Event Bus Integration](#23-event-bus-integration)
24. [Fluxo de Inicialização Completo](#24-fluxo-de-inicialização-completo)

### PARTE VIII: IMPLEMENTAÇÃO E MANUTENÇÃO
25. [Roadmap de Implementação](#25-roadmap-de-implementação)
26. [Estrutura de Arquivos](#26-estrutura-de-arquivos)
27. [Solução de Problemas](#27-solução-de-problemas)
28. [Checklist de Validação](#28-checklist-de-validação)
29. [Melhorias Sugeridas](#29-melhorias-sugeridas)

---

# PARTE I: VISÃO GERAL

---

## 1) INTRODUÇÃO AO SUBSISTEMA DE UI

### 1.1 Objetivo

O **Lote 5 (Parte 2)** complementa a documentação da interface do usuário do Medieval Thrones, cobrindo três subsistemas críticos:

- **Top HUD**: Informações persistentes (tempo, recursos, ranking, chat)
- **Button HUD**: Ações de comando (mover, atacar, formações) e hotkeys de grupo
- **Minimapa**: Navegação espacial e visão estratégica

### 1.2 Arquitetura Geral

```
┌─────────────────────────────────────────────────────────┐
│                      GameEvents (Event Bus)              │
│  - OnTimeOfDay / OnClockChanged                         │
│  - OnResourceChanged (futuro)                           │
│  - OnUnitSpawned / OnUnitDespawned                      │
│  - OnSelectionChanged                                    │
└────────────────┬────────────────────────────────────────┘
                 │
    ┌────────────┼────────────┬───────────────┐
    ↓            ↓            ↓               ↓
┌─────────┐ ┌─────────┐ ┌──────────┐ ┌──────────────┐
│ Top HUD │ │Button   │ │ Minimapa │ │ Left Bar     │
│         │ │  HUD    │ │          │ │ (Parte 1)    │
├─────────┤ ├─────────┤ ├──────────┤ ├──────────────┤
│ Clock   │ │Command  │ │ Icons    │ │ UnitList     │
│Resource │ │Buttons  │ │ Zoom     │ │ Groups       │
│Ranking  │ │Hotkeys  │ │ Click-To │ │ Drag-n-Drop  │
│ Chat    │ │         │ │  -Move   │ │              │
└─────────┘ └─────────┘ └──────────┘ └──────────────┘
```

### 1.3 Estado Atual vs. Futuro

| Componente | Status | Código Disponível | Prioridade |
|------------|--------|-------------------|------------|
| **TimeManager** | ✅ Implementado | `TimeManager.cs` | - |
| **ClockUI** | ✅ Implementado | `ClockUI.cs` | - |
| **MinimapController** | ✅ Implementado | `MinimapController.cs` | - |
| **FactionDatabase** | ✅ Implementado | `FactionDatabase.cs` | - |
| **ResourceManager** | ⏳ Futuro | Arquitetura proposta | Alta |
| **CommandController** | ⏳ Futuro | Arquitetura proposta | Alta |
| **RankingSystem** | ⏳ Futuro | Arquitetura proposta | Média |
| **ChatSystem** | ⏳ Futuro | Arquitetura proposta | Baixa |
| **InputManager** | ⏳ Futuro | Arquitetura proposta | Alta |

### 1.4 Princípios de Design

✅ **Event-Driven**: Comunicação via `GameEvents` (Lote 1)  
✅ **Modular**: Componentes independentes e reutilizáveis  
✅ **Escalável**: Fácil adicionar novos recursos/comandos  
✅ **Performático**: Pooling de ícones, updates otimizados  
✅ **Testável**: Lógica separada de visual  

---

## 2) ARQUITETURA EVENT-DRIVEN

### 2.1 Fluxo de Comunicação

```
[Fonte de Dados]
    │
    ↓ (dispara evento)
GameEvents.RaiseXXX(data)
    │
    ↓ (múltiplos listeners)
├─→ TopHUD.OnXXX(data)
├─→ ButtonHUD.OnXXX(data)
├─→ Minimap.OnXXX(data)
└─→ [Outros sistemas]
```

**Benefícios:**
- 🔌 **Desacoplamento**: Sistemas não se referenciam diretamente
- 🔄 **Reatividade**: UI atualiza automaticamente
- 🧩 **Extensibilidade**: Adicionar listeners sem modificar emissores
- 🧪 **Testabilidade**: Emitir eventos de teste facilmente

### 2.2 Eventos Utilizados (Top HUD & Minimapa)

#### Eventos de Tempo

```csharp
// TimeManager → ClockUI
GameEvents.OnTimeOfDay      // (float time01) - Todo frame
GameEvents.OnDayChanged      // (int dayCount) - Ao virar o dia
GameEvents.OnClockChanged    // (int day, int hour, int minute) - A cada minuto
```

#### Eventos de Unidades

```csharp
// UnitRegistry → MinimapController
GameEvents.OnUnitSpawned     // (Unit unit) - Ao spawnar
GameEvents.OnUnitDespawned   // (Unit unit) - Ao despawnar
```

#### Eventos de Seleção (futura integração Button HUD)

```csharp
// SelectionManager → ButtonHUD
GameEvents.OnSelectionChanged // (IReadOnlyCollection<Unit> selection)
```

#### Eventos Futuros (Recursos, Ranking, Chat)

```csharp
// ResourceManager → ResourceDisplayUI (proposto)
GameEvents.OnResourceChanged  // (ResourceType type, int amount)

// RankingSystem → RankingUI (proposto)
GameEvents.OnRankingUpdated   // (List<RankEntry> rankings)

// ChatSystem → ChatUI (proposto)
GameEvents.OnChatMessageReceived // (ChatMessage message)
```

### 2.3 Padrão Observer (Event Bus)

**Implementação no GameEvents (Lote 1):**

```csharp
// GameEvents.cs (resumo)
public static class GameEvents
{
    // Eventos de Tempo
    public static event System.Action<float> OnTimeOfDay;
    public static event System.Action<int> OnDayChanged;
    public static event System.Action<int, int, int> OnClockChanged;
    
    // Métodos de Disparo
    public static void RaiseTimeOfDay(float time01)
    {
        OnTimeOfDay?.Invoke(time01);
    }
    
    public static void RaiseDayChanged(int dayCount)
    {
        OnDayChanged?.Invoke(dayCount);
    }
    
    public static void RaiseClockChanged(int day, int hour, int minute)
    {
        OnClockChanged?.Invoke(day, hour, minute);
    }
}
```

**Uso em Componentes:**

```csharp
// Emissor (TimeManager)
void Update()
{
    // ... lógica de tempo ...
    GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
}

// Listener (ClockUI)
void OnEnable()
{
    GameEvents.OnClockChanged += UpdateClock;
}

void OnDisable()
{
    GameEvents.OnClockChanged -= UpdateClock; // CRÍTICO: Sempre desinscrever
}

void UpdateClock(int day, int hour, int minute)
{
    // Atualizar UI
}
```

---

## 3) ESTADO ATUAL VS. ROADMAP

### 3.1 Hierarquia de UI (Atual)

**Baseado no screenshot fornecido:**

```
Canvas (CanvasUI)
├── TopHud (GameObject)
│   ├── Clock (GameObject) ✅ IMPLEMENTADO
│   │   ├── Day (TMP_Text) → "Day 0"
│   │   └── Time (TMP_Text) → "Time 00:00"
│   ├── Online (GameObject) ⏳ FUTURO
│   │   ├── Ranking (Button)
│   │   └── Chat (Button)
│   └── Resource (GameObject) ⏳ FUTURO
│       ├── Gold (TMP_Text + Icon)
│       ├── Wood (TMP_Text + Icon)
│       └── Food (TMP_Text + Icon)
│
├── LeftBar (GameObject) ✅ DOCUMENTADO (Parte 1)
│   ├── Button (GameObject)
│   ├── DragRoot (GameObject)
│   ├── Menu (GameObject)
│   ├── UnitList (ScrollRect)
│   └── CreateGroup (GameObject)
│
├── BottomBar (GameObject) ⏳ FUTURO
│   ├── Hud (GameObject)
│   │   └── Image (RawImage) → Portrait da unidade
│   └── Buttons (GameObject) → Comandos de ação
│
├── Minimap (GameObject) ✅ IMPLEMENTADO
│   ├── Mask (Image)
│   └── Buttons (GameObject) → Zoom In/Out
│
└── EventSystem (GameObject) ✅ EXISTENTE
```

### 3.2 Roadmap de Implementação

#### FASE 1: Sistemas Core (ATUAL)
- [x] TimeManager + ClockUI (completo)
- [x] MinimapController (completo)
- [x] FactionDatabase (completo)
- [x] UnitListPanel (Parte 1 - completo)

#### FASE 2: Recursos e Economia (PRÓXIMA PRIORIDADE)
- [ ] ResourceManager (gerenciamento de ouro/madeira/comida)
- [ ] ResourceDisplayUI (display no Top HUD)
- [ ] Integração com construções/unidades (consumo/produção)

#### FASE 3: Comandos e Ações (ALTA PRIORIDADE)
- [ ] CommandController (sistema de comandos)
- [ ] UnitActionButtons (botões Move/Attack/Defend)
- [ ] InputManager (hotkeys Ctrl+1~9)
- [ ] GroupHotkeyManager (atalhos de grupo)

#### FASE 4: Social e Multiplayer (MÉDIA PRIORIDADE)
- [ ] RankingSystem (placar de jogadores)
- [ ] RankingUI (display no Top HUD)
- [ ] ChatSystem (mensagens multiplayer)
- [ ] ChatUI (interface de chat)

#### FASE 5: Polimento e UX (BAIXA PRIORIDADE)
- [ ] Notificações (alertas de eventos)
- [ ] Tooltips (informações ao hover)
- [ ] Animações de transição
- [ ] Feedback visual (flash, shake)

---

# PARTE II: TOP HUD (SISTEMA DE TEMPO)

---

## 4) TIMEMANAGER - GERENCIADOR DE TEMPO

### 4.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `TimeManager.cs`  
**Responsabilidade:** Gerencia o relógio global do jogo, avançando o tempo e disparando eventos.

### 4.2 Estrutura da Classe

```csharp
/// <summary>
/// Avança o relógio global e emite eventos de tempo.
/// Sistema completo de dia/noite com conversão para HH:MM.
/// </summary>
public class TimeManager : MonoBehaviour
{
    // ========== CONFIGURAÇÃO ==========
    public GameConfig config;                    // Configuração global (SecondsPerDay, etc)
    [Range(0f, 1f)] public float startTime01 = 0.25f; // Tempo inicial (0.25 = ~06:00)
    
    // ========== PROPRIEDADES PÚBLICAS ==========
    [field: SerializeField]
    public float Time01 { get; private set; }    // Fração do dia (0.0 = 00:00, 0.5 = 12:00)
    
    [field: SerializeField]
    public int DayCount { get; private set; }    // Contador de dias
    
    [field: SerializeField]
    public int Hour { get; private set; }        // Hora atual (0-23)
    
    [field: SerializeField]
    public int Minute { get; private set; }      // Minuto atual (0-59)
    
    // ========== ESTADO INTERNO ==========
    private int _lastMinute = -1;                // Para disparar evento só quando muda
    
    // ========== MÉTODOS ==========
    void Awake() { /* inicialização */ }
    void Update() { /* avança tempo */ }
    public bool IsNight() { /* verifica se é noite */ }
}
```

### 4.3 Inicialização

```csharp
/// <summary>
/// Inicializa o tempo e dispara evento inicial.
/// </summary>
void Awake()
{
    Time01 = Mathf.Repeat(startTime01, 1f); // Normaliza entre 0-1
    GameEvents.RaiseTimeOfDay(Time01);       // Dispara evento inicial
}
```

**Explicação:**
- `startTime01 = 0.25` → 25% do dia → ~06:00 (amanhecer)
- `Mathf.Repeat()` garante que o valor esteja entre 0-1
- Evento inicial sincroniza sistemas dependentes (iluminação, skybox)

### 4.4 Avanço de Tempo

```csharp
/// <summary>
/// Avança o tempo a cada frame e dispara eventos.
/// </summary>
void Update()
{
    if (config == null) return;
    
    // 1. Calcular delta de tempo (fração do dia por frame)
    var delta01 = Time.deltaTime / Mathf.Max(1f, config.SecondsPerDay);
    var old = Time01;
    
    // 2. Avançar tempo (com wrap em 1.0)
    Time01 = Mathf.Repeat(Time01 + delta01, 1f);
    
    // 3. Verificar mudança de dia
    if (Time01 < old) // Houve wrap (passou de 1.0 para 0.0)
    {
        DayCount++;
        GameEvents.RaiseDayChanged(DayCount);
    }
    
    // 4. Converter fração para HH:MM (24h)
    int totalMinutes = Mathf.FloorToInt(Time01 * 1440f); // 24h * 60min = 1440
    Hour = (totalMinutes / 60) % 24;   // Hora (0-23)
    Minute = totalMinutes % 60;        // Minuto (0-59)
    
    // 5. Disparar evento de relógio (apenas quando minuto muda)
    if (Minute != _lastMinute)
    {
        _lastMinute = Minute;
        GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
    }
    
    // 6. Disparar evento de fração do dia (todo frame)
    GameEvents.RaiseTimeOfDay(Time01);
}
```

**Explicação do Cálculo:**

```
SecondsPerDay = 600 segundos (10 minutos reais = 1 dia in-game)
deltaTime = 0.016s (60 FPS)
delta01 = 0.016 / 600 = 0.0000267 (avanço por frame)

Time01 = 0.0 → 00:00 (meia-noite)
Time01 = 0.25 → 06:00 (amanhecer)
Time01 = 0.5 → 12:00 (meio-dia)
Time01 = 0.75 → 18:00 (entardecer)
Time01 = 1.0 → 00:00 (meia-noite novamente)

totalMinutes = 0.5 * 1440 = 720 min
Hour = 720 / 60 = 12
Minute = 720 % 60 = 0
→ 12:00
```

### 4.5 Verificação de Noite

```csharp
/// <summary>
/// Verifica se o horário atual é noite.
/// Baseado em config.dayFraction (ex: 0.5 = noite começa ao meio-dia).
/// </summary>
public bool IsNight()
{
    // Ex.: dayFraction = 0.5
    // Noite = [0.5, 1.0) (12:00 até 00:00)
    return Time01 >= config.dayFraction;
}
```

**Uso:**

```csharp
// Em outro sistema (ex: EnemyAI)
if (timeManager.IsNight())
{
    // Aumentar spawn de inimigos noturnos
}
```

### 4.6 Propriedades Públicas

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Time01` | `float` | Fração do dia (0.0 = 00:00, 1.0 = 24:00) |
| `DayCount` | `int` | Contador de dias (começa em 0) |
| `Hour` | `int` | Hora atual (0-23) |
| `Minute` | `int` | Minuto atual (0-59) |

### 4.7 Eventos Disparados

```csharp
// Todo frame (para iluminação/skybox)
GameEvents.RaiseTimeOfDay(Time01);

// Quando dia vira (00:00)
GameEvents.RaiseDayChanged(DayCount);

// A cada minuto (para relógio UI)
GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
```

### 4.8 Integração com GameConfig

**GameConfig.cs (resumo):**

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Config")]
public class GameConfig : ScriptableObject
{
    [Header("Time Settings")]
    public float SecondsPerDay = 600f;     // 10 minutos reais = 1 dia in-game
    [Range(0f, 1f)] public float dayFraction = 0.5f; // 0.5 = noite começa ao meio-dia
}
```

**Setup no Inspector:**

```
TimeManager (GameObject)
├── TimeManager (MonoBehaviour)
│   ├── config: GameConfig (ScriptableObject)
│   └── startTime01: 0.25 (amanhecer)
```

### 4.9 Diagrama de Fluxo

```
[TimeManager.Update()]
    │
    ├─→ Calcular delta01 (Time.deltaTime / SecondsPerDay)
    │
    ├─→ Avançar Time01 (wrap em 1.0)
    │
    ├─→ if (Time01 < old):
    │       DayCount++
    │       GameEvents.RaiseDayChanged(DayCount)
    │
    ├─→ Converter Time01 → HH:MM
    │
    ├─→ if (Minute != _lastMinute):
    │       GameEvents.RaiseClockChanged(Day, Hour, Minute)
    │
    └─→ GameEvents.RaiseTimeOfDay(Time01)
```

---

## 5) CLOCKUI - DISPLAY DE RELÓGIO

### 5.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `ClockUI.cs`  
**Responsabilidade:** Exibe o relógio (dia e hora) no Top HUD. Escuta `GameEvents.OnClockChanged` e atualiza UI.

### 5.2 Estrutura da Classe

```csharp
/// <summary>
/// Display do relógio no Top HUD.
/// REFATORADO: Usa GameEvents em vez de polling.
/// </summary>
public class ClockUI : MonoBehaviour
{
    // ========== REFS ==========
    public TimeManager timeManager;   // Referência (usado apenas no Start)
    public TMP_Text clockText;        // "Time 12:34"
    public TMP_Text dayText;          // "Day 5"
    
    // ========== CACHE LOCAL ==========
    private int day, hour, minute;
    
    // ========== MÉTODOS ==========
    void OnEnable() { /* subscribe */ }
    void OnDisable() { /* unsubscribe */ }
    void Start() { /* sincronização inicial */ }
    void Update() { /* atualiza texto */ }
    public void UpdateClock(int day, int hour, int minute) { /* handler */ }
}
```

### 5.3 Subscrição de Eventos

```csharp
/// <summary>
/// Inscreve no evento OnClockChanged.
/// CRÍTICO: Sempre desinscrever em OnDisable.
/// </summary>
void OnEnable()
{
    GameEvents.OnClockChanged += UpdateClock;
}

void OnDisable()
{
    GameEvents.OnClockChanged -= UpdateClock; // Evita memory leak
}
```

**Importância:**
- ✅ **Performance**: Não precisa poll `TimeManager` todo frame
- ✅ **Desacoplamento**: `ClockUI` não referencia `TimeManager` diretamente (exceto no `Start`)
- ⚠️ **Memory Leak**: Sempre desinscrever em `OnDisable`

### 5.4 Sincronização Inicial

```csharp
/// <summary>
/// Sincroniza estado inicial do relógio.
/// Necessário porque OnEnable pode vir antes do primeiro evento.
/// </summary>
void Start()
{
    if (timeManager != null)
    {
        UpdateClock(timeManager.DayCount, timeManager.Hour, timeManager.Minute);
    }
}
```

**Por que é necessário?**

```
Timeline:
1. ClockUI.OnEnable() → Subscribe em OnClockChanged
2. ClockUI.Start() → Sincronizar estado inicial
3. TimeManager.Update() → Primeiro evento disparado

Sem o Start():
- UI ficaria com "Day 0, Time 00:00" até o próximo minuto
```

### 5.5 Handler de Evento

```csharp
/// <summary>
/// Handler de GameEvents.OnClockChanged.
/// Apenas cacheia valores (Update renderiza).
/// </summary>
public void UpdateClock(int day, int hour, int minute)
{
    this.day = day;
    this.hour = hour;
    this.minute = minute;
}
```

### 5.6 Renderização (Update)

```csharp
/// <summary>
/// Atualiza textos a cada frame.
/// NOTA: Poderia ser otimizado para só atualizar quando valores mudam.
/// </summary>
void Update()
{
    if (clockText)
        clockText.text = $"Time {hour:00}:{minute:00}";
    
    if (dayText)
        dayText.text = $"Day {day}";
}
```

**Otimização Futura:**

```csharp
// Otimizado: só atualiza quando valores mudam
public void UpdateClock(int day, int hour, int minute)
{
    bool changed = (this.day != day || this.hour != hour || this.minute != minute);
    
    this.day = day;
    this.hour = hour;
    this.minute = minute;
    
    if (changed)
    {
        RenderClock();
    }
}

void RenderClock()
{
    if (clockText) clockText.text = $"Time {hour:00}:{minute:00}";
    if (dayText) dayText.text = $"Day {day}";
}
```

### 5.7 Setup no Inspector

```
TopHud (GameObject)
└── Clock (GameObject)
    ├── ClockUI (MonoBehaviour)
    │   ├── timeManager: TimeManager (Scene Reference)
    │   ├── clockText: Time (TMP_Text)
    │   └── dayText: Day (TMP_Text)
    │
    ├── Day (TMP_Text)
    │   └── Text: "Day 0"
    │
    └── Time (TMP_Text)
        └── Text: "Time 00:00"
```

### 5.8 Fluxo Completo

```
[TimeManager.Update()]
    │
    ├─→ Minute mudou?
    │   ├─→ Sim: GameEvents.RaiseClockChanged(Day, Hour, Minute)
    │   │       │
    │   │       ↓
    │   │   [ClockUI.UpdateClock(day, hour, minute)]
    │   │       │
    │   │       ├─→ Cachear valores (this.day = day, etc)
    │   │       │
    │   │       └─→ [ClockUI.Update()]
    │   │               │
    │   │               └─→ Atualizar textos na UI
    │   │
    │   └─→ Não: Continue
    │
    └─→ [Fim do frame]
```

---

## 6) INTEGRAÇÃO TIME/CLOCK VIA GAMEEVENTS

### 6.1 Diagrama de Integração

```
┌──────────────────────────────────────────────────────────┐
│                  TimeManager (Emissor)                    │
│  ┌──────────────────────────────────────────────┐        │
│  │ Update()                                      │        │
│  │   ├─> Avançar Time01                         │        │
│  │   ├─> Converter para HH:MM                   │        │
│  │   └─> if (Minute != _lastMinute):            │        │
│  │           GameEvents.RaiseClockChanged(...)  │────┐   │
│  └──────────────────────────────────────────────┘    │   │
└──────────────────────────────────────────────────────┼───┘
                                                       │
                    ┌──────────────────────────────────┘
                    │ (Event Bus)
                    ↓
┌──────────────────────────────────────────────────────────┐
│              GameEvents (Mediador)                        │
│  public static event Action<int,int,int> OnClockChanged; │
│  public static void RaiseClockChanged(day, hour, min)    │
└────────────────────┬─────────────────────────────────────┘
                     │
        ┌────────────┼────────────┬─────────────┐
        │            │            │             │
        ↓            ↓            ↓             ↓
┌────────────┐ ┌─────────┐ ┌─────────┐ ┌──────────────┐
│  ClockUI   │ │Lighting │ │ SkyBox  │ │ [Futuro]     │
│  (Display) │ │ System  │ │ Control │ │ AI Behavior  │
└────────────┘ └─────────┘ └─────────┘ └──────────────┘
```

### 6.2 Código Completo da Integração

**GameEvents.cs (Lote 1):**

```csharp
public static class GameEvents
{
    // ========== EVENTOS DE TEMPO ==========
    public static event System.Action<float> OnTimeOfDay;
    public static event System.Action<int> OnDayChanged;
    public static event System.Action<int, int, int> OnClockChanged;
    
    // ========== MÉTODOS DE DISPARO ==========
    public static void RaiseTimeOfDay(float time01)
    {
        OnTimeOfDay?.Invoke(time01);
    }
    
    public static void RaiseDayChanged(int dayCount)
    {
        OnDayChanged?.Invoke(dayCount);
    }
    
    public static void RaiseClockChanged(int day, int hour, int minute)
    {
        OnClockChanged?.Invoke(day, hour, minute);
    }
}
```

**TimeManager.cs (Emissor):**

```csharp
public class TimeManager : MonoBehaviour
{
    void Update()
    {
        // ... avanço de tempo ...
        
        // Disparar evento de relógio
        if (Minute != _lastMinute)
        {
            _lastMinute = Minute;
            GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
        }
        
        // Disparar evento de fração do dia
        GameEvents.RaiseTimeOfDay(Time01);
    }
}
```

**ClockUI.cs (Listener):**

```csharp
public class ClockUI : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnClockChanged += UpdateClock;
    }
    
    void OnDisable()
    {
        GameEvents.OnClockChanged -= UpdateClock;
    }
    
    public void UpdateClock(int day, int hour, int minute)
    {
        this.day = day;
        this.hour = hour;
        this.minute = minute;
    }
}
```

### 6.3 Vantagens da Arquitetura

**Desacoplamento:**
```csharp
// SEM Event Bus (acoplado)
public class ClockUI : MonoBehaviour
{
    public TimeManager timeManager; // Dependência direta
    
    void Update()
    {
        day = timeManager.DayCount;     // Poll constante
        hour = timeManager.Hour;
        minute = timeManager.Minute;
    }
}

// COM Event Bus (desacoplado)
public class ClockUI : MonoBehaviour
{
    // Nenhuma dependência direta no Update
    
    void OnEnable()
    {
        GameEvents.OnClockChanged += UpdateClock; // Subscribe uma vez
    }
}
```

**Extensibilidade:**
```csharp
// Adicionar novo sistema sem modificar TimeManager
public class NPCBehavior : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnTimeOfDay += OnTimeChanged;
    }
    
    void OnTimeChanged(float time01)
    {
        if (time01 > 0.75f) // Noite
        {
            GoToSleep();
        }
    }
}
```

### 6.4 Exemplo de Múltiplos Listeners

```csharp
// TimeManager dispara evento
GameEvents.RaiseTimeOfDay(0.8f); // 19:12 (noite)

// Múltiplos sistemas reagem:

// 1. Iluminação
public class LightingController : MonoBehaviour
{
    void OnTimeChanged(float time01)
    {
        directionalLight.intensity = Mathf.Lerp(0.1f, 1.0f, time01);
    }
}

// 2. Skybox
public class SkyboxController : MonoBehaviour
{
    void OnTimeChanged(float time01)
    {
        RenderSettings.skybox.SetFloat("_Rotation", time01 * 360f);
    }
}

// 3. AI Behavior
public class EnemySpawner : MonoBehaviour
{
    void OnTimeChanged(float time01)
    {
        if (time01 > 0.75f) // Noite
        {
            spawnRate *= 2f; // Mais inimigos à noite
        }
    }
}

// 4. Clock UI
public class ClockUI : MonoBehaviour
{
    void OnTimeChanged(float time01)
    {
        // Atualiza relógio (via OnClockChanged)
    }
}
```

---

# PARTE III: TOP HUD (RECURSOS - ARQUITETURA FUTURA)

---

## 7) RESOURCEMANAGER - ARQUITETURA PROPOSTA

### 7.1 Visão Geral

**Tipo:** `MonoBehaviour` (Singleton)  
**Arquivo:** `ResourceManager.cs` (a ser implementado)  
**Responsabilidade:** Gerencia recursos do jogador (ouro, madeira, comida). Dispara eventos ao mudar valores.

### 7.2 Estrutura Proposta

```csharp
/// <summary>
/// Gerenciador de recursos do jogador.
/// Sistema centralizado com eventos para atualizações de UI.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static ResourceManager Instance { get; private set; }
    
    // ========== RECURSOS INICIAIS ==========
    [Header("Starting Resources")]
    [SerializeField] private int startingGold = 1000;
    [SerializeField] private int startingWood = 500;
    [SerializeField] private int startingFood = 300;
    
    // ========== ESTADO ATUAL ==========
    private Dictionary<ResourceType, int> _resources = new();
    
    // ========== PROPRIEDADES ==========
    public int Gold => GetResource(ResourceType.Gold);
    public int Wood => GetResource(ResourceType.Wood);
    public int Food => GetResource(ResourceType.Food);
    
    // ========== MÉTODOS ==========
    void Awake() { /* setup singleton */ }
    void Start() { /* inicializar recursos */ }
    public int GetResource(ResourceType type) { /* consulta */ }
    public bool HasResources(Dictionary<ResourceType, int> cost) { /* verificação */ }
    public bool TrySpendResources(Dictionary<ResourceType, int> cost) { /* gastar */ }
    public void AddResource(ResourceType type, int amount) { /* adicionar */ }
    void SetResource(ResourceType type, int amount) { /* interno */ }
}

/// <summary>
/// Tipos de recursos disponíveis.
/// </summary>
public enum ResourceType
{
    Gold,
    Wood,
    Food
}
```

### 7.3 Singleton Pattern

```csharp
void Awake()
{
    // Singleton pattern
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    
    Instance = this;
    DontDestroyOnLoad(gameObject); // Opcional (se recursos persistem entre cenas)
}
```

### 7.4 Inicialização

```csharp
void Start()
{
    // Inicializar recursos
    _resources[ResourceType.Gold] = startingGold;
    _resources[ResourceType.Wood] = startingWood;
    _resources[ResourceType.Food] = startingFood;
    
    // Disparar eventos iniciais
    GameEvents.RaiseResourceChanged(ResourceType.Gold, startingGold);
    GameEvents.RaiseResourceChanged(ResourceType.Wood, startingWood);
    GameEvents.RaiseResourceChanged(ResourceType.Food, startingFood);
}
```

### 7.5 Consulta de Recursos

```csharp
/// <summary>
/// Retorna quantidade atual de um recurso.
/// </summary>
public int GetResource(ResourceType type)
{
    return _resources.TryGetValue(type, out int amount) ? amount : 0;
}

/// <summary>
/// Verifica se o jogador tem recursos suficientes.
/// </summary>
/// <param name="cost">Dicionário de custos (ex: {Gold: 100, Wood: 50})</param>
public bool HasResources(Dictionary<ResourceType, int> cost)
{
    foreach (var kvp in cost)
    {
        if (GetResource(kvp.Key) < kvp.Value)
            return false;
    }
    return true;
}
```

### 7.6 Gastar Recursos

```csharp
/// <summary>
/// Tenta gastar recursos. Retorna true se bem-sucedido.
/// Dispara eventos de mudança.
/// </summary>
public bool TrySpendResources(Dictionary<ResourceType, int> cost)
{
    // Verificar se tem recursos suficientes
    if (!HasResources(cost))
        return false;
    
    // Gastar recursos
    foreach (var kvp in cost)
    {
        int current = GetResource(kvp.Key);
        SetResource(kvp.Key, current - kvp.Value);
    }
    
    return true;
}
```

### 7.7 Adicionar Recursos

```csharp
/// <summary>
/// Adiciona recursos (ex: ao coletar madeira, construir fazenda).
/// Dispara eventos de mudança.
/// </summary>
public void AddResource(ResourceType type, int amount)
{
    if (amount <= 0) return;
    
    int current = GetResource(type);
    SetResource(type, current + amount);
}
```

### 7.8 Método Interno (Set)

```csharp
/// <summary>
/// Define valor de um recurso e dispara evento.
/// </summary>
void SetResource(ResourceType type, int amount)
{
    amount = Mathf.Max(0, amount); // Não permite valores negativos
    
    _resources[type] = amount;
    
    // Disparar evento
    GameEvents.RaiseResourceChanged(type, amount);
}
```

### 7.9 Eventos Disparados

```csharp
// Ao mudar qualquer recurso
GameEvents.RaiseResourceChanged(ResourceType type, int amount);

// Exemplo de uso:
SetResource(ResourceType.Gold, 1500);
// → GameEvents.RaiseResourceChanged(ResourceType.Gold, 1500)
//   → ResourceDisplayUI.OnResourceChanged(Gold, 1500)
//   → Atualiza texto "1500" no Gold icon
```

### 7.10 Exemplo de Uso

```csharp
// Em BuildingConstructor
public class BuildingConstructor : MonoBehaviour
{
    public void TryBuildBarracks()
    {
        var cost = new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold, 200 },
            { ResourceType.Wood, 150 }
        };
        
        if (ResourceManager.Instance.TrySpendResources(cost))
        {
            // Construir quartel
            Instantiate(barracksPrefab, buildPosition, Quaternion.identity);
            Debug.Log("Barracks constructed!");
        }
        else
        {
            Debug.Log("Not enough resources!");
            // Exibir feedback visual (UI shake, som de erro)
        }
    }
}

// Em ResourceCollector (ex: trabalhador cortando árvore)
public class ResourceCollector : MonoBehaviour
{
    void OnTreeHarvested()
    {
        ResourceManager.Instance.AddResource(ResourceType.Wood, 10);
        // → GameEvents.RaiseResourceChanged(Wood, currentAmount + 10)
    }
}
```

### 7.11 Integração com GameEvents (a ser adicionado no Lote 1)

```csharp
// GameEvents.cs (adicionar)
public static class GameEvents
{
    // ========== EVENTOS DE RECURSOS ==========
    public static event System.Action<ResourceType, int> OnResourceChanged;
    
    // ========== MÉTODOS DE DISPARO ==========
    public static void RaiseResourceChanged(ResourceType type, int amount)
    {
        OnResourceChanged?.Invoke(type, amount);
    }
}
```

---

## 8) RESOURCEDISPLAYUI - DISPLAY DE RECURSOS

### 8.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `ResourceDisplayUI.cs` (a ser implementado)  
**Responsabilidade:** Exibe recursos (ouro, madeira, comida) no Top HUD. Escuta `GameEvents.OnResourceChanged`.

### 8.2 Estrutura Proposta

```csharp
/// <summary>
/// Display de recursos no Top HUD.
/// Escuta GameEvents.OnResourceChanged e atualiza textos.
/// </summary>
public class ResourceDisplayUI : MonoBehaviour
{
    // ========== REFS ==========
    [Header("Resource Texts")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text foodText;
    
    [Header("Icons (optional)")]
    [SerializeField] private Image goldIcon;
    [SerializeField] private Image woodIcon;
    [SerializeField] private Image foodIcon;
    
    // ========== CACHE ==========
    private int _goldAmount;
    private int _woodAmount;
    private int _foodAmount;
    
    // ========== MÉTODOS ==========
    void OnEnable() { /* subscribe */ }
    void OnDisable() { /* unsubscribe */ }
    void Start() { /* sincronização inicial */ }
    void OnResourceChanged(ResourceType type, int amount) { /* handler */ }
    void UpdateDisplay(ResourceType type) { /* renderizar */ }
}
```

### 8.3 Subscrição de Eventos

```csharp
void OnEnable()
{
    GameEvents.OnResourceChanged += OnResourceChanged;
}

void OnDisable()
{
    GameEvents.OnResourceChanged -= OnResourceChanged;
}
```

### 8.4 Sincronização Inicial

```csharp
void Start()
{
    // Sincronizar com ResourceManager
    if (ResourceManager.Instance != null)
    {
        _goldAmount = ResourceManager.Instance.Gold;
        _woodAmount = ResourceManager.Instance.Wood;
        _foodAmount = ResourceManager.Instance.Food;
        
        UpdateDisplay(ResourceType.Gold);
        UpdateDisplay(ResourceType.Wood);
        UpdateDisplay(ResourceType.Food);
    }
}
```

### 8.5 Handler de Evento

```csharp
/// <summary>
/// Handler de GameEvents.OnResourceChanged.
/// Cacheia valor e atualiza display.
/// </summary>
void OnResourceChanged(ResourceType type, int amount)
{
    switch (type)
    {
        case ResourceType.Gold:
            _goldAmount = amount;
            break;
        case ResourceType.Wood:
            _woodAmount = amount;
            break;
        case ResourceType.Food:
            _foodAmount = amount;
            break;
    }
    
    UpdateDisplay(type);
}
```

### 8.6 Renderização

```csharp
/// <summary>
/// Atualiza texto do recurso específico.
/// </summary>
void UpdateDisplay(ResourceType type)
{
    switch (type)
    {
        case ResourceType.Gold:
            if (goldText) goldText.text = _goldAmount.ToString();
            break;
        case ResourceType.Wood:
            if (woodText) woodText.text = _woodAmount.ToString();
            break;
        case ResourceType.Food:
            if (foodText) foodText.text = _foodAmount.ToString();
            break;
    }
}
```

### 8.7 Setup no Inspector

```
TopHud (GameObject)
└── Resource (GameObject)
    ├── ResourceDisplayUI (MonoBehaviour)
    │   ├── goldText: Gold (TMP_Text)
    │   ├── woodText: Wood (TMP_Text)
    │   ├── foodText: Food (TMP_Text)
    │   ├── goldIcon: GoldIcon (Image) [opcional]
    │   ├── woodIcon: WoodIcon (Image) [opcional]
    │   └── foodIcon: FoodIcon (Image) [opcional]
    │
    ├── Gold (GameObject)
    │   ├── Icon (Image) → Sprite de moeda
    │   └── Text (TMP_Text) → "1000"
    │
    ├── Wood (GameObject)
    │   ├── Icon (Image) → Sprite de madeira
    │   └── Text (TMP_Text) → "500"
    │
    └── Food (GameObject)
        ├── Icon (Image) → Sprite de comida
        └── Text (TMP_Text) → "300"
```

### 8.8 Melhorias Futuras

#### Formatação com Separador de Milhares

```csharp
void UpdateDisplay(ResourceType type)
{
    switch (type)
    {
        case ResourceType.Gold:
            if (goldText) goldText.text = _goldAmount.ToString("N0"); // "1,234"
            break;
        // ... outras
    }
}
```

#### Animação de Mudança (Pulse)

```csharp
void UpdateDisplay(ResourceType type)
{
    TMP_Text text = GetTextForType(type);
    if (text == null) return;
    
    text.text = GetAmountForType(type).ToString();
    
    // Animar (pulse)
    text.transform.DOScale(1.2f, 0.1f).OnComplete(() => {
        text.transform.DOScale(1f, 0.1f);
    });
}
```

#### Feedback de Ganho/Perda (Color Tween)

```csharp
void OnResourceChanged(ResourceType type, int amount)
{
    int oldAmount = GetCachedAmount(type);
    int delta = amount - oldAmount;
    
    SetCachedAmount(type, amount);
    UpdateDisplay(type);
    
    // Feedback visual
    TMP_Text text = GetTextForType(type);
    if (text != null)
    {
        Color color = delta > 0 ? Color.green : Color.red;
        text.DOColor(color, 0.2f).OnComplete(() => {
            text.DOColor(Color.white, 0.2f);
        });
    }
}
```

---

## 9) INTEGRAÇÃO COM GAMEEVENTS

### 9.1 Diagrama de Fluxo Completo

```
[Fonte de Mudança de Recurso]
    │
    ├─→ BuildingConstructor.TryBuildBarracks()
    │       ResourceManager.TrySpendResources({Gold: 200, Wood: 150})
    │
    ├─→ ResourceCollector.OnTreeHarvested()
    │       ResourceManager.AddResource(Wood, 10)
    │
    └─→ Farm.ProduceFood() (passive income)
            ResourceManager.AddResource(Food, 5)
    │
    ↓ (ResourceManager.SetResource)
GameEvents.RaiseResourceChanged(type, amount)
    │
    ↓ (Event Bus)
┌───────────────────────────────────────────┐
│         GameEvents.OnResourceChanged       │
└───────────────┬───────────────────────────┘
                │
    ┌───────────┼───────────┬────────────┐
    ↓           ↓           ↓            ↓
┌──────────┐ ┌──────┐ ┌─────────┐ ┌──────────┐
│Resource  │ │Audio │ │Analytics│ │[Futuro]  │
│DisplayUI │ │FX    │ │Tracker  │ │Tutorial  │
└──────────┘ └──────┘ └─────────┘ └──────────┘
    │
    └─→ UpdateDisplay(type)
            goldText.text = "1500"
```

### 9.2 Exemplo Completo de Integração

**1. Jogador clica em "Build Barracks":**

```csharp
// BuildingConstructor.cs
public void OnBarracksButtonClicked()
{
    var cost = new Dictionary<ResourceType, int>
    {
        { ResourceType.Gold, 200 },
        { ResourceType.Wood, 150 }
    };
    
    if (ResourceManager.Instance.TrySpendResources(cost))
    {
        // Sucesso: construir
        ConstructBuilding(BuildingType.Barracks);
    }
    else
    {
        // Falha: feedback
        ShowInsufficientResourcesUI();
    }
}
```

**2. ResourceManager gasta recursos:**

```csharp
// ResourceManager.cs
public bool TrySpendResources(Dictionary<ResourceType, int> cost)
{
    if (!HasResources(cost)) return false;
    
    // Gold: 1000 → 800
    SetResource(ResourceType.Gold, 800);
    // → GameEvents.RaiseResourceChanged(Gold, 800)
    
    // Wood: 500 → 350
    SetResource(ResourceType.Wood, 350);
    // → GameEvents.RaiseResourceChanged(Wood, 350)
    
    return true;
}
```

**3. ResourceDisplayUI escuta evento:**

```csharp
// ResourceDisplayUI.cs
void OnResourceChanged(ResourceType type, int amount)
{
    switch (type)
    {
        case ResourceType.Gold:
            _goldAmount = amount; // 800
            goldText.text = "800"; // Atualiza UI
            break;
        case ResourceType.Wood:
            _woodAmount = amount; // 350
            woodText.text = "350";
            break;
    }
}
```

**4. Outros sistemas também reagem:**

```csharp
// AudioManager.cs (opcional)
void OnResourceChanged(ResourceType type, int amount)
{
    // Tocar som de moedas/madeira
    PlayResourceSound(type);
}

// AnalyticsTracker.cs (opcional)
void OnResourceChanged(ResourceType type, int amount)
{
    // Enviar para analytics
    TrackResourceChange(type, amount);
}
```

---

# PARTE IV: TOP HUD (RANKING & CHAT - ARQUITETURA FUTURA)

---

## 10) RANKINGSYSTEM - SISTEMA DE RANKING

### 10.1 Visão Geral

**Tipo:** `MonoBehaviour` (Singleton)  
**Arquivo:** `RankingSystem.cs` (a ser implementado)  
**Responsabilidade:** Gerencia ranking de jogadores/facções. Calcula pontuação baseada em métricas (unidades, construções, recursos).

### 10.2 Estrutura Proposta

```csharp
/// <summary>
/// Sistema de ranking multiplayer/singleplayer.
/// Calcula pontuação e dispara eventos de mudança.
/// </summary>
public class RankingSystem : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static RankingSystem Instance { get; private set; }
    
    // ========== CONFIGURAÇÃO ==========
    [Header("Score Weights")]
    [SerializeField] private int pointsPerUnit = 10;
    [SerializeField] private int pointsPerBuilding = 50;
    [SerializeField] private int pointsPerResource = 1;
    
    // ========== ESTADO ==========
    private List<RankEntry> _rankings = new();
    
    // ========== MÉTODOS ==========
    void Awake() { /* singleton */ }
    void Start() { /* inicializar */ }
    public void UpdateRanking() { /* recalcular */ }
    int CalculateScore(FactionId faction) { /* cálculo */ }
}

/// <summary>
/// Entrada de ranking (jogador/facção).
/// </summary>
[System.Serializable]
public class RankEntry
{
    public FactionId faction;
    public string playerName;
    public int score;
    public int rank; // 1st, 2nd, 3rd...
}
```

### 10.3 Cálculo de Pontuação

```csharp
/// <summary>
/// Calcula pontuação de uma facção.
/// Score = (Units * 10) + (Buildings * 50) + (Resources / 100)
/// </summary>
int CalculateScore(FactionId faction)
{
    int score = 0;
    
    // Unidades
    var units = UnitRegistry.GetByFaction(faction);
    score += units.Count * pointsPerUnit;
    
    // Construções (exemplo - requer BuildingRegistry)
    // var buildings = BuildingRegistry.GetByFaction(faction);
    // score += buildings.Count * pointsPerBuilding;
    
    // Recursos (se aplicável)
    if (faction == PlayerController.Instance.myFaction)
    {
        int totalResources = ResourceManager.Instance.Gold +
                             ResourceManager.Instance.Wood +
                             ResourceManager.Instance.Food;
        score += totalResources / 100; // Dividir para não dominar score
    }
    
    return score;
}
```

### 10.4 Atualização de Ranking

```csharp
/// <summary>
/// Recalcula ranking de todas as facções.
/// Dispara GameEvents.OnRankingUpdated.
/// </summary>
public void UpdateRanking()
{
    _rankings.Clear();
    
    // Para cada facção ativa
    foreach (var faction in FactionManager.GetActiveFactions())
    {
        var entry = new RankEntry
        {
            faction = faction.id,
            playerName = faction.playerName,
            score = CalculateScore(faction.id)
        };
        _rankings.Add(entry);
    }
    
    // Ordenar por pontuação (maior primeiro)
    _rankings.Sort((a, b) => b.score.CompareTo(a.score));
    
    // Atribuir ranks
    for (int i = 0; i < _rankings.Count; i++)
    {
        _rankings[i].rank = i + 1;
    }
    
    // Disparar evento
    GameEvents.RaiseRankingUpdated(_rankings);
}
```

### 10.5 Update Periódico

```csharp
[Header("Update Settings")]
[SerializeField] private float updateInterval = 10f; // Segundos
private float _timer;

void Update()
{
    _timer += Time.deltaTime;
    if (_timer >= updateInterval)
    {
        _timer = 0f;
        UpdateRanking();
    }
}
```

### 10.6 Integração com GameEvents

```csharp
// GameEvents.cs (adicionar)
public static class GameEvents
{
    // ========== EVENTOS DE RANKING ==========
    public static event System.Action<List<RankEntry>> OnRankingUpdated;
    
    // ========== MÉTODOS DE DISPARO ==========
    public static void RaiseRankingUpdated(List<RankEntry> rankings)
    {
        OnRankingUpdated?.Invoke(rankings);
    }
}
```

---

## 11) CHATSYSTEM - SISTEMA DE CHAT

### 11.1 Visão Geral

**Tipo:** `MonoBehaviour` (Singleton)  
**Arquivo:** `ChatSystem.cs` (a ser implementado)  
**Responsabilidade:** Gerencia mensagens de chat multiplayer. Envia/recebe via rede (Mirror, Netcode, etc).

### 11.2 Estrutura Proposta

```csharp
/// <summary>
/// Sistema de chat multiplayer.
/// Envia/recebe mensagens e dispara eventos.
/// </summary>
public class ChatSystem : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static ChatSystem Instance { get; private set; }
    
    // ========== CONFIGURAÇÃO ==========
    [Header("Settings")]
    [SerializeField] private int maxMessages = 100;
    
    // ========== ESTADO ==========
    private List<ChatMessage> _messages = new();
    
    // ========== MÉTODOS ==========
    void Awake() { /* singleton */ }
    public void SendMessage(string text) { /* enviar */ }
    public void ReceiveMessage(ChatMessage msg) { /* receber */ }
}

/// <summary>
/// Mensagem de chat.
/// </summary>
[System.Serializable]
public class ChatMessage
{
    public string senderName;
    public FactionId senderFaction;
    public string text;
    public float timestamp;
    
    public ChatMessage(string sender, FactionId faction, string message)
    {
        senderName = sender;
        senderFaction = faction;
        text = message;
        timestamp = Time.time;
    }
}
```

### 11.3 Enviar Mensagem

```csharp
/// <summary>
/// Envia mensagem de chat.
/// TODO: Integrar com sistema de rede (Mirror/Netcode).
/// </summary>
public void SendMessage(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return;
    
    var msg = new ChatMessage(
        PlayerController.Instance.playerName,
        PlayerController.Instance.myFaction,
        text
    );
    
    // TODO: Enviar via rede
    // NetworkServer.SendToAll(msg);
    
    // Por enquanto: apenas adicionar localmente
    ReceiveMessage(msg);
}
```

### 11.4 Receber Mensagem

```csharp
/// <summary>
/// Recebe mensagem de chat (local ou remota).
/// Dispara GameEvents.OnChatMessageReceived.
/// </summary>
public void ReceiveMessage(ChatMessage msg)
{
    _messages.Add(msg);
    
    // Limitar histórico
    if (_messages.Count > maxMessages)
    {
        _messages.RemoveAt(0);
    }
    
    // Disparar evento
    GameEvents.RaiseChatMessageReceived(msg);
}
```

### 11.5 Integração com GameEvents

```csharp
// GameEvents.cs (adicionar)
public static class GameEvents
{
    // ========== EVENTOS DE CHAT ==========
    public static event System.Action<ChatMessage> OnChatMessageReceived;
    
    // ========== MÉTODOS DE DISPARO ==========
    public static void RaiseChatMessageReceived(ChatMessage message)
    {
        OnChatMessageReceived?.Invoke(message);
    }
}
```

---

## 12) UI MODULAR (RANKING/CHAT)

### 12.1 RankingUI (Painel de Ranking)

```csharp
/// <summary>
/// Painel de ranking (toggle ao clicar botão "Ranking").
/// Exibe lista de jogadores ordenados por pontuação.
/// </summary>
public class RankingUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private RankEntryUI entryPrefab;
    
    private List<RankEntryUI> _entries = new();
    
    void OnEnable()
    {
        GameEvents.OnRankingUpdated += OnRankingUpdated;
    }
    
    void OnDisable()
    {
        GameEvents.OnRankingUpdated -= OnRankingUpdated;
    }
    
    void OnRankingUpdated(List<RankEntry> rankings)
    {
        // Limpar entries antigas
        foreach (var entry in _entries)
        {
            Destroy(entry.gameObject);
        }
        _entries.Clear();
        
        // Criar novas entries
        foreach (var rank in rankings)
        {
            var entryUI = Instantiate(entryPrefab, entryContainer);
            entryUI.SetData(rank);
            _entries.Add(entryUI);
        }
    }
    
    public void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
    }
}

/// <summary>
/// UI de uma entrada de ranking.
/// </summary>
public class RankEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;   // "#1"
    [SerializeField] private TMP_Text nameText;   // "Player1"
    [SerializeField] private TMP_Text scoreText;  // "1234"
    [SerializeField] private Image factionIcon;   // Brasão da facção
    
    public void SetData(RankEntry entry)
    {
        rankText.text = $"#{entry.rank}";
        nameText.text = entry.playerName;
        scoreText.text = entry.score.ToString();
        
        // Ícone de facção
        var factionDef = FactionDatabase.Instance.Get(entry.faction);
        if (factionDef != null && factionIcon != null)
        {
            factionIcon.sprite = factionDef.icon;
            factionIcon.color = factionDef.color;
        }
    }
}
```

### 12.2 ChatUI (Painel de Chat)

```csharp
/// <summary>
/// Painel de chat (toggle ao clicar botão "Chat").
/// Exibe histórico de mensagens e input field.
/// </summary>
public class ChatUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private ChatMessageUI messagePrefab;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    
    private List<ChatMessageUI> _messages = new();
    
    void OnEnable()
    {
        GameEvents.OnChatMessageReceived += OnMessageReceived;
    }
    
    void OnDisable()
    {
        GameEvents.OnChatMessageReceived -= OnMessageReceived;
    }
    
    void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener(OnInputSubmit);
    }
    
    void OnMessageReceived(ChatMessage msg)
    {
        var msgUI = Instantiate(messagePrefab, messageContainer);
        msgUI.SetData(msg);
        _messages.Add(msgUI);
        
        // Scroll para o final
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
    
    void OnSendClicked()
    {
        SendMessage();
    }
    
    void OnInputSubmit(string text)
    {
        SendMessage();
    }
    
    void SendMessage()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        ChatSystem.Instance.SendMessage(text);
        inputField.text = "";
        inputField.ActivateInputField();
    }
    
    public void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
        if (panelRoot.activeSelf)
        {
            inputField.ActivateInputField();
        }
    }
}

/// <summary>
/// UI de uma mensagem de chat.
/// </summary>
public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    
    public void SetData(ChatMessage msg)
    {
        // Formato: "[Player1]: Hello!"
        messageText.text = $"[{msg.senderName}]: {msg.text}";
        
        // Colorir por facção
        var factionDef = FactionDatabase.Instance.Get(msg.senderFaction);
        if (factionDef != null)
        {
            messageText.color = factionDef.color;
        }
    }
}
```

---

# PARTE V: BUTTON HUD (COMANDOS - ARQUITETURA FUTURA)

---

## 13) COMMANDCONTROLLER - CONTROLADOR DE COMANDOS

### 13.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `CommandController.cs` (a ser implementado)  
**Responsabilidade:** Centraliza comandos enviados para unidades selecionadas (Move, Attack, Defend, Hold, Patrol).

### 13.2 Arquitetura (Command Pattern)

```
┌──────────────────────────────────────────────────┐
│            CommandController (Invoker)            │
│  ┌────────────────────────────────────────┐      │
│  │ ExecuteCommand(ICommand command)       │      │
│  └────────────────────────────────────────┘      │
└────────────────────┬─────────────────────────────┘
                     │
        ┌────────────┼────────────┬────────────┐
        ↓            ↓            ↓            ↓
┌─────────────┐ ┌─────────┐ ┌─────────┐ ┌──────────┐
│ MoveCommand │ │ Attack  │ │ Defend  │ │ Patrol   │
│             │ │ Command │ │ Command │ │ Command  │
│ Execute()   │ │Execute()│ │Execute()│ │Execute() │
└─────────────┘ └─────────┘ └─────────┘ └──────────┘
        │            │            │            │
        └────────────┴────────────┴────────────┘
                     │
                     ↓
            ┌─────────────────┐
            │ Unit.ReceiveCmd │
            └─────────────────┘
```

### 13.3 Interface ICommand

```csharp
/// <summary>
/// Interface base para comandos.
/// Implementa o Command Pattern.
/// </summary>
public interface ICommand
{
    void Execute(List<Unit> units);
}
```

### 13.4 Estrutura do CommandController

```csharp
/// <summary>
/// Controlador centralizado de comandos.
/// Recebe input de botões/hotkeys e executa comandos nas unidades selecionadas.
/// </summary>
public class CommandController : MonoBehaviour
{
    // ========== REFS ==========
    [Header("Refs")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private InputSelection inputSelection;
    
    // ========== COMMANDS ==========
    private MoveCommand _moveCommand;
    private AttackCommand _attackCommand;
    private DefendCommand _defendCommand;
    private HoldCommand _holdCommand;
    
    // ========== MÉTODOS ==========
    void Awake() { /* inicializar comandos */ }
    public void ExecuteCommand(ICommand command) { /* executar */ }
    public void OrderMove(Vector3 targetPosition) { /* mover */ }
    public void OrderAttack(Unit targetUnit) { /* atacar */ }
    public void OrderDefend() { /* defender */ }
    public void OrderHold() { /* parar */ }
}
```

### 13.5 Inicialização de Comandos

```csharp
void Awake()
{
    // Instanciar comandos (reusáveis)
    _moveCommand = new MoveCommand();
    _attackCommand = new AttackCommand();
    _defendCommand = new DefendCommand();
    _holdCommand = new HoldCommand();
}
```

### 13.6 Execução de Comando

```csharp
/// <summary>
/// Executa comando nas unidades selecionadas.
/// </summary>
public void ExecuteCommand(ICommand command)
{
    if (selectionManager == null || command == null) return;
    
    var selected = selectionManager.Selection.ToList();
    if (selected.Count == 0)
    {
        Debug.Log("No units selected");
        return;
    }
    
    command.Execute(selected);
}
```

### 13.7 Comandos Específicos

#### Move Command

```csharp
/// <summary>
/// Comando de movimento.
/// </summary>
public class MoveCommand : ICommand
{
    public Vector3 TargetPosition { get; set; }
    
    public void Execute(List<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit == null) continue;
            
            // Delegar para componente de movimento
            var movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.MoveTo(TargetPosition);
            }
        }
    }
}

// Uso no CommandController
public void OrderMove(Vector3 targetPosition)
{
    _moveCommand.TargetPosition = targetPosition;
    ExecuteCommand(_moveCommand);
}
```

#### Attack Command

```csharp
/// <summary>
/// Comando de ataque.
/// </summary>
public class AttackCommand : ICommand
{
    public Unit TargetUnit { get; set; }
    
    public void Execute(List<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit == null) continue;
            
            var combat = unit.GetComponent<UnitCombat>();
            if (combat != null)
            {
                combat.AttackTarget(TargetUnit);
            }
        }
    }
}

// Uso
public void OrderAttack(Unit targetUnit)
{
    _attackCommand.TargetUnit = targetUnit;
    ExecuteCommand(_attackCommand);
}
```

#### Defend Command

```csharp
/// <summary>
/// Comando de defesa (stance defensivo).
/// </summary>
public class DefendCommand : ICommand
{
    public void Execute(List<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit == null) continue;
            
            var combat = unit.GetComponent<UnitCombat>();
            if (combat != null)
            {
                combat.SetStance(CombatStance.Defensive);
            }
        }
    }
}
```

#### Hold Command

```csharp
/// <summary>
/// Comando de parar (cancela ações atuais).
/// </summary>
public class HoldCommand : ICommand
{
    public void Execute(List<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit == null) continue;
            
            // Cancelar movimento
            var movement = unit.GetComponent<UnitMovement>();
            movement?.Stop();
            
            // Cancelar ataque
            var combat = unit.GetComponent<UnitCombat>();
            combat?.StopAttack();
        }
    }
}
```

### 13.8 Integração com Input

```csharp
void Update()
{
    // Right-click no mundo: mover
    if (Input.GetMouseButtonDown(1))
    {
        if (TryGetWorldPosition(out Vector3 worldPos))
        {
            OrderMove(worldPos);
        }
    }
    
    // Hotkeys
    if (Input.GetKeyDown(KeyCode.S)) // Stop
    {
        OrderHold();
    }
    
    if (Input.GetKeyDown(KeyCode.D)) // Defend
    {
        OrderDefend();
    }
}

bool TryGetWorldPosition(out Vector3 worldPos)
{
    worldPos = default;
    
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit))
    {
        worldPos = hit.point;
        return true;
    }
    return false;
}
```

---

## 14) UNITACTIONBUTTONS - BOTÕES DE AÇÃO

### 14.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `UnitActionButtons.cs` (a ser implementado)  
**Responsabilidade:** Gerencia botões de ação no Bottom HUD. Atualiza visuais baseado em seleção.

### 14.2 Estrutura Proposta

```csharp
/// <summary>
/// Gerencia botões de ação de unidades.
/// Atualiza visuais baseado em unidades selecionadas.
/// </summary>
public class UnitActionButtons : MonoBehaviour
{
    // ========== REFS ==========
    [Header("Command Buttons")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button holdButton;
    [SerializeField] private Button patrolButton;
    
    [Header("Refs")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CommandController commandController;
    
    // ========== MÉTODOS ==========
    void OnEnable() { /* subscribe */ }
    void OnDisable() { /* unsubscribe */ }
    void Start() { /* ligar botões */ }
    void OnSelectionChanged(IReadOnlyCollection<Unit> selection) { /* atualizar */ }
    void UpdateButtons(IReadOnlyCollection<Unit> selection) { /* visuais */ }
}
```

### 14.3 Subscrição de Eventos

```csharp
void OnEnable()
{
    GameEvents.OnSelectionChanged += OnSelectionChanged;
}

void OnDisable()
{
    GameEvents.OnSelectionChanged -= OnSelectionChanged;
}
```

### 14.4 Ligação de Botões

```csharp
void Start()
{
    if (moveButton)
        moveButton.onClick.AddListener(OnMoveButtonClicked);
    
    if (attackButton)
        attackButton.onClick.AddListener(OnAttackButtonClicked);
    
    if (defendButton)
        defendButton.onClick.AddListener(() => commandController.OrderDefend());
    
    if (holdButton)
        holdButton.onClick.AddListener(() => commandController.OrderHold());
}

void OnMoveButtonClicked()
{
    // Ativar modo "click para mover"
    // (implementação depende de sistema de input)
}

void OnAttackButtonClicked()
{
    // Ativar modo "click para atacar"
}
```

### 14.5 Atualização Baseada em Seleção

```csharp
/// <summary>
/// Atualiza botões baseado em unidades selecionadas.
/// Habilita/desabilita botões conforme capabilities.
/// </summary>
void UpdateButtons(IReadOnlyCollection<Unit> selection)
{
    if (selection == null || selection.Count == 0)
    {
        // Nenhuma unidade selecionada: desabilitar todos
        SetButtonsInteractable(false);
        return;
    }
    
    // Verificar capabilities (exemplo: se alguma unidade pode atacar)
    bool canMove = selection.Any(u => u.GetComponent<UnitMovement>() != null);
    bool canAttack = selection.Any(u => u.GetComponent<UnitCombat>() != null);
    
    // Atualizar interatividade
    if (moveButton) moveButton.interactable = canMove;
    if (attackButton) attackButton.interactable = canAttack;
    if (defendButton) defendButton.interactable = canAttack;
    if (holdButton) holdButton.interactable = true; // Sempre disponível
}

void SetButtonsInteractable(bool interactable)
{
    if (moveButton) moveButton.interactable = interactable;
    if (attackButton) attackButton.interactable = interactable;
    if (defendButton) defendButton.interactable = interactable;
    if (holdButton) holdButton.interactable = interactable;
    if (patrolButton) patrolButton.interactable = interactable;
}
```

---

## 15) GROUPHOTKEYMANAGER - ATALHOS CTRL+1~9

### 15.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `GroupHotkeyManager.cs` (a ser implementado)  
**Responsabilidade:** Gerencia atalhos de grupo (Ctrl+1~9 para criar, 1~9 para selecionar).

### 15.2 Estrutura Proposta

```csharp
/// <summary>
/// Gerencia hotkeys de grupo (Ctrl+1~9 para criar, 1~9 para selecionar).
/// Integra com UnitListPanel (grupos criados lá aparecem aqui).
/// </summary>
public class GroupHotkeyManager : MonoBehaviour
{
    // ========== REFS ==========
    [Header("Refs")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private UnitListPanel unitListPanel;
    
    // ========== GRUPOS ==========
    private Dictionary<int, UnitGroup> _hotkeyGroups = new(); // Key: 1-9
    
    // ========== MÉTODOS ==========
    void OnEnable() { /* subscribe */ }
    void OnDisable() { /* unsubscribe */ }
    void Update() { /* input */ }
    void OnGroupCreated(UnitGroup group) { /* handler */ }
    void AssignGroupToHotkey(int key, UnitGroup group) { /* atribuir */ }
    void SelectGroup(int key) { /* selecionar */ }
}
```

### 15.3 Subscrição de Eventos

```csharp
void OnEnable()
{
    GameEvents.OnGroupCreated += OnGroupCreated;
    GameEvents.OnGroupDeleted += OnGroupDeleted;
}

void OnDisable()
{
    GameEvents.OnGroupCreated -= OnGroupCreated;
    GameEvents.OnGroupDeleted -= OnGroupDeleted;
}
```

### 15.4 Handler de Criação de Grupo

```csharp
/// <summary>
/// Handler de GameEvents.OnGroupCreated.
/// Atribui grupo ao próximo slot de hotkey disponível.
/// </summary>
void OnGroupCreated(UnitGroup group)
{
    // Encontrar slot vazio (1-9)
    for (int i = 1; i <= 9; i++)
    {
        if (!_hotkeyGroups.ContainsKey(i))
        {
            AssignGroupToHotkey(i, group);
            Debug.Log($"Group '{group.GroupName}' assigned to hotkey {i}");
            return;
        }
    }
    
    Debug.LogWarning("All hotkey slots (1-9) are full!");
}

void OnGroupDeleted(UnitGroup group)
{
    // Remover grupo dos hotkeys
    var key = _hotkeyGroups.FirstOrDefault(x => x.Value == group).Key;
    if (key != 0)
    {
        _hotkeyGroups.Remove(key);
        Debug.Log($"Group removed from hotkey {key}");
    }
}
```

### 15.5 Input de Hotkeys

```csharp
void Update()
{
    // Ctrl+1~9: Atribuir seleção atual ao hotkey
    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
    {
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                AssignSelectionToHotkey(i);
                return;
            }
        }
    }
    // 1~9: Selecionar grupo
    else
    {
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SelectGroup(i);
                return;
            }
        }
    }
    
    // Duplo clique no número: centralizar câmera no grupo
    // (implementação depende de detecção de double-click)
}
```

### 15.6 Atribuir Seleção a Hotkey

```csharp
/// <summary>
/// Ctrl+N: Atribui seleção atual ao hotkey N.
/// Cria grupo se necessário.
/// </summary>
void AssignSelectionToHotkey(int key)
{
    if (selectionManager == null) return;
    
    var selected = selectionManager.Selection.ToList();
    if (selected.Count == 0)
    {
        Debug.Log("No units selected");
        return;
    }
    
    // Verificar se já existe grupo neste hotkey
    if (_hotkeyGroups.TryGetValue(key, out UnitGroup existingGroup))
    {
        // Atualizar grupo existente
        existingGroup.Units.Clear();
        existingGroup.Units.AddRange(selected);
        Debug.Log($"Group hotkey {key} updated with {selected.Count} units");
    }
    else
    {
        // Criar novo grupo
        var newGroup = new UnitGroup
        {
            GroupName = $"Group {key}",
            Units = selected
        };
        
        _hotkeyGroups[key] = newGroup;
        
        // Disparar evento (opcional: adicionar à lista)
        GameEvents.RaiseGroupCreated(newGroup);
        
        Debug.Log($"Group hotkey {key} created with {selected.Count} units");
    }
}
```

### 15.7 Selecionar Grupo por Hotkey

```csharp
/// <summary>
/// N: Seleciona grupo atribuído ao hotkey N.
/// </summary>
void SelectGroup(int key)
{
    if (!_hotkeyGroups.TryGetValue(key, out UnitGroup group))
    {
        Debug.Log($"No group assigned to hotkey {key}");
        return;
    }
    
    // Filtrar unidades que ainda existem
    var validUnits = group.Units.Where(u => u != null).ToList();
    
    if (validUnits.Count == 0)
    {
        Debug.Log($"Group hotkey {key} has no valid units");
        return;
    }
    
    // Selecionar unidades
    selectionManager.SelectExactly(validUnits);
    
    Debug.Log($"Selected {validUnits.Count} units from group hotkey {key}");
}
```

---

## 16) INTEGRAÇÃO COM SELECTIONMANAGER

### 16.1 Diagrama de Fluxo

```
[Usuário Pressiona Ctrl+2]
    │
    ↓
GroupHotkeyManager.AssignSelectionToHotkey(2)
    │
    ├─→ Obter unidades selecionadas (SelectionManager.Selection)
    │
    ├─→ Criar/Atualizar UnitGroup
    │
    ├─→ Armazenar em _hotkeyGroups[2]
    │
    └─→ GameEvents.RaiseGroupCreated(group)
            │
            ↓
        [UnitListPanel.HandleGroupCreated]
            │
            └─→ Adiciona grupo à lista visual

---

[Usuário Pressiona 2]
    │
    ↓
GroupHotkeyManager.SelectGroup(2)
    │
    ├─→ Obter grupo de _hotkeyGroups[2]
    │
    ├─→ Filtrar unidades válidas
    │
    └─→ SelectionManager.SelectExactly(units)
            │
            ↓
        GameEvents.RaiseSelectionChanged(selection)
            │
            ↓
        [UnitListPanel.RefreshFromSelection]
        [UnitActionButtons.UpdateButtons]
        [Outros sistemas...]
```

### 16.2 Sincronização Bidirecional

**1. Grupo criado no Left Bar → Aparece em Hotkey:**

```csharp
// UnitListPanel.CreateNewGroup()
public void CreateNewGroup(string groupName)
{
    // ... criar grupo ...
    
    GameEvents.RaiseGroupCreated(newGroup);
    // → GroupHotkeyManager.OnGroupCreated(newGroup)
    //   → Atribui ao próximo slot livre (1-9)
}
```

**2. Hotkey criado → Aparece no Left Bar:**

```csharp
// GroupHotkeyManager.AssignSelectionToHotkey()
void AssignSelectionToHotkey(int key)
{
    // ... criar grupo ...
    
    GameEvents.RaiseGroupCreated(newGroup);
    // → UnitListPanel.HandleGroupCreated(newGroup) (se implementado)
    //   → Adiciona à lista visual
}
```

---

# PARTE VI: MINIMAPA

---

## 17) MINIMAPCONTROLLER - CONTROLADOR PRINCIPAL

### 17.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `MinimapController.cs`  
**Implementa:** `IPointerClickHandler`, `IScrollHandler`  
**Responsabilidade:** Controlador completo do minimapa. Gerencia:
- Ícones de unidades (pooling)
- Zoom independente
- Click-to-move (Go To)
- Sincronização com câmera principal
- Culling e fade de ícones

### 17.2 Estrutura da Classe

```csharp
/// <summary>
/// Controlador completo do minimapa.
/// REFATORADO: Usa GameEvents.OnUnitSpawned/OnUnitDespawned.
/// </summary>
public class MinimapController : MonoBehaviour, IPointerClickHandler, IScrollHandler
{
    // ========== REFS ==========
    [Header("Refs")]
    public RawImage minimapImage;                 // RawImage do minimapa
    public RectTransform iconsRoot;               // Container de ícones
    public RectTransform iconPrefab;              // Prefab de ícone
    public Camera minimapCamera;                  // Câmera ortográfica do minimapa
    public RTSCameraCinemachineV3Controller rtsCamera; // Câmera principal
    public FactionDatabase factionDb;             // Para cores de facção
    
    // ========== MAPA ==========
    [Header("Mapa (bounds)")]
    public Vector2 boundsCenter = Vector2.zero;   // Centro do mapa (x,z)
    public Vector2 boundsSize = new Vector2(200, 200); // Tamanho do mapa
    
    // ========== APARÊNCIA ==========
    [Header("Aparência")]
    public Vector2 iconSize = new Vector2(8, 8);
    public bool clampIconsInside = true;
    
    // ========== FOLLOW MAIN VIEW ==========
    [Header("Follow Main View")]
    public bool followMainView = true;
    public Camera mainCamera;                     // Main Camera (com Brain)
    public LayerMask groundMask = ~0;             // Camadas de chão
    public float groundY = 0f;                    // Altura do plano
    public float viewPadding = 1.2f;              // Padding de visão (120%)
    
    // ========== CULLING ==========
    [Header("Culling de Ícones")]
    public bool hideIconsOutside = true;
    [Range(0f, 0.1f)] public float uvBorderTolerance = 0.0f;
    public bool fadeNearBorder = false;
    public float fadeWidthUV = 0.03f;
    
    // ========== ZOOM ==========
    [Header("Minimap Zoom")]
    public float baseWorldHeight = 150f;          // Altura base (world units)
    public float minZoom = 0.5f;                  // Zoom mínimo
    public float maxZoom = 3.0f;                  // Zoom máximo
    public float zoom = 1.0f;                     // Zoom atual
    public float zoomStepButtons = 0.15f;         // Step dos botões +/-
    public float zoomScrollSensitivity = 0.2f;    // Sensibilidade do scroll
    
    // ========== POOLING ==========
    readonly Dictionary<Unit, RectTransform> _icons = new();
    readonly Stack<RectTransform> _pool = new();
    
    // ========== MÉTODOS ==========
    void OnEnable() { /* subscribe */ }
    void OnDisable() { /* unsubscribe */ }
    void Update() { /* atualizar */ }
    void HandleSpawn(Unit u) { /* criar ícone */ }
    void HandleDespawn(Unit u) { /* remover ícone */ }
    void UpdateIcons() { /* posicionar ícones */ }
    public void OnPointerClick(PointerEventData e) { /* click-to-move */ }
    public void OnScroll(PointerEventData e) { /* zoom */ }
    public void ZoomIn() { /* zoom in */ }
    public void ZoomOut() { /* zoom out */ }
    void SyncMinimapCamera() { /* sincronizar câmera */ }
}
```

### 17.3 Subscrição de Eventos

```csharp
void OnEnable()
{
    // REFATORAÇÃO: Usar GameEvents em vez de UnitRegistry
    GameEvents.OnUnitSpawned += HandleSpawn;
    GameEvents.OnUnitDespawned += HandleDespawn;
    
    RebuildAll();
    SyncMinimapCamera();
}

void OnDisable()
{
    // REFATORAÇÃO: Desinscrever do GameEvents
    GameEvents.OnUnitSpawned -= HandleSpawn;
    GameEvents.OnUnitDespawned -= HandleDespawn;
    
    ClearAll();
}
```

### 17.4 Rebuild Inicial

```csharp
/// <summary>
/// Popula minimapa com unidades existentes (ao ativar).
/// </summary>
void RebuildAll()
{
    ClearAll();
    
    // Criar ícone para cada unidade do registry
    foreach (var u in UnitRegistry.All)
    {
        CreateIcon(u);
    }
}

void ClearAll()
{
    // Retornar todos os ícones ao pool
    foreach (var kv in _icons)
    {
        ReturnIcon(kv.Value);
    }
    _icons.Clear();
}
```

### 17.5 Update (Follow Main View)

```csharp
void Update()
{
    // Seguir câmera principal (se habilitado)
    if (followMainView && mainCamera != null && minimapCamera != null)
    {
        if (TryGetMainCenterOnGround(out var centerXZ))
        {
            // Atualizar centro do mapa
            boundsCenter = centerXZ;
            
            // Tamanho vem do zoom (não da câmera principal)
            float worldHeight = Mathf.Max(5f, baseWorldHeight * zoom);
            float aspect = (float)minimapCamera.pixelWidth / Mathf.Max(1, minimapCamera.pixelHeight);
            float worldWidth = worldHeight * aspect;
            
            boundsSize = new Vector2(worldWidth, worldHeight);
            
            // Sincronizar câmera do minimapa
            SyncMinimapCamera();
        }
    }
    
    // Atualizar posição de ícones
    UpdateIcons();
}
```

**Explicação:**
- `followMainView = true`: Minimapa acompanha o centro da câmera principal
- `boundsCenter`: Centro do mapa segue o centro da câmera
- `boundsSize`: Tamanho do mapa baseado em zoom (não na câmera principal)
- `SyncMinimapCamera()`: Posiciona câmera ortográfica do minimapa

### 17.6 Obter Centro da Câmera Principal

```csharp
/// <summary>
/// Obtém centro da câmera principal (raycast no chão).
/// </summary>
bool TryGetMainCenterOnGround(out Vector2 centerXZ)
{
    centerXZ = default;
    if (mainCamera == null) return false;
    
    // Ray do centro da tela (viewport 0.5, 0.5)
    var ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    
    // Raycast no chão
    if (Physics.Raycast(ray, out var hit, 50000f, groundMask, QueryTriggerInteraction.Ignore))
    {
        centerXZ = new Vector2(hit.point.x, hit.point.z);
        return true;
    }
    
    // Fallback: usar plano Y=groundY
    var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
    if (plane.Raycast(ray, out float t))
    {
        var p = ray.GetPoint(t);
        centerXZ = new Vector2(p.x, p.z);
        return true;
    }
    
    return false;
}
```

### 17.7 Sincronização da Câmera do Minimapa

```csharp
/// <summary>
/// Posiciona e configura câmera ortográfica do minimapa.
/// </summary>
void SyncMinimapCamera()
{
    if (!minimapCamera) return;
    
    // Posição (acima do centro do mapa)
    minimapCamera.transform.position = new Vector3(
        boundsCenter.x,
        minimapCamera.transform.position.y, // Manter altura
        boundsCenter.y
    );
    
    // Rotação (olhando para baixo)
    minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    
    // Modo ortográfico
    minimapCamera.orthographic = true;
    
    // Orthographic size = metade da ALTURA visível (em world units)
    minimapCamera.orthographicSize = Mathf.Max(1f, boundsSize.y * 0.5f);
}
```

**Explicação:**
```
boundsSize.y = 150 (world units de altura)
orthographicSize = 150 / 2 = 75

Câmera ortográfica renderiza:
- Altura: 150 units (de -75 a +75)
- Largura: 150 * aspect (ex: 150 * 1.0 = 150)
```

---

## 18) SISTEMA DE ÍCONES E POOL

### 18.1 Criação de Ícone

```csharp
/// <summary>
/// Cria ícone para uma unidade.
/// Usa pooling para performance.
/// </summary>
void CreateIcon(Unit u)
{
    if (u == null || _icons.ContainsKey(u)) return;
    
    // Obter ícone do pool (ou instanciar)
    var rt = GetIcon();
    rt.sizeDelta = iconSize;
    
    // Cor por facção
    if (factionDb != null)
    {
        var def = factionDb.Get(u.owner);
        var img = rt.GetComponent<Image>();
        if (def != null && img != null)
        {
            img.color = def.color;
        }
    }
    
    _icons[u] = rt;
}
```

### 18.2 Remoção de Ícone

```csharp
/// <summary>
/// Remove ícone de uma unidade.
/// Retorna ao pool.
/// </summary>
void RemoveIcon(Unit u)
{
    if (u == null) return;
    
    if (_icons.TryGetValue(u, out var rt))
    {
        _icons.Remove(u);
        ReturnIcon(rt);
    }
}
```

### 18.3 Pooling

```csharp
/// <summary>
/// Obtém ícone do pool (ou instancia novo).
/// </summary>
RectTransform GetIcon()
{
    RectTransform rt;
    
    if (_pool.Count > 0)
    {
        // Reusar do pool
        rt = _pool.Pop();
    }
    else
    {
        // Instanciar novo
        rt = Instantiate(iconPrefab, iconsRoot);
    }
    
    rt.gameObject.SetActive(true);
    rt.SetAsLastSibling();
    return rt;
}

/// <summary>
/// Retorna ícone ao pool.
/// </summary>
void ReturnIcon(RectTransform rt)
{
    if (!rt) return;
    
    rt.gameObject.SetActive(false);
    rt.SetParent(iconsRoot, false);
    _pool.Push(rt);
}
```

**Benefícios do Pooling:**
- ✅ Evita `Instantiate()` constante (caro)
- ✅ Evita `Destroy()` constante (gera garbage)
- ✅ Melhora performance em larga escala (100+ unidades)

### 18.4 Atualização de Ícones

```csharp
/// <summary>
/// Atualiza posição de todos os ícones a cada frame.
/// Aplica culling e fade nas bordas.
/// </summary>
void UpdateIcons()
{
    if (!iconsRoot || minimapCamera == null) return;
    
    var rect = iconsRoot.rect;
    
    foreach (var kv in _icons)
    {
        var u = kv.Key;
        var rt = kv.Value;
        
        if (u == null)
        {
            ReturnIcon(rt);
            continue;
        }
        
        // Converter posição world → viewport (0..1)
        Vector3 vp = minimapCamera.WorldToViewportPoint(u.transform.position);
        
        // Verificar se está fora do minimapa
        bool outside =
            vp.z < 0f ||  // Atrás da câmera
            vp.x < -uvBorderTolerance || vp.x > 1f + uvBorderTolerance ||
            vp.y < -uvBorderTolerance || vp.y > 1f + uvBorderTolerance;
        
        // Esconder se estiver fora (e culling habilitado)
        if (hideIconsOutside && outside)
        {
            if (rt.gameObject.activeSelf)
                rt.gameObject.SetActive(false);
            continue;
        }
        else if (!rt.gameObject.activeSelf)
        {
            rt.gameObject.SetActive(true);
        }
        
        // Clampar viewport 0..1
        float uNorm = Mathf.Clamp01(vp.x);
        float vNorm = Mathf.Clamp01(vp.y);
        
        // Converter viewport → anchoredPosition
        Vector2 anchored = new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, uNorm),
            Mathf.Lerp(rect.yMin, rect.yMax, vNorm)
        );
        
        rt.anchoredPosition = anchored;
        
        // Fade nas bordas (opcional)
        if (fadeNearBorder)
        {
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                // Distância da borda mais próxima
                float edge = Mathf.Min(uNorm, 1f - uNorm, vNorm, 1f - vNorm);
                
                // Alpha baseado em distância
                float a = Mathf.Clamp01(edge / Mathf.Max(0.0001f, fadeWidthUV));
                
                var c = img.color;
                c.a = a;
                img.color = c;
            }
        }
    }
}
```

**Explicação do Cálculo:**

```
Unit position (world): (100, 0, 50)
MinimapCamera: viewport (0.6, 0.4, 5)

Normalizar:
uNorm = Clamp01(0.6) = 0.6
vNorm = Clamp01(0.4) = 0.4

IconsRoot.rect: (-100, -100, 100, 100) [200x200]

Anchored position:
x = Lerp(-100, 100, 0.6) = 20
y = Lerp(-100, 100, 0.4) = -20

→ Ícone aparece em (20, -20) dentro do minimapa
```

---

## 19) ZOOM E NAVEGAÇÃO

### 19.1 Zoom por Scroll

```csharp
/// <summary>
/// Handler de scroll do mouse (IScrollHandler).
/// Scroll up = zoom in, scroll down = zoom out.
/// </summary>
public void OnScroll(PointerEventData eventData)
{
    // Scroll up (delta.y > 0) = aproximar (diminuir zoom)
    // Scroll down (delta.y < 0) = afastar (aumentar zoom)
    float delta = -eventData.scrollDelta.y * zoomScrollSensitivity;
    
    SetZoom(zoom + delta);
}
```

### 19.2 Zoom por Botões

```csharp
/// <summary>
/// Botão "+" (aproximar).
/// </summary>
public void ZoomIn()
{
    SetZoom(zoom - zoomStepButtons);
}

/// <summary>
/// Botão "-" (afastar).
/// </summary>
public void ZoomOut()
{
    SetZoom(zoom + zoomStepButtons);
}
```

### 19.3 Aplicar Zoom

```csharp
/// <summary>
/// Define zoom e sincroniza câmera do minimapa.
/// </summary>
void SetZoom(float z)
{
    zoom = Mathf.Clamp(z, minZoom, maxZoom);
    
    // Forçar sync imediato (Update também fará)
    SyncMinimapCamera();
}
```

**Explicação:**

```
baseWorldHeight = 150
zoom = 1.0 → worldHeight = 150 (padrão)
zoom = 0.5 → worldHeight = 75  (mais próximo)
zoom = 3.0 → worldHeight = 450 (mais longe)

orthographicSize = worldHeight / 2
zoom = 0.5 → orthSize = 37.5 (vê menos área)
zoom = 3.0 → orthSize = 225  (vê mais área)
```

### 19.4 Gizmos (Debug)

```csharp
/// <summary>
/// Desenha bounds do minimapa no Scene View.
/// </summary>
void OnDrawGizmosSelected()
{
    Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
    
    var center = new Vector3(boundsCenter.x, groundY, boundsCenter.y);
    var size = new Vector3(boundsSize.x, 0.1f, boundsSize.y);
    
    Gizmos.DrawCube(center, size);
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(center, size);
}
```

---

## 20) CLICK-TO-MOVE (GO TO)

### 20.1 Handler de Clique

```csharp
/// <summary>
/// Handler de clique no minimapa (IPointerClickHandler).
/// Left/Right click: move câmera principal para posição clicada.
/// </summary>
public void OnPointerClick(PointerEventData e)
{
    // Aceitar left ou right click
    if (e.button != PointerEventData.InputButton.Left &&
        e.button != PointerEventData.InputButton.Right)
        return;
    
    if (!minimapImage || !rtsCamera || minimapCamera == null)
        return;
    
    // 1. Converter clique (screenPos) → local (dentro do RawImage)
    var local = ScreenToLocal(minimapImage.rectTransform, e.position, e.pressEventCamera);
    
    // 2. Converter local → viewport (0..1)
    var rect = minimapImage.rectTransform.rect;
    float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
    float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
    
    // 3. Ray a partir da MinimapCamera
    Ray ray = minimapCamera.ViewportPointToRay(new Vector3(u, v, 0f));
    
    // 4. Raycast no chão (ou usar plano fallback)
    Vector3 world;
    if (Physics.Raycast(ray, out var hit, 50000f, groundMask, QueryTriggerInteraction.Ignore))
    {
        world = hit.point;
    }
    else
    {
        // Fallback: plano Y=groundY
        var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        if (!plane.Raycast(ray, out float t)) return;
        world = ray.GetPoint(t);
    }
    
    // 5. Mover câmera principal para posição clicada
    rtsCamera.GoToXZ(new Vector2(world.x, world.z), snap: false, duration: 0.35f);
}
```

**Explicação:**

```
Usuário clica em (800, 300) (screenPos)
    │
    ↓ (ScreenToLocal)
Local dentro do RawImage: (50, -20)
    │
    ↓ (InverseLerp)
Viewport (0..1): u=0.6, v=0.4
    │
    ↓ (ViewportPointToRay)
Ray da MinimapCamera
    │
    ↓ (Raycast)
World position: (120, 0, 80)
    │
    ↓ (GoToXZ)
RTSCamera move para (120, 80)
```

### 20.2 Conversão Screen → Local

```csharp
/// <summary>
/// Converte posição de tela para local dentro de um RectTransform.
/// </summary>
Vector2 ScreenToLocal(RectTransform rt, Vector2 screenPos, Camera uiCam)
{
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        rt, screenPos, uiCam, out var local
    );
    return local;
}
```

### 20.3 Integração com RTSCamera

**RTSCameraCinemachineV3Controller.cs (Lote 2 - resumo):**

```csharp
/// <summary>
/// Move câmera para posição XZ (mapa 2D).
/// </summary>
public void GoToXZ(Vector2 targetXZ, bool snap, float duration)
{
    if (snap)
    {
        // Teleportar imediatamente
        transform.position = new Vector3(targetXZ.x, transform.position.y, targetXZ.y);
    }
    else
    {
        // Animar movimento
        DOTween.To(
            () => new Vector2(transform.position.x, transform.position.z),
            v => transform.position = new Vector3(v.x, transform.position.y, v.y),
            targetXZ,
            duration
        ).SetEase(Ease.OutCubic);
    }
}
```

---

## 21) DISABLEMINIMAPSHADOWS - OTIMIZAÇÃO

### 21.1 Visão Geral

**Tipo:** `MonoBehaviour`  
**Arquivo:** `DisableMinimapShadows.cs`  
**Responsabilidade:** Desabilita sombras em objetos renderizados pela câmera do minimapa (otimização).

### 21.2 Código Completo

```csharp
using UnityEngine;

/// <summary>
/// Desabilita sombras em objetos vistos pela câmera do minimapa.
/// Otimização: sombras não são necessárias no minimapa.
/// </summary>
public class DisableMinimapShadows : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera minimapCamera;
    
    void Start()
    {
        if (minimapCamera == null)
        {
            minimapCamera = GetComponent<Camera>();
        }
        
        if (minimapCamera != null)
        {
            // Desabilitar sombras para esta câmera
            minimapCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("ShadowsOnly"));
            
            // Ou, se quiser desabilitar globalmente para esta câmera:
            // minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            // minimapCamera.backgroundColor = Color.black;
            
            Debug.Log("Minimap shadows disabled");
        }
    }
}
```

### 21.3 Alternativa: Desabilitar via Quality Settings

```csharp
void Start()
{
    if (minimapCamera != null)
    {
        // Desabilitar todas as sombras para esta câmera
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f); // Cinza escuro
        
        // Ou criar layer "NoShadows" e colocar objetos do minimapa nele
    }
}
```

### 21.4 Setup Recomendado

**Opção 1: Culling Mask**
```
MinimapCamera
├── Culling Mask: Everything EXCEPT ShadowsOnly
└── Clear Flags: Solid Color (preto)
```

**Opção 2: Layer Separado**
```
1. Criar layer "Minimap"
2. Colocar objetos visíveis no minimapa neste layer
3. MinimapCamera.cullingMask = (1 << LayerMask.NameToLayer("Minimap"))
```

---

# PARTE VII: INTEGRAÇÃO E PATTERNS

---

## 22) FACTIONDATABASE - SISTEMA DE FACÇÕES

### 22.1 Visão Geral

**Tipo:** `ScriptableObject`  
**Arquivo:** `FactionDatabase.cs`  
**Responsabilidade:** Database de facções (cores, ícones, nomes). Usado pelo minimapa e outros sistemas.

### 22.2 Estrutura Completa

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database de facções (ScriptableObject).
/// Armazena definições de todas as facções do jogo.
/// </summary>
[CreateAssetMenu(fileName = "FactionDatabase", menuName = "Game/Faction Database")]
public class FactionDatabase : ScriptableObject
{
    public List<FactionDefinition> factions = new();
    
    /// <summary>
    /// Obtém definição de uma facção por ID.
    /// </summary>
    public FactionDefinition Get(FactionId id)
    {
        return factions.Find(f => f.id == id);
    }
}

/// <summary>
/// Definição de uma facção.
/// </summary>
[System.Serializable]
public class FactionDefinition
{
    public FactionId id;           // Enum ID
    public string displayName;     // Nome exibido
    public Color color;            // Cor da facção (minimapa, UI)
    public Sprite icon;            // Ícone/brasão da facção
    public Sprite banner;          // Banner (opcional)
}

/// <summary>
/// IDs de facções.
/// </summary>
public enum FactionId
{
    None = 0,
    Player1 = 1,
    Player2 = 2,
    Player3 = 3,
    Player4 = 4,
    Neutral = 99,
    Enemy = 100
}
```

### 22.3 Uso no Minimapa

```csharp
// MinimapController.CreateIcon()
void CreateIcon(Unit u)
{
    // ... criar ícone ...
    
    // Cor por facção
    if (factionDb != null)
    {
        var def = factionDb.Get(u.owner);
        var img = rt.GetComponent<Image>();
        if (def != null && img != null)
        {
            img.color = def.color; // Verde para Player1, Vermelho para Enemy, etc
        }
    }
}
```

### 22.4 Exemplo de Configuração

**FactionDatabase (ScriptableObject):**

```
FactionDatabase
├── factions[0]: Player1
│   ├── id: Player1
│   ├── displayName: "Kingdom of Elaria"
│   ├── color: (0.2, 0.8, 0.2, 1) → Verde
│   ├── icon: ElariaCrest.png
│   └── banner: ElariaBanner.png
│
├── factions[1]: Player2
│   ├── id: Player2
│   ├── displayName: "Empire of Drakonor"
│   ├── color: (0.8, 0.2, 0.2, 1) → Vermelho
│   └── ...
│
└── factions[2]: Neutral
    ├── id: Neutral
    ├── displayName: "Neutral Forces"
    ├── color: (0.7, 0.7, 0.7, 1) → Cinza
    └── ...
```

---

## 23) EVENT BUS INTEGRATION

### 23.1 Eventos Utilizados (Resumo Completo)

| Evento | Emissor | Listeners | Payload |
|--------|---------|-----------|---------|
| `OnTimeOfDay` | TimeManager | LightingController, SkyboxController, NPCs | `float time01` |
| `OnDayChanged` | TimeManager | Farm (produção), Quest (prazo) | `int dayCount` |
| `OnClockChanged` | TimeManager | ClockUI | `int day, int hour, int minute` |
| `OnUnitSpawned` | UnitRegistry | MinimapController, UnitListPanel | `Unit unit` |
| `OnUnitDespawned` | UnitRegistry | MinimapController, UnitListPanel | `Unit unit` |
| `OnSelectionChanged` | SelectionManager | UnitListPanel, UnitActionButtons | `IReadOnlyCollection<Unit>` |
| `OnResourceChanged` | ResourceManager (futuro) | ResourceDisplayUI | `ResourceType, int amount` |
| `OnRankingUpdated` | RankingSystem (futuro) | RankingUI | `List<RankEntry>` |
| `OnChatMessageReceived` | ChatSystem (futuro) | ChatUI | `ChatMessage` |
| `OnGroupCreated` | UnitListPanel | GroupHotkeyManager | `UnitGroup` |
| `OnGroupDeleted` | UnitListPanel | GroupHotkeyManager | `UnitGroup` |

### 23.2 Diagrama de Event Bus Completo

```
┌──────────────────────────────────────────────────────────┐
│                   GameEvents (Event Bus)                  │
│  ┌────────────────────────────────────────────────┐      │
│  │ public static event Action<...> OnXXX;         │      │
│  │ public static void RaiseXXX(...) { ... }       │      │
│  └────────────────────────────────────────────────┘      │
└────────────────┬─────────────────────────────────────────┘
                 │
    ┌────────────┼────────────┬───────────────┬────────────┐
    │            │            │               │            │
    ↓            ↓            ↓               ↓            ↓
┌─────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐
│TimeMan  │ │UnitReg  │ │Selection │ │Resource  │ │Ranking │
│ager     │ │istry    │ │Manager   │ │Manager   │ │System  │
└────┬────┘ └────┬────┘ └────┬─────┘ └────┬─────┘ └────┬───┘
     │           │           │            │            │
     ↓           ↓           ↓            ↓            ↓
┌─────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐
│ClockUI  │ │Minimap  │ │UnitList  │ │Resource  │ │Ranking │
│         │ │Ctrl     │ │Panel     │ │Display   │ │UI      │
└─────────┘ └─────────┘ └──────────┘ └──────────┘ └────────┘
```

---

## 24) FLUXO DE INICIALIZAÇÃO COMPLETO

### 24.1 Sequência de Startup

```
1. Unity Scene Load
   │
2. Awake() de todos os MonoBehaviours
   ├─→ TimeManager.Awake()
   │   └─→ Inicializar Time01 com startTime01
   │
   ├─→ MinimapController.Awake() (se houver)
   │
   └─→ ClockUI.Awake() (se houver)
   │
3. OnEnable() de todos os MonoBehaviours
   ├─→ ClockUI.OnEnable()
   │   └─→ GameEvents.OnClockChanged += UpdateClock
   │
   ├─→ MinimapController.OnEnable()
   │   ├─→ GameEvents.OnUnitSpawned += HandleSpawn
   │   ├─→ GameEvents.OnUnitDespawned += HandleDespawn
   │   ├─→ RebuildAll() (criar ícones de unidades existentes)
   │   └─→ SyncMinimapCamera()
   │
   └─→ ResourceDisplayUI.OnEnable() (futuro)
       └─→ GameEvents.OnResourceChanged += OnResourceChanged
   │
4. Start() de todos os MonoBehaviours
   ├─→ ClockUI.Start()
   │   └─→ Sincronizar com TimeManager (display inicial)
   │
   ├─→ ResourceDisplayUI.Start() (futuro)
   │   └─→ Sincronizar com ResourceManager
   │
   └─→ TimeManager.Start() (se houver)
   │
5. Primeiro Update()
   ├─→ TimeManager.Update()
   │   ├─→ Avançar Time01
   │   ├─→ Converter para HH:MM
   │   ├─→ GameEvents.RaiseClockChanged(...)
   │   │       │
   │   │       ↓
   │   │   ClockUI.UpdateClock(...)
   │   │       │
   │   │       └─→ ClockUI.Update() → Atualizar textos
   │   │
   │   └─→ GameEvents.RaiseTimeOfDay(Time01)
   │
   └─→ MinimapController.Update()
       ├─→ Seguir câmera principal (se followMainView)
       ├─→ SyncMinimapCamera()
       └─→ UpdateIcons() (posicionar ícones)
```

### 24.2 Primeira Unidade Spawn

```
[Unit GameObject Ativa]
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
    ↓ (Lote 5 - Minimapa)
MinimapController.HandleSpawn(unit)
    │
    ├─→ CreateIcon(unit)
    │   ├─→ GetIcon() (do pool ou Instantiate)
    │   ├─→ Colorir por facção (FactionDatabase)
    │   └─→ _icons[unit] = rt
    │
    └─→ [Ícone Visível no Minimapa]
```

---

# PARTE VIII: IMPLEMENTAÇÃO E MANUTENÇÃO

---

## 25) ROADMAP DE IMPLEMENTAÇÃO

### 25.1 Fase 1: Fundação (CONCLUÍDA ✅)

**Sistemas Core:**
- [x] TimeManager (avanço de tempo)
- [x] ClockUI (display de relógio)
- [x] MinimapController (ícones, zoom, click-to-move)
- [x] FactionDatabase (cores de facção)
- [x] GameEvents (eventos de tempo e unidades)

**Integração:**
- [x] TimeManager → ClockUI via `OnClockChanged`
- [x] UnitRegistry → MinimapController via `OnUnitSpawned/OnUnitDespawned`
- [x] RTSCamera → MinimapController (click-to-move)

### 25.2 Fase 2: Recursos e Economia (PRÓXIMA PRIORIDADE 🔥)

**Tarefas:**

1. **ResourceManager (1-2 dias)**
   ```
   - [ ] Criar ResourceManager.cs (singleton)
   - [ ] Implementar GetResource, AddResource, TrySpendResources
   - [ ] Integrar com GameEvents (OnResourceChanged)
   - [ ] Testar com script de debug (AddResource via botão)
   ```

2. **ResourceDisplayUI (1 dia)**
   ```
   - [ ] Criar ResourceDisplayUI.cs
   - [ ] Ligar refs no Inspector (goldText, woodText, foodText)
   - [ ] Subscribe em OnResourceChanged
   - [ ] Testar atualização de UI
   ```

3. **Integração com Gameplay (2-3 dias)**
   ```
   - [ ] Construções consomem recursos
   - [ ] Treinar unidades consome recursos
   - [ ] Farms/minas produzem recursos
   - [ ] Feedback visual (sem recursos suficientes)
   ```

### 25.3 Fase 3: Comandos e Ações (ALTA PRIORIDADE 🔥)

**Tarefas:**

1. **CommandController (2-3 dias)**
   ```
   - [ ] Criar CommandController.cs (Command Pattern)
   - [ ] Implementar MoveCommand, AttackCommand, DefendCommand, HoldCommand
   - [ ] Integrar com SelectionManager
   - [ ] Testar comandos via hotkeys (S = Stop, D = Defend)
   ```

2. **UnitActionButtons (1-2 dias)**
   ```
   - [ ] Criar UnitActionButtons.cs
   - [ ] Criar prefab de botões (Move, Attack, Defend, etc)
   - [ ] Ligar botões ao CommandController
   - [ ] Atualizar visuais baseado em seleção
   ```

3. **GroupHotkeyManager (1-2 dias)**
   ```
   - [ ] Criar GroupHotkeyManager.cs
   - [ ] Implementar Ctrl+1~9 (criar grupo)
   - [ ] Implementar 1~9 (selecionar grupo)
   - [ ] Integrar com UnitListPanel (grupos aparecem em ambos)
   - [ ] Feedback visual (slot de hotkey ocupado)
   ```

4. **InputManager (1 dia)**
   ```
   - [ ] Criar InputManager.cs (centralizar input)
   - [ ] Mapear hotkeys (WASD = mover câmera, Ctrl+1~9 = grupos, etc)
   - [ ] Permitir rebinding (opcional)
   ```

### 25.4 Fase 4: Social e Multiplayer (MÉDIA PRIORIDADE)

**Tarefas:**

1. **RankingSystem (2-3 dias)**
   ```
   - [ ] Criar RankingSystem.cs
   - [ ] Implementar cálculo de score (unidades + construções + recursos)
   - [ ] Update periódico (a cada 10s)
   - [ ] Integrar com GameEvents (OnRankingUpdated)
   ```

2. **RankingUI (1 dia)**
   ```
   - [ ] Criar RankingUI.cs + RankEntryUI.cs
   - [ ] Criar prefab de painel de ranking
   - [ ] Botão "Ranking" no Top HUD (toggle painel)
   - [ ] Lista de jogadores ordenados por score
   ```

3. **ChatSystem (3-4 dias)**
   ```
   - [ ] Criar ChatSystem.cs
   - [ ] Integrar com sistema de rede (Mirror/Netcode)
   - [ ] Implementar SendMessage, ReceiveMessage
   - [ ] Integrar com GameEvents (OnChatMessageReceived)
   ```

4. **ChatUI (1-2 dias)**
   ```
   - [ ] Criar ChatUI.cs + ChatMessageUI.cs
   - [ ] Criar prefab de painel de chat
   - [ ] Input field + botão "Send"
   - [ ] ScrollRect com histórico de mensagens
   - [ ] Botão "Chat" no Top HUD (toggle painel)
   ```

### 25.5 Fase 5: Polimento e UX (BAIXA PRIORIDADE)

**Tarefas:**

1. **Notificações (2 dias)**
   ```
   - [ ] Sistema de notificações (toasts)
   - [ ] "Recursos insuficientes"
   - [ ] "Construção completa"
   - [ ] "Unidade treinada"
   ```

2. **Tooltips (1-2 dias)**
   ```
   - [ ] Tooltip ao hover em botões
   - [ ] Tooltip ao hover em recursos (mostrar taxa de produção)
   - [ ] Tooltip ao hover em unidades (stats)
   ```

3. **Animações (2-3 dias)**
   ```
   - [ ] Fade in/out de painéis
   - [ ] Pulse ao ganhar/perder recursos
   - [ ] Shake ao clicar sem recursos
   - [ ] Smooth scroll no minimapa
   ```

4. **Feedback Visual (1 dia)**
   ```
   - [ ] Flash ao gastar recursos
   - [ ] Partículas ao criar grupo
   - [ ] Som ao clicar em botão
   ```

### 25.6 Estimativa de Tempo Total

| Fase | Dias de Trabalho | Status |
|------|------------------|--------|
| Fase 1: Fundação | ~5 dias | ✅ Concluída |
| Fase 2: Recursos | ~5 dias | ⏳ Próxima |
| Fase 3: Comandos | ~7 dias | 🔜 Futura |
| Fase 4: Social | ~8 dias | 🔜 Futura |
| Fase 5: Polimento | ~6 dias | 🔜 Futura |
| **TOTAL** | **~31 dias** | **~6 semanas** |

---

## 26) ESTRUTURA DE ARQUIVOS

### 26.1 Organização de Scripts

```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── TopHUD/
│   │   │   ├── Time/
│   │   │   │   ├── TimeManager.cs                 ✅ Implementado
│   │   │   │   └── ClockUI.cs                     ✅ Implementado
│   │   │   │
│   │   │   ├── Resources/
│   │   │   │   ├── ResourceManager.cs             ⏳ Futuro
│   │   │   │   └── ResourceDisplayUI.cs           ⏳ Futuro
│   │   │   │
│   │   │   ├── Ranking/
│   │   │   │   ├── RankingSystem.cs               ⏳ Futuro
│   │   │   │   ├── RankingUI.cs                   ⏳ Futuro
│   │   │   │   └── RankEntryUI.cs                 ⏳ Futuro
│   │   │   │
│   │   │   └── Chat/
│   │   │       ├── ChatSystem.cs                  ⏳ Futuro
│   │   │       ├── ChatUI.cs                      ⏳ Futuro
│   │   │       └── ChatMessageUI.cs               ⏳ Futuro
│   │   │
│   │   ├── ButtonHUD/
│   │   │   ├── CommandController.cs               ⏳ Futuro
│   │   │   ├── UnitActionButtons.cs               ⏳ Futuro
│   │   │   └── GroupHotkeyManager.cs              ⏳ Futuro
│   │   │
│   │   ├── Minimap/
│   │   │   ├── MinimapController.cs               ✅ Implementado
│   │   │   └── DisableMinimapShadows.cs           ✅ Implementado
│   │   │
│   │   └── UnitList/ (Lote 5 - Parte 1)
│   │       ├── Core/
│   │       │   ├── UnitListPanel.cs               ✅ Documentado
│   │       │   ├── ListItemWrapper.cs             ✅ Documentado
│   │       │   └── IListItemModel.cs              ✅ Documentado
│   │       └── ...
│   │
│   ├── Core/
│   │   ├── GameEvents.cs (Lote 1)                 ✅ Existente
│   │   ├── GameConfig.cs (Lote 1)                 ✅ Existente
│   │   └── ...
│   │
│   ├── Factions/
│   │   ├── FactionDatabase.cs                     ✅ Implementado
│   │   └── FactionDefinition.cs                   (inline no database)
│   │
│   └── Input/ (futuro)
│       └── InputManager.cs                        ⏳ Futuro
│
└── Prefabs/
    ├── UI/
    │   ├── TopHUD.prefab
    │   ├── ButtonHUD.prefab
    │   ├── Minimap.prefab
    │   ├── MinimapIcon.prefab
    │   └── UnitList.prefab (Parte 1)
    │
    └── ...
```

### 26.2 ScriptableObjects

```
Assets/
└── Data/
    ├── Config/
    │   └── GameConfig.asset                       ✅ Existente
    │
    └── Factions/
        └── FactionDatabase.asset                  ✅ Existente
```

---

## 27) SOLUÇÃO DE PROBLEMAS

### 27.1 Problema: "Relógio não atualiza"

**Sintomas:**
- ClockUI mostra "Day 0, Time 00:00" e não muda
- Console não mostra erros

**Diagnóstico:**
```csharp
// No ClockUI.OnEnable()
void OnEnable()
{
    Debug.Log("[ClockUI] OnEnable - subscribing to OnClockChanged");
    GameEvents.OnClockChanged += UpdateClock;
}

// No TimeManager.Update()
if (Minute != _lastMinute)
{
    Debug.Log($"[TimeManager] Clock changed: Day={DayCount}, Hour={Hour}, Minute={Minute}");
    GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
}
```

**Causas Comuns:**
1. `TimeManager.config` não está ligado (null)
2. `ClockUI.OnEnable()` não foi chamado (objeto desativado)
3. `GameEvents.OnClockChanged` não tem listeners

**Solução:**
```csharp
// Validar config
void Update()
{
    if (config == null)
    {
        Debug.LogError("[TimeManager] GameConfig is null!");
        return;
    }
    // ... resto
}

// Validar ClockUI ativo
void Start()
{
    if (!gameObject.activeInHierarchy)
    {
        Debug.LogWarning("[ClockUI] GameObject is not active!");
    }
}
```

---

### 27.2 Problema: "Ícones do minimapa não aparecem"

**Sintomas:**
- Minimapa vazio (sem ícones de unidades)
- Unidades existem na cena

**Diagnóstico:**
```csharp
// No MinimapController.HandleSpawn()
void HandleSpawn(Unit u)
{
    Debug.Log($"[MinimapController] Spawn: {u?.DisplayName}");
    CreateIcon(u);
}

// No CreateIcon()
void CreateIcon(Unit u)
{
    if (u == null)
    {
        Debug.LogWarning("[MinimapController] CreateIcon: unit is null!");
        return;
    }
    
    if (_icons.ContainsKey(u))
    {
        Debug.LogWarning($"[MinimapController] Icon already exists for {u.DisplayName}");
        return;
    }
    
    Debug.Log($"[MinimapController] Creating icon for {u.DisplayName}");
    // ... resto
}
```

**Causas Comuns:**
1. `minimapCamera` não está ligado
2. `iconPrefab` não está ligado
3. `iconsRoot` não está ligado
4. Unidades spawnadas antes de `MinimapController.OnEnable()`
5. Culling esconde ícones (`hideIconsOutside = true`)

**Solução:**
```csharp
// Validar refs no Awake
void Awake()
{
    if (minimapCamera == null)
        Debug.LogError("[MinimapController] minimapCamera is null!");
    
    if (iconPrefab == null)
        Debug.LogError("[MinimapController] iconPrefab is null!");
    
    if (iconsRoot == null)
        Debug.LogError("[MinimapController] iconsRoot is null!");
}

// RebuildAll no OnEnable (pega unidades já spawnadas)
void OnEnable()
{
    // ... subscribe ...
    
    RebuildAll(); // IMPORTANTE
    SyncMinimapCamera();
}
```

---

### 27.3 Problema: "Click no minimapa não move câmera"

**Sintomas:**
- Clicar no minimapa não faz nada
- Ou move para posição errada

**Diagnóstico:**
```csharp
public void OnPointerClick(PointerEventData e)
{
    Debug.Log($"[MinimapController] Click: button={e.button}, pos={e.position}");
    
    // ... conversão ...
    
    Debug.Log($"[MinimapController] Viewport: u={u}, v={v}");
    Debug.Log($"[MinimapController] World: {world}");
    
    // ...
}
```

**Causas Comuns:**
1. `rtsCamera` não está ligado
2. `minimapCamera` não está ligado
3. `minimapImage` não tem `GraphicRaycaster`
4. Canvas não tem `GraphicRaycaster`
5. Raycast não acerta o chão (`groundMask` incorreto)

**Solução:**
```csharp
// Validar refs
void Awake()
{
    if (rtsCamera == null)
        Debug.LogError("[MinimapController] rtsCamera is null!");
    
    if (minimapImage == null)
        Debug.LogError("[MinimapController] minimapImage is null!");
}

// Validar raycast
public void OnPointerClick(PointerEventData e)
{
    // ... código existente ...
    
    if (Physics.Raycast(ray, out var hit, 50000f, groundMask, QueryTriggerInteraction.Ignore))
    {
        Debug.Log($"[MinimapController] Raycast HIT: {hit.point}");
        world = hit.point;
    }
    else
    {
        Debug.LogWarning("[MinimapController] Raycast MISS - using plane fallback");
        // ... plane fallback ...
    }
}
```

---

### 27.4 Problema: "Zoom do minimapa não funciona"

**Sintomas:**
- Scroll/botões não mudam zoom
- Zoom está travado

**Diagnóstico:**
```csharp
public void OnScroll(PointerEventData eventData)
{
    Debug.Log($"[MinimapController] Scroll: delta={eventData.scrollDelta}");
    
    float delta = -eventData.scrollDelta.y * zoomScrollSensitivity;
    Debug.Log($"[MinimapController] Zoom delta: {delta}, zoom={zoom}");
    
    SetZoom(zoom + delta);
}

void SetZoom(float z)
{
    float oldZoom = zoom;
    zoom = Mathf.Clamp(z, minZoom, maxZoom);
    Debug.Log($"[MinimapController] SetZoom: {oldZoom} → {zoom}");
    
    SyncMinimapCamera();
}
```

**Causas Comuns:**
1. `minimapImage` não implementa `IScrollHandler` (mas `MinimapController` sim)
2. `EventSystem` ausente
3. `minZoom == maxZoom` (range inválido)

**Solução:**
```csharp
// Verificar range
void OnValidate()
{
    if (minZoom >= maxZoom)
    {
        Debug.LogWarning("[MinimapController] minZoom >= maxZoom - fixing range");
        minZoom = 0.5f;
        maxZoom = 3.0f;
    }
}

// Verificar EventSystem
void Start()
{
    var es = FindFirstObjectByType<EventSystem>();
    if (es == null)
    {
        Debug.LogError("[MinimapController] EventSystem not found in scene!");
    }
}
```

---

### 27.5 Problema: "Ícones aparecem fora do minimapa"

**Sintomas:**
- Ícones ultrapassam bordas do minimapa
- Aparecem em posições incorretas

**Diagnóstico:**
```csharp
void UpdateIcons()
{
    // ...
    
    Vector3 vp = minimapCamera.WorldToViewportPoint(u.transform.position);
    Debug.Log($"[Icon] {u.DisplayName}: viewport=({vp.x}, {vp.y}, {vp.z})");
    
    // ...
}
```

**Causas:**
1. `clampIconsInside = false`
2. `uvBorderTolerance` muito alto
3. Câmera do minimapa mal configurada

**Solução:**
```csharp
// Forçar clamp
void UpdateIcons()
{
    // ...
    
    float uNorm = Mathf.Clamp01(vp.x); // SEMPRE clampar
    float vNorm = Mathf.Clamp01(vp.y);
    
    // ...
}

// Verificar câmera
void SyncMinimapCamera()
{
    if (!minimapCamera) return;
    
    // Garantir modo ortográfico
    if (!minimapCamera.orthographic)
    {
        Debug.LogWarning("[MinimapController] Camera is not orthographic!");
        minimapCamera.orthographic = true;
    }
    
    // ...
}
```

---

## 28) CHECKLIST DE VALIDAÇÃO

### 28.1 Top HUD (Time)

**TimeManager:**
- [ ] `GameConfig` ligado no Inspector
- [ ] `startTime01` configurado (ex: 0.25 = amanhecer)
- [ ] Play → Time01 avança (watch no Inspector)
- [ ] Console mostra logs de `RaiseClockChanged` (se debug ativo)

**ClockUI:**
- [ ] `timeManager` ligado no Inspector
- [ ] `clockText` ligado (TMP_Text "Time XX:XX")
- [ ] `dayText` ligado (TMP_Text "Day X")
- [ ] Play → Relógio atualiza a cada minuto in-game
- [ ] Dia avança ao chegar 24:00

### 28.2 Top HUD (Resources - Futuro)

**ResourceManager:**
- [ ] Singleton funciona (só uma instância)
- [ ] `startingGold/Wood/Food` configurados
- [ ] Play → Console mostra recursos iniciais
- [ ] AddResource() aumenta recursos
- [ ] TrySpendResources() retorna false se insuficiente

**ResourceDisplayUI:**
- [ ] Textos ligados (goldText, woodText, foodText)
- [ ] Play → Mostra valores iniciais
- [ ] AddResource() → UI atualiza imediatamente
- [ ] SpendResources() → UI diminui valores

### 28.3 Button HUD (Comandos - Futuro)

**CommandController:**
- [ ] `selectionManager` ligado
- [ ] Selecionar unidades → OrderMove() funciona
- [ ] Right-click no mundo → Unidades movem
- [ ] Hotkey "S" → Unidades param (Hold)

**UnitActionButtons:**
- [ ] Botões ligados (moveButton, attackButton, etc)
- [ ] Nenhuma seleção → Botões desabilitados
- [ ] Selecionar unidade → Botões habilitados
- [ ] Clicar botão → Comando executado

**GroupHotkeyManager:**
- [ ] Ctrl+2 → Cria grupo (ou atualiza)
- [ ] 2 → Seleciona grupo
- [ ] Duplo 2 → Centraliza câmera no grupo (futuro)

### 28.4 Minimapa

**MinimapController:**
- [ ] Refs ligadas (minimapImage, iconsRoot, iconPrefab, minimapCamera, rtsCamera, factionDb)
- [ ] Play → Ícones aparecem para unidades existentes
- [ ] Spawnar unidade → Ícone aparece
- [ ] Despawnar unidade → Ícone desaparece
- [ ] Ícones têm cores corretas (baseado em facção)
- [ ] Click no minimapa → Câmera move para posição
- [ ] Scroll no minimapa → Zoom funciona
- [ ] Botões +/- → Zoom funciona
- [ ] `followMainView = true` → Minimapa segue câmera principal

**DisableMinimapShadows:**
- [ ] `minimapCamera` ligado
- [ ] Play → Minimapa sem sombras (performance melhor)

### 28.5 Integração

**GameEvents:**
- [ ] `OnTimeOfDay` disparado (todo frame)
- [ ] `OnDayChanged` disparado (ao virar dia)
- [ ] `OnClockChanged` disparado (a cada minuto)
- [ ] `OnUnitSpawned` disparado (ao spawnar)
- [ ] `OnUnitDespawned` disparado (ao despawnar)
- [ ] `OnSelectionChanged` disparado (ao mudar seleção)

**FactionDatabase:**
- [ ] ScriptableObject criado
- [ ] Facções configuradas (cor, ícone)
- [ ] Minimapa usa cores corretas

---

## 29) MELHORIAS SUGERIDAS

### 29.1 Melhorias de UX

#### 1. Animação de Transição de Painéis

```csharp
/// <summary>
/// Anima abertura/fechamento de painel (Ranking/Chat).
/// </summary>
public class PanelAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private float duration = 0.3f;
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Fade in
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration);
        
        // Scale in
        panel.localScale = Vector3.one * 0.8f;
        panel.DOScale(1f, duration).SetEase(Ease.OutBack);
    }
    
    public void Hide()
    {
        // Fade out
        canvasGroup.DOFade(0f, duration).OnComplete(() => {
            gameObject.SetActive(false);
        });
        
        // Scale out
        panel.DOScale(0.8f, duration).SetEase(Ease.InBack);
    }
}
```

#### 2. Tooltip System

```csharp
/// <summary>
/// Sistema de tooltips (ao hover em botões/recursos).
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipText;
    [SerializeField] private float delay = 0.5f;
    
    private Coroutine _showCoroutine;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _showCoroutine = StartCoroutine(ShowTooltipDelayed());
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }
        
        TooltipManager.Instance.Hide();
    }
    
    IEnumerator ShowTooltipDelayed()
    {
        yield return new WaitForSeconds(delay);
        TooltipManager.Instance.Show(tooltipText, Input.mousePosition);
    }
}
```

#### 3. Notificações (Toasts)

```csharp
/// <summary>
/// Sistema de notificações temporárias.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    [SerializeField] private NotificationUI notificationPrefab;
    [SerializeField] private Transform container;
    
    public void ShowNotification(string text, NotificationType type)
    {
        var notif = Instantiate(notificationPrefab, container);
        notif.SetData(text, type);
        notif.Show();
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
```

### 29.2 Melhorias de Performance

#### 1. Frustum Culling de Ícones

```csharp
/// <summary>
/// Culling de ícones baseado em frustum da câmera do minimapa.
/// Mais preciso que culling por UV.
/// </summary>
void UpdateIcons()
{
    var planes = GeometryUtility.CalculateFrustumPlanes(minimapCamera);
    
    foreach (var kv in _icons)
    {
        var u = kv.Key;
        var rt = kv.Value;
        
        // Testar se está dentro do frustum
        bool visible = GeometryUtility.TestPlanesAABB(planes, u.bounds);
        
        rt.gameObject.SetActive(visible);
        
        if (visible)
        {
            // Atualizar posição
            // ...
        }
    }
}
```

#### 2. Update Throttling

```csharp
/// <summary>
/// Atualizar ícones a cada N frames (ao invés de todo frame).
/// </summary>
[Header("Performance")]
[SerializeField] private int updateEveryNFrames = 2;
private int _frameCounter = 0;

void Update()
{
    _frameCounter++;
    
    if (_frameCounter % updateEveryNFrames == 0)
    {
        UpdateIcons();
    }
    
    // Outros updates sempre rodam
    if (followMainView && mainCamera != null)
    {
        // ...
    }
}
```

#### 3. Dirty Flag Pattern

```csharp
/// <summary>
/// Só atualizar ClockUI quando valores mudarem.
/// </summary>
public class ClockUI : MonoBehaviour
{
    private int _cachedDay, _cachedHour, _cachedMinute;
    private bool _isDirty = false;
    
    public void UpdateClock(int day, int hour, int minute)
    {
        if (_cachedDay != day || _cachedHour != hour || _cachedMinute != minute)
        {
            _cachedDay = day;
            _cachedHour = hour;
            _cachedMinute = minute;
            _isDirty = true;
        }
    }
    
    void Update()
    {
        if (_isDirty)
        {
            RenderClock();
            _isDirty = false;
        }
    }
    
    void RenderClock()
    {
        clockText.text = $"Time {_cachedHour:00}:{_cachedMinute:00}";
        dayText.text = $"Day {_cachedDay}";
    }
}
```

### 29.3 Melhorias de Qualidade de Código

#### 1. Validator Pattern

```csharp
/// <summary>
/// Validar refs no Inspector (evita nulls em runtime).
/// </summary>
public class MinimapController : MonoBehaviour
{
    void OnValidate()
    {
        ValidateReferences();
    }
    
    void ValidateReferences()
    {
        if (minimapCamera == null)
            Debug.LogWarning("[MinimapController] minimapCamera is not assigned!", this);
        
        if (iconPrefab == null)
            Debug.LogWarning("[MinimapController] iconPrefab is not assigned!", this);
        
        if (iconsRoot == null)
            Debug.LogWarning("[MinimapController] iconsRoot is not assigned!", this);
        
        // ... outras validações
    }
}
```

#### 2. Builder Pattern (Commands)

```csharp
/// <summary>
/// Builder para comandos complexos.
/// </summary>
public class CommandBuilder
{
    private List<ICommand> _commands = new();
    
    public CommandBuilder Move(Vector3 position)
    {
        _commands.Add(new MoveCommand { TargetPosition = position });
        return this;
    }
    
    public CommandBuilder Attack(Unit target)
    {
        _commands.Add(new AttackCommand { TargetUnit = target });
        return this;
    }
    
    public CommandBuilder Hold()
    {
        _commands.Add(new HoldCommand());
        return this;
    }
    
    public CompositeCommand Build()
    {
        return new CompositeCommand(_commands);
    }
}

// Uso:
var command = new CommandBuilder()
    .Move(targetPosition)
    .Attack(enemy)
    .Hold()
    .Build();

commandController.ExecuteCommand(command);
```

#### 3. Object Pool Genérico

```csharp
/// <summary>
/// Pool genérico reutilizável.
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _pool = new();
    
    public ObjectPool(T prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }
    
    public T Get()
    {
        T obj;
        
        if (_pool.Count > 0)
        {
            obj = _pool.Pop();
        }
        else
        {
            obj = Object.Instantiate(_prefab, _parent);
        }
        
        obj.gameObject.SetActive(true);
        return obj;
    }
    
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Push(obj);
    }
}

// Uso no MinimapController:
private ObjectPool<RectTransform> _iconPool;

void Awake()
{
    _iconPool = new ObjectPool<RectTransform>(iconPrefab, iconsRoot);
}

RectTransform GetIcon() => _iconPool.Get();
void ReturnIcon(RectTransform rt) => _iconPool.Return(rt);
```

---

**FIM DO LOTE 5 (PARTE 2) - TOP HUD, BUTTON HUD & MINIMAPA (DOCUMENTAÇÃO COMPLETA)**

---

**Documento mantido por:** Equipe de Desenvolvimento  
**Última atualização:** Outubro 2025  
**Versão:** 2.1 (Híbrida: Código Real + Arquitetura Futura)  
**Website:** https://luciano-claudio.github.io/MedievalThrones  

---
