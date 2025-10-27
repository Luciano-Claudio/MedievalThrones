using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    // ========== TEMPO ==========
    public static event Action<float> OnTimeOfDay01;          // 0..1 ao longo do dia
    public static event Action<int> OnDayChanged;             // dia inteiro (0,1,2...)
    public static event Action<int, int, int> OnClockChanged; // dia, hora, minuto

    // ========== ECONOMIA ==========
    public static event Action<FactionId, ResourceType, int> OnResourceGathered;

    // ========== DIPLOMACIA ==========
    public static event Action OnReputationMatrixReady;
    public static event Action<FactionId, FactionId, float> OnReputationChanged;

    // ========== CÂMERA ==========
    /// <summary>Shake de câmera disparado por eventos de jogo (impactos, explosões)</summary>
    public static event Action<float, float, float> OnCameraShake; // amplitude, frequency, duration

    /// <summary>Foco em uma posição 3D (ex: unidade selecionada, objetivo de missão)</summary>
    public static event Action<Vector3, bool, float> OnCameraFocus; // worldPos, snap, duration

    /// <summary>Foco em posição XZ do mini-mapa</summary>
    public static event Action<Vector2, bool, float> OnCameraFocusXZ; // worldXZ, snap, duration

    /// <summary>Iniciar cutscene apontando para um alvo</summary>
    public static event Action<Transform, float, int> OnCutsceneStart; // target, fov, priority

    /// <summary>Finalizar cutscene atual</summary>
    public static event Action OnCutsceneEnd;

    // ========== SELEÇÃO / MINIMAP ==========
    /// <summary>Ping no mini-mapa (jogador clica no mapa)</summary>
    public static event Action<Vector2> OnMinimapPing; // worldXZ

    /// <summary>Unidade/construção selecionada (para foco automático opcional)</summary>
    public static event Action<Transform> OnSelectionFocus; // target transform

    // ========== UNIDADES ==========
    /// <summary>Disparado quando unidade spawna na cena</summary>
    public static event Action<Unit> OnUnitSpawned;

    /// <summary>Disparado quando unidade é removida da cena</summary>
    public static event Action<Unit> OnUnitDespawned;

    /// <summary>Disparado quando seleção de unidade muda (disparado por Unit.SetSelected)</summary>
    public static event Action<Unit, bool> OnUnitSelectionChanged; // unit, isSelected

    /// <summary>Disparado quando XP ou Level de unidade mudam</summary>
    public static event Action<Unit> OnUnitProgressChanged;

    /// <summary>
    /// NOVO: Disparado quando uma unidade morre
    /// Usado pelo FormationGroupManager para remover unidades de grupos
    /// </summary>
    public static event Action<Unit> OnUnitDied;

    // ========== SELEÇÃO (NOVO - Refatoração Módulo Selection) ==========
    /// <summary>Disparado quando a seleção de unidades muda (conjunto completo)</summary>
    public static event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;

    /// <summary>Disparado quando jogador clica em uma unidade</summary>
    public static event Action<Unit, bool> OnUnitClick; // unit, ctrlPressed

    /// <summary>Disparado quando jogador duplo-clica em uma unidade</summary>
    public static event Action<Unit> OnUnitDoubleClick;

    /// <summary>Disparado quando jogador clica no chão/terreno</summary>
    public static event Action<Vector3, bool> OnGroundClick; // worldPoint, ctrlPressed

    /// <summary>Disparado quando jogador inicia drag de seleção</summary>
    public static event Action<Vector2> OnDragBegin; // screenPos

    /// <summary>Disparado durante drag de seleção (cada frame)</summary>
    public static event Action<Vector2> OnDragging; // screenPos

    /// <summary>Disparado quando jogador finaliza drag de seleção</summary>
    public static event Action<Vector2> OnDragEnd; // screenPos

    /// <summary>Disparado quando ponteiro do mouse é pressionado</summary>
    public static event Action<Vector2> OnPointerDown; // screenPos

    /// <summary>Disparado quando ponteiro do mouse é liberado</summary>
    public static event Action<Vector2> OnPointerUp; // screenPos

    // ========== GRUPOS (NOVO - Refatoração Lote 5: UI/Left Bar) ==========
    /// <summary>Disparado quando um grupo de unidades é criado</summary>
    public static event Action<UnitGroup> OnGroupCreated;

    /// <summary>Disparado quando um grupo de unidades é deletado</summary>
    public static event Action<UnitGroup> OnGroupDeleted;

    /// <summary>Disparado quando um grupo é renomeado</summary>
    public static event Action<UnitGroup, string> OnGroupRenamed; // group, newName

    /// <summary>Disparado quando unidades são adicionadas a um grupo</summary>
    public static event Action<UnitGroup, IReadOnlyList<Unit>> OnUnitsAddedToGroup;

    /// <summary>Disparado quando unidades são removidas de um grupo</summary>
    public static event Action<UnitGroup, IReadOnlyList<Unit>> OnUnitsRemovedFromGroup;

    /// <summary>
    /// Disparado quando o jogador clica com botão esquerdo no terreno para mover unidades
    /// </summary>
    public static event Action<Vector3> OnMoveCommand;

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

    /// <summary>
    /// NOVO: Notifica que uma unidade morreu
    /// Chame este método no Unit.Die() ou sistema de combate
    /// </summary>
    public static void RaiseUnitDied(Unit unit)
        => OnUnitDied?.Invoke(unit);

    // --- Seleção (NOVO - Refatoração Módulo Selection) ---
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

    // --- Grupos (NOVO - Refatoração Lote 5: UI/Left Bar) ---
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

    /// <summary>
    /// Disparado quando o jogador clica com botão esquerdo no terreno para mover unidades
    /// </summary>
    public static void RaiseMoveCommand(Vector3 worldPosition)
        => OnMoveCommand?.Invoke(worldPosition);
}