// ListItemWrapper.cs
// REFATORADO (Lote 5): Simplificado para eliminar classe interna redundante

using System.Collections.Generic;

/// <summary>
/// Wrapper unificado que pode representar tanto uma Unit quanto um UnitGroup.
/// Simplifica a interface e elimina a classe interna UnitWrapper desnecessária.
/// </summary>
public class ListItemWrapper : IListItemModel
{
    public IListItemModel Model { get; }

    // Construtor para uma Unit (agora cria um modelo inline)
    public ListItemWrapper(Unit unit)
    {
        Model = new SimpleUnitModel(unit);
    }

    // Construtor para um UnitGroup (passa direto, pois já implementa IListItemModel)
    public ListItemWrapper(UnitGroup group)
    {
        Model = group;
    }

    // Pass-through da interface
    public string DisplayName => Model.DisplayName;
    public bool IsGroup => Model.IsGroup;
    public Unit GetUnit() => Model.GetUnit();
    public IReadOnlyList<Unit> GetUnitsInItem() => Model.GetUnitsInItem();

    // ==================== MODELO INTERNO SIMPLIFICADO ====================

    /// <summary>
    /// Modelo simples para uma Unit individual.
    /// REFATORAÇÃO: Renomeado de UnitWrapper para SimpleUnitModel para maior clareza.
    /// Implementa IListItemModel para consistência.
    /// </summary>
    private class SimpleUnitModel : IListItemModel
    {
        private readonly Unit _unit;

        public SimpleUnitModel(Unit unit) => _unit = unit;

        public string DisplayName => _unit != null ? _unit.DisplayName : "null";
        public bool IsGroup => false;
        public Unit GetUnit() => _unit;
        public IReadOnlyList<Unit> GetUnitsInItem() => _unit != null ? new List<Unit> { _unit } : new List<Unit>();
    }
}