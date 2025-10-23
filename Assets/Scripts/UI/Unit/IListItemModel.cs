using System.Collections.Generic;

public interface IListItemModel
{
    string DisplayName { get; }
    bool IsGroup { get; }
    // Retorna a Unit se for uma, ou null se for um Group.
    Unit GetUnit();
    // Retorna a lista de Units se for um Group, ou uma lista vazia/com a Unit se for um item.
    IReadOnlyList<Unit> GetUnitsInItem();
}