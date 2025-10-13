// ListItemWrapper.cs
using System.Collections.Generic;
using System.Linq;

// O elemento que o UnitListPanel._order irá realmente armazenar.
public class ListItemWrapper : IListItemModel
{
    public IListItemModel Model { get; }

    // Construtor para uma Unit
    public ListItemWrapper(Unit unit)
    {
        Model = new UnitWrapper(unit);
    }

    // Construtor para um UnitGroup
    public ListItemWrapper(UnitGroup group)
    {
        Model = group;
    }

    // Pass-through da interface
    public string DisplayName => Model.DisplayName;
    public bool IsGroup => Model.IsGroup;
    public Unit GetUnit() => Model.GetUnit();
    public IReadOnlyList<Unit> GetUnitsInItem() => Model.GetUnitsInItem();


    // Wrapper interno para a Unit
    private class UnitWrapper : IListItemModel
    {
        private readonly Unit _unit;

        public UnitWrapper(Unit unit) => _unit = unit;

        public string DisplayName => _unit.DisplayName;
        public bool IsGroup => false;
        public Unit GetUnit() => _unit;
        public IReadOnlyList<Unit> GetUnitsInItem() => new List<Unit> { _unit };
    }
}