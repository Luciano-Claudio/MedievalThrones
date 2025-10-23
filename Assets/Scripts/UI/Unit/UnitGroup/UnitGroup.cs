// UnitGroup.cs (classe de dados que será injetada no GroupListItemUI)
using System.Collections.Generic;
using System.Linq;

public class UnitGroup : IListItemModel
{
    public string GroupName { get; set; } = "Novo Grupo";

    // As unidades que pertencem a este grupo
    public List<Unit> Units { get; set; } = new List<Unit>();

    public string ID { get; private set; } = System.Guid.NewGuid().ToString();

    // Estado de UI (expansão) - será usada para esconder os itens filhos.
    public bool IsExpanded { get; set; } = true;

    // NOVO: altura preferida do item visual do grupo (LayoutElement.preferredHeight)
    public float PreferredHeight { get; set; } = -1f;

    // IListItemModel Implementation
    public string DisplayName => GroupName;
    public bool IsGroup => true;
    public Unit GetUnit() => null;
    public IReadOnlyList<Unit> GetUnitsInItem() => Units;
}
