using UnityEngine;

/// <summary>
/// Marca um GameObject visual com a referência ao wrapper lógico correspondente.
/// Isso permite reordenar os filhos do 'content' sem heurísticas.
/// </summary>
public class ListItemMarker : MonoBehaviour
{
    public ListItemWrapper Wrapper; // setado no Build()
}
