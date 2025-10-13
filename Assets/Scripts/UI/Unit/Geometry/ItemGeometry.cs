using UnityEngine;

/// <summary>
/// Estrutura auxiliar para armazenar dados geométricos estáticos de um item de lista.
/// Essencial para o cálculo estável do Limiar Ponderado Proporcional (PWT).
/// </summary>
public struct ItemGeometry
{
    // A referência ao RectTransform do item de UI
    public RectTransform Rect;

    // A altura estática, essencial para o cálculo PWT
    public float Height;

    // O topo do item no espaço de coordenadas de TELA (para hit-test)
    public float TopY;

    public ItemGeometry(RectTransform rect, float height, float topY)
    {
        Rect = rect;
        Height = height;
        TopY = topY;
    }
}