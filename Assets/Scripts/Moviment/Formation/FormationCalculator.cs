using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Calculador de formações de tropas
/// Implementa as 6 formações com priorização por tipo de unidade
/// </summary>
public static class FormationCalculator
{
    // ========== CONSTANTES ==========

    private const float DEFAULT_SPACING = 2f;
    private const int DEFAULT_UNITS_PER_ROW = 10;
    private const float PASSAGE_WIDTH = 4f; // Largura da passagem central (2 unidades de espaço)
    private const float OUTLIER_THRESHOLD = 15f; // Distância para considerar uma unidade como outlier

    // ========== API PÚBLICA ==========

    /// <summary>
    /// Calcula as posições de formação para uma lista de unidades
    /// </summary>
    /// <param name="units">Lista de unidades</param>
    /// <param name="formation">Tipo de formação</param>
    /// <param name="spacing">Espaçamento entre unidades</param>
    /// <returns>Dictionary com Unit → Offset de posição</returns>
    public static Dictionary<Unit, Vector3> CalculateFormation(
        List<Unit> units,
        FormationType formation,
        float spacing = DEFAULT_SPACING)
    {
        if (units == null || units.Count == 0)
            return new Dictionary<Unit, Vector3>();

        // Formação None retorna offsets zero para todos
        if (formation == FormationType.None)
        {
            var result = new Dictionary<Unit, Vector3>();
            foreach (var unit in units)
            {
                result[unit] = Vector3.zero;
            }
            return result;
        }

        // Ordenar unidades por prioridade (maior prioridade = frente)
        var sortedUnits = SortUnitsByPriority(units);

        // Calcular baseado no tipo de formação
        return formation switch
        {
            FormationType.Line => CalculateLine(sortedUnits, spacing),
            FormationType.Column => CalculateColumn(sortedUnits, spacing),
            FormationType.Triangular => CalculateTriangular(sortedUnits, spacing),
            FormationType.Wedge => CalculateWedge(sortedUnits, spacing),
            FormationType.Circular => CalculateCircular(sortedUnits, spacing),
            FormationType.Square => CalculateSquare(sortedUnits, spacing),
            _ => CalculateLine(sortedUnits, spacing)
        };
    }

    /// <summary>
    /// Calcula o centro inteligente de um grupo de unidades, ignorando outliers
    /// </summary>
    /// <param name="units">Lista de unidades</param>
    /// <returns>Posição central do grupo principal</returns>
    public static Vector3 CalculateIntelligentCenter(List<Unit> units)
    {
        if (units == null || units.Count == 0)
            return Vector3.zero;

        if (units.Count == 1)
            return units[0].transform.position;

        // Primeiro, calcular o centro bruto
        Vector3 roughCenter = Vector3.zero;
        foreach (var unit in units)
        {
            roughCenter += unit.transform.position;
        }
        roughCenter /= units.Count;

        // Identificar outliers (unidades muito distantes do centro)
        List<Unit> mainGroup = new List<Unit>();
        List<Unit> outliers = new List<Unit>();

        foreach (var unit in units)
        {
            float distance = Vector3.Distance(unit.transform.position, roughCenter);
            if (distance <= OUTLIER_THRESHOLD)
            {
                mainGroup.Add(unit);
            }
            else
            {
                outliers.Add(unit);
            }
        }

        // Se todos são outliers ou não há grupo principal, usar centro bruto
        if (mainGroup.Count == 0)
            return roughCenter;

        // Calcular centro refinado apenas com o grupo principal
        Vector3 refinedCenter = Vector3.zero;
        foreach (var unit in mainGroup)
        {
            refinedCenter += unit.transform.position;
        }
        refinedCenter /= mainGroup.Count;

        // Log para debug
        if (outliers.Count > 0)
        {
            Debug.Log($"<color=orange>[FormationCalculator] Centro inteligente: {mainGroup.Count} unidades principais, {outliers.Count} outliers ignorados</color>");
        }

        return refinedCenter;
    }

    // ========== ORDENAÇÃO ==========

    /// <summary>
    /// Ordena unidades por prioridade de formação (maior = frente)
    /// Secundário: por tipo de unidade (ordem definida)
    /// </summary>
    static List<Unit> SortUnitsByPriority(List<Unit> units)
    {
        return units
            .OrderByDescending(u => u.FormationPriority) // Maior prioridade primeiro
            .ThenBy(u => u.def != null ? (int)u.def.type : 99) // Tipo como desempate
            .ToList();
    }

    // ========== FORMAÇÃO 1: LINHA (CORRIGIDA) ==========

    /// <summary>
    /// Formação em Linha: múltiplas linhas de N unidades com passagem central
    /// Prioridades maiores vão nas primeiras linhas
    /// CORRIGIDO: Agora tem corredor central dividindo as unidades em dois grupos
    /// </summary>
    static Dictionary<Unit, Vector3> CalculateLine(List<Unit> units, float spacing)
    {
        var result = new Dictionary<Unit, Vector3>();
        int unitsPerRow = DEFAULT_UNITS_PER_ROW;
        int halfRow = unitsPerRow / 2; // 5 unidades de cada lado

        for (int i = 0; i < units.Count; i++)
        {
            int row = i / unitsPerRow;
            int colInRow = i % unitsPerRow;

            float offsetX;

            // Dividir em dois grupos com passagem central
            if (colInRow < halfRow)
            {
                // Lado esquerdo (0-4): posições negativas
                // colInRow = 0 → offsetX = -4.5 * spacing - halfPassage
                // colInRow = 4 → offsetX = -0.5 * spacing - halfPassage
                offsetX = (colInRow - halfRow + 0.5f) * spacing - (PASSAGE_WIDTH / 2f);
            }
            else
            {
                // Lado direito (5-9): posições positivas
                // colInRow = 5 → offsetX = 0.5 * spacing + halfPassage
                // colInRow = 9 → offsetX = 4.5 * spacing + halfPassage
                offsetX = (colInRow - halfRow + 0.5f) * spacing + (PASSAGE_WIDTH / 2f);
            }

            // Offset Z: linhas para trás
            float offsetZ = -row * spacing;

            result[units[i]] = new Vector3(offsetX, 0, offsetZ);
        }

        return result;
    }

    // ========== FORMAÇÃO 2: COLUNA (CORRIGIDA) ==========

    /// <summary>
    /// Formação em Coluna: múltiplas colunas com passagem central
    /// Ajusta dinamicamente o número de unidades por coluna baseado na quantidade total
    /// CORRIGIDO: Agora calcula dinamicamente o número de colunas e unidades por coluna
    /// </summary>
    static Dictionary<Unit, Vector3> CalculateColumn(List<Unit> units, float spacing)
    {
        var result = new Dictionary<Unit, Vector3>();

        // Calcular número de colunas baseado na quantidade de unidades
        int numColumns = CalculateOptimalColumnCount(units.Count);
        int unitsPerColumn = Mathf.CeilToInt((float)units.Count / numColumns);

        Debug.Log($"<color=cyan>[Column Formation] {units.Count} unidades → {numColumns} colunas, ~{unitsPerColumn} unidades/coluna</color>");

        for (int i = 0; i < units.Count; i++)
        {
            int col = i / unitsPerColumn; // Qual coluna (largura)
            int row = i % unitsPerColumn; // Posição dentro da coluna (profundidade)

            // Dividir colunas em dois grupos com passagem central
            float offsetX;
            int halfColumns = numColumns / 2;

            if (col < halfColumns)
            {
                // Lado esquerdo
                offsetX = (col - halfColumns + 0.5f) * spacing - (PASSAGE_WIDTH / 2f);
            }
            else
            {
                // Lado direito
                offsetX = (col - halfColumns + 0.5f) * spacing + (PASSAGE_WIDTH / 2f);
            }

            // Offset Z: profundidade da coluna
            float offsetZ = -row * spacing;

            result[units[i]] = new Vector3(offsetX, 0, offsetZ);
        }

        return result;
    }

    /// <summary>
    /// Calcula o número ótimo de colunas baseado na quantidade de unidades
    /// Garante que haja múltiplas colunas mesmo com poucas unidades
    /// </summary>
    static int CalculateOptimalColumnCount(int totalUnits)
    {
        if (totalUnits <= 0) return 2;

        // Menos de 10 unidades: 2 colunas (mínimo para ter "colunas")
        if (totalUnits < 10)
            return 2;

        // 10-20 unidades: 4 colunas
        if (totalUnits < 20)
            return 4;

        // 20-30 unidades: 6 colunas
        if (totalUnits < 30)
            return 6;

        // 30-50 unidades: 8 colunas
        if (totalUnits < 50)
            return 8;

        // 50+ unidades: máximo 10 colunas
        // Cada coluna terá aproximadamente totalUnits/10 unidades
        return Mathf.Min(10, Mathf.CeilToInt(totalUnits / 10f));
    }

    // ========== FORMAÇÃO 3: TRIANGULAR ==========

    /// <summary>
    /// Formação Triangular: triângulos inscritos com frente agressiva
    /// Sem passagem central - formação densa para romper linhas
    /// </summary>
    static Dictionary<Unit, Vector3> CalculateTriangular(List<Unit> units, float spacing)
    {
        var result = new Dictionary<Unit, Vector3>();

        int currentRow = 0;
        int currentPosInRow = 0;

        for (int i = 0; i < units.Count; i++)
        {
            int unitsInThisRow = currentRow + 1; // 1, 2, 3, 4, 5...

            // Centro da linha
            float rowWidth = (unitsInThisRow - 1) * spacing;
            float offsetX = (currentPosInRow * spacing) - (rowWidth / 2f);
            float offsetZ = -currentRow * spacing * 0.866f; // 0.866 = sin(60°) para triângulo

            result[units[i]] = new Vector3(offsetX, 0, offsetZ);

            // Próxima posição
            currentPosInRow++;
            if (currentPosInRow >= unitsInThisRow)
            {
                currentRow++;
                currentPosInRow = 0;
            }
        }

        return result;
    }

    // ========== FORMAÇÃO 4: CUNHA (PONTA DE FLECHA) ==========

    /// <summary>
    /// Formação Cunha: apenas a ponta do triângulo, sem a base
    /// Focada em penetração rápida e perseguição
    /// </summary>
    static Dictionary<Unit, Vector3> CalculateWedge(List<Unit> units, float spacing)
    {
        var result = new Dictionary<Unit, Vector3>();

        // Cunha é essencialmente um triângulo mais alongado
        // Cada linha tem no máximo +2 unidades que a anterior
        int currentRow = 0;
        int currentPosInRow = 0;
        int maxUnitsInRow = 1;

        for (int i = 0; i < units.Count; i++)
        {
            // Centro da linha
            float rowWidth = (maxUnitsInRow - 1) * spacing;
            float offsetX = (currentPosInRow * spacing) - (rowWidth / 2f);

            // Cunha mais alongada (factor 1.2 ao invés de 0.866)
            float offsetZ = -currentRow * spacing * 1.2f;

            result[units[i]] = new Vector3(offsetX, 0, offsetZ);

            // Próxima posição
            currentPosInRow++;
            if (currentPosInRow >= maxUnitsInRow)
            {
                currentRow++;
                currentPosInRow = 0;

                // Cunha: cada linha tem +2 unidades (1, 3, 5, 7, 9...)
                maxUnitsInRow = Mathf.Min(maxUnitsInRow + 2, DEFAULT_UNITS_PER_ROW);
            }
        }

        return result;
    }

    // ========== FORMAÇÃO 5: CIRCULAR ==========

    /// <summary>
    /// Formação Circular: círculos concêntricos
    /// Prioritários fora, menos prioritários dentro (defesa 360°)
    /// </summary>
    static Dictionary<Unit, Vector3> CalculateCircular(List<Unit> units, float spacing)
    {
        var result = new Dictionary<Unit, Vector3>();

        // Calcular quantos círculos precisamos
        int[] unitsPerRing = CalculateUnitsPerRing(units.Count, spacing);

        int unitIndex = 0;
        float currentRadius = spacing; // Primeiro anel

        foreach (int unitsInRing in unitsPerRing)
        {
            if (unitIndex >= units.Count) break;

            for (int i = 0; i < unitsInRing && unitIndex < units.Count; i++, unitIndex++)
            {
                // Ângulo para distribuir uniformemente
                float angle = (i / (float)unitsInRing) * Mathf.PI * 2f;

                float offsetX = Mathf.Cos(angle) * currentRadius;
                float offsetZ = Mathf.Sin(angle) * currentRadius;

                result[units[unitIndex]] = new Vector3(offsetX, 0, offsetZ);
            }

            // Próximo anel (mais para dentro)
            currentRadius += spacing * 1.5f;
        }

        return result;
    }

    /// <summary>
    /// Calcula quantas unidades cabem em cada anel circular
    /// </summary>
    static int[] CalculateUnitsPerRing(int totalUnits, float spacing)
    {
        List<int> rings = new List<int>();
        int remaining = totalUnits;
        float radius = spacing;

        while (remaining > 0)
        {
            // Circunferência = 2πr
            float circumference = 2f * Mathf.PI * radius;
            int unitsInRing = Mathf.Max(3, Mathf.FloorToInt(circumference / spacing));

            unitsInRing = Mathf.Min(unitsInRing, remaining);
            rings.Add(unitsInRing);
            remaining -= unitsInRing;
            radius += spacing * 1.5f;
        }

        return rings.ToArray();
    }

    // ========== FORMAÇÃO 6: QUADRÁTICA ==========

    /// <summary>
    /// Formação Quadrática: quadrado denso sem passagem central
    /// Máxima densidade para resistência e defesa de posição
    /// </summary>
    static Dictionary<Unit, Vector3> CalculateSquare(List<Unit> units, float spacing)
    {
        var result = new Dictionary<Unit, Vector3>();

        // Calcular lado do quadrado (raiz quadrada arredondada para cima)
        int sideLength = Mathf.CeilToInt(Mathf.Sqrt(units.Count));

        for (int i = 0; i < units.Count; i++)
        {
            int row = i / sideLength;
            int col = i % sideLength;

            // Centralizar o quadrado
            float halfSide = (sideLength - 1) * spacing / 2f;

            float offsetX = col * spacing - halfSide;
            float offsetZ = -row * spacing + halfSide;

            result[units[i]] = new Vector3(offsetX, 0, offsetZ);
        }

        return result;
    }

    // ========== UTILITÁRIOS ==========

    /// <summary>
    /// Calcula o centro geométrico de uma formação
    /// </summary>
    public static Vector3 GetFormationCenter(Dictionary<Unit, Vector3> formation)
    {
        if (formation.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var offset in formation.Values)
        {
            sum += offset;
        }

        return sum / formation.Count;
    }

    /// <summary>
    /// Retorna o raio aproximado da formação (distância máxima do centro)
    /// </summary>
    public static float GetFormationRadius(Dictionary<Unit, Vector3> formation)
    {
        if (formation.Count == 0) return 0f;

        Vector3 center = GetFormationCenter(formation);
        float maxDistance = 0f;

        foreach (var offset in formation.Values)
        {
            float distance = Vector3.Distance(offset, center);
            if (distance > maxDistance)
                maxDistance = distance;
        }

        return maxDistance;
    }

    /// <summary>
    /// Debug: Imprime informações sobre a formação
    /// </summary>
    public static void DebugFormation(Dictionary<Unit, Vector3> formation, FormationType type)
    {
        Debug.Log($"=== FORMAÇÃO {type} ===\n" +
                  $"Unidades: {formation.Count}\n" +
                  $"Centro: {GetFormationCenter(formation)}\n" +
                  $"Raio: {GetFormationRadius(formation):F2}");
    }
}